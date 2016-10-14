// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class StringUtilsTests
    {
        [Test]
        public void HtmlStrip_unescapes_all_html_character_codes_correctly()
        {
            foreach (var value in Enumerable.Range(ushort.MinValue, ushort.MaxValue).Select(i => (ushort)i))
            {
#if NETCORE
                var expected = ((char)value).ToString();
#else
                var expected = ((char)value).ToString(CultureInfo.InvariantCulture);
#endif

                var decimalNotation = String.Format("&#{0};", value);
                var decimalActual = decimalNotation.StripHtml().ConvertHtmlCodes();
                Assert.AreEqual(expected, decimalActual);

                var hexNotation = String.Format("&#x{0:X};", value);
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
    }
}