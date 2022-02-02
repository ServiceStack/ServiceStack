using System;
using NUnit.Framework;

namespace ServiceStack.Common.Tests.Models
{
    public class ShipperFactory
        : ModelFactoryBase<Shipper>
    {
        public override Shipper CreateInstance(int i)
        {
            var hex = ((i % 240) + 16).ToString("X");
            return new Shipper
            {
                Id = i,
                CompanyName = "Shipper" + i,
                DateCreated = new DateTime(i + 1 % 3000, (i % 11) + 1, (i % 27) + 1, 0, 0, 0, DateTimeKind.Utc),
                ShipperType = (ShipperType)(i % 3),
                UniqueRef = new Guid(hex + "D148A5-E5F1-4E5A-8C60-52E5A80ACCC6"),
            };
        }

        public override void AssertIsEqual(Shipper actual, Shipper expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.CompanyName, Is.EqualTo(expected.CompanyName));
            Assert.That(actual.ShipperType, Is.EqualTo(expected.ShipperType));
            Assert.That(actual.DateCreated, Is.EqualTo(expected.DateCreated));
            Assert.That(actual.UniqueRef, Is.EqualTo(expected.UniqueRef));
        }
    }
}