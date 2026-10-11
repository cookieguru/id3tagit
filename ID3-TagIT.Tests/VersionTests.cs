using Xunit;

namespace ID3_TagIT.Tests
{
  public class VersionTests
  {
    [Fact]
    public void VersionIs350Beta()
    {
      System.Version version = typeof(MP3).Assembly.GetName().Version;

      Assert.Equal("3.5.0", version.ToString(3));
      Assert.True(Id3TagIT_Main.IS_BETA);
      Assert.False(Id3TagIT_Main.IS_ALPHA);
    }
  }
}
