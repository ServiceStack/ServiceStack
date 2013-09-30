using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    /*
     * Leave commented out - this pollutes and causes many failures in other tests
     */

    //[Explicit("This isn't supported at the moment.")]
    //public class SharedDtoTests
    //{
    //    [Route("/shareddto")]
    //    public class RequestDto : IReturn<ResponseDto> { }
    //    public class ResponseDto
    //    {
    //        public string ServiceName { get; set; }
    //    }

    //    public class Service1 : IService
    //    {
    //        public object Get(RequestDto req)
    //        {
    //            return new ResponseDto { ServiceName = GetType().Name };
    //        }
    //    }

    //    public class Service2 : IService
    //    {
    //        public object Post(RequestDto req)
    //        {
    //            return new ResponseDto { ServiceName = GetType().Name };
    //        }
    //    }

    //    private const string ListeningOn = "http://localhost:8080/";

    //    public class AppHost
    //        : AppHostHttpListenerBase
    //    {

    //        public AppHost()
    //            : base("Shared dto tests", typeof(Service1).Assembly) { }

    //        public override void Configure(Container container)
    //        {
    //        }
    //    }

    //    AppHost appHost;

    //    [TestFixtureSetUp]
    //    public void OnTestFixtureSetUp()
    //    {
    //        appHost = new AppHost();
    //        appHost.Init();
    //        appHost.Start(ListeningOn);
    //    }

    //    [TestFixtureTearDown]
    //    public void OnTestFixtureTearDown()
    //    {
    //        appHost.Dispose();
    //        EndpointHost.ExceptionHandler = null;
    //    }

    //    protected static IRestClient[] RestClients = 
    //    {
    //        new JsonServiceClient(ListeningOn),
    //        new XmlServiceClient(ListeningOn),
    //        new JsvServiceClient(ListeningOn)
    //    };

    //    [Test, TestCaseSource("RestClients")]
    //    public void Can_call_service1(IRestClient client)
    //    {
    //        var response = client.Get(new RequestDto());
    //        Assert.That(response.ServiceName, Is.EqualTo(typeof(Service1).Name));
    //    }

    //    [Test, TestCaseSource("RestClients")]
    //    public void Can_call_service2(IRestClient client)
    //    {
    //        var response = client.Post(new RequestDto());
    //        Assert.That(response.ServiceName, Is.EqualTo(typeof(Service2).Name));
    //    }
    //}
}
