using System;
using System.IO;

namespace ID3_TagIT
{
  public class CRC32
  {
    private const int BUFFER_SIZE = 0x400;
    private int[] crc32Table;

    public CRC32()
    {
      int num2 = -306674912;
      this.crc32Table = new int[0x101];
      int index = 0;
      do
      {
        int num = index;
        int num4 = 8;
        do
        {
          if ((num & 1) > 0)
          {
            num = (int)((((long)(num & -2)) / 2L) & 0x7fffffffL);
            num ^= num2;
          }
          else
          {
            num = (int)((((long)(num & -2)) / 2L) & 0x7fffffffL);
          }
          num4 += -1;
        }
        while (num4 >= 1);
        this.crc32Table[index] = num;
        index++;
      }
      while (index <= 0xff);
    }

    public int CRC_32(int intCRC32, ref byte[] bArrayIn, long Length)
    {
      int num2 = -1;
      int num6 = (int)Length;
      for (int i = 0; i <= num6; i++)
      {
        int index = (num2 & 0xff) ^ bArrayIn[i];
        num2 = (int)((uint)num2 >> 8);
        num2 ^= this.crc32Table[index];
      }
      return ~num2;
    }

    // CRC32 of length bytes of the stream from start. Reads in blocks and polls cancelRequested
    // (if given) between them. Returns false if cancelled or if the stream ends early, in which
    // case checksum is 0.
    public bool TryCRC_32(Stream stream, long start, long length, Func<bool> cancelRequested, out int checksum)
    {
      int crc = -1;
      byte[] buffer = new byte[0x10000];

      checksum = 0;
      stream.Seek(start, SeekOrigin.Begin);

      while (length > 0)
      {
        if (cancelRequested != null && cancelRequested())
          return false;

        int read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, length));

        if (read <= 0)
          return false;

        for (int i = 0; i < read; i++)
        {
          int index = (crc & 0xff) ^ buffer[i];
          crc = (int)((uint)crc >> 8);
          crc ^= this.crc32Table[index];
        }

        length -= read;
      }

      checksum = ~crc;

      return true;
    }
  }
}