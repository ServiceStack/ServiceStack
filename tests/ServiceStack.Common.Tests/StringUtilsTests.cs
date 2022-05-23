// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class StringUtilsTests
    {
        [Test]
        public void Does_replace_outside_of_quotes()
        {
            Assert.That("{it}".ReplaceOutsideOfQuotes("{", "{{", "}", "}}"), Is.EqualTo("{{it}}"));
            Assert.That("{it} '{it}'".ReplaceOutsideOfQuotes("{", "{{", "}", "}}"), Is.EqualTo("{{it}} '{it}'"));
            Assert.That("{it} `'{it}' {it}`".ReplaceOutsideOfQuotes("{", "{{", "}", "}}"), Is.EqualTo("{{it}} `'{it}' {it}`"));
            Assert.That("{it} `'{it}' {it}` {it}".ReplaceOutsideOfQuotes("{", "{{", "}", "}}"), Is.EqualTo("{{it}} `'{it}' {it}` {{it}}"));
            
            Assert.That("{ '{0:00}' | fmt(it) }\n".ReplaceOutsideOfQuotes("{", "{{", "}", "}}"), Is.EqualTo("{{ '{0:00}' | fmt(it) }}\n"));
        }

        [Test]
        public void HtmlStrip_unescapes_all_html_character_codes_correctly()
        {
            foreach (var value in Enumerable.Range(ushort.MinValue, ushort.MaxValue).Select(i => (ushort)i))
            {
                var expected = ((char)value).ToString(CultureInfo.InvariantCulture);

                var decimalNotation = $"&#{value};";
                var decimalActual = decimalNotation.StripHtml().ConvertHtmlCodes();
                Assert.AreEqual(expected, decimalActual);

                var hexNotation = $"&#x{value:X};";
                var hexActual = hexNotation.StripHtml().ConvertHtmlCodes();
                Assert.AreEqual(expected, hexActual);
            }

            foreach (var htmlNotation in StringUtils.HtmlCharacterCodes)
            {
                var actual = htmlNotation.Key.StripHtml().ConvertHtmlCodes();
                var expected = htmlNotation.Value;
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void HtmlStrip_fixes_actual_production_example()
        {
            var encoded = @"Du tr&auml;umst von node.js und willst mithelfen einen Request in unter 50ms auszuliefern? PHP ist Deine Muttersprache und Dein Verstand schreit nach immer neuen Herausforderungen? Dann passt du zu uns. Bringe Deine Kompetenzen in ein Unternehmen ein, das Dir neben hervorragenden Arbeitsbedingungen wirklich etwas zu bieten hat: Perspektiven!
Werde Teil unseres Teams und gestalte aktiv die technische Zukunft der weltweit gr&ouml;&szlig;ten Online Hotelsuche mit. Arbeite mit neuesten Technologien in einem global aufgestellten Unternehmen. Nutze die Freiheit Bestehendes in Frage zu stellen, Deinen Horizont zu erweitern und Neues zu entwickeln.";
            var expected = @"Du träumst von node.js und willst mithelfen einen Request in unter 50ms auszuliefern? PHP ist Deine Muttersprache und Dein Verstand schreit nach immer neuen Herausforderungen? Dann passt du zu uns. Bringe Deine Kompetenzen in ein Unternehmen ein, das Dir neben hervorragenden Arbeitsbedingungen wirklich etwas zu bieten hat: Perspektiven!
Werde Teil unseres Teams und gestalte aktiv die technische Zukunft der weltweit größten Online Hotelsuche mit. Arbeite mit neuesten Technologien in einem global aufgestellten Unternehmen. Nutze die Freiheit Bestehendes in Frage zu stellen, Deinen Horizont zu erweitern und Neues zu entwickeln.";
            var actual = encoded.StripHtml().ConvertHtmlCodes();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Does_convert_snake_case_to_PascalCase()
        {
            Assert.That(StringUtils.SnakeCaseToPascalCase(""), Is.EqualTo(""));
            Assert.That(StringUtils.SnakeCaseToPascalCase("a"), Is.EqualTo("A"));
            Assert.That(StringUtils.SnakeCaseToPascalCase("a_b"), Is.EqualTo("AB"));
            Assert.That(StringUtils.SnakeCaseToPascalCase("a1_b2"), Is.EqualTo("A1B2"));
            Assert.That(StringUtils.SnakeCaseToPascalCase("aa_bb"), Is.EqualTo("AaBb"));
            Assert.That(StringUtils.SnakeCaseToPascalCase("aaBb"), Is.EqualTo("AaBb"));
        }

        [Test]
        public void Does_split_fields()
        {
            Assert.That(StringUtils.SplitVarNames(""), Is.EqualTo(new string[0]));
            Assert.That(StringUtils.SplitVarNames("A"), Is.EqualTo(new[]{ "A"}));
            Assert.That(StringUtils.SplitVarNames("A,B,C"), Is.EqualTo(new[]{ "A","B","C" }));
            Assert.That(StringUtils.SplitVarNames("A, B , C"), Is.EqualTo(new[]{ "A","B","C" }));
            Assert.That(StringUtils.SplitVarNames("A, B , C, "), Is.EqualTo(new[]{ "A","B","C" }));
        }

        [Test]
        public void Can_parse_base_datauri()
        {
            var utf8Bytes = "abc".ToUtf8Bytes();
            var dataUri = "data:image/jpg;base64," + Convert.ToBase64String(utf8Bytes);
            var content = StaticContent.CreateFromDataUri(dataUri);
            Assert.That(content.MimeType, Is.EqualTo("image/jpg"));
            Assert.That(content.Data.ToArray(), Is.EqualTo(utf8Bytes));
        }

        [Test]
        public void Can_parse_svg_datauri()
        {
            var dataUri = Svg.GetDataUri(Svg.Icons.Male);
            var content = StaticContent.CreateFromDataUri(dataUri);
            Assert.That(content.MimeType, Is.EqualTo("image/svg+xml"));
            Assert.That(content.Data.FromUtf8().ToString(), Is.EqualTo(Svg.GetImage(Svg.Icons.Male)));
        }
    }
}