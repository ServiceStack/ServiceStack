using NUnit.Framework;
using System;
using System.Collections.Generic;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Tests.CsvTests
{
    [TestFixture]
    public class ObjectSerializerTests
    {
        [Test]
        public void MidnightAndNoonTestSerialization()
        {
            JsConfig.Reset();
            JsConfig<DateTime>.SerializeFn = null;
            JsConfig<DateTime>.Reset();

            JsConfig.AlwaysUseUtc = true;
            JsConfig.AssumeUtc = true;
            // Set the format for DatTimeFormatting explicitly using DateTimeSerializer.XsdDateTimeFormat because it is ISO8601 fractional seconds
            JsConfig.DateTimeFormat = DateTimeSerializer.XsdDateTimeFormat;

            var midnight = new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var noon = midnight.AddHours(12);
            var dotnetValues = new
            {
                Midnight = midnight.ToString("o"),
                Noon = noon.ToString("o")
            };
            var data = new object[] {
                    new POCO { DateTime = midnight },
                    new POCO { DateTime = noon }
                    };
            var csv = CsvSerializer.SerializeToCsv(data);
            // Reset back to defaults
            JsConfig.Reset();
            JsConfig<DateTime>.SerializeFn = null;
            JsConfig<DateTime>.Reset();

            Console.WriteLine(csv);

            const string endLineChars = "\r\n";
            Assert.AreEqual($"DateTime{endLineChars}" +
                            $"{dotnetValues.Midnight}{endLineChars}" +
                            $"{dotnetValues.Noon}{endLineChars}", csv);

            // Now don't use custom DateTimeFormat
            JsConfig.AlwaysUseUtc = true;
            JsConfig.AssumeUtc = true;            
            csv = CsvSerializer.SerializeToCsv(data);
            Console.WriteLine(csv);
            Assert.AreEqual($"DateTime{endLineChars}" +
                            $"2018-01-01{endLineChars}" +
                            $"2018-01-01T12:00:00Z{endLineChars}", csv);

            JsConfig.Reset();
            JsConfig<DateTime>.SerializeFn = null;
            JsConfig<DateTime>.Reset();
        }

        [Test]
        public void IEnumerableObjectSerialization()
        {
            var data = GenerateSampleData();

            JsConfig<DateTime>.SerializeFn =
                time => new DateTime(time.Ticks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss");

            var csv = CsvSerializer.SerializeToCsv(data);
            Console.WriteLine(csv);

            Assert.AreEqual("DateTime\r\n"
                + "2017-06-14 00:00:00\r\n"
                + "2017-01-31 01:23:45\r\n",
                csv);
        }

        [Test]
        public void IEnumerableObjectSerializationBaseline()
        {
            var data = new object[]
            {
                new { Value = true },
                new { Value = false },
                new { Value = new bool?() }
            };

            var csv = CsvSerializer.SerializeToCsv(data);
            Console.WriteLine(csv);

            Assert.AreEqual("Value\r\n"
                + "True\r\n"
                + "False\r\n"
                + "\r\n",
                csv);
        }

        [Test]
        public void IEnumerableObjectSerializationCustomSerializer()
        {
            var data = new object[]
            {
                new { Value = true },
                new { Value = false }
            };

            JsConfig<bool>.SerializeFn =
                value => value == true ? "Yes" : "No";

            var csv = CsvSerializer.SerializeToCsv(data);
            Console.WriteLine(csv);

            Assert.AreEqual("Value\r\n"
                + "Yes\r\n"
                + "No\r\n",
                csv);
        }

        [Test]
        public void IEnumerableObjectSerializationCustomSerializerOfNullableType()
        {
            var data = new object[]
            {
                new { Value = new bool?(true) },
                new { Value = new bool?(false) },
                new { Value = new bool?() }
            };

            JsConfig<bool?>.SerializeFn =
                value => value.HasValue ? (value == true ? "Yes" : "No") : "Maybe";

            var csv = CsvSerializer.SerializeToCsv(data);
            Console.WriteLine(csv);

            Assert.AreEqual("Value\r\n"
                + "Yes\r\n"
                + "No\r\n"
                + "\r\n",
                csv);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            JsConfig<bool>.SerializeFn = null;
            JsConfig<bool>.Reset();
            JsConfig<bool?>.SerializeFn = null;
            JsConfig<bool?>.Reset();
            JsConfig<DateTime>.SerializeFn = null;
            JsConfig<DateTime>.Reset();

            CsvConfig.Reset();
            JsConfig.Reset();
        }

        object[] GenerateSampleData()
        {
            return new object[] {
                new POCO
                {
                    DateTime = new DateTime(2017,6,14)
                },
                new POCO
                {
                    DateTime = new DateTime(2017,1,31, 01, 23, 45)
                }
             };
        }

        [Test]
        public void Can_serialize_text_with_unmatched_list_or_map_chars()
        {
            var src = new List<POCO2>
            {
                new POCO2
                {
                    Prop1 = "1",
                    Prop2 = JsWriter.ListStartChar + "2",
                    Prop3 = JsWriter.MapStartChar + "3",
                    Prop4 = "4",
                    Prop5 = "5"
                }
            };

            var csv = CsvSerializer.SerializeToCsv(src);
            var des = csv.FromCsv<List<POCO2>>();

            Assert.That(des[0].Prop1, Is.EqualTo(src[0].Prop1));
            Assert.That(des[0].Prop2, Is.EqualTo(src[0].Prop2));
            Assert.That(des[0].Prop3, Is.EqualTo(src[0].Prop3));
            Assert.That(des[0].Prop4, Is.EqualTo(src[0].Prop4));
            Assert.That(des[0].Prop5, Is.EqualTo(src[0].Prop5));
        }

        [Test]
        public void Can_serialize_csv_and_deserialize_List_string()
        {
            var list = new List<string>
            {
                "one",
                "two",
                "three"
            };

            var flatList = list.ToCsv();

            var originalList = flatList.FromCsv<List<string>>();

            Assert.That(originalList.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_serialize_csv_and_deserialize_List_primitives()
        {
            var list = new List<int>
            {
                1,
                2,
                3
            };

            var flatList = list.ToCsv();

            var originalList = flatList.FromCsv<List<int>>();

            Assert.That(originalList.Count, Is.EqualTo(3));
        }
    }

    public class POCO
    {
        public DateTime DateTime { get; set; }
    }

    public class POCO2
    {
        public string Prop1 { get; set; }
        public string Prop2 { get; set; }
        public string Prop3 { get; set; }
        public string Prop4 { get; set; }
        public string Prop5 { get; set; }
    }
}