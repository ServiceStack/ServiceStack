using System.Linq;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Server.Tests.Auth;
using ServiceStack.Server.Tests.Services;
#pragma warning disable CS0169

namespace ServiceStack.Server.Tests.Async
{
    [TestFixture]
    public class ValueTaskTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(ApiKeyAuthTests), typeof(AppHost).Assembly) { }

            public override void Configure(Container container)
            {
                container.Register<IRedisClientsManager>(c =>
                    new RedisManagerPool());
                
                container.Register<IAuthRepository>(c =>
                    new RedisAuthRepository(c.Resolve<IRedisClientsManager>()));
                
                container.Resolve<IAuthRepository>().InitSchema();

                Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                    new IAuthProvider[] {
                        new CredentialsAuthProvider(), 
                        new ApiKeyAuthProvider(AppSettings) { RequireSecureConnection = false },
                    }) {
                    IncludeRegistrationService = true,
                });
            }
        }

        public const string ListeningOn = "http://localhost:20000/";
        public const string Username = "user";
        public const string Password = "p@55word";
        private ServiceStackHost appHost;
        private string userId;
        private ApiKey liveKey;
        private ApiKey testKey;

        JsonServiceClient client = new JsonServiceClient(ListeningOn);

        public ValueTaskTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(ListeningOn);

            // var response = client.Post(new Register {
            //     UserName = Username,
            //     Password = Password,
            //     Email = "as@if{0}.com",
            //     DisplayName = "DisplayName",
            //     FirstName = "FirstName",
            //     LastName = "LastName",
            // });
            //
            // userId = response.UserId;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public async Task Can_call_AsyncRedis_ValueTask()
        {
            await using var redis = await appHost.GetRedisClientAsync();
            await redis.FlushAllAsync();
            
            var response = await client.GetAsync(new AsyncRedis {
                Incr = 1,
            });
            
            Assert.That(response.Id, Is.EqualTo("1"));
        }

        [Test]
        public async Task Can_call_SGAsyncRedis1_ValueTask()
        {
            await using var redis = await appHost.GetRedisClientAsync();
            await redis.FlushAllAsync();
            
            var response = await client.GetAsync(new SGAsyncRedis1 {
                Incr = 1,
            });
            
            Assert.That(response.Id, Is.EqualTo("1"));
        }

        [Test]
        public async Task Can_call_SGAsyncRedis2_ValueTask()
        {
            await using var redis = await appHost.GetRedisClientAsync();
            await redis.FlushAllAsync();
            
            var response = await client.GetAsync(new SGAsyncRedis1 {
                Incr = 1,
            });
            
            Assert.That(response.Id, Is.EqualTo("1"));
        }

        [Test]
        public async Task Can_call_SGAsyncRedisSync_ValueTask()
        {
            await using var redis = await appHost.GetRedisClientAsync();
            await redis.FlushAllAsync();
            
            var response = await client.GetAsync(new SGAsyncRedisSync {
                Incr = 1,
            });
            
            Assert.That(response.Id, Is.EqualTo("1"));
        }
    }
}