using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.WindowsAPI;
using TagLib;
using TagLib.Id3v2;
using Xunit;

namespace ID3_TagIT.SmokeTests
{
  // Fills every field the tag editors offer on a tagless file, saves, and reads everything back
  // through the editors and through TagLib#, an independent reader.
  public class TagRoundTripTests
  {
    // v2 editor text fields: control ID, value, the ID3v2.4 frame it is saved to, and the editor
    // panel it is on (fields on hidden panels are disabled).
    private static readonly string[,] V2Fields =
    {
      { "txtTitle", "Smoke Title", "TIT2", "Main Entries" },
      { "cmbArtist", "Smoke Artist", "TPE1", "Main Entries" },
      { "txtAlbum", "Smoke Album", "TALB", "Main Entries" },
      { "txtYear", "2001", "TDRC", "Main Entries" },
      { "txtBPM", "120", "TBPM", "Main Entries" },
      { "txtComposer", "Smoke Composer", "TCOM", "Detailed Information" },
      { "txtBand", "Smoke Band", "TPE2", "Detailed Information" },
      { "txtModified", "Smoke Remixer", "TPE4", "Detailed Information" },
      { "txtConductor", "Smoke Conductor", "TPE3", "Detailed Information" },
      { "txtCopyright", "2001 Smoke Records", "TCOP", "Detailed Information" },
      { "txtLyWriter", "Smoke Lyricist", "TEXT", "Detailed Information" },
      { "txtEncoded", "Smoke Encoder", "TENC", "Detailed Information" },
      { "txtPublisher", "Smoke Publisher", "TPUB", "Detailed Information" },
      { "txtContent", "Smoke Group", "TIT1", "Detailed Information" },
      { "txtSubTitle", "Smoke Subtitle", "TIT3", "Detailed Information" },
      { "txtSortArtist", "Artist, Smoke", "TSOP", "Detailed Information" },
      { "txtSortTitle", "Title, Smoke", "TSOT", "Detailed Information" },
      { "txtSortAlbum", "Album, Smoke", "TSOA", "Detailed Information" },
      { "txtTLEN", "123456", "TLEN", "Detailed Information" },
      { "cmbMedia", "DIG", "TMED", "Detailed Information" },
      { "txtTORY", "1999", "TDOR", "Original Information" },
      { "txtOOwner", "Smoke Owner", "TOWN", "Original Information" },
      { "txtOArtist", "Smoke Original Artist", "TOPE", "Original Information" },
      { "txtOAlbum", "Smoke Original Album", "TOAL", "Original Information" },
      { "txtOLyWriter", "Smoke Original Lyricist", "TOLY", "Original Information" },
      { "txtOFilename", "original.mp3", "TOFN", "Original Information" },
      { "txtCOMMInfURL", "http://smoke.example/commercial", "WCOM", "Web Information" },
      { "txtPubURL", "http://smoke.example/publisher", "WPUB", "Web Information" },
      { "txtPayURL", "http://smoke.example/payment", "WPAY", "Web Information" },
      { "txtINetRadioURL", "http://smoke.example/radio", "WORS", "Web Information" },
      { "txtAudioSRCURL", "http://smoke.example/source", "WOAS", "Web Information" },
      { "txtCopyInfURL", "http://smoke.example/copyright", "WCOP", "Web Information" },
      { "txtArtistURL", "http://smoke.example/artist", "WOAR", "Web Information" },
      { "txtAudioFileURL", "http://smoke.example/file", "WOAF", "Web Information" }
    };

    private static readonly string[,] V1Fields =
    {
      { "txtTitle", "V1 Title" },
      { "cmbArtist", "V1 Artist" },
      { "txtAlbum", "V1 Album" },
      { "txtYear", "1998" },
      { "txtComment", "V1 Comment" },
      { "txtTrack", "7" },
      { "cmbGenre", "Rock" }
    };

