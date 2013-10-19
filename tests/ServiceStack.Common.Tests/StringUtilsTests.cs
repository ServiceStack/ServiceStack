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
                var expected = ((char)value).ToString(CultureInfo.InvariantCulture);

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
    }
}