using System;
using NUnit.Framework;
using ServiceStack.Model;
using ServiceStack.Logging;

namespace ServiceStack.Common.Tests.Models
{
    public class ModelWithFieldsOfNullableTypes
        : IHasIntId
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ModelWithFieldsOfNullableTypes));

        public int Id { get; set; }
        public int? NId { get; set; }

        public long? NLongId { get; set; }

        public Guid? NGuid { get; set; }

        public bool? NBool { get; set; }

        public DateTime? NDateTime { get; set; }

        public float? NFloat { get; set; }

        public double? NDouble { get; set; }

        public decimal? NDecimal { get; set; }

        public TimeSpan? NTimeSpan { get; set; }

        public static ModelWithFieldsOfNullableTypes Create(int id)
        {
            var row = new ModelWithFieldsOfNullableTypes
            {
                Id = id,
                NId = id,
                NBool = id % 2 == 0,
                NDateTime = DateTime.Now.AddDays(id),
                NFloat = 1.11f + id,
                NDouble = 1.11d + id,
                NGuid = Guid.NewGuid(),
                NLongId = 999 + id,
                NDecimal = id + 0.5m,
                NTimeSpan = TimeSpan.FromSeconds(id),
            };

            return row;
        }

        public static ModelWithFieldsOfNullableTypes CreateConstant(int id)
        {
            var row = new ModelWithFieldsOfNullableTypes
            {
                Id = id,
                NId = id,
                NBool = id % 2 == 0,
                NDateTime = new DateTime(1979, (id % 12) + 1, (id % 28) + 1),
                NFloat = 1.11f + id,
                NDouble = 1.11d + id,
                NGuid = new Guid(((id % 240) + 16).ToString("X") + "7DA519-73B6-4525-84BA-B57673B2360D"),
                NLongId = 999 + id,
                NDecimal = id + 0.5m,
                NTimeSpan = TimeSpan.FromSeconds(id),
            };

            return row;
        }

        public static void AssertIsEqual(ModelWithFieldsOfNullableTypes actual, ModelWithFieldsOfNullableTypes expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.NId, Is.EqualTo(expected.NId));
            Assert.That(actual.NGuid, Is.EqualTo(expected.NGuid));
            Assert.That(actual.NLongId, Is.EqualTo(expected.NLongId));
            Assert.That(actual.NBool, Is.EqualTo(expected.NBool));
            Assert.That(actual.NTimeSpan, Is.EqualTo(expected.NTimeSpan));
            Assert.That(actual.NDateTime, Is.EqualTo(expected.NDateTime).Within(TimeSpan.FromSeconds(1)));
            Assert.That(actual.NFloat, Is.EqualTo(expected.NFloat).Within(1f));
            Assert.That(actual.NDouble, Is.EqualTo(expected.NDouble).Within(1d));
        }
    }
}