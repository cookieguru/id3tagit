using System;
using System.IO;

namespace ID3_TagIT.Tests
{
  // A uniquely named folder under the temp folder, deleted with everything in it on Dispose.
  internal sealed class TempFolder : IDisposable
  {
    public TempFolder()
    {
      this.Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ID3-TagIT.Tests-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(this.Path);
    }

    public string Path { get; private set; }

    public string File(string name)
    {
      return System.IO.Path.Combine(this.Path, name);
    }

    public string Folder(string name)
    {
      string folder = System.IO.Path.Combine(this.Path, name);
      Directory.CreateDirectory(folder);

      return folder;
    }

    public void Dispose()
    {
      foreach (string file in Directory.GetFiles(this.Path, "*", SearchOption.AllDirectories))
        System.IO.File.SetAttributes(file, FileAttributes.Normal);

      Directory.Delete(this.Path, true);
    }
  }
}
