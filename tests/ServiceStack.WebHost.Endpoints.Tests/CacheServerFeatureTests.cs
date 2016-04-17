using System;
using System.Collections.Generic;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class CacheServerFeatureTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(typeof(CacheServerFeatureTests).Name, typeof(CacheEtagServices).Assembly)
            { }

            public override void Configure(Container container) { }
        }

        private readonly ServiceStackHost appHost;
        public CacheServerFeatureTests()
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

        protected JsonServiceClient GetClient()
        {
            return new JsonServiceClient(Config.ListeningOn);
        }

        [Test]
        public void Does_set_Etag_and_Default_MaxAge()
        {
            var client = GetClient();
            client.ResponseFilter = res =>
            {
                Assert.That(res.Headers[HttpHeaders.ETag], Is.EqualTo("etag".Quoted()));
                Assert.That(res.Headers[HttpHeaders.CacheControl], Is.EqualTo("max-age=600"));
            };

            var request = new SetCache { ETag = "etag" };
            var response = client.Get(request);

            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public void Does_not_set_Etag_and_Default_MaxAge_on_POST()
        {
            var client = GetClient();
            client.ResponseFilter = res =>
            {
                Assert.That(res.Headers[HttpHeaders.ETag], Is.Null);
                Assert.That(res.Headers[HttpHeaders.CacheControl], Is.Null);
            };

            var request = new SetCache { ETag = "etag" };
            var response = client.Post(request);

            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public void Does_set_LastModified_and_Default_MaxAge()
        {
            var client = GetClient();
            var request = new SetCache { LastModified = new DateTime(2016, 1, 1, 0, 0, 0) };

            client.ResponseFilter = res =>
            {
                Assert.That(res.Headers[HttpHeaders.LastModified], Is.EqualTo(request.LastModified.Value.ToUniversalTime().ToString("r")));
                Assert.That(res.Headers[HttpHeaders.CacheControl], Is.EqualTo("max-age=600"));
            };

            var response = client.Get(request);

            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public void Does_set_Etag_MaxAge_and_CacheControl()
        {
            var client = GetClient();
            client.ResponseFilter = res =>
            {
                Assert.That(res.Headers[HttpHeaders.Age], Is.EqualTo("864000"));
                Assert.That(res.Headers[HttpHeaders.ETag], Is.EqualTo("etag".Quoted()));
                Assert.That(res.Headers[HttpHeaders.CacheControl], Is.EqualTo("max-age=86400, public, must-revalidate, no-store"));
            };

            var request = new SetCache
            {
                ETag = "etag",
                Age = TimeSpan.FromDays(10),
                MaxAge = TimeSpan.FromDays(1),
                CacheControl = CacheControl.Public | CacheControl.NoStore | CacheControl.MustRevalidate,
            };
            var response = client.Get(request);

            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public void Does_throw_304_when_etag_matches()
        {
            var client = GetClient();
            client.RequestFilter = req =>
                req.Headers[HttpHeaders.IfNoneMatch] = "etag".Quoted();

            client.ResponseFilter = res =>
                Assert.That(res.ContentLength, Is.EqualTo(0));

            try
            {
                var response = client.Get(new SetCache { ETag = "etag" });
                Assert.Fail("Should throw 304 NotModified");
            }
            catch (Exception ex)
            {
                if (!ex.IsNotModified())
                    throw;
            }
        }

        [Test]
        public void Returns_response_when_etag_does_not_match()
        {
            var client = GetClient();
            client.RequestFilter = req =>
                req.Headers[HttpHeaders.IfNoneMatch] = "etag".Quoted();

            client.ResponseFilter = res =>
            {
                Assert.That(res.Headers[HttpHeaders.ETag], Is.EqualTo("etag-alt".Quoted()));
                Assert.That(res.Headers[HttpHeaders.CacheControl], Is.EqualTo("max-age=600"));
            };

            var request = new SetCache { ETag = "etag-alt" };
            var response = client.Get(request);
            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public void Does_throw_304_when_not_ModifiedSince()
        {
            var client = GetClient();
            var request = new SetCache { LastModified = new DateTime(2016, 1, 1, 0, 0, 0) };

            client.RequestFilter = req =>
                req.IfModifiedSince = request.LastModified.Value;

            client.ResponseFilter = res =>
            {
                Assert.That(res.ContentLength, Is.EqualTo(0));
                Assert.That(res.Headers[HttpHeaders.CacheControl], Is.EqualTo("max-age=3600"));
            };

            try
            {
                var response = client.Get(request);
                Assert.Fail("Should throw 304 NotModified");
            }
            catch (Exception ex)
            {
                if (!ex.IsNotModified())
                    throw;
            }
        }

        [Test]
        public void Returns_response_when_ModifiedSince_LastModified()
        {
            var client = GetClient();
            var request = new SetCache { LastModified = new DateTime(2016, 1, 1, 0, 0, 0) };

            client.RequestFilter = req =>
                req.IfModifiedSince = request.LastModified.Value + TimeSpan.FromSeconds(1);

            client.ResponseFilter = res =>
                Assert.That(res.Headers[HttpHeaders.CacheControl], Is.EqualTo("max-age=600"));

            var response = client.Get(request);
            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public void Can_short_circuit_Service_implementation_when_ETag_matches()
        {
            var client = GetClient();
            client.RequestFilter = req =>
                req.Headers[HttpHeaders.IfNoneMatch] = "etag".Quoted();

            client.ResponseFilter = res =>
            {
                Assert.That(res.ContentLength, Is.EqualTo(0));
                Assert.That(res.Headers[HttpHeaders.Age], Is.Null); //short-circuit
            };

            try
            {
                var response = client.Get(new ShortCircuitImpl { ETag = "etag", Age = TimeSpan.FromDays(1) });
                Assert.Fail("Should throw 304 NotModified");
            }
            catch (Exception ex)
            {
                if (!ex.IsNotModified())
                    throw;
            }
        }

        [Test]
        public void Does_bypass_short_circuit_Service_implementation_when_ETag_not_matches()
        {
            var client = GetClient();
            client.RequestFilter = req =>
                req.Headers[HttpHeaders.IfNoneMatch] = "etag".Quoted();

            client.ResponseFilter = res =>
                Assert.That(res.Headers[HttpHeaders.Age], Is.EqualTo("86400"));

            var request = new ShortCircuitImpl
            {
                ETag = "etag-alt",
                Age = TimeSpan.FromDays(1)
            };
            var response = client.Get(request);

            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public void ToOptimizedResult_does_populate_LastModified()
        {
            var client = GetClient();

            client.ResponseFilter = res =>
                Assert.That(DateTime.Parse(res.Headers[HttpHeaders.LastModified]).ToUniversalTime(),
                    Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));

            var request = new CachedRequest { ETag = "etag" };
            var response = client.Get(request);
            Assert.That(response, Is.EqualTo(request));

            response = client.Get(request);
            Assert.That(response, Is.EqualTo(request));
        }

        [Test]
        public void ToOptimizedResult_throws_304_when_not_ModifiedSince()
        {
            var client = GetClient();

            DateTime? lastModified = null;

            client.ResponseFilter = res =>
                lastModified = DateTime.Parse(res.Headers[HttpHeaders.LastModified]);

            var request = new CachedRequest { Age = TimeSpan.FromHours(1) };
            var response = client.Get(request);
            Assert.That(response, Is.EqualTo(request));

            try
            {
                client.RequestFilter = req =>
                    req.IfModifiedSince = lastModified.Value;

                response = client.Get(request);
                Assert.Fail("Should throw 304 NotModified");
            }
            catch (Exception ex)
            {
                if (!ex.IsNotModified())
                    throw;
            }
        }

        [Test]
        public void CachedServiceClient_does_return_cached_ETag_Requests()
        {
            var client = new CachedServiceClient(GetClient());

            var request = new SetCache { ETag = "etag" };

            var response = client.Get(request);
            Assert.That(client.CacheHits, Is.EqualTo(0));
            Assert.That(response, Is.EqualTo(request));

            response = client.Get(request);
            Assert.That(client.CacheHits, Is.EqualTo(1));
            Assert.That(response, Is.EqualTo(request));
        }
    }

    public abstract class CacheRequestBase
    {
        public string ETag { get; set; }
        public TimeSpan? Age { get; set; }
        public TimeSpan? MaxAge { get; set; }
        public DateTime? Expires { get; set; }
        public DateTime? LastModified { get; set; }
        public CacheControl? CacheControl { get; set; }

        public bool Equals(CacheRequestBase other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(ETag, other.ETag)
                && MaxAge.Equals(other.MaxAge)
                && Expires.Equals(other.Expires)
                && LastModified.Equals(other.LastModified)
                && CacheControl == other.CacheControl;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SetCache)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (ETag != null ? ETag.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ MaxAge.GetHashCode();
                hashCode = (hashCode * 397) ^ Expires.GetHashCode();
                hashCode = (hashCode * 397) ^ LastModified.GetHashCode();
                hashCode = (hashCode * 397) ^ CacheControl.GetHashCode();
                return hashCode;
            }
        }
    }

    [Route("/set-cache")]
    public class SetCache : CacheRequestBase, IReturn<SetCache>, IEquatable<SetCache>
    {
        public bool Equals(SetCache other)
        {
            return base.Equals(other);
        }
    }

    public class ShortCircuitImpl : CacheRequestBase, IReturn<ShortCircuitImpl>, IEquatable<ShortCircuitImpl>
    {
        public bool Equals(ShortCircuitImpl other)
        {
            return base.Equals(other);
        }
    }

    public class CachedRequest : CacheRequestBase, IReturn<CachedRequest>, IEquatable<CachedRequest>
    {
        public bool Equals(CachedRequest other)
        {
            return base.Equals(other);
        }
    }

    public class FailsAfterOnce : CacheRequestBase, IReturn<FailsAfterOnce>, IEquatable<FailsAfterOnce>
    {
        internal static int Count = 0;

        public bool Equals(FailsAfterOnce other)
        {
            return base.Equals(other);
        }
    }

    public class CacheEtagServices : Service
    {
        public object Any(SetCache request)
        {
            return new HttpResult(request)
            {
                Age = request.Age,
                ETag = request.ETag,
                MaxAge = request.MaxAge,
                Expires = request.Expires,
                LastModified = request.LastModified,
                CacheControl = request.CacheControl.GetValueOrDefault(CacheControl.None),
            };
        }

        public object Any(ShortCircuitImpl request)
        {
            if (Request.HasValidCache(request.ETag, request.LastModified))
                return HttpResult.NotModified();

            return new HttpResult(request)
            {
                Age = request.Age,
                ETag = request.ETag,
                MaxAge = request.MaxAge,
                Expires = request.Expires,
                LastModified = request.LastModified,
                CacheControl = request.CacheControl.GetValueOrDefault(CacheControl.None),
            };
        }

        public object Any(CachedRequest request)
        {
            return Request.ToOptimizedResultUsingCache(Cache,
                Request.QueryString.ToString(),
                () => request);
        }

        public object Any(FailsAfterOnce request)
        {
            if (FailsAfterOnce.Count++ > 0)
                throw new Exception("Can only be called once");

            return new HttpResult(request)
            {
                Age = request.Age,
                ETag = request.ETag,
                MaxAge = request.MaxAge,
                Expires = request.Expires,
                LastModified = request.LastModified,
                CacheControl = request.CacheControl.GetValueOrDefault(CacheControl.None),
            };
        }
    }
}