using System.Drawing;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace ID3_TagIT.SmokeTests
{
  // The collapsible groups in the left pane of the tag editors.
  public class TagEditorPaneTests
  {
    private const int HEADER_CLICK_X = 20;
    private const int HEADER_CLICK_Y = 12;

    private static bool IsShown(Window dialog, string id)
    {
      AutomationElement element = dialog.FindFirstDescendant(cf => cf.ByAutomationId(id));

      return element != null && !element.IsOffscreen;
    }

    [Fact]
    public void ClickingAGroupHeaderCollapsesAndExpandsIt()
    {
      using (PortableApp app = new PortableApp())
      {
        Mp3Files.WriteTagless(app.Music("tagless.mp3"));
        app.Launch(1);
        app.SelectFile("tagless");
        Window v2 = app.OpenDialog("frmTAG2", VirtualKeyShort.KEY_W, VirtualKeyShort.CONTROL);
        Assert.True(IsShown(v2, "lblPrevious"), "the Commands links start shown");

        Rectangle commands = Ui.Find(v2, "pnlCommands").BoundingRectangle;
        Mouse.Click(new Point(commands.Left + HEADER_CLICK_X, commands.Top + HEADER_CLICK_Y));
        Thread.Sleep(300);
        bool shownAfterCollapse = IsShown(v2, "lblPrevious");

        Mouse.Click(new Point(commands.Left + HEADER_CLICK_X, commands.Top + HEADER_CLICK_Y));
        Thread.Sleep(300);
        bool shownAfterExpand = IsShown(v2, "lblPrevious");

        app.CloseDialog(v2, "btnCancel");
        app.Close();

        Assert.False(shownAfterCollapse, "the Commands links should be hidden once the group collapses");
        Assert.True(shownAfterExpand, "the Commands links should come back when the group expands");
      }
    }

    [Theory]
    [InlineData((byte)1, false)]
    [InlineData((byte)2, true)]
    public void TheTagEditorPaneFollowsTheTheme(byte theme, bool dark)
    {
      using (PortableApp app = new PortableApp())
      {
        Mp3Files.WriteTagless(app.Music("tagless.mp3"));
        app.CreateSettings();
        app.EditSetting("Theme", theme);
        app.Launch(1);
        app.SelectFile("tagless");
        Window v2 = app.OpenDialog("frmTAG2", VirtualKeyShort.KEY_W, VirtualKeyShort.CONTROL);

        float brightness;

        // A point in the empty lower right of the tag information group, below its last link.
        using (CaptureImage image = Capture.Element(Ui.Find(v2, "pnlTagInfo")))
          brightness = image.Bitmap.GetPixel(image.Bitmap.Width - 12, image.Bitmap.Height - 8).GetBrightness();

        app.CloseDialog(v2, "btnCancel");
        app.Close();

        Assert.True(dark ? brightness < 0.3f : brightness > 0.7f, "tag information group brightness " + brightness);
      }
    }
  }
}
