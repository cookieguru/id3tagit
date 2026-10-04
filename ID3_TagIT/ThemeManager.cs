using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ID3_TagIT
{
  // Applies a light or dark color scheme to the application's forms.
  //
  // The Theme setting picks the mode. THEME_SYSTEM follows the Windows "app mode"
  // (Settings > Personalization > Colors) and switches live when it changes.
  //
  // Dark mode remaps *system* colors (SystemColors.*, plus the named colors White and Black)
  // on every control and remembers the original values, so switching back to light restores
  // what the designer set. Colors built with Color.FromArgb are treated as deliberate and left
  // alone (user highlight colors, the color swatches in Preferences), except that dark custom
  // text colors are lightened so they stay readable on a dark background.
  //
  // Forms opt in by calling ThemeManager.Register(this) in their constructor.
  internal static class ThemeManager
  {
    public const byte THEME_SYSTEM = 0;
    public const byte THEME_LIGHT = 1;
    public const byte THEME_DARK = 2;

    public static readonly Color DarkSurface = Color.FromArgb(32, 32, 32);
    public static readonly Color DarkWindow = Color.FromArgb(25, 25, 25);
    public static readonly Color DarkRaised = Color.FromArgb(45, 45, 45);
    public static readonly Color DarkButton = Color.FromArgb(51, 51, 51);
    public static readonly Color DarkHover = Color.FromArgb(65, 65, 65);
    public static readonly Color DarkBorder = Color.FromArgb(85, 85, 85);
    public static readonly Color DarkText = Color.FromArgb(241, 241, 241);
    public static readonly Color DarkDisabledText = Color.FromArgb(150, 150, 150);
    public static readonly Color DarkAccentText = Color.FromArgb(110, 170, 255);
    public static readonly Color DarkSelection = Color.FromArgb(0, 95, 184);

    private enum ColorRole
    {
      Back,
      ButtonBack,
      Fore,
      Line
    }

    private sealed class FormState
    {
      public bool Applied;      // themed at least once (light or dark)
      public bool EverDark;     // themed dark at least once, so originals may need restoring
      public bool LastDark;     // mode of the most recent Apply
    }

    private static bool vbooIsDark;
    private static bool vbooListening;
    private static ToolStripRenderer objLightRenderer;
    private static ToolStripRenderer objDarkRenderer;

    // Original values of every property changed for dark mode, per object. "key" holds the
    // original value and "key#" the value we set, so later changes made by the application
    // itself can be detected and respected.
    private static readonly ConditionalWeakTable<object, Dictionary<string, object>> Originals = new ConditionalWeakTable<object, Dictionary<string, object>>();
    private static readonly ConditionalWeakTable<object, object> Hooked = new ConditionalWeakTable<object, object>();
    private static readonly ConditionalWeakTable<Form, FormState> Forms = new ConditionalWeakTable<Form, FormState>();
    private static readonly ConditionalWeakTable<Control, ThemePainter> Painters = new ConditionalWeakTable<Control, ThemePainter>();
    private static readonly List<WeakReference> RegisteredForms = new List<WeakReference>();

    public static bool IsDark
    {
      get
      {
        return vbooIsDark;
      }
    }

    // Call once at startup, after the settings are read.
    public static void Initialize()
    {
      vbooIsDark = ResolveDark();
      UpdateGlobalRenderer();

      if (!vbooListening)
      {
        vbooListening = true;
        SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(SystemEvents_UserPreferenceChanged);
        Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
      }
    }

    // Re-evaluates the mode (after the Theme setting or the Windows setting changed) and
    // re-themes every open form if it differs.
    public static void Refresh()
    {
      bool dark = ResolveDark();

      if (dark == vbooIsDark)
        return;

      vbooIsDark = dark;
      UpdateGlobalRenderer();

      foreach (Form form in GetRegisteredForms())
        if (form.IsHandleCreated && !form.IsDisposed)
          Apply(form);
    }

    public static void Register(Form form)
    {
      if (IsHooked(form))
        return;

      RegisteredForms.Add(new WeakReference(form));
      form.HandleCreated += new EventHandler(Form_HandleCreated);
      // Subscribed after the form's own Load handler (which constructors attach before
      // InitializeComponent), so controls the form adds or recolors in Load are themed too.
      form.Load += new EventHandler(Form_Load);
      form.VisibleChanged += new EventHandler(Form_VisibleChanged);
    }

    public static void Apply(Form form)
    {
      FormState state = Forms.GetOrCreateValue(form);

      state.Applied = true;
      state.LastDark = vbooIsDark;

      // Nothing was ever changed on a form that has only been shown light.
      if (!vbooIsDark && !state.EverDark)
        return;

      if (vbooIsDark)
        state.EverDark = true;

      form.SuspendLayout();

      try
      {
        ApplyControl(form);
      }
      finally
      {
        form.ResumeLayout(false);
      }

      if (form.IsHandleCreated)
      {
        SetTitleBar(form);
        RedrawWindow(form.Handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN | RDW_FRAME);
      }
    }

    public static bool SystemUsesDarkApps()
    {
      try
      {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
        {
          if (key == null)
            return false;

          object value = key.GetValue("AppsUseLightTheme");

          return (value is int) && ((int)value == 0);
        }
      }
      catch (Exception)
      {
        return false;
      }
    }

    private static bool ResolveDark()
    {
      // High contrast themes define their own colors; never override them.
      if (SystemInformation.HighContrast)
        return false;

      byte mode = THEME_SYSTEM;

      try
      {
        mode = Declarations.objSettings.Theme;
      }
      catch (Exception)
      {
      }

      if (mode == THEME_DARK)
        return true;

      if (mode == THEME_LIGHT)
        return false;

      return SystemUsesDarkApps();
    }

    private static List<Form> GetRegisteredForms()
    {
      List<Form> forms = new List<Form>();

      for (int i = RegisteredForms.Count - 1; i >= 0; i--)
      {
        Form form = RegisteredForms[i].Target as Form;

        if (form == null || form.IsDisposed)
          RegisteredForms.RemoveAt(i);
        else
          forms.Add(form);
      }

      return forms;
    }

    private static void UpdateGlobalRenderer()
    {
      if (objLightRenderer == null)
        objLightRenderer = ToolStripManager.Renderer;

      if (vbooIsDark)
      {
        if (objDarkRenderer == null)
          objDarkRenderer = new DarkToolStripRenderer();

        ToolStripManager.Renderer = objDarkRenderer;
      }
      else
        ToolStripManager.Renderer = objLightRenderer;
    }

    // Theme as soon as the window exists, before Load and before anything paints; forms such as
    // frmMain show themselves and do slow work inside their own Load handler, which would
    // otherwise appear light until Load returns.
    private static void Form_HandleCreated(object sender, EventArgs e)
    {
      Apply((Form)sender);
    }

    private static void Form_Load(object sender, EventArgs e)
    {
      Apply((Form)sender);
    }

    private static void Form_VisibleChanged(object sender, EventArgs e)
    {
      Form form = (Form)sender;

      if (!form.Visible)
        return;

      // Catches forms that were created, hidden and shown again after the mode changed.
      FormState state = Forms.GetOrCreateValue(form);

      if (state.Applied && state.LastDark != vbooIsDark)
        Apply(form);
    }

    private static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
      if (e.Category != UserPreferenceCategory.General && e.Category != UserPreferenceCategory.Color)
        return;

      // SystemEvents may raise this on its own thread; marshal to the UI thread.
      List<Form> forms = GetRegisteredForms();

      foreach (Form form in forms)
      {
        if (form.IsHandleCreated && !form.IsDisposed)
        {
          if (form.InvokeRequired)
            form.BeginInvoke(new MethodInvoker(Refresh));
          else
            Refresh();

          return;
        }
      }
    }

    private static void Application_ApplicationExit(object sender, EventArgs e)
    {
      SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(SystemEvents_UserPreferenceChanged);
    }

    private static bool IsHooked(object target)
    {
      object marker;

      if (Hooked.TryGetValue(target, out marker))
        return true;

      Hooked.Add(target, new object());

      return false;
    }

    private static void ApplyControl(Control control)
    {
      if (!IsHooked(control))
        control.ControlAdded += new ControlEventHandler(Control_ControlAdded);

      ApplyControlColors(control);

      if (control.ContextMenuStrip != null)
        ApplyToolStrip(control.ContextMenuStrip);

      ToolStrip strip = control as ToolStrip;

      if (strip != null)
      {
        ApplyToolStrip(strip);
        return;
      }

      foreach (Control child in control.Controls)
        ApplyControl(child);
    }

    private static void Control_ControlAdded(object sender, ControlEventArgs e)
    {
      Form form = e.Control.FindForm();
      FormState state;

      // Controls added before the form is themed are picked up by the form's Apply, and
      // light mode needs nothing.
      if (!vbooIsDark || form == null || !Forms.TryGetValue(form, out state) || !state.Applied)
        return;

      ApplyControl(e.Control);
    }

    private static void ApplyControlColors(Control control)
    {
      ColorRole backRole = ColorRole.Back;

      if (control is Button)
        backRole = ColorRole.ButtonBack;

      ThemeColor(control, "BackColor", () => control.BackColor, v => control.BackColor = v, backRole);
      ThemeColor(control, "ForeColor", () => control.ForeColor, v => control.ForeColor = v, ColorRole.Fore);

      LinkLabel link = control as LinkLabel;

      if (link != null)
      {
        ThemeColor(link, "LinkColor", () => link.LinkColor, v => link.LinkColor = v, ColorRole.Fore);
        ThemeColor(link, "ActiveLinkColor", () => link.ActiveLinkColor, v => link.ActiveLinkColor = v, ColorRole.Fore);
        ThemeColor(link, "VisitedLinkColor", () => link.VisitedLinkColor, v => link.VisitedLinkColor = v, ColorRole.Fore);
        ThemeColor(link, "DisabledLinkColor", () => link.DisabledLinkColor, v => link.DisabledLinkColor = v, ColorRole.Fore);
      }

      ButtonBase button = control as ButtonBase;

      // Classic 3D buttons, check boxes and radio buttons ignore dark colors for their frame
      // and box; flat ones draw with BackColor/ForeColor. Leave buttons with custom colors
      // (such as color swatches) alone.
      if (button != null && (!vbooIsDark || IsThemeOrSystemColor(button.BackColor)))
      {
        ThemeValue(button, "FlatStyle", () => button.FlatStyle, v => button.FlatStyle = v, FlatStyle.Flat);
        ThemeValue(button, "FlatAppearance.BorderColor", () => button.FlatAppearance.BorderColor, v => button.FlatAppearance.BorderColor = v, DarkBorder);
        ThemeValue(button, "FlatAppearance.MouseOverBackColor", () => button.FlatAppearance.MouseOverBackColor, v => button.FlatAppearance.MouseOverBackColor = v, DarkHover);
        ThemeValue(button, "FlatAppearance.MouseDownBackColor", () => button.FlatAppearance.MouseDownBackColor, v => button.FlatAppearance.MouseDownBackColor = v, DarkBorder);
      }

      ApplyBorderStyle(control);

      TreeView tree = control as TreeView;

      if (tree != null)
        ThemeColor(tree, "LineColor", () => tree.LineColor, v => tree.LineColor = v, ColorRole.Line);

      ListView list = control as ListView;

      if (list != null)
      {
        // Classic grid lines are always drawn in a light system color.
        ThemeValueIf(list, "GridLines", () => list.GridLines, v => list.GridLines = v, false, v => v);
        AttachPainter(list, () => new DarkHeaderPainter(list));
      }

      TabControl tabs = control as TabControl;

      if (tabs != null)
        AttachPainter(tabs, () => new DarkTabPainter(tabs));

      DataGrid grid = control as DataGrid;

      if (grid != null)
        ApplyDataGrid(grid);
    }

    // Classic sunken (Fixed3D) borders are drawn with white highlights; use a single line.
    private static void ApplyBorderStyle(Control control)
    {
      Func<BorderStyle, bool> is3D = v => v == BorderStyle.Fixed3D;
      TextBoxBase textBox = control as TextBoxBase;
      ListBox listBox = control as ListBox;
      ListView listView = control as ListView;
      TreeView treeView = control as TreeView;
      Label label = control as Label;
      Panel panel = control as Panel;
      UpDownBase upDown = control as UpDownBase;

      if (textBox != null)
        ThemeValueIf(textBox, "BorderStyle", () => textBox.BorderStyle, v => textBox.BorderStyle = v, BorderStyle.FixedSingle, is3D);
      else if (listBox != null)
        ThemeValueIf(listBox, "BorderStyle", () => listBox.BorderStyle, v => listBox.BorderStyle = v, BorderStyle.FixedSingle, is3D);
      else if (listView != null)
        ThemeValueIf(listView, "BorderStyle", () => listView.BorderStyle, v => listView.BorderStyle = v, BorderStyle.FixedSingle, is3D);
      else if (treeView != null)
        ThemeValueIf(treeView, "BorderStyle", () => treeView.BorderStyle, v => treeView.BorderStyle = v, BorderStyle.FixedSingle, is3D);
      else if (label != null)
        ThemeValueIf(label, "BorderStyle", () => label.BorderStyle, v => label.BorderStyle = v, BorderStyle.FixedSingle, is3D);
      else if (panel != null)
        ThemeValueIf(panel, "BorderStyle", () => panel.BorderStyle, v => panel.BorderStyle = v, BorderStyle.FixedSingle, is3D);
      else if (upDown != null)
        ThemeValueIf(upDown, "BorderStyle", () => upDown.BorderStyle, v => upDown.BorderStyle = v, BorderStyle.FixedSingle, is3D);
    }

    private static void ApplyDataGrid(DataGrid grid)
    {
      ThemeColor(grid, "BackgroundColor", () => grid.BackgroundColor, v => grid.BackgroundColor = v, ColorRole.Back);
      ThemeColor(grid, "AlternatingBackColor", () => grid.AlternatingBackColor, v => grid.AlternatingBackColor = v, ColorRole.Back);
      ThemeColor(grid, "HeaderBackColor", () => grid.HeaderBackColor, v => grid.HeaderBackColor = v, ColorRole.Back);
      ThemeColor(grid, "HeaderForeColor", () => grid.HeaderForeColor, v => grid.HeaderForeColor = v, ColorRole.Fore);
      ThemeColor(grid, "GridLineColor", () => grid.GridLineColor, v => grid.GridLineColor = v, ColorRole.Line);
      ThemeColor(grid, "CaptionBackColor", () => grid.CaptionBackColor, v => grid.CaptionBackColor = v, ColorRole.Back);
      ThemeColor(grid, "CaptionForeColor", () => grid.CaptionForeColor, v => grid.CaptionForeColor = v, ColorRole.Fore);
      ThemeColor(grid, "ParentRowsBackColor", () => grid.ParentRowsBackColor, v => grid.ParentRowsBackColor = v, ColorRole.Back);
      ThemeColor(grid, "ParentRowsForeColor", () => grid.ParentRowsForeColor, v => grid.ParentRowsForeColor = v, ColorRole.Fore);
      ThemeColor(grid, "LinkColor", () => grid.LinkColor, v => grid.LinkColor = v, ColorRole.Fore);

      foreach (DataGridTableStyle style in grid.TableStyles)
      {
        DataGridTableStyle s = style;

        ThemeColor(s, "BackColor", () => s.BackColor, v => s.BackColor = v, ColorRole.Back);
        ThemeColor(s, "ForeColor", () => s.ForeColor, v => s.ForeColor = v, ColorRole.Fore);
        ThemeColor(s, "AlternatingBackColor", () => s.AlternatingBackColor, v => s.AlternatingBackColor = v, ColorRole.Back);
        ThemeColor(s, "HeaderBackColor", () => s.HeaderBackColor, v => s.HeaderBackColor = v, ColorRole.Back);
        ThemeColor(s, "HeaderForeColor", () => s.HeaderForeColor, v => s.HeaderForeColor = v, ColorRole.Fore);
        ThemeColor(s, "GridLineColor", () => s.GridLineColor, v => s.GridLineColor = v, ColorRole.Line);
        ThemeColor(s, "LinkColor", () => s.LinkColor, v => s.LinkColor = v, ColorRole.Fore);
      }
    }

    private static void ApplyToolStrip(ToolStrip strip)
    {
      if (!IsHooked(strip))
        strip.ItemAdded += new ToolStripItemEventHandler(ToolStrip_ItemAdded);

      // Strips in ManagerRenderMode (the default, including menu drop-downs) follow
      // ToolStripManager.Renderer, which UpdateGlobalRenderer swaps. Others get it explicitly.
      if (strip.RenderMode != ToolStripRenderMode.ManagerRenderMode || HasOriginal(strip, "Renderer"))
        ThemeValue(strip, "Renderer", () => strip.Renderer, v => strip.Renderer = v, objDarkRenderer);

      foreach (ToolStripItem item in strip.Items)
        ApplyToolStripItem(item);
    }

    private static void ToolStrip_ItemAdded(object sender, ToolStripItemEventArgs e)
    {
      if (vbooIsDark)
        ApplyToolStripItem(e.Item);
    }

    private static void ApplyToolStripItem(ToolStripItem item)
    {
      ToolStripControlHost host = item as ToolStripControlHost;

      if (host != null)
      {
        // The host pushes its own colors onto the hosted control, so theme both.
        ThemeColor(host, "BackColor", () => host.BackColor, v => host.BackColor = v, ColorRole.Back);
        ThemeColor(host, "ForeColor", () => host.ForeColor, v => host.ForeColor = v, ColorRole.Fore);
        ApplyControl(host.Control);
      }

      ToolStripDropDownItem dropDownItem = item as ToolStripDropDownItem;

      if (dropDownItem != null && dropDownItem.HasDropDownItems)
        ApplyToolStrip(dropDownItem.DropDown);
    }

    // Painters stay attached once created; they only paint while dark mode is on.
    private static void AttachPainter(Control control, Func<ThemePainter> create)
    {
      ThemePainter painter;

      if (!Painters.TryGetValue(control, out painter))
      {
        if (!vbooIsDark)
          return;

        painter = create();
        Painters.Add(control, painter);
        control.HandleCreated += delegate (object sender, EventArgs e) { painter.Attach(); };
      }

      painter.Attach();
      painter.Invalidate();
    }

    // Sets a color for the current mode. Dark: derived from the original value. Light: the
    // original is restored. Values the application changed after theming are respected.
    private static void ThemeColor(object target, string key, Func<Color> getter, Action<Color> setter, ColorRole role)
    {
      Dictionary<string, object> saved = Originals.GetOrCreateValue(target);
      Color current = getter();
      object original;
      object applied;
      bool hasOriginal = saved.TryGetValue(key, out original) && saved.TryGetValue(key + "#", out applied) && SameColor((Color)applied, current);

      if (!hasOriginal)
      {
        saved.Remove(key);
        saved.Remove(key + "#");
      }

      if (vbooIsDark)
      {
        Color source = hasOriginal ? (Color)original : current;
        Color mapped = DarkEquivalent(source, role);

        if (mapped.IsEmpty)
        {
          // Nothing to map; if a previous mapping was undone by the application, forget it.
          return;
        }

        if (!hasOriginal)
          saved[key] = source;

        saved[key + "#"] = mapped;

        if (!SameColor(current, mapped))
          setter(mapped);
      }
      else if (hasOriginal)
      {
        setter((Color)original);
        saved.Remove(key);
        saved.Remove(key + "#");
      }
    }

    private static void ThemeValue<T>(object target, string key, Func<T> getter, Action<T> setter, T darkValue)
    {
      Dictionary<string, object> saved = Originals.GetOrCreateValue(target);
      object original;
      bool hasOriginal = saved.TryGetValue(key, out original);

      if (vbooIsDark)
      {
        if (!hasOriginal)
          saved[key] = getter();

        setter(darkValue);
      }
      else if (hasOriginal)
      {
        setter((T)original);
        saved.Remove(key);
      }
    }

    // Like ThemeValue, but in dark mode only changes values the predicate accepts.
    private static void ThemeValueIf<T>(object target, string key, Func<T> getter, Action<T> setter, T darkValue, Func<T, bool> applies)
    {
      if (vbooIsDark && !HasOriginal(target, key) && !applies(getter()))
        return;

      ThemeValue(target, key, getter, setter, darkValue);
    }

    private static bool HasOriginal(object target, string key)
    {
      Dictionary<string, object> saved;

      return Originals.TryGetValue(target, out saved) && saved.ContainsKey(key);
    }

    private static bool SameColor(Color a, Color b)
    {
      return a.ToArgb() == b.ToArgb() && a.IsSystemColor == b.IsSystemColor;
    }

    private static bool IsThemeOrSystemColor(Color color)
    {
      if (color.IsSystemColor)
        return true;

      if (color.IsKnownColor && (color.ToKnownColor() == KnownColor.White || color.ToKnownColor() == KnownColor.Black))
        return true;

      int argb = color.ToArgb();

      return argb == DarkSurface.ToArgb() || argb == DarkWindow.ToArgb() || argb == DarkRaised.ToArgb() || argb == DarkButton.ToArgb();
    }

    // Returns the dark replacement for a color, or Color.Empty to leave it unchanged.
    private static Color DarkEquivalent(Color color, ColorRole role)
    {
      if (color.IsEmpty || color.A < 255)
        return Color.Empty;

      KnownColor known = color.IsKnownColor ? color.ToKnownColor() : 0;

      switch (role)
      {
        case ColorRole.Back:
        case ColorRole.ButtonBack:
          if (color.IsSystemColor)
          {
            switch (known)
            {
              case KnownColor.Highlight:
              case KnownColor.HotTrack:
              case KnownColor.MenuHighlight:
                return Color.Empty;

              case KnownColor.Window:
                return DarkWindow;

              case KnownColor.Info:
              case KnownColor.ControlLight:
              case KnownColor.ControlLightLight:
              case KnownColor.ControlDark:
              case KnownColor.ControlDarkDark:
                return DarkRaised;

              default:
                return (role == ColorRole.ButtonBack) ? DarkButton : DarkSurface;
            }
          }

          if (known == KnownColor.White)
            return DarkWindow;

          return Color.Empty;

        case ColorRole.Line:
          if (color.IsSystemColor || known == KnownColor.Black)
            return DarkBorder;

          return Color.Empty;

        default:
          if (color.IsSystemColor)
          {
            switch (known)
            {
              case KnownColor.Highlight:
              case KnownColor.HighlightText:
              case KnownColor.MenuHighlight:
                return Color.Empty;

              case KnownColor.HotTrack:
                return DarkAccentText;

              case KnownColor.GrayText:
              case KnownColor.InactiveCaptionText:
                return DarkDisabledText;

              default:
                return DarkText;
            }
          }

          if (known == KnownColor.Black)
            return DarkText;

          // Custom text colors (mostly dark blue headings and links): lighten dark ones.
          if (((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255.0 < 0.5)
            return Color.FromArgb(Lighten(color.R), Lighten(color.G), Lighten(color.B));

          return Color.Empty;
      }
    }

    private static int Lighten(int value)
    {
      return value + (int)((255 - value) * 0.55);
    }

    private static void SetTitleBar(Form form)
    {
      if (!form.IsHandleCreated)
        return;

      FormState state;

      // Leave the title bar alone until the form has been themed dark at least once.
      if (!vbooIsDark && (!Forms.TryGetValue(form, out state) || !state.EverDark))
        return;

      int value = vbooIsDark ? 1 : 0;

      try
      {
        // DWMWA_USE_IMMERSIVE_DARK_MODE is 20 on Windows 10 20H1 and later, 19 before that.
        if (DwmSetWindowAttribute(form.Handle, 20, ref value, sizeof(int)) != 0)
          DwmSetWindowAttribute(form.Handle, 19, ref value, sizeof(int));

        // A window that is already showing doesn't repaint its caption for the new attribute
        // until it is next activated; toggle the non-client active state to force it.
        if (form.Visible)
        {
          bool active = (Form.ActiveForm == form);
          SendMessage(form.Handle, WM_NCACTIVATE, (IntPtr)(active ? 0 : 1), IntPtr.Zero);
          SendMessage(form.Handle, WM_NCACTIVATE, (IntPtr)(active ? 1 : 0), IntPtr.Zero);
        }
      }
      catch (Exception)
      {
        // dwmapi missing or attribute unsupported: keep the default title bar.
      }
    }

    private const int RDW_INVALIDATE = 0x1;
    private const int RDW_ERASE = 0x4;
    private const int RDW_ALLCHILDREN = 0x80;
    private const int RDW_FRAME = 0x400;
    private const int WM_NCACTIVATE = 0x86;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, int flags);
  }

  // Base for the window subclasses that paint native parts WinForms can't recolor (the
  // ListView column header and the TabControl tab strip). Only active in dark mode.
  internal abstract class ThemePainter : NativeWindow
  {
    protected const int WM_PAINT = 0xF;
    protected const int WM_ERASEBKGND = 0x14;

    public abstract void Attach();

    public void Invalidate()
    {
      if (this.Handle != IntPtr.Zero)
        InvalidateRect(this.Handle, IntPtr.Zero, true);
    }

    protected override void WndProc(ref Message m)
    {
      if (ThemeManager.IsDark)
      {
        if (m.Msg == WM_ERASEBKGND)
        {
          m.Result = (IntPtr)1;
          return;
        }

        if (m.Msg == WM_PAINT)
        {
          PAINTSTRUCT ps;
          IntPtr hdc = BeginPaint(this.Handle, out ps);

          try
          {
            RECT client;
            GetClientRect(this.Handle, out client);
            Rectangle bounds = Rectangle.FromLTRB(client.Left, client.Top, client.Right, client.Bottom);

            if (bounds.Width > 0 && bounds.Height > 0)
            {
              using (BufferedGraphics buffer = BufferedGraphicsManager.Current.Allocate(hdc, bounds))
              {
                this.Paint(buffer.Graphics, bounds);
                buffer.Render();
              }
            }
          }
          finally
          {
            EndPaint(this.Handle, ref ps);
          }

          m.Result = IntPtr.Zero;
          return;
        }
      }

      base.WndProc(ref m);
    }

    protected abstract void Paint(Graphics g, Rectangle bounds);

    protected void AssignTo(IntPtr hwnd)
    {
      if (hwnd == IntPtr.Zero || hwnd == this.Handle)
        return;

      if (this.Handle != IntPtr.Zero)
        this.ReleaseHandle();

      this.AssignHandle(hwnd);
    }

    [StructLayout(LayoutKind.Sequential)]
    protected struct RECT
    {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PAINTSTRUCT
    {
      public IntPtr hdc;
      public bool fErase;
      public RECT rcPaint;
      public bool fRestore;
      public bool fIncUpdate;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
      public byte[] rgbReserved;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);

    [DllImport("user32.dll")]
    private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
  }

  // Paints a ListView's column header, including the native sort images frmMain sets with
  // LVM_SETCOLUMN, because the classic header always uses the light system colors.
  internal sealed class DarkHeaderPainter : ThemePainter
  {
    private const int LVM_GETHEADER = 0x101F;
    private const int HDM_GETITEMCOUNT = 0x1200;
    private const int HDM_GETITEMRECT = 0x1207;
    private const int HDM_GETIMAGELIST = 0x1209;
    private const int HDM_GETITEMW = 0x120B;
    private const int HDI_TEXT = 0x2;
    private const int HDI_FORMAT = 0x4;
    private const int HDI_IMAGE = 0x20;
    private const int HDF_JUSTIFYMASK = 0x3;
    private const int HDF_RIGHT = 0x1;
    private const int HDF_CENTER = 0x2;
    private const int HDF_SORTDOWN = 0x200;
    private const int HDF_SORTUP = 0x400;
    private const int HDF_IMAGE = 0x800;
    private const int HDF_BITMAP_ON_RIGHT = 0x1000;
    private const int ILD_TRANSPARENT = 0x1;
    private const int TEXT_PADDING = 6;

    private readonly ListView objListView;

    public DarkHeaderPainter(ListView listView)
    {
      this.objListView = listView;
    }

    public override void Attach()
    {
      if (this.objListView.IsHandleCreated)
        this.AssignTo(SendMessage(this.objListView.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero));
    }

    protected override void Paint(Graphics g, Rectangle bounds)
    {
      g.Clear(ThemeManager.DarkRaised);

      int count = SendMessage(this.Handle, HDM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero).ToInt32();
      IntPtr imageList = SendMessage(this.Handle, HDM_GETIMAGELIST, IntPtr.Zero, IntPtr.Zero);
      IntPtr textBuffer = Marshal.AllocHGlobal(520);

      try
      {
        using (Pen border = new Pen(ThemeManager.DarkBorder))
        {
          for (int i = 0; i < count; i++)
          {
            RECT rect = new RECT();
            SendMessage(this.Handle, HDM_GETITEMRECT, (IntPtr)i, ref rect);
            Rectangle itemBounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);

            HDITEM item = new HDITEM();
            item.mask = HDI_TEXT | HDI_FORMAT | HDI_IMAGE;
            item.pszText = textBuffer;
            item.cchTextMax = 260;
            SendMessage(this.Handle, HDM_GETITEMW, (IntPtr)i, ref item);

            string text = Marshal.PtrToStringUni(textBuffer) ?? "";

            this.PaintItem(g, itemBounds, text, item, imageList);
            g.DrawLine(border, itemBounds.Right - 1, itemBounds.Top, itemBounds.Right - 1, itemBounds.Bottom - 1);
          }

          g.DrawLine(border, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
        }
      }
      finally
      {
        Marshal.FreeHGlobal(textBuffer);
      }
    }

    private void PaintItem(Graphics g, Rectangle bounds, string text, HDITEM item, IntPtr imageList)
    {
      Rectangle textBounds = Rectangle.FromLTRB(bounds.Left + TEXT_PADDING, bounds.Top, bounds.Right - TEXT_PADDING, bounds.Bottom);
      bool hasImage = ((item.fmt & HDF_IMAGE) != 0) && (imageList != IntPtr.Zero) && (item.iImage >= 0);
      int imageWidth = 0;
      int imageHeight = 0;

      if (hasImage)
        ImageList_GetIconSize(imageList, out imageWidth, out imageHeight);

      if (hasImage && ((item.fmt & HDF_BITMAP_ON_RIGHT) == 0))
        textBounds.X += imageWidth + 4;

      if (hasImage)
        textBounds.Width -= imageWidth + 4;

      if (textBounds.Width < 0)
        textBounds.Width = 0;

      TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

      switch (item.fmt & HDF_JUSTIFYMASK)
      {
        case HDF_RIGHT:
          flags |= TextFormatFlags.Right;
          break;

        case HDF_CENTER:
          flags |= TextFormatFlags.HorizontalCenter;
          break;
      }

      TextRenderer.DrawText(g, text, this.objListView.Font, textBounds, ThemeManager.DarkText, flags);

      if (hasImage)
      {
        int x;

        if ((item.fmt & HDF_BITMAP_ON_RIGHT) != 0)
        {
          int textWidth = Math.Min(TextRenderer.MeasureText(g, text, this.objListView.Font, textBounds.Size, flags).Width, textBounds.Width);

          if ((item.fmt & HDF_JUSTIFYMASK) == HDF_RIGHT)
            x = textBounds.Right + 4;
          else
            x = textBounds.Left + textWidth + 4;
        }
        else
          x = bounds.Left + TEXT_PADDING;

        int y = bounds.Top + ((bounds.Height - imageHeight) / 2);
        IntPtr hdc = g.GetHdc();

        try
        {
          ImageList_Draw(imageList, item.iImage, hdc, x, y, ILD_TRANSPARENT);
        }
        finally
        {
          g.ReleaseHdc(hdc);
        }
      }
      else if ((item.fmt & (HDF_SORTUP | HDF_SORTDOWN)) != 0)
      {
        int cx = bounds.Left + (bounds.Width / 2);
        Point[] arrow = ((item.fmt & HDF_SORTUP) != 0)
          ? new Point[] { new Point(cx - 4, bounds.Top + 5), new Point(cx + 4, bounds.Top + 5), new Point(cx, bounds.Top + 1) }
          : new Point[] { new Point(cx - 4, bounds.Top + 1), new Point(cx + 4, bounds.Top + 1), new Point(cx, bounds.Top + 5) };

        using (SolidBrush brush = new SolidBrush(ThemeManager.DarkDisabledText))
          g.FillPolygon(brush, arrow);
      }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HDITEM
    {
      public int mask;
      public int cxy;
      public IntPtr pszText;
      public IntPtr hbm;
      public int cchTextMax;
      public int fmt;
      public IntPtr lParam;
      public int iImage;
      public int iOrder;
      public uint type;
      public IntPtr pvFilter;
      public uint state;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref HDITEM lParam);

    [DllImport("comctl32.dll")]
    private static extern bool ImageList_Draw(IntPtr himl, int i, IntPtr hdcDst, int x, int y, int fStyle);

    [DllImport("comctl32.dll")]
    private static extern bool ImageList_GetIconSize(IntPtr himl, out int cx, out int cy);
  }

  // Paints a TabControl's tab strip and page border; the tab pages paint themselves.
  internal sealed class DarkTabPainter : ThemePainter
  {
    private readonly TabControl objTabControl;

    public DarkTabPainter(TabControl tabControl)
    {
      this.objTabControl = tabControl;
    }

    public override void Attach()
    {
      if (this.objTabControl.IsHandleCreated)
        this.AssignTo(this.objTabControl.Handle);
    }

    protected override void Paint(Graphics g, Rectangle bounds)
    {
      TabControl tabs = this.objTabControl;
      Color parentBack = (tabs.Parent != null) ? tabs.Parent.BackColor : ThemeManager.DarkSurface;
      Color pageBack = (tabs.SelectedTab != null) ? tabs.SelectedTab.BackColor : ThemeManager.DarkSurface;

      g.Clear(parentBack);

      Rectangle page = tabs.DisplayRectangle;
      Rectangle frame = Rectangle.Inflate(page, 2, 2);

      using (SolidBrush pageBrush = new SolidBrush(pageBack))
      using (SolidBrush tabBrush = new SolidBrush(ThemeManager.DarkRaised))
      using (Pen border = new Pen(ThemeManager.DarkBorder))
      {
        g.FillRectangle(pageBrush, frame);
        g.DrawRectangle(border, frame.X, frame.Y, frame.Width - 1, frame.Height - 1);

        for (int i = 0; i < tabs.TabCount; i++)
        {
          Rectangle tab = tabs.GetTabRect(i);
          bool selected = (i == tabs.SelectedIndex);

          if (selected)
            tab = Rectangle.FromLTRB(tab.Left - 2, tab.Top - 2, tab.Right + 2, frame.Top + 1);

          g.FillRectangle(selected ? pageBrush : tabBrush, tab);
          g.DrawLine(border, tab.Left, tab.Bottom - 1, tab.Left, tab.Top);
          g.DrawLine(border, tab.Left, tab.Top, tab.Right - 1, tab.Top);
          g.DrawLine(border, tab.Right - 1, tab.Top, tab.Right - 1, tab.Bottom - 1);

          if (!selected)
            g.DrawLine(border, tab.Left, tab.Bottom - 1, tab.Right - 1, tab.Bottom - 1);

          TabPage tabPage = tabs.TabPages[i];
          Rectangle textBounds = tab;

          if (tabs.ImageList != null && tabPage.ImageIndex >= 0 && tabPage.ImageIndex < tabs.ImageList.Images.Count)
          {
            Image image = tabs.ImageList.Images[tabPage.ImageIndex];
            g.DrawImage(image, tab.Left + 4, tab.Top + ((tab.Height - image.Height) / 2));
            textBounds = Rectangle.FromLTRB(tab.Left + image.Width + 6, tab.Top, tab.Right, tab.Bottom);
          }

          TextRenderer.DrawText(g, tabPage.Text, tabs.Font, textBounds, ThemeManager.DarkText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }
      }
    }
  }

  internal sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
  {
    public DarkToolStripRenderer() : base(new DarkColorTable())
    {
      this.RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
      e.TextColor = e.Item.Enabled ? ThemeManager.DarkText : ThemeManager.DarkDisabledText;
      base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
      e.ArrowColor = ((e.Item == null) || e.Item.Enabled) ? ThemeManager.DarkText : ThemeManager.DarkDisabledText;
      base.OnRenderArrow(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
      // The default check glyph is a black bitmap; draw a light one instead.
      if (e.Item.Image != null)
      {
        base.OnRenderItemCheck(e);
        return;
      }

      Rectangle box = Rectangle.Inflate(e.ImageRectangle, 1, 1);

      using (SolidBrush back = new SolidBrush(e.Item.Selected ? this.ColorTable.CheckSelectedBackground : this.ColorTable.CheckBackground))
        e.Graphics.FillRectangle(back, box);

      using (Pen pen = new Pen(e.Item.Enabled ? ThemeManager.DarkText : ThemeManager.DarkDisabledText, 2f))
      {
        System.Drawing.Drawing2D.SmoothingMode mode = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        int x = box.Left + (box.Width / 2);
        int y = box.Top + (box.Height / 2);
        e.Graphics.DrawLines(pen, new Point[] { new Point(x - 4, y), new Point(x - 1, y + 3), new Point(x + 4, y - 3) });
        e.Graphics.SmoothingMode = mode;
      }
    }
  }

  internal sealed class DarkColorTable : ProfessionalColorTable
  {
    private static readonly Color Selected = Color.FromArgb(65, 65, 65);
    private static readonly Color Pressed = Color.FromArgb(80, 80, 80);
    private static readonly Color Checked = Color.FromArgb(0, 84, 153);

    public DarkColorTable()
    {
      this.UseSystemColors = false;
    }

    public override Color ButtonCheckedGradientBegin { get { return Checked; } }
    public override Color ButtonCheckedGradientEnd { get { return Checked; } }
    public override Color ButtonCheckedGradientMiddle { get { return Checked; } }
    public override Color ButtonCheckedHighlight { get { return Checked; } }
    public override Color ButtonCheckedHighlightBorder { get { return ThemeManager.DarkBorder; } }
    public override Color ButtonPressedBorder { get { return ThemeManager.DarkBorder; } }
    public override Color ButtonPressedGradientBegin { get { return Pressed; } }
    public override Color ButtonPressedGradientEnd { get { return Pressed; } }
    public override Color ButtonPressedGradientMiddle { get { return Pressed; } }
    public override Color ButtonPressedHighlight { get { return Pressed; } }
    public override Color ButtonPressedHighlightBorder { get { return ThemeManager.DarkBorder; } }
    public override Color ButtonSelectedBorder { get { return ThemeManager.DarkBorder; } }
    public override Color ButtonSelectedGradientBegin { get { return Selected; } }
    public override Color ButtonSelectedGradientEnd { get { return Selected; } }
    public override Color ButtonSelectedGradientMiddle { get { return Selected; } }
    public override Color ButtonSelectedHighlight { get { return Selected; } }
    public override Color ButtonSelectedHighlightBorder { get { return ThemeManager.DarkBorder; } }
    public override Color CheckBackground { get { return Checked; } }
    public override Color CheckPressedBackground { get { return Checked; } }
    public override Color CheckSelectedBackground { get { return Checked; } }
    public override Color GripDark { get { return ThemeManager.DarkBorder; } }
    public override Color GripLight { get { return ThemeManager.DarkRaised; } }
    public override Color ImageMarginGradientBegin { get { return ThemeManager.DarkRaised; } }
    public override Color ImageMarginGradientEnd { get { return ThemeManager.DarkRaised; } }
    public override Color ImageMarginGradientMiddle { get { return ThemeManager.DarkRaised; } }
    public override Color MenuBorder { get { return ThemeManager.DarkBorder; } }
    public override Color MenuItemBorder { get { return Selected; } }
    public override Color MenuItemPressedGradientBegin { get { return ThemeManager.DarkRaised; } }
    public override Color MenuItemPressedGradientEnd { get { return ThemeManager.DarkRaised; } }
    public override Color MenuItemPressedGradientMiddle { get { return ThemeManager.DarkRaised; } }
    public override Color MenuItemSelected { get { return Selected; } }
    public override Color MenuItemSelectedGradientBegin { get { return Selected; } }
    public override Color MenuItemSelectedGradientEnd { get { return Selected; } }
    public override Color MenuStripGradientBegin { get { return ThemeManager.DarkSurface; } }
    public override Color MenuStripGradientEnd { get { return ThemeManager.DarkSurface; } }
    public override Color OverflowButtonGradientBegin { get { return ThemeManager.DarkRaised; } }
    public override Color OverflowButtonGradientEnd { get { return ThemeManager.DarkRaised; } }
    public override Color OverflowButtonGradientMiddle { get { return ThemeManager.DarkRaised; } }
    public override Color RaftingContainerGradientBegin { get { return ThemeManager.DarkSurface; } }
    public override Color RaftingContainerGradientEnd { get { return ThemeManager.DarkSurface; } }
    public override Color SeparatorDark { get { return ThemeManager.DarkBorder; } }
    public override Color SeparatorLight { get { return ThemeManager.DarkRaised; } }
    public override Color StatusStripGradientBegin { get { return ThemeManager.DarkSurface; } }
    public override Color StatusStripGradientEnd { get { return ThemeManager.DarkSurface; } }
    public override Color ToolStripBorder { get { return ThemeManager.DarkRaised; } }
    public override Color ToolStripContentPanelGradientBegin { get { return ThemeManager.DarkSurface; } }
    public override Color ToolStripContentPanelGradientEnd { get { return ThemeManager.DarkSurface; } }
    public override Color ToolStripDropDownBackground { get { return ThemeManager.DarkRaised; } }
    public override Color ToolStripGradientBegin { get { return ThemeManager.DarkSurface; } }
    public override Color ToolStripGradientEnd { get { return ThemeManager.DarkSurface; } }
    public override Color ToolStripGradientMiddle { get { return ThemeManager.DarkSurface; } }
    public override Color ToolStripPanelGradientBegin { get { return ThemeManager.DarkSurface; } }
    public override Color ToolStripPanelGradientEnd { get { return ThemeManager.DarkSurface; } }
  }
}
