using System.IO;
using Xunit;

namespace ID3_TagIT.Tests
{
  // Files that must sit next to the exe in every edition.
  public class PackagingTests
  {
    private static string ExeFolder
    {
      get { return Path.GetDirectoryName(typeof(MP3).Assembly.Location); }
    }

    [Theory]
    [InlineData("CSV-Short.itl")]
    [InlineData("HTML-Short.itl")]
    [InlineData("RTF-Short.itl")]
    [InlineData("SFV (CRC32 Checksum file).itl")]
    [InlineData("Readme.txt")]
    public void TemplatesAreNextToTheExe(string file)
    {
      Assert.True(File.Exists(Path.Combine(ExeFolder, "Templetes", file)), file + " is missing");
    }
  }
}
