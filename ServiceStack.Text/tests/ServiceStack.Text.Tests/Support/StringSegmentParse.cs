using NUnit.Framework;
using ServiceStack.Text.Support;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ServiceStack.Text.Tests.Support
{
    [TestFixture]
    public class StringSpanParseTests
    {
        [Test]
        public void Can_parse_int32()
        {
            Assert.That("0".AsSpan().ParseInt32(), Is.EqualTo(0));
            Assert.That("-0".AsSpan().ParseInt32(), Is.EqualTo(0));
            Assert.That("1".AsSpan().ParseInt32(), Is.EqualTo(1));
            Assert.That(int.MaxValue.ToString().AsSpan().ParseInt32(), Is.EqualTo(int.MaxValue));
            Assert.That(int.MinValue.ToString().AsSpan().ParseInt32(), Is.EqualTo(int.MinValue));
            Assert.That("234".AsSpan().ParseInt32(), Is.EqualTo(234));
            Assert.That("    234  ".AsSpan().ParseInt32(), Is.EqualTo(234));
            Assert.That("234  ".AsSpan().ParseInt32(), Is.EqualTo(234));
            Assert.That("   234".AsSpan().ParseInt32(), Is.EqualTo(234));
            Assert.That("   -234    ".AsSpan().ParseInt32(), Is.EqualTo(-234));
            Assert.Throws<FormatException>(() => "".AsSpan().ParseInt32());
            Assert.Throws<FormatException>(() => "-01".AsSpan().ParseInt32());
            Assert.Throws<FormatException>(() => "   - 234    ".AsSpan().ParseInt32());
            Assert.Throws<FormatException>(() => "   2.34    ".AsSpan().ParseInt32());
            Assert.Throws<OverflowException>(() => "12345678901234567890".AsSpan().ParseInt32());
            Assert.Throws<FormatException>(() => "abbababab".AsSpan().ParseInt32());
            Assert.Throws<FormatException>(() => "x10".AsSpan().ParseInt32());
            Assert.Throws<FormatException>(() => "    1234  123".AsSpan().ParseInt32());
        }

        [Test]
        public void Can_parse_invalid_int32()
        {
            foreach (var data in Parse_Invalid_TestData())
            {
                Assert.Throws((Type)data[3], () => ((string) data[0]).AsSpan().ParseInt32());
            }
        }

        //ivalid tests data from 
        //https://github.com/dotnet/corefx/blob/df8d8ac7c49e6c4acdce2ea684d8815be5da6a25/src/System.Runtime/tests/System/Int32Tests.cs#L150
        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            // String is null, empty or entirely whitespace
            yield return new object[] { null, NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { null, NumberStyles.Any, null, typeof(FormatException) };

            yield return new object[] { "", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "", NumberStyles.Any, null, typeof(FormatException) };
            
            yield return new object[] { " \t \n \r ", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { " \t \n \r ", NumberStyles.Any, null, typeof(FormatException) };

            // String is garbage
            yield return new object[] { "Garbage", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "Garbage", NumberStyles.Any, null, typeof(FormatException) };

            // String has leading zeros
            yield return new object[] { "\0\0123", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "\0\0123", NumberStyles.Any, null, typeof(FormatException) };

            // String has internal zeros
            yield return new object[] { "1\023", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "1\023", NumberStyles.Any, null, typeof(FormatException) };

            // Integer doesn't allow hex, exponents, paretheses, currency, thousands, decimal
            yield return new object[] { "abc", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "1E23", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { "(123)", NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { 1000.ToString("C0"), NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { 1000.ToString("N0"), NumberStyles.Integer, null, typeof(FormatException) };
            yield return new object[] { 678.90.ToString("F2"), NumberStyles.Integer, null, typeof(FormatException) };

            // HexNumber
            yield return new object[] { "0xabc", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "&habc", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "G1", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "g1", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "+abc", NumberStyles.HexNumber, null, typeof(FormatException) };
            yield return new object[] { "-abc", NumberStyles.HexNumber, null, typeof(FormatException) };

            // AllowLeadingSign
            yield return new object[] { "+", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "-", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "+-123", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "-+123", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "- 123", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };
            yield return new object[] { "+ 123", NumberStyles.AllowLeadingSign, null, typeof(FormatException) };

            // AllowTrailingSign
            yield return new object[] { "123-+", NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "123+-", NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "123 -", NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "123 +", NumberStyles.AllowTrailingSign, null, typeof(FormatException) };

            // Parentheses has priority over CurrencySymbol and PositiveSign
            NumberFormatInfo currencyNegativeParenthesesFormat = new NumberFormatInfo()
            {
                CurrencySymbol = "(",
                PositiveSign = "))"
            };
            yield return new object[] { "(100))", NumberStyles.AllowParentheses | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowTrailingSign, currencyNegativeParenthesesFormat, typeof(FormatException) };

            // AllowTrailingSign and AllowLeadingSign
            yield return new object[] { "+123+", NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "+123-", NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "-123+", NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign, null, typeof(FormatException) };
            yield return new object[] { "-123-", NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign, null, typeof(FormatException) };

            // AllowLeadingSign and AllowParentheses
            yield return new object[] { "-(1000)", NumberStyles.AllowLeadingSign | NumberStyles.AllowParentheses, null, typeof(FormatException) };
            yield return new object[] { "(-1000)", NumberStyles.AllowLeadingSign | NumberStyles.AllowParentheses, null, typeof(FormatException) };

            // Not in range of Int32
            yield return new object[] { "2147483648", NumberStyles.Any, null, typeof(OverflowException) };
            yield return new object[] { "2147483648", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { "-2147483649", NumberStyles.Any, null, typeof(OverflowException) };
            yield return new object[] { "-2147483649", NumberStyles.Integer, null, typeof(OverflowException) };

            yield return new object[] { "9223372036854775808", NumberStyles.Integer, null, typeof(OverflowException) };
            yield return new object[] { "-9223372036854775809", NumberStyles.Integer, null, typeof(OverflowException) };
        }

        [Test]
        public void Can_parse_decimal()
        {
            Assert.That("1234.5678".AsSpan().ParseDecimal(), Is.EqualTo(1234.5678m));
            Assert.That("1234".AsSpan().ParseDecimal(), Is.EqualTo(1234m));
            Assert.Throws<FormatException>(() => ".".AsSpan().ParseDecimal());
            Assert.Throws<FormatException>(() => "".AsSpan().ParseDecimal());
            Assert.That("0".AsSpan().ParseDecimal(), Is.EqualTo(0));
            Assert.That("-0".AsSpan().ParseDecimal(), Is.EqualTo(0));
            Assert.That("0.".AsSpan().ParseDecimal(), Is.EqualTo(0));
            Assert.That("-0.".AsSpan().ParseDecimal(), Is.EqualTo(0));
            Assert.That(".1".AsSpan().ParseDecimal(), Is.EqualTo(.1m));
            Assert.That("-.1".AsSpan().ParseDecimal(), Is.EqualTo(-.1m));
            Assert.That("10.001".AsSpan().ParseDecimal(), Is.EqualTo(10.001m));
            Assert.That("  10.001".AsSpan().ParseDecimal(), Is.EqualTo(10.001m));
            Assert.That("10.001  ".AsSpan().ParseDecimal(), Is.EqualTo(10.001m));
            Assert.That(" 10.001  ".AsSpan().ParseDecimal(), Is.EqualTo(10.001m));
            Assert.That("-10.001".AsSpan().ParseDecimal(), Is.EqualTo(-10.001m));
            //large
            Assert.That("12345678901234567890".AsSpan().ParseDecimal(), Is.EqualTo(12345678901234567890m));
            Assert.That("12345678901234567890.12".AsSpan().ParseDecimal(), Is.EqualTo(12345678901234567890.12m));
            Assert.That(decimal.MaxValue.ToString(CultureInfo.InvariantCulture).AsSpan().ParseDecimal(), Is.EqualTo(decimal.MaxValue));
            Assert.That(decimal.MinValue.ToString(CultureInfo.InvariantCulture).AsSpan().ParseDecimal(), Is.EqualTo(decimal.MinValue));

            //exponent
            Assert.That("7.67e-6".AsSpan().ParseDecimal(), Is.EqualTo(7.67e-6f));
            Assert.That("10.001E3".AsSpan().ParseDecimal(), Is.EqualTo(10001m));
            Assert.That(".001e5".AsSpan().ParseDecimal(), Is.EqualTo(100m));
            Assert.That("10.001E-2".AsSpan().ParseDecimal(), Is.EqualTo(0.10001m));
            Assert.That("10.001e-8".AsSpan().ParseDecimal(), Is.EqualTo(0.00000010001m));
            Assert.That("2.e2".AsSpan().ParseDecimal(), Is.EqualTo(200m));
            Assert.Throws<FormatException>(() => ".e2".AsSpan().ParseDecimal());
            Assert.That("9.e+000027".AsSpan().ParseDecimal(), Is.EqualTo(decimal.Parse("9.e+000027", NumberStyles.Float, CultureInfo.InvariantCulture)));

            //allow thouthands
            Assert.That("1,234.5678".AsSpan().ParseDecimal(true), Is.EqualTo(1234.5678m));
            Assert.Throws<FormatException>(() => ",1234.5678".AsSpan().ParseDecimal(true));

        }

        [Test]
        public void Can_parse_guid()
        {
            Assert.That("{b6170a18-3dd7-4a9b-b5d6-21033b5ad162}".AsSpan().ParseGuid(), Is.EqualTo(new Guid("{b6170a18-3dd7-4a9b-b5d6-21033b5ad162}")));
            Assert.That("b6170a18-3dd7-4a9b-b5d6-21033b5ad162".AsSpan().ParseGuid(), Is.EqualTo(new Guid("{b6170a18-3dd7-4a9b-b5d6-21033b5ad162}")));
            Assert.That("b6170a183dd74a9bb5d621033b5ad162".AsSpan().ParseGuid(), Is.EqualTo(new Guid("{b6170a18-3dd7-4a9b-b5d6-21033b5ad162}")));

            foreach (var data in GuidStrings_Valid_TestData())
            {
                Assert.That(((string)data[0]).AsSpan().ParseGuid(), Is.EqualTo(data[2]));
            }
        }


        [Test]
        public void Can_parse_invalid_guid()
        {
            foreach (var data in GuidStrings_Invalid_TestData())
            {
                Assert.Throws((Type)data[1], () => ((string)data[0]).AsSpan().ParseGuid());
            }
        }


        private static readonly Guid s_testGuid = new Guid("a8a110d5-fc49-43c5-bf46-802db8f843ff");
        private static readonly Guid s_fullGuid = new Guid(uint.MaxValue, ushort.MaxValue, ushort.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

        //Tests from corefx tests
        //https://github.com/dotnet/corefx/blob/df8d8ac7c49e6c4acdce2ea684d8815be5da6a25/src/System.Runtime/tests/System/GuidTests.cs#L382
        public static IEnumerable<object[]> GuidStrings_Valid_TestData()
        {
            yield return new object[] { "a8a110d5fc4943c5bf46802db8f843ff", "N", s_testGuid };
            yield return new object[] { "  \r \n \t a8a110d5fc4943c5bf46802db8f843ff   \r \n \t  ", "N", s_testGuid };

            yield return new object[] { "00000000000000000000000000000000", "N", Guid.Empty };
            yield return new object[] { "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", "N", s_fullGuid };
            yield return new object[] { "ffffffffffffffffffffffffffffffff", "N", s_fullGuid };

            yield return new object[] { "a8a110d5-fc49-43c5-bf46-802db8f843ff", "D", s_testGuid };
            yield return new object[] { "  \r \n \t a8a110d5-fc49-43c5-bf46-802db8f843ff   \r \n \t  ", "D", s_testGuid };

            yield return new object[] { "00000000-0000-0000-0000-000000000000", "D", Guid.Empty };
            yield return new object[] { "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF", "D", s_fullGuid };
            yield return new object[] { "ffffffff-ffff-ffff-ffff-ffffffffffff", "D", s_fullGuid };

            //TODO: add hex support
            //yield return new object[] { "+ddddddd-+ddd-+ddd-+ddd-+ddddddddddd", "D", Guid.Parse("0ddddddd-0ddd-0ddd-0ddd-0ddddddddddd") };
            //yield return new object[] { "0xdddddd-0xdd-0xdd-0xdd-0xdddddddddd", "D", Guid.Parse("00dddddd-00dd-00dd-00dd-00dddddddddd") };
            //yield return new object[] { "+0Xddddd-+0Xd-+0Xd-+0Xd-+0Xddddddddd", "D", Guid.Parse("000ddddd-000d-000d-000d-000ddddddddd") };

            yield return new object[] { "{a8a110d5-fc49-43c5-bf46-802db8f843ff}", "B", s_testGuid };
            yield return new object[] { "  \r \n \t {a8a110d5-fc49-43c5-bf46-802db8f843ff}   \r \n \t  ", "B", s_testGuid };

            yield return new object[] { "{00000000-0000-0000-0000-000000000000}", "B", Guid.Empty };
            yield return new object[] { "{FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF}", "B", s_fullGuid };
            yield return new object[] { "{ffffffff-ffff-ffff-ffff-ffffffffffff}", "B", s_fullGuid };

            //TODO: add hex support 
            //yield return new object[] { "{+ddddddd-+ddd-+ddd-+ddd-+ddddddddddd}", "B", Guid.Parse("0ddddddd-0ddd-0ddd-0ddd-0ddddddddddd") };
            //yield return new object[] { "{0xdddddd-0xdd-0xdd-0xdd-0xdddddddddd}", "B", Guid.Parse("00dddddd-00dd-00dd-00dd-00dddddddddd") };
            //yield return new object[] { "{+0Xddddd-+0Xd-+0Xd-+0Xd-+0Xddddddddd}", "B", Guid.Parse("000ddddd-000d-000d-000d-000ddddddddd") };

            yield return new object[] { "(a8a110d5-fc49-43c5-bf46-802db8f843ff)", "P", s_testGuid };
            yield return new object[] { "  \r \n \t (a8a110d5-fc49-43c5-bf46-802db8f843ff)   \r \n \t  ", "P", s_testGuid };

            yield return new object[] { "(00000000-0000-0000-0000-000000000000)", "P", Guid.Empty };
            yield return new object[] { "(FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF)", "P", s_fullGuid };
            yield return new object[] { "(ffffffff-ffff-ffff-ffff-ffffffffffff)", "P", s_fullGuid };
        }

        //Tests from corefx tests
        //https://github.com/dotnet/corefx/blob/df8d8ac7c49e6c4acdce2ea684d8815be5da6a25/src/System.Runtime/tests/System/GuidTests.cs#L436
        public static IEnumerable<object[]> GuidStrings_Invalid_TestData()
        {
//            yield return new object[] { null, typeof(ArgumentNullException) }; // String is null

//          ReadOnlySpan<char> can't be null or empty
            yield return new object[] { "", typeof(FormatException) }; // String is invalid
            yield return new object[] { "     \t", typeof(FormatException) }; // String is invalid

            yield return new object[] { "ddddddddddddddddddddddddddddddd", typeof(FormatException) }; // Length < 32
            yield return new object[] { "ddddddddddddddddddddddddddddddddd", typeof(FormatException) }; // Length > 32
            yield return new object[] { "{dddddddddddddddddddddddddddddddd}", typeof(FormatException) }; // Surrounded by braces

            // Can't contain inner whitespace
            yield return new object[] { "d d d d d  dd d d d d d dd d d d dd d d dd d dd d d d d d d", typeof(FormatException) };
            yield return new object[] { "{d d d d d  dd d d d d d dd d d d dd d d dd d dd d d d d d d}", typeof(FormatException) };
            yield return new object[] { "(d d d d d  dd d d d d d dd d d d dd d d dd d dd d d d d d d)", typeof(FormatException) };

            yield return new object[] { "dddddddd-dddddddd-dddddddd", typeof(FormatException) }; // 8-8-8
            yield return new object[] { "dddddddd-dddddddd-dddddddd-ddddddddd", typeof(FormatException) }; // 8-8-8
            yield return new object[] { "dddddddd-dddddddd-dddddddd-dddddddd-dddddddd", typeof(FormatException) }; // 8-8-8-8

            yield return new object[] { "ddddddd-dddd-dddd-dddd-dddddddd", typeof(FormatException) }; // 7-4-4-4-8
            yield return new object[] { "ddddddddd-dddd-dddd-dddd-dddddddd", typeof(FormatException) }; // 9-4-4-4-8

            yield return new object[] { "dddddddd-ddd-dddd-dddd-dddddddd", typeof(FormatException) }; // 8-3-4-4-8
            yield return new object[] { "dddddddd-ddddd-dddd-dddd-dddddddd", typeof(FormatException) }; // 8-5-4-4-8

            yield return new object[] { "dddddddd-dddd-ddd-dddd-dddddddd", typeof(FormatException) }; // 8-4-3-4-8
            yield return new object[] { "dddddddd-dddd-ddddd-dddd-dddddddd", typeof(FormatException) }; // 8-4-5-4-8

            yield return new object[] { "dddddddd-dddd-dddd-ddd-dddddddd", typeof(FormatException) }; // 8-4-4-3-8
            yield return new object[] { "dddddddd-dddd-dddd-ddddd-dddddddd", typeof(FormatException) }; // 8-4-4-5-8

            yield return new object[] { "dddddddd-dddd-dddd-dddd-ddddddd", typeof(FormatException) }; // 8-4-4-4-7
            yield return new object[] { "dddddddd-dddd-dddd-dddd-ddddddddd", typeof(FormatException) }; // 8-4-4-9

            yield return new object[] { "{dddddddd-dddd-dddd-dddd-dddddddd", typeof(FormatException) }; // 8-4-4-4-8 with leading brace only
            yield return new object[] { "dddddddd-dddd-dddd-dddd-dddddddd}", typeof(FormatException) }; // 8-4-4-4-8 with trailing brace only

            yield return new object[] { "(dddddddd-dddd-dddd-dddd-dddddddd", typeof(FormatException) }; // 8-4-4-4-8 with leading parenthesis only
            yield return new object[] { "dddddddd-dddd-dddd-dddd-dddddddd)", typeof(FormatException) }; // 8-4-4-4-8 with trailing parenthesis only

            yield return new object[] { "(dddddddd-dddd-dddd-dddd-dddddddd}", typeof(FormatException) }; // 8-4-4-4-8 with leading parenthesis and trailing brace
            yield return new object[] { "{dddddddd-dddd-dddd-dddd-dddddddd)", typeof(FormatException) }; // 8-4-4-4-8 with trailing parenthesis and leading brace

            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} - missing group

            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without a trailing brace
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without a trailing brace

            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without a leading brace
            yield return new object[] { "0xdddddddd, 0xdddd,{0xdddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without a leading brace
            yield return new object[] { "{0xdddddddd, 0xdddd,0xdddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without a leading brace

            yield return new object[] { "(0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd))", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} with parentheses
            yield return new object[] { "(0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd})", typeof(FormatException) };// 8-4-4-{2-2-2-2-2-2-2-2} with parentheses

            //yield return new object[] { "{0xdddddddd 0xdddd 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
            yield return new object[] { "{0xdddddddd, 0xdddd 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
            yield return new object[] { "{0xdddddddd, 0xdddd 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma

            yield return new object[] { "{dddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, dddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, dddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{dd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,dd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,dd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,dd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,dd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,dd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,dd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,dd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix

            yield return new object[] { "{0x, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0x, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0x,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0x,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0x,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0x,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0x,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0x,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0x,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x

            // Invalid length of 1st argument
            //yield return new object[] { "{0xddddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 9-4-4-{2-2-2-2-2-2-2-2}

            // Invalid length of 2nd agument
            //yield return new object[] { "{0xdddddddd, 0xddddddddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) };
            yield return new object[] { "{0xdddddddd, 0xddddddd, 0xdddd,{0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xddddd, 0xdddd,{0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 3rd argument
            //yield return new object[] { "{0xdddddddd, 0xdddd, 0xddddddddd,{0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xddddddd,{0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xddddd,{0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 4th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdddddddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 5th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdddddddd,0xdd,0xddd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xddd,0xdd,0xddd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 6th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdddddddd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 7th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdddddddd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xddd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 8th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdddddddd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xddd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 9th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xddd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 10th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdddddddd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xddd,0xdd}}", typeof(FormatException) };

            // Invalid length of 11th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdddddddd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xddd}}", typeof(FormatException) };

            // Missing a comma
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd0xdd}}", typeof(FormatException) };

            // Extra characters at the end
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}abc", typeof(FormatException) };

            // Missing all components (ordered)
            yield return new object[] { "{", typeof(FormatException) };
            yield return new object[] { "{0x", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0x", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0x", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0x}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0x}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0x}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0x}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0x}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0x}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}", typeof(FormatException) };

            // Invalid hex
            yield return new object[] { "Gddddddddddddddddddddddddddddddd", typeof(FormatException) };
            yield return new object[] { "ddddddddGddddddddddddddddddddddd", typeof(FormatException) };
            yield return new object[] { "ddddddddddddGddddddddddddddddddd", typeof(FormatException) };
            yield return new object[] { "ddddddddddddddddGddddddddddddddd", typeof(FormatException) };
            yield return new object[] { "ddddddddddddddddddddGddddddddddd", typeof(FormatException) };

            yield return new object[] { "-ddddddd-dddd-dddd-dddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "(-ddddddd-dddd-dddd-dddd-dddddddddddd)", typeof(FormatException) };
            yield return new object[] { "{-ddddddd-dddd-dddd-dddd-dddddddddddd}", typeof(FormatException) };
            yield return new object[] { "Gddddddd-dddd-dddd-dddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "dGdddddd-dddd-dddd-dddd-dddddddddddd", typeof(FormatException) };

            yield return new object[] { "dddddddd--ddd-dddd-dddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "{dddddddd--ddd-dddd-dddd-dddddddddddd)", typeof(FormatException) };
            yield return new object[] { "(dddddddd--ddd-dddd-dddd-dddddddddddd}", typeof(FormatException) };
            yield return new object[] { "dddddddd-Gddd-dddd-dddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "dddddddd-dGdd-dddd-dddd-dddddddddddd", typeof(FormatException) };

            yield return new object[] { "dddddddd-dddd--ddd-dddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "{dddddddd-dddd--ddd-dddd-dddddddddddd)", typeof(FormatException) };
            yield return new object[] { "(dddddddd-dddd--ddd-dddd-dddddddddddd}", typeof(FormatException) };
            yield return new object[] { "dddddddd-dddd-Gddd-dddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "dddddddd-dddd-dGdd-dddd-dddddddddddd", typeof(FormatException) };

            yield return new object[] { "dddddddd-dddd-dddd--ddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "{dddddddd-dddd-dddd--ddd-dddddddddddd}", typeof(FormatException) };
            yield return new object[] { "(dddddddd-dddd-dddd--ddd-dddddddddddd)", typeof(FormatException) };
            yield return new object[] { "dddddddd-dddd-dddd-Gddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "dddddddd-dddd-dddd-dGdd-dddddddddddd", typeof(FormatException) };

            yield return new object[] { "dddddddd-dddd-dddd-dddd--ddddddddddd", typeof(FormatException) };
            yield return new object[] { "{dddddddd-dddd-dddd-dddd--ddddddddddd}", typeof(FormatException) };
            yield return new object[] { "(dddddddd-dddd-dddd-dddd--ddddddddddd)", typeof(FormatException) };
            yield return new object[] { "dddddddd-dddd-dddd-dddd-Gddddddddddd", typeof(FormatException) };
            yield return new object[] { "dddddddd-dddd-dddd-dddd-dGdddddddddd", typeof(FormatException) };

            yield return new object[] { "{0xGddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdGdddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0x-dddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xGddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdGdd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xddddddd,0x-dddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xdddd,0xGddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdGdd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xddddddd,0xdddd,0x-dddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xddddddd,0xdddd,0xdddd,{0x-dd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0x-dd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0x-dd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0x-dd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0x-dd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0x-dd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x-dd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x-dd}}", typeof(FormatException) };
        }
    }
}
