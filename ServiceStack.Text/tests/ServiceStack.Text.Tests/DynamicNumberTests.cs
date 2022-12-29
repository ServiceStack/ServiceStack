using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public class DynamicNumberTests
    {
        [Test]
        public void Does_right_operation()
        {
            Assert.That(DynamicNumber.Add(8, 2) is int a && a == 10);
            Assert.That(DynamicNumber.Sub(8, 2) is int s && s == 6);
            Assert.That(DynamicNumber.Mul(8, 2) is int m && m == 16);
            Assert.That(DynamicNumber.Div(8, 2) is int d && d == 4);
        }

        private static object Add<T>(T lhs, T rhs)
        {
            var result = DynamicNumber.Add(lhs, rhs);
            "{0}: {1} + {2} = {3} ({4})".Print(typeof(T).Name, lhs, rhs, result, result.GetType());
            return result;
        }

        private static object Add2<T,R>(T lhs, R rhs)
        {
            var result = DynamicNumber.Add(lhs, rhs);
            "{0}: {1} + {2} = {3} ({4})".Print(typeof(T).Name, lhs, rhs, result, result.GetType());
            return result;
        }

        [Test]
        public void Returns_natural_number_type_when_both_are_the_same()
        {
            Assert.That(Add((byte)1, (byte)1) is int b && b == 2);
            Assert.That(Add((short)1, (short)1) is int s && s == 2);
            Assert.That(Add((char)1, (char)1) is int c && c == 2);
            Assert.That(Add(1, 1) is int i && i == 2);
            Assert.That(Add(1d, 1d) is double d && d == 2);
            Assert.That(Add(1f, 1f) is float f && f == 2);
            Assert.That(Add(1M, 1M) is decimal m && m == 2);
        }

        [Test]
        public void Does_upcast_when_using_different_integer_types()
        {
            Assert.That(Add2((sbyte)1, (byte)1) is int b && b == 2);
            Assert.That(Add2((byte)1, (short)1) is int s && s == 2);
            Assert.That(Add2((short)1, (ushort)1) is int us && us == 2);
            Assert.That(Add2((ushort)1, (int)1) is int i && i == 2);
            Assert.That(Add2((int)1, (uint)1) is uint ui && ui == 2);
            Assert.That(Add2((uint)1, (long)1) is long l && l == 2);
            Assert.That(Add2((long)1, (ulong)1) is ulong ul && ul == 2);
        }

        [Test]
        public void Does_upcast_when_using_different_float_types()
        {
            Assert.That(Add2((float)1, (double)1) is double d && d == 2);
            Assert.That(Add2((double)1, (decimal)1) is decimal m && m == 2);
        }

        [Test]
        public void Does_upcast_from_integer_to_float_type()
        {
            Assert.That(Add2((int)1, (float)1) is float f && f == 2);
            Assert.That(Add2((int)1, (double)1) is double d && d == 2);
            Assert.That(Add2((int)1, (decimal)1) is decimal m && m == 2);

            Assert.That(Add2((long)1, (float)1) is float fl && fl == 2);
            Assert.That(Add2((long)1, (double)1) is double dl && dl == 2);
            Assert.That(Add2((long)1, (decimal)1) is decimal ml && ml == 2);
        }

        [Test]
        public void Does_convert_string_to_appropriate_popular_type()
        {
            object o;
            Assert.That(DynamicNumber.TryParse("1", out o) && o is int i && i == 1);
            Assert.That(DynamicNumber.TryParse(int.MaxValue.ToString(), out o) && o is int imax && imax == int.MaxValue);
            Assert.That(DynamicNumber.TryParse((int.MaxValue + (long)1).ToString(), out o) && o is long l && l == int.MaxValue + (long)1);
            Assert.That(DynamicNumber.TryParse((long.MaxValue + (double)1).ToString(CultureInfo.InvariantCulture), out o) && o is double d ? d : 0, Is.EqualTo(long.MaxValue + (double)1).Within(10000));
            Assert.That(DynamicNumber.TryParse("1.1", out o) && o is double d2 && d2 == 1.1);
        }

        [Test]
        public void Can_convert_string_into_number_types()
        {
            Assert.That(("1").ConvertTo<int>(), Is.EqualTo(1));
            Assert.That(int.MaxValue.ToString().ConvertTo<int>(), Is.EqualTo(int.MaxValue));
            Assert.That((long.MaxValue + (double)1).ToString().ConvertTo<double>(), Is.EqualTo(long.MaxValue + (double)1).Within(10000));
            Assert.That(("1.1").ConvertTo<double>(), Is.EqualTo(1.1d));
        }

        [Test]
        public void Can_convert_all_types_that_fit_to_int()
        {
            Assert.That(((sbyte)1).ConvertTo<int>(), Is.EqualTo(1));
            Assert.That(((byte)1).ConvertTo<int>(), Is.EqualTo(1));
            Assert.That(((short)1).ConvertTo<int>(), Is.EqualTo(1));
            Assert.That(((ushort)1).ConvertTo<int>(), Is.EqualTo(1));
            Assert.That(((int)1).ConvertTo<int>(), Is.EqualTo(1));
            Assert.That(((uint)1).ConvertTo<int>(), Is.EqualTo(1));
            Assert.That(((long)1).ConvertTo<int>(), Is.EqualTo(1));
            Assert.That(((ulong)1).ConvertTo<int>(), Is.EqualTo(1));
            Assert.That(((float)1).ConvertTo<int>(), Is.EqualTo(1));
            Assert.That(((double)1).ConvertTo<int>(), Is.EqualTo(1));
            Assert.That(((decimal)1).ConvertTo<int>(), Is.EqualTo(1));
        }

        [Test]
        public void Can_convert_all_types_that_fit_to_double()
        {
            Assert.That(((sbyte)1).ConvertTo<double>(), Is.EqualTo(1d));
            Assert.That(((byte)1).ConvertTo<double>(), Is.EqualTo(1d));
            Assert.That(((short)1).ConvertTo<double>(), Is.EqualTo(1d));
            Assert.That(((ushort)1).ConvertTo<double>(), Is.EqualTo(1d));
            Assert.That(((int)1).ConvertTo<double>(), Is.EqualTo(1d));
            Assert.That(((uint)1).ConvertTo<double>(), Is.EqualTo(1d));
            Assert.That(((long)1).ConvertTo<double>(), Is.EqualTo(1d));
            Assert.That(((ulong)1).ConvertTo<double>(), Is.EqualTo(1d));
            Assert.That(((float)1).ConvertTo<double>(), Is.EqualTo(1d));
            Assert.That(((double)1).ConvertTo<double>(), Is.EqualTo(1d));
            Assert.That(((decimal)1).ConvertTo<double>(), Is.EqualTo(1d));
        }

        [Test]
        public void Can_convert_from_chars()
        {
            Assert.That('1'.ConvertTo<int>(), Is.EqualTo(1));
            Assert.That('1'.ConvertTo<long>(), Is.EqualTo(1L));
            Assert.That('1'.ConvertTo<double>(), Is.EqualTo(1d));
            Assert.That('1'.ConvertTo<decimal>(), Is.EqualTo(1m));
        }

        [Test]
        public void Can_convert_to_chars()
        {
            Assert.That(1.ConvertTo<char>(), Is.EqualTo('1'));
            Assert.That(1L.ConvertTo<char>(), Is.EqualTo('1'));
            Assert.That(1d.ConvertTo<char>(), Is.EqualTo('1'));
            Assert.That(1M.ConvertTo<char>(), Is.EqualTo('1'));
        }

        [Test]
        public void Can_convert_from_string()
        {
            Assert.That("1".ConvertTo<int>(), Is.EqualTo(1));
            Assert.That("1".ConvertTo<long>(), Is.EqualTo(1L));
            Assert.That("1".ConvertTo<double>(), Is.EqualTo(1d));
            Assert.That("1".ConvertTo<decimal>(), Is.EqualTo(1m));
        }

        [Test]
        public void Can_convert_to_string()
        {
            Assert.That(1.ConvertTo<string>(), Is.EqualTo("1"));
            Assert.That(1L.ConvertTo<string>(), Is.EqualTo("1"));
            Assert.That(1d.ConvertTo<string>(), Is.EqualTo("1"));
            Assert.That(1M.ConvertTo<string>(), Is.EqualTo("1"));
        }

        [Test]
        public void Can_convert_between_string_and_char()
        {
            Assert.That('a'.ConvertTo<string>(), Is.EqualTo("a"));
            Assert.That("a".ConvertTo<char>(), Is.EqualTo('a'));
        }

        [Test]
        public void Can_apply_operations_to_strings_containing_numbers()
        {
            var result = DynamicNumber.Add("1", "1");
            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void Dynamic_number_examples()
        {
            object objInt = 1;
            object objDouble = 1.1;

            Assert.That(DynamicNumber.Add(objInt, objDouble) is double d1 && d1 == 2.1);
            Assert.That(DynamicNumber.Multiply('2', "1.1") is double d2 && d2 == 2.2);
            Assert.That(DynamicNumber.TryParseIntoBestFit("1", out object result) && result is byte b && b == 1);
        }

        [Test]
        public void TryParseIntoBestFit_tests()
        {
            JsConfig.TryParseIntoBestFit = true;

            Assert.That(DynamicNumber.TryParse("1", out object result) && result is byte b && b == 1);

            JsConfig.TryParseIntoBestFit = false;
        }

        [Test]
        public void Can_get_Nullable_number()
        {
            int? null1 = 1;
            var number = DynamicNumber.GetNumber(null1.GetType());
            Assert.That(number.ToString(null1), Is.EqualTo("1"));
        }

        [Test]
        public void Can_text_for_numbers()
        {
            object x = null;
            Assert.That(DynamicNumber.TryParse("(", out x), Is.False);
            Assert.That(DynamicNumber.TryParse("-", out x), Is.False);
        }


    }
}