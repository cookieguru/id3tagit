using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Xunit;

namespace ID3_TagIT.Tests
{
  public class NativeThemeTests
  {
    private sealed class Recorder : IDisposable
    {
      public readonly List<IntPtr> Handles = new List<IntPtr>();
      public readonly List<string> Names = new List<string>();
      public readonly List<int> Ordinals = new List<int>();
      private readonly Func<IntPtr, string, string, int> objThemeCall;
      private readonly Action<int, int> objOrdinalCall;
      private readonly Func<int> objBuild;

      public Recorder()
      {
        this.objThemeCall = NativeTheme.SetWindowThemeCall;
        this.objOrdinalCall = NativeTheme.UxThemeOrdinalCall;
        this.objBuild = NativeTheme.WindowsBuild;
        NativeTheme.SetWindowThemeCall = delegate (IntPtr hwnd, string name, string ids)
        {
          this.Handles.Add(hwnd);
          this.Names.Add(name);
          return 0;
        };
        NativeTheme.UxThemeOrdinalCall = delegate (int ordinal, int argument) { this.Ordinals.Add(ordinal); };
      }

      public void Dispose()
      {
        NativeTheme.SetWindowThemeCall = this.objThemeCall;
        NativeTheme.UxThemeOrdinalCall = this.objOrdinalCall;
        NativeTheme.WindowsBuild = this.objBuild;
      }
    }

    [Fact]
    public void DarkIsSupportedFromTheFirstDarkBuild()
    {
      Assert.False(NativeTheme.IsDarkSupported(NativeTheme.FirstDarkBuild - 1));
      Assert.True(NativeTheme.IsDarkSupported(NativeTheme.FirstDarkBuild));
    }

    [Fact]
    public void DarkSettingResolvesToLightBelowTheFloor()
    {
      TestApp.EnsureInitialized();
      Sta.Run(delegate ()
      {
        using (new Recorder())
        {
          TestApp.Settings.Theme = ThemeManager.THEME_DARK;
          NativeTheme.WindowsBuild = delegate () { return NativeTheme.FirstDarkBuild - 1; };

          try
          {
            ThemeManager.Refresh();

            Assert.False(ThemeManager.IsDark);
            Assert.Equal(ThemeManager.THEME_DARK, TestApp.Settings.Theme);
          }
          finally
          {
            TestApp.Settings.Theme = ThemeManager.THEME_LIGHT;
            NativeTheme.WindowsBuild = delegate () { return NativeTheme.FirstDarkBuild; };
            ThemeManager.Refresh();
          }
        }
      });
    }

    [Fact]
    public void SetAppDarkDoesNothingBelowTheFloor()
    {
      using (Recorder recorder = new Recorder())
      {
        NativeTheme.WindowsBuild = delegate () { return NativeTheme.FirstDarkBuild - 1; };
        NativeTheme.SetAppDark(true);

        Assert.Empty(recorder.Ordinals);
      }
    }

    [Fact]
    public void SetAppDarkCallsTheOrdinalsAtTheFloor()
    {
      using (Recorder recorder = new Recorder())
      {
        NativeTheme.WindowsBuild = delegate () { return NativeTheme.FirstDarkBuild; };
        NativeTheme.SetAppDark(true);

        Assert.Equal(new int[] { 135, 136 }, recorder.Ordinals.ToArray());
      }
    }

