using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ID3_TagIT.Tests
{
  public class ScanForFilesTests
  {
    // root\b.mp3, root\a\a1.mp3, root\a\x\ax.mp3, root\c\c1.mp3, root\c\skip.txt
    private static string MakeTree(TempFolder folder)
    {
      string root = folder.Folder("root");
      string a = Directory.CreateDirectory(Path.Combine(root, "a")).FullName;
      string ax = Directory.CreateDirectory(Path.Combine(a, "x")).FullName;
      string c = Directory.CreateDirectory(Path.Combine(root, "c")).FullName;

      foreach (string file in new[] { Path.Combine(root, "b.mp3"), Path.Combine(a, "a1.mp3"), Path.Combine(ax, "ax.mp3"), Path.Combine(c, "c1.mp3"), Path.Combine(c, "skip.txt") })
        File.WriteAllText(file, "");

      return root;
    }

    private static List<string> Scan(string root, bool subfolders, System.Func<bool> canceled, List<string> scannedFolders)
    {
      ArrayList files = new ArrayList();
      Id3TagIT_Main.ScanForFiles(new ArrayList { root }, "*.mp3", subfolders, ref files, canceled, folder => scannedFolders.Add(folder));

      return files.Cast<string>().Select(f => f.Substring(root.Length + 1)).ToList();
    }

    [Fact]
    public void FindsSubfoldersBeforeEachFoldersOwnFiles()
    {
      using (TempFolder folder = new TempFolder())
      {
        List<string> scanned = new List<string>();
        List<string> files = Scan(MakeTree(folder), true, () => false, scanned);

        Assert.Equal(new[] { @"a\x\ax.mp3", @"a\a1.mp3", @"c\c1.mp3", "b.mp3" }, files);
        Assert.Equal(4, scanned.Count);
      }
    }

    [Fact]
    public void WithoutSubfoldersFindsOnlyTheTopLevel()
    {
      using (TempFolder folder = new TempFolder())
      {
        Assert.Equal(new[] { "b.mp3" }, Scan(MakeTree(folder), false, () => false, new List<string>()));
      }
    }

    [Fact]
    public void StopsWhenCancelled()
    {
      using (TempFolder folder = new TempFolder())
      {
        List<string> scanned = new List<string>();
        List<string> files = Scan(MakeTree(folder), true, () => scanned.Count >= 1, scanned);

        // Cancelling stops the scan going into another folder. root\a\x finishes first, then
        // root\a still collects its own files; root\c and root's own files are skipped.
        Assert.Equal(new[] { @"a\x\ax.mp3", @"a\a1.mp3" }, files);
      }
    }

    [Fact]
    public void SkipsAFolderThatDoesNotExist()
    {
      using (TempFolder folder = new TempFolder())
      {
        Assert.Empty(Scan(Path.Combine(folder.Path, "missing"), true, () => false, new List<string>()));
      }
    }
  }
}
