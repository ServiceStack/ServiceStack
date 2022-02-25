//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Support;
using static System.String;

namespace ServiceStack
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts from base: 0 - 62
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns></returns>
        public static string BaseConvert(this string source, int from, int to)
        {
            var chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var len = source.Length;
            if (len == 0)
                throw new Exception($"Parameter: '{source}' is not valid integer (in base {@from}).");
            var minus = source[0] == '-' ? "-" : "";
            var src = minus == "" ? source : source.Substring(1);
            len = src.Length;
            if (len == 0)
                throw new Exception($"Parameter: '{source}' is not valid integer (in base {@from}).");

            var d = 0;
            for (int i = 0; i < len; i++) // Convert to decimal
            {
                int c = chars.IndexOf(src[i]);
                if (c >= from)
                    throw new Exception($"Parameter: '{source}' is not valid integer (in base {@from}).");
                d = d * from + c;
            }
            if (to == 10 || d == 0)
                return minus + d;

            var result = "";
            while (d > 0)   // Convert to desired
            {
                result = chars[d % to] + result;
                d /= to;
            }
            return minus + result;
        }

        public static string EncodeXml(this string value)
        {
            return value.Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;");
        }

        public static string EncodeJson(this string value)
        {
            return Concat
            ("\"",
                value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n"),
                "\""
            );
        }

        public static string EncodeJsv(this string value)
        {
            if (JsState.QueryStringMode)
            {
                return UrlEncode(value);
            }
            return String.IsNullOrEmpty(value) || !JsWriter.HasAnyEscapeChars(value)
                ? value
                : Concat
                    (
                        JsWriter.QuoteString,
                        value.Replace(JsWriter.QuoteString, TypeSerializer.DoubleQuoteString),
                        JsWriter.QuoteString
                    );
        }

        public static string DecodeJsv(this string value)
        {
            const int startingQuotePos = 1;
            const int endingQuotePos = 2;
            return String.IsNullOrEmpty(value) || value[0] != JsWriter.QuoteChar
                    ? value
                    : value.Substring(startingQuotePos, value.Length - endingQuotePos)
                        .Replace(TypeSerializer.DoubleQuoteString, JsWriter.QuoteString);
        }

        public static string UrlEncode(this string text, bool upperCase=false)
        {
            if (string.IsNullOrEmpty(text)) 
                return text;

            var sb = StringBuilderThreadStatic.Allocate();
            var fmt = upperCase ? "X2" : "x2";

            foreach (var charCode in Encoding.UTF8.GetBytes(text))
            {

                if (
                    charCode >= 65 && charCode <= 90        // A-Z
                    || charCode >= 97 && charCode <= 122    // a-z
                    || charCode >= 48 && charCode <= 57     // 0-9
                    || charCode >= 44 && charCode <= 46     // ,-.
                    )
                {
                    sb.Append((char)charCode);
                }
                else if(charCode == 32)
                {
                    sb.Append('+');
                }
                else
                {
                    sb.Append('%' + charCode.ToString(fmt));
                }
            }

            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }

        public static string UrlDecode(this string text)
        {
            if (String.IsNullOrEmpty(text)) return null;

            var bytes = new List<byte>();

            var textLength = text.Length;
            for (var i = 0; i < textLength; i++)
            {
                var c = text[i];
                if (c == '+')
                {
                    bytes.Add(32);
                }
                else if (c == '%')
                {
                    var hexNo = Convert.ToByte(text.Substring(i + 1, 2), 16);
                    bytes.Add(hexNo);
                    i += 2;
                }
                else
                {
                    bytes.Add((byte)c);
                }
            }

            byte[] byteArray = bytes.ToArray();
            return Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
        }

        public static string HexUnescape(this string text, params char[] anyCharOf)
        {
            if (String.IsNullOrEmpty(text)) return null;
            if (anyCharOf == null || anyCharOf.Length == 0) return text;

            var sb = StringBuilderThreadStatic.Allocate();

            var textLength = text.Length;
            for (var i = 0; i < textLength; i++)
            {
                var c = text.Substring(i, 1);
                if (c == "%")
                {
                    var hexNo = Convert.ToInt32(text.Substring(i + 1, 2), 16);
                    sb.Append((char)hexNo);
                    i += 2;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }

        public static string UrlFormat(this string url, params string[] urlComponents)
        {
            var encodedUrlComponents = new string[urlComponents.Length];
            for (var i = 0; i < urlComponents.Length; i++)
            {
                var x = urlComponents[i];
                encodedUrlComponents[i] = x.UrlEncode();
            }

            return Format(url, encodedUrlComponents);
        }

        public static string ToRot13(this string value)
        {
            var array = value.ToCharArray();
            for (var i = 0; i < array.Length; i++)
            {
                var number = (int)array[i];

                if (number >= 'a' && number <= 'z')
                    number += (number > 'm') ? -13 : 13;

                else if (number >= 'A' && number <= 'Z')
                    number += (number > 'M') ? -13 : 13;

                array[i] = (char)number;
            }
            return new string(array);
        }

        private static char[] UrlPathDelims = new[] {'?', '#'};

        public static string UrlWithTrailingSlash(this string url)
        {
            var endPos = url?.IndexOfAny(UrlPathDelims) ?? -1;
            return endPos >= 0
                ? url.Substring(0, endPos).WithTrailingSlash() + url.Substring(endPos)
                : url.WithTrailingSlash();
        }
        
        public static string WithTrailingSlash(this string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path == "")
                return "/";

            return path[path.Length - 1] != '/' ? path + "/" : path;
        }

        public static string AppendPath(this string uri, params string[] uriComponents)
        {
            return AppendUrlPaths(uri, uriComponents);
        }

        public static string AppendUrlPaths(this string uri, params string[] uriComponents)
        {
            var sb = StringBuilderThreadStatic.Allocate();
            sb.Append(uri.WithTrailingSlash());
            var i = 0;
            foreach (var uriComponent in uriComponents)
            {
                if (i++ > 0) sb.Append('/');
                sb.Append(uriComponent.UrlEncode());
            }
            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }

        public static string AppendUrlPathsRaw(this string uri, params string[] uriComponents)
        {
            var sb = StringBuilderThreadStatic.Allocate();
            sb.Append(uri.WithTrailingSlash());
            var i = 0;
            foreach (var uriComponent in uriComponents)
            {
                if (i++ > 0) sb.Append('/');
                sb.Append(uriComponent);
            }
            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }

        public static string FromUtf8Bytes(this byte[] bytes)
        {
            return bytes == null ? null
                : bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF  
                    ? Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3)
                    : Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public static byte[] ToUtf8Bytes(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public static byte[] ToUtf8Bytes(this int intVal)
        {
            return FastToUtf8Bytes(intVal.ToString());
        }

        public static byte[] ToUtf8Bytes(this long longVal)
        {
            return FastToUtf8Bytes(longVal.ToString());
        }

        public static byte[] ToUtf8Bytes(this ulong ulongVal)
        {
            return FastToUtf8Bytes(ulongVal.ToString());
        }

        public static byte[] ToUtf8Bytes(this double doubleVal)
        {
            var doubleStr = doubleVal.ToString(CultureInfo.InvariantCulture.NumberFormat);

            if (doubleStr.IndexOf('E') != -1 || doubleStr.IndexOf('e') != -1)
                doubleStr = DoubleConverter.ToExactString(doubleVal);

            return FastToUtf8Bytes(doubleStr);
        }

        public static string WithoutBom(this string value)
        {
            return value.Length > 0 && value[0] == 65279 
                ? value.Substring(1) 
                : value;
        }

        // from JWT spec
        public static string ToBase64UrlSafe(this byte[] input)
        {
            var output = Convert.ToBase64String(input);
            output = output.LeftPart('='); // Remove any trailing '='s
            output = output.Replace('+', '-'); // 62nd char of encoding
            output = output.Replace('/', '_'); // 63rd char of encoding
            return output;
        }

        public static string ToBase64UrlSafe(this MemoryStream ms)
        {
            var output = Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
            output = output.LeftPart('='); // Remove any trailing '='s
            output = output.Replace('+', '-'); // 62nd char of encoding
            output = output.Replace('/', '_'); // 63rd char of encoding
            return output;
        }

        // from JWT spec
        public static byte[] FromBase64UrlSafe(this string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break;  // One pad char
                default: throw new Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }

        /// <summary>
        /// Skip the encoding process for 'safe strings' 
        /// </summary>
        /// <param name="strVal"></param>
        /// <returns></returns>
        private static byte[] FastToUtf8Bytes(string strVal)
        {
            var bytes = new byte[strVal.Length];
            for (var i = 0; i < strVal.Length; i++)
                bytes[i] = (byte)strVal[i];

            return bytes;
        }

        public static string LeftPart(this string strVal, char needle)
        {
            if (strVal == null) return null;
            var pos = strVal.IndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Substring(0, pos);
        }

        public static string LeftPart(this string strVal, string needle)
        {
            if (strVal == null) return null;
            var pos = strVal.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            return pos == -1
                ? strVal
                : strVal.Substring(0, pos);
        }

        public static string RightPart(this string strVal, char needle)
        {
            if (strVal == null) return null;
            var pos = strVal.IndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Substring(pos + 1);
        }

        public static string RightPart(this string strVal, string needle)
        {
            if (strVal == null) return null;
            var pos = strVal.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            return pos == -1
                ? strVal
                : strVal.Substring(pos + needle.Length);
        }

        public static string LastLeftPart(this string strVal, char needle)
        {
            if (strVal == null) return null;
            var pos = strVal.LastIndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Substring(0, pos);
        }

        public static string LastLeftPart(this string strVal, string needle)
        {
            if (strVal == null) return null;
            var pos = strVal.LastIndexOf(needle, StringComparison.OrdinalIgnoreCase);
            return pos == -1
                ? strVal
                : strVal.Substring(0, pos);
        }

        public static string LastRightPart(this string strVal, char needle)
        {
            if (strVal == null) return null;
            var pos = strVal.LastIndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Substring(pos + 1);
        }

        public static string LastRightPart(this string strVal, string needle)
        {
            if (strVal == null) return null;
            var pos = strVal.LastIndexOf(needle, StringComparison.OrdinalIgnoreCase);
            return pos == -1
                ? strVal
                : strVal.Substring(pos + needle.Length);
        }

        public static string[] SplitOnFirst(this string strVal, char needle)
        {
            if (strVal == null) return TypeConstants.EmptyStringArray;
            var pos = strVal.IndexOf(needle);
            return pos == -1
                ? new[] { strVal }
                : new[] { strVal.Substring(0, pos), strVal.Substring(pos + 1) };
        }

        public static string[] SplitOnFirst(this string strVal, string needle)
        {
            if (strVal == null) return TypeConstants.EmptyStringArray;
            var pos = strVal.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            return pos == -1
                ? new[] { strVal }
                : new[] { strVal.Substring(0, pos), strVal.Substring(pos + needle.Length) };
        }

        public static string[] SplitOnLast(this string strVal, char needle)
        {
            if (strVal == null) return TypeConstants.EmptyStringArray;
            var pos = strVal.LastIndexOf(needle);
            return pos == -1
                ? new[] { strVal }
                : new[] { strVal.Substring(0, pos), strVal.Substring(pos + 1) };
        }

        public static string[] SplitOnLast(this string strVal, string needle)
        {
            if (strVal == null) return TypeConstants.EmptyStringArray;
            var pos = strVal.LastIndexOf(needle, StringComparison.OrdinalIgnoreCase);
            return pos == -1
                ? new[] { strVal }
                : new[] { strVal.Substring(0, pos), strVal.Substring(pos + needle.Length) };
        }

        public static string WithoutExtension(this string filePath)
        {
            if (String.IsNullOrEmpty(filePath)) 
                return null;

            var extPos = filePath.LastIndexOf('.');
            if (extPos == -1) return filePath;

            var dirPos = filePath.LastIndexOfAny(PclExport.DirSeps);
            return extPos > dirPos ? filePath.Substring(0, extPos) : filePath;
        }

        public static string GetExtension(this string filePath)
        {
            if (String.IsNullOrEmpty(filePath)) 
                return null;

            var extPos = filePath.LastIndexOf('.');
            return extPos == -1 ? Empty : filePath.Substring(extPos);
        }

        public static string ParentDirectory(this string filePath)
        {
            if (String.IsNullOrEmpty(filePath)) return null;

            var dirSep = filePath.IndexOf(PclExport.Instance.DirSep) != -1
                         ? PclExport.Instance.DirSep
                         : filePath.IndexOf(PclExport.Instance.AltDirSep) != -1
                            ? PclExport.Instance.AltDirSep 
                            : (char)0;

            return dirSep == 0 ? null : filePath.TrimEnd(dirSep).SplitOnLast(dirSep)[0];
        }

        public static string ToJsv<T>(this T obj)
        {
            return TypeSerializer.SerializeToString(obj);
        }

        public static string ToJsv<T>(this T obj, Action<Config> configure)
        {
            var config = new Config();
            configure(config);
            using (JsConfig.With(config))
            {
                return ToJsv(obj);
            }
        }

        public static string ToSafeJsv<T>(this T obj)
        {
            return TypeSerializer.HasCircularReferences(obj)
                ? obj.ToSafePartialObjectDictionary().ToJsv()
                : obj.ToJsv();
        }

        public static T FromJsv<T>(this string jsv)
        {
            return TypeSerializer.DeserializeFromString<T>(jsv);
        }

        public static T FromJsvSpan<T>(this ReadOnlySpan<char> jsv)
        {
            return TypeSerializer.DeserializeFromSpan<T>(jsv);
        }

        public static string ToJson<T>(this T obj, Action<Config> configure)
        {
            var config = new Config();
            configure(config);
            using (JsConfig.With(config))
            {
                return ToJson(obj);
            }
        }
        
        public static string ToJson<T>(this T obj)
        {
            return JsConfig.PreferInterfaces
                ? JsonSerializer.SerializeToString(obj, AssemblyUtils.MainInterface<T>())
                : JsonSerializer.SerializeToString(obj);
        }

        public static string ToSafeJson<T>(this T obj)
        {
            return TypeSerializer.HasCircularReferences(obj)
                ? obj.ToSafePartialObjectDictionary().ToJson()
                : obj.ToJson();
        }

        public static T FromJson<T>(this string json)
        {
            return JsonSerializer.DeserializeFromString<T>(json);
        }

        public static T FromJsonSpan<T>(this ReadOnlySpan<char> json)
        {
            return JsonSerializer.DeserializeFromSpan<T>(json);
        }

        public static string ToCsv<T>(this T obj)
        {
            return CsvSerializer.SerializeToString(obj);
        }

        public static string ToCsv<T>(this T obj, Action<Config> configure)
        {
            var config = new Config();
            configure(config);
            using (JsConfig.With(config))
            {
                return ToCsv(obj);
            }
        }

        public static T FromCsv<T>(this string csv)
        {
            return CsvSerializer.DeserializeFromString<T>(csv);
        }

        public static string FormatWith(this string text, params object[] args)
        {
            return Format(text, args);
        }

        public static string Fmt(this string text, params object[] args)
        {
            return Format(text, args);
        }
        public static string Fmt(this string text, IFormatProvider provider, params object[] args)
        {
            return Format(provider, text, args);
        }

        public static string Fmt(this string text, object arg1)
        {
            return Format(text, arg1);
        }

        public static string Fmt(this string text, object arg1, object arg2)
        {
            return Format(text, arg1, arg2);
        }

        public static string Fmt(this string text, object arg1, object arg2, object arg3)
        {
            return Format(text, arg1, arg2, arg3);
        }

        public static bool StartsWithIgnoreCase(this string text, string startsWith)
        {
            return text != null
                && text.StartsWith(startsWith, PclExport.Instance.InvariantComparisonIgnoreCase);
        }

        public static bool EndsWithIgnoreCase(this string text, string endsWith)
        {
            return text != null
                && text.EndsWith(endsWith, PclExport.Instance.InvariantComparisonIgnoreCase);
        }

        public static string ReadAllText(this string filePath)
        {
            return PclExport.Instance.ReadAllText(filePath);
        }

        public static bool FileExists(this string filePath)
        {
            return PclExport.Instance.FileExists(filePath);
        }

        public static bool DirectoryExists(this string dirPath)
        {
            return PclExport.Instance.DirectoryExists(dirPath);
        }

        public static void CreateDirectory(this string dirPath)
        {
            PclExport.Instance.CreateDirectory(dirPath);
        }

        public static int IndexOfAny(this string text, params string[] needles)
        {
            return IndexOfAny(text, 0, needles);
        }

        public static int IndexOfAny(this string text, int startIndex, params string[] needles)
        {
            var firstPos = -1;
            if (text != null)
            {
                foreach (var needle in needles)
                {
                    var pos = text.IndexOf(needle, startIndex, StringComparison.Ordinal);
                    if (pos >= 0 && (firstPos == -1 || pos < firstPos))
                        firstPos = pos;
                }
            }

            return firstPos;
        }

        public static string ExtractContents(this string fromText, string startAfter, string endAt)
        {
            return ExtractContents(fromText, startAfter, startAfter, endAt);
        }

        public static string ExtractContents(this string fromText, string uniqueMarker, string startAfter, string endAt)
        {
            if (String.IsNullOrEmpty(uniqueMarker))
                throw new ArgumentNullException(nameof(uniqueMarker));
            if (String.IsNullOrEmpty(startAfter))
                throw new ArgumentNullException(nameof(startAfter));
            if (String.IsNullOrEmpty(endAt))
                throw new ArgumentNullException(nameof(endAt));

            if (String.IsNullOrEmpty(fromText)) return null;

            var markerPos = fromText.IndexOf(uniqueMarker, StringComparison.Ordinal);
            if (markerPos == -1) return null;

            var startPos = fromText.IndexOf(startAfter, markerPos, StringComparison.Ordinal);
            if (startPos == -1) return null;
            startPos += startAfter.Length;

            var endPos = fromText.IndexOf(endAt, startPos, StringComparison.Ordinal);
            if (endPos == -1) endPos = fromText.Length;

            return fromText.Substring(startPos, endPos - startPos);
        }

        static readonly Regex StripHtmlRegEx = new Regex(@"<(.|\n)*?>", PclExport.Instance.RegexOptions);

        public static string StripHtml(this string html)
        {
            return String.IsNullOrEmpty(html) ? null : StripHtmlRegEx.Replace(html, "");
        }

        public static string Quoted(this string text)
        {
            return text == null || text.IndexOf('"') >= 0
                ? text
                : '"' + text + '"';
        }

        public static string StripQuotes(this string text)
        {
            return string.IsNullOrEmpty(text) || text.Length < 2
                ? text
                : (text[0] == '"' && text[text.Length - 1] == '"') ||
                  (text[0] == '\'' && text[text.Length - 1] == '\'') ||
                  (text[0] == '`' && text[text.Length - 1] == '`')
                    ? text.Substring(1, text.Length - 2)
                    : text;
        }

        static readonly Regex StripBracketsRegEx = new Regex(@"\[(.|\n)*?\]", PclExport.Instance.RegexOptions);
        static readonly Regex StripBracesRegEx = new Regex(@"\((.|\n)*?\)", PclExport.Instance.RegexOptions);

        public static string StripMarkdownMarkup(this string markdown)
        {
            if (String.IsNullOrEmpty(markdown)) return null;
            markdown = StripBracketsRegEx.Replace(markdown, "");
            markdown = StripBracesRegEx.Replace(markdown, "");
            markdown = markdown
                .Replace("*", "")
                .Replace("!", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("#", "");

            return markdown;
        }

        private const int LowerCaseOffset = 'a' - 'A';
        public static string ToCamelCase(this string value)
        {
            if (string.IsNullOrEmpty(value)) 
                return value;

            var len = value.Length;
            var newValue = new char[len];
            var firstPart = true;

            for (var i = 0; i < len; ++i)
            {
                var c0 = value[i];
                var c1 = i < len - 1 ? value[i + 1] : 'A';
                var c0isUpper = c0 is >= 'A' and <= 'Z';
                var c1isUpper = c1 is >= 'A' and <= 'Z';

                if (firstPart && c0isUpper && (c1isUpper || i == 0))
                    c0 = (char)(c0 + LowerCaseOffset);
                else
                    firstPart = false;

                newValue[i] = c0;
            }

            return new string(newValue);
        }

        public static string ToPascalCase(this string value)
        {
            if (string.IsNullOrEmpty(value)) 
                return value;

            if (value.IndexOf('_') >= 0)
            {
                var parts = value.Split('_');
                var sb = StringBuilderThreadStatic.Allocate();
                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part))
                        continue;
                    var str = part.ToCamelCase();
                    sb.Append(char.ToUpper(str[0]) + str.SafeSubstring(1, str.Length));
                }
                return StringBuilderThreadStatic.ReturnAndFree(sb);
            }

            var camelCase = value.ToCamelCase();
            return char.ToUpper(camelCase[0]) + camelCase.SafeSubstring(1, camelCase.Length);
        }

        public static string ToTitleCase(this string value)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value).Replace("_", String.Empty);
        }

        public static string ToLowercaseUnderscore(this string value)
        {
            if (String.IsNullOrEmpty(value)) return value;
            value = value.ToCamelCase();

            var sb = StringBuilderThreadStatic.Allocate();
            foreach (char t in value)
            {
                if (char.IsDigit(t) || (char.IsLetter(t) && char.IsLower(t)) || t == '_')
                {
                    sb.Append(t);
                }
                else
                {
                    sb.Append("_");
                    sb.Append(char.ToLower(t));
                }
            }
            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }

        public static string ToLowerSafe(this string value)
        {
            return value?.ToLower();
        }

        public static string ToUpperSafe(this string value)
        {
            return value?.ToUpper();
        }

        public static string SafeSubstring(this string value, int startIndex)
        {
            if (String.IsNullOrEmpty(value)) return Empty;
            return SafeSubstring(value, startIndex, value.Length);
        }

        public static string SafeSubstring(this string value, int startIndex, int length)
        {
            if (String.IsNullOrEmpty(value) || length <= 0) return Empty;
            if (startIndex < 0) startIndex = 0;
            if (value.Length >= (startIndex + length))
                return value.Substring(startIndex, length);

            return value.Length > startIndex ? value.Substring(startIndex) : Empty;
        }

        [Obsolete("typo")]
        public static string SubstringWithElipsis(this string value, int startIndex, int length) => SubstringWithEllipsis(value, startIndex, length);

        public static string SubstringWithEllipsis(this string value, int startIndex, int length)
        {
            var str = value.SafeSubstring(startIndex, length);
            return str.Length == length
                ? str + "..."
                : str;
        }

        public static bool IsAnonymousType(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return PclExport.Instance.IsAnonymousType(type);
        }

        public static int CompareIgnoreCase(this string strA, string strB)
        {
            return Compare(strA, strB, PclExport.Instance.InvariantComparisonIgnoreCase);
        }

        public static bool EndsWithInvariant(this string str, string endsWith)
        {
            return str.EndsWith(endsWith, PclExport.Instance.InvariantComparison);
        }

        private static readonly Regex InvalidVarCharsRegex = new(@"[^A-Za-z0-9_]", RegexOptions.Compiled);
        private static readonly Regex ValidVarCharsRegex = new(@"^[A-Za-z0-9_]+$", RegexOptions.Compiled);
        private static readonly Regex InvalidVarRefCharsRegex = new(@"[^A-Za-z0-9._]", RegexOptions.Compiled);
        private static readonly Regex ValidVarRefCharsRegex = new(@"^[A-Za-z0-9._]+$", RegexOptions.Compiled);
        
        private static readonly Regex SplitCamelCaseRegex = new("([A-Z]|[0-9]+)", RegexOptions.Compiled);
        private static readonly Regex HttpRegex = new(@"^http://",
            PclExport.Instance.RegexOptions | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static T ToEnumOrDefault<T>(this string value, T defaultValue)
        {
            if (String.IsNullOrEmpty(value)) return defaultValue;
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static string SplitCamelCase(this string value)
        {
            return SplitCamelCaseRegex.Replace(value, " $1").TrimStart();
        }

        public static string ToInvariantUpper(this char value)
        {
            return PclExport.Instance.ToInvariantUpper(value);
        }

        public static string ToEnglish(this string camelCase)
        {
            var ucWords = camelCase.SplitCamelCase().ToLower();
            return ucWords[0].ToInvariantUpper() + ucWords.Substring(1);
        }

        public static string ToHttps(this string url)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }
            return HttpRegex.Replace(url.Trim(), "https://");
        }

        public static bool IsEmpty(this string value)
        {
            return String.IsNullOrEmpty(value);
        }

        public static bool IsNullOrEmpty(this string value)
        {
            return String.IsNullOrEmpty(value);
        }

        public static bool EqualsIgnoreCase(this string value, string other)
        {
            return String.Equals(value, other, StringComparison.OrdinalIgnoreCase);
        }

        public static string ReplaceFirst(this string haystack, string needle, string replacement)
        {
            var pos = haystack.IndexOf(needle, StringComparison.Ordinal);
            if (pos < 0) return haystack;

            return haystack.Substring(0, pos) + replacement + haystack.Substring(pos + needle.Length);
        }

        [Obsolete("Use built-in string.Replace()")]
        public static string ReplaceAll(this string haystack, string needle, string replacement) => 
            haystack.Replace(needle, replacement);

        public static bool ContainsAny(this string text, params string[] testMatches)
        {
            foreach (var testMatch in testMatches)
            {
                if (text.Contains(testMatch)) return true;
            }
            return false;
        }

        public static bool ContainsAny(this string text, string[] testMatches, StringComparison comparisonType)
        {
            foreach (var testMatch in testMatches)
            {
                if (text.IndexOf(testMatch, comparisonType) >= 0) return true;
            }
            return false;
        }
      
        public static bool IsValidVarName(this string name) => ValidVarCharsRegex.IsMatch(name);
        public static bool IsValidVarRef(this string name) => ValidVarRefCharsRegex.IsMatch(name);

        public static string SafeVarName(this string text) => !string.IsNullOrEmpty(text) 
            ? InvalidVarCharsRegex.Replace(text, "_") : null;

        public static string SafeVarRef(this string text) => !string.IsNullOrEmpty(text) 
            ? InvalidVarRefCharsRegex.Replace(text, "_") : null;

        public static string Join(this List<string> items)
        {
            return string.Join(JsWriter.ItemSeperatorString, items.ToArray());
        }

        public static string Join(this List<string> items, string delimeter)
        {
            return string.Join(delimeter, items.ToArray());
        }

        public static string ToParentPath(this string path)
        {
            var pos = path.LastIndexOf('/');
            if (pos == -1) return "/";

            var parentPath = path.Substring(0, pos);
            return parentPath;
        }

        public static string RemoveCharFlags(this string text, bool[] charFlags)
        {
            if (text == null) return null;

            var copy = text.ToCharArray();
            var nonWsPos = 0;

            for (var i = 0; i < text.Length; i++)
            {
                var @char = text[i];
                if (@char < charFlags.Length && charFlags[@char]) continue;
                copy[nonWsPos++] = @char;
            }

            return new string(copy, 0, nonWsPos);
        }

        public static string ToNullIfEmpty(this string text)
        {
            return string.IsNullOrEmpty(text) ? null : text;
        }
        
        private static readonly char[] SystemTypeChars = { '<', '>', '+' };

        public static bool IsUserType(this Type type)
        {
            return type.IsClass
                && !type.IsSystemType();
        }

        public static bool IsUserEnum(this Type type)
        {
            return type.IsEnum
                && !type.IsSystemType();
        }

        public static bool IsSystemType(this Type type)
        {
            return type.Namespace == null
                || type.Namespace.StartsWith("System")
                || type.Name.IndexOfAny(SystemTypeChars) >= 0;
        }

        public static bool IsTuple(this Type type) => type.Name.StartsWith("Tuple`");

        public static bool IsInt(this string text) => !string.IsNullOrEmpty(text) && int.TryParse(text, out _);

        public static int ToInt(this string text) => text == null ? default(int) : int.Parse(text);

        public static int ToInt(this string text, int defaultValue) => int.TryParse(text, out var ret) ? ret : defaultValue;

        public static long ToLong(this string text) => long.Parse(text);
        public static long ToInt64(this string text) => long.Parse(text);

        public static long ToLong(this string text, long defaultValue) => long.TryParse(text, out var ret) ? ret : defaultValue;
        public static long ToInt64(this string text, long defaultValue) => long.TryParse(text, out var ret) ? ret : defaultValue;

        public static float ToFloat(this string text) => text == null ? default(float) : float.Parse(text);

        public static float ToFloatInvariant(this string text) => text == null ? default(float) : float.Parse(text, CultureInfo.InvariantCulture);

        public static float ToFloat(this string text, float defaultValue) => float.TryParse(text, out var ret) ? ret : defaultValue;

        public static double ToDouble(this string text) => text == null ? default(double) : double.Parse(text);

        public static double ToDoubleInvariant(this string text) => text == null ? default(double) : double.Parse(text, CultureInfo.InvariantCulture);

        public static double ToDouble(this string text, double defaultValue) => double.TryParse(text, out var ret) ? ret : defaultValue;

        public static decimal ToDecimal(this string text) => text == null ? default(decimal) : decimal.Parse(text);

        public static decimal ToDecimalInvariant(this string text) => text == null ? default(decimal) : decimal.Parse(text, CultureInfo.InvariantCulture);

        public static decimal ToDecimal(this string text, decimal defaultValue) => decimal.TryParse(text, out var ret) ? ret : defaultValue;

        public static bool Matches(this string value, string pattern) => value.Glob(pattern);

        public static bool Glob(this string value, string pattern)
        {
            int pos;
            for (pos = 0; pattern.Length != pos; pos++)
            {
                switch (pattern[pos])
                {
                    case '?':
                        break;

                    case '*':
                        for (int i = value.Length; i >= pos; i--)
                        {
                            if (Glob(value.Substring(i), pattern.Substring(pos + 1)))
                                return true;
                        }
                        return false;

                    default:
                        if (value.Length == pos || char.ToUpper(pattern[pos]) != char.ToUpper(value[pos]))
                        {
                            return false;
                        }
                        break;
                }
            }

            return value.Length == pos;
        }

        public static bool GlobPath(this string filePath, string pattern)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(pattern))
                return false;

            var sanitizedPath = filePath.Replace('\\','/');
            if (sanitizedPath[0] == '/')
                sanitizedPath = sanitizedPath.Substring(1);
            var sanitizedPattern = pattern.Replace('\\', '/');
            if (sanitizedPattern[0] == '/')
                sanitizedPattern = sanitizedPattern.Substring(1);

            if (sanitizedPattern.IndexOf('*') == -1 && sanitizedPattern.IndexOf('?') == -1)
                return sanitizedPath == sanitizedPattern;

            var patternParts = sanitizedPattern.SplitOnLast('/');
            var parts = sanitizedPath.SplitOnLast('/');
            if (parts.Length == 1)
                return parts[0].Glob(pattern);

            var dirPart = parts[0];
            var filePart = parts[1];
            if (patternParts.Length == 1)
                return filePart.Glob(patternParts[0]);

            var dirPattern = patternParts[0];
            var filePattern = patternParts[1];

            if (dirPattern.IndexOf("**", StringComparison.Ordinal) >= 0)
            {
                if (!sanitizedPath.StartsWith(dirPattern.LeftPart("**").TrimEnd('*', '/')))
                    return false;
            }
            else if (dirPattern.IndexOf('*') >= 0 || dirPattern.IndexOf('?') >= 0)
            {
                var regex = new Regex(
                    "^" + Regex.Escape(dirPattern).Replace(@"\*", "[^\\/]*").Replace(@"\?", ".") + "$"
                );
                if (!regex.IsMatch(dirPart))
                    return false;
            }
            else
            {
                if (dirPart != dirPattern)
                    return false;
            }

            return filePart.Glob(filePattern);
        }

        public static string TrimPrefixes(this string fromString, params string[] prefixes)
        {
            if (string.IsNullOrEmpty(fromString))
                return fromString;

            foreach (var prefix in prefixes)
            {
                if (fromString.StartsWith(prefix))
                    return fromString.Substring(prefix.Length);
            }

            return fromString;
        }

        public static string FromAsciiBytes(this byte[] bytes)
        {
            return bytes == null ? null
                : PclExport.Instance.GetAsciiString(bytes);
        }

        public static byte[] ToAsciiBytes(this string value)
        {
            return PclExport.Instance.GetAsciiBytes(value);
        }

        public static Dictionary<string,string> ParseKeyValueText(this string text, string delimiter=" ")
        {
            var to = new Dictionary<string, string>();
            if (text == null) return to;

            foreach (var parts in text.ReadLines().Select(line => line.Trim().SplitOnFirst(delimiter)))
            {
                var key = parts[0].Trim();
                if (key.Length == 0 || key.StartsWith("#")) continue;
                to[key] = parts.Length == 2 ? parts[1].Trim() : null;
            }

            return to;
        }

        public static List<KeyValuePair<string,string>> ParseAsKeyValues(this string text, string delimiter=" ")
        {
            var to = new List<KeyValuePair<string,string>>();
            if (text == null) return to;

            foreach (var parts in text.ReadLines().Select(line => line.Trim().SplitOnFirst(delimiter)))
            {
                var key = parts[0].Trim();
                if (key.Length == 0 || key.StartsWith("#")) continue;
                to.Add(new KeyValuePair<string, string>(key, parts.Length == 2 ? parts[1].Trim() : null));
            }

            return to;
        }

        public static IEnumerable<string> ReadLines(this string text)
        {
            string line;
            var reader = new StringReader(text ?? "");
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        public static int CountOccurrencesOf(this string text, char needle) =>
            text.AsSpan().CountOccurrencesOf(needle);

        public static string NormalizeNewLines(this string text)
        {
            return text?.Replace("\r\n", "\n").Trim();
        }

#if !LITE
        public static string HexEscape(this string text, params char[] anyCharOf)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (anyCharOf == null || anyCharOf.Length == 0) return text;

            var encodeCharMap = new HashSet<char>(anyCharOf);

            var sb = StringBuilderThreadStatic.Allocate();
            var textLength = text.Length;
            for (var i = 0; i < textLength; i++)
            {
                var c = text[i];
                if (encodeCharMap.Contains(c))
                {
                    sb.Append('%' + ((int)c).ToString("x"));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }

        public static string ToXml<T>(this T obj)
        {
            return XmlSerializer.SerializeToString(obj);
        }

        public static T FromXml<T>(this string json)
        {
            return XmlSerializer.DeserializeFromString<T>(json);
        }
#endif

        public static string ToHex(this byte[] hashBytes, bool upper=false)
        {
            var len = hashBytes.Length * 2;
            var chars = new char[len];
            var i = 0;
            var index = 0;
            for (i = 0; i < len; i += 2)
            {
                var b = hashBytes[index++];
                chars[i] = GetHexValue(b / 16, upper);
                chars[i + 1] = GetHexValue(b % 16, upper);
            }
            return new string(chars);
        }
 
        private static char GetHexValue(int i, bool upper)
        {
            if (i < 0 || i > 15)
                throw new ArgumentOutOfRangeException(nameof(i), "must be between 0 and 15");

            return i < 10 
                ? (char) (i + '0') 
                : (char) (i - 10 + (upper ? 'A' : 'a'));
        }
        
    }
}

namespace ServiceStack.Text
{
    public static class StringTextExtensions
    {
        [Obsolete("Use ConvertTo<T>")]
        public static T To<T>(this string value)
        {
            return TypeSerializer.DeserializeFromString<T>(value);
        }

        [Obsolete("Use ConvertTo<T>")]
        public static T To<T>(this string value, T defaultValue)
        {
            return String.IsNullOrEmpty(value) ? defaultValue : TypeSerializer.DeserializeFromString<T>(value);
        }

        [Obsolete("Use ConvertTo<T>")]
        public static T ToOrDefaultValue<T>(this string value)
        {
            return String.IsNullOrEmpty(value) ? default(T) : TypeSerializer.DeserializeFromString<T>(value);
        }

        [Obsolete("Use ConvertTo<T>")]
        public static object To(this string value, Type type)
        {
            return TypeSerializer.DeserializeFromString(value, type);
        }
    }
}