    [Fact]
    public void ThemeNameForFollowsTheTheMatrix()
    {
      Sta.Run(delegate ()
      {
        Assert.Equal("DarkMode_Explorer", NativeTheme.ThemeNameFor(new ListView(), true));
        Assert.Equal("DarkMode_Explorer", NativeTheme.ThemeNameFor(new TreeView(), true));
        Assert.Equal("DarkMode_Explorer", NativeTheme.ThemeNameFor(new RichTextBox(), true));
        Assert.Equal("DarkMode_Explorer", NativeTheme.ThemeNameFor(new TextBox { Multiline = true }, true));
        Assert.Equal("DarkMode_CFD", NativeTheme.ThemeNameFor(new TextBox(), true));
        Assert.Equal("DarkMode_CFD", NativeTheme.ThemeNameFor(new MaskedTextBox(), true));
        Assert.Equal("DarkMode_CFD", NativeTheme.ThemeNameFor(new ComboBox(), true));
        Assert.Equal("DarkMode_Explorer", NativeTheme.ThemeNameFor(new Panel { AutoScroll = true }, true));
        Assert.Null(NativeTheme.ThemeNameFor(new Panel(), true));
        Assert.Null(NativeTheme.ThemeNameFor(new Label(), true));
        Assert.Null(NativeTheme.ThemeNameFor(new Button(), true));
        Assert.Equal("DarkMode_Explorer", NativeTheme.ThemeNameFor(new Button { FlatStyle = FlatStyle.System }, true));
        Assert.Equal("DarkMode_Explorer", NativeTheme.ThemeNameFor(new CheckBox { FlatStyle = FlatStyle.System }, true));
        Assert.Equal("DarkMode_Explorer", NativeTheme.ThemeNameFor(new RadioButton { FlatStyle = FlatStyle.System }, true));
        Assert.Null(NativeTheme.ThemeNameFor(new ListView(), false));
      });
    }

    [Fact]
    public void ThemeSurvivesHandleRecreation()
    {
      Sta.Run(delegate ()
      {
        using (Recorder recorder = new Recorder())
        using (ListView list = new ListView())
        {
          IntPtr first = list.Handle;
          NativeTheme.Apply(list, true);
          recorder.Handles.Clear();
          recorder.Names.Clear();

          typeof(Control).GetMethod("RecreateHandle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(list, null);
          IntPtr second = list.Handle;

          Assert.Contains(second, recorder.Handles);
          Assert.Equal("DarkMode_Explorer", recorder.Names[recorder.Handles.IndexOf(second)]);
        }
      });
    }

    [Fact]
    public void SwitchingToLightClearsTheNativeTheme()
    {
      Sta.Run(delegate ()
      {
        using (Recorder recorder = new Recorder())
        using (ListView list = new ListView())
        {
          IntPtr handle = list.Handle;
          NativeTheme.Apply(list, true);
          recorder.Handles.Clear();
          recorder.Names.Clear();

          NativeTheme.Apply(list, false);

          Assert.Equal(handle, recorder.Handles[0]);
          Assert.Null(recorder.Names[0]);
        }
      });
    }

    [Fact]
    public void ListViewColumnHeaderGetsTheItemsViewTheme()
    {
      Sta.Run(delegate ()
      {
        using (Recorder recorder = new Recorder())
        using (ListView list = new ListView())
        {
          list.View = View.Details;
          list.Columns.Add("Name");
          IntPtr handle = list.Handle;
          NativeTheme.Apply(list, true);

          Assert.Equal("DarkMode_Explorer", recorder.Names[recorder.Handles.IndexOf(handle)]);
          Assert.Contains("DarkMode_ItemsView", recorder.Names);

          recorder.Names.Clear();
          NativeTheme.Apply(list, false);

          Assert.DoesNotContain("DarkMode_ItemsView", recorder.Names);
          Assert.Equal(2, recorder.Names.Count);
        }
      });
    }

    [Fact]
    public void ComboDropDownListIsThemedToo()
    {
      Sta.Run(delegate ()
      {
        using (Recorder recorder = new Recorder())
        using (ComboBox combo = new ComboBox())
        {
          IntPtr handle = combo.Handle;
          NativeTheme.Apply(combo, true);

          Assert.True(recorder.Handles.Count >= 2, "the combo and its drop-down list");
          Assert.Equal("DarkMode_CFD", recorder.Names[recorder.Handles.IndexOf(handle)]);
          Assert.Contains("DarkMode_Explorer", recorder.Names);
        }
      });
    }
  }
}
