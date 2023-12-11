using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;
using ServiceStack.Text.Pools;

namespace ServiceStack.Text;

public sealed class DefaultMemory : MemoryProvider
{
    private static DefaultMemory provider;
    public static DefaultMemory Provider => provider ??= new DefaultMemory();
    private DefaultMemory() { }

    public static void Configure() => Instance = Provider;

    public override bool ParseBoolean(ReadOnlySpan<char> value)
    {
        if (!value.TryParseBoolean(out bool result))
            throw new FormatException(BadFormat);

        return result;
    }

    public override bool TryParseBoolean(ReadOnlySpan<char> value, out bool result)
    {
        result = false;

        if (value.CompareIgnoreCase(bool.TrueString.AsSpan()))
        {
            result = true;
            return true;
        }

        return value.CompareIgnoreCase(bool.FalseString.AsSpan());
    }

    public override bool TryParseDecimal(ReadOnlySpan<char> value, out decimal result) =>
        TryParseDecimal(value, allowThousands: true, out result);

    public override decimal ParseDecimal(ReadOnlySpan<char> value) => ParseDecimal(value, allowThousands: true);

    public override decimal ParseDecimal(ReadOnlySpan<char> value, bool allowThousands)
    {
        if (!TryParseDecimal(value, allowThousands, out var result))
            throw new FormatException(BadFormat);

        return result;
    }

    public override bool TryParseFloat(ReadOnlySpan<char> value, out float result) => float.TryParse(
        value.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture,
        out result);

    public override float ParseFloat(ReadOnlySpan<char> value) => float.Parse(value.ToString(),
        NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);

    public override bool TryParseDouble(ReadOnlySpan<char> value, out double result) => double.TryParse(
        value.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture,
        out result);

    public override double ParseDouble(ReadOnlySpan<char> value) => double.Parse(value.ToString(),
        NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);

    public override sbyte ParseSByte(ReadOnlySpan<char> value) => SignedInteger<sbyte>.ParseSByte(value);

    public override byte ParseByte(ReadOnlySpan<char> value) => UnsignedInteger<byte>.ParseByte(value);

    public override short ParseInt16(ReadOnlySpan<char> value) => SignedInteger<short>.ParseInt16(value);

    public override ushort ParseUInt16(ReadOnlySpan<char> value) => UnsignedInteger<ushort>.ParseUInt16(value);

    public override int ParseInt32(ReadOnlySpan<char> value) => SignedInteger<int>.ParseInt32(value);

    public override uint ParseUInt32(ReadOnlySpan<char> value) => UnsignedInteger<uint>.ParseUInt32(value);

    public override uint ParseUInt32(ReadOnlySpan<char> value, NumberStyles style) =>
        uint.Parse(value.ToString(), style);

    public override long ParseInt64(ReadOnlySpan<char> value) => SignedInteger<int>.ParseInt64(value);

    public override ulong ParseUInt64(ReadOnlySpan<char> value) => UnsignedInteger<ulong>.ParseUInt64(value);

    internal static Exception CreateOverflowException(long maxValue) =>
        new OverflowException(string.Format(OverflowMessage, SignedMaxValueToIntType(maxValue)));

