using System.IO;
using Xunit;

namespace ID3_TagIT.Tests
{
  // Reading, writing and checksumming MP3 files, using synthetic files from Mp3Builder.
  public class Mp3Tests
  {
    public Mp3Tests()
    {
      TestApp.EnsureInitialized();
    }

    private static MP3 Read(string path)
    {
      MP3 mp3 = new MP3(path);
      Assert.True(mp3.ExploreMP3());

      return mp3;
    }

    [Fact]
    public void ReadsAnID3v1Tag()
    {
      using (TempFolder folder = new TempFolder())
      {
        MP3 mp3 = Read(new Mp3Builder().WithV1("A Title", "An Artist", "An Album", "1999").Write(folder.File("v1.mp3")));

        Assert.True(mp3.V1TAG.TAGPresent);
        Assert.Equal("A Title", mp3.V1TAG.Title);
        Assert.Equal("An Artist", mp3.V1TAG.Artist);
        Assert.Equal("An Album", mp3.V1TAG.Album);
        Assert.Equal(1999, mp3.V1TAG.Year);
      }
    }

    [Fact]
    public void ReadsID3v23TextFrames()
    {
      using (TempFolder folder = new TempFolder())
      {
        MP3 mp3 = Read(new Mp3Builder().WithV2Text("TIT2", "Title").WithV2Text("TPE1", "Artist").WithV2Text("TPUB", "Label").Write(folder.File("v2.mp3")));

        Assert.Equal(3, (int)mp3.V2TAG.TAGVersion);
        Assert.Equal("Title", mp3.V2TAG.GetTextWebFrameContent("TIT2"));
        Assert.Equal("Artist", mp3.V2TAG.GetTextWebFrameContent("TPE1"));
        Assert.Equal("Label", mp3.V2TAG.GetTextWebFrameContent("TPUB"));
      }
    }

    [Fact]
    public void ReadsTheMpegHeader()
    {
      using (TempFolder folder = new TempFolder())
      {
        MP3 mp3 = Read(new Mp3Builder().WithV2Text("TIT2", "Title").Write(folder.File("header.mp3")));

        Assert.Equal(128000, mp3.Bitrate);
        Assert.Equal(44100, mp3.Samplerate);
      }
    }

    [Fact]
    public void WritesChangedTagsAndKeepsTheAudio()
    {
      using (TempFolder folder = new TempFolder())
      {
        Mp3Builder builder = new Mp3Builder().WithV1("Old", "Artist", "Album", "2000").WithV2Text("TIT2", "Old").WithAudio(60, 3);
        string path = builder.Write(folder.File("write.mp3"));
        MP3 mp3 = Read(path);
        mp3.CalcAudioCheckSum();
        int checksumBefore = mp3.AudioCheckSum;

        mp3.V1TAG.Title = "New v1 title";
        mp3.V2TAG.RemoveFrames("TIT2");
        mp3.V2TAG.AddFrame(ID3Functions.CreateTextFrame("TIT2", "A much longer new v2 title"));
        Assert.True(mp3.WriteChanges());

        MP3 reread = Read(path);
        reread.CalcAudioCheckSum();

        Assert.Equal("New v1 title", reread.V1TAG.Title);
        Assert.Equal("A much longer new v2 title", reread.V2TAG.GetTextWebFrameContent("TIT2"));
        Assert.Equal(checksumBefore, reread.AudioCheckSum);
      }
    }

    [Fact]
    public void DoesNotWriteAReadOnlyFile()
    {
      using (TempFolder folder = new TempFolder())
      {
        string path = new Mp3Builder().WithV1("Old", "Artist", "Album", "2000").Write(folder.File("readonly.mp3"));
        byte[] before = File.ReadAllBytes(path);
        File.SetAttributes(path, FileAttributes.ReadOnly);
        MP3 mp3 = Read(path);
        mp3.V1TAG.Title = "New";

        // IgnoreWrite (the "Ignore write protection" preference) is off in the test settings.
        Assert.False(mp3.WriteChanges());
        Assert.Equal(before, File.ReadAllBytes(path));
      }
    }

    [Fact]
    public void AudioChecksumIgnoresTheTags()
    {
      using (TempFolder folder = new TempFolder())
      {
        MP3 plain = Read(new Mp3Builder().WithAudio(30, 5).Write(folder.File("plain.mp3")));
        MP3 tagged = Read(new Mp3Builder().WithAudio(30, 5).WithV1("T", "A", "B", "2001").WithV2Text("TIT2", "Tagged").Write(folder.File("tagged.mp3")));
        MP3 different = Read(new Mp3Builder().WithAudio(30, 6).Write(folder.File("different.mp3")));
        plain.CalcAudioCheckSum();
        tagged.CalcAudioCheckSum();
        different.CalcAudioCheckSum();

        Assert.NotEqual(0, plain.AudioCheckSum);
        Assert.Equal(plain.AudioCheckSum, tagged.AudioCheckSum);
        Assert.NotEqual(plain.AudioCheckSum, different.AudioCheckSum);
      }
    }

    [Fact]
    public void CancellingTheAudioChecksumLeavesItAtZero()
    {
      using (TempFolder folder = new TempFolder())
      {
        MP3 mp3 = Read(new Mp3Builder().Write(folder.File("cancel.mp3")));
        mp3.CalcAudioCheckSum(() => true);

        Assert.Equal(0, mp3.AudioCheckSum);
      }
    }
  }
}
