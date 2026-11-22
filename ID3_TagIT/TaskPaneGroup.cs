using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ID3_TagIT
{
  // A collapsible box with a header and a round chevron button, standing in for the groups of
  // the DotNetBar task pane in 3.3.0. The links go in Body, which collapsing hides as a whole,
  // so a link the form hid stays hidden after the group is expanded again.
  //
  // The group paints its own fill and border from the theme colors, because Windows has no
  // dark rendering for a hand-drawn box like this.
  internal class TaskPaneGroup : Panel
  {
    private const int BORDER = 1;
    private const int HEADER_PADDING = 10;
    private const int TEXT_INDENT = 6;
    private const int ICON_GAP = 4;
    private const int CHEVRON_SIZE = 17;
    private const int CHEVRON_MARGIN = 4;
    private const int ARROW_WIDTH = 7;
    private const int ARROW_HEIGHT = 4;

    private static readonly Color LightFill = Color.FromArgb(228, 235, 249);
    private static readonly Color LightBorder = Color.FromArgb(188, 202, 234);
    private static readonly Color LightText = Color.FromArgb(33, 93, 198);

    // Tests replace this, because high contrast can't be switched on from a test.
    internal static Func<bool> IsHighContrast = delegate () { return SystemInformation.HighContrast; };

    private readonly Panel objBody = new Panel();
    private Font objHeaderFont;
    private Image objHeaderImage;
    private bool vbooExpanded = true;
    private int vintExpandedHeight;

    public TaskPaneGroup()
    {
      this.DoubleBuffered = true;
      this.SetStyle(ControlStyles.ResizeRedraw, true);

      this.objBody.Dock = DockStyle.Fill;
      this.objBody.BackColor = Color.Transparent;
      this.Controls.Add(this.objBody);
      this.UpdatePadding();
    }

    internal event EventHandler ExpandedChanged;

    internal Panel Body
    {
      get
      {
        return this.objBody;
      }
    }

    [Browsable(true)]
    public override string Text
    {
      get
      {
        return base.Text;
      }
      set
      {
        base.Text = value;
        this.Invalidate();
      }
    }

    [DefaultValue(null)]
    public Image HeaderImage
    {
      get
      {
        return this.objHeaderImage;
      }
      set
      {
        this.objHeaderImage = value;
        this.Invalidate();
      }
    }

    [DefaultValue(true)]
    public bool Expanded
    {
      get
      {
        return this.vbooExpanded;
      }
      set
      {
        if (value == this.vbooExpanded)
          return;

        this.vbooExpanded = value;

        if (value)
        {
          this.objBody.Visible = true;
          this.Height = this.vintExpandedHeight;
        }
        else
        {
          this.vintExpandedHeight = this.Height;
          this.objBody.Visible = false;
          this.MoveFocusOutOfBody();
          this.Height = this.CollapsedHeight;
        }

        this.Invalidate();

        if (this.ExpandedChanged != null)
          this.ExpandedChanged(this, EventArgs.Empty);
      }
    }

    internal int HeaderHeight
    {
      get
      {
        return this.Font.Height + HEADER_PADDING;
      }
    }

    internal Rectangle HeaderBounds
    {
      get
      {
        return new Rectangle(BORDER, BORDER, this.Width - (2 * BORDER), this.HeaderHeight);
      }
    }

    internal Rectangle ChevronBounds
    {
      get
      {
        Rectangle header = this.HeaderBounds;

        return new Rectangle(header.Right - CHEVRON_MARGIN - CHEVRON_SIZE, header.Top + ((header.Height - CHEVRON_SIZE) / 2), CHEVRON_SIZE, CHEVRON_SIZE);
      }
    }

    internal Color FillColor
    {
      get
      {
        if (IsHighContrast())
          return SystemColors.Window;

        return ThemeManager.IsDark ? ThemeManager.DarkTint : LightFill;
      }
    }

    internal Color BorderColor
    {
      get
      {
        if (IsHighContrast())
          return SystemColors.WindowText;

        return ThemeManager.IsDark ? ThemeManager.DarkTintBorder : LightBorder;
      }
    }

    internal Color TextColor
    {
      get
      {
        if (IsHighContrast())
          return SystemColors.WindowText;

        return ThemeManager.IsDark ? ThemeManager.DarkAccentText : LightText;
      }
    }

    private int CollapsedHeight
    {
      get
      {
        return this.HeaderHeight + (2 * BORDER);
      }
    }

    private void UpdatePadding()
    {
      this.Padding = new Padding(BORDER, BORDER + this.HeaderHeight, BORDER, BORDER);
    }

    // Hiding the body leaves the form's active control pointing at a link nobody can see.
    private void MoveFocusOutOfBody()
    {
      Form form = this.FindForm();

      if (form != null && form.ActiveControl != null && this.objBody.Contains(form.ActiveControl))
        form.SelectNextControl(this, true, true, true, true);
    }

    protected override void OnFontChanged(EventArgs e)
    {
      base.OnFontChanged(e);

      if (this.objHeaderFont != null)
      {
        this.objHeaderFont.Dispose();
        this.objHeaderFont = null;
      }

      this.UpdatePadding();

      if (!this.vbooExpanded)
        this.Height = this.CollapsedHeight;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
      base.OnMouseUp(e);

      if (e.Button == MouseButtons.Left && this.HeaderBounds.Contains(e.Location))
        this.Expanded = !this.Expanded;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      Graphics g = e.Graphics;
      Rectangle header = this.HeaderBounds;

      using (SolidBrush fill = new SolidBrush(this.FillColor))
        g.FillRectangle(fill, this.ClientRectangle);

      using (Pen border = new Pen(this.BorderColor))
      {
        g.DrawRectangle(border, 0, 0, this.Width - 1, this.Height - 1);

        if (this.vbooExpanded)
          g.DrawLine(border, header.Left, header.Bottom - 1, header.Right - 1, header.Bottom - 1);
      }

      int textLeft = header.Left + TEXT_INDENT;

      if (this.objHeaderImage != null)
      {
        g.DrawImage(this.objHeaderImage, textLeft, header.Top + ((header.Height - this.objHeaderImage.Height) / 2), this.objHeaderImage.Width, this.objHeaderImage.Height);
        textLeft += this.objHeaderImage.Width + ICON_GAP;
      }

      Rectangle textBounds = new Rectangle(textLeft, header.Top, this.ChevronBounds.Left - textLeft, header.Height);
      TextRenderer.DrawText(g, this.Text, this.HeaderFont, textBounds, this.TextColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
      this.PaintChevron(g);
    }

    private Font HeaderFont
    {
      get
      {
        if (this.objHeaderFont == null)
          this.objHeaderFont = new Font(this.Font, FontStyle.Bold);

        return this.objHeaderFont;
      }
    }

    private void PaintChevron(Graphics g)
    {
      Rectangle button = this.ChevronBounds;
      Color buttonFill = IsHighContrast() ? SystemColors.Window : (ThemeManager.IsDark ? ThemeManager.DarkWindow : Color.White);
      SmoothingMode saved = g.SmoothingMode;
      g.SmoothingMode = SmoothingMode.AntiAlias;

      using (SolidBrush brush = new SolidBrush(buttonFill))
        g.FillEllipse(brush, button.Left, button.Top, button.Width - 1, button.Height - 1);

      using (Pen outline = new Pen(this.BorderColor))
        g.DrawEllipse(outline, button.Left, button.Top, button.Width - 1, button.Height - 1);

      int centerX = button.Left + (button.Width / 2);
      int centerY = button.Top + (button.Height / 2);
      int tip = this.vbooExpanded ? centerY - (ARROW_HEIGHT / 2) : centerY + (ARROW_HEIGHT / 2);
      int baseLine = this.vbooExpanded ? centerY + (ARROW_HEIGHT / 2) : centerY - (ARROW_HEIGHT / 2);
      Point[] arrow =
      {
        new Point(centerX - (ARROW_WIDTH / 2), baseLine),
        new Point(centerX + (ARROW_WIDTH / 2), baseLine),
        new Point(centerX, tip)
      };

      using (SolidBrush arrowBrush = new SolidBrush(this.TextColor))
        g.FillPolygon(arrowBrush, arrow);

      g.SmoothingMode = saved;
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.objHeaderFont != null)
      {
        this.objHeaderFont.Dispose();
        this.objHeaderFont = null;
      }

      base.Dispose(disposing);
    }
  }
}
