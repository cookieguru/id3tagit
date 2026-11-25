using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ID3_TagIT
{
  // The Windows visual style names and uxtheme calls behind dark mode.  Only this class knows
  // them, so a later move to another dark mode approach stays small.
  internal static class NativeTheme
  {
    // Windows 10 1903, the first build with the dark scrollbar and combo box themes.
    internal const int FirstDarkBuild = 18362;

    private const string EXPLORER = "DarkMode_Explorer";
    private const string COMMON_FILE_DIALOG = "DarkMode_CFD";

    private const int CB_GETCOMBOBOXINFO = 0x164;
    private const int ORDINAL_SET_PREFERRED_APP_MODE = 135;
    private const int ORDINAL_FLUSH_MENU_THEMES = 136;
    private const int APP_MODE_FORCE_DARK = 2;
    private const int APP_MODE_FORCE_LIGHT = 3;

    // Tests replace these to see the calls without touching the real window.
    internal static Func<IntPtr, string, string, int> SetWindowThemeCall = SetWindowTheme;
    internal static Action<int, int> UxThemeOrdinalCall = CallUxThemeOrdinal;
    internal static Func<int> WindowsBuild = ReadWindowsBuild;

    private sealed class State
    {
      public bool Dark;
    }

    private static readonly ConditionalWeakTable<Control, State> States = new ConditionalWeakTable<Control, State>();

    internal static bool DarkSupported
    {
      get
      {
        return IsDarkSupported(WindowsBuild());
      }
    }

    internal static bool IsDarkSupported(int windowsBuild)
    {
      return windowsBuild >= FirstDarkBuild;
    }

    // Null for light mode and for controls Windows has no dark theme for.
    internal static string ThemeNameFor(Control control, bool dark)
    {
      if (!dark)
        return null;

      if (control is MaskedTextBox || control is ComboBox)
        return COMMON_FILE_DIALOG;

      if (control is RichTextBox || control is ListView || control is TreeView || control is ListBox || control is TrackBar || control is ScrollBar)
        return EXPLORER;

      TextBox textBox = control as TextBox;

      if (textBox != null)
        return textBox.Multiline ? EXPLORER : COMMON_FILE_DIALOG;

      ButtonBase button = control as ButtonBase;

      // The style is set to System before this is asked; a button with its own colors keeps its own look.
      if (button != null)
        return button.FlatStyle == FlatStyle.System ? EXPLORER : null;

      Panel panel = control as Panel;

      if (panel != null && panel.AutoScroll)
        return EXPLORER;

      // The spin buttons of a NumericUpDown are an internal control.
      if (control.Parent is UpDownBase && control.GetType().Name == "UpDownButtons")
        return EXPLORER;

      return null;
    }

    // The common file dialog theme draws a dark border and a focus border, so a classic 3D border
    // can stay; Explorer themed controls still get a bright 3D border.
    internal static bool DrawsDarkBorder(Control control)
    {
      return ThemeNameFor(control, true) == COMMON_FILE_DIALOG;
    }

    // Themes the control now and again whenever its window is recreated.
    internal static void Apply(Control control, bool dark)
    {
      State state;

      if (!States.TryGetValue(control, out state))
      {
        if (!dark || ThemeNameFor(control, true) == null)
          return;

        state = new State();
        States.Add(control, state);
        control.HandleCreated += new EventHandler(Control_HandleCreated);
      }

      state.Dark = dark;

      if (control.IsHandleCreated)
        ApplyToWindow(control, dark);
    }

    // Dark mode for the pop-ups Windows draws itself, such as the text box right-click menu.
    internal static void SetAppDark(bool dark)
    {
      if (!DarkSupported)
        return;

      UxThemeOrdinalCall(ORDINAL_SET_PREFERRED_APP_MODE, dark ? APP_MODE_FORCE_DARK : APP_MODE_FORCE_LIGHT);
      UxThemeOrdinalCall(ORDINAL_FLUSH_MENU_THEMES, 0);
    }

    private static void Control_HandleCreated(object sender, EventArgs e)
    {
      Control control = (Control)sender;
      State state;

      if (States.TryGetValue(control, out state))
        ApplyToWindow(control, state.Dark);
    }

    private static void ApplyToWindow(Control control, bool dark)
    {
      string name = ThemeNameFor(control, dark);

      SetWindowThemeCall(control.Handle, name, null);

      ComboBox combo = control as ComboBox;

      if (combo != null)
      {
        IntPtr list = ReadComboList(combo);

        if (list != IntPtr.Zero)
          SetWindowThemeCall(list, dark ? EXPLORER : null, null);
      }
    }

    private static IntPtr ReadComboList(ComboBox combo)
    {
      COMBOBOXINFO info = new COMBOBOXINFO();
      info.cbSize = Marshal.SizeOf(typeof(COMBOBOXINFO));

      if (SendMessage(combo.Handle, CB_GETCOMBOBOXINFO, IntPtr.Zero, ref info) == IntPtr.Zero)
        return IntPtr.Zero;

      return info.hwndList;
    }

    private static int ReadWindowsBuild()
    {
      try
      {
        OSVERSIONINFO info = new OSVERSIONINFO();
        info.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFO));

        if (RtlGetVersion(ref info) != 0)
          return 0;

        return info.dwBuildNumber;
      }
      catch (Exception)
      {
        return 0;
      }
    }

    private delegate int SetAppModeDelegate(int mode);
    private delegate void FlushMenuThemesDelegate();

    private static void CallUxThemeOrdinal(int ordinal, int argument)
    {
      try
      {
        IntPtr module = LoadLibrary("uxtheme.dll");
        IntPtr address = (module == IntPtr.Zero) ? IntPtr.Zero : GetProcAddress(module, (IntPtr)ordinal);

        if (address == IntPtr.Zero)
          return;

        if (ordinal == ORDINAL_SET_PREFERRED_APP_MODE)
          ((SetAppModeDelegate)Marshal.GetDelegateForFunctionPointer(address, typeof(SetAppModeDelegate)))(argument);
        else
          ((FlushMenuThemesDelegate)Marshal.GetDelegateForFunctionPointer(address, typeof(FlushMenuThemesDelegate)))();
      }
      catch (Exception)
      {
        // Dark pop-ups are cosmetic; the app works without them.
      }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct COMBOBOXINFO
    {
      public int cbSize;
      public int rcItemLeft;
      public int rcItemTop;
      public int rcItemRight;
      public int rcItemBottom;
      public int rcButtonLeft;
      public int rcButtonTop;
      public int rcButtonRight;
      public int rcButtonBottom;
      public int stateButton;
      public IntPtr hwndCombo;
      public IntPtr hwndItem;
      public IntPtr hwndList;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OSVERSIONINFO
    {
      public int dwOSVersionInfoSize;
      public int dwMajorVersion;
      public int dwMinorVersion;
      public int dwBuildNumber;
      public int dwPlatformId;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
      public string szCSDVersion;
    }

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref COMBOBOXINFO lParam);

    [DllImport("ntdll.dll")]
    private static extern int RtlGetVersion(ref OSVERSIONINFO info);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string fileName);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetProcAddress(IntPtr module, IntPtr ordinal);
  }
}
