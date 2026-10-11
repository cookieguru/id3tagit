using System.IO;
using System.Linq;
using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.WindowsAPI;
using TagLib;
using Xunit;

namespace ID3_TagIT.SmokeTests
{
  // Tag files the app didn't create, and files it can't write.
  public class TagFileTests
  {
    // Frames the app doesn't understand are listed as unsupported and written back unchanged.
    [Fact]
    public void UnsupportedFramesArePreserved()
    {
      using (PortableApp app = new PortableApp())
      {
        byte[] priv = Encoding.ASCII.GetBytes("smoke@owner\0").Concat(new byte[] { 1, 2, 3, 4, 5 }).ToArray();
        byte[] unknown = Encoding.ASCII.GetBytes("smoke frame data");
        string file = Mp3Files.WriteTagged(app.Music("unsupported.mp3"), 3,
          Mp3Files.Text("TIT2", "Old Title"), Mp3Files.Frame("PRIV", priv), Mp3Files.Frame("XSMK", unknown));

        app.Launch(1);
        app.SelectFile("unsupported");
        Window v2 = app.OpenDialog("frmTAG2", VirtualKeyShort.KEY_W, VirtualKeyShort.CONTROL);
        Ui.ShowPanel(v2, "Unsupported frames");
        string[] listed = Ui.ListItems(v2, "NotSupportList");
        Assert.Contains(listed, i => i.Contains("PRIV"));
        Assert.Contains(listed, i => i.Contains("XSMK"));

        Ui.ShowPanel(v2, "Main Entries");
        Ui.SetText(v2, "txtTitle", "New Title");
        app.CloseDialog(v2);
        app.SaveAll();
        app.Close();

        using (TagLib.File mp3 = TagLib.File.Create(file))
          Assert.Equal("New Title", mp3.Tag.Title);

        Assert.True(Mp3Files.Contains(file, Encoding.ASCII.GetBytes("PRIV")) && Mp3Files.Contains(file, priv), "PRIV frame lost");
        Assert.True(Mp3Files.Contains(file, Encoding.ASCII.GetBytes("XSMK")) && Mp3Files.Contains(file, unknown), "Unknown frame lost");
      }
    }

    // iTunes wrote ID3v2.2 tags (three-letter frame IDs); fixing their handling is a goal of this fork.
    [Fact(Skip = "The app doesn't read ID3v2.2 frames yet, and MP3.WriteChanges never writes a v2.2 tag")]
    public void ITunesV22TagsAreReadAndKeptWhenEditing()
    {
      using (PortableApp app = new PortableApp())
      {
        string file = Mp3Files.WriteTagged(app.Music("itunes.mp3"), 2,
          Mp3Files.Text("TT2", "iTunes Title"), Mp3Files.Text("TP1", "iTunes Artist"), Mp3Files.Text("TAL", "iTunes Album"),
          Mp3Files.Text("TYE", "2005"), Mp3Files.Text("TRK", "4/10"), Mp3Files.Comment("COM", "iTunes comment"));

        // The input itself must be a valid v2.2 tag.
        using (TagLib.File mp3 = TagLib.File.Create(file))
        {
          Assert.Equal(2, ((TagLib.Id3v2.Tag)mp3.GetTag(TagTypes.Id3v2)).Version);
          Assert.Equal("iTunes Title", mp3.Tag.Title);
        }

        app.Launch(1);
        app.SelectFile("itunes");
        Window v2 = app.OpenDialog("frmTAG2", VirtualKeyShort.KEY_W, VirtualKeyShort.CONTROL);
        Assert.Equal("iTunes Title", Ui.GetText(v2, "txtTitle"));
        Assert.Equal("iTunes Artist", Ui.GetText(v2, "cmbArtist"));
        Assert.Equal("iTunes Album", Ui.GetText(v2, "txtAlbum"));

        Ui.SetText(v2, "txtAlbum", "Changed Album");
        app.CloseDialog(v2);
        app.SaveAll();
        app.Close();

        using (TagLib.File mp3 = TagLib.File.Create(file))
        {
          Assert.Equal("iTunes Title", mp3.Tag.Title);
          Assert.Equal("iTunes Artist", mp3.Tag.Performers.Single());
          Assert.Equal("Changed Album", mp3.Tag.Album);
          Assert.Equal(2005u, mp3.Tag.Year);
          Assert.Equal(4u, mp3.Tag.Track);
          Assert.Equal("iTunes comment", mp3.Tag.Comment);
        }
      }
    }

    // A file that can't be written is listed in the error pane, which opens even if collapsed.
    [Fact]
    public void AReadOnlyFileIsReportedInTheErrorPane()
    {
      using (PortableApp app = new PortableApp())
      {
        string file = Mp3Files.WriteTagless(app.Music("locked.mp3"));
        app.CreateSettings();
        app.EditSetting("ErrorExpanded", false);
        System.IO.File.SetAttributes(file, FileAttributes.ReadOnly);

        app.Launch(1);
        // A collapsed pane takes its list out of the automation tree.
        AutomationElement errors = app.Main.FindFirstDescendant(cf => cf.ByAutomationId("ErrorMsg"));
        Assert.True(errors == null || errors.IsOffscreen, "the error pane should start collapsed");

        app.SelectFile("locked");
        Window v1 = app.OpenDialog("frmTAG1", VirtualKeyShort.KEY_E, VirtualKeyShort.CONTROL);
        Ui.SetText(v1, "txtTitle", "Locked Title");
        app.CloseDialog(v1);
        app.SaveAll();

        errors = app.Main.FindFirstDescendant(cf => cf.ByAutomationId("ErrorMsg"));
        Assert.True(errors != null && !errors.IsOffscreen, "the error pane should have opened");
        Assert.Contains(Ui.ListItems(app.Main, "ErrorMsg"), i => i.Contains("locked.mp3"));
        Assert.Equal(Mp3Files.Audio(), System.IO.File.ReadAllBytes(file));
      }
    }
  }
}
