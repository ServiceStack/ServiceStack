using System.Text.RegularExpressions;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public static class TestUtils
    {
        public static string NormalizeNewLines(this string text) => text.Trim().Replace("\r", "");
        public static string RemoveNewLines(this string text) => text.Trim().Replace("\r", "").Replace("\n", "");
        
        static readonly Regex whitespace = new Regex(@"\s+", RegexOptions.Compiled);
        public static string RemoveAllWhitespace(this string text) => whitespace.Replace(text, "");
    }
}