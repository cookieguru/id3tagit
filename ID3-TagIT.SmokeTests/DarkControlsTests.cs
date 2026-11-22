using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace ID3_TagIT.SmokeTests
{
  // Looks at the pixels of the real controls in dark mode, because the native dark themes can't
  // be checked any other way.  Every failure message carries the Windows build and whether
  // visual styles are active, so a result from CI shows whether the runner can render them.
  public class DarkControlsTests
  {
    private const float DARK = 0.30f;

    [DllImport("uxtheme.dll")]
    private static extern bool IsThemeActive();

    private static string Environment_()
    {
      return "Windows " + Environment.OSVersion.Version + ", visual styles active: " + IsThemeActive();
    }

    private static float Brightness(AutomationElement element, int x, int y)
    {
      using (CaptureImage image = Capture.Element(element))
      {
        Bitmap bitmap = image.Bitmap;
        return bitmap.GetPixel(Math.Min(x, bitmap.Width - 1), Math.Min(y, bitmap.Height - 1)).GetBrightness();
      }
    }

    // Pixels measured from the middle of an element's left or bottom, where the frame is.
    private static float BrightestOfColumns(AutomationElement element, int firstX, int lastX)
    {
      using (CaptureImage image = Capture.Element(element))
      {
        Bitmap bitmap = image.Bitmap;
        float brightest = 0;

        for (int y = bitmap.Height / 3; y < bitmap.Height * 2 / 3; y++)
          for (int x = firstX; x <= lastX; x++)
            brightest = Math.Max(brightest, bitmap.GetPixel(x, y).GetBrightness());

        return brightest;
      }
    }

    private static PortableApp DarkApp()
    {
      PortableApp app = new PortableApp();
      Mp3Files.WriteTagless(app.Music("tagless.mp3"));
      app.CreateSettings();
      app.EditSetting("Theme", (byte)2);
      return app;
    }

    [Fact]
    public void TheMainWindowControlsAreDark()
    {
      using (PortableApp app = DarkApp())
      {
        app.Launch(1);

        AutomationElement tabs = Ui.Find(app.Main, "tabControl1");
        AutomationElement title = Ui.Find(app.Main, "txtQETitle");
        AutomationElement artist = Ui.Find(app.Main, "cboQEArtist");
        AutomationElement ok = Ui.Find(app.Main, "btnQuickEditOK");
        AutomationElement information = Ui.Find(app.Main, "txtInformation");

        // The page of a tab control is drawn by Windows in white unless it is told otherwise.
        Dictionary<string, float> measured = new Dictionary<string, float>();
        measured["left tabs frame"] = BrightestOfColumns(tabs, 0, 12);
        measured["title"] = Brightness(title, title.BoundingRectangle.Width / 2, title.BoundingRectangle.Height / 2);
        measured["artist"] = Brightness(artist, artist.BoundingRectangle.Width / 4, artist.BoundingRectangle.Height / 2);
        measured["ok button"] = Brightness(ok, 6, ok.BoundingRectangle.Height / 2);
        measured["information"] = Brightness(information, information.BoundingRectangle.Width - 30, information.BoundingRectangle.Height - 20);
        app.AssertNoMessageBox();
        app.Close();

        List<string> tooBright = new List<string>();

        foreach (KeyValuePair<string, float> pair in measured)
        {
          // The frame itself is a mid gray; everything else is nearly black.
          float limit = pair.Key == "left tabs frame" ? 0.45f : DARK;

          if (pair.Value > limit)
            tooBright.Add(pair.Key + " " + pair.Value.ToString("0.00"));
        }

        Assert.True(tooBright.Count == 0, "Too bright: " + string.Join(", ", tooBright) + " (" + Environment_() + ")");
      }
    }

    [Fact]
    public void ThePreferencesPageIsDark()
    {
      using (PortableApp app = DarkApp())
      {
        app.Launch(1);
        FlaUI.Core.AutomationElements.Window preferences = app.OpenDialog("frmPreferences", VirtualKeyShort.F12);
        float page = Brightness(preferences, preferences.BoundingRectangle.Width / 2, preferences.BoundingRectangle.Height - 65);
        app.CloseDialog(preferences, "btnCancel");
        app.Close();

        Assert.True(page < DARK, "Preferences page brightness " + page.ToString("0.00") + " (" + Environment_() + ")");
      }
    }
  }
}
