using AutorestClient;
using NUnit.Framework;
using ServiceStack.OpenApi.Tests.Host;
using System;

namespace ServiceStack.OpenApi.Tests
{
    [TestFixture]
    public class ReturnTypeTests : GeneratedClientTestBase
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

        [Test]
        public void Can_get_returned_dto_dictionary()
        {
            using (var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri)))
            {
                var result = client.ReturnDictionaryDtoRequest.Get();

                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That(result["key1"].Id, Is.EqualTo(1));
                Assert.That(result["key2"].Id, Is.EqualTo(2));
            }
        }

        [Test]
        public void Can_get_returned_string_dictionary()
        {
            using (var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri)))
            {
                var result = client.ReturnDictionaryStringRequest.Get();

                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That(result["key1"], Is.EqualTo("value1"));
                Assert.That(result["key2"], Is.EqualTo("value2"));

            }
        }

        [Test]
        public void Can_Get_Returned_KeyPair()
        {
            using (var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri)))
            {
                var result = client.ReturnKeyValuePairRequest.Get();

                Assert.That(result.Key, Is.EqualTo("key1"));
                Assert.That(result.Value, Is.EqualTo("value1"));
            }
        }
    }
}
