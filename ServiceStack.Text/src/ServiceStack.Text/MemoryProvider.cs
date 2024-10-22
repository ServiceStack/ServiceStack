using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;

namespace ServiceStack.Text;

public abstract class MemoryProvider
{
    public static MemoryProvider Instance =
#if NET6_0_OR_GREATER
        NetCoreMemory.Provider;
#else
            DefaultMemory.Provider;
#endif

    internal const string BadFormat = "Input string was not in a correct format.";
    internal const string OverflowMessage = "Value was either too large or too small for an {0}.";

    public abstract bool TryParseBoolean(ReadOnlySpan<char> value, out bool result);
    public abstract bool ParseBoolean(ReadOnlySpan<char> value);

    public abstract bool TryParseDecimal(ReadOnlySpan<char> value, out decimal result);
    public abstract decimal ParseDecimal(ReadOnlySpan<char> value);
    public abstract decimal ParseDecimal(ReadOnlySpan<char> value, bool allowThousands);

    public abstract bool TryParseFloat(ReadOnlySpan<char> value, out float result);
    public abstract float ParseFloat(ReadOnlySpan<char> value);

    public abstract bool TryParseDouble(ReadOnlySpan<char> value, out double result);
    public abstract double ParseDouble(ReadOnlySpan<char> value);

    public abstract sbyte ParseSByte(ReadOnlySpan<char> value);
    public abstract byte ParseByte(ReadOnlySpan<char> value);
    public abstract short ParseInt16(ReadOnlySpan<char> value);
    public abstract ushort ParseUInt16(ReadOnlySpan<char> value);
    public abstract int ParseInt32(ReadOnlySpan<char> value);
    public abstract uint ParseUInt32(ReadOnlySpan<char> value);
    public abstract uint ParseUInt32(ReadOnlySpan<char> value, NumberStyles style);
    public abstract long ParseInt64(ReadOnlySpan<char> value);
    public abstract ulong ParseUInt64(ReadOnlySpan<char> value);

    public abstract Guid ParseGuid(ReadOnlySpan<char> value);

    public abstract byte[] ParseBase64(ReadOnlySpan<char> value);

    public abstract string ToBase64(ReadOnlyMemory<byte> value);

    public abstract void Write(Stream stream, ReadOnlyMemory<char> value);
    public abstract void Write(Stream stream, ReadOnlyMemory<byte> value);

    public abstract Task WriteAsync(Stream stream, ReadOnlyMemory<char> value, CancellationToken token = default);
    public abstract Task WriteAsync(Stream stream, ReadOnlyMemory<byte> value, CancellationToken token = default);

    public abstract Task WriteAsync(Stream stream, ReadOnlySpan<char> value, CancellationToken token = default);

    public abstract object Deserialize(Stream stream, Type type, DeserializeStringSpanDelegate deserializer);

    public abstract Task<object> DeserializeAsync(Stream stream, Type type,
        DeserializeStringSpanDelegate deserializer);

    public abstract StringBuilder Append(StringBuilder sb, ReadOnlySpan<char> value);

    public abstract int GetUtf8CharCount(ReadOnlySpan<byte> bytes);
    public abstract int GetUtf8ByteCount(ReadOnlySpan<char> chars);

    public abstract ReadOnlyMemory<byte> ToUtf8(ReadOnlySpan<char> source);
    public abstract ReadOnlyMemory<char> FromUtf8(ReadOnlySpan<byte> source);

    public abstract int ToUtf8(ReadOnlySpan<char> source, Span<byte> destination);
    public abstract int FromUtf8(ReadOnlySpan<byte> source, Span<char> destination);

    public abstract byte[] ToUtf8Bytes(ReadOnlySpan<char> source);
    public abstract string FromUtf8Bytes(ReadOnlySpan<byte> source);
    public abstract MemoryStream ToMemoryStream(ReadOnlySpan<byte> source);

    public abstract void WriteUtf8ToStream(string contents, Stream stream);
}