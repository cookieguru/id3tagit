using System;
using System.Drawing;
using System.Runtime.InteropServices;
using FlaUI.Core.Capturing;
using Xunit;

namespace ID3_TagIT.SmokeTests
{
  public class ThemeTests
  {
    private const int CCM_GETVERSION = 0x2008;
    private const int COMMON_CONTROLS_WITH_VISUAL_STYLES = 6;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    // The brightness of the file list's empty area, below the last row.
    private static float ListBrightness(PortableApp app)
    {
      using (CaptureImage image = Capture.Element(app.FileList))
      {
        Bitmap bitmap = image.Bitmap;
        return bitmap.GetPixel(bitmap.Width / 2, bitmap.Height - 20).GetBrightness();
      }
    }

    [Fact]
    public void TheAppUsesModernCommonControls()
    {
      using (PortableApp app = new PortableApp())
      {
        Mp3Files.WriteTagless(app.Music("tagless.mp3"));
        app.Launch(1);
        IntPtr list = app.FileList.Properties.NativeWindowHandle.Value;
        int version = (int)SendMessage(list, CCM_GETVERSION, IntPtr.Zero, IntPtr.Zero);
        app.Close();

        Assert.Equal(COMMON_CONTROLS_WITH_VISUAL_STYLES, version);
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
