using System;
using System.Net;
using System.Threading;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/cache/serveronly/{Id}")]
    public class ServerCacheOnly
    {
        internal static int Count = 0;

        public int Id { get; set; }
        public string Value { get; set; }
    }

    [Route("/cache/servershort/{Id}")]
    public class ServerCacheShort
    {
        internal static int Count = 0;

        public int Id { get; set; }
        public string Value { get; set; }
    }

    public class CacheResponseServices : Service
    {
        [CacheResponse(Duration = 10)]
        public object Any(ServerCacheOnly request)
        {
            Interlocked.Increment(ref ServerCacheOnly.Count);
            return request;
        }

        [CacheResponse(Duration = 1)]
        public object Any(ServerCacheShort request)
        {
            Interlocked.Increment(ref ServerCacheShort.Count);
            return request;
        }
    }

    public class CacheResponseTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(typeof(CacheServerFeatureTests).Name, typeof(CacheEtagServices).Assembly) {}

            public override void Configure(Container container)
            {                
            }
        }

        private readonly ServiceStackHost appHost;
        public CacheResponseTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        private void AssertEquals(ServerCacheOnly actual, ServerCacheOnly expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.Value, Is.EqualTo(expected.Value));
        }

        private void AssertEquals(ServerCacheShort actual, ServerCacheShort expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.Value, Is.EqualTo(expected.Value));
        }

        [Test]
        public void Does_cache_duplicate_requests()
        {
            ServerCacheOnly.Count = 0;
            var request = new ServerCacheOnly { Id = 1, Value = "foo" };

            var response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl(responseFilter: res => {
                    Assert.That(res.ContentType, Is.StringStarting(MimeTypes.Json));
                    Assert.That(res.Headers[HttpHeaders.CacheControl], Is.Null);
                })
                .FromJson<ServerCacheOnly>();

            Assert.That(ServerCacheOnly.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl(responseFilter: res => {
                    Assert.That(res.ContentType, Is.StringStarting(MimeTypes.Json));
                    Assert.That(res.Headers[HttpHeaders.CacheControl], Is.Null);
                })
                .FromJson<ServerCacheOnly>();

            Assert.That(ServerCacheOnly.Count, Is.EqualTo(1));
            AssertEquals(response, request);
        }

        [Test]
        public void Does_vary_cache_by_QueryString()
        {
            ServerCacheOnly.Count = 0;
            var request = new ServerCacheOnly { Id = 1, Value = "foo" };

            var response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl()
                .FromJson<ServerCacheOnly>();

            Assert.That(ServerCacheOnly.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            response = Config.ListeningOn.CombineWith(new ServerCacheOnly { Id = 1 }.ToGetUrl())
                .GetJsonFromUrl()
                .FromJson<ServerCacheOnly>();

            Assert.That(ServerCacheOnly.Count, Is.EqualTo(2));
            Assert.That(response.Id, Is.EqualTo(1));
            Assert.That(response.Value, Is.Null);
        }

        [Test]
        public void Does_cache_different_content_types_and_encoding()
        {
            ServerCacheOnly.Count = 0;
            var request = new ServerCacheOnly { Id = 2, Value = "bar" };
            var url = Config.ListeningOn.CombineWith(request.ToGetUrl());

            ServerCacheOnly response;

            //JSON + Deflate
            response = url.GetJsonFromUrl(responseFilter: res => {
                    Assert.That(res.ContentType, Is.StringStarting(MimeTypes.Json));
                })
                .FromJson<ServerCacheOnly>();

            Assert.That(ServerCacheOnly.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            //JSON + No Accept-Encoding
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            webReq.Accept = MimeTypes.Json;
            webReq.AutomaticDecompression = DecompressionMethods.None;
            var webRes = webReq.GetResponse();
            Assert.That(webRes.ContentType, Is.StringStarting(MimeTypes.Json));
            response = webRes.GetResponseStream().ReadFully().FromUtf8Bytes()
                .FromJson<ServerCacheOnly>();
            Assert.That(ServerCacheOnly.Count, Is.EqualTo(1)); //Uses plain json cache from #1
            AssertEquals(response, request);

            //JSON + GZip
            webReq = (HttpWebRequest)WebRequest.Create(url);
            webReq.Accept = MimeTypes.Json;
            webReq.Headers[HttpHeaders.AcceptEncoding] = CompressionTypes.GZip;
            webReq.AutomaticDecompression = DecompressionMethods.GZip;
            webRes = webReq.GetResponse();
            Assert.That(webRes.ContentType, Is.StringStarting(MimeTypes.Json));
            response = webRes.GetResponseStream().ReadFully().FromUtf8Bytes()
                .FromJson<ServerCacheOnly>();
            Assert.That(ServerCacheOnly.Count, Is.EqualTo(2)); //New encoding new cache
            AssertEquals(response, request);

            //XML + Deflate
            response = url.GetXmlFromUrl(responseFilter: res => {
                    Assert.That(res.ContentType, Is.StringStarting(MimeTypes.Xml));
                })
                .FromXml<ServerCacheOnly>();
            Assert.That(ServerCacheOnly.Count, Is.EqualTo(3));
            AssertEquals(response, request);
        }

        [Test]
        public void Cache_does_Expire()
        {
            var request = new ServerCacheShort { Id = 1, Value = "foo" };

            var response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl()
                .FromJson<ServerCacheShort>();
            response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl()
                .FromJson<ServerCacheShort>();

            Assert.That(ServerCacheShort.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            Thread.Sleep(1100);

            response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl()
                .FromJson<ServerCacheShort>();

            Assert.That(ServerCacheShort.Count, Is.EqualTo(2));
            AssertEquals(response, request);
        }
    }
}