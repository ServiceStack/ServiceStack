using AutorestClient;
using NUnit.Framework;
using ServiceStack.OpenApi.Tests.Host;
using System;

namespace ServiceStack.OpenApi.Tests
{
    [TestFixture]
    public class ArraysTests : GeneratedClientTestBase
    {
        [Test]
        public void Can_get_generic_list()
        {
            var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri));

            var result = client.ReturnListRequest.Get();

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].Id, Is.EqualTo(1));
            Assert.That(result[1].Id, Is.EqualTo(2));
            Assert.That(result[2].Id, Is.EqualTo(3));
        }

        [Test]
        public void Can_get_array()
        {
            var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri));

            var result = client.ReturnArrayRequest.Get();

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].Id, Is.EqualTo(1));
            Assert.That(result[1].Id, Is.EqualTo(2));
            Assert.That(result[2].Id, Is.EqualTo(3));
        }

        [Test]
        public void Can_get_keyvaluepair()
        {
            var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri));

            var result = client.ReturnKeyValuePairRequest.Get();

            Assert.That(result.Key, Is.EqualTo("key1"));
            Assert.That(result.Value, Is.EqualTo("value1"));
        }

    }
}
