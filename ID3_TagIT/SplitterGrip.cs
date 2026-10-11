using System;
using System.Drawing;
using System.Windows.Forms;

namespace ID3_TagIT
{
  // Restores the collapse/expand grip that the original DotNetBar ExpandableSplitter drew in
  // the middle of the splitter bar. Clicking the splitter collapses one panel of a
  // SplitContainer and clicking it again restores the previous size; dragging the splitter
  // works as before.
  //
  // Collapsing moves the splitter to the edge instead of using Panel1Collapsed/Panel2Collapsed,
  // because those hide the splitter bar too and leave nothing to click to bring the panel back.
  internal sealed class SplitterGrip
  {
    private const int ARROW_LENGTH = 7;
    private const int ARROW_DEPTH = 4;
    private const int ARROW_GAP = 7;
    private const int DOT_COUNT = 9;
    private const int DOT_SPACING = 4;
    private const int GRIP_LENGTH = (2 * ARROW_LENGTH) + (2 * ARROW_GAP) + (DOT_COUNT * DOT_SPACING) - 1;

    private readonly SplitContainer objSplit;
    private readonly bool vbooCollapsePanel1;
    private bool vbooCollapsed;
    private bool vbooAdjusting;
    private bool vbooSplitterPressed;
    private bool vbooDragged;
    private Point objMouseDown;
    private int vintRestoreSize;

    public SplitterGrip(SplitContainer split, FixedPanel collapsiblePanel)
    {
      this.objSplit = split;
      this.vbooCollapsePanel1 = (collapsiblePanel == FixedPanel.Panel1);
      this.vintRestoreSize = this.PanelSize;

      if (this.vbooCollapsePanel1)
        split.Panel1MinSize = 0;
      else
        split.Panel2MinSize = 0;

      // Keep the collapsible panel's size when the container resizes (the window, or a
      // neighboring pane collapsing), as the DotNetBar side panels did, instead of scaling it.
      split.FixedPanel = collapsiblePanel;

      // Wide enough for the grip, matching the original 5 pixel splitter.
      if (split.SplitterWidth < ARROW_DEPTH + 1)
        split.SplitterWidth = ARROW_DEPTH + 1;

      split.Paint += new PaintEventHandler(this.Split_Paint);
      split.MouseDown += new MouseEventHandler(this.Split_MouseDown);
      split.MouseUp += new MouseEventHandler(this.Split_MouseUp);
      split.SplitterMoving += new SplitterCancelEventHandler(this.Split_SplitterMoving);
      split.SplitterMoved += new SplitterEventHandler(this.Split_SplitterMoved);
      split.SizeChanged += new EventHandler(this.Split_SizeChanged);
    }

    public bool Expanded
    {
      get
      {
        return !this.vbooCollapsed;
      }
      set
      {
        if (value == !this.vbooCollapsed)
          return;

        if (value)
        {
          this.vbooCollapsed = false;
          this.SetPanelSize(Math.Max(this.vintRestoreSize, 1));
        }
        else
        {
          if (this.PanelSize > 0)
            this.vintRestoreSize = this.PanelSize;

          this.vbooCollapsed = true;
          this.SetPanelSize(0);
        }

        this.objSplit.Invalidate();
      }
    }

    private bool IsVertical
    {
      get
      {
        return this.objSplit.Orientation == Orientation.Vertical;
      }
    }

    private int TotalSize
    {
      get
      {
        return this.IsVertical ? this.objSplit.Width : this.objSplit.Height;
      }
    }

    // Size of the panel that collapses.
    private int PanelSize
    {
      get
      {
        if (this.vbooCollapsePanel1)
          return this.objSplit.SplitterDistance;

        return this.TotalSize - this.objSplit.SplitterDistance - this.objSplit.SplitterWidth;
      }
    }

    private Rectangle GripBounds
    {
      get
      {
        Rectangle splitter = this.objSplit.SplitterRectangle;

        if (this.IsVertical)
          return new Rectangle(splitter.Left, splitter.Top + ((splitter.Height - GRIP_LENGTH) / 2), splitter.Width, GRIP_LENGTH);

        return new Rectangle(splitter.Left + ((splitter.Width - GRIP_LENGTH) / 2), splitter.Top, GRIP_LENGTH, splitter.Height);
      }
    }

    private void SetPanelSize(int size)
    {
      int maximum = this.TotalSize - this.objSplit.SplitterWidth;

      if (maximum <= 0)
        return;

      size = Math.Max(0, Math.Min(size, maximum));
      this.vbooAdjusting = true;

      try
      {
        this.objSplit.SplitterDistance = this.vbooCollapsePanel1 ? size : maximum - size;
      }
      finally
      {
        this.vbooAdjusting = false;
      }
    }

    // SplitterMoving is raised only while the user drags the splitter, never when the container
    // rescales on resize, so it tells a drag apart from a resize in SplitterMoved.
    private void Split_SplitterMoving(object sender, SplitterCancelEventArgs e)
    {
      if (!this.vbooAdjusting)
        this.vbooDragged = true;
    }

