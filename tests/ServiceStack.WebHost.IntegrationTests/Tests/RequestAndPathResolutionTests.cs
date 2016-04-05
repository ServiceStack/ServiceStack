using NUnit.Framework;
using ServiceStack.Common.Tests;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class RequestAndPathResolutionTests
        : TestBase
    {
        public RequestAndPathResolutionTests()
            : base(Config.ServiceStackBaseUri, typeof(ReverseService).Assembly)
        {
        }

        protected override void Configure(Funq.Container container) { }

        [SetUp]
        public void OnBeforeTest()
        {
            base.OnBeforeEachTest();
            RegisterConfig();
        }

        private void RegisterConfig()
        {
            HostContext.CatchAllHandlers.Add(new PredefinedRoutesFeature().ProcessRequest);
            HostContext.CatchAllHandlers.Add(new MetadataFeature().ProcessRequest);
        }

        [Test]
        public void Can_process_default_request()
        {
            var request = (EchoRequest)ExecutePath("/Xml/reply/EchoRequest");
            Assert.That(request, Is.Not.Null);
        }

        [Test]
        public void Can_process_default_case_insensitive_request()
        {
            var request = (EchoRequest)ExecutePath("/xml/reply/echorequest");
            Assert.That(request, Is.Not.Null);
        }

        [Test]
        public void Can_process_default_request_with_queryString()
        {
            var request = (EchoRequest)ExecutePath("/Xml/reply/EchoRequest?Id=1&String=Value");
            Assert.That(request, Is.Not.Null);
            Assert.That(request.Id, Is.EqualTo(1));
            Assert.That(request.String, Is.EqualTo("Value"));
        }
    }
}