    internal static Exception CreateOverflowException(ulong maxValue) =>
        new OverflowException(string.Format(OverflowMessage, UnsignedMaxValueToIntType(maxValue)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string SignedMaxValueToIntType(long maxValue)
    {
        switch (maxValue)
        {
            case sbyte.MaxValue:
                return nameof(SByte);
            case short.MaxValue:
                return nameof(Int16);
            case int.MaxValue:
                return nameof(Int32);
            case long.MaxValue:
                return nameof(Int64);
            default:
                return "Unknown";
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string UnsignedMaxValueToIntType(ulong maxValue)
    {
        switch (maxValue)
        {
            case byte.MaxValue:
                return nameof(Byte);
            case ushort.MaxValue:
                return nameof(UInt16);
            case uint.MaxValue:
                return nameof(UInt32);
            case ulong.MaxValue:
                return nameof(UInt64);
            default:
                return "Unknown";
        }
    }

    public static bool TryParseDecimal(ReadOnlySpan<char> value, bool allowThousands, out decimal result)
    {
        result = 0;

        if (value.Length == 0)
            return false;

        ulong preResult = 0;
        bool isLargeNumber = false;
        int i = 0;
        int end = value.Length;
        var state = ParseState.LeadingWhite;
        bool negative = false;
        bool noIntegerPart = false;
        sbyte scale = 0;

        while (i < end)
        {
            var c = value[i++];

            switch (state)
            {
                case ParseState.LeadingWhite:
                    if (JsonUtils.IsWhiteSpace(c))
                        break;

                    if (c == '-')
                    {
                        negative = true;
                        state = ParseState.Sign;
                    }
                    else if (c == '.')
                    {
                        noIntegerPart = true;
                        state = ParseState.FractionNumber;

                        if (i == end)
                            return false;
                    }
                    else if (c == '0')
                    {
                        state = ParseState.DecimalPoint;
                    }
                    else if (c is > '0' and <= '9')
                    {
                        preResult = (ulong) (c - '0');
                        state = ParseState.Number;
                    }
                    else return false;

                    break;
                case ParseState.Sign:
                    if (c == '.')
                    {
                        noIntegerPart = true;
                        state = ParseState.FractionNumber;

                        if (i == end)
                            return false;
                    }
                    else if (c == '0')
                    {
                        state = ParseState.DecimalPoint;
                    }
                    else if (c is > '0' and <= '9')
                    {
                        preResult = (ulong) (c - '0');
                        state = ParseState.Number;
                    }
                    else return false;

                    break;
                case ParseState.Number:
                    if (c == '.')
                    {
                        state = ParseState.FractionNumber;
                    }
                    else if (c is >= '0' and <= '9')
                    {
                        if (isLargeNumber)
                        {
                            checked
                            {
                                result = 10 * result + (c - '0');
                            }
                        }
                        else
                        {
                            preResult = 10 * preResult + (ulong) (c - '0');
                            if (preResult > ulong.MaxValue / 10 - 10)
                            {
                                isLargeNumber = true;
                                result = preResult;
                            }
                        }
                    }
                    else if (JsonUtils.IsWhiteSpace(c))
                    {
                        state = ParseState.TrailingWhite;
                    }
                    else if (allowThousands && c == ',') { }
                    else return false;

                    break;
                case ParseState.DecimalPoint:
                    if (c == '.')
                    {
                        state = ParseState.FractionNumber;
                    }
                    else return false;

                    break;
                case ParseState.FractionNumber:
                    if (JsonUtils.IsWhiteSpace(c))
                    {
                        if (noIntegerPart)
                            return false;
                        state = ParseState.TrailingWhite;
                    }
                    else if (c == 'e' || c == 'E')
                    {
                        if (noIntegerPart && scale == 0)
                            return false;
                        state = ParseState.Exponent;
                    }
                    else if (c >= '0' && c <= '9')
                    {
                        if (isLargeNumber)
                        {
                            checked
                            {
                                result = 10 * result + (c - '0');
                            }
                        }
                        else
                        {
                            preResult = 10 * preResult + (ulong) (c - '0');
                            if (preResult > ulong.MaxValue / 10 - 10)
                            {
                                isLargeNumber = true;
                                result = preResult;
                            }
                        }

                        scale++;
                    }
                    else return false;

                    break;
                case ParseState.Exponent:
                    bool expNegative = false;
                    if (c == '-')
                    {
                        if (i == end)
                            return false;

                        expNegative = true;
                        c = value[i++];
                    }
                    else if (c == '+')
                    {
                        if (i == end)
                            return false;
                        c = value[i++];
                    }

                    //skip leading zeroes
                    while (c == '0' && i < end) c = value[i++];

                    if (c > '0' && c <= '9')
                    {
                        var exp = SignedInteger<long>.ParseInt64(value.Slice(i - 1, end - i + 1));
                        if (exp is < sbyte.MinValue or > sbyte.MaxValue)
                            return false;

                        if (!expNegative)
                        {
                            exp = (sbyte) -exp;
                        }

                        if (exp >= 0 || scale > -exp)
                        {
                            scale += (sbyte) exp;
                        }
                        else
                        {
                            for (int j = 0; j < -exp - scale; j++)
                            {
                                if (isLargeNumber)
                                {
                                    checked
                                    {
                                        result = 10 * result;
                                    }
                                }
                                else
                                {
                                    preResult = 10 * preResult;
                                    if (preResult > ulong.MaxValue / 10)
                                    {
                                        isLargeNumber = true;
                                        result = preResult;
                                    }
                                }
                            }

                            scale = 0;
                        }

                        //set i to end of string, because ParseInt16 eats number and all trailing whites
                        i = end;
                    }
                    else return false;

                    break;
                case ParseState.TrailingWhite:
                    if (!JsonUtils.IsWhiteSpace(c))
                        return false;
                    break;
            }
        }

        if (!isLargeNumber)
        {
            var mid = (int) (preResult >> 32);
            var lo = (int) (preResult & 0xffffffff);
            result = new decimal(lo, mid, 0, negative, (byte) scale);
        }
        else
        {
            var bits = decimal.GetBits(result);
            result = new decimal(bits[0], bits[1], bits[2], negative, (byte) scale);
        }

        return true;
    }

    private static readonly byte[] lo16 = {
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 0, 1,
        2, 3, 4, 5, 6, 7, 8, 9, 255, 255,
        255, 255, 255, 255, 255, 10, 11, 12, 13, 14,
        15, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 10, 11, 12,
        13, 14, 15
    };

    private static readonly byte[] hi16 = {
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 0, 16,
        32, 48, 64, 80, 96, 112, 128, 144, 255, 255,
        255, 255, 255, 255, 255, 160, 176, 192, 208, 224,
        240, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 160, 176, 192,
        208, 224, 240
    };

    public override Guid ParseGuid(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            throw new FormatException(BadFormat);

        //Guid can be in one of 3 forms:
        //1. General `{dddddddd-dddd-dddd-dddd-dddddddddddd}` or `(dddddddd-dddd-dddd-dddd-dddddddddddd)` 8-4-4-4-12 chars
        //2. Hex `{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}`  8-4-4-8x2 chars
        //3. No style `dddddddddddddddddddddddddddddddd` 32 chars

        int i = 0;
        int end = value.Length;
        while (i < end && JsonUtils.IsWhiteSpace(value[i])) i++;

        if (i == end)
            throw new FormatException(BadFormat);

        var result = ParseGeneralStyleGuid(value.Slice(i, end - i), out var guidLen);
        i += guidLen;

        while (i < end && JsonUtils.IsWhiteSpace(value[i])) i++;

        if (i < end)
            throw new FormatException(BadFormat);

        return result;
    }

    public override void Write(Stream stream, ReadOnlyMemory<char> value)
    {
        var bytes = BufferPool.GetBuffer(Encoding.UTF8.GetMaxByteCount(value.Length));
        try
        {
            var chars = value.ToArray();
            int bytesCount = Encoding.UTF8.GetBytes(chars, 0, chars.Length, bytes, 0);
            stream.Write(bytes, 0, bytesCount);
        }
        finally
        {
            BufferPool.ReleaseBufferToPool(ref bytes);
        }
    }

    public override void Write(Stream stream, ReadOnlyMemory<byte> value)
    {
        if (MemoryMarshal.TryGetArray(value, out var segment) && segment.Array != null)
        {
            byte[] bytes = BufferPool.GetBuffer(segment.Count);
            try
            {
                stream.Write(segment.Array, 0, segment.Count);
            }
            finally
            {
                BufferPool.ReleaseBufferToPool(ref bytes);
            }
        }
        else
        {
            var bytes = value.ToArray();
            stream.Write(bytes, 0, value.Length);
        }
    }

    public override Task WriteAsync(Stream stream, ReadOnlySpan<char> value, CancellationToken token = default)
    {
        // encode the span into a buffer; this should never fail, so if it does: something
        // is very very ill; don't stress about returning to the pool
        byte[] bytes = BufferPool.GetBuffer(Encoding.UTF8.GetMaxByteCount(value.Length));
        var chars = value.ToArray();
        int bytesCount = Encoding.UTF8.GetBytes(chars, 0, chars.Length, bytes, 0);
        // now do the write async - this returns to the pool
        return WriteAsyncAndReturn(stream, bytes, 0, bytesCount, token);
    }
        
    private static async Task WriteAsyncAndReturn(Stream stream, byte[] bytes, int offset, int count, CancellationToken token)
    {
        try
        {
            await stream.WriteAsync(bytes, offset, count, token).ConfigAwait();
        }
        finally
        {
            BufferPool.ReleaseBufferToPool(ref bytes);
        }
    }
        
    public override Task WriteAsync(Stream stream, ReadOnlyMemory<char> value, CancellationToken token = default) =>
        WriteAsync(stream, value.Span, token);

    public override async Task WriteAsync(Stream stream, ReadOnlyMemory<byte> value, CancellationToken token = default)
    {
        byte[] bytes = BufferPool.GetBuffer(value.Length);
        try
        {
            value.CopyTo(bytes);
            if (stream is MemoryStream ms)
            {
                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                ms.Write(bytes, 0, value.Length);
            }
            else
            {
                await stream.WriteAsync(bytes, 0, value.Length, token).ConfigAwait();
            }
        }
        finally
        {
            BufferPool.ReleaseBufferToPool(ref bytes);
        }
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

    public override async Task<object> DeserializeAsync(Stream stream, Type type,
        DeserializeStringSpanDelegate deserializer)
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

    private static object Deserialize(MemoryStream ms, bool fromPool, Type type,
        DeserializeStringSpanDelegate deserializer)
    {
        var bytes = ms.GetBufferAsBytes();
        var utf8 = CharPool.GetBuffer(Encoding.UTF8.GetCharCount(bytes, 0, (int) ms.Length));
        try
        {
            var charsWritten = Encoding.UTF8.GetChars(bytes, 0, (int) ms.Length, utf8, 0);
            var ret = deserializer(type, new ReadOnlySpan<char>(utf8, 0, charsWritten).WithoutBom());
            return ret;
        }
        finally
        {
            CharPool.ReleaseBufferToPool(ref utf8);

            if (fromPool)
                ms.Dispose();
        }
    }

    public override byte[] ParseBase64(ReadOnlySpan<char> value)
    {
        return Convert.FromBase64String(value.ToString());
    }

    public override string ToBase64(ReadOnlyMemory<byte> value)
    {
        return MemoryMarshal.TryGetArray(value, out var segment)
            ? Convert.ToBase64String(segment.Array, 0, segment.Count)
            : Convert.ToBase64String(value.ToArray());
    }

    public override StringBuilder Append(StringBuilder sb, ReadOnlySpan<char> value)
    {
        return sb.Append(value.ToArray());
    }

    public override int GetUtf8CharCount(ReadOnlySpan<byte> bytes) =>
        Encoding.UTF8.GetCharCount(bytes.ToArray()); //SLOW

    public override int GetUtf8ByteCount(ReadOnlySpan<char> chars) =>
        Encoding.UTF8.GetByteCount(chars.ToArray()); //SLOW

    public override ReadOnlyMemory<byte> ToUtf8(ReadOnlySpan<char> source)
    {
        var chars = source.ToArray();
        var bytes = new byte[Encoding.UTF8.GetByteCount(chars)];
        var bytesWritten = Encoding.UTF8.GetBytes(chars, 0, source.Length, bytes, 0);
        return new ReadOnlyMemory<byte>(bytes, 0, bytesWritten);
    }

    public override ReadOnlyMemory<char> FromUtf8(ReadOnlySpan<byte> source)
    {
        var bytes = source.WithoutBom().ToArray();
        var chars = new char[Encoding.UTF8.GetCharCount(bytes)];
        var charsWritten = Encoding.UTF8.GetChars(bytes, 0, bytes.Length, chars, 0);
        return new ReadOnlyMemory<char>(chars, 0, charsWritten);
    }

    public override int ToUtf8(ReadOnlySpan<char> source, Span<byte> destination)
    {
        var chars = source.ToArray();
        var bytes = destination.ToArray();
        var bytesWritten = Encoding.UTF8.GetBytes(chars, 0, source.Length, bytes, 0);
        new ReadOnlySpan<byte>(bytes, 0, bytesWritten).CopyTo(destination);
        return bytesWritten;
    }

    public override int FromUtf8(ReadOnlySpan<byte> source, Span<char> destination)
    {
        var bytes = source.WithoutBom().ToArray();
        var chars = destination.ToArray();
        var charsWritten = Encoding.UTF8.GetChars(bytes, 0, bytes.Length, chars, 0);
        new ReadOnlySpan<char>(chars, 0, charsWritten).CopyTo(destination);
        return charsWritten;
    }

    public override byte[] ToUtf8Bytes(ReadOnlySpan<char> source) => Encoding.UTF8.GetBytes(source.ToArray());

    public override string FromUtf8Bytes(ReadOnlySpan<byte> source) => Encoding.UTF8.GetString(source.WithoutBom().ToArray());
        
    public override MemoryStream ToMemoryStream(ReadOnlySpan<byte> source) => 
        MemoryStreamFactory.GetStream(source.ToArray());

    public override void WriteUtf8ToStream(string contents, Stream stream)
    {
        var bytes = ToUtf8Bytes(contents.AsSpan());
        stream.Write(bytes, 0, bytes.Length);
    }

    private static Guid ParseGeneralStyleGuid(ReadOnlySpan<char> value, out int len)
    {
        var buf = value;
        var n = 0;

        int dash = 0;
        len = 32;
        bool hasParenthesis = false;

        if (value.Length < len)
            throw new FormatException(BadFormat);

        var cs = value[0];
        if (cs == '{' || cs == '(')
        {
            n++;
            len += 2;
            hasParenthesis = true;

            if (buf[8 + n] != '-')
                throw new FormatException(BadFormat);
        }

        if (buf[8 + n] == '-')
        {
            if (buf[13 + n] != '-'
                || buf[18 + n] != '-'
                || buf[23 + n] != '-')
                throw new FormatException(BadFormat);

            len += 4;
            dash = 1;
        }

        if (value.Length < len)
            throw new FormatException(BadFormat);

        if (hasParenthesis)
        {
            var ce = buf[len - 1];

            if ((cs != '{' || ce != '}') && (cs != '(' || ce != ')'))
                throw new FormatException(BadFormat);
        }

        int a;
        short b, c;
        byte d, e, f, g, h, i, j, k;

        byte a1 = ParseHexByte(buf[n], buf[n + 1]);
        n += 2;
        byte a2 = ParseHexByte(buf[n], buf[n + 1]);
        n += 2;
        byte a3 = ParseHexByte(buf[n], buf[n + 1]);
        n += 2;
        byte a4 = ParseHexByte(buf[n], buf[n + 1]);
        a = (a1 << 24) + (a2 << 16) + (a3 << 8) + a4;
        n += 2 + dash;

        byte b1 = ParseHexByte(buf[n], buf[n + 1]);
        n += 2;
        byte b2 = ParseHexByte(buf[n], buf[n + 1]);
        b = (short) ((b1 << 8) + b2);
        n += 2 + dash;

        byte c1 = ParseHexByte(buf[n], buf[n + 1]);
        n += 2;
        byte c2 = ParseHexByte(buf[n], buf[n + 1]);
        c = (short) ((c1 << 8) + c2);
        n += 2 + dash;

        d = ParseHexByte(buf[n], buf[n + 1]);
        n += 2;
        e = ParseHexByte(buf[n], buf[n + 1]);
        n += 2 + dash;

        f = ParseHexByte(buf[n], buf[n + 1]);
        n += 2;
        g = ParseHexByte(buf[n], buf[n + 1]);
        n += 2;
        h = ParseHexByte(buf[n], buf[n + 1]);
        n += 2;
        i = ParseHexByte(buf[n], buf[n + 1]);
        n += 2;
        j = ParseHexByte(buf[n], buf[n + 1]);
        n += 2;
        k = ParseHexByte(buf[n], buf[n + 1]);

        return new Guid(a, b, c, d, e, f, g, h, i, j, k);
    }

    private static byte ParseHexByte(char c1, char c2)
    {
        try
        {
            byte lo = lo16[c2];
            byte hi = hi16[c1];

            if (lo == 255 || hi == 255)
                throw new FormatException(BadFormat);

            return (byte) (hi + lo);
        }
        catch (IndexOutOfRangeException)
        {
            throw new FormatException(BadFormat);
        }
    }
}
    
enum ParseState
{
    LeadingWhite,
    Sign,
    Number,
    DecimalPoint,
    FractionNumber,
    Exponent,
    ExponentSign,
    ExponentValue,
    TrailingWhite
}

internal static class SignedInteger<T> where T : struct, IComparable<T>, IEquatable<T>, IConvertible
{
    private static readonly TypeCode typeCode;
    private static readonly long minValue;
    private static readonly long maxValue;

    static SignedInteger()
    {
        typeCode = Type.GetTypeCode(typeof(T));

        switch (typeCode)
        {
            case TypeCode.SByte:
                minValue = sbyte.MinValue;
                maxValue = sbyte.MaxValue;
                break;
            case TypeCode.Int16:
                minValue = short.MinValue;
                maxValue = short.MaxValue;
                break;
            case TypeCode.Int32:
                minValue = int.MinValue;
                maxValue = int.MaxValue;
                break;
            case TypeCode.Int64:
                minValue = long.MinValue;
                maxValue = long.MaxValue;
                break;
            default:
                throw new NotSupportedException($"{typeof(T).Name} is not a signed integer");
        }
    }

    internal static object ParseNullableObject(ReadOnlySpan<char> value)
    {
        if (value.IsNullOrEmpty())
            return null;

        return ParseObject(value);
    }

    internal static object ParseObject(ReadOnlySpan<char> value)
    {
        var result = ParseInt64(value);
        switch (typeCode)
        {
            case TypeCode.SByte:
                return (sbyte) result;
            case TypeCode.Int16:
                return (short) result;
            case TypeCode.Int32:
                return (int) result;
            default:
                return result;
        }
    }

    public static sbyte ParseSByte(ReadOnlySpan<char> value) => (sbyte) ParseInt64(value);
    public static short ParseInt16(ReadOnlySpan<char> value) => (short) ParseInt64(value);
    public static int ParseInt32(ReadOnlySpan<char> value) => (int) ParseInt64(value);

    public static long ParseInt64(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            throw new FormatException(MemoryProvider.BadFormat);

        long result = 0;
        int i = 0;
        int end = value.Length;
        var state = ParseState.LeadingWhite;
        bool negative = false;

        //skip leading whitespaces
        while (i < end && JsonUtils.IsWhiteSpace(value[i])) i++;

        if (i == end)
            throw new FormatException(MemoryProvider.BadFormat);

        //skip leading zeros
        while (i < end && value[i] == '0')
        {
            state = ParseState.Number;
            i++;
        }

        while (i < end)
        {
            var c = value[i++];

            switch (state)
            {
                case ParseState.LeadingWhite:
                    if (c == '-')
                    {
                        negative = true;
                        state = ParseState.Sign;
                    }
                    else if (c == '0')
                    {
                        state = ParseState.TrailingWhite;
                    }
                    else if (c > '0' && c <= '9')
                    {
                        result = -(c - '0');
                        state = ParseState.Number;
                    }
                    else throw new FormatException(MemoryProvider.BadFormat);

                    break;
                case ParseState.Sign:
                    if (c == '0')
                    {
                        state = ParseState.TrailingWhite;
                    }
                    else if (c > '0' && c <= '9')
                    {
                        result = -(c - '0');
                        state = ParseState.Number;
                    }
                    else throw new FormatException(MemoryProvider.BadFormat);

                    break;
                case ParseState.Number:
                    if (c >= '0' && c <= '9')
                    {
                        checked
                        {
                            result = 10 * result - (c - '0');
                        }

                        if (result < minValue
                           ) //check only minvalue, because in absolute value it's greater than maxvalue
                            throw DefaultMemory.CreateOverflowException(maxValue);
                    }
                    else if (JsonUtils.IsWhiteSpace(c))
                    {
                        state = ParseState.TrailingWhite;
                    }
                    else throw new FormatException(MemoryProvider.BadFormat);

                    break;
                case ParseState.TrailingWhite:
                    if (JsonUtils.IsWhiteSpace(c))
                    {
                        state = ParseState.TrailingWhite;
                    }
                    else throw new FormatException(MemoryProvider.BadFormat);

                    break;
            }
        }

        if (state != ParseState.Number && state != ParseState.TrailingWhite)
            throw new FormatException(MemoryProvider.BadFormat);

        if (negative)
            return result;

        checked
        {
            result = -result;
        }

        if (result > maxValue)
            throw DefaultMemory.CreateOverflowException(maxValue);

        return result;
    }
}

internal static class UnsignedInteger<T> where T : struct, IComparable<T>, IEquatable<T>, IConvertible
{
    private static readonly TypeCode typeCode;
    private static readonly ulong maxValue;

    static UnsignedInteger()
    {
        typeCode = Type.GetTypeCode(typeof(T));

        switch (typeCode)
        {
            case TypeCode.Byte:
                maxValue = byte.MaxValue;
                break;
            case TypeCode.UInt16:
                maxValue = ushort.MaxValue;
                break;
            case TypeCode.UInt32:
                maxValue = uint.MaxValue;
                break;
            case TypeCode.UInt64:
                maxValue = ulong.MaxValue;
                break;
            default:
                throw new NotSupportedException($"{typeof(T).Name} is not a signed integer");
        }
    }

    internal static object ParseNullableObject(ReadOnlySpan<char> value)
    {
        if (value.IsNullOrEmpty())
            return null;

        return ParseObject(value);
    }
        
    internal static object ParseObject(ReadOnlySpan<char> value)
    {
        var result = ParseUInt64(value);
        switch (typeCode)
        {
            case TypeCode.Byte:
                return (byte) result;
            case TypeCode.UInt16:
                return (ushort) result;
            case TypeCode.UInt32:
                return (uint) result;
            default:
                return result;
        }
    }

    public static byte ParseByte(ReadOnlySpan<char> value) => (byte) ParseUInt64(value);
    public static ushort ParseUInt16(ReadOnlySpan<char> value) => (ushort) ParseUInt64(value);
    public static uint ParseUInt32(ReadOnlySpan<char> value) => (uint) ParseUInt64(value);

    internal static ulong ParseUInt64(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            throw new FormatException(MemoryProvider.BadFormat);

        ulong result = 0;
        int i = 0;
        int end = value.Length;
        var state = ParseState.LeadingWhite;

        //skip leading whitespaces
        while (i < end && JsonUtils.IsWhiteSpace(value[i])) i++;

        if (i == end)
            throw new FormatException(MemoryProvider.BadFormat);

        //skip leading zeros
        while (i < end && value[i] == '0')
        {
            state = ParseState.Number;
            i++;
        }

        while (i < end)
        {
            var c = value[i++];

            switch (state)
            {
                case ParseState.LeadingWhite:
                    if (JsonUtils.IsWhiteSpace(c))
                        break;
                    if (c == '0')
                    {
                        state = ParseState.TrailingWhite;
                    }
                    else if (c > '0' && c <= '9')
                    {
                        result = (ulong) (c - '0');
                        state = ParseState.Number;
                    }
                    else throw new FormatException(MemoryProvider.BadFormat);


                    break;
                case ParseState.Number:
                    if (c >= '0' && c <= '9')
                    {
                        checked
                        {
                            result = 10 * result + (ulong) (c - '0');
                        }

                        if (result > maxValue
                           ) //check only minvalue, because in absolute value it's greater than maxvalue
                            throw DefaultMemory.CreateOverflowException(maxValue);
                    }
                    else if (JsonUtils.IsWhiteSpace(c))
                    {
                        state = ParseState.TrailingWhite;
                    }
                    else throw new FormatException(MemoryProvider.BadFormat);

                    break;
                case ParseState.TrailingWhite:
                    if (JsonUtils.IsWhiteSpace(c))
                    {
                        state = ParseState.TrailingWhite;
                    }
                    else throw new FormatException(MemoryProvider.BadFormat);

                    break;
            }
        }

        if (state != ParseState.Number && state != ParseState.TrailingWhite)
            throw new FormatException(MemoryProvider.BadFormat);

        return result;
    }    
}