using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace ID3_TagIT.Tests
{
  public class TaskPaneGroupTests
  {
    private const int GROUP_HEIGHT = 120;
    private const int BORDER_PIXELS = 2;

    private static readonly Color LightFill = Color.FromArgb(228, 235, 249);
    private static readonly Color LightBorder = Color.FromArgb(188, 202, 234);

    // A shown but off-screen form: a control only reports itself visible once its form is shown.
    private sealed class Host : Form
    {
      public readonly TaskPaneGroup Group = new TaskPaneGroup();
      public readonly LinkLabel Link = new LinkLabel();
      public readonly TextBox After = new TextBox();

      public Host()
      {
        this.StartPosition = FormStartPosition.Manual;
        this.Location = new Point(-20000, -20000);
        this.ShowInTaskbar = false;
        this.Size = new Size(300, 400);

        this.Group.Text = "Group";
        this.Group.Size = new Size(150, GROUP_HEIGHT);
        this.Link.Text = "Link";
        this.Link.TabStop = true;
        this.Link.Location = new Point(8, 8);
        this.Group.Body.Controls.Add(this.Link);
        this.After.Location = new Point(0, 200);
        this.After.TabIndex = 1;
        this.Controls.Add(this.After);
        this.Controls.Add(this.Group);
        this.Group.TabIndex = 0;
      }
    }

    private static void WithHost(Action<Host> test)
    {
      TestApp.EnsureInitialized();
      Sta.Run(delegate ()
      {
        using (Host host = new Host())
        {
          host.Show();

          try
          {
            test(host);
          }
          finally
          {
            TestApp.Settings.Theme = ThemeManager.THEME_LIGHT;
            ThemeManager.Refresh();
          }
        }
      });
    }

    private static void ClickAt(TaskPaneGroup group, int x, int y)
    {
      MethodInfo mouseUp = typeof(Control).GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic);
      mouseUp.Invoke(group, new object[] { new MouseEventArgs(MouseButtons.Left, 1, x, y, 0) });
    }

    [Fact]
    public void CollapsingShrinksTheGroupToItsHeaderAndRaisesTheEvent()
    {
      WithHost(delegate (Host host)
      {
        int raised = 0;
        host.Group.ExpandedChanged += delegate { raised++; };

        host.Group.Expanded = false;

        Assert.Equal(host.Group.HeaderHeight + BORDER_PIXELS, host.Group.Height);
        Assert.Equal(1, raised);
        Assert.False(host.Link.Visible);
      });
    }

    [Fact]
    public void ExpandingRestoresTheHeightItHad()
    {
      WithHost(delegate (Host host)
      {
        host.Group.Expanded = false;
        host.Group.Expanded = true;

        Assert.Equal(GROUP_HEIGHT, host.Group.Height);
        Assert.True(host.Link.Visible);
      });
    }

    [Fact]
    public void ClickingTheHeaderToggles()
    {
      WithHost(delegate (Host host)
      {
        ClickAt(host.Group, 10, host.Group.HeaderHeight / 2);
        Assert.False(host.Group.Expanded);

        ClickAt(host.Group, host.Group.ChevronBounds.X + 2, host.Group.ChevronBounds.Y + 2);
        Assert.True(host.Group.Expanded);

        ClickAt(host.Group, 10, host.Group.HeaderHeight + 20);
        Assert.True(host.Group.Expanded);
      });
    }

    [Fact]
    public void GroupsInAFlowPanelMoveUpWhenOneCollapses()
    {
      TestApp.EnsureInitialized();
      Sta.Run(delegate ()
      {
        using (Form form = new Form())
        {
          FlowLayoutPanel flow = new FlowLayoutPanel();
          flow.FlowDirection = FlowDirection.TopDown;
          flow.WrapContents = false;
          flow.Size = new Size(200, 400);
          TaskPaneGroup first = new TaskPaneGroup();
          TaskPaneGroup second = new TaskPaneGroup();
          first.Size = new Size(150, 100);
          second.Size = new Size(150, 100);
          first.Margin = new Padding(0);
          second.Margin = new Padding(0);
          flow.Controls.Add(first);
          flow.Controls.Add(second);
          form.Controls.Add(flow);
          form.Show();

          int before = second.Top;
          first.Expanded = false;
          flow.PerformLayout();

          Assert.Equal(first.Height, second.Top);
          Assert.True(second.Top < before);
        }
      });
    }

    [Fact]
    public void ColorsFollowTheThemeWhileExpanded()
    {
      AssertColorsFollowTheTheme(true);
    }

    [Fact]
    public void ColorsFollowTheThemeWhileCollapsed()
    {
      AssertColorsFollowTheTheme(false);
    }

    private static void AssertColorsFollowTheTheme(bool expanded)
    {
      WithHost(delegate (Host host)
      {
        host.Group.Expanded = expanded;

        TestApp.Settings.Theme = ThemeManager.THEME_DARK;
        ThemeManager.Refresh();
        Assert.Equal(ThemeManager.DarkTint.ToArgb(), host.Group.FillColor.ToArgb());
        Assert.Equal(ThemeManager.DarkTintBorder.ToArgb(), host.Group.BorderColor.ToArgb());

        TestApp.Settings.Theme = ThemeManager.THEME_LIGHT;
        ThemeManager.Refresh();
        Assert.Equal(LightFill.ToArgb(), host.Group.FillColor.ToArgb());
        Assert.Equal(LightBorder.ToArgb(), host.Group.BorderColor.ToArgb());
      });
    }

    [Fact]
    public void HighContrastUsesSystemColors()
    {
      WithHost(delegate (Host host)
      {
        Func<bool> original = TaskPaneGroup.IsHighContrast;
        TaskPaneGroup.IsHighContrast = delegate () { return true; };

        try
        {
          Assert.Equal(SystemColors.Window.ToArgb(), host.Group.FillColor.ToArgb());
          Assert.Equal(SystemColors.WindowText.ToArgb(), host.Group.BorderColor.ToArgb());
          Assert.Equal(SystemColors.WindowText.ToArgb(), host.Group.TextColor.ToArgb());
        }
        finally
        {
          TaskPaneGroup.IsHighContrast = original;
        }
      });
    }

    [Fact]
    public void CollapsingMovesFocusOffAHiddenLink()
    {
      WithHost(delegate (Host host)
      {
        host.ActiveControl = host.Link;

        host.Group.Expanded = false;

        Assert.NotSame(host.Link, host.ActiveControl);
        Assert.Same(host.After, host.ActiveControl);
      });
    }

    [Fact]
    public void ExpandingKeepsLinksTheFormHid()
    {
      WithHost(delegate (Host host)
      {
        host.Link.Visible = false;

        host.Group.Expanded = false;
        host.Group.Expanded = true;

        Assert.False(host.Link.Visible);
      });
    }

    [Fact]
    public void HeaderHeightComesFromTheFont()
    {
      TestApp.EnsureInitialized();
      Sta.Run(delegate ()
      {
        using (TaskPaneGroup normal = new TaskPaneGroup())
        using (TaskPaneGroup large = new TaskPaneGroup())
        using (Font big = new Font("Segoe UI", 20f))
        {
          large.Font = big;

          Assert.True(large.HeaderHeight > normal.HeaderHeight);
        }
      });
    }
  }
}
