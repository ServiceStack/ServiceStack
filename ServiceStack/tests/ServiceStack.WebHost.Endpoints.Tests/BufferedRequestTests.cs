using Funq;
using NUnit.Framework;
using System.Runtime.Serialization;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class BufferedRequestTests
{
    private BufferedRequestAppHost appHost;

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        appHost = new BufferedRequestAppHost();
        appHost.Init();
        appHost.Start(Config.AbsoluteBaseUri);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        appHost.Dispose();
    }

    [Test]
    public void BufferedRequest_allows_rereading_of_Request_InputStream()
    {
        appHost.LastRequestBody = null;
        appHost.UseBufferedStream = true;

        var client = new JsonServiceClient(Config.ServiceStackBaseUri);
        var request = new MyRequest { Data = "RequestData" };
        var response = client.Post(request);

        Assert.That(response.Data, Is.EqualTo(request.Data));
        Assert.That(appHost.LastRequestBody, Is.EqualTo(request.ToJson()));
    }

    [Test]
    public void Cannot_reread_Request_InputStream_without_buffering()
    {
        appHost.LastRequestBody = null;
        appHost.UseBufferedStream = false;

        var client = new JsonServiceClient(Config.ServiceStackBaseUri);
        var request = new MyRequest { Data = "RequestData" };

        try
        {
            var response = client.Post(request);

            Assert.That(appHost.LastRequestBody, Is.EqualTo(request.ToJson()));
            Assert.That(response.Data, Is.Null);
        }
        catch (WebServiceException e)
        {
            //.NET 5
            Assert.That(e.Message, Does.StartWith("Could not deserialize 'application/json' request"));
        }
    }

    [Test]
    public void Cannot_see_RequestBody_in_RequestLogger_without_buffering()
    {
        appHost.LastRequestBody = null;
        appHost.UseBufferedStream = false;

        var client = new JsonServiceClient(Config.ServiceStackBaseUri);
        var request = new MyRequest { Data = "RequestData" };

        try
        {
            var response = client.Post(request);

            Assert.That(appHost.LastRequestBody, Is.EqualTo(request.ToJson()));
            Assert.That(response.Data, Is.Null);

            var requestLogger = appHost.TryResolve<IRequestLogger>();
            var lastEntry = requestLogger.GetLatestLogs(1);
            Assert.That(lastEntry[0].RequestBody, Is.Null);
        }
        catch (WebServiceException e)
        {
            //.NET 5
            Assert.That(e.Message, Does.StartWith("Could not deserialize 'application/json' request"));
        }
    }
}

[TestFixture]
public class BufferedRequestLoggerTests
{
    private BufferedRequestAppHost appHost;
    MyRequest request = new MyRequest { Data = "RequestData" };

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        appHost = new BufferedRequestAppHost { EnableRequestBodyTracking = true };
        appHost.Init();
        appHost.Start(Config.AbsoluteBaseUri);
    }

    [OneTimeTearDown]
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

#if NETFRAMEWORK
        [Test]
        public void Can_see_RequestBody_in_RequestLogger_when_EnableRequestBodyTracking_Soap12()
        {
            const string soap12start = @"<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:a=""http://www.w3.org/2005/08/addressing""><s:Header><a:Action s:mustUnderstand=""1"">MyRequest</a:Action><a:MessageID>urn:uuid:";
            const string soap12end = "<Data>RequestData</Data></MyRequest></s:Body></s:Envelope>";

            var logBody = Run(new Soap12ServiceClient(Config.ServiceStackBaseUri));

            Assert.That(appHost.LastRequestBody, Does.StartWith(soap12start));
            Assert.That(appHost.LastRequestBody, Does.EndWith(soap12end));
            Assert.That(logBody, Does.StartWith(soap12start));
            Assert.That(logBody, Does.EndWith(soap12end));
        }

        [Test]
        public void Can_see_RequestBody_in_RequestLogger_when_EnableRequestBodyTracking_Soap11()
        {
            const string soap11 = @"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/""><s:Body><MyRequest xmlns=""http://schemas.servicestack.net/types"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Data>RequestData</Data></MyRequest></s:Body></s:Envelope>";

            var logBody = Run(new Soap11ServiceClient(Config.ServiceStackBaseUri));
            Assert.That(appHost.LastRequestBody, Is.EqualTo(soap11));
            Assert.That(logBody, Is.EqualTo(soap11));
        }
#endif
        
    string Run(IServiceClient client)
    {
        var requestLogger = appHost.TryResolve<IRequestLogger>();
        appHost.LastRequestBody = null;
        appHost.UseBufferedStream = false;

        var response = client.Send(request);
        //Debug.WriteLine(appHost.LastRequestBody);

        Assert.That(response.Data, Is.EqualTo(request.Data));

        var lastEntry = requestLogger.GetLatestLogs(int.MaxValue);
        return lastEntry[lastEntry.Count - 1].RequestBody;
    }
        
}

public class BufferedRequestAppHost()
    : AppHostHttpListenerBase(nameof(BufferedRequestTests), typeof(MyService).Assembly)
{
    public string LastRequestBody { get; set; }
    public bool UseBufferedStream { get; set; }
    public bool EnableRequestBodyTracking { get; set; }

    public override void Configure(Container container)
    {
#if NETFRAMEWORK
        Plugins.Add(new SoapFormat());
#endif
        PreRequestFilters.Add((httpReq, httpRes) => {
            if (UseBufferedStream)
                httpReq.UseBufferedStream = UseBufferedStream;

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