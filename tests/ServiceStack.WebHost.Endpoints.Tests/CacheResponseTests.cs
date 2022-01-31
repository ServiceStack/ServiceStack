using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/cache/serveronly/{Id}")]
    public class ServerCacheOnly : ICacheDto
    {
        internal static int Count = 0;

        public int Id { get; set; }
        public string Value { get; set; }
    }

    [Route("/cache/serveronlyasync/{Id}")]
    public class ServerCacheOnlyAsync : ICacheDto
    {
        internal static int Count = 0;

        public int Id { get; set; }
        public string Value { get; set; }
    }

    [Route("/cache/servershort/{Id}")]
    public class ServerCacheShort : ICacheDto
    {
        internal static int Count = 0;

        public int Id { get; set; }
        public string Value { get; set; }
    }

    [Route("/cache/serversuser/{Id}")]
    public class ServerCacheUser : ICacheDto
    {
        internal static int Count = 0;

        public int Id { get; set; }
        public string Value { get; set; }
    }

    [Route("/cache/serversroles/{Id}")]
    public class ServerCacheRoles : ICacheDto
    {
        internal static int Count = 0;

        public int Id { get; set; }
        public string Value { get; set; }
    }

    [Route("/cache/clientmaxage/{Id}")]
    public class ClientCacheMaxAge : IReturn<ClientCacheMaxAge>, ICacheDto
    {
        internal static int Count = 0;

        public int Id { get; set; }
        public string Value { get; set; }
    }

    [Route("/cache/clientmustrevalidate/{Id}")]
    public class ClientCacheMustRevalidate : IReturn<ClientCacheMustRevalidate>, ICacheDto
    {
        internal static int Count = 0;

        public int Id { get; set; }
        public string Value { get; set; }
    }

    [Route("/cache/serverscustomkey/{Id}")]
    public class ServerCustomCacheKey : ICacheDto
    {
        internal static int Count = 0;

        public int Id { get; set; }
        public string Value { get; set; }
    }

    [Route("/cache/alwaysthrows")]
    public class CacheAlwaysThrows : IReturn<CacheAlwaysThrows>
    {
        public string Message { get; set; }
    }

    public interface ICacheDto
    {
        int Id { get; set; }
        string Value { get; set; }
    }

    public class HelloCache : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    [Route("/cache/custom-json/{Id}")]
    public class CacheCustomJson : ICacheDto
    {
        internal static int Count = 0;

        public int Id { get; set; }
        public string Value { get; set; }
    }

    [Route("/cache/stream-result/{Id}")]
    public class CacheStream : ICacheDto
    {
        internal static int Count = 0;

        public int Id { get; set; }
        public string Value { get; set; }
    }

    public class CacheResponseServices : Service
    {
        [CacheResponse(Duration = 5000)]
        public object Any(HelloCache request)
        {
            return new HelloResponse { Result = $"Hello, {request.Name}!"};
        }

        [CacheResponse(Duration = 10)]
        public object Any(ServerCacheOnly request)
        {
            Interlocked.Increment(ref ServerCacheOnly.Count);
            return request;
        }

        [CacheResponse(Duration = 10)]
        public async Task<object> Any(ServerCacheOnlyAsync request)
        {
            await Task.Yield();
            Interlocked.Increment(ref ServerCacheOnlyAsync.Count);
            return request;
        }

        [CacheResponse(Duration = 1)]
        public object Any(ServerCacheShort request)
        {
            Interlocked.Increment(ref ServerCacheShort.Count);
            return request;
        }

        [CacheResponse(Duration = 10, VaryByUser = true)]
        public object Any(ServerCacheUser request)
        {
            Interlocked.Increment(ref ServerCacheUser.Count);
            return request;
        }

        [CacheResponse(Duration = 10, VaryByRoles = new[]{ "RoleA", "RoleB" })]
        public object Any(ServerCacheRoles request)
        {
            Interlocked.Increment(ref ServerCacheRoles.Count);
            return request;
        }

        [CacheResponse(Duration = 10, MaxAge = 10)]
        public object Any(ClientCacheMaxAge request)
        {
            Interlocked.Increment(ref ClientCacheMaxAge.Count);
            return request;
        }

        [CacheResponse(Duration = 10, MaxAge = 0, CacheControl = CacheControl.MustRevalidate)]
        public object Any(ClientCacheMustRevalidate request)
        {
            Interlocked.Increment(ref ClientCacheMustRevalidate.Count);
            return request;
        }

        [CacheResponse(Duration = 10)]
        public async Task<object> Any(ServerCustomCacheKey request)
        {
            if (Request.GetItem(Keywords.CacheInfo) is CacheInfo cacheInfo)
            {
                cacheInfo.KeyBase += "::flag=" + (ServerCustomCacheKey.Count % 2 == 0);
                if (await Request.HandleValidCache(cacheInfo))
                    return null;
            }

            Interlocked.Increment(ref ServerCustomCacheKey.Count);
            return request;
        }

        [CacheResponse(Duration = 5000)]
        public object Any(CacheAlwaysThrows request)
        {
            throw new Exception(request.Message);
        }

        [CacheResponse(Duration = 10)]
        public object Any(CacheCustomJson request)
        {
            Interlocked.Increment(ref CacheCustomJson.Count);
            return new HttpResult(request)
            {
                ResultScope = () => JsConfig.With(new Text.Config
                {
                    TextCase = TextCase.CamelCase,
                    IncludeNullValues = true,
                })
            };
        }

        [AddHeader(ContentType = MimeTypes.Jsv)]
        [CacheResponse(Duration = 5000)]
        public object Any(CacheStream request)
        {
            Interlocked.Increment(ref CacheStream.Count);

            var jsv = request.ToJsv();
            var bytes = jsv.ToUtf8Bytes();
            var ms = bytes.InMemoryStream();
            return ms;
        }
    }

    [TestFixture]
    public class CacheResponseTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(CacheServerFeatureTests), typeof (CacheEtagServices).Assembly) {}

            public override void Configure(Container container)
            {
                PreRequestFilters.Add((req, res) =>
                {
                    var roleHeader = req.GetHeader("X-role");
                    if (roleHeader == null)
                        return;

                    req.Items[Keywords.Session] = new AuthUserSession
                    {
                        UserAuthId = "1",
                        UserAuthName = "test",
                        Roles = new List<string> { roleHeader }
                    };
                });

                ServiceExceptionHandlers.Add((req, dto, ex) =>
                {
                    return DtoUtils.CreateErrorResponse(dto, ex);
                });
            }
        }

        private readonly ServiceStackHost appHost;
        public CacheResponseTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        private void AssertEquals(ICacheDto actual, ICacheDto expected)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.Value, Is.EqualTo(expected.Value));
        }

        private static IJsonServiceClient CreateClient()
        {
#if NET6_0_OR_GREATER
            return new JsonApiClient(Config.ListeningOn);
#else
            return new JsonServiceClient(Config.ListeningOn);
#endif
        }

        [Test]
        public void Does_cache_duplicate_requests()
        {
            ServerCacheOnly.Count = 0;
            var request = new ServerCacheOnly { Id = 1, Value = "foo" };

            var response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl(responseFilter: res => {
                    Assert.That(res.GetHeader(HttpHeaders.ContentType), Does.StartWith(MimeTypes.Json));
                    Assert.That(res.GetHeader(HttpHeaders.CacheControl), Is.Null);
                })
                .FromJson<ServerCacheOnly>();

            Assert.That(ServerCacheOnly.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl(responseFilter: res =>
                {
                    Assert.That(res.GetHeader(HttpHeaders.ContentType), Does.StartWith(MimeTypes.Json));
                    Assert.That(res.GetHeader(HttpHeaders.CacheControl), Is.Null);
                })
                .FromJson<ServerCacheOnly>();

            Assert.That(ServerCacheOnly.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            var client = CreateClient();
            response = client.Get<ServerCacheOnly>(request);
            Assert.That(ServerCacheOnly.Count, Is.EqualTo(1));
            AssertEquals(response, request);
        }

        [Test]
        public async Task Does_cache_duplicate_requests_async()
        {
            ServerCacheOnlyAsync.Count = 0;
            var request = new ServerCacheOnlyAsync { Id = 1, Value = "foo" };

            var response = (await Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrlAsync(responseFilter: res =>
                {
                    Assert.That(res.GetHeader(HttpHeaders.ContentType), Does.StartWith(MimeTypes.Json));
                    Assert.That(res.GetHeader(HttpHeaders.CacheControl), Is.Null);
                }))
                .FromJson<ServerCacheOnlyAsync>();

            Assert.That(ServerCacheOnlyAsync.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            response = (await Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrlAsync(responseFilter: res =>
                {
                    Assert.That(res.GetHeader(HttpHeaders.ContentType), Does.StartWith(MimeTypes.Json));
                    Assert.That(res.GetHeader(HttpHeaders.CacheControl), Is.Null);
                }))
                .FromJson<ServerCacheOnlyAsync>();

            Assert.That(ServerCacheOnlyAsync.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            var client = CreateClient();
            response = await client.GetAsync<ServerCacheOnlyAsync>(request);
            Assert.That(ServerCacheOnlyAsync.Count, Is.EqualTo(1));
            AssertEquals(response, request);
        }

        [Test]
        public void Does_vary_cache_by_QueryString()
        {
            ServerCacheOnly.Count = 0;
            var request = new ServerCacheOnly { Id = 2, Value = "foo" };

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
        public void Does_vary_cache_by_UserSession()
        {
            ServerCacheOnly.Count = 0;
            var request = new ServerCacheUser { Id = 3, Value = "foo" };

            var response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl(requestFilter: req => req.AddHeader("X-ss-id", "1"))
                .FromJson<ServerCacheUser>();

            Assert.That(ServerCacheUser.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl(requestFilter: req => req.AddHeader("X-ss-id", "1"))
                .FromJson<ServerCacheUser>();

            Assert.That(ServerCacheUser.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl(requestFilter: req => req.AddHeader("X-ss-id", "2"))
                .FromJson<ServerCacheUser>();

            Assert.That(ServerCacheUser.Count, Is.EqualTo(2));
            AssertEquals(response, request);
        }

        [Test]
        public void Does_vary_cache_by_Role()
        {
            ServerCacheRoles.Count = 0;
            var request = new ServerCacheRoles { Id = 3, Value = "foo" };

            var response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl(requestFilter: req => req.AddHeader("X-role", "RoleA"))
                .FromJson<ServerCacheRoles>();

            Assert.That(ServerCacheRoles.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl(requestFilter: req => req.AddHeader("X-role", "RoleA"))
                .FromJson<ServerCacheRoles>();

            Assert.That(ServerCacheRoles.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl(requestFilter: req => req.AddHeader("X-role", "RoleB"))
                .FromJson<ServerCacheRoles>();

            Assert.That(ServerCacheRoles.Count, Is.EqualTo(2));
            AssertEquals(response, request);
        }

        [Test]
        public void Does_cache_different_content_types_and_encoding()
        {
            ServerCacheOnly.Count = 0;
            var request = new ServerCacheOnly { Id = 4, Value = "bar" };
            var url = Config.ListeningOn.CombineWith(request.ToGetUrl());

            ServerCacheOnly response;

            //JSON + Deflate
            response = url.GetJsonFromUrl(responseFilter: res =>
            {
                Assert.That(res.GetHeader(HttpHeaders.ContentType), Does.StartWith(MimeTypes.Json));
            })
                .FromJson<ServerCacheOnly>();

            Assert.That(ServerCacheOnly.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            //JSON + No Accept-Encoding
            var webReq = WebRequest.CreateHttp(url);
            webReq.Accept = MimeTypes.Json;
#if !NETCORE            
            webReq.AutomaticDecompression = DecompressionMethods.None;
#endif
            var webRes = webReq.GetResponse();
            Assert.That(webRes.ContentType, Does.StartWith(MimeTypes.Json));
            response = webRes.GetResponseStream().ReadToEnd()
                .FromJson<ServerCacheOnly>();
            Assert.That(ServerCacheOnly.Count, Is.EqualTo(1)); //Uses plain json cache from #1
            AssertEquals(response, request);

            //JSON + GZip
            webReq = WebRequest.CreateHttp(url);
            webReq.Accept = MimeTypes.Json;
            webReq.Headers[HttpHeaders.AcceptEncoding] = CompressionTypes.GZip;
#if !NETCORE            
            webReq.AutomaticDecompression = DecompressionMethods.GZip;
#endif
            webRes = webReq.GetResponse();
            Assert.That(webRes.ContentType, Does.StartWith(MimeTypes.Json));
            var responseGzip = webRes.GetResponseStream().ReadFully();
#if !NETCORE
            response = responseGzip.FromUtf8Bytes().FromJson<ServerCacheOnly>();
#else
            response = responseGzip.Decompress("gzip").FromJson<ServerCacheOnly>();
#endif
            Assert.That(ServerCacheOnly.Count, Is.EqualTo(2)); //New encoding new cache
            AssertEquals(response, request);

            //XML + Deflate
            response = url.GetXmlFromUrl(responseFilter: res => {
                    Assert.That(res.GetHeader(HttpHeaders.ContentType), Does.StartWith(MimeTypes.Xml));
                })
                .FromXml<ServerCacheOnly>();
            Assert.That(ServerCacheOnly.Count, Is.EqualTo(3));
            AssertEquals(response, request);

            //HTML + Deflate
            var html = url.GetStringFromUrl(requestFilter:req => req.With(c => c.Accept = MimeTypes.Html));
            Assert.That(ServerCacheOnly.Count, Is.EqualTo(4));
            Assert.That(html, Does.StartWith("<!doctype html>"));
            html = url.GetStringFromUrl(requestFilter:req => req.With(c => c.Accept = MimeTypes.Html));
            Assert.That(ServerCacheOnly.Count, Is.EqualTo(4));
            Assert.That(html, Does.StartWith("<!doctype html>"));
        }

        [Test]
        public void Can_execute_with_CompressionDisabled()
        {
            var client = new JsvServiceClient(Config.ListeningOn)
            {
                DisableAutoCompression = true,
            };

            var result = client.Get<ServerCacheOnly>(new ServerCacheOnly { Value = "Hello" });
            Assert.That(result.Value, Is.EqualTo("Hello"));

            var response = client.Get(new HelloCache { Name = "World" });
            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }

        [Test]
        public void Cache_does_Expire()
        {
            var request = new ServerCacheShort { Id = 5, Value = "foo" };

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

        [Test]
        public void Cached_client_does_return_local_cache_when_MaxAge()
        {
            ClientCacheMaxAge.Count = 0;
            var request = new ClientCacheMaxAge { Id = 6, Value = "foo" };
            var client = new CachedServiceClient(new JsonServiceClient(Config.ListeningOn));

            ClientCacheMaxAge response;

            response = client.Get(request);
            Assert.That(ClientCacheMaxAge.Count, Is.EqualTo(1));
            Assert.That(client.CacheHits, Is.EqualTo(0));
            AssertEquals(response, request);

            response = client.Get(request);
            Assert.That(ClientCacheMaxAge.Count, Is.EqualTo(1));
            Assert.That(client.CacheHits, Is.EqualTo(1));
            AssertEquals(response, request);
        }

        [Test]
        public void Cached_client_does_return_NotModified_when_MustRevalidate()
        {
            ClientCacheMaxAge.Count = 0;
            var request = new ClientCacheMustRevalidate { Id = 7, Value = "foo" };
            var client = new CachedServiceClient(new JsonServiceClient(Config.ListeningOn));

            ClientCacheMustRevalidate response;

            response = client.Get(request);
            Assert.That(ClientCacheMustRevalidate.Count, Is.EqualTo(1));
            Assert.That(client.NotModifiedHits, Is.EqualTo(0));
            AssertEquals(response, request);

            response = client.Get(request);
            Assert.That(ClientCacheMustRevalidate.Count, Is.EqualTo(1));
            Assert.That(client.NotModifiedHits, Is.EqualTo(1));
            AssertEquals(response, request);
        }

        [Test]
        public void Does_cache_by_custom_CacheKey()
        {
            ServerCustomCacheKey.Count = 0;
            var request = new ServerCustomCacheKey { Id = 8, Value = "foo" };

            var response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl()
                .FromJson<ServerCustomCacheKey>();

            Assert.That(ServerCustomCacheKey.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl()
                .FromJson<ServerCustomCacheKey>();

            Assert.That(ServerCustomCacheKey.Count, Is.EqualTo(2));
            AssertEquals(response, request);

            response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetJsonFromUrl()
                .FromJson<ServerCustomCacheKey>();

            Assert.That(ServerCustomCacheKey.Count, Is.EqualTo(2));
            AssertEquals(response, request);
        }

        [Test]
        public void Does_not_cache_Error_Responses()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            try
            {
                var response = client.Get(new CacheAlwaysThrows { Message = "foo" });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorMessage, Is.EqualTo("foo"));
            }

            try
            {
                var response = client.Get(new CacheAlwaysThrows { Message = "bar" });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorMessage, Is.EqualTo("bar"));
            }
        }

        [Test]
        public void Cache_does_use_custom_serialization()
        {
            var json = Config.ListeningOn.CombineWith("/cache/custom-json/1")
                .GetJsonFromUrl();
            
            Assert.That(json, Is.EqualTo("{\"id\":1,\"value\":null}"));
        }

        [Test]
        public void Does_cache_MemoryStream_HttpResult_Responses_preserving_ContentType()
        {
            CacheStream.Count = 0;
            var request = new CacheStream { Id = 1, Value = "foo" };

            var response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetStringFromUrl(responseFilter: res =>
                {
                    Assert.That(res.GetHeader(HttpHeaders.ContentType), Does.StartWith(MimeTypes.Jsv));
                    Assert.That(res.GetHeader(HttpHeaders.CacheControl), Is.Null);
                })
                .FromJsv<CacheStream>();

            Assert.That(CacheStream.Count, Is.EqualTo(1));
            AssertEquals(response, request);

            response = Config.ListeningOn.CombineWith(request.ToGetUrl())
                .GetStringFromUrl(responseFilter: res =>
                {
                    Assert.That(res.GetHeader(HttpHeaders.ContentType), Does.StartWith(MimeTypes.Jsv));
                    Assert.That(res.GetHeader(HttpHeaders.CacheControl), Is.Null);
                })
                .FromJsv<CacheStream>();

            Assert.That(CacheStream.Count, Is.EqualTo(1));
            AssertEquals(response, request);

#if NNETFX            
            var client = new JsvServiceClient(Config.ListeningOn);
            response = client.Get<CacheStream>(request);
            Assert.That(CacheStream.Count, Is.EqualTo(1));
            AssertEquals(response, request);
#endif
        }

    }
}