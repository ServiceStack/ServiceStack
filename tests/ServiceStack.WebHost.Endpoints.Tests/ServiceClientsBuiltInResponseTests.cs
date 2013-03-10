﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/headers/{Text}")]
    public class Headers : IReturn<HttpWebResponse>
    {
        public string Text { get; set; }
    }

    [Route("/strings/{Text}")]
    public class Strings : IReturn<string>
    {
        public string Text { get; set; }
    }

    [Route("/bytes/{Text}")]
    public class Bytes : IReturn<byte[]>
    {
        public string Text { get; set; }
    }

    [Route("/streams/{Text}")]
    public class Streams : IReturn<Stream>
    {
        public string Text { get; set; }
    }

    public class BuiltInTypesService : ServiceInterface.Service
    {
        public object Any(Headers request)
        {
            base.Response.AddHeader("X-Response", request.Text);
            return null;
        }

        public string Any(Strings request)
        {
            return "Hello, " + (request.Text ?? "World!");
        }

        public byte[] Any(Bytes request)
        {
            return new Guid(request.Text).ToByteArray();
        }

        public byte[] Any(Streams request)
        {
            return new Guid(request.Text).ToByteArray();
        }        
    }
    
    public class BuiltInTypesAppHost : AppHostHttpListenerBase
    {
        public BuiltInTypesAppHost() : base(typeof(BuiltInTypesAppHost).Name, typeof(BuiltInTypesService).Assembly) { }

        public string LastRequestBody { get; set; }
        public bool UseBufferredStream { get; set; }
        public bool EnableRequestBodyTracking { get; set; }

        public override void Configure(Container container) {}
    }

    public class ServiceClientsBuiltInResponseTests
    {
        private BufferedRequestAppHost appHost;

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
            appHost = null;
        }

        protected static IRestClient[] RestClients = 
		{
			new JsonServiceClient(Config.AbsoluteBaseUri),
			new XmlServiceClient(Config.AbsoluteBaseUri),
			new JsvServiceClient(Config.AbsoluteBaseUri),
		};

        protected static IServiceClient[] ServiceClients = 
            RestClients.OfType<IServiceClient>().ToArray();

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Headers_response(IRestClient client)
        {
            HttpWebResponse response = client.Get(new Headers { Text = "Test" });
            Assert.That(response.Headers["X-Response"], Is.EqualTo("Test"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Headers_response_Async(IServiceClient client)
        {
            //Note: HttpWebResponse is returned before any response is read, so it's ideal point for streaming in app code

            HttpWebResponse response = null;
            client.GetAsync(new Headers { Text = "Test" }, r => response = r,
                (r, ex) => Assert.Fail(ex.Message));

            Thread.Sleep(2000);

            Assert.That(response.Headers["X-Response"], Is.EqualTo("Test"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Strings_response(IRestClient client)
        {
            string response = client.Get(new Strings { Text = "Test" });
            Assert.That(response, Is.EqualTo("Hello, Test"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Strings_response_Async(IServiceClient client)
        {
            string response = null;
            client.GetAsync(new Strings { Text = "Test" }, r => response = r,
                (r, ex) => Assert.Fail(ex.Message));

            Thread.Sleep(2000);

            Assert.That(response, Is.EqualTo("Hello, Test"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Bytes_response(IRestClient client)
        {
            var guid = Guid.NewGuid();
            byte[] response = client.Get(new Bytes { Text = guid.ToString() });
            Assert.That(new Guid(response), Is.EqualTo(guid));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Bytes_response_Async(IServiceClient client)
        {
            byte[] bytes = null;
            var guid = Guid.NewGuid();
            client.GetAsync(new Bytes { Text = guid.ToString() }, r => bytes = r,
                (r, ex) => Assert.Fail(ex.Message));

            Thread.Sleep(2000);

            Assert.That(new Guid(bytes), Is.EqualTo(guid));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Streams_response(IRestClient client)
        {
            var guid = Guid.NewGuid();
            Stream response = client.Get(new Streams { Text = guid.ToString() });
            using (response)
            {
                var bytes = response.ReadFully();
                Assert.That(new Guid(bytes), Is.EqualTo(guid));
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Streams_response_Async(IServiceClient client)
        {
            //Note: The populated MemoryStream which bufferred the response is returned (i.e. after the response is read async-ly)

            byte[] bytes = null;
            var guid = Guid.NewGuid();
            client.GetAsync(new Streams { Text = guid.ToString() }, stream => {
                using (stream)
                {
                    bytes = stream.ReadFully();
                }
            }, (stream, ex) => Assert.Fail(ex.Message));

            Thread.Sleep(2000);

            Assert.That(new Guid(bytes), Is.EqualTo(guid));
        }
         
    }
}