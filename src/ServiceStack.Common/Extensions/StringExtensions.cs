using System;
using System.Collections.Generic;
using Proxy = ServiceStack.Common.StringExtensions;

namespace ServiceStack.Common.Extensions
{
    [Obsolete("Use ServiceStack.Common.StringExtensions")]
    public static class StringExtensions
    {
        public static T ToEnum<T>(this string value)
        {
            return Proxy.ToEnum<T>(value);
        }

        public static T ToEnumOrDefault<T>(this string value, T defaultValue)
        {
            return Proxy.ToEnumOrDefault<T>(value, defaultValue);
        }

        public static string SplitCamelCase(this string value)
        {
            return Proxy.SplitCamelCase(value);
        }

        public static string ToEnglish(this string camelCase)
        {
            return Proxy.ToEnglish(camelCase);
        }

        public static bool IsEmpty(this string value)
        {
            return Proxy.IsEmpty(value);
        }

        public static bool IsNullOrEmpty(this string value)
        {
            return Proxy.IsNullOrEmpty(value);
        }

        public static bool EqualsIgnoreCase(this string value, string other)
        {
            return Proxy.EqualsIgnoreCase(value, other);
        }

        public static string ReplaceFirst(this string haystack, string needle, string replacement)
        {
            return Proxy.ReplaceFirst(haystack, needle, replacement);
        }

        public static string ReplaceAll(this string haystack, string needle, string replacement)
        {
            return Proxy.ReplaceAll(haystack, needle, replacement);
        }

        public static bool ContainsAny(this string text, params string[] testMatches)
        {
            return Proxy.ContainsAny(text, testMatches);
        }

        public static string SafeVarName(this string text)
        {
            return Proxy.SafeVarName(text);
        }

        public static string Join(this List<string> items)
        {
            return Proxy.Join(items);
        }

        public static string Join(this List<string> items, string delimeter)
        {
            return Proxy.Join(items, delimeter);
        }
    }
}