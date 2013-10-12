using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using System.Runtime.Serialization;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class BufferedRequestTests
    {
        private BufferedRequestAppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BufferedRequestAppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void BufferredRequest_allows_rereading_of_Request_InputStream()
        {
            appHost.LastRequestBody = null;
            appHost.UseBufferredStream = true;

            var client = new JsonServiceClient(Config.ServiceStackBaseUri);
            var request = new MyRequest { Data = "RequestData" };
            var response = client.Post(request);

            Assert.That(response.Data, Is.EqualTo(request.Data));
            Assert.That(appHost.LastRequestBody, Is.EqualTo(request.ToJson()));
        }

        [Test]
        public void Cannot_reread_Request_InputStream_without_bufferring()
        {
            appHost.LastRequestBody = null;
            appHost.UseBufferredStream = false;

            var client = new JsonServiceClient(Config.ServiceStackBaseUri);
            var request = new MyRequest { Data = "RequestData" };

            var response = client.Post(request);

            Assert.That(appHost.LastRequestBody, Is.EqualTo(request.ToJson()));
            Assert.That(response.Data, Is.Null);
        }

        [Test]
        public void Cannot_see_RequestBody_in_RequestLogger_without_bufferring()
        {
            appHost.LastRequestBody = null;
            appHost.UseBufferredStream = false;

            var client = new JsonServiceClient(Config.ServiceStackBaseUri);
            var request = new MyRequest { Data = "RequestData" };

            var response = client.Post(request);

            Assert.That(appHost.LastRequestBody, Is.EqualTo(request.ToJson()));
            Assert.That(response.Data, Is.Null);

            var requestLogger = appHost.TryResolve<IRequestLogger>();
            var lastEntry = requestLogger.GetLatestLogs(1);
            Assert.That(lastEntry[0].RequestBody, Is.Null);
        }
    }

    [TestFixture]
    public class BufferedRequestLoggerTests
    {
        private BufferedRequestAppHost appHost;
        MyRequest request = new MyRequest { Data = "RequestData" };

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BufferedRequestAppHost { EnableRequestBodyTracking = true };
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_see_RequestBody_in_RequestLogger_when_EnableRequestBodyTracking()
        {
            var logBody = Run(new JsonServiceClient(Config.ServiceStackBaseUri));
            Assert.That(appHost.LastRequestBody, Is.EqualTo(request.ToJson()));
            Assert.That(logBody, Is.EqualTo(request.ToJson()));
        }

        [Test]
        public void Can_see_RequestBody_in_RequestLogger_when_EnableRequestBodyTracking_Soap12()
        {
            const string soap12start = @"<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:a=""http://www.w3.org/2005/08/addressing""><s:Header><a:Action s:mustUnderstand=""1"">MyRequest</a:Action><a:MessageID>urn:uuid:";
            const string soap12end = "<Data>RequestData</Data></MyRequest></s:Body></s:Envelope>";

            var logBody = Run(new Soap12ServiceClient(Config.ServiceStackBaseUri));

            Assert.That(appHost.LastRequestBody, Is.StringStarting(soap12start));
            Assert.That(appHost.LastRequestBody, Is.StringEnding(soap12end));
            Assert.That(logBody, Is.StringStarting(soap12start));
            Assert.That(logBody, Is.StringEnding(soap12end));
        }


        [Test]
        public void Can_see_RequestBody_in_RequestLogger_when_EnableRequestBodyTracking_Soap11()
        {
            const string soap11 = @"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/""><s:Body><MyRequest xmlns=""http://schemas.servicestack.net/types"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Data>RequestData</Data></MyRequest></s:Body></s:Envelope>";

            var logBody = Run(new Soap11ServiceClient(Config.ServiceStackBaseUri));
            Assert.That(appHost.LastRequestBody, Is.EqualTo(soap11));
            Assert.That(logBody, Is.EqualTo(soap11));
        }

        
        string Run(IServiceClient client)
        {
            var requestLogger = appHost.TryResolve<IRequestLogger>();
            appHost.LastRequestBody = null;
            appHost.UseBufferredStream = false;

            var response = client.Send(request);
            //Debug.WriteLine(appHost.LastRequestBody);

            Assert.That(response.Data, Is.EqualTo(request.Data));

            var lastEntry = requestLogger.GetLatestLogs(int.MaxValue);
            return lastEntry[lastEntry.Count - 1].RequestBody;
        }
        
    }

    public class BufferedRequestAppHost : AppHostHttpListenerBase
    {
        public BufferedRequestAppHost() : base(typeof(BufferedRequestTests).Name, typeof(MyService).Assembly) { }

        public string LastRequestBody { get; set; }
        public bool UseBufferredStream { get; set; }
        public bool EnableRequestBodyTracking { get; set; }

        public override void Configure(Container container)
        {
            PreRequestFilters.Add((httpReq, httpRes) => {
                if (UseBufferredStream)
                    httpReq.UseBufferedStream = UseBufferredStream;

                LastRequestBody = null;
                LastRequestBody = httpReq.GetRawBody();
            });

            Plugins.Add(new RequestLogsFeature { EnableRequestBodyTracking = EnableRequestBodyTracking });
        }
    }

    [DataContract]
    public class MyRequest : IReturn<MyRequest>
    {
        [DataMember]
        public string Data { get; set; }
    }

    public class MyService : IService
    {
        public object Any(MyRequest request)
        {
            return request;
        }
    }
}
