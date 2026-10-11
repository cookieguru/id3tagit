using System;
using System.IO;
using System.Text;
using Xunit;

namespace ID3_TagIT.Tests
{
  public class Crc32Tests
  {
    // The standard CRC-32 check value: CRC-32 of the ASCII string "123456789".
    private const int CHECK_VALUE = unchecked((int)0xCBF43926);
    private static readonly byte[] CheckInput = Encoding.ASCII.GetBytes("123456789");

    [Fact]
    public void ByteArrayOverloadMatchesTheStandardCheckValue()
    {
      byte[] input = (byte[])CheckInput.Clone();

      // The length argument is the index of the last byte, not the count.
      Assert.Equal(CHECK_VALUE, new CRC32().CRC_32(0, ref input, input.Length - 1));
    }

    [Fact]
    public void StreamOverloadMatchesTheStandardCheckValue()
    {
      int checksum;

      Assert.True(new CRC32().TryCRC_32(new MemoryStream(CheckInput), 0, CheckInput.Length, null, out checksum));
      Assert.Equal(CHECK_VALUE, checksum);
    }

    [Fact]
    public void StreamOverloadChecksumsOnlyTheRequestedRange()
    {
      byte[] padded = new byte[CheckInput.Length + 20];
      CheckInput.CopyTo(padded, 10);
      int checksum;

      Assert.True(new CRC32().TryCRC_32(new MemoryStream(padded), 10, CheckInput.Length, null, out checksum));
      Assert.Equal(CHECK_VALUE, checksum);
    }

    [Fact]
    public void OverloadsAgreeAcrossReadBlocks()
    {
      // Larger than the 64 KB read block, so the stream overload reads several blocks.
      byte[] data = new byte[200000];
      new Random(7).NextBytes(data);
      byte[] copy = (byte[])data.Clone();
      int streamChecksum;

      new CRC32().TryCRC_32(new MemoryStream(data), 0, data.Length, null, out streamChecksum);

      Assert.Equal(new CRC32().CRC_32(0, ref copy, copy.Length - 1), streamChecksum);
    }

    [Fact]
    public void CancellingReturnsFalseAndZero()
    {
      byte[] data = new byte[200000];
      int checksum;

      Assert.False(new CRC32().TryCRC_32(new MemoryStream(data), 0, data.Length, () => true, out checksum));
      Assert.Equal(0, checksum);
    }

    [Fact]
    public void AStreamEndingEarlyReturnsFalseAndZero()
    {
      int checksum;

      Assert.False(new CRC32().TryCRC_32(new MemoryStream(CheckInput), 0, CheckInput.Length + 1, null, out checksum));
      Assert.Equal(0, checksum);
    }
  }
}
