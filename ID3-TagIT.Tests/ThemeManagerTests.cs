using System.Drawing;
using System.Windows.Forms;
using Xunit;

namespace ID3_TagIT.Tests
{
  public class ThemeManagerTests
  {
    private sealed class TestForm : Form
    {
      public readonly Button Button = new Button();
      public readonly Button Swatch = new Button();
      public readonly TextBox TextBox = new TextBox();
      public readonly Label Heading = new Label();
      public readonly ListView List = new ListView();

      public TestForm()
      {
        this.Button.BackColor = SystemColors.Control; // as the designer writes it
        this.Swatch.BackColor = Color.FromArgb(255, 255, 128); // a user's highlight colour
        this.TextBox.BorderStyle = BorderStyle.Fixed3D;
        this.Heading.ForeColor = Color.FromArgb(33, 93, 198); // dark blue heading
        this.List.GridLines = true;
        this.Controls.AddRange(new Control[] { this.Button, this.Swatch, this.TextBox, this.Heading, this.List });
      }
    }

    private static void SetTheme(byte theme)
    {
      TestApp.Settings.Theme = theme;
      ThemeManager.Refresh();
    }

    private static void WithTheme(byte theme, System.Action<TestForm> test)
    {
      TestApp.EnsureInitialized();
      Sta.Run(delegate ()
      {
        using (TestForm form = new TestForm())
        {
          SetTheme(ThemeManager.THEME_LIGHT);
          ThemeManager.Register(form);
          ThemeManager.Apply(form);

          try
          {
            SetTheme(theme);
            ThemeManager.Apply(form);
            test(form);
          }
          finally
          {
            SetTheme(ThemeManager.THEME_LIGHT);
          }
        }
      });
    }

    [Fact]
    public void DarkModeRemapsSystemColors()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        Assert.True(ThemeManager.IsDark);
        Assert.Equal(ThemeManager.DarkSurface.ToArgb(), form.BackColor.ToArgb());
        Assert.Equal(ThemeManager.DarkText.ToArgb(), form.ForeColor.ToArgb());
        Assert.Equal(ThemeManager.DarkWindow.ToArgb(), form.TextBox.BackColor.ToArgb());
        Assert.Equal(ThemeManager.DarkButton.ToArgb(), form.Button.BackColor.ToArgb());
        Assert.Equal(FlatStyle.Flat, form.Button.FlatStyle);
      });
    }

    [Fact]
    public void DarkModeLeavesCustomBackColorsAlone()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        Assert.Equal(Color.FromArgb(255, 255, 128).ToArgb(), form.Swatch.BackColor.ToArgb());
        Assert.Equal(FlatStyle.Standard, form.Swatch.FlatStyle);
      });
    }

    [Fact]
    public void DarkModeLightensDarkCustomTextColors()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        Color heading = form.Heading.ForeColor;
        Assert.True(heading.GetBrightness() > Color.FromArgb(33, 93, 198).GetBrightness());
        Assert.True(heading.B > heading.R); // still blue
      });
    }

    [Fact]
    public void DarkModeReplacesLightClassicDecorations()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        Assert.Equal(BorderStyle.FixedSingle, form.TextBox.BorderStyle);
        Assert.False(form.List.GridLines);
      });
    }

    [Fact]
    public void LightModeRestoresTheOriginals()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        SetTheme(ThemeManager.THEME_LIGHT);
        ThemeManager.Apply(form);

        Assert.Equal(SystemColors.Control, form.BackColor);
        Assert.Equal(SystemColors.Window, form.TextBox.BackColor);
        Assert.Equal(Color.FromArgb(33, 93, 198).ToArgb(), form.Heading.ForeColor.ToArgb());
        Assert.Equal(FlatStyle.Standard, form.Button.FlatStyle);
        Assert.Equal(BorderStyle.Fixed3D, form.TextBox.BorderStyle);
        Assert.True(form.List.GridLines);
      });
    }

    [Fact]
    public void ColorsTheAppSetsAfterThemingAreKept()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        form.TextBox.BackColor = Color.Red;
        ThemeManager.Apply(form);
        Assert.Equal(Color.Red, form.TextBox.BackColor);

        SetTheme(ThemeManager.THEME_LIGHT);
        ThemeManager.Apply(form);
        Assert.Equal(Color.Red, form.TextBox.BackColor);
      });
    }

    [Fact]
    public void HighlightColorsFollowTheTheme()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        Settings settings = TestApp.Settings;
        Assert.Equal(settings.ColorChangedBackDark, settings.ChangedBackColor.ToArgb());

        SetTheme(ThemeManager.THEME_LIGHT);
        Assert.Equal(settings.ColorChangedBack, settings.ChangedBackColor.ToArgb());
      });
    }
  }
}
