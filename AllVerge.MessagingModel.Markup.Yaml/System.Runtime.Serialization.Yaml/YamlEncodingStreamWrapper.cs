// Decompiled with JetBrains decompiler
// Type: System.Runtime.Serialization.Json.JsonEncodingStreamWrapper
// Assembly: System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: B2BB7AE7-A91A-4F0A-8DA5-0D652A3FFFA0
// Assembly location: C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Runtime.Serialization\v4.0_4.0.0.0__b77a5c561934e089\System.Runtime.Serialization.dll

using System.IO;
using System.ServiceModel;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Yaml
{
  internal class YamlEncodingStreamWrapper : Stream
  {
    private static readonly UnicodeEncoding SafeBEUTF16 = new UnicodeEncoding(true, false, false);
    private static readonly UnicodeEncoding SafeUTF16 = new UnicodeEncoding(false, false, false);
    private static readonly UTF8Encoding SafeUTF8 = new UTF8Encoding(false, false);
    private static readonly UnicodeEncoding ValidatingBEUTF16 = new UnicodeEncoding(true, false, true);
    private static readonly UnicodeEncoding ValidatingUTF16 = new UnicodeEncoding(false, false, true);
    private static readonly UTF8Encoding ValidatingUTF8 = new UTF8Encoding(false, true);
    private byte[] byteBuffer = new byte[1];
    private const int BufferLength = 128;
    private int byteCount;
    private int byteOffset;
    private byte[] bytes;
    private char[] chars;
    private Decoder dec;
    private Encoder enc;
    private Encoding encoding;
    private YamlEncodingStreamWrapper.SupportedEncoding encodingCode;
    private bool isReading;
    private Stream stream;

    public YamlEncodingStreamWrapper(Stream stream, Encoding encoding, bool isReader)
    {
      this.isReading = isReader;
      if (isReader)
      {
        this.InitForReading(stream, encoding);
      }
      else
      {
        if (encoding == null)
          throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof (encoding));
        this.InitForWriting(stream, encoding);
      }
    }

    public override bool CanRead
    {
      get
      {
        if (!this.isReading)
          return false;
        return this.stream.CanRead;
      }
    }

    public override bool CanSeek
    {
      get
      {
        return false;
      }
    }

    public override bool CanTimeout
    {
      get
      {
        return this.stream.CanTimeout;
      }
    }

    public override bool CanWrite
    {
      get
      {
        if (this.isReading)
          return false;
        return this.stream.CanWrite;
      }
    }

    public override long Length
    {
      get
      {
        return this.stream.Length;
      }
    }

    public override long Position
    {
      get
      {
        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new NotSupportedException());
      }
      set
      {
        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new NotSupportedException());
      }
    }

    public override int ReadTimeout
    {
      get
      {
        return this.stream.ReadTimeout;
      }
      set
      {
        this.stream.ReadTimeout = value;
      }
    }

    public override int WriteTimeout
    {
      get
      {
        return this.stream.WriteTimeout;
      }
      set
      {
        this.stream.WriteTimeout = value;
      }
    }

    public static ArraySegment<byte> ProcessBuffer(byte[] buffer, int offset, int count, Encoding encoding)
    {
      try
      {
        YamlEncodingStreamWrapper.SupportedEncoding supportedEncoding1 = YamlEncodingStreamWrapper.GetSupportedEncoding(encoding);
        YamlEncodingStreamWrapper.SupportedEncoding supportedEncoding2 = count >= 2 ? YamlEncodingStreamWrapper.ReadEncoding(buffer[offset], buffer[offset + 1]) : YamlEncodingStreamWrapper.SupportedEncoding.UTF8;
        if (supportedEncoding1 != YamlEncodingStreamWrapper.SupportedEncoding.None && supportedEncoding1 != supportedEncoding2)
          YamlEncodingStreamWrapper.ThrowExpectedEncodingMismatch(supportedEncoding1, supportedEncoding2);
        if (supportedEncoding2 == YamlEncodingStreamWrapper.SupportedEncoding.UTF8)
          return new ArraySegment<byte>(buffer, offset, count);
        return new ArraySegment<byte>(YamlEncodingStreamWrapper.ValidatingUTF8.GetBytes(YamlEncodingStreamWrapper.GetEncoding(supportedEncoding2).GetChars(buffer, offset, count)));
      }
      catch (DecoderFallbackException ex)
      {
        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new XmlException(PublicSR.JsonInvalidBytes, (Exception) ex));
      }
    }

    public override void Close()
    {
      this.Flush();
      base.Close();
      this.stream.Close();
    }

    public override void Flush()
    {
      this.stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      try
      {
        if (this.byteCount == 0)
        {
          if (this.encodingCode == YamlEncodingStreamWrapper.SupportedEncoding.UTF8)
            return this.stream.Read(buffer, offset, count);
          this.byteOffset = 0;
          this.byteCount = this.stream.Read(this.bytes, this.byteCount, (this.chars.Length - 1) * 2);
          if (this.byteCount == 0)
            return 0;
          this.CleanupCharBreak();
          this.byteCount = Encoding.UTF8.GetBytes(this.chars, 0, this.encoding.GetChars(this.bytes, 0, this.byteCount, this.chars, 0), this.bytes, 0);
        }
        if (this.byteCount < count)
          count = this.byteCount;
        Buffer.BlockCopy((Array) this.bytes, this.byteOffset, (Array) buffer, offset, count);
        this.byteOffset += count;
        this.byteCount -= count;
        return count;
      }
      catch (DecoderFallbackException ex)
      {
        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new XmlException(PublicSR.JsonInvalidBytes, (Exception) ex));
      }
    }

    public override int ReadByte()
    {
      if (this.byteCount == 0 && this.encodingCode == YamlEncodingStreamWrapper.SupportedEncoding.UTF8)
        return this.stream.ReadByte();
      if (this.Read(this.byteBuffer, 0, 1) == 0)
        return -1;
      return (int) this.byteBuffer[0];
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new NotSupportedException());
    }

    public override void SetLength(long value)
    {
      throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new NotSupportedException());
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      if (this.encodingCode == YamlEncodingStreamWrapper.SupportedEncoding.UTF8)
      {
        this.stream.Write(buffer, offset, count);
      }
      else
      {
        while (count > 0)
        {
          int byteCount = this.chars.Length < count ? this.chars.Length : count;
          this.byteCount = this.enc.GetBytes(this.chars, 0, this.dec.GetChars(buffer, offset, byteCount, this.chars, 0, false), this.bytes, 0, false);
          this.stream.Write(this.bytes, 0, this.byteCount);
          offset += byteCount;
          count -= byteCount;
        }
      }
    }

    public override void WriteByte(byte b)
    {
      if (this.encodingCode == YamlEncodingStreamWrapper.SupportedEncoding.UTF8)
      {
        this.stream.WriteByte(b);
      }
      else
      {
        this.byteBuffer[0] = b;
        this.Write(this.byteBuffer, 0, 1);
      }
    }

    private static Encoding GetEncoding(YamlEncodingStreamWrapper.SupportedEncoding e)
    {
      switch (e)
      {
        case YamlEncodingStreamWrapper.SupportedEncoding.UTF8:
          return (Encoding) YamlEncodingStreamWrapper.ValidatingUTF8;
        case YamlEncodingStreamWrapper.SupportedEncoding.UTF16LE:
          return (Encoding) YamlEncodingStreamWrapper.ValidatingUTF16;
        case YamlEncodingStreamWrapper.SupportedEncoding.UTF16BE:
          return (Encoding) YamlEncodingStreamWrapper.ValidatingBEUTF16;
        default:
          throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new XmlException(AMMMYR.YamlEncodingNotSupported));
      }
    }

    private static string GetEncodingName(YamlEncodingStreamWrapper.SupportedEncoding enc)
    {
      switch (enc)
      {
        case YamlEncodingStreamWrapper.SupportedEncoding.UTF8:
          return "utf-8";
        case YamlEncodingStreamWrapper.SupportedEncoding.UTF16LE:
          return "utf-16LE";
        case YamlEncodingStreamWrapper.SupportedEncoding.UTF16BE:
          return "utf-16BE";
        default:
          throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new XmlException(AMMMYR.YamlEncodingNotSupported));
      }
    }

    private static YamlEncodingStreamWrapper.SupportedEncoding GetSupportedEncoding(Encoding encoding)
    {
      if (encoding == null)
        return YamlEncodingStreamWrapper.SupportedEncoding.None;
      if (encoding.WebName == YamlEncodingStreamWrapper.ValidatingUTF8.WebName)
        return YamlEncodingStreamWrapper.SupportedEncoding.UTF8;
      if (encoding.WebName == YamlEncodingStreamWrapper.ValidatingUTF16.WebName)
        return YamlEncodingStreamWrapper.SupportedEncoding.UTF16LE;
      if (encoding.WebName == YamlEncodingStreamWrapper.ValidatingBEUTF16.WebName)
        return YamlEncodingStreamWrapper.SupportedEncoding.UTF16BE;
      throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new XmlException(AMMMYR.YamlEncodingNotSupported));
    }

    private static YamlEncodingStreamWrapper.SupportedEncoding ReadEncoding(byte b1, byte b2)
    {
      if (b1 == (byte) 0 && b2 != (byte) 0)
        return YamlEncodingStreamWrapper.SupportedEncoding.UTF16BE;
      if (b1 != (byte) 0 && b2 == (byte) 0)
        return YamlEncodingStreamWrapper.SupportedEncoding.UTF16LE;
      if (b1 == (byte) 0 && b2 == (byte) 0)
        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new XmlException(PublicSR.JsonInvalidBytes));
      return YamlEncodingStreamWrapper.SupportedEncoding.UTF8;
    }

    private static void ThrowExpectedEncodingMismatch(YamlEncodingStreamWrapper.SupportedEncoding expEnc, YamlEncodingStreamWrapper.SupportedEncoding actualEnc)
    {
      throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new XmlException(PublicSR.Format(PublicSR.JsonExpectedEncoding, (object) YamlEncodingStreamWrapper.GetEncodingName(expEnc), (object) YamlEncodingStreamWrapper.GetEncodingName(actualEnc))));
    }

    private void CleanupCharBreak()
    {
      int num1 = this.byteOffset + this.byteCount;
      if (this.byteCount % 2 != 0)
      {
        int num2 = this.stream.ReadByte();
        if (num2 < 0)
          throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new XmlException(PublicSR.JsonUnexpectedEndOfFile));
        this.bytes[num1++] = (byte) num2;
        ++this.byteCount;
      }
      int num3 = this.encodingCode != YamlEncodingStreamWrapper.SupportedEncoding.UTF16LE ? (int) this.bytes[num1 - 1] + ((int) this.bytes[num1 - 2] << 8) : (int) this.bytes[num1 - 2] + ((int) this.bytes[num1 - 1] << 8);
      if ((num3 & 56320) == 56320 || num3 < 55296 || num3 > 56319)
        return;
      int num4 = this.stream.ReadByte();
      int num5 = this.stream.ReadByte();
      if (num5 < 0)
        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new XmlException(PublicSR.JsonUnexpectedEndOfFile));
      byte[] bytes1 = this.bytes;
      int index1 = num1;
      int num6 = index1 + 1;
      int num7 = (int) (byte) num4;
      bytes1[index1] = (byte) num7;
      byte[] bytes2 = this.bytes;
      int index2 = num6;
      int num8 = index2 + 1;
      int num9 = (int) (byte) num5;
      bytes2[index2] = (byte) num9;
      this.byteCount += 2;
    }

    private void EnsureBuffers()
    {
      this.EnsureByteBuffer();
      if (this.chars != null)
        return;
      this.chars = new char[128];
    }

    private void EnsureByteBuffer()
    {
      if (this.bytes != null)
        return;
      this.bytes = new byte[512];
      this.byteOffset = 0;
      this.byteCount = 0;
    }

    private void FillBuffer(int count)
    {
      count -= this.byteCount;
      while (count > 0)
      {
        int num = this.stream.Read(this.bytes, this.byteOffset + this.byteCount, count);
        if (num == 0)
          break;
        this.byteCount += num;
        count -= num;
      }
    }

    private void InitForReading(Stream inputStream, Encoding expectedEncoding)
    {
      try
      {
        this.stream = (Stream) new BufferedStream(inputStream);
        YamlEncodingStreamWrapper.SupportedEncoding supportedEncoding1 = YamlEncodingStreamWrapper.GetSupportedEncoding(expectedEncoding);
        YamlEncodingStreamWrapper.SupportedEncoding supportedEncoding2 = this.ReadEncoding();
        if (supportedEncoding1 != YamlEncodingStreamWrapper.SupportedEncoding.None && supportedEncoding1 != supportedEncoding2)
          YamlEncodingStreamWrapper.ThrowExpectedEncodingMismatch(supportedEncoding1, supportedEncoding2);
        if (supportedEncoding2 == YamlEncodingStreamWrapper.SupportedEncoding.UTF8)
          return;
        this.EnsureBuffers();
        this.FillBuffer(254);
        this.encodingCode = supportedEncoding2;
        this.encoding = YamlEncodingStreamWrapper.GetEncoding(supportedEncoding2);
        this.CleanupCharBreak();
        int chars = this.encoding.GetChars(this.bytes, this.byteOffset, this.byteCount, this.chars, 0);
        this.byteOffset = 0;
        this.byteCount = YamlEncodingStreamWrapper.ValidatingUTF8.GetBytes(this.chars, 0, chars, this.bytes, 0);
      }
      catch (DecoderFallbackException ex)
      {
        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError((Exception) new XmlException(PublicSR.JsonInvalidBytes, (Exception) ex));
      }
    }

    private void InitForWriting(Stream outputStream, Encoding writeEncoding)
    {
      this.encoding = writeEncoding;
      this.stream = (Stream) new BufferedStream(outputStream);
      this.encodingCode = YamlEncodingStreamWrapper.GetSupportedEncoding(writeEncoding);
      if (this.encodingCode == YamlEncodingStreamWrapper.SupportedEncoding.UTF8)
        return;
      this.EnsureBuffers();
      this.dec = YamlEncodingStreamWrapper.ValidatingUTF8.GetDecoder();
      this.enc = this.encoding.GetEncoder();
    }

    private YamlEncodingStreamWrapper.SupportedEncoding ReadEncoding()
    {
      int num1 = this.stream.ReadByte();
      int num2 = this.stream.ReadByte();
      this.EnsureByteBuffer();
      YamlEncodingStreamWrapper.SupportedEncoding supportedEncoding;
      if (num1 == -1)
      {
        supportedEncoding = YamlEncodingStreamWrapper.SupportedEncoding.UTF8;
        this.byteCount = 0;
      }
      else if (num2 == -1)
      {
        supportedEncoding = YamlEncodingStreamWrapper.SupportedEncoding.UTF8;
        this.bytes[0] = (byte) num1;
        this.byteCount = 1;
      }
      else
      {
        supportedEncoding = YamlEncodingStreamWrapper.ReadEncoding((byte) num1, (byte) num2);
        this.bytes[0] = (byte) num1;
        this.bytes[1] = (byte) num2;
        this.byteCount = 2;
      }
      return supportedEncoding;
    }

    private enum SupportedEncoding
    {
      UTF8,
      UTF16LE,
      UTF16BE,
      None,
    }
  }
}
