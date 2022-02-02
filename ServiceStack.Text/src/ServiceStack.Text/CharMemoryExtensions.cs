using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ServiceStack.Text 
{
    public static class CharMemoryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty(this ReadOnlyMemory<char> value) => value.IsEmpty;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWhiteSpace(this ReadOnlyMemory<char> value)
        {
            var span = value.Span;
            for (int i = 0; i < span.Length; i++)
            {
                if (!char.IsWhiteSpace(span[i]))
                    return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrWhiteSpace(this ReadOnlyMemory<char> value) => value.IsEmpty || value.IsWhiteSpace();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<char> Advance(this ReadOnlyMemory<char> text, int to) => text.Slice(to, text.Length - to);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<char> AdvancePastWhitespace(this ReadOnlyMemory<char> literal)
        {
            var span = literal.Span;
            var i = 0;
            while (i < span.Length && char.IsWhiteSpace(span[i]))
                i++;

            return i == 0 ? literal : literal.Slice(i < literal.Length ? i : literal.Length);
        }

        public static ReadOnlyMemory<char> AdvancePastChar(this ReadOnlyMemory<char> literal, char delim)
        {
            var i = 0;
            var c = (char) 0;
            var span = literal.Span;
            while (i < span.Length && (c = span[i]) != delim)
                i++;

            if (c == delim)
                return literal.Slice(i + 1);

            return i == 0 ? literal : literal.Slice(i < literal.Length ? i : literal.Length);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ParseBoolean(this ReadOnlyMemory<char> value) => MemoryProvider.Instance.ParseBoolean(value.Span);

        public static bool TryParseBoolean(this ReadOnlyMemory<char> value, out bool result) =>
            MemoryProvider.Instance.TryParseBoolean(value.Span, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseDecimal(this ReadOnlyMemory<char> value, out decimal result) =>
            MemoryProvider.Instance.TryParseDecimal(value.Span, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseFloat(this ReadOnlyMemory<char> value, out float result) => 
            MemoryProvider.Instance.TryParseFloat(value.Span, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseDouble(this ReadOnlyMemory<char> value, out double result) => 
            MemoryProvider.Instance.TryParseDouble(value.Span, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal ParseDecimal(this ReadOnlyMemory<char> value) => 
            MemoryProvider.Instance.ParseDecimal(value.Span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ParseFloat(this ReadOnlyMemory<char> value) => 
            MemoryProvider.Instance.ParseFloat(value.Span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ParseDouble(this ReadOnlyMemory<char> value) => 
            MemoryProvider.Instance.ParseDouble(value.Span);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ParseSByte(this ReadOnlyMemory<char> value) => 
            MemoryProvider.Instance.ParseSByte(value.Span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ParseByte(this ReadOnlyMemory<char> value) => 
            MemoryProvider.Instance.ParseByte(value.Span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ParseInt16(this ReadOnlyMemory<char> value) => 
            MemoryProvider.Instance.ParseInt16(value.Span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ParseUInt16(this ReadOnlyMemory<char> value) => 
            MemoryProvider.Instance.ParseUInt16(value.Span);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ParseInt32(this ReadOnlyMemory<char> value) => 
            MemoryProvider.Instance.ParseInt32(value.Span);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ParseUInt32(this ReadOnlyMemory<char> value) => 
            MemoryProvider.Instance.ParseUInt32(value.Span);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ParseInt64(this ReadOnlyMemory<char> value) => 
            MemoryProvider.Instance.ParseInt64(value.Span);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ParseUInt64(this ReadOnlyMemory<char> value) => 
            MemoryProvider.Instance.ParseUInt64(value.Span);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid ParseGuid(this ReadOnlyMemory<char> value) =>
            MemoryProvider.Instance.ParseGuid(value.Span);
        
        public static ReadOnlyMemory<char> LeftPart(this ReadOnlyMemory<char> strVal, char needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.IndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(0, pos);
        }

        public static ReadOnlyMemory<char> LeftPart(this ReadOnlyMemory<char> strVal, string needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.IndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(0, pos);
        }

        public static ReadOnlyMemory<char> RightPart(this ReadOnlyMemory<char> strVal, char needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.IndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(pos + 1);
        }

        public static ReadOnlyMemory<char> RightPart(this ReadOnlyMemory<char> strVal, string needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.IndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(pos + needle.Length);
        }

        public static ReadOnlyMemory<char> LastLeftPart(this ReadOnlyMemory<char> strVal, char needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.LastIndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(0, pos);
        }

        public static ReadOnlyMemory<char> LastLeftPart(this ReadOnlyMemory<char> strVal, string needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.LastIndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(0, pos);
        }

        public static ReadOnlyMemory<char> LastRightPart(this ReadOnlyMemory<char> strVal, char needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.LastIndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(pos + 1);
        }

        public static ReadOnlyMemory<char> LastRightPart(this ReadOnlyMemory<char> strVal, string needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.LastIndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(pos + needle.Length);
        }
        
        public static bool TryReadLine(this ReadOnlyMemory<char> text, out ReadOnlyMemory<char> line, ref int startIndex)
        {
            if (startIndex >= text.Length)
            {
                line = TypeConstants.NullStringMemory;
                return false;
            }

            text = text.Slice(startIndex);

            var nextLinePos = text.Span.IndexOfAny('\r', '\n');
            if (nextLinePos == -1)
            {
                var nextLine = text.Slice(0, text.Length);
                startIndex += text.Length;
                line = nextLine;
                return true;
            }
            else
            {
                var nextLine = text.Slice(0, nextLinePos);

                startIndex += nextLinePos + 1;

                var span = text.Span;
                if (span[nextLinePos] == '\r' && span.Length > nextLinePos + 1 && span[nextLinePos + 1] == '\n')
                    startIndex += 1;

                line = nextLine;
                return true;
            }
        }

        public static bool TryReadPart(this ReadOnlyMemory<char> text, ReadOnlyMemory<char> needle, out ReadOnlyMemory<char> part, ref int startIndex)
        {
            if (startIndex >= text.Length)
            {
                part = TypeConstants.NullStringMemory;
                return false;
            }

            text = text.Slice(startIndex);
            var nextPartPos = text.Span.IndexOf(needle.Span);
            if (nextPartPos == -1)
            {
                var nextPart = text.Slice(0, text.Length);
                startIndex += text.Length;
                part = nextPart;
                return true;
            }
            else
            {
                var nextPart = text.Slice(0, nextPartPos);
                startIndex += nextPartPos + needle.Length;
                part = nextPart;
                return true;
            }
        }
        
        public static void SplitOnFirst(this ReadOnlyMemory<char> strVal, char needle, out ReadOnlyMemory<char> first, out ReadOnlyMemory<char> last)
        {
            first = default;
            last = default;
            if (strVal.IsEmpty) return;
            
            var pos = strVal.Span.IndexOf(needle);
            if (pos == -1)
            {
                first = strVal;
            }
            else
            {
                first = strVal.Slice(0, pos);
                last = strVal.Slice(pos + 1);
            }
        }

        public static void SplitOnFirst(this ReadOnlyMemory<char> strVal, ReadOnlyMemory<char> needle, out ReadOnlyMemory<char> first, out ReadOnlyMemory<char> last)
        {
            first = default;
            last = default;
            if (strVal.IsEmpty) return;
            
            var pos = strVal.Span.IndexOf(needle.Span);
            if (pos == -1)
            {
                first = strVal;
            }
            else
            {
                first = strVal.Slice(0, pos);
                last = strVal.Slice(pos + needle.Length);
            }
        }

        public static void SplitOnLast(this ReadOnlyMemory<char> strVal, char needle, out ReadOnlyMemory<char> first, out ReadOnlyMemory<char> last)
        {
            first = default;
            last = default;
            if (strVal.IsEmpty) return;
            
            var pos = strVal.Span.LastIndexOf(needle);
            if (pos == -1)
            {
                first = strVal;
            }
            else
            {
                first = strVal.Slice(0, pos);
                last = strVal.Slice(pos + 1);
            }
        }

        public static void SplitOnLast(this ReadOnlyMemory<char> strVal, ReadOnlyMemory<char> needle, out ReadOnlyMemory<char> first, out ReadOnlyMemory<char> last)
        {
            first = default;
            last = default;
            if (strVal.IsEmpty) return;
            
            var pos = strVal.Span.LastIndexOf(needle.Span);
            if (pos == -1)
            {
                first = strVal;
            }
            else
            {
                first = strVal.Slice(0, pos);
                last = strVal.Slice(pos + needle.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this ReadOnlyMemory<char> value, char needle) => value.Span.IndexOf(needle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this ReadOnlyMemory<char> value, string needle) => value.Span.IndexOf(needle.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this ReadOnlyMemory<char> value, char needle, int start)
        {
            var pos = value.Slice(start).Span.IndexOf(needle);
            return pos == -1 ? -1 : start + pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this ReadOnlyMemory<char> value, string needle, int start)
        {
            var pos = value.Slice(start).Span.IndexOf(needle.AsSpan());
            return pos == -1 ? -1 : start + pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf(this ReadOnlyMemory<char> value, char needle) => value.Span.LastIndexOf(needle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf(this ReadOnlyMemory<char> value, string needle) => value.Span.LastIndexOf(needle.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf(this ReadOnlyMemory<char> value, char needle, int start)
        {
            var pos = value.Slice(start).Span.LastIndexOf(needle);
            return pos == -1 ? -1 : start + pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf(this ReadOnlyMemory<char> value, string needle, int start)
        {
            var pos = value.Slice(start).Span.LastIndexOf(needle.AsSpan());
            return pos == -1 ? -1 : start + pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith(this ReadOnlyMemory<char> value, string other) => value.Span.StartsWith(other.AsSpan(), StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith(this ReadOnlyMemory<char> value, string other, StringComparison comparison) => value.Span.StartsWith(other.AsSpan(), comparison);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWith(this ReadOnlyMemory<char> value, string other) => value.Span.EndsWith(other.AsSpan(), StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWith(this ReadOnlyMemory<char> value, string other, StringComparison comparison) => value.Span.EndsWith(other.AsSpan(), comparison);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsOrdinal(this ReadOnlyMemory<char> value, string other) => value.Span.Equals(other.AsSpan(), StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsOrdinal(this ReadOnlyMemory<char> value, ReadOnlyMemory<char> other) => value.Span.Equals(other.Span, StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<char> SafeSlice(this ReadOnlyMemory<char> value, int startIndex) => SafeSlice(value, startIndex, value.Length);

        public static ReadOnlyMemory<char> SafeSlice(this ReadOnlyMemory<char> value, int startIndex, int length)
        {
            if (value.IsEmpty) return TypeConstants.NullStringMemory;
            if (startIndex < 0) startIndex = 0;
            if (value.Length >= startIndex + length)
                return value.Slice(startIndex, length);

            return value.Length > startIndex ? value.Slice(startIndex) : TypeConstants.NullStringMemory;
        }

        public static string SubstringWithEllipsis(this ReadOnlyMemory<char> value, int startIndex, int length)
        {
            if (value.IsEmpty) return string.Empty;
            var str = value.Slice(startIndex, length);
            return str.Length == length
                ? str + "..."
                : str.ToString();
        }

        public static ReadOnlyMemory<byte> ToUtf8(this ReadOnlyMemory<char> chars) =>
            MemoryProvider.Instance.ToUtf8(chars.Span);

        public static ReadOnlyMemory<char> FromUtf8(this ReadOnlyMemory<byte> bytes) =>
            MemoryProvider.Instance.FromUtf8(bytes.Span);
    }
}