    [Fact]
    public void EveryEditorFieldIsSavedAndReadBack()
    {
      using (PortableApp app = new PortableApp())
      {
        string file = Mp3Files.WriteTagless(app.Music("tagless.mp3"));
        string cover = app.Music("cover.png");

        using (Bitmap bitmap = new Bitmap(8, 8))
        {
          bitmap.SetPixel(1, 1, Color.Red);
          bitmap.Save(cover, ImageFormat.Png);
        }

        // ID3v2.4, so the fields marked "Ver. 2.4 only" are written too.
        app.CreateSettings();
        app.EditSetting("NewV2Version", (byte)4);
        app.Launch(1);
        app.SelectFile("tagless");

        Window v1 = app.OpenDialog("frmTAG1", VirtualKeyShort.KEY_E, VirtualKeyShort.CONTROL);
        for (int i = 0; i < V1Fields.GetLength(0); i++)
          Ui.SetText(v1, V1Fields[i, 0], V1Fields[i, 1]);
        app.CloseDialog(v1);

        Window v2 = app.OpenDialog("frmTAG2", VirtualKeyShort.KEY_W, VirtualKeyShort.CONTROL);
        FillV2(v2, cover);
        app.CloseDialog(v2);
        app.SaveAll();

        // Back through the editors.
        v1 = app.OpenDialog("frmTAG1", VirtualKeyShort.KEY_E, VirtualKeyShort.CONTROL);
        for (int i = 0; i < V1Fields.GetLength(0); i++)
          Assert.Equal(V1Fields[i, 1], Ui.GetText(v1, V1Fields[i, 0]));
        app.CloseDialog(v1, "btnCancel");

        v2 = app.OpenDialog("frmTAG2", VirtualKeyShort.KEY_W, VirtualKeyShort.CONTROL);
        for (int i = 0; i < V2Fields.GetLength(0); i++)
        {
          Ui.ShowPanel(v2, V2Fields[i, 3]);
          Assert.True(V2Fields[i, 1] == Ui.GetText(v2, V2Fields[i, 0]), V2Fields[i, 0] + ": expected " + V2Fields[i, 1] + ", got " + Ui.GetText(v2, V2Fields[i, 0]));
        }
        Ui.ShowPanel(v2, "Main Entries");
        // Numbers may be padded to the "Number of digits" setting, so compare them as numbers.
        Assert.Equal(3, int.Parse(Ui.GetText(v2, "txtTrack1")));
        Assert.Equal(12, int.Parse(Ui.GetText(v2, "txtTrack2")));
        Assert.Equal(1, int.Parse(Ui.GetText(v2, "txtPOS1")));
        Assert.Equal(2, int.Parse(Ui.GetText(v2, "txtPOS2")));
        string[,] lists =
        {
          { "GenreList", "Main Entries" }, { "CommentList", "Main Entries" }, { "LyricsList", "Lyrics" }, { "PicList", "Pictures" },
          { "RatingList", "Rating" }, { "TXXXList", "User defined information" }, { "WXXXList", "User defined information" },
          { "TIPLList", "Involved People" }, { "TMCLList", "Involved People" }
        };
        for (int i = 0; i < lists.GetLength(0); i++)
        {
          Ui.ShowPanel(v2, lists[i, 1]);
          Assert.True(Ui.ListItems(v2, lists[i, 0]).Length == 1, lists[i, 0] + " should hold one entry");
        }
        app.CloseDialog(v2, "btnCancel");
        app.Close();

        // Through TagLib#.
        using (TagLib.File mp3 = TagLib.File.Create(file))
        {
          TagLib.Id3v1.Tag tag1 = (TagLib.Id3v1.Tag)mp3.GetTag(TagTypes.Id3v1, false);
          Assert.NotNull(tag1);
          Assert.Equal("V1 Title", tag1.Title);
          Assert.Equal("V1 Artist", tag1.Performers.Single());
          Assert.Equal("V1 Album", tag1.Album);
          Assert.Equal(1998u, tag1.Year);
          Assert.Equal("V1 Comment", tag1.Comment);
          Assert.Equal(7u, tag1.Track);
          Assert.Equal("Rock", tag1.Genres.Single());

          TagLib.Id3v2.Tag tag2 = (TagLib.Id3v2.Tag)mp3.GetTag(TagTypes.Id3v2, false);
          Assert.NotNull(tag2);
          Assert.Equal(4, tag2.Version);

          for (int i = 0; i < V2Fields.GetLength(0); i++)
            Assert.True(V2Fields[i, 1] == FrameText(tag2, V2Fields[i, 2]), V2Fields[i, 2] + ": expected " + V2Fields[i, 1] + ", got " + FrameText(tag2, V2Fields[i, 2]));

          Assert.Equal(new[] { 3, 12 }, FrameText(tag2, "TRCK").Split('/').Select(int.Parse));
          Assert.Equal(new[] { 1, 2 }, FrameText(tag2, "TPOS").Split('/').Select(int.Parse));
          Assert.Equal("Rock", tag2.Genres.Single());
          Assert.Equal("Smoke comment", CommentsFrame.Get(tag2, "SmokeDesc", "eng", false).Text);
          Assert.Equal("Smoke lyrics", tag2.GetFrames<UnsynchronisedLyricsFrame>().Single().Text.Trim());
          Assert.Equal("SmokeValue", UserTextInformationFrame.Get(tag2, "SmokeKey", false).Text.Single());
          // TagLib# 2.3 reads this valid WXXX frame as empty, so check its bytes: ISO-8859-1,
          // description, terminator, URL.
          Assert.True(Mp3Files.Contains(file, System.Text.Encoding.ASCII.GetBytes("WXXX\0\0\0\x25\0\0\0SmokeLink\0http://smoke.example/link")), "WXXX frame missing or malformed");
          Assert.Equal(new[] { "producer", "Smoke Producer" }, TextFrame(tag2, "TIPL").Text);
          Assert.Equal(new[] { "guitar", "Smoke Guitarist" }, TextFrame(tag2, "TMCL").Text);

          PopularimeterFrame rating = PopularimeterFrame.Get(tag2, "smoke@test", false);
          Assert.Equal(200, rating.Rating);
          Assert.Equal(7ul, rating.PlayCount);

          AttachmentFrame picture = tag2.GetFrames<AttachmentFrame>().Single();
          Assert.Equal("Smoke cover", picture.Description);
          Assert.Equal(System.IO.File.ReadAllBytes(cover), picture.Data.Data);
        }
      }
    }

