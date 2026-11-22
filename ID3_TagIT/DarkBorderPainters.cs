using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ID3_TagIT
{
  // Windows draws the border of a group box, a spin box and a rich text box in the text color,
  // which is bright in dark mode.  These painters draw the border in the theme's border color.

  // Paints a whole group box: Windows has no dark theme for it.
  internal sealed class DarkGroupBoxPainter : ThemePainter
  {
    private const int TEXT_LEFT = 8;

    private readonly GroupBox objGroupBox;

    public DarkGroupBoxPainter(GroupBox groupBox)
    {
      this.objGroupBox = groupBox;
    }

    public override void Attach()
    {
      if (this.objGroupBox.IsHandleCreated)
        this.AssignTo(this.objGroupBox.Handle);
    }

    protected override void Paint(Graphics g, Rectangle bounds)
    {
      TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.HidePrefix;
      string text = this.objGroupBox.Text;
      Size textSize = string.IsNullOrEmpty(text) ? Size.Empty : TextRenderer.MeasureText(g, text, this.objGroupBox.Font, new Size(int.MaxValue, int.MaxValue), flags);
      int top = textSize.Height / 2;
      int right = bounds.Width - 1;
      int bottom = bounds.Height - 1;

      g.Clear(this.BackdropColor());

      using (Pen border = new Pen(ThemeManager.DarkBorder))
      {
        g.DrawLine(border, 0, top, 0, bottom);
        g.DrawLine(border, 0, bottom, right, bottom);
        g.DrawLine(border, right, top, right, bottom);

        if (textSize.Width == 0)
        {
          g.DrawLine(border, 0, top, right, top);
        }
        else
        {
          g.DrawLine(border, 0, top, TEXT_LEFT - 2, top);
          g.DrawLine(border, TEXT_LEFT + textSize.Width + 1, top, right, top);
        }
      }

      if (textSize.Width > 0)
      {
        Color color = this.objGroupBox.Enabled ? this.objGroupBox.ForeColor : ThemeManager.DarkDisabledText;
        TextRenderer.DrawText(g, text, this.objGroupBox.Font, new Point(TEXT_LEFT, 0), color, flags);
      }
    }

    private Color BackdropColor()
    {
      for (Control control = this.objGroupBox; control != null; control = control.Parent)
      {
        if (control.BackColor.A == 255)
          return control.BackColor;
      }

      return ThemeManager.DarkSurface;
    }
  }

  // Draws over the one pixel border Windows paints, after Windows has painted it.
  internal sealed class DarkBorderPainter : ThemePainter
  {
    private const int WM_NCPAINT = 0x85;

    private readonly Control objControl;
    private readonly bool vbooNonClient;

    // A border in the non-client area (text boxes) or in the client area (spin boxes).
    public DarkBorderPainter(Control control, bool nonClient)
    {
      this.objControl = control;
      this.vbooNonClient = nonClient;
    }

    public override void Attach()
    {
      if (this.objControl.IsHandleCreated)
        this.AssignTo(this.objControl.Handle);
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
      base.WndProc(ref m);

      if (!ThemeManager.IsDark)
        return;

      if (this.vbooNonClient ? m.Msg == WM_NCPAINT : m.Msg == WM_PAINT)
        this.PaintBorder();
    }

    private void PaintBorder()
    {
      IntPtr hdc = this.vbooNonClient ? GetWindowDC(this.Handle) : GetDC(this.Handle);

      if (hdc == IntPtr.Zero)
        return;

      try
      {
        RECT rect;

        if (!(this.vbooNonClient ? GetWindowRect(this.Handle, out rect) : GetClientRect(this.Handle, out rect)))
          return;

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        using (Graphics g = Graphics.FromHdc(hdc))
        using (Pen border = new Pen(ThemeManager.DarkBorder))
          g.DrawRectangle(border, 0, 0, width - 1, height - 1);
      }
      finally
      {
        ReleaseDC(this.Handle, hdc);
      }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
  }
}
