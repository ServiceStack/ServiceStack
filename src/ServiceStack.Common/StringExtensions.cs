using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using ServiceStack.Common.Utils;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace ServiceStack.Common
{
    public static class StringExtensions
    {
        static readonly Regex RegexSplitCamelCase = new Regex("([A-Z]|[0-9]+)", 
#if !SILVERLIGHT && !MONOTOUCH && !XBOX
            RegexOptions.Compiled
#else
            RegexOptions.None
#endif
        );

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
            return RegexSplitCamelCase.Replace(value, " $1").TrimStart();
        }

        public static string ToInvariantUpper(this char value)
        {
#if NETFX_CORE
            return value.ToString().ToUpperInvariant();
#else
            return value.ToString(CultureInfo.InvariantCulture).ToUpper();
#endif
        }

        public static string ToEnglish(this string camelCase)
        {
            var ucWords = camelCase.SplitCamelCase().ToLower();
            return ucWords[0].ToInvariantUpper() + ucWords.Substring(1);
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
            return String.Equals(value, other, StringComparison.CurrentCultureIgnoreCase);
        }

        public static string ReplaceFirst(this string haystack, string needle, string replacement)
        {
            var pos = haystack.IndexOf(needle);
            if (pos < 0) return haystack;

            return haystack.Substring(0, pos) + replacement + haystack.Substring(pos + needle.Length);
        }

        public static string ReplaceAll(this string haystack, string needle, string replacement)
        {
            int pos;
            // Avoid a possible infinite loop
            if (needle == replacement) return haystack;
            while ((pos = haystack.IndexOf(needle)) > 0)
            {
                haystack = haystack.Substring(0, pos) 
                    + replacement 
                    + haystack.Substring(pos + needle.Length);
            }
            return haystack;
        }

        public static bool ContainsAny(this string text, params string[] testMatches)
        {
            foreach (var testMatch in testMatches)
            {
                if (text.Contains(testMatch)) return true;
            }
            return false;
        }

        private static readonly Regex InvalidVarCharsRegEx = new Regex(@"[^A-Za-z0-9]",
#if !SILVERLIGHT && !MONOTOUCH && !XBOX
            RegexOptions.Compiled
#else
 RegexOptions.None
#endif
        );

        public static string SafeVarName(this string text)
        {
            if (String.IsNullOrEmpty(text)) return null;
            return InvalidVarCharsRegEx.Replace(text, "_");
        }

        public static string Join(this List<string> items)
        {
            return String.Join(JsWriter.ItemSeperatorString, items.ToArray());
        }

        public static string Join(this List<string> items, string delimeter)
        {
            return String.Join(delimeter, items.ToArray());
        }

        public static string CombineWith(this string path, params string[] thesePaths)
        {
            if (thesePaths.Length == 1 && thesePaths[0] == null) return path;
            return PathUtils.CombinePaths(new StringBuilder(path.TrimEnd('/', '\\')), thesePaths);
        }

        public static string CombineWith(this string path, params object[] thesePaths)
        {
            if (thesePaths.Length == 1 && thesePaths[0] == null) return path;
            return PathUtils.CombinePaths(new StringBuilder(path.TrimEnd('/', '\\')), 
                thesePaths.SafeConvertAll(x => x.ToString()).ToArray());
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

            return new String(copy, 0, nonWsPos);
        }

        public static string ToNullIfEmpty(this string text)
        {
            return String.IsNullOrEmpty(text) ? null : text;
        }


        private static char[] SystemTypeChars = new[] { '<', '>', '+' };

        public static bool IsUserType(this Type type)
        {
            return type.IsClass()
                && type.Namespace != null
                && !type.Namespace.StartsWith("System")
                && type.Name.IndexOfAny(SystemTypeChars) == -1;
        }

        public static bool IsInt(this string text)
        {
            if (String.IsNullOrEmpty(text)) return false;
            int ret;
            return Int32.TryParse(text, out ret);
        }

        public static int ToInt(this string text)
        {
            return Int32.Parse(text);
        }

        public static int ToInt(this string text, int defaultValue)
        {
            int ret;
            return Int32.TryParse(text, out ret) ? ret : defaultValue;
        }

        public static long ToInt64(this string text)
        {
            return Int64.Parse(text);
        }

        public static long ToInt64(this string text, long defaultValue)
        {
            long ret;
            return Int64.TryParse(text, out ret) ? ret : defaultValue;
        }

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
                        if (value.Length == pos || Char.ToUpper(pattern[pos]) != Char.ToUpper(value[pos]))
                        {
                            return false;
                        }
                        break;
                }
            }

            return value.Length == pos;
        }
    }

}