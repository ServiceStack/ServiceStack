using System;
using System.Runtime.Serialization;
using Funq;
using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/interceptinputitream", "POST")]
    [DataContract]
    public class InterceptRequestRequest
    {
        [DataMember]
        public string Data { get; set; }
    }

    public class InterceptRequestResponse {}

    public class InterceptRequestService : ServiceInterface.Service, IAny<InterceptRequestRequest>
    {
        public object Any(InterceptRequestRequest request)
        {
            return new InterceptRequestResponse();
        }
    }

    public class InterceptRequestTests
    {
        private const string ListeningOn = "http://localhost:82/";

        public class InterceptRequestAppHostHttpListener
            : AppHostHttpListenerBase
        {

            public InterceptRequestAppHostHttpListener()
                : base("Intercept InputStream tests", typeof(HelloService).Assembly) { }

            public override void Configure(Container container)
            {
                InterceptRequestHandler = request =>
                {
                    if (ThrowException)
                    {
                        throw new InvalidOperationException();
                    }

                    RawBody = request.GetRawBody();
                };
            }

            public string RawBody { get; private set; }
            public bool ThrowException { get; set; }
        }

        InterceptRequestAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new InterceptRequestAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }
        
        private static readonly IServiceClient[] ServiceClients = 
		{
			new JsonServiceClient(ListeningOn),
			new XmlServiceClient(ListeningOn),
			new JsvServiceClient(ListeningOn),
            new Soap11ServiceClient(ListeningOn),
            new Soap12ServiceClient(ListeningOn)
		};

        [Test, TestCaseSource("ServiceClients")]
        public void Can_Intercept_Request(IServiceClient client)
        {
            const string data = "SDBF4U%^TRMJY%^IgfnhjSD^*o*(PdbfnSEVF#$%TK";
            client.Send<InterceptRequestResponse>(new InterceptRequestRequest { Data = data });

            Console.WriteLine(appHost.RawBody);

            Assert.That(appHost.RawBody, Is.Not.Null.Or.Empty);
            Assert.That(appHost.RawBody, Contains.Substring(data));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Ignores_Thrown_Exception_In_Intercept_Callback(IServiceClient client)
        {
            var request = new InterceptRequestRequest { Data = "abcdefg" };
            appHost.ThrowException = true;

            client.Send<InterceptRequestResponse>(request);
            Assert.Pass();
        }
    }
}