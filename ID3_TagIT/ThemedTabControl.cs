using System.Drawing;
using System.Windows.Forms;

namespace ID3_TagIT
{
  // A TabControl that paints its own tab strip and page border in dark mode, because Windows
  // has no dark tab theme.  In light mode it is the ordinary themed TabControl.
  internal class ThemedTabControl : TabControl
  {
    private bool vbooDark;

    public bool DarkPaint
    {
      get
      {
        return this.vbooDark;
      }

      set
      {
        if (this.vbooDark == value)
          return;

        this.vbooDark = value;
        this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value);
        this.UpdateStyles();
        this.Invalidate(true);
      }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      if (!this.vbooDark)
      {
        base.OnPaint(e);
        return;
      }

      Graphics g = e.Graphics;
      Color parentBack = (this.Parent != null) ? this.Parent.BackColor : ThemeManager.DarkSurface;
      Color pageBack = (this.SelectedTab != null) ? this.SelectedTab.BackColor : ThemeManager.DarkSurface;

      g.Clear(parentBack);

      Rectangle frame = Rectangle.Inflate(this.DisplayRectangle, 2, 2);

      using (SolidBrush pageBrush = new SolidBrush(pageBack))
      using (SolidBrush tabBrush = new SolidBrush(ThemeManager.DarkRaised))
      using (Pen border = new Pen(ThemeManager.DarkBorder))
      {
        g.FillRectangle(pageBrush, frame);
        g.DrawRectangle(border, frame.X, frame.Y, frame.Width - 1, frame.Height - 1);

        for (int i = 0; i < this.TabCount; i++)
        {
          Rectangle tab = this.GetTabRect(i);
          bool selected = (i == this.SelectedIndex);

          if (selected)
            tab = Rectangle.FromLTRB(tab.Left - 2, tab.Top - 2, tab.Right + 2, frame.Top + 1);

          g.FillRectangle(selected ? pageBrush : tabBrush, tab);
          g.DrawLine(border, tab.Left, tab.Bottom - 1, tab.Left, tab.Top);
          g.DrawLine(border, tab.Left, tab.Top, tab.Right - 1, tab.Top);
          g.DrawLine(border, tab.Right - 1, tab.Top, tab.Right - 1, tab.Bottom - 1);

          if (!selected)
            g.DrawLine(border, tab.Left, tab.Bottom - 1, tab.Right - 1, tab.Bottom - 1);

          TabPage tabPage = this.TabPages[i];
          Rectangle textBounds = tab;

          if (this.ImageList != null && tabPage.ImageIndex >= 0 && tabPage.ImageIndex < this.ImageList.Images.Count)
          {
            Image image = this.ImageList.Images[tabPage.ImageIndex];
            g.DrawImage(image, tab.Left + 4, tab.Top + ((tab.Height - image.Height) / 2));
            textBounds = Rectangle.FromLTRB(tab.Left + image.Width + 6, tab.Top, tab.Right, tab.Bottom);
          }

          TextRenderer.DrawText(g, tabPage.Text, this.Font, textBounds, ThemeManager.DarkText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }
      }
    }
  }
}
