using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests;

[Route("/onewayrequest", "DELETE")]
public class DeleteOneWayRequest : IReturnVoid
{
    public string Prefix { get; set; }
}

[Route("/onewayrequest", "POST")]
[Route("/onewayrequest", "PUT")]
[DataContract]
public class PostOneWayRequest : IReturnVoid
{
    [DataMember]
    public string Prefix { get; set; }

    [DataMember(Name = "some-title")]
    public string Title { get; set; }
}    

public class OneWayService : Service
{
    public static string LastResult { get; set; }
    public void Delete(DeleteOneWayRequest oneWayRequest)
    {
        LastResult = oneWayRequest.Prefix + " " + Request.Verb;
    }

    public void Post(PostOneWayRequest oneWayRequest)
    {
        LastResult = oneWayRequest.Prefix + " " + Request.Verb + oneWayRequest.Title;
    }

    public void Put(PostOneWayRequest oneWayRequest)
    {
        Post(oneWayRequest);
    }
}

[TestFixture]
public class OneWayServiceTests
{
    private const string ListeningOn = "http://localhost:8023/";
    private const string ServiceClientBaseUri = "http://localhost:8023/";

    public class OneWayServiceAppHostHttpListener
        : AppHostHttpListenerBase
    {

        public OneWayServiceAppHostHttpListener()
            : base("", typeof(OneWayService).Assembly) { }

        public override void Configure(Funq.Container container)
        {
        }
    }

    OneWayServiceAppHostHttpListener appHost;
    private IRestClient client;

    [OneTimeSetUp]
    public void OnTestFixtureSetUp()
    {
        appHost = new OneWayServiceAppHostHttpListener();
        appHost.Init();
        appHost.Start(ListeningOn);

        client = new JsonServiceClient(ServiceClientBaseUri);
        OneWayService.LastResult = null;
    }

    [OneTimeTearDown]
    public void OnTestFixtureTearDown()
    {
        appHost.Dispose();
    }


    [Test]
    public void Delete()
    {
        client.Delete(new DeleteOneWayRequest() { Prefix = "Delete" });
        Assert.That(OneWayService.LastResult, Is.EqualTo("Delete DELETE"));

    }

    [Test]
    public void Send()
    {
        client.Post(new PostOneWayRequest() { Prefix = "Post" });
        Assert.That(OneWayService.LastResult, Is.EqualTo("Post POST"));
    }

    [Test]
    public void Should_Respect_DataMember_Name()
    {
        GetResponse(ServiceClientBaseUri + "onewayrequest", "{\"some-title\": \"right\", \"Title\": \"wrong\"}");
        Assert.That(OneWayService.LastResult, Is.EqualTo(" PUTright"));
    }

    public static string GetResponse(String url, string json)
    {
#pragma warning disable CS0618, SYSLIB0014
        var webRequest = WebRequest.CreateHttp(url);
#pragma warning restore CS0618, SYSLIB0014
        webRequest.Method = "PUT";
        var formDataBytes = Encoding.UTF8.GetBytes(json);
        webRequest.SetContentLength(formDataBytes.Length);
        webRequest.ContentType = "application/json";
        PclExport.Instance.GetRequestStream(webRequest).Write(formDataBytes, 0, formDataBytes.Length);
        var webResponse = webRequest.GetResponse();
        return webResponse.GetResponseStream().ReadToEnd();
    }
}