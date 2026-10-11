using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ID3_TagIT.Tests
{
  // Writes small synthetic MP3 files: MPEG-1 Layer III frames (128 kbit/s, 44.1 kHz) with
  // pseudo-random audio bytes, optionally preceded by an ID3v2.3 tag and followed by an ID3v1
  // tag.
  internal sealed class Mp3Builder
  {
    private const int FRAME_LENGTH = 417; // 144 * 128000 / 44100, no padding
    private readonly Dictionary<string, string> objV2Frames = new Dictionary<string, string>();
    private string[] astrV1;
    private int vintFrames = 40;
    private int vintSeed = 1;

    public Mp3Builder WithV1(string title, string artist, string album, string year)
    {
      this.astrV1 = new string[] { title, artist, album, year };
      return this;
    }

    public Mp3Builder WithV2Text(string frameID, string text)
    {
      this.objV2Frames[frameID] = text;
      return this;
    }

    public Mp3Builder WithAudio(int frames, int seed)
    {
      this.vintFrames = frames;
      this.vintSeed = seed;
      return this;
    }

    public string Write(string path)
    {
      using (FileStream stream = File.Create(path))
      {
        if (this.objV2Frames.Count > 0)
        {
          byte[] tag = this.BuildV2();
          stream.Write(tag, 0, tag.Length);
        }

        byte[] audio = this.BuildAudio();
        stream.Write(audio, 0, audio.Length);

        if (this.astrV1 != null)
        {
          byte[] tag = this.BuildV1();
          stream.Write(tag, 0, tag.Length);
        }
      }

      return path;
    }

    // The audio frames alone, which the audio checksum covers.
    public byte[] BuildAudio()
    {
      Random random = new Random(this.vintSeed);
      byte[] audio = new byte[FRAME_LENGTH * this.vintFrames];

      for (int frame = 0; frame < this.vintFrames; frame++)
      {
        int offset = frame * FRAME_LENGTH;
        audio[offset] = 0xFF;
        audio[offset + 1] = 0xFB; // MPEG-1, Layer III, no CRC
        audio[offset + 2] = 0x90; // 128 kbit/s, 44.1 kHz, no padding
        audio[offset + 3] = 0x64; // joint stereo

        for (int i = offset + 4; i < offset + FRAME_LENGTH; i++)
          audio[i] = (byte)random.Next(256);
      }

      return audio;
    }

    private byte[] BuildV1()
    {
      byte[] tag = new byte[128];
      Encoding.ASCII.GetBytes("TAG").CopyTo(tag, 0);
      Field(this.astrV1[0], 30).CopyTo(tag, 3);
      Field(this.astrV1[1], 30).CopyTo(tag, 33);
      Field(this.astrV1[2], 30).CopyTo(tag, 63);
      Field(this.astrV1[3], 4).CopyTo(tag, 93);
      tag[127] = 0xFF; // no genre

      return tag;
    }

    private byte[] BuildV2()
    {
      MemoryStream frames = new MemoryStream();

      foreach (KeyValuePair<string, string> frame in this.objV2Frames)
      {
        byte[] text = Encoding.GetEncoding(28591).GetBytes(frame.Value);
        int size = text.Length + 1; // plus the encoding byte (0 = ISO-8859-1)
        frames.Write(Encoding.ASCII.GetBytes(frame.Key), 0, 4);
        frames.Write(new byte[] { (byte)(size >> 24), (byte)(size >> 16), (byte)(size >> 8), (byte)size, 0, 0, 0 }, 0, 7);
        frames.Write(text, 0, text.Length);
      }

      frames.Write(new byte[64], 0, 64); // padding
      int tagSize = (int)frames.Length;
      MemoryStream tag = new MemoryStream();
      tag.Write(Encoding.ASCII.GetBytes("ID3"), 0, 3);
      tag.Write(new byte[] { 3, 0, 0 }, 0, 3); // v2.3.0, no flags
      tag.Write(new byte[] { (byte)((tagSize >> 21) & 0x7F), (byte)((tagSize >> 14) & 0x7F), (byte)((tagSize >> 7) & 0x7F), (byte)(tagSize & 0x7F) }, 0, 4);
      frames.WriteTo(tag);

      return tag.ToArray();
    }

    private static byte[] Field(string value, int length)
    {
      byte[] field = new byte[length];
      Encoding.GetEncoding(28591).GetBytes(value ?? "").CopyTo(field, 0);

      return field;
    }
  }
}
