using System;
using System.Drawing;
using System.Runtime.InteropServices;
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
      public readonly TextBox Notes = new TextBox();
      public readonly Label Heading = new Label();
      public readonly ListView List = new ListView();
      public readonly TabControl Tabs = new TabControl();
      public readonly TabPage Page = new TabPage();

      public TestForm()
      {
        this.Button.BackColor = SystemColors.Control; // as the designer writes it
        this.Button.UseVisualStyleBackColor = true;
        this.Page.BackColor = SystemColors.Control;
        this.Page.UseVisualStyleBackColor = true;
        this.Tabs.TabPages.Add(this.Page);
        this.Swatch.BackColor = Color.FromArgb(255, 255, 128); // a user's highlight colour
        this.TextBox.BorderStyle = BorderStyle.Fixed3D;
        this.Heading.ForeColor = Color.FromArgb(33, 93, 198); // dark blue heading
        this.List.GridLines = true;
        this.Notes.Multiline = true;
        this.Notes.BorderStyle = BorderStyle.Fixed3D;
        this.Controls.AddRange(new Control[] { this.Button, this.Swatch, this.TextBox, this.Notes, this.Heading, this.List, this.Tabs });
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
        Assert.Equal(FlatStyle.System, form.Button.FlatStyle);
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

    [StructLayout(LayoutKind.Sequential)]
    private struct NMCUSTOMDRAW
    {
      public IntPtr hwndFrom;
      public IntPtr idFrom;
      public int code;
      public int dwDrawStage;
      public IntPtr hdc;
      public int left;
      public int top;
      public int right;
      public int bottom;
      public IntPtr dwItemSpec;
      public int uItemState;
      public IntPtr lItemlParam;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private static IntPtr CustomDrawHeader(ListView list, int stage)
    {
      IntPtr header = SendMessage(list.Handle, 0x101F, IntPtr.Zero, IntPtr.Zero);
      NMCUSTOMDRAW notice = new NMCUSTOMDRAW();
      notice.hwndFrom = header;
      notice.code = -12;
      notice.dwDrawStage = stage;
      IntPtr memory = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NMCUSTOMDRAW)));

      try
      {
        Marshal.StructureToPtr(notice, memory, false);

        return SendMessage(list.Handle, 0x4E, IntPtr.Zero, memory);
      }
      finally
      {
        Marshal.FreeHGlobal(memory);
      }
    }

    [Fact]
    public void DarkModeDrawsTheColumnHeaderTextInTheThemeTextColor()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        form.List.View = View.Details;
        form.List.Columns.Add("Name");
        IntPtr handle = form.List.Handle;

        // Windows' own header text color under the dark theme is nearly black.
        Assert.Equal((IntPtr)0x20, CustomDrawHeader(form.List, 0x1));
        Assert.Equal((IntPtr)0x2, CustomDrawHeader(form.List, 0x10001));
      });
    }

    [Fact]
    public void DarkModeReplacesLightClassicDecorations()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        Assert.Equal(BorderStyle.FixedSingle, form.Notes.BorderStyle);
        Assert.False(form.List.GridLines);
      });
    }

    [Fact]
    public void DarkModeKeepsTheBorderWindowsDrawsOnSingleLineTextBoxes()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        Assert.Equal(BorderStyle.Fixed3D, form.TextBox.BorderStyle);
      });
    }

    [Fact]
    public void DarkModePaintsTheTabPageDark()
    {
      TestApp.EnsureInitialized();
      Sta.Run(delegate ()
      {
        Application.EnableVisualStyles();

        using (Form form = new Form())
        {
          ThemedTabControl tabs = new ThemedTabControl();
          TabPage page = new TabPage();
          page.Padding = new Padding(3);
          page.UseVisualStyleBackColor = true;
          tabs.TabPages.Add(page);
          tabs.Dock = DockStyle.Fill;
          form.Controls.Add(tabs);
          form.ClientSize = new Size(300, 200);

          SetTheme(ThemeManager.THEME_LIGHT);
          ThemeManager.Register(form);

          try
          {
            SetTheme(ThemeManager.THEME_DARK);
            form.Show();
            ThemeManager.Apply(form);

            // With visual styles on, a page using the visual style color reports Transparent.
            Assert.False(page.UseVisualStyleBackColor);
            Assert.Equal(ThemeManager.DarkSurface.ToArgb(), page.BackColor.ToArgb());
          }
          finally
          {
            SetTheme(ThemeManager.THEME_LIGHT);
          }
        }
      });
    }

    [Fact]
    public void DarkModeThemesAButtonOnATransparentPanel()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        TaskPaneGroup group = new TaskPaneGroup();
        Button button = new Button();
        button.UseVisualStyleBackColor = true;
        group.Body.Controls.Add(button);
        form.Controls.Add(group);
        ThemeManager.Apply(form);

        Assert.Equal(FlatStyle.System, button.FlatStyle);
        Assert.False(button.UseVisualStyleBackColor);
        Assert.Equal(ThemeManager.DarkButton.ToArgb(), button.BackColor.ToArgb());

        SetTheme(ThemeManager.THEME_LIGHT);
        ThemeManager.Apply(form);

        Assert.Equal(FlatStyle.Standard, button.FlatStyle);
        Assert.True(button.UseVisualStyleBackColor);
      });
    }

    [Fact]
    public void DarkModeKeepsTheStyleOfCheckBoxesAndRadioButtons()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        GroupBox group = new GroupBox();
        RadioButton radio = new RadioButton();
        CheckBox check = new CheckBox();
        group.Controls.Add(radio);
        form.Controls.AddRange(new Control[] { group, check });
        ThemeManager.Apply(form);

        // Windows draws the text of a system style button in a group box black.
        Assert.Equal(FlatStyle.Standard, radio.FlatStyle);
        Assert.Equal(FlatStyle.Standard, check.FlatStyle);
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
        Assert.Equal(BorderStyle.Fixed3D, form.Notes.BorderStyle);
        Assert.True(form.List.GridLines);
      });
    }

    [Fact]
    public void LightModeRestoresVisualStyleBackColor()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        Assert.False(form.Button.UseVisualStyleBackColor);

        SetTheme(ThemeManager.THEME_LIGHT);
        ThemeManager.Apply(form);

        Assert.True(form.Button.UseVisualStyleBackColor);
      });
    }

    [Fact]
    public void LightModeRestoresTabPageVisualStyleBackColor()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        Assert.False(form.Page.UseVisualStyleBackColor);

        SetTheme(ThemeManager.THEME_LIGHT);
        ThemeManager.Apply(form);

        Assert.True(form.Page.UseVisualStyleBackColor);
      });
    }

    [Fact]
    public void SwatchKeepsItsColorInBothThemes()
    {
      WithTheme(ThemeManager.THEME_DARK, delegate (TestForm form)
      {
        AssertSwatchIsUntouched(form);

        SetTheme(ThemeManager.THEME_LIGHT);
        ThemeManager.Apply(form);

        AssertSwatchIsUntouched(form);
      });
    }

    private static void AssertSwatchIsUntouched(TestForm form)
    {
      Assert.Equal(Color.FromArgb(255, 255, 128).ToArgb(), form.Swatch.BackColor.ToArgb());
      Assert.False(form.Swatch.UseVisualStyleBackColor);
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
