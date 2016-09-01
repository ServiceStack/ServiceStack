// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !NETCORE_SUPPORT
using NUnit.Framework;
using ServiceStack.Testing;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class MockRestGatewayTests
    {
        [Test]
        public void Can_Mock_RestGateway()
        {
            var gateway = new MockRestGateway();

            var response = gateway.Get(new TestGetRequest { Id = 1 });
            Assert.That(response, Is.Null);

            gateway.ResultsFilter = (verb, type, dto) =>
                verb == HttpMethods.Post
                 ? new TestPostResponse {
                     Id = (int)dto.GetId(),
                     Verb = verb
                 }
                 : (object)new TestResponse {
                     Id = (int)dto.GetId(),
                     Verb = verb
                 };

            response = gateway.Get(new TestGetRequest { Id = 1 });
            Assert.That(response.Id, Is.EqualTo(1));
            Assert.That(response.Verb, Is.EqualTo("GET"));

            response = gateway.Send(new TestGetRequest { Id = 1 });
            Assert.That(response.Id, Is.EqualTo(1));
            Assert.That(response.Verb, Is.EqualTo("GET"));

            var postResponse = gateway.Post(new TestPostRequest { Id = 2 });
            Assert.That(postResponse.Id, Is.EqualTo(2));
            Assert.That(postResponse.Verb, Is.EqualTo("POST"));
        }
    }

    public class TestGetRequest : IGet, IReturn<TestResponse>
    {
        public int Id { get; set; }
    }

    public class TestResponse
    {
        public int Id { get; set; }
        public string Verb { get; set; }
    }

    public class TestPostRequest : IGet, IReturn<TestPostResponse>
    {
        public int Id { get; set; }
    }

    public class TestPostResponse
    {
        public int Id { get; set; }
        public string Verb { get; set; }
    }
}
#endif