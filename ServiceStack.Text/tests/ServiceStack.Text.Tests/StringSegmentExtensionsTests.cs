using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class StringSpanExtensionsTests
    {
        [Test]
        public void Can_SplitOnFirst_char_needle()
        {
            "user:pass@w:rd".AsSpan().SplitOnFirst(':', out var first, out var last);
            Assert.That(first.EqualTo("user"));
            Assert.That(last.EqualTo("pass@w:rd"));
        }

        [Test]
        public void Can_LeftPart_and_LeftPart_char_needle()
        {
            var str = "user:pass@w:rd".AsSpan();
            Assert.That(str.LeftPart(':').EqualTo("user"));
            
            str.SplitOnFirst(':', out var first, out var last);
            Assert.That(first.EqualTo("user"));
            Assert.That(str.RightPart(':').EqualTo("pass@w:rd"));

            str.SplitOnFirst(':', out first, out last);
            Assert.That(last.EqualTo("pass@w:rd"));

            Assert.That(str.LeftPart('|').EqualTo("user:pass@w:rd"));
            
            str.SplitOnFirst('|', out first, out last);
            Assert.That(first.EqualTo("user:pass@w:rd"));
            
            Assert.That(str.RightPart('|').EqualTo("user:pass@w:rd"));
            Assert.That(last.IsEmpty);
        }

        [Test]
        public void Can_SplitOnFirst_string_needle()
        {
            "user:pass@w:rd".AsSpan().SplitOnFirst(":", out var first, out var last);
            Assert.That(first.EqualTo("user"));
            Assert.That(last.EqualTo("pass@w:rd"));
        }

        [Test]
        public void Can_LeftPart_and_RightPart_string_needle()
        {
            var str = "user::pass@w:rd".AsSpan();
            
            Assert.That(str.LeftPart("::").EqualTo("user"));

            str.SplitOnFirst("::", out var first, out var last);
            Assert.That(first.EqualTo("user"));
            
            Assert.That(str.RightPart("::").EqualTo("pass@w:rd"));
            Assert.That(last.EqualTo("pass@w:rd"));

            Assert.That(str.LeftPart("||").EqualTo("user::pass@w:rd"));
            
            str.SplitOnFirst("||", out first, out last);
            Assert.That(first.EqualTo("user::pass@w:rd"));
            
            Assert.That(str.RightPart("||").EqualTo("user::pass@w:rd"));
            Assert.That(last.IsEmpty);
        }

        [Test]
        public void Can_SplitOnLast_char_needle()
        {
            "user:name:pass@word".AsSpan().SplitOnLast(':', out var first, out var last);
            Assert.That(first.EqualTo("user:name"));
            Assert.That(last.EqualTo("pass@word"));
        }

        [Test]
        public void Can_LastLeftPart_and_LastRightPart_char_needle()
        {
            var str = "user:name:pass@word".AsSpan();
            Assert.That(str.LastLeftPart(':').EqualTo("user:name"));

            str.SplitOnLast(':', out var first, out var last);
            Assert.That(first.EqualTo("user:name"));

            Assert.That(str.LastRightPart(':').EqualTo("pass@word"));
            
            Assert.That(last.EqualTo("pass@word"));

            Assert.That(str.LastLeftPart('|').EqualTo("user:name:pass@word"));

            str.SplitOnLast('|', out first, out last);
            Assert.That(first.EqualTo("user:name:pass@word"));
            Assert.That(str.LastRightPart('|').EqualTo("user:name:pass@word"));
            Assert.That(last.IsEmpty);
        }

        [Test]
        public void Can_SplitOnLast_string_needle()
        {
            "user:name:pass@word".AsSpan().SplitOnLast(":", out var first, out var last);
            Assert.That(first.EqualTo("user:name"));
            Assert.That(last.EqualTo("pass@word"));
        }

        [Test]
        public void Can_LastLeftPart_and_LastRightPart_string_needle()
        {
            var str = "user::name::pass@word".AsSpan();
            Assert.That(str.LastLeftPart("::").EqualTo("user::name"));

            str.SplitOnLast("::", out var first, out var last);
            Assert.That(first.EqualTo("user::name"));
            Assert.That(str.LastRightPart("::").EqualTo("pass@word"));
            Assert.That(last.EqualTo("pass@word"));

            Assert.That(str.LastLeftPart("||").EqualTo("user::name::pass@word"));

            str.SplitOnLast("||", out first, out last);
            Assert.That(first.EqualTo("user::name::pass@word"));
            Assert.That(str.LastRightPart("||").EqualTo("user::name::pass@word"));
            Assert.That(last.IsEmpty);
        }

        private static readonly char DirSep = Path.DirectorySeparatorChar;
        private static readonly char AltDirSep = '/';

        [Test]
        public void Does_get_ParentDirectory()
        {
            var dirSep = DirSep;
            var filePath = $"path{dirSep}to{dirSep}file".AsSpan();
            Assert.That(filePath.ParentDirectory().EqualTo("path{0}to".FormatWith(dirSep)));
            Assert.That(filePath.ParentDirectory().ParentDirectory().EqualTo("path".FormatWith(dirSep)));
            Assert.That(filePath.ParentDirectory().ParentDirectory().ParentDirectory().IsEmpty);

            var filePathWithExt = $"path{dirSep}to{dirSep}file/".AsSpan();
            Assert.That(filePathWithExt.ParentDirectory().EqualTo($"path{dirSep}to"));
            Assert.That(filePathWithExt.ParentDirectory().ParentDirectory().EqualTo("path"));
            Assert.That(filePathWithExt.ParentDirectory().ParentDirectory().ParentDirectory().IsEmpty);
        }

        [Test]
        public void Does_get_ParentDirectory_of_AltDirectorySeperator()
        {
            var dirSep = AltDirSep;
            var filePath = $"path{dirSep}to{dirSep}file".AsSpan();
            Assert.That(filePath.ParentDirectory().EqualTo($"path{dirSep}to"));
            Assert.That(filePath.ParentDirectory().ParentDirectory().EqualTo("path"));
            Assert.That(filePath.ParentDirectory().ParentDirectory().ParentDirectory().IsEmpty);

            var filePathWithExt = $"path{dirSep}to{dirSep}file{dirSep}".AsSpan();
            Assert.That(filePathWithExt.ParentDirectory().EqualTo($"path{dirSep}to"));
            Assert.That(filePathWithExt.ParentDirectory().ParentDirectory().EqualTo("path"));
            Assert.That(filePathWithExt.ParentDirectory().ParentDirectory().ParentDirectory().IsEmpty);
        }

        [Test]
        public void Does_not_alter_filepath_without_extension()
        {
            var path = "path/dir.with.dot/to/file".AsSpan();
            Assert.That(path.WithoutExtension().EqualTo(path));

            Assert.That("path/to/file.ext".AsSpan().WithoutExtension().EqualTo("path/to/file"));
        }

        //[TestCase(null, null)]
        //[TestCase("/", "")]
        //[TestCase("/a", "")]
        [TestCase("/a.b", ".b")]
        [TestCase("/a.b.c", ".c")]
        [TestCase("/{a.b}.c", ".c")]
        [TestCase("/:=#%$@{a.b}.c", ".c")]
        public void Does_get_Path_extension(string actual, string expected)
        {
            Assert.That(actual.AsSpan().GetExtension().EqualTo(Path.GetExtension(actual)));
            Assert.That(actual.AsSpan().GetExtension().EqualTo(expected));
        }

        //TODO: impl if needed
        //         0         1
        //         01234567890123456789
        //[TestCase("text with /* and <!--", "<!--", "/*", 10)]
        //[TestCase("text with /* and <!--", "<!--x", "/*", 10)]
        //[TestCase("text with /* and <!--", "<!--", "/*x", 17)]
        //[TestCase("text with /* and <!--", "<!--x", "/*x", -1)]
        //public void Does_find_IndexOfAny_strings(string text, string needle1, string needle2, int expectedPos)
        //{
        //    var pos = text.AsSpan().IndexOfAny(needle1, needle2);
        //    Assert.That(pos, Is.EqualTo(expectedPos));
        //}

        //[Test]
        //public void Does_ExtractContent_first_pattern_from_Document_without_marker()
        //{
        //    var text = "text with random <!--comment--> and Contents: <!--Contents--> are here";
        //    var extract = text.ExtractContents("<!--", "-->");

        //    Assert.That(extract, Is.EqualTo("comment"));
        //}

        //[Test]
        //public void Does_ExtractContents_from_Document()
        //{
        //    var text = "text with random <!--comment--> and Contents: <!--Contents--> are here";
        //    var extract = text.ExtractContents("Contents:", "<!--", "-->");

        //    Assert.That(extract, Is.EqualTo("Contents"));
        //}

        //[Test]
        //public void Can_Url_Encode_String()
        //{
        //    var text = "This string & has % unsafe ? characters for )_(*&^%$$^$@# a query string".AsSpan();

        //    var encoded = text.UrlEncode();

        //    Assert.That(encoded,
        //        Is.EqualTo("This+string+%26+has+%25+unsafe+%3f+characters+for+%29%5f%28%2a%26%5e%25%24%24%5e%24%40%23+a+query+string"));

        //    var decoded = encoded.UrlDecode();

        //    Assert.That(decoded, Is.EqualTo(text));
        //}

        //[Test]
        //public void Can_UrlEncode_in_upper_case()
        //{
        //    var chars = "/=";
        //    Assert.That(chars.UrlEncode(), Is.EqualTo("%2f%3d"));
        //    Assert.That(chars.UrlEncode(upperCase:true), Is.EqualTo("%2F%3D"));
        //}

        //[Test]
        //public void Can_ToCamelCase_String()
        //{
        //    Assert.That("U".ToCamelCase(), Is.EqualTo("u"));
        //    Assert.That("UU".ToCamelCase(), Is.EqualTo("uu"));
        //    Assert.That("UUU".ToCamelCase(), Is.EqualTo("uuu"));
        //    Assert.That("UUUU".ToCamelCase(), Is.EqualTo("uuuu"));
        //    Assert.That("l".ToCamelCase(), Is.EqualTo("l"));
        //    Assert.That("ll".ToCamelCase(), Is.EqualTo("ll"));
        //    Assert.That("lll".ToCamelCase(), Is.EqualTo("lll"));
        //    Assert.That("llll".ToCamelCase(), Is.EqualTo("llll"));
        //    Assert.That("Ul".ToCamelCase(), Is.EqualTo("ul"));
        //    Assert.That("Ull".ToCamelCase(), Is.EqualTo("ull"));
        //    Assert.That("Ulll".ToCamelCase(), Is.EqualTo("ulll"));
        //    Assert.That("UUl".ToCamelCase(), Is.EqualTo("uUl"));
        //    Assert.That("UUll".ToCamelCase(), Is.EqualTo("uUll"));
        //    Assert.That("UUUl".ToCamelCase(), Is.EqualTo("uuUl"));
        //    Assert.That("lU".ToCamelCase(), Is.EqualTo("lU"));
        //    Assert.That("lUl".ToCamelCase(), Is.EqualTo("lUl"));
        //    Assert.That("lUll".ToCamelCase(), Is.EqualTo("lUll"));
        //    Assert.That("llU".ToCamelCase(), Is.EqualTo("llU"));
        //    Assert.That("llUl".ToCamelCase(), Is.EqualTo("llUl"));
        //    Assert.That("lllU".ToCamelCase(), Is.EqualTo("lllU"));
        //    Assert.That("llUlll".ToCamelCase(), Is.EqualTo("llUlll"));
        //    Assert.That("lllUlll".ToCamelCase(), Is.EqualTo("lllUlll"));
        //    Assert.That("lllUUUlll".ToCamelCase(), Is.EqualTo("lllUUUlll"));
        //    Assert.That("lllUlllUlll".ToCamelCase(), Is.EqualTo("lllUlllUlll"));
        //    Assert.That("".ToCamelCase(), Is.EqualTo(""));
        //    Assert.That(((string)null).ToCamelCase(), Is.EqualTo((string)null));
        //}

        //[Test]
        //public void Can_ToTitleCase_String()
        //{
        //    var text = "Abc_def";

        //    var ttc = text.ToTitleCase();
        //    Assert.That(ttc, Is.EqualTo("AbcDef"));
        //}

        //[Test]
        //public void Can_ToTitleCase_Empty_String()
        //{
        //    var text = "";

        //    var ttc = text.ToTitleCase();
        //    Assert.That(ttc, Is.EqualTo(""));
        //}

        //[Test]
        //public void Can_Url_Encode_Unicode_String()
        //{
        //    var text = "This string & has % 权뜑簒㮐ᾟ䗚璥趮⚦䭌䳅浝䕌ਥ⤧笫 characters";

        //    var encoded = text.UrlEncode();

        //    Assert.That(encoded, Is.EqualTo("This+string+%26+has+%25+%e6%9d%83%eb%9c%91%e7%b0%92%e3%ae%90%e1%be%9f" +
        //        "%e4%97%9a%e7%92%a5%e8%b6%ae%e2%9a%a6%e4%ad%8c%e4%b3%85%e6%b5%9d%e4%95%8c%e0%a8%a5%e2%a4%a7%e7%ac%ab+characters"));

        //    var decoded = encoded.UrlDecode();

        //    Assert.That(decoded, Is.EqualTo(text));
        //}

//        [Test]
//        public void Can_trim_prefixes()
//        {
//            Assert.That("/www_deploy/path/info".TrimPrefixes("/www_deploy", "~/www_deploy"),
//                Is.EqualTo("/path/info"));
//            Assert.That("~/www_deploy/path/info".TrimPrefixes("/www_deploy", "~/www_deploy"),
//                Is.EqualTo("/path/info"));
//            Assert.That("/path/info".TrimPrefixes("/www_deploy", "~/www_deploy"),
//                Is.EqualTo("/path/info"));
//        }
//
//        [Test]
//        public void Can_read_lines()
//        {
//            Assert.That((null as string).ReadLines().Count(), Is.EqualTo(0));
//            Assert.That("".ReadLines().Count(), Is.EqualTo(0));
//            Assert.That("a".ReadLines().Count(), Is.EqualTo(1));
//            Assert.That("a\nb".ReadLines().Count(), Is.EqualTo(2));
//            Assert.That("a\r\nb".ReadLines().Count(), Is.EqualTo(2));
//        }
//
//        [Test]
//        public void Can_ParseKeyValueText()
//        {
//            Assert.That("".ParseKeyValueText().Count, Is.EqualTo(0));
//            Assert.That("a".ParseKeyValueText().Count, Is.EqualTo(1));
//            Assert.That("a".ParseKeyValueText()["a"], Is.Null);
//            Assert.That("a ".ParseKeyValueText().Count, Is.EqualTo(1));
//            Assert.That("a ".ParseKeyValueText()["a"], Is.EqualTo(""));
//            Assert.That("a b".ParseKeyValueText()["a"], Is.EqualTo("b"));
//            Assert.That("a b c".ParseKeyValueText()["a"], Is.EqualTo("b c"));
//            Assert.That("a  b c ".ParseKeyValueText()["a"], Is.EqualTo("b c"));
//            Assert.That("a b\nc d".ParseKeyValueText()["c"], Is.EqualTo("d"));
//            Assert.That("a b\r\nc d".ParseKeyValueText()["c"], Is.EqualTo("d"));
//        }

        [Test]
        public void Can_SafeSubstring_with_no_length()
        {

            var input = "TestString".AsSpan();
            Assert.That(input.SafeSubstring(0).EqualTo("TestString"));
            Assert.That(input.SafeSubstring(2).EqualTo("stString"));
            Assert.That(input.SafeSubstring(20).IsEmpty);
        }

        [Test]
        public void Can_SafeSubstring_with_length()
        {
            var input = "TestString".AsSpan();
            Assert.That(input.SafeSubstring(0, 4).EqualTo("Test"));
            Assert.That(input.SafeSubstring(2, 4).EqualTo("stSt"));
            Assert.That(input.SafeSubstring(20, 4).IsEmpty);
            Assert.That(input.SafeSubstring(0, 20).EqualTo("TestString"));
        }

//        [Test]
//        public void Can_convert_ToPascalCase()
//        {
//            Assert.That(((string)null).ToPascalCase(), Is.Null);
//            Assert.That("".ToPascalCase(), Is.EqualTo(""));
//            Assert.That("a".ToPascalCase(), Is.EqualTo("A"));
//            Assert.That("aB".ToPascalCase(), Is.EqualTo("AB"));
//            Assert.That("AB".ToPascalCase(), Is.EqualTo("Ab"));
//            Assert.That("aaBb".ToPascalCase(), Is.EqualTo("AaBb"));
//            Assert.That("aaBbCc".ToPascalCase(), Is.EqualTo("AaBbCc"));
//            Assert.That("a_B".ToPascalCase(), Is.EqualTo("AB"));
//            Assert.That("a_b".ToPascalCase(), Is.EqualTo("AB"));
//            Assert.That("aa_bb".ToPascalCase(), Is.EqualTo("AaBb"));
//            Assert.That("Aa_Bb".ToPascalCase(), Is.EqualTo("AaBb"));
//            Assert.That("AA_BB".ToPascalCase(), Is.EqualTo("AaBb"));
//        }
    }
}
