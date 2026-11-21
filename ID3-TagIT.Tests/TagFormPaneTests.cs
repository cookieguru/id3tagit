using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace ID3_TagIT.Tests
{
  // The left pane of the tag editors: collapsible task pane groups holding icon links.
  public class TagFormPaneTests
  {
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly string[] TagLinks =
    {
      "lblMainEntries", "lblDetailedInfo", "lblOriginalInfo", "lblInvolvedPeople", "lblWebInfo",
      "lblPictures", "lblLyricsPane", "lblRating", "lblUserDefined", "lblUnsupported"
    };

    private static readonly string[] SwapLinks = { "lblSwapArtTtl", "lblSwapArtAlb", "lblSwapTtlAlb" };
    private static readonly string[] CommandLinks = { "lblPrevious", "lblNext" };
    private static readonly string[] TransferLinks = { "lblGetFromFile", "lblXferFromV1", "lblXferToV1" };

    private static T Get<T>(Form form, string field)
    {
      return (T)form.GetType().GetField(field, Members).GetValue(form);
    }

    private static bool HasLinkHandler(LinkLabel link)
    {
      PropertyInfo events = typeof(Component).GetProperty("Events", Members);
      object key = typeof(LinkLabel).GetField("EventLinkClicked", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

      return ((EventHandlerList)events.GetValue(link, null))[key] != null;
    }

    private static void WithTag2Form(Action<frmTAG2> test)
    {
      Sta.Run(delegate ()
      {
        using (MainForm main = new MainForm())
        {
          frmMain mainForm = main.Form;

          using (frmTAG2 form = new frmTAG2(ref mainForm))
            test(form);
        }
      });
    }

    [Fact]
    public void Tag2PaneGroupsAreTaskPaneGroups()
    {
      WithTag2Form(delegate (frmTAG2 form)
      {
        TaskPaneGroup info = Get<TaskPaneGroup>(form, "pnlTagInfo");
        TaskPaneGroup transfer = Get<TaskPaneGroup>(form, "pnlXferImport");
        TaskPaneGroup commands = Get<TaskPaneGroup>(form, "pnlCommands");

        Assert.Equal("TAG Information", info.Text);
        Assert.Equal("Transfer and Import", transfer.Text);
        Assert.Equal("Commands", commands.Text);
        Assert.True(info.Expanded);
        Assert.False(transfer.Expanded);
        Assert.True(commands.Expanded);
        Assert.IsType<FlowLayoutPanel>(Get<Control>(form, "pnlLeft"));
      });
    }

    [Fact]
    public void Tag2EveryLinkKeepsItsHandler()
    {
      WithTag2Form(delegate (frmTAG2 form)
      {
        Control pane = Get<Control>(form, "pnlLeft");

        foreach (string name in TagLinks.Concat2(SwapLinks, CommandLinks, TransferLinks))
        {
          Control[] found = pane.Controls.Find(name, true);

          Assert.True(found.Length == 1, name + " found " + found.Length + " times in the pane");
          Assert.True(HasLinkHandler((LinkLabel)found[0]), name + " has no LinkClicked handler");
        }
      });
    }

    [Fact]
    public void Tag2LinksShowTheirIcons()
    {
      WithTag2Form(delegate (frmTAG2 form)
      {
        foreach (string name in TagLinks.Concat2(CommandLinks))
          Assert.NotNull(Get<LinkLabel>(form, name).Image);

        foreach (string name in SwapLinks)
          Assert.Null(Get<LinkLabel>(form, name).Image);
      });
    }

    private static void WithTag2MultiForm(Action<frmTAG2Multi> test)
    {
      Sta.Run(delegate ()
      {
        using (MainForm main = new MainForm())
        {
          frmMain mainForm = main.Form;

          using (frmTAG2Multi form = new frmTAG2Multi(ref mainForm))
            test(form);
        }
      });
    }

    private static void WithTag1MultiForm(Action<frmTAG1Multi> test)
    {
      Sta.Run(delegate ()
      {
        using (MainForm main = new MainForm())
        {
          frmMain mainForm = main.Form;

          using (frmTAG1Multi form = new frmTAG1Multi(ref mainForm))
            test(form);
        }
      });
    }

    private static void AssertLinksKeepHandlers(Control pane, params string[][] names)
    {
      foreach (string name in new string[0].Concat2(names))
      {
        Control[] found = pane.Controls.Find(name, true);

        Assert.True(found.Length == 1, name + " found " + found.Length + " times in the pane");
        Assert.True(HasLinkHandler((LinkLabel)found[0]), name + " has no LinkClicked handler");
      }
    }

    [Fact]
    public void Tag2MultiPaneHoldsTwoTaskPaneGroups()
    {
      string[] multiTagLinks = TagLinks.Where(name => name != "lblUnsupported").ToArray();

      WithTag2MultiForm(delegate (frmTAG2Multi form)
      {
        Control pane = Get<Control>(form, "pnlLeft");

        Assert.IsType<FlowLayoutPanel>(pane);
        Assert.Equal(168, pane.Width);
        Assert.Equal("TAG Information", Get<TaskPaneGroup>(form, "pnlTagInfo").Text);
        Assert.Equal("Commands", Get<TaskPaneGroup>(form, "pnlCommands").Text);
        Assert.True(Get<TaskPaneGroup>(form, "pnlTagInfo").Expanded);
        Assert.True(Get<TaskPaneGroup>(form, "pnlCommands").Expanded);
        AssertLinksKeepHandlers(pane, multiTagLinks, SwapLinks);

        foreach (string name in multiTagLinks)
          Assert.NotNull(Get<LinkLabel>(form, name).Image);

        foreach (string name in SwapLinks)
          Assert.Null(Get<LinkLabel>(form, name).Image);
      });
    }

    [Fact]
    public void Tag2MultiLeavesTheRightSideAlone()
    {
      WithTag2MultiForm(delegate (frmTAG2Multi form)
      {
        Panel detail = Get<Panel>(form, "panDetail");

        Assert.Equal(new Point(168, 24), detail.Location);
        Assert.Equal(new Size(688, 456), detail.Size);
      });
    }

    [Fact]
    public void Tag1MultiCommandsIsATaskPaneGroup()
    {
      WithTag1MultiForm(delegate (frmTAG1Multi form)
      {
        TaskPaneGroup commands = Get<TaskPaneGroup>(form, "pnlCommands");

        Assert.Equal("Commands", commands.Text);
        Assert.True(commands.Expanded);
        AssertLinksKeepHandlers(commands, SwapLinks);

        foreach (string name in SwapLinks)
          Assert.Null(Get<LinkLabel>(form, name).Image);

        Assert.Equal(new Point(160, 128), Get<Label>(form, "lblGenre").Location);
        Assert.True(commands.Right <= 160, "the group reaches into the fields on the right");
      });
    }

    [Fact]
    public void Tag2PaneKeepsItsWidthAndLeavesTheRightSideAlone()
    {
      WithTag2Form(delegate (frmTAG2 form)
      {
        Assert.Equal(168, Get<Control>(form, "pnlLeft").Width);

        Panel main = Get<Panel>(form, "panMain");
        Assert.Equal(new Point(168, 48), main.Location);
        Assert.Equal(new Size(664, 456), main.Size);
      });
    }
  }

  internal static class StringArrayExtensions
  {
    internal static string[] Concat2(this string[] first, params string[][] others)
    {
      System.Collections.Generic.List<string> all = new System.Collections.Generic.List<string>(first);

      foreach (string[] other in others)
        all.AddRange(other);

      return all.ToArray();
    }
  }
}
