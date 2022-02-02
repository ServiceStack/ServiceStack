using NUnit.Framework;
using ServiceStack.Common.Tests;
using ServiceStack.Web;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class RestPathResolutionUnitTests
        : TestBase
    {
        public RestPathResolutionUnitTests()
            : base(Config.ServiceStackBaseUri, typeof(ReverseService).Assembly)
        {
        }

        protected override void Configure(Funq.Container container)
        {

        }

        [SetUp]
        public void OnBeforeTest()
        {
            base.OnBeforeEachTest();
        }

        [Test]
        public void Can_execute_EchoRequest_rest_path()
        {
            var request = (EchoRequest)GetRequest("/echo/1/One");
            Assert.That(request, Is.Not.Null);
            Assert.That(request.Id, Is.EqualTo(1));
            Assert.That(request.String, Is.EqualTo("One"));
        }

        [Test]
        public void Can_call_EchoRequest_with_QueryString()
        {
            var request = (EchoRequest)GetRequest("/echo/1/One?Long=2&Bool=True");

            Assert.That(request.Id, Is.EqualTo(1));
            Assert.That(request.String, Is.EqualTo("One"));
            Assert.That(request.Long, Is.EqualTo(2));
            Assert.That(request.Bool, Is.EqualTo(true));
        }

        [Test]
        public void Can_get_empty_BasicWildcard()
        {
            var request = GetRequest<BasicWildcard>("/path");
            Assert.That(request.Tail, Is.Null);
            request = GetRequest<BasicWildcard>("/path/");
            Assert.That(request.Tail, Is.Null);
            request = GetRequest<BasicWildcard>("/path/a");
            Assert.That(request.Tail, Is.EqualTo("a"));
            request = GetRequest<BasicWildcard>("/path/a/b/c");
            Assert.That(request.Tail, Is.EqualTo("a/b/c"));
        }

        [Test]
        public void Can_call_WildCardRequest_with_alternate_matching_WildCard_defined()
        {
            var request = (WildCardRequest)GetRequest("/wildcard/1/aPath/edit");
            Assert.That(request.Id, Is.EqualTo(1));
            Assert.That(request.Path, Is.EqualTo("aPath"));
            Assert.That(request.Action, Is.EqualTo("edit"));
            Assert.That(request.RemainingPath, Is.Null);
        }

        [Test]
        public void Can_call_WildCardRequest_WildCard_mapping()
        {
            var request = (WildCardRequest)GetRequest("/wildcard/1/remaining/path/to/here");
            Assert.That(request.Id, Is.EqualTo(1));
            Assert.That(request.Path, Is.Null);
            Assert.That(request.Action, Is.Null);
            Assert.That(request.RemainingPath, Is.EqualTo("remaining/path/to/here"));
        }

        [Test]
        public void Can_call_WildCardRequest_WildCard_mapping_with_QueryString()
        {
            var request = (WildCardRequest)GetRequest("/wildcard/1/remaining/path/to/here?Action=edit");
            Assert.That(request.Id, Is.EqualTo(1));
            Assert.That(request.Path, Is.Null);
            Assert.That(request.Action, Is.EqualTo("edit"));
            Assert.That(request.RemainingPath, Is.EqualTo("remaining/path/to/here"));
        }

        [Test]
        public void Can_call_GET_on_VerbMatch_Services()
        {
            var request = (VerbMatch1)GetRequest(HttpMethods.Get, "/verbmatch");
            Assert.That(request.Name, Is.Null);

            var request2 = (VerbMatch1)GetRequest(HttpMethods.Get, "/verbmatch/arg");
            Assert.That(request2.Name, Is.EqualTo("arg"));
        }

        [Test]
        public void Can_call_POST_on_VerbMatch_Services()
        {
            var request = (VerbMatch2)GetRequest(HttpMethods.Post, "/verbmatch");
            Assert.That(request.Name, Is.Null);

            var request2 = (VerbMatch2)GetRequest(HttpMethods.Post, "/verbmatch/arg");
            Assert.That(request2.Name, Is.EqualTo("arg"));
        }

        [Test]
        public void Can_call_DELETE_on_VerbMatch_Services()
        {
            var request = (VerbMatch1)GetRequest(HttpMethods.Delete, "/verbmatch");
            Assert.That(request.Name, Is.Null);

            var request2 = (VerbMatch1)GetRequest(HttpMethods.Delete, "/verbmatch/arg");
            Assert.That(request2.Name, Is.EqualTo("arg"));
        }

        [Test]
        public void Can_call_PUT_on_VerbMatch_Services()
        {
            var request = (VerbMatch2)GetRequest(HttpMethods.Put, "/verbmatch");
            Assert.That(request.Name, Is.Null);

            var request2 = (VerbMatch2)GetRequest(HttpMethods.Put, "/verbmatch/arg");
            Assert.That(request2.Name, Is.EqualTo("arg"));
        }

        [Test]
        public void Can_call_PATCH_on_VerbMatch_Services()
        {
            var request = (VerbMatch2)GetRequest(HttpMethods.Patch, "/verbmatch");
            Assert.That(request.Name, Is.Null);

            var request2 = (VerbMatch2)GetRequest(HttpMethods.Patch, "/verbmatch/arg");
            Assert.That(request2.Name, Is.EqualTo("arg"));
        }

    }
}