    private static void FillV2(Window v2, string cover)
    {
      for (int i = 0; i < V2Fields.GetLength(0); i++)
      {
        Ui.ShowPanel(v2, V2Fields[i, 3]);
        Ui.SetText(v2, V2Fields[i, 0], V2Fields[i, 1]);
      }

      Ui.ShowPanel(v2, "Main Entries");
      Ui.SetText(v2, "txtTrack1", "3");
      Ui.SetText(v2, "txtTrack2", "12");
      Ui.SetText(v2, "txtPOS1", "1");
      Ui.SetText(v2, "txtPOS2", "2");

      Ui.SetText(v2, "cmbGenre", "Rock");
      Ui.Click(v2, "btnAddGenre");
      Ui.SetText(v2, "cmbCDescriptor", "SmokeDesc");
      Ui.SetText(v2, "txtComment", "Smoke comment");
      Ui.Click(v2, "btnAddComment");

      Ui.ShowPanel(v2, "Lyrics");
      Ui.SetText(v2, "txtLDescriptor", "SmokeLyrics");
      Ui.SetText(v2, "txtLyrics", "Smoke lyrics");
      Ui.Click(v2, "btnAddLyrics");

      Ui.ShowPanel(v2, "Pictures");
      Ui.Press(v2, "btnGetPic");
      Ui.ChooseFile(v2, cover);
      Ui.SetText(v2, "txtPDescriptor", "Smoke cover");
      Ui.Check(v2, "chkPicInclude", true);
      Ui.Click(v2, "btnAddPicture");

      Ui.ShowPanel(v2, "Rating");
      Ui.SetText(v2, "txtRatingUser", "smoke@test");
      Ui.SetText(v2, "txtRatingRating", "200");
      Ui.SetText(v2, "txtRatingCounter", "7");
      Ui.Click(v2, "btnAddRating");

      Ui.ShowPanel(v2, "User defined information");
      Ui.SetText(v2, "txtTXXXDesc", "SmokeKey");
      Ui.SetText(v2, "txtTXXXContent", "SmokeValue");
      Ui.Click(v2, "btnAddTXXX");
      Ui.SetText(v2, "txtWXXXDesc", "SmokeLink");
      Ui.SetText(v2, "txtWXXXContent", "http://smoke.example/link");
      Ui.Click(v2, "btnAddWXXX");

      Ui.ShowPanel(v2, "Involved People");
      Ui.SetText(v2, "txtInvFunction", "producer");
      Ui.SetText(v2, "txtInvPerson", "Smoke Producer");
      Ui.Click(v2, "btnAddInv");
      Ui.SetText(v2, "txtMusicianInst", "guitar");
      Ui.SetText(v2, "txtMusicianName", "Smoke Guitarist");
      Ui.Click(v2, "btnAddMusician");
    }

    private static TextInformationFrame TextFrame(TagLib.Id3v2.Tag tag, string id)
    {
      return tag.GetFrames<TextInformationFrame>(ByteVector.FromString(id, StringType.Latin1)).FirstOrDefault();
    }

    // The text of a text or URL frame, or null when the frame is missing.
    private static string FrameText(TagLib.Id3v2.Tag tag, string id)
    {
      if (id[0] == 'W')
      {
        UrlLinkFrame url = tag.GetFrames<UrlLinkFrame>(ByteVector.FromString(id, StringType.Latin1)).FirstOrDefault();
        return (url == null) ? null : string.Join("/", url.Text);
      }

      TextInformationFrame text = TextFrame(tag, id);
      return (text == null) ? null : string.Join("/", text.Text);
    }
  }
}
