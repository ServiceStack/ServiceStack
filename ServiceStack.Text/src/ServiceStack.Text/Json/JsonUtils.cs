//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ServiceStack.Text.Json
{
    public static class JsonUtils
    {
        public const long MaxInteger = 9007199254740992;
        public const long MinInteger = -9007199254740992;

        public const char EscapeChar = '\\';

        public const char QuoteChar = '"';
        public const string Null = "null";
        public const string True = "true";
        public const string False = "false";

        public const char SpaceChar = ' ';
        public const char TabChar = '\t';
        public const char CarriageReturnChar = '\r';
        public const char LineFeedChar = '\n';
        public const char FormFeedChar = '\f';
        public const char BackspaceChar = '\b';

        /// <summary>
        /// Micro-optimization keep pre-built char arrays saving a .ToCharArray() + function call (see .net implementation of .Write(string))
        /// </summary>
        private static readonly char[] EscapedBackslash = { EscapeChar, EscapeChar };
        private static readonly char[] EscapedTab = { EscapeChar, 't' };
        private static readonly char[] EscapedCarriageReturn = { EscapeChar, 'r' };
        private static readonly char[] EscapedLineFeed = { EscapeChar, 'n' };
        private static readonly char[] EscapedFormFeed = { EscapeChar, 'f' };
        private static readonly char[] EscapedBackspace = { EscapeChar, 'b' };
        private static readonly char[] EscapedQuote = { EscapeChar, QuoteChar };

        public static readonly char[] WhiteSpaceChars = { ' ', TabChar, CarriageReturnChar, LineFeedChar };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWhiteSpace(char c)
        {
            return c == ' ' || (c >= '\x0009' && c <= '\x000d') || c == '\x00a0' || c == '\x0085' || c == TypeConstants.NonWidthWhiteSpace;
        }

        public static void WriteString(TextWriter writer, string value)
        {
            if (value == null)
            {
                writer.Write(Null);
                return;
            }

            var config = JsConfig.GetConfig();
            var escapeHtmlChars = config.EscapeHtmlChars;
            var escapeUnicode = config.EscapeUnicode;

            if (!HasAnyEscapeChars(value, escapeHtmlChars))
            {
                writer.Write(QuoteChar);
                writer.Write(value);
                writer.Write(QuoteChar);
                return;
            }

            var hexSeqBuffer = new char[4];
            writer.Write(QuoteChar);

            var len = value.Length;
            for (var i = 0; i < len; i++)
            {
                char c = value[i];

                switch (c)
                {
                    case LineFeedChar:
                        writer.Write(EscapedLineFeed);
                        continue;

                    case CarriageReturnChar:
                        writer.Write(EscapedCarriageReturn);
                        continue;

                    case TabChar:
                        writer.Write(EscapedTab);
                        continue;

                    case QuoteChar:
                        writer.Write(EscapedQuote);
                        continue;

                    case EscapeChar:
                        writer.Write(EscapedBackslash);
                        continue;

                    case FormFeedChar:
                        writer.Write(EscapedFormFeed);
                        continue;

                    case BackspaceChar:
                        writer.Write(EscapedBackspace);
                        continue;
                }

                if (escapeHtmlChars)
                {
                    switch (c)
                    {
                        case '<':
                            writer.Write("\\u003c");
                            continue;
                        case '>':
                            writer.Write("\\u003e");
                            continue;
                        case '&':
                            writer.Write("\\u0026");
                            continue;
                        case '=':
                            writer.Write("\\u003d");
                            continue;
                        case '\'':
                            writer.Write("\\u0027");
                            continue;
                    }
                }

                if (c.IsPrintable())
                {
                    writer.Write(c);
                    continue;
                }

                // http://json.org/ spec requires any control char to be escaped
                if (escapeUnicode || char.IsControl(c))
                {
                    // Default, turn into a \uXXXX sequence
                    IntToHex(c, hexSeqBuffer);
                    writer.Write("\\u");
                    writer.Write(hexSeqBuffer);
                }
                else
                {
                    writer.Write(c);
                }
            }

            writer.Write(QuoteChar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPrintable(this char c)
        {
            return c >= 32 && c <= 126;
        }

        /// <summary>
        /// Searches the string for one or more non-printable characters.
        /// </summary>
        /// <param name="value">The string to search.</param>
        /// <param name="escapeHtmlChars"></param>
        /// <returns>True if there are any characters that require escaping. False if the value can be written verbatim.</returns>
        /// <remarks>
        /// Micro optimizations: since quote and backslash are the only printable characters requiring escaping, removed previous optimization
        /// (using flags instead of value.IndexOfAny(EscapeChars)) in favor of two equality operations saving both memory and CPU time.
        /// Also slightly reduced code size by re-arranging conditions.
        /// TODO: Possible Linq-only solution requires profiling: return value.Any(c => !c.IsPrintable() || c == QuoteChar || c == EscapeChar);
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasAnyEscapeChars(string value, bool escapeHtmlChars)
        {
            var len = value.Length;
            for (var i = 0; i < len; i++)
            {
                var c = value[i];

                // c is not printable
                // OR c is a printable that requires escaping (quote and backslash).
                if (!c.IsPrintable() || c == QuoteChar || c == EscapeChar)
                    return true;

                if (escapeHtmlChars && (c == '<' || c == '>' || c == '&' || c == '=' || c == '\\'))
                    return true;
            }
            return false;
        }

        // Micro optimized
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IntToHex(int intValue, char[] hex)
        {
            // TODO: test if unrolling loop is faster
            for (var i = 3; i >= 0; i--)
            {
                var num = intValue & 0xF; // intValue % 16

                // 0x30 + num == '0' + num
                // 0x37 + num == 'A' + (num - 10)
                hex[i] = (char)((num < 10 ? 0x30 : 0x37) + num);

                intValue >>= 4;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsJsObject(string value)
        {
            return !string.IsNullOrEmpty(value)
                && value[0] == '{'
                && value[value.Length - 1] == '}';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsJsObject(ReadOnlySpan<char> value)
        {
            return !value.IsNullOrEmpty()
               && value[0] == '{'
               && value[value.Length - 1] == '}';
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsJsArray(string value)
        {
            return !string.IsNullOrEmpty(value)
                && value[0] == '['
                && value[value.Length - 1] == ']';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsJsArray(ReadOnlySpan<char> value)
        {
            return !value.IsNullOrEmpty()
               && value[0] == '['
               && value[value.Length - 1] == ']';
        }

    }
}
