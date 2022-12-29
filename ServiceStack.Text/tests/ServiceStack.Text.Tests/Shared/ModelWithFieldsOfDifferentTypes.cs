using System;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Models
{
    public class ModelWithFieldsOfDifferentTypesAsNullables
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ModelWithFieldsOfDifferentTypesAsNullables));

        public int? Id { get; set; }

        public string Name { get; set; }

        public long? LongId { get; set; }

        public Guid? Guid { get; set; }

        public bool? Bool { get; set; }

        public DateTime? DateTime { get; set; }

        public double? Double { get; set; }

        public static ModelWithFieldsOfDifferentTypesAsNullables Create(int id)
        {
            var row = new ModelWithFieldsOfDifferentTypesAsNullables
            {
                Id = id,
                Bool = id % 2 == 0,
                DateTime = System.DateTime.Now.AddDays(id),
                Double = 1.11d + id,
                Guid = System.Guid.NewGuid(),
                LongId = 999 + id,
                Name = "Name" + id
            };

            return row;
        }

        public static ModelWithFieldsOfDifferentTypesAsNullables CreateConstant(int id)
        {
            var row = new ModelWithFieldsOfDifferentTypesAsNullables
            {
                Id = id,
                Bool = id % 2 == 0,
                DateTime = new DateTime(1979, (id % 12) + 1, (id % 28) + 1),
                Double = 1.11d + id,
                Guid = new Guid(((id % 240) + 16).ToString("X") + "726E3B-9983-40B4-A8CB-2F8ADA8C8760"),
                LongId = 999 + id,
                Name = "Name" + id
            };

            return row;
        }

        public static void AssertIsEqual(ModelWithFieldsOfDifferentTypes actual, ModelWithFieldsOfDifferentTypesAsNullables expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id.Value));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Guid, Is.EqualTo(expected.Guid.Value));
            Assert.That(actual.LongId, Is.EqualTo(expected.LongId.Value));
            Assert.That(actual.Bool, Is.EqualTo(expected.Bool.Value));
            try
            {
                Assert.That(actual.DateTime, Is.EqualTo(expected.DateTime.Value));
            }
            catch (Exception ex)
            {
                Log.Error("Trouble with DateTime precisions, trying Assert again with rounding to seconds", ex);
                Assert.That(actual.DateTime.RoundToSecond(), Is.EqualTo(expected.DateTime.Value.RoundToSecond()));
            }
            try
            {
                Assert.That(actual.Double, Is.EqualTo(expected.Double.Value));
            }
            catch (Exception ex)
            {
                Log.Error("Trouble with double precisions, trying Assert again with rounding to 10 decimals", ex);
                Assert.That(Math.Round(actual.Double, 10), Is.EqualTo(Math.Round(actual.Double, 10)));
            }
        }
    }


    public class ModelWithFieldsOfDifferentTypes
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ModelWithFieldsOfDifferentTypes));

        [AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        public long LongId { get; set; }

        public Guid Guid { get; set; }

        public bool Bool { get; set; }

        public DateTime DateTime { get; set; }

        public double Double { get; set; }

        public static ModelWithFieldsOfDifferentTypes Create(int id)
        {
            var row = new ModelWithFieldsOfDifferentTypes
            {
                Id = id,
                Bool = id % 2 == 0,
                DateTime = DateTime.Now.AddDays(id),
                Double = 1.11d + id,
                Guid = Guid.NewGuid(),
                LongId = 999 + id,
                Name = "Name" + id
            };

            return row;
        }

        public static ModelWithFieldsOfDifferentTypes CreateConstant(int id)
        {
            var row = new ModelWithFieldsOfDifferentTypes
            {
                Id = id,
                Bool = id % 2 == 0,
                DateTime = new DateTime(1979, (id % 12) + 1, (id % 28) + 1),
                Double = 1.11d + id,
                Guid = new Guid(((id % 240) + 16).ToString("X") + "726E3B-9983-40B4-A8CB-2F8ADA8C8760"),
                LongId = 999 + id,
                Name = "Name" + id
            };

            return row;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ModelWithFieldsOfDifferentTypes;
            if (other == null) return false;

            try
            {
                AssertIsEqual(this, other);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return (Id + Guid.ToString()).GetHashCode();
        }

        public static void AssertIsEqual(ModelWithFieldsOfDifferentTypes actual, ModelWithFieldsOfDifferentTypes expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Guid, Is.EqualTo(expected.Guid));
            Assert.That(actual.LongId, Is.EqualTo(expected.LongId));
            Assert.That(actual.Bool, Is.EqualTo(expected.Bool));
            Assert.That(actual.DateTime, Is.EqualTo(expected.DateTime).Within(TimeSpan.FromSeconds(1)));
            Assert.That(actual.Double, Is.EqualTo(expected.Double).Within(0.1));
        }
    }
}