    private void Split_SplitterMoved(object sender, SplitterEventArgs e)
    {
      // After a drag the panel is collapsed only if it was dragged fully shut.
      if (!this.vbooAdjusting && this.vbooDragged)
      {
        this.vbooDragged = false;
        this.vbooCollapsed = (this.PanelSize == 0);
      }

      this.objSplit.Invalidate();
    }

    private void Split_SizeChanged(object sender, EventArgs e)
    {
      // SplitContainer rescales the splitter on resize; keep a collapsed panel fully closed.
      if (this.vbooCollapsed && this.PanelSize != 0)
        this.SetPanelSize(0);
    }

    private void Split_MouseDown(object sender, MouseEventArgs e)
    {
      this.vbooSplitterPressed = (e.Button == MouseButtons.Left) && this.objSplit.SplitterRectangle.Contains(e.Location);
      this.objMouseDown = e.Location;
    }

    // A click anywhere on the splitter toggles the panel, as with the original splitters; a
    // drag resizes it instead.
    private void Split_MouseUp(object sender, MouseEventArgs e)
    {
      if (!this.vbooSplitterPressed)
        return;

      this.vbooSplitterPressed = false;
      Size drag = SystemInformation.DragSize;

      if (Math.Abs(e.X - this.objMouseDown.X) > drag.Width || Math.Abs(e.Y - this.objMouseDown.Y) > drag.Height)
        return;

      // SplitContainer finishes its own splitter drag after raising MouseUp, which would
      // overwrite a distance set here; toggle once it is done.
      this.objSplit.BeginInvoke(new MethodInvoker(this.Toggle));
    }

    private void Toggle()
    {
      this.Expanded = !this.Expanded;
    }

    private void Split_Paint(object sender, PaintEventArgs e)
    {
      Rectangle grip = this.GripBounds;
      bool dark = ThemeManager.IsDark;
      Color dot = dark ? Color.FromArgb(150, 150, 150) : Color.FromArgb(120, 120, 120);
      Color dotHighlight = dark ? Color.FromArgb(20, 20, 20) : Color.White;
      Color arrowOutline = dark ? Color.FromArgb(200, 200, 200) : Color.FromArgb(64, 64, 64);
      Color arrowFill = dark ? Color.FromArgb(110, 110, 110) : Color.FromArgb(160, 160, 160);

      // Arrows point the way the splitter will move: toward the collapsible panel's edge while
      // it is expanded, away from it while collapsed.
      bool towardStart = this.vbooCollapsePanel1 != this.vbooCollapsed;
      int across = this.IsVertical ? grip.Left + ((grip.Width - ARROW_DEPTH) / 2) : grip.Top + ((grip.Height - ARROW_DEPTH) / 2);
      int along = this.IsVertical ? grip.Top : grip.Left;

      this.DrawArrow(e.Graphics, across, along, towardStart, arrowOutline, arrowFill);
      this.DrawArrow(e.Graphics, across, along + GRIP_LENGTH - ARROW_LENGTH, towardStart, arrowOutline, arrowFill);

      using (SolidBrush dotBrush = new SolidBrush(dot))
      using (SolidBrush highlightBrush = new SolidBrush(dotHighlight))
      {
        int dotAcross = across + ((ARROW_DEPTH - 2) / 2);

        for (int i = 0; i < DOT_COUNT; i++)
        {
          int position = along + ARROW_LENGTH + ARROW_GAP + (i * DOT_SPACING);
          Point point = this.IsVertical ? new Point(dotAcross, position) : new Point(position, dotAcross);

          e.Graphics.FillRectangle(highlightBrush, point.X + 1, point.Y + 1, 2, 2);
          e.Graphics.FillRectangle(dotBrush, point.X, point.Y, 2, 2);
        }
      }
    }

    // Draws a small outlined triangle ARROW_DEPTH deep and ARROW_LENGTH long. "across" and
    // "along" are relative to the splitter's orientation.
    private void DrawArrow(Graphics g, int across, int along, bool towardStart, Color outline, Color fill)
    {
      int tip = towardStart ? across : across + ARROW_DEPTH - 1;
      int baseLine = towardStart ? across + ARROW_DEPTH - 1 : across;
      int middle = along + (ARROW_LENGTH / 2);
      Point[] points;

      if (this.IsVertical)
        points = new Point[] { new Point(baseLine, along), new Point(tip, middle), new Point(baseLine, along + ARROW_LENGTH - 1) };
      else
        points = new Point[] { new Point(along, baseLine), new Point(middle, tip), new Point(along + ARROW_LENGTH - 1, baseLine) };

      using (SolidBrush brush = new SolidBrush(fill))
        g.FillPolygon(brush, points);

      using (Pen pen = new Pen(outline))
        g.DrawPolygon(pen, points);
    }
  }
}
