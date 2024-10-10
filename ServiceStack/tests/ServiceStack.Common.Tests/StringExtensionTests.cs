using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NUnit.Framework;
using ServiceStack.Text;
#if !NETFRAMEWORK
using HttpUtility = System.Net.WebUtility;
#endif

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class StringExtensionTests
    {
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
            Assert.That("COUNT()".ParseCommands().Map(x => x.ToDebugString()), Is.EquivalentTo(new[] { "[COUNT:]" }));
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

        [Test]
        public void Does_clean_input()
        {
            Assert.That("a.b+c@<d>&.com=|".SafeInput(), Is.EqualTo("a.b+c@d.com"));
            Assert.That("/a/b.c".SafeInput(), Is.EqualTo("/a/b.c"));
            Assert.That("1,000.00".SafeInput(), Is.EqualTo("1,000.00"));
            Assert.That("a b c".SafeInput(), Is.EqualTo("a b c"));
        }

        [Test]
        public void Does_parse_complex_arguments()
        {
            Assert.That("add(1,add(2,3))".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[add:1|add(2,3)]" }));
            Assert.That(" add ( 1, add(2,3) ) ".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[add:1|add(2,3)]" }));
            Assert.That("cat('1',cat(\"2\",'3'))".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[cat:'1'|cat(\"2\",'3')]" }));
            
            Assert.That(" add ( 1, add(add(2,3),4) ) ".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[add:1|add(add(2,3),4)]" }));
        }

        [Test]
        public void Does_preserve_js_literal_string()
        {
            Assert.That("li({ id:'id-{name}', className:'cls'})".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[li:{ id:'id-{name}', className:'cls'}]" }));
        }

        [Test]
        public void Does_preserve_ternary_expression()
        {
            Assert.That("filter( true ? 'Y' : 'N' )".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[filter:true ? 'Y' : 'N']" }));
        }

        [Test]
        public void Does_parse_binding_expressions()
        {
            Assert.That("var".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[var:]" }));
            Assert.That("var2".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[var2:]" }));
            Assert.That("var.prop".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[var.prop:]" }));
            Assert.That("var.prop.p2.p3".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[var.prop.p2.p3:]" }));
            Assert.That("var.prop[key]".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[var.prop[key]:]" }));
            Assert.That("var.prop['key']".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[var.prop['key']:]" }));
            Assert.That("var.prop[\"key\"]".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[var.prop[\"key\"]:]" }));
            
            Assert.That("fn(var)".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[fn:var]" }));
            Assert.That("fn(var.prop)".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[fn:var.prop]" }));
            Assert.That("fn(var.prop.p2.p3)".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[fn:var.prop.p2.p3]" }));
            Assert.That("fn(var.prop[key])".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[fn:var.prop[key]]" }));
            Assert.That("fn(var.prop['key'])".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[fn:var.prop['key']]" }));
            Assert.That("fn(var.prop[\"key\"])".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[fn:var.prop[\"key\"]]" }));
            
            Assert.That("fn(var,var)".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[fn:var|var]" }));
            Assert.That("fn(var.prop,var.prop)".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[fn:var.prop|var.prop]" }));
            Assert.That("fn(var.prop.p2.p3,var.prop.p2.p3)".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[fn:var.prop.p2.p3|var.prop.p2.p3]" }));
            Assert.That("fn(var.prop[key],var.prop[key])".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[fn:var.prop[key]|var.prop[key]]" }));
            Assert.That("fn(var.prop['key'],var.prop['key'])".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[fn:var.prop['key']|var.prop['key']]" }));
            Assert.That("fn(var.prop[\"key\"],var.prop[\"key\"])".ParseCommands().Map(x => x.ToDebugString()), Is.EqualTo(new[]{ "[fn:var.prop[\"key\"]|var.prop[\"key\"]]" }));
        }

        [Test]
        public void Can_parse_SELECT_Expression_with_concatenation()
        {
            var sql = "\"UserName\", \"Email\", \"FirstName\" AS \"GivenName\", \"LastName\" AS \"Surname\", \"FirstName\" || @1 || \"LastName\" AS FullName";

            var commands = sql.ParseCommands();

            var names = commands.Map(x => x.Original.ToString());
            Assert.That(names, Is.EquivalentTo(new[] {
                "\"UserName\"",
                "\"Email\"",
                "\"FirstName\" AS \"GivenName\"",
                "\"LastName\" AS \"Surname\"",
                "\"FirstName\" || @1 || \"LastName\" AS FullName",
            }));

            var aliasesOrNames = names.Map(x => x.LastRightPart("AS").Trim().StripQuotes() );
            aliasesOrNames.PrintDump();
            
            Assert.That(aliasesOrNames, Is.EquivalentTo(new[] {
                "UserName",
                "Email",
                "GivenName",
                "Surname",
                "FullName",
            }));            
        }

        [Test]
        public void Can_parse_SELECT_Expression_with_nested_functions()
        {
            var sql = "CONCAT(CONCAT(\"FirstName\", @1), \"LastName\") AS FullName, \"FirstName\" AS \"GivenName\", \"LastName\" AS \"Surname\", \"Email\", \"UserName\"";

            var commands = sql.ParseCommands();

            var names = commands.Map(x => x.Original.ToString());
            Assert.That(names, Is.EquivalentTo(new[] {
                "CONCAT(CONCAT(\"FirstName\", @1), \"LastName\") AS FullName",
                "\"FirstName\" AS \"GivenName\"",
                "\"LastName\" AS \"Surname\"",
                "\"Email\"",
                "\"UserName\"",
            }));

            var aliasesOrNames = names.Map(x => x.LastRightPart("AS").Trim().StripQuotes() );
            aliasesOrNames.PrintDump();
            
            Assert.That(aliasesOrNames, Is.EquivalentTo(new[] {
                "FullName",
                "GivenName",
                "Surname",
                "Email",
                "UserName",
            }));            
        }
    }
}
