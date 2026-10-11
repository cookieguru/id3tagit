using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace ID3_TagIT.Tests
{
  // Drives a real SplitContainer with mouse messages posted to it (the real cursor isn't used).
  public class SplitterGripTests
  {
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int WM_MOUSEMOVE = 0x200;
    private const int WM_LBUTTONDOWN = 0x201;
    private const int WM_LBUTTONUP = 0x202;

    private sealed class Harness : IDisposable
    {
      public readonly Form Form = new Form { Width = 800, Height = 400, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
      public readonly SplitContainer Split = new SplitContainer { Dock = DockStyle.Fill };
      public readonly SplitterGrip Grip;
      private readonly bool vbooPanel1;

      public Harness(bool collapsePanel1)
      {
        this.vbooPanel1 = collapsePanel1;
        this.Form.Controls.Add(this.Split);
        this.Form.Show();
        this.Split.SplitterDistance = collapsePanel1 ? 200 : this.Split.Width - this.Split.SplitterWidth - 200;
        this.Grip = new SplitterGrip(this.Split, collapsePanel1 ? FixedPanel.Panel1 : FixedPanel.Panel2);
        Pump();
      }

      public int PanelSize
      {
        get { return this.vbooPanel1 ? this.Split.SplitterDistance : this.Split.Width - this.Split.SplitterDistance - this.Split.SplitterWidth; }
      }

      private int SplitterX
      {
        get { return this.Split.SplitterRectangle.Left + (this.Split.SplitterWidth / 2); }
      }

      // y = 20 is well away from the arrows, which are centred vertically.
      public void Click(int y = 20)
      {
        PostMessage(this.Split.Handle, WM_LBUTTONDOWN, (IntPtr)1, Point(this.SplitterX, y));
        PostMessage(this.Split.Handle, WM_LBUTTONUP, IntPtr.Zero, Point(this.SplitterX, y));
        Pump();
      }

      // Positive dx opens the collapsible panel.
      public void Drag(int dx)
      {
        int x = this.SplitterX;
        int direction = this.vbooPanel1 ? 1 : -1;
        PostMessage(this.Split.Handle, WM_LBUTTONDOWN, (IntPtr)1, Point(x, 20));

        for (int i = 1; i <= 10; i++)
          PostMessage(this.Split.Handle, WM_MOUSEMOVE, (IntPtr)1, Point(x + (direction * dx * i / 10), 20));

        PostMessage(this.Split.Handle, WM_LBUTTONUP, IntPtr.Zero, Point(x + (direction * dx), 20));
        Pump();
      }

      public void Resize(int dx)
      {
        this.Form.Width += dx;
        Pump();
      }

      public void Dispose()
      {
        this.Form.Dispose();
      }

      private static IntPtr Point(int x, int y)
      {
        return (IntPtr)(((y & 0xFFFF) << 16) | (x & 0xFFFF));
      }

      private static void Pump()
      {
        DateTime end = DateTime.Now.AddMilliseconds(150);

        while (DateTime.Now < end)
        {
          Application.DoEvents();
          Thread.Sleep(5);
        }
      }
    }

    private static void Test(Action<Harness> test)
    {
      foreach (bool panel1 in new[] { true, false })
        Sta.Run(delegate ()
        {
          using (Harness harness = new Harness(panel1))
            test(harness);
        });
    }

    [Fact]
    public void ClickingAnywhereOnTheSplitterTogglesThePanel()
    {
      Test(delegate (Harness h)
      {
        h.Click();
        Assert.False(h.Grip.Expanded);
        Assert.Equal(0, h.PanelSize);

        h.Click();
        Assert.True(h.Grip.Expanded);
        Assert.Equal(200, h.PanelSize);
      });
    }

    [Fact]
    public void DraggingACollapsedPanelOpenExpandsIt()
    {
      Test(delegate (Harness h)
      {
        h.Click();
        h.Drag(150);
        int dragged = h.PanelSize;

        Assert.True(h.Grip.Expanded);
        Assert.InRange(dragged, 100, 199);

        h.Click();
        Assert.Equal(0, h.PanelSize);
        h.Click();
        Assert.Equal(dragged, h.PanelSize);
      });
    }

    [Fact]
    public void ResizingTheWindowKeepsTheState()
    {
      Test(delegate (Harness h)
      {
        h.Resize(100);
        Assert.Equal(200, h.PanelSize);

        h.Click();
        h.Resize(-150);
        Assert.False(h.Grip.Expanded);
        Assert.Equal(0, h.PanelSize);
      });
    }
  }
}
