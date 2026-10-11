using System.Drawing;
using FlaUI.Core.Capturing;
using Xunit;

namespace ID3_TagIT.SmokeTests
{
  public class ThemeTests
  {
    // The brightness of the file list's empty area, below the last row.
    private static float ListBrightness(PortableApp app)
    {
      using (CaptureImage image = Capture.Element(app.FileList))
      {
        Bitmap bitmap = image.Bitmap;
        return bitmap.GetPixel(bitmap.Width / 2, bitmap.Height - 20).GetBrightness();
      }
    }

    [Theory]
    [InlineData((byte)1, false)]
    [InlineData((byte)2, true)]
    public void TheChosenThemeIsUsedFromTheStart(byte theme, bool dark)
    {
      using (PortableApp app = new PortableApp())
      {
        Mp3Files.WriteTagless(app.Music("tagless.mp3"));
        app.CreateSettings();
        app.EditSetting("Theme", theme);

        app.Launch(1);
        float brightness = ListBrightness(app);
        app.AssertNoMessageBox();
        app.Close();

        Assert.True(dark ? brightness < 0.3f : brightness > 0.7f, "file list brightness " + brightness);
      }
    }
  }
}
