using System;
using System.Drawing;
using Xunit;

namespace ID3_TagIT.Tests
{
  public class PaneIconsTests
  {
    private const int MINIMUM_VISIBLE_PIXELS = 20;

    [Fact]
    public void EveryPaneIconLoadsAt16By16()
    {
      foreach (string name in PaneIcons.Names)
        using (Image icon = PaneIcons.Get(name))
          Assert.True(icon.Width == 16 && icon.Height == 16, name + " is " + icon.Width + "x" + icon.Height);
    }

    [Fact]
    public void EveryPaneIconHasVisiblePixels()
    {
      foreach (string name in PaneIcons.Names)
        using (Bitmap icon = new Bitmap(PaneIcons.Get(name)))
        {
          int visible = 0;

          for (int y = 0; y < icon.Height; y++)
            for (int x = 0; x < icon.Width; x++)
              if (icon.GetPixel(x, y).A > 0)
                visible++;

          Assert.True(visible >= MINIMUM_VISIBLE_PIXELS, name + " has " + visible + " visible pixels");
        }
    }

    [Fact]
    public void UnknownIconNameThrows()
    {
      Assert.Throws<ArgumentException>(delegate () { PaneIcons.Get("nope"); });
    }
  }
}
