using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ID3_TagIT.SmokeTests
{
  // Tiny MP3 files written byte by byte, so the tests don't depend on any tagging library to
  // create their input.
  internal static class Mp3Files
  {
    // 20 silent MPEG-1 Layer III frames (128 kbit/s, 44.1 kHz): about half a second, no tags.
    public static byte[] Audio()
    {
      List<byte> audio = new List<byte>();

      for (int i = 0; i < 20; i++)
      {
        audio.AddRange(new byte[] { 0xFF, 0xFB, 0x90, 0x64 });
        audio.AddRange(new byte[413]);
      }

      return audio.ToArray();
    }

    public static string WriteTagless(string path)
    {
      File.WriteAllBytes(path, Audio());
      return path;
    }

    // An ID3v2 tag of the given major version (2, 3 or 4) followed by the audio. Frames are
    // complete frame bodies keyed by frame ID (3 characters for v2.2, 4 otherwise).
    public static string WriteTagged(string path, int version, params KeyValuePair<string, byte[]>[] frames)
    {
      List<byte> body = new List<byte>();

      foreach (KeyValuePair<string, byte[]> frame in frames)
      {
        body.AddRange(Encoding.ASCII.GetBytes(frame.Key));

        if (version == 2)
          body.AddRange(new byte[] { (byte)(frame.Value.Length >> 16), (byte)(frame.Value.Length >> 8), (byte)frame.Value.Length });
        else
        {
          body.AddRange(version == 4 ? SyncSafe(frame.Value.Length) : BigEndian(frame.Value.Length));
          body.AddRange(new byte[2]);
        }

        body.AddRange(frame.Value);
      }

      body.AddRange(new byte[64]); // padding
      List<byte> file = new List<byte>(Encoding.ASCII.GetBytes("ID3"));
      file.AddRange(new byte[] { (byte)version, 0, 0 });
      file.AddRange(SyncSafe(body.Count));
      file.AddRange(body);
      file.AddRange(Audio());
      File.WriteAllBytes(path, file.ToArray());
      return path;
    }

    public static KeyValuePair<string, byte[]> Frame(string id, byte[] body)
    {
      return new KeyValuePair<string, byte[]>(id, body);
    }

    // A text frame body in ISO-8859-1.
    public static KeyValuePair<string, byte[]> Text(string id, string text)
    {
      List<byte> body = new List<byte> { 0 };
      body.AddRange(Encoding.GetEncoding(28591).GetBytes(text));
      return Frame(id, body.ToArray());
    }

    // A comment frame body (v2.2 COM or v2.3 COMM) in ISO-8859-1 with an empty description.
    public static KeyValuePair<string, byte[]> Comment(string id, string text)
    {
      List<byte> body = new List<byte> { 0 };
      body.AddRange(Encoding.ASCII.GetBytes("eng"));
      body.Add(0);
      body.AddRange(Encoding.GetEncoding(28591).GetBytes(text));
      return Frame(id, body.ToArray());
    }

    private static byte[] SyncSafe(int value)
    {
      return new byte[] { (byte)((value >> 21) & 0x7F), (byte)((value >> 14) & 0x7F), (byte)((value >> 7) & 0x7F), (byte)(value & 0x7F) };
    }

    private static byte[] BigEndian(int value)
    {
      return new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
    }

    // True if `needle` occurs anywhere in the file.
    public static bool Contains(string path, byte[] needle)
    {
      byte[] data = File.ReadAllBytes(path);

      for (int i = 0; i <= data.Length - needle.Length; i++)
      {
        int j = 0;

        while (j < needle.Length && data[i + j] == needle[j])
          j++;

        if (j == needle.Length)
          return true;
      }

      return false;
    }
  }
}
