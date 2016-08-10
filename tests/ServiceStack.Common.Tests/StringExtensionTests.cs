using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NUnit.Framework;
using ServiceStack.Text;
#if NETCORE
using HttpUtility = System.Net.WebUtility;
#endif

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class StringExtensionTests
    {
        [Test]
        public void To_works_with_ValueTypes()
        {
            Assert.That(1.ToString().To<int>(), Is.EqualTo(1));
        }

        [Test]
        public void To_on_null_or_empty_string_returns_default_value_supplied()
        {
            const string nullString = null;
            Assert.That("".To(1), Is.EqualTo(1));
            Assert.That("".To(default(int)), Is.EqualTo(default(int)));
            Assert.That(nullString.To(1), Is.EqualTo(1));
        }

        [Test]
        public void To_ValueType_on_null_or_empty_string_returns_default_value()
        {
            Assert.That("".To<int>(), Is.EqualTo(default(int)));
        }

        [Test]
        public void To_UrlEncode()
        {
            const string url = "http://www.servicestack.net/a?b=c&d=f";
            var urlEncoded = url.UrlEncode();

            Assert.That(urlEncoded.ToUpper(), Is.EqualTo(HttpUtility.UrlEncode(url).ToUpper()));
        }

        [Test]
        public void To_UrlDecode()
        {
            const string url = "http://www.servicestack.net/a?b=c&d=f";
            var urlEncoded = url.UrlEncode();
            var decodedUrl = urlEncoded.UrlDecode();

            Assert.That(decodedUrl, Is.EqualTo(url));
        }

        [Test]
        public void UrlFormat_encodes_components()
        {
            const string url = "http://www.servicestack.net/a?b={0}&d={1}";
            const string arg1 = "as@if.com";
            const string arg2 = "&=";

            var urlFormat = url.UrlFormat(arg1, arg2);
            var expectedUrlFormat = string.Format(url, arg1.UrlEncode(), arg2.UrlEncode());

            Assert.That(urlFormat, Is.EqualTo(expectedUrlFormat));
        }

        [Test]
        public void ErrorCode_to_English_format()
        {
            const string code = "EmailAddressIsInvalid";
            Assert.That(code.ToEnglish(), Is.EqualTo("Email address is invalid"));
        }

        [Test]
        public void Print_special_chars()
        {
            var specialChars = new List<char> { '"', ':', ',', '%' };
            specialChars.ForEach(x => Console.WriteLine(x + " = " + ((int)x).ToString("x")));
        }

        [Test]
        public void HexEscape_escapes_special_chars()
        {
            var specialChars = new List<char> { '"', ':', ',', '%' };
            const string unescapedString = "\"1st 2:nd 3r,d 4th%";
            const string expectedString = "%221st 2%3and 3r%2cd 4th%25";
            Assert.That(unescapedString.HexEscape(specialChars.ToArray()), Is.EqualTo(expectedString));
        }

        [Test]
        public void HexUnescape_unescapes_special_chars()
        {
            var specialChars = new List<char> { '"', ':', ',', '%' };
            const string escapedString = "%221st 2%3and 3r%2cd 4th%25";
            const string expectedString = "\"1st 2:nd 3r,d 4th%";
            Assert.That(escapedString.HexUnescape(specialChars.ToArray()), Is.EqualTo(expectedString));
        }

        [Test]
        public void SafeVarName_strips_illegal_chars()
        {
            Assert.That("with space".SafeVarName(), Is.EqualTo("with_space"));
            Assert.That("with @+:\\illegals".SafeVarName(), Is.EqualTo("with_____illegals"));
            Assert.That("UPPER_lower_0123456789".SafeVarName(), Is.EqualTo("UPPER_lower_0123456789"));
        }

        [Test]
        public void Glob_finds_right_strings()
        {
            var input = new[] { "Foo", "Boo", "Hoo", "Baz" }.ToList();
            var expected = input.Where(s => s.EndsWith("oo")).ToList();

            var actual = input.Where(s => s.Glob("*oo")).ToList();

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void Does_combine_paths()
        {
            Assert.That("/".CombineWith("/some/other/path"), Is.EqualTo("/some/other/path"));
            Assert.That("a".CombineWith("/some/other/path"), Is.EqualTo("a/some/other/path"));
            Assert.That("a/".CombineWith("/some/other/path"), Is.EqualTo("a/some/other/path"));
            Assert.That("/a".CombineWith("/some/other/path"), Is.EqualTo("/a/some/other/path"));
            Assert.That("/a/".CombineWith("/some/other/path"), Is.EqualTo("/a/some/other/path"));
            Assert.That("/a".CombineWith("some", "other", "path"), Is.EqualTo("/a/some/other/path"));

            Assert.That("/a".CombineWith("/some/other/path/"), Is.EqualTo("/a/some/other/path/"));
            Assert.That("/a".CombineWith("/some/", "other", "/path/"), Is.EqualTo("/a/some/other/path/"));
            Assert.That("/a".CombineWith("some", "other", "path/"), Is.EqualTo("/a/some/other/path/"));

            Assert.That("".CombineWith("some", "other", "path/"), Is.EqualTo("some/other/path/"));
        }

        [Test]
        public void ToHttps_is_idempotent()
        {
            Assert.That("https://host.example.com/path".ToHttps().ToHttps(), Is.EqualTo("https://host.example.com/path"));
        }

        [Test]
        public void ToHttps_replaces_http_with_https()
        {
            Assert.That("http://host.example.com/path".ToHttps(), Is.EqualTo("https://host.example.com/path"));
        }

        [Test]
        public void ToHttps_only_replaces_http_at_beginning_of_string()
        {
            Assert.That("http://host.example.com/http/path".ToHttps(), Is.EqualTo("https://host.example.com/http/path"));
        }

        [Test]
        public void ToHttps_ignores_whitespace_at_beginning_of_string()
        {
            Assert.That("  http://host.example.com".ToHttps(), Is.EqualTo("https://host.example.com"));
        }

        [Test]
        public void ToHttps_is_not_case_sensitive()
        {
            Assert.That("HTTP://HOST.EXAMPLE.COM".ToHttps(), Is.EqualTo("https://HOST.EXAMPLE.COM"));
        }

        [Test]
        public void Can_parse_commands()
        {
            Assert.That("COUNT".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[COUNT:]" }));
            Assert.That("COUNT(*)".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[COUNT:*]" }));
            Assert.That("COUNT(DISTINCT Name)".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[COUNT:DISTINCT Name]" }));
            Assert.That("COUNT('Name')".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[COUNT:'Name']" }));
            Assert.That("COUNT(\"Name\")".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[COUNT:\"Name\"]" }));
            Assert.That("COUNT('N,a(m\"e')".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[COUNT:'N,a(m\"e']" }));
            Assert.That("COUNT(*,'foo',1)".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[COUNT:*|'foo'|1]" }));
            Assert.That("COUNT( * , 'foo' , 1 )".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[COUNT:*|'foo'|1]" }));
            Assert.That("Count(*), Min(Age), Max(Age), Sum(Id)".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(
                new[] { "[Count:*]", "[Min:Age]", "[Max:Age]", "[Sum:Id]" }));
            Assert.That("Count(*,\",\"), Min(Age,')'), Max(Age,1), Sum(Id,Foo,2.0)".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(
                new[] { "[Count:*|\",\"]", "[Min:Age|')']", "[Max:Age|1]", "[Sum:Id|Foo|2.0]" }));

            Assert.That("Field1,Field2".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[Field1:]", "[Field2:]" }));
        }

        [Test]
        public void Can_parse_commands_with_Aliases()
        {
            Assert.That("COUNT(*) Count".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[COUNT:*] Count" }));
            Assert.That("COUNT(DISTINCT LivingStatus) as UniqueStatus".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[COUNT:DISTINCT LivingStatus] as UniqueStatus" }));
            Assert.That("MIN(Age) MinAge".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[MIN:Age] MinAge" }));
            Assert.That("Count(*) count, Min(Age) min, Max(Age) max, Sum(Id) sum".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] {
                "[Count:*] count", "[Min:Age] min", "[Max:Age] max", "[Sum:Id] sum"
            }));
        }
    }
}
