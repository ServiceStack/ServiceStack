using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.MiniProfiler.Helpers
{
    /// <summary>
    /// Common extension methods to use only in this project
    /// </summary>
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Answers true if this String is either null or empty.
        /// </summary>
        internal static bool IsNullOrWhiteSpace(this string s)
        {
			return s == null || s.Trim() == string.Empty;
        }

        /// <summary>
        /// Answers true if this String is neither null or empty.
        /// </summary>
        internal static bool HasValue(this string s)
        {
			return !s.IsNullOrWhiteSpace();
        }

        internal static string Truncate(this string s, int maxLength)
        {
            return s != null && s.Length > maxLength ? s.Substring(0, maxLength) : s;
        }

        /// <summary>
        /// Removes trailing / characters from a path and leaves just one
        /// </summary>
        internal static string EnsureTrailingSlash(this string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return Regex.Replace(input, "/+$", "") + "/";
        }

        /// <summary>
        /// Removes any leading / characters from a path
        /// </summary>
        internal static string RemoveLeadingSlash(this string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return Regex.Replace(input, "^/+", "");
        }

        /// <summary>
        /// Removes any leading / characters from a path
        /// </summary>
        internal static string RemoveTrailingSlash(this string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return Regex.Replace(input, "/+$", "");
        }

        /// <summary>
        /// Serializes <paramref name="o"/> to a json string.
        /// </summary>
        internal static string ToJson(this object o)
        {
            if (o == null) return null;
            return JsonSerializer.SerializeToString(o);
        }
    }
}
