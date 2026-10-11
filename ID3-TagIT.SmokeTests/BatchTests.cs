using System.IO;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace ID3_TagIT.SmokeTests
{
  // Working on several files at once.
  public class BatchTests
  {
    [Fact]
    public void MultipleEditSetsTheAlbumOnEveryFile()
    {
      using (PortableApp app = new PortableApp())
      {
        string[] files = new[] { "one.mp3", "two.mp3", "three.mp3" }.Select(n => Mp3Files.WriteTagless(app.Music(n))).ToArray();

        app.Launch(3);
        app.Shortcut(VirtualKeyShort.KEY_A, VirtualKeyShort.CONTROL);
        Window multi = app.OpenDialog("frmTAG2Multi", VirtualKeyShort.KEY_N, VirtualKeyShort.CONTROL);
        Ui.SetText(multi, "txtAlbum", "Shared Album");
        app.CloseDialog(multi);
        app.SaveAll();
        app.Close();

        foreach (string file in files)
          using (TagLib.File mp3 = TagLib.File.Create(file))
            Assert.Equal("Shared Album", mp3.Tag.Album);
      }
    }

    [Fact]
    public void TagsComeFromFilenamesAndFilenamesFromTags()
    {
      using (PortableApp app = new PortableApp())
      {
        Mp3Files.WriteTagless(app.Music("Artist A - Album B - 01 - Song One.mp3"));
        Mp3Files.WriteTagless(app.Music("Artist A - Album B - 02 - Song Two.mp3"));

        app.Launch(2);
        app.Shortcut(VirtualKeyShort.KEY_A, VirtualKeyShort.CONTROL);
        Window toTag = app.OpenDialog("frmFilenameToTAG2", VirtualKeyShort.KEY_D, VirtualKeyShort.CONTROL);
        Ui.SetText(toTag, "cmbFormat", "<A> - <B> - <K> - <T>");
        app.CloseDialog(toTag);
        app.SaveAll();

        using (TagLib.File mp3 = TagLib.File.Create(app.Music("Artist A - Album B - 02 - Song Two.mp3")))
        {
          Assert.Equal("Artist A", mp3.Tag.Performers.Single());
          Assert.Equal("Album B", mp3.Tag.Album);
          Assert.Equal(2u, mp3.Tag.Track);
          Assert.Equal("Song Two", mp3.Tag.Title);
        }

        app.Shortcut(VirtualKeyShort.KEY_A, VirtualKeyShort.CONTROL);
        Window toName = app.OpenDialog("frmTAG2ToFilename", VirtualKeyShort.KEY_R, VirtualKeyShort.CONTROL);
        Ui.SetText(toName, "cmbFormat", "<K>. <T>");
        app.CloseDialog(toName);
        app.SaveAll();
        app.Close();

        Assert.Equal(new[] { "01. Song One.mp3", "02. Song Two.mp3" }, Directory.GetFiles(app.MusicFolder, "*.mp3").Select(Path.GetFileName).OrderBy(n => n));
      }
    }
  }
}
