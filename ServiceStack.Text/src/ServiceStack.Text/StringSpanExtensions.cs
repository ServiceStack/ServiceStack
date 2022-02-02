using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Text
{
    /// <summary>
    /// Helpful extensions on ReadOnlySpan&lt;char&gt;
    /// Previous extensions on StringSegment available from: https://gist.github.com/mythz/9825689f0db7464d1d541cb62954614c
    /// </summary>
    public static class StringSpanExtensions
    {
        /// <summary>
        /// Returns null if Length == 0, string.Empty if value[0] == NonWidthWhitespace, otherwise returns value.ToString()
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Value(this ReadOnlySpan<char> value) => value.IsEmpty 
            ? null 
            : value.Length == 1 && value[0] == TypeConstants.NonWidthWhiteSpace 
                ? ""
                : value.ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static object Value(this object obj) =>
            obj is string value && value.Length == 1 && value[0] == TypeConstants.NonWidthWhiteSpace
                ? ""
                : obj;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty(this ReadOnlySpan<char> value) => value.IsEmpty || (value.Length == 1 && value[0] == TypeConstants.NonWidthWhiteSpace);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrWhiteSpace(this ReadOnlySpan<char> value) => value.IsNullOrEmpty() || value.IsWhiteSpace();

        [Obsolete("Use value[index]")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char GetChar(this ReadOnlySpan<char> value, int index) => value[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Substring(this ReadOnlySpan<char> value, int pos) => value.Slice(pos, value.Length - pos).ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Substring(this ReadOnlySpan<char> value, int pos, int length) => value.Slice(pos, length).ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareIgnoreCase(this ReadOnlySpan<char> value, ReadOnlySpan<char> text) => value.Equals(text, StringComparison.OrdinalIgnoreCase);

        public static ReadOnlySpan<char> FromCsvField(this ReadOnlySpan<char> text)
        {
            //TODO replace with native Replace() when exists
            if (text.IsNullOrEmpty())
                return text;
            
            var delim = CsvConfig.ItemDelimiterString;
            if (delim.Length == 1)
            {
                if (text[0] != delim[0])
                    return text;
            }
            else if (!text.StartsWith(delim.AsSpan(), StringComparison.Ordinal))
            {
                return text;
            }
            
            var ret = text.Slice(CsvConfig.ItemDelimiterString.Length, text.Length - CsvConfig.EscapedItemDelimiterString.Length)
                .ToString().Replace(CsvConfig.EscapedItemDelimiterString, CsvConfig.ItemDelimiterString);
            
            if (ret == string.Empty)
                return TypeConstants.EmptyStringSpan;

            return ret.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ParseBoolean(this ReadOnlySpan<char> value)
        {
            //Lots of kids like to use '1', HTML checkboxes use 'on' as a soft convention
            switch (value.Length)
            {
                case 0:
                    return false;
                case 1:
                    switch (value[0])
                    {
                        case '1':
                        case 't':
                        case 'T':
                        case 'y':
                        case 'Y':
                            return true;
                        case '0':
                        case 'f':
                        case 'F':
                        case 'n':
                        case 'N':
                            return false;
                    }
                    break;
                case 2:
                    if (value[0] == 'o' && value[1] == 'n')
                        return true;
                    break;
                case 3:
                    if (value[0] == 'o' && value[1] == 'f' && value[1] == 'f')
                        return false;
                    break;
            }

            return MemoryProvider.Instance.ParseBoolean(value);
        }

        public static bool TryParseBoolean(this ReadOnlySpan<char> value, out bool result) =>
            MemoryProvider.Instance.TryParseBoolean(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseDecimal(this ReadOnlySpan<char> value, out decimal result) =>
            MemoryProvider.Instance.TryParseDecimal(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseFloat(this ReadOnlySpan<char> value, out float result) => 
            MemoryProvider.Instance.TryParseFloat(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseDouble(this ReadOnlySpan<char> value, out double result) => 
            MemoryProvider.Instance.TryParseDouble(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal ParseDecimal(this ReadOnlySpan<char> value) => 
            MemoryProvider.Instance.ParseDecimal(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal ParseDecimal(this ReadOnlySpan<char> value, bool allowThousands) => 
            MemoryProvider.Instance.ParseDecimal(value, allowThousands);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ParseFloat(this ReadOnlySpan<char> value) => 
            MemoryProvider.Instance.ParseFloat(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ParseDouble(this ReadOnlySpan<char> value) => 
            MemoryProvider.Instance.ParseDouble(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ParseSByte(this ReadOnlySpan<char> value) =>
            SignedInteger<sbyte>.ParseSByte(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ParseByte(this ReadOnlySpan<char> value) => 
            UnsignedInteger<byte>.ParseByte(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ParseInt16(this ReadOnlySpan<char> value) => 
            SignedInteger<short>.ParseInt16(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ParseUInt16(this ReadOnlySpan<char> value) => 
            UnsignedInteger<ushort>.ParseUInt16(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ParseInt32(this ReadOnlySpan<char> value) =>
            SignedInteger<int>.ParseInt32(value);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ParseUInt32(this ReadOnlySpan<char> value) => 
            UnsignedInteger<uint>.ParseUInt32(value);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ParseInt64(this ReadOnlySpan<char> value) => 
            SignedInteger<long>.ParseInt64(value);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ParseUInt64(this ReadOnlySpan<char> value) => 
            UnsignedInteger<ulong>.ParseUInt64(value);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid ParseGuid(this ReadOnlySpan<char> value) =>
            DefaultMemory.Provider.ParseGuid(value);
        
        public static object ParseSignedInteger(this ReadOnlySpan<char> value)
        {
            var longValue = ParseInt64(value);
            if (longValue >= int.MinValue && longValue <= int.MaxValue)
                return (int)longValue;
            return longValue;
        }
        
        public static bool TryReadLine(this ReadOnlySpan<char> text, out ReadOnlySpan<char> line, ref int startIndex)
        {
            if (startIndex >= text.Length)
            {
                line = TypeConstants.NullStringSpan;
                return false;
            }

            text = text.Slice(startIndex);

            var nextLinePos = text.IndexOfAny('\r', '\n');
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

                if (text[nextLinePos] == '\r' && text.Length > nextLinePos + 1 && text[nextLinePos + 1] == '\n')
                    startIndex += 1;

                line = nextLine;
                return true;
            }
        }

        public static bool TryReadPart(this ReadOnlySpan<char> text, string needle, out ReadOnlySpan<char> part, ref int startIndex)
        {
            if (startIndex >= text.Length)
            {
                part = TypeConstants.NullStringSpan;
                return false;
            }

            text = text.Slice(startIndex);
            var nextPartPos = text.IndexOf(needle);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> Advance(this ReadOnlySpan<char> text, int to) => text.Slice(to, text.Length - to);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> AdvancePastWhitespace(this ReadOnlySpan<char> literal)
        {
            var i = 0;
            while (i < literal.Length && char.IsWhiteSpace(literal[i]))
                i++;

            return i == 0 ? literal : literal.Slice(i < literal.Length ? i : literal.Length);
        }

        public static ReadOnlySpan<char> AdvancePastChar(this ReadOnlySpan<char> literal, char delim)
        {
            var i = 0;
            var c = (char) 0;
            while (i < literal.Length && (c = literal[i]) != delim)
                i++;

            if (c == delim)
                return literal.Slice(i + 1);

            return i == 0 ? literal : literal.Slice(i < literal.Length ? i : literal.Length);
        }

        [Obsolete("Use Slice()")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> Subsegment(this ReadOnlySpan<char> text, int startPos) => text.Slice(startPos, text.Length - startPos);

        [Obsolete("Use Slice()")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> Subsegment(this ReadOnlySpan<char> text, int startPos, int length) => text.Slice(startPos, length);

        public static ReadOnlySpan<char> LeftPart(this ReadOnlySpan<char> strVal, char needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.IndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(0, pos);
        }

        public static ReadOnlySpan<char> LeftPart(this ReadOnlySpan<char> strVal, string needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.IndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(0, pos);
        }

        public static ReadOnlySpan<char> RightPart(this ReadOnlySpan<char> strVal, char needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.IndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(pos + 1);
        }

        public static ReadOnlySpan<char> RightPart(this ReadOnlySpan<char> strVal, string needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.IndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(pos + needle.Length);
        }

        public static ReadOnlySpan<char> LastLeftPart(this ReadOnlySpan<char> strVal, char needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.LastIndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(0, pos);
        }

        public static ReadOnlySpan<char> LastLeftPart(this ReadOnlySpan<char> strVal, string needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.LastIndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(0, pos);
        }

        public static ReadOnlySpan<char> LastRightPart(this ReadOnlySpan<char> strVal, char needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.LastIndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(pos + 1);
        }

        public static ReadOnlySpan<char> LastRightPart(this ReadOnlySpan<char> strVal, string needle)
        {
            if (strVal.IsEmpty) return strVal;
            var pos = strVal.LastIndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Slice(pos + needle.Length);
        }

        public static void SplitOnFirst(this ReadOnlySpan<char> strVal, char needle, out ReadOnlySpan<char> first, out ReadOnlySpan<char> last)
        {
            first = default;
            last = default;
            if (strVal.IsEmpty) return;
            
            var pos = strVal.IndexOf(needle);
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

        public static void SplitOnFirst(this ReadOnlySpan<char> strVal, string needle, out ReadOnlySpan<char> first, out ReadOnlySpan<char> last)
        {
            first = default;
            last = default;
            if (strVal.IsEmpty) return;
            
            var pos = strVal.IndexOf(needle);
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

        public static void SplitOnLast(this ReadOnlySpan<char> strVal, char needle, out ReadOnlySpan<char> first, out ReadOnlySpan<char> last)
        {
            first = default;
            last = default;
            if (strVal.IsEmpty) return;
            
            var pos = strVal.LastIndexOf(needle);
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

        public static void SplitOnLast(this ReadOnlySpan<char> strVal, string needle, out ReadOnlySpan<char> first, out ReadOnlySpan<char> last)
        {
            first = default;
            last = default;
            if (strVal.IsEmpty) return;
            
            var pos = strVal.LastIndexOf(needle);
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

        public static ReadOnlySpan<char> WithoutExtension(this ReadOnlySpan<char> filePath)
        {
            if (filePath.IsNullOrEmpty())
                return TypeConstants.NullStringSpan;

            var extPos = filePath.LastIndexOf('.');
            if (extPos == -1) return filePath;

            var dirPos = filePath.LastIndexOfAny(PclExport.DirSeps);
            return extPos > dirPos ? filePath.Slice(0, extPos) : filePath;
        }

        public static ReadOnlySpan<char> GetExtension(this ReadOnlySpan<char> filePath)
        {
            if (filePath.IsNullOrEmpty())
                return TypeConstants.NullStringSpan;

            var extPos = filePath.LastIndexOf('.');
            return extPos == -1 ? TypeConstants.NullStringSpan : filePath.Slice(extPos);
        }

        public static ReadOnlySpan<char> ParentDirectory(this ReadOnlySpan<char> filePath)
        {
            if (filePath.IsNullOrEmpty())
                return TypeConstants.NullStringSpan;

            var dirSep = filePath.IndexOf(PclExport.Instance.DirSep) != -1
                ? PclExport.Instance.DirSep
                : filePath.IndexOf(PclExport.Instance.AltDirSep) != -1
                    ? PclExport.Instance.AltDirSep
                    : (char)0;

            if (dirSep == 0)
                return TypeConstants.NullStringSpan;
            
            MemoryExtensions.TrimEnd(filePath, dirSep).SplitOnLast(dirSep, out var first, out _); 
            return first;
        }

        public static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> value, params char[] trimChars)
        {
            if (value.IsEmpty) return TypeConstants.NullStringSpan;
            if (trimChars == null || trimChars.Length == 0)
                return value.TrimHelper(1);
            return value.TrimHelper(trimChars, 1);
        }

        private static ReadOnlySpan<char> TrimHelper(this ReadOnlySpan<char> value, int trimType)
        {
            if (value.IsEmpty) return TypeConstants.NullStringSpan;
            int end = value.Length - 1;
            int start = 0;
            if (trimType != 1)
            {
                start = 0;
                while (start < value.Length && char.IsWhiteSpace(value[start]))
                    ++start;
            }
            if (trimType != 0)
            {
                end = value.Length - 1;
                while (end >= start && char.IsWhiteSpace(value[end]))
                    --end;
            }
            return value.CreateTrimmedString(start, end);
        }

        private static ReadOnlySpan<char> TrimHelper(this ReadOnlySpan<char> value, char[] trimChars, int trimType)
        {
            if (value.IsEmpty) return TypeConstants.NullStringSpan;
            int end = value.Length - 1;
            int start = 0;
            if (trimType != 1)
            {
                for (start = 0; start < value.Length; ++start)
                {
                    char ch = value[start];
                    int index = 0;
                    while (index < trimChars.Length && (int)trimChars[index] != (int)ch)
                        ++index;
                    if (index == trimChars.Length)
                        break;
                }
            }
            if (trimType != 0)
            {
                for (end = value.Length - 1; end >= start; --end)
                {
                    char ch = value[end];
                    int index = 0;
                    while (index < trimChars.Length && (int)trimChars[index] != (int)ch)
                        ++index;
                    if (index == trimChars.Length)
                        break;
                }
            }
            return value.CreateTrimmedString(start, end);
        }

        private static ReadOnlySpan<char> CreateTrimmedString(this ReadOnlySpan<char> value, int start, int end)
        {
            if (value.IsEmpty) return TypeConstants.NullStringSpan;
            int length = end - start + 1;
            if (length == value.Length)
                return value;
            if (length == 0)
                return TypeConstants.NullStringSpan;
            return value.Slice(start, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> SafeSlice(this ReadOnlySpan<char> value, int startIndex) => SafeSlice(value, startIndex, value.Length);

        public static ReadOnlySpan<char> SafeSlice(this ReadOnlySpan<char> value, int startIndex, int length)
        {
            if (value.IsEmpty) return TypeConstants.NullStringSpan;
            if (startIndex < 0) startIndex = 0;
            if (value.Length >= startIndex + length)
                return value.Slice(startIndex, length);

            return value.Length > startIndex ? value.Slice(startIndex) : TypeConstants.NullStringSpan;
        }

        public static string SubstringWithEllipsis(this ReadOnlySpan<char> value, int startIndex, int length)
        {
            if (value.IsEmpty) return string.Empty;
            var str = value.SafeSlice(startIndex, length);
            return str.Length == length
                ? str.ToString() + "..."
                : str.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this ReadOnlySpan<char> value, string other) => value.IndexOf(other.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this ReadOnlySpan<char> value, string needle, int start)
        {
            var pos = value.Slice(start).IndexOf(needle.AsSpan());
            return pos == -1 ? -1 : start + pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf(this ReadOnlySpan<char> value, string other) => value.LastIndexOf(other.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf(this ReadOnlySpan<char> value, string needle, int start)
        {
            var pos = value.Slice(start).LastIndexOf(needle.AsSpan());
            return pos == -1 ? -1 : start + pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualTo(this ReadOnlySpan<char> value, string other) => value.Equals(other.AsSpan(), StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualTo(this ReadOnlySpan<char> value, ReadOnlySpan<char> other) => value.Equals(other, StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsOrdinal(this ReadOnlySpan<char> value, string other) => value.Equals(other.AsSpan(), StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith(this ReadOnlySpan<char> value, string other) => value.StartsWith(other.AsSpan(), StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith(this ReadOnlySpan<char> value, string other, StringComparison comparison) => value.StartsWith(other.AsSpan(), comparison);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWith(this ReadOnlySpan<char> value, string other, StringComparison comparison) => value.EndsWith(other.AsSpan(), comparison);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWith(this ReadOnlySpan<char> value, string other) => value.EndsWith(other.AsSpan(), StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsIgnoreCase(this ReadOnlySpan<char> value, ReadOnlySpan<char> other) => value.Equals(other, StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWithIgnoreCase(this ReadOnlySpan<char> value, ReadOnlySpan<char> other) => value.StartsWith(other, StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWithIgnoreCase(this ReadOnlySpan<char> value, ReadOnlySpan<char> other) => value.EndsWith(other, StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteAsync(this Stream stream, ReadOnlySpan<char> value, CancellationToken token = default(CancellationToken)) =>
            MemoryProvider.Instance.WriteAsync(stream, value, token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> SafeSubstring(this ReadOnlySpan<char> value, int startIndex) => SafeSubstring(value, startIndex, value.Length);

        public static ReadOnlySpan<char> SafeSubstring(this ReadOnlySpan<char> value, int startIndex, int length)
        {
            if (value.IsEmpty) return TypeConstants.NullStringSpan;
            if (startIndex < 0) startIndex = 0;
            if (value.Length >= (startIndex + length))
                return value.Slice(startIndex, length);

            return value.Length > startIndex ? value.Slice(startIndex) : TypeConstants.NullStringSpan;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder Append(this StringBuilder sb, ReadOnlySpan<char> value) =>
            MemoryProvider.Instance.Append(sb, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ParseBase64(this ReadOnlySpan<char> value) => MemoryProvider.Instance.ParseBase64(value);

        public static ReadOnlyMemory<byte> ToUtf8(this ReadOnlySpan<char> value) =>
            MemoryProvider.Instance.ToUtf8(value);

        public static ReadOnlyMemory<char> FromUtf8(this ReadOnlySpan<byte> value) =>
            MemoryProvider.Instance.FromUtf8(value);

        public static byte[] ToUtf8Bytes(this ReadOnlySpan<char> value) =>
            MemoryProvider.Instance.ToUtf8Bytes(value);

        public static string FromUtf8Bytes(this ReadOnlySpan<byte> value) =>
            MemoryProvider.Instance.FromUtf8Bytes(value);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<string> ToStringList(this IEnumerable<ReadOnlyMemory<char>> from)
        {
            var to = new List<string>();
            if (from != null)
            {
                foreach (var item in from)
                {
                    to.Add(item.ToString());
                }
            }
            return to;
        }
        
        public static int CountOccurrencesOf(this ReadOnlySpan<char> value, char needle)
        {
            var count = 0;
            var length = value.Length;
            for (var n = length - 1; n >= 0; n--)
            {
                if (value[n] == needle)
                    count++;
            }
            return count;
        }

        public static ReadOnlySpan<char> WithoutBom(this ReadOnlySpan<char> value)
        {
            return value.Length > 0 && value[0] == 65279 
                ? value.Slice(1) 
                : value;
        }

        public static ReadOnlySpan<byte> WithoutBom(this ReadOnlySpan<byte> value)
        {
            return value.Length > 3 && value[0] == 0xEF && value[1] == 0xBB && value[2] == 0xBF 
                ? value.Slice(3) 
                : value;
        }

    }
}
