using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class AdhocExtensionTests
    {
        static Func<T, object> Fn<T>(Func<T, object> fn)
        {
            return fn;
        }

        [Test]
        public void Does_return_AssignedValue()
        {
            Assert.That(Fn<Customer>(x => x.Name = "Foo").AssignedValue(), Is.EquivalentTo(
                new Dictionary<string, object> { { "Name", "Foo" } }));

            Assert.That(Fn<Customer>(x => x.Age = -1).AssignedValue(), Is.EquivalentTo(
                new Dictionary<string, object> { { "Age", -1 } }));

            Assert.Throws<ArgumentException>(() => Fn<Customer>(x => x.Id = 0).AssignedValue());
        }

        [Test]
        public void Does_return_object_keys()
        {
            Func<Customer, object> fn = x => new { x.Name, x.Orders };

            Assert.That(fn.ToObjectKeys(), Is.EquivalentTo(new[] { "Name", "Orders" }));
        }
    }
}