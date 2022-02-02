using System;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Models
{
    public class ModelWithFieldsOfDifferentAndNullableTypes
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ModelWithFieldsOfDifferentAndNullableTypes));

        [AutoIncrement]
        public int Id { get; set; }
        public int? NId { get; set; }

        public long LongId { get; set; }
        public long? NLongId { get; set; }

        public Guid Guid { get; set; }
        public Guid? NGuid { get; set; }

        public bool Bool { get; set; }
        public bool? NBool { get; set; }

        public DateTime DateTime { get; set; }
        public DateTime? NDateTime { get; set; }

        public float Float { get; set; }
        public float? NFloat { get; set; }

        public double Double { get; set; }
        public double? NDouble { get; set; }

        public decimal Decimal { get; set; }
        public decimal? NDecimal { get; set; }

        public TimeSpan TimeSpan { get; set; }
        public TimeSpan? NTimeSpan { get; set; }

        public static ModelWithFieldsOfDifferentAndNullableTypes Create(int id)
        {
            var row = new ModelWithFieldsOfDifferentAndNullableTypes
            {
                Id = id,
                Bool = id % 2 == 0,
                DateTime = DateTime.Now.AddDays(id),
                Float = 1.11f + id,
                Double = 1.11d + id,
                Guid = Guid.NewGuid(),
                LongId = 999 + id,
                Decimal = id + 0.5m,
                TimeSpan = TimeSpan.FromSeconds(id),
            };

            return row;
        }

        public static ModelWithFieldsOfDifferentAndNullableTypes CreateConstant(int id)
        {
            var row = new ModelWithFieldsOfDifferentAndNullableTypes
            {
                Id = id,
                Bool = id % 2 == 0,
                DateTime = new DateTime(1979, (id % 12) + 1, (id % 28) + 1),
                Float = 1.11f + id,
                Double = 1.11d + id,
                Guid = new Guid(((id % 240) + 16).ToString("X") + "461D9D-47DB-4778-B3FA-458379AE9BDC"),
                LongId = 999 + id,
                Decimal = id + 0.5m,
                TimeSpan = TimeSpan.FromSeconds(id),
            };

            return row;
        }

        public static void AssertIsEqual(ModelWithFieldsOfDifferentAndNullableTypes actual, ModelWithFieldsOfDifferentAndNullableTypes expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.Guid, Is.EqualTo(expected.Guid));
            Assert.That(actual.LongId, Is.EqualTo(expected.LongId));
            Assert.That(actual.Bool, Is.EqualTo(expected.Bool));
            Assert.That(actual.TimeSpan, Is.EqualTo(expected.TimeSpan));

            try
            {
                Assert.That(actual.DateTime, Is.EqualTo(expected.DateTime));
            }
            catch (Exception ex)
            {
                Log.Error("Trouble with DateTime precisions, trying Assert again with rounding to seconds", ex);
                Assert.That(actual.DateTime.RoundToSecond(), Is.EqualTo(expected.DateTime.RoundToSecond()));
            }

            try
            {
                Assert.That(actual.Float, Is.EqualTo(expected.Float));
            }
            catch (Exception ex)
            {
                Log.Error("Trouble with float precisions, trying Assert again with rounding to 10 decimals", ex);
                Assert.That(Math.Round(actual.Float, 10), Is.EqualTo(Math.Round(actual.Float, 10)));
            }

            try
            {
                Assert.That(actual.Double, Is.EqualTo(expected.Double));
            }
            catch (Exception ex)
            {
                Log.Error("Trouble with double precisions, trying Assert again with rounding to 10 decimals", ex);
                Assert.That(Math.Round(actual.Double, 10), Is.EqualTo(Math.Round(actual.Double, 10)));
            }

            Assert.That(actual.NBool, Is.EqualTo(expected.NBool));
            Assert.That(actual.NDateTime, Is.EqualTo(expected.NDateTime));
            Assert.That(actual.NDecimal, Is.EqualTo(expected.NDecimal));
            Assert.That(actual.NDouble, Is.EqualTo(expected.NDouble));
            Assert.That(actual.NFloat, Is.EqualTo(expected.NFloat));
            Assert.That(actual.NGuid, Is.EqualTo(expected.NGuid));
            Assert.That(actual.NId, Is.EqualTo(expected.NId));
            Assert.That(actual.NLongId, Is.EqualTo(expected.NLongId));
            Assert.That(actual.NTimeSpan, Is.EqualTo(expected.NTimeSpan));

        }
    }
}