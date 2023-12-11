#if NETCORE && !NETSTANDARD2_0

using System;
using System.Buffers.Text;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text.Common;
using ServiceStack.Text.Pools;

namespace ServiceStack.Text;

public sealed class NetCoreMemory : MemoryProvider
{
    private static NetCoreMemory provider;
    public static NetCoreMemory Provider => provider ??= new NetCoreMemory();
    private NetCoreMemory() { }
    
    public static void Configure() => Instance = Provider;
    
    public override bool ParseBoolean(ReadOnlySpan<char> value) => bool.Parse(value);

    public override bool TryParseBoolean(ReadOnlySpan<char> value, out bool result) =>
        bool.TryParse(value, out result);

    public override bool TryParseDecimal(ReadOnlySpan<char> value, out decimal result) =>
        decimal.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);

    public override decimal ParseDecimal(ReadOnlySpan<char> value, bool allowThousands) =>
        decimal.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);

    public override bool TryParseFloat(ReadOnlySpan<char> value, out float result) =>
        float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);

    public override bool TryParseDouble(ReadOnlySpan<char> value, out double result) =>
        double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);

    public override decimal ParseDecimal(ReadOnlySpan<char> value) =>
        decimal.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
    
    public override float ParseFloat(ReadOnlySpan<char> value) =>
        float.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);

    public override double ParseDouble(ReadOnlySpan<char> value) =>
        double.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);

    public override sbyte ParseSByte(ReadOnlySpan<char> value) => sbyte.Parse(value);

    public override byte ParseByte(ReadOnlySpan<char> value) => byte.Parse(value);

    public override short ParseInt16(ReadOnlySpan<char> value) => short.Parse(value);

    public override ushort ParseUInt16(ReadOnlySpan<char> value) => ushort.Parse(value);

    public override int ParseInt32(ReadOnlySpan<char> value) => int.Parse(value);

    public override uint ParseUInt32(ReadOnlySpan<char> value) => uint.Parse(value);

    public override uint ParseUInt32(ReadOnlySpan<char> value, NumberStyles style) => uint.Parse(value.ToString(), NumberStyles.HexNumber);

    public override long ParseInt64(ReadOnlySpan<char> value) => long.Parse(value);

    public override ulong ParseUInt64(ReadOnlySpan<char> value) => ulong.Parse(value);

    public override Guid ParseGuid(ReadOnlySpan<char> value) => Guid.Parse(value);
    
    public override byte[] ParseBase64(ReadOnlySpan<char> value)
    {
        byte[] bytes = BufferPool.GetBuffer(Base64.GetMaxDecodedFromUtf8Length(value.Length));
        try
        {
            if (Convert.TryFromBase64Chars(value, bytes, out var bytesWritten))
            {
                var ret = new byte[bytesWritten];
                Buffer.BlockCopy(bytes, 0, ret, 0, bytesWritten);
                return ret;
            }
            else
            {
                var chars = value.ToArray();
                return Convert.FromBase64CharArray(chars, 0, chars.Length);
            }
        }
        finally 
        {
            BufferPool.ReleaseBufferToPool(ref bytes);
        }
    }

    public override string ToBase64(ReadOnlyMemory<byte> value)
    {
        return Convert.ToBase64String(value.Span);
    }

    public override void Write(Stream stream, ReadOnlyMemory<char> value)
    {
        var utf8 = ToUtf8(value.Span);
        if (stream is MemoryStream ms)
            ms.Write(utf8.Span);
        else
            stream.Write(utf8.Span);
    }

    public override void Write(Stream stream, ReadOnlyMemory<byte> value)
    {
        if (stream is MemoryStream ms)
            ms.Write(value.Span);
        else
            stream.Write(value.Span);
    }

    public override async Task WriteAsync(Stream stream, ReadOnlyMemory<char> value, CancellationToken token = default)
    {
        var utf8 = ToUtf8(value.Span);
        if (stream is MemoryStream ms)
            ms.Write(utf8.Span);
        else
            await stream.WriteAsync(utf8, token).ConfigAwait();
    }

    public override Task WriteAsync(Stream stream, ReadOnlySpan<char> value, CancellationToken token=default)
    {
        var utf8 = ToUtf8(value);
        if (stream is MemoryStream ms)
        {
            ms.Write(utf8.Span);
            return Task.CompletedTask;
        }
        return stream.WriteAsync(utf8, token).AsTask();
    }

    public override async Task WriteAsync(Stream stream, ReadOnlyMemory<byte> value, CancellationToken token = default)
    {
        if (stream is MemoryStream ms)
            ms.Write(value.Span);
        else
            await stream.WriteAsync(value, token).ConfigAwait();
    }

    public override object Deserialize(Stream stream, Type type, DeserializeStringSpanDelegate deserializer)
    {
        var fromPool = false;

        if (!(stream is MemoryStream ms))
        {
            fromPool = true;

            if (stream.CanSeek)
                stream.Position = 0;

            ms = stream.CopyToNewMemoryStream();
        }

        return Deserialize(ms, fromPool, type, deserializer);
    }

    public override async Task<object> DeserializeAsync(Stream stream, Type type, DeserializeStringSpanDelegate deserializer)
    {
        var fromPool = false;
        
        if (!(stream is MemoryStream ms))
        {
            fromPool = true;
            
            if (stream.CanSeek)
                stream.Position = 0;

            ms = await stream.CopyToNewMemoryStreamAsync().ConfigAwait();
        }

        return Deserialize(ms, fromPool, type, deserializer);
    }

    private static object Deserialize(MemoryStream memoryStream, bool fromPool, Type type, DeserializeStringSpanDelegate deserializer)
    {
        var bytes = memoryStream.GetBufferAsSpan().WithoutBom();
        var chars = CharPool.GetBuffer(Encoding.UTF8.GetCharCount(bytes));
        try
        {
            var charsWritten = Encoding.UTF8.GetChars(bytes, chars);
            ReadOnlySpan<char> charsSpan = chars; 
            var ret = deserializer(type, charsSpan.Slice(0, charsWritten));
            return ret;
        }
        finally
        {
            CharPool.ReleaseBufferToPool(ref chars);

            if (fromPool)
                memoryStream.Dispose();
        }
    }

    public override StringBuilder Append(StringBuilder sb, ReadOnlySpan<char> value)
    {
        return sb.Append(value);
    }

    public override int GetUtf8CharCount(ReadOnlySpan<byte> bytes) => Encoding.UTF8.GetCharCount(bytes);

    public override int GetUtf8ByteCount(ReadOnlySpan<char> chars) => Encoding.UTF8.GetByteCount(chars);

    public override ReadOnlyMemory<byte> ToUtf8(ReadOnlySpan<char> source)
    {
        Memory<byte> bytes = new byte[Encoding.UTF8.GetByteCount(source)];
        var bytesWritten = Encoding.UTF8.GetBytes(source, bytes.Span);
        return bytes.Slice(0, bytesWritten);
    }

    public override ReadOnlyMemory<char> FromUtf8(ReadOnlySpan<byte> source)
    {
        source = source.WithoutBom();
        Memory<char> chars = new char[Encoding.UTF8.GetCharCount(source)];
        var charsWritten = Encoding.UTF8.GetChars(source, chars.Span);
        return chars.Slice(0, charsWritten);
    }

    public override int ToUtf8(ReadOnlySpan<char> source, Span<byte> destination) => Encoding.UTF8.GetBytes(source, destination);

    public override int FromUtf8(ReadOnlySpan<byte> source, Span<char> destination) => Encoding.UTF8.GetChars(source.WithoutBom(), destination);

    public override byte[] ToUtf8Bytes(ReadOnlySpan<char> source) => ToUtf8(source).ToArray();

    public override string FromUtf8Bytes(ReadOnlySpan<byte> source) => FromUtf8(source.WithoutBom()).ToString();

    public override MemoryStream ToMemoryStream(ReadOnlySpan<byte> source)
    {
        var ms = MemoryStreamFactory.GetStream(source.Length);
        ms.Write(source);
        return ms;
    }

    public override void WriteUtf8ToStream(string contents, Stream stream)
    {
        stream.Write(ToUtf8(contents).Span);
    }
}    

#endif
