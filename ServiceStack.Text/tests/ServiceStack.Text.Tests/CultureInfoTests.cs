using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class CultureInfoTests
        : TestBase
    {

        private CultureInfo previousCulture = CultureInfo.InvariantCulture;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
#if NETCORE
			previousCulture = CultureInfo.CurrentCulture;
			CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
#else
            previousCulture = Thread.CurrentThread.CurrentCulture;
            //Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-BR");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");
#endif
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
#if NETCORE
			CultureInfo.CurrentCulture = previousCulture;
#else
            Thread.CurrentThread.CurrentCulture = previousCulture;
#endif
        }

        [Test]
        public void Can_deserialize_type_with_doubles_in_different_culture()
        {
            var point = new Point { Latitude = -23.5707, Longitude = -46.57239 };
            SerializeAndCompare(point);
        }

        [Test]
        public void Can_deserialize_type_with_Single_in_different_culture()
        {
            Single single = (float)1.123;
            var txt = TypeSerializer.SerializeToString(single);

            Console.WriteLine(txt);
        }

        [Test]
        public void Serializes_doubles_using_InvariantCulture()
        {
            //Used in RedisClient
            var doubleUtf8 = 66121.202.ToUtf8Bytes();
            var doubleStr = doubleUtf8.FromUtf8Bytes();
            Assert.That(doubleStr, Is.EqualTo("66121.202"));
        }

        [Test]
        public void Serializes_long_double_without_E_notation()
        {
            //Used in RedisClient
            var doubleUtf8 = 1234567890123456d.ToUtf8Bytes();
            var doubleStr = doubleUtf8.FromUtf8Bytes();
            Assert.That(doubleStr, Is.EqualTo("1234567890123456"));
        }

        public class NumberClass
        {
            public int IntValue { get; set; }
            public uint UIntValue { get; set; }
            public long LongValue { get; set; }
            public ulong ULongValue { get; set; }
            public float FloatValue { get; set; }
            public double DoubleValue { get; set; }
            public decimal DecimalValue { get; set; }

            public static NumberClass Create(int i)
            {
                return new NumberClass
                {
                    IntValue = i * 1000,
                    UIntValue = (uint)(i * 1000),
                    LongValue = i * 1000,
                    ULongValue = (ulong)(i * 1000),
                    FloatValue = (float)(i * 1000 + .999),
                    DoubleValue = i * 1000 + .999,
                    DecimalValue = (decimal)(i * 1000 + .999),
                };
            }
        }

        [Test]
        public void Does_use_invariant_culture_for_numbers()
        {
            var dto = NumberClass.Create(1);
            dto.ToJson().Print();
            dto.ToJsv().Print();
            dto.ToCsv().Print();

            Assert.That(dto.ToJson(), Does.Not.Contain("1000,9"));
            Assert.That(dto.ToJsv(), Does.Not.Contain("1000,9"));
            Assert.That(dto.ToCsv(), Does.Not.Contain("1000,9"));
        }

    }
}
