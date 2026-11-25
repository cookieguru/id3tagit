using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ID3_TagIT
{
  // The arrows of combo boxes and spin boxes are drawn by Windows in a gray the dark themes
  // can't change, and a drop-down list combo box is a gray button.  These painters draw them
  // from the theme colors.

  internal static class DarkArrow
  {
    private const int WIDTH = 7;
    private const int HEIGHT = 4;

    public static void Draw(Graphics g, Rectangle bounds, bool up, Color color)
    {
      int centerX = bounds.Left + (bounds.Width / 2);
      int centerY = bounds.Top + (bounds.Height / 2);
      int tip = up ? centerY - (HEIGHT / 2) : centerY + (HEIGHT / 2);
      int baseLine = up ? centerY + (HEIGHT / 2) : centerY - (HEIGHT / 2);
      Point[] arrow =
      {
        new Point(centerX - (WIDTH / 2), baseLine),
        new Point(centerX + (WIDTH / 2), baseLine),
        new Point(centerX, tip)
      };
      SmoothingMode saved = g.SmoothingMode;
      g.SmoothingMode = SmoothingMode.AntiAlias;

      using (SolidBrush brush = new SolidBrush(color))
        g.FillPolygon(brush, arrow);

      g.SmoothingMode = saved;
    }
  }

  // A drop-down list combo box is painted whole.  An editable one keeps what Windows paints
  // except for its button.
  internal sealed class DarkComboPainter : ThemePainter
  {
    private const int CB_GETCOMBOBOXINFO = 0x164;
    private const int TEXT_PADDING = 4;

    private readonly ComboBox objCombo;

    public DarkComboPainter(ComboBox combo)
    {
      this.objCombo = combo;
    }

    public override void Attach()
    {
      if (this.objCombo.IsHandleCreated)
        this.AssignTo(this.objCombo.Handle);
    }

    protected override bool OwnsPainting
    {
      get
      {
        return this.objCombo.DropDownStyle == ComboBoxStyle.DropDownList;
      }
    }

    protected override void WndProc(ref Message m)
    {
      base.WndProc(ref m);

      if (m.Msg == WM_PAINT && ThemeManager.IsDark && !this.OwnsPainting)
        this.PaintEditableButton();
    }

    protected override void Paint(Graphics g, Rectangle bounds)
    {
      bool enabled = this.objCombo.Enabled;
      int buttonWidth = SystemInformation.VerticalScrollBarWidth;
      Rectangle button = new Rectangle(bounds.Right - buttonWidth - 1, bounds.Top + 1, buttonWidth, bounds.Height - 2);
      Rectangle text = Rectangle.FromLTRB(bounds.Left + 1, bounds.Top + 1, button.Left, bounds.Bottom - 1);
      bool focused = this.objCombo.Focused && enabled;

      g.Clear(enabled ? ThemeManager.DarkWindow : ThemeManager.DarkSurface);

      using (Pen border = new Pen(ThemeManager.DarkBorder))
        g.DrawRectangle(border, 0, 0, bounds.Width - 1, bounds.Height - 1);

      Color textColor = enabled ? ThemeManager.DarkText : ThemeManager.DarkDisabledText;

      if (focused)
      {
        using (SolidBrush highlight = new SolidBrush(SystemColors.Highlight))
          g.FillRectangle(highlight, Rectangle.Inflate(text, -1, -1));

        textColor = SystemColors.HighlightText;
      }

      Rectangle textBounds = Rectangle.FromLTRB(text.Left + TEXT_PADDING - 1, text.Top, text.Right, text.Bottom);
      TextRenderer.DrawText(g, this.objCombo.Text, this.objCombo.Font, textBounds, textColor,
        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

      DarkArrow.Draw(g, button, false, enabled ? ThemeManager.DarkText : ThemeManager.DarkDisabledText);
    }

    private void PaintEditableButton()
    {
      COMBOBOXINFO info = new COMBOBOXINFO();
      info.cbSize = Marshal.SizeOf(typeof(COMBOBOXINFO));

      if (SendMessage(this.Handle, CB_GETCOMBOBOXINFO, IntPtr.Zero, ref info) == IntPtr.Zero)
        return;

      Rectangle button = Rectangle.FromLTRB(info.rcButtonLeft, info.rcButtonTop, info.rcButtonRight, info.rcButtonBottom);

      if (button.Width <= 0 || button.Height <= 0)
        return;

      IntPtr hdc = GetDC(this.Handle);

      if (hdc == IntPtr.Zero)
        return;

      try
      {
        using (Graphics g = Graphics.FromHdc(hdc))
        using (SolidBrush fill = new SolidBrush(this.objCombo.Enabled ? ThemeManager.DarkWindow : ThemeManager.DarkSurface))
        {
          g.FillRectangle(fill, button);
          DarkArrow.Draw(g, button, false, this.objCombo.Enabled ? ThemeManager.DarkText : ThemeManager.DarkDisabledText);
        }
      }
      finally
      {
        ReleaseDC(this.Handle, hdc);
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

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref COMBOBOXINFO lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
  }

  // Windows draws a list view's column header with the items view theme, but in a text color
  // that is nearly black.  Windows keeps drawing the header and its sort images; this only
  // sets the text color, through the header's custom draw notification to its parent.
  internal sealed class DarkHeaderTextPainter : ThemePainter
  {
    private const int WM_NOTIFY = 0x4E;
    private const int LVM_GETHEADER = 0x101F;
    private const int NM_CUSTOMDRAW = -12;
    private const int CDDS_PREPAINT = 0x1;
    private const int CDDS_ITEMPREPAINT = 0x10001;
    private const int CDRF_NEWFONT = 0x2;
    private const int CDRF_NOTIFYITEMDRAW = 0x20;

    private readonly ListView objListView;

    public DarkHeaderTextPainter(ListView listView)
    {
      this.objListView = listView;
    }

    public override void Attach()
    {
      if (this.objListView.IsHandleCreated)
        this.AssignTo(this.objListView.Handle);
    }

    protected override bool OwnsPainting
    {
      get
      {
        return false;
      }
    }

    protected override void Paint(Graphics g, Rectangle bounds)
    {
    }

    protected override void WndProc(ref Message m)
    {
      if (m.Msg == WM_NOTIFY && ThemeManager.IsDark && this.CustomDrawHeader(ref m))
        return;

      base.WndProc(ref m);
    }

    private bool CustomDrawHeader(ref Message m)
    {
      NMCUSTOMDRAW notice = (NMCUSTOMDRAW)Marshal.PtrToStructure(m.LParam, typeof(NMCUSTOMDRAW));

      if (notice.code != NM_CUSTOMDRAW || notice.hwndFrom != SendMessage(this.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero))
        return false;

      if (notice.dwDrawStage == CDDS_PREPAINT)
      {
        m.Result = (IntPtr)CDRF_NOTIFYITEMDRAW;
        return true;
      }

      if (notice.dwDrawStage != CDDS_ITEMPREPAINT)
        return false;

      SetTextColor(notice.hdc, ColorTranslator.ToWin32(ThemeManager.DarkText));
      m.Result = (IntPtr)CDRF_NEWFONT;

      return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NMCUSTOMDRAW
    {
      public IntPtr hwndFrom;
      public IntPtr idFrom;
      public int code;
      public int dwDrawStage;
      public IntPtr hdc;
      public RECT rc;
      public IntPtr dwItemSpec;
      public int uItemState;
      public IntPtr lItemlParam;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("gdi32.dll")]
    private static extern int SetTextColor(IntPtr hdc, int color);
  }

  // The up and down buttons inside a NumericUpDown.
  internal sealed class DarkSpinButtonsPainter : ThemePainter
  {
    private readonly Control objButtons;

    public DarkSpinButtonsPainter(Control buttons)
    {
      this.objButtons = buttons;
    }

    public override void Attach()
    {
      if (this.objButtons.IsHandleCreated)
        this.AssignTo(this.objButtons.Handle);
    }

    protected override void Paint(Graphics g, Rectangle bounds)
    {
      Color arrow = this.objButtons.Enabled ? ThemeManager.DarkText : ThemeManager.DarkDisabledText;
      int half = bounds.Height / 2;

      g.Clear(ThemeManager.DarkRaised);

      using (Pen border = new Pen(ThemeManager.DarkBorder))
        g.DrawLine(border, bounds.Left, half, bounds.Right, half);

      DarkArrow.Draw(g, new Rectangle(bounds.Left, bounds.Top, bounds.Width, half), true, arrow);
      DarkArrow.Draw(g, new Rectangle(bounds.Left, bounds.Top + half + 1, bounds.Width, bounds.Height - half - 1), false, arrow);
    }
  }
}
