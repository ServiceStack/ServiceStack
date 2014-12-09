using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ReplyAllAppHost : AppSelfHostBase
    {
        public ReplyAllAppHost()
            : base(typeof(ReplyAllTests).Name, typeof(ReplyAllService).Assembly) { }

        public override void Configure(Container container)
        {
            GlobalRequestFilters.Add((rew, res, dto) =>
                ReplyAllRequestAttribute.AssertSingleDto(dto));

            GlobalResponseFilters.Add((rew, res, dto) =>
                ReplyAllResponseAttribute.AssertSingleDto(dto));

            container.Register<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            container.Register<IRedisClientsManager>(c =>
                new RedisManagerPool());
        }
    }

    public class ReplyAllRequestAttribute : RequestFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            AssertSingleDto(requestDto);
        }

        public static void AssertSingleDto(object dto)
        {
            if (!(dto is HelloAll || dto is HelloGet || dto is HelloAllCustom || dto is HelloAllTransaction || dto is Request))
                throw new Exception("Invalid " + dto.GetType().Name);
        }
    }

    public class ReplyAllResponseAttribute : ResponseFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object responseDto)
        {
            AssertSingleDto(responseDto);
        }

        public static void AssertSingleDto(object dto)
        {
            if (!(dto == null || dto is HelloAllResponse || dto is HelloAllCustomResponse
               || dto is HelloAllTransactionResponse || dto is IHttpResult))
                throw new Exception("Invalid " + dto.GetType().Name);
        }
    }

    public class ReplyAllArrayRequestAttribute : RequestFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (requestDto.GetType() != typeof(HelloAllCustom[]))
                throw new Exception("Invalid " + requestDto.GetType().Name);
        }
    }

    public class ReplyAllArrayResponseAttribute : ResponseFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object responseDto)
        {
            //still based on Response of Service
            if (responseDto.GetType() != typeof(List<HelloAllCustomResponse>))
                throw new Exception("Invalid " + responseDto.GetType().Name);
        }
    }

    public class HelloAll : IReturn<HelloAllResponse>
    {
        public string Name { get; set; }
    }

    public class HelloGet : IReturn<HelloAllResponse>
    {
        public string Name { get; set; }
    }

    public class HelloAllResponse
    {
        public string Result { get; set; }
    }

    public class HelloAllCustom : IReturn<HelloAllCustomResponse>
    {
        public string Name { get; set; }
    }

    public class HelloAllCustomResponse
    {
        public string Result { get; set; }
    }

    public class HelloAllTransaction : IReturn<HelloAllTransactionResponse>
    {
        public string Name { get; set; }
    }

    public class HelloAllTransactionResponse
    {
        public string Result { get; set; }
    }

    public class Request : IReturnVoid
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ReplyAllService : Service
    {
        [ReplyAllRequest]
        [ReplyAllResponse]
        public object Any(HelloAll request)
        {
            return new HelloAllResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }

        public object Get(HelloGet request)
        {
            return new HelloAllResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }

        [ReplyAllRequest]
        [ReplyAllResponse]
        public object Any(HelloAllCustom request)
        {
            return new HelloAllCustomResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }

        [ReplyAllArrayRequest]
        [ReplyAllArrayResponse]
        public object Any(HelloAllCustom[] requests)
        {
            return requests.Map(x => new HelloAllCustomResponse
            {
                Result = "Custom, {0}!".Fmt(x.Name)
            });
        }

        public object Any(HelloAllTransaction request)
        {
            if (request.Name == "Bar")
                throw new ArgumentException("No Bar allowed here");

            Db.Insert(request);

            return new HelloAllTransactionResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }

        public object Any(HelloAllTransaction[] requests)
        {
            using (var trans = Db.OpenTransaction())
            {
                var response = requests.Map(Any);

                trans.Commit();

                return response;
            }
        }

        public void Any(Request request)
        {
            Redis.Store(request);
        }

        public void Any(Request[] requests)
        {
            Redis.StoreAll(requests);
        }
    }

    [TestFixture]
    public class ReplyAllTests
    {
        private ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new ReplyAllAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_send_single_HelloAll_request()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var request = new HelloAll { Name = "Foo" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo("Hello, Foo!"));
        }

        [Test]
        public void Can_send_multi_reply_HelloAll_requests()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var requests = new[]
            {
                new HelloAll { Name = "Foo" },
                new HelloAll { Name = "Bar" },
                new HelloAll { Name = "Baz" },
            };

            var responses = client.SendAll(requests);
            responses.PrintDump();

            var results = responses.Map(x => x.Result);

            Assert.That(results, Is.EquivalentTo(new[] {
                "Hello, Foo!", "Hello, Bar!", "Hello, Baz!"
            }));
        }

        [Test]
        public void Can_send_multi_reply_HelloGet_requests()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri)
            {
                RequestFilter = req =>
                    req.Headers[HttpHeaders.XHttpMethodOverride] = HttpMethods.Get
            };

            var requests = new[]
            {
                new HelloGet { Name = "Foo" },
                new HelloGet { Name = "Bar" },
                new HelloGet { Name = "Baz" },
            };

            client.Get(new HelloGet { Name = "aaa" });

            var responses = client.SendAll(requests);
            responses.PrintDump();

            var results = responses.Map(x => x.Result);

            Assert.That(results, Is.EquivalentTo(new[] {
                "Hello, Foo!", "Hello, Bar!", "Hello, Baz!"
            }));
        }

        [Test]
        public void Can_send_single_HelloAllCustom_request()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var request = new HelloAllCustom { Name = "Foo" };
            var response = client.Send(request);
            Assert.That(response.Result, Is.EqualTo("Hello, Foo!"));
        }

        [Test]
        public void Can_send_multi_reply_HelloAllCustom_requests()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var requests = new[]
            {
                new HelloAllCustom { Name = "Foo" },
                new HelloAllCustom { Name = "Bar" },
                new HelloAllCustom { Name = "Baz" },
            };

            var responses = client.SendAll(requests);
            responses.PrintDump();

            var results = responses.Map(x => x.Result);

            Assert.That(results, Is.EquivalentTo(new[] {
                "Custom, Foo!", "Custom, Bar!", "Custom, Baz!"
            }));
        }

        [Test]
        public async Task Can_send_async_multi_reply_HelloAllCustom_requests()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var requests = new[]
            {
                new HelloAllCustom { Name = "Foo" },
                new HelloAllCustom { Name = "Bar" },
                new HelloAllCustom { Name = "Baz" },
            };

            var responses = await client.SendAllAsync(requests);
            responses.PrintDump();

            var results = responses.Map(x => x.Result);

            Assert.That(results, Is.EquivalentTo(new[] {
                "Custom, Foo!", "Custom, Bar!", "Custom, Baz!"
            }));
        }

        [Test]
        public void Can_send_multi_oneway_HelloAllCustom_requests()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var requests = new[]
            {
                new HelloAllCustom { Name = "Foo" },
                new HelloAllCustom { Name = "Bar" },
                new HelloAllCustom { Name = "Baz" },
            };

            client.SendAllOneWay(requests);
        }

        [Test]
        public void Can_send_multiple_single_HelloAllTransaction()
        {
            using (var db = appHost.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<HelloAllTransaction>();
            }

            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var names = new[] { "Foo", "Bar", "Baz" };

            try
            {
                foreach (var name in names)
                {
                    client.Send(new HelloAllTransaction { Name = name });
                }

                Assert.Fail("Should throw on Bar");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(typeof(ArgumentException).Name));
            }

            using (var db = appHost.Resolve<IDbConnectionFactory>().Open())
            {
                var allRequests = db.Select<HelloAllTransaction>();
                Assert.That(allRequests.Count, Is.EqualTo(1));
                Assert.That(allRequests[0].Name, Is.EqualTo("Foo"));
            }
        }

        [Test]
        public void Sending_multiple_HelloAllTransaction_does_rollback_transaction()
        {
            using (var db = appHost.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<HelloAllTransaction>();
            }

            var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            var requests = new[]
            {
                new HelloAllTransaction { Name = "Foo" },
                new HelloAllTransaction { Name = "Bar" },
                new HelloAllTransaction { Name = "Baz" },
            };

            try
            {
                var responses = client.SendAll(requests);

                Assert.Fail("Should throw on Bar");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(typeof(ArgumentException).Name));
            }

            using (var db = appHost.Resolve<IDbConnectionFactory>().Open())
            {
                var allRequests = db.Select<HelloAllTransaction>();
                Assert.That(allRequests.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void Can_store_multiple_requests_with_SendAllOneWay()
        {
            using (var redis = appHost.Resolve<IRedisClientsManager>().GetClient())
            {
                redis.FlushAll();

                var client = new JsonServiceClient(Config.AbsoluteBaseUri);
                var requests = new[]
                {
                    new Request { Id = 1, Name = "Foo" },
                    new Request { Id = 2, Name = "Bar" },
                    new Request { Id = 3, Name = "Baz" },
                };

                client.SendAllOneWay(requests);

                var savedRequests = redis.As<Request>().GetAll();

                Assert.That(savedRequests.Map(x => x.Name), Is.EquivalentTo(new[] {
                    "Foo", "Bar", "Baz"
                }));
            }
        }
    }
}