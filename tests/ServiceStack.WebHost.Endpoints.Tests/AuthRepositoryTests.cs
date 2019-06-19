using System;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Testing;
using ServiceStack.DataAnnotations;

using MongoDB.Driver;
using ServiceStack.Authentication.MongoDb;

using Raven.Client;
using ServiceStack.Authentication.RavenDb;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Text;
#if NETCORE_SUPPORT
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Queries;
#else
using Raven.Client.Document;
#endif

namespace ServiceStack.WebHost.Endpoints.Tests
{
    // Custom UserAuth Data Model with extended Metadata properties
    [Index(Name = nameof(Key))]
    public class AppUser : UserAuth
    {
        public string Key { get; set; }
        public string ProfileUrl { get; set; }
        public string LastLoginIp { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class AppUserDetails : UserAuthDetails
    {
        public DateTime? LastLoginDate { get; set; }
    }
    
    public class MemoryAuthRepositoryTests : AuthRepositoryTestsBase
    {
        public override void ConfigureAuthRepo(Container container)
        {
            container.Register<IAuthRepository>(c => new InMemoryAuthRepository());
        }
    }
    
    public class RedisAuthRepositoryTests : AuthRepositoryTestsBase
    {
        public override void ConfigureAuthRepo(Container container)
        {
            container.Register<IRedisClientsManager>(c => new RedisManagerPool());
            container.Register<IAuthRepository>(c => 
                new RedisAuthRepository(c.Resolve<IRedisClientsManager>()));

            using (var client = container.Resolve<IRedisClientsManager>().GetClient())
            {
                client.FlushAll();
            }
        }
    }
    
    public class OrmLiteAuthRepositoryTests : AuthRepositoryTestsBase
    {
        public override void ConfigureAuthRepo(Container container)
        {
            container.Register<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider) {
                    AutoDisposeConnection = false,
                });

            container.Register<IAuthRepository>(c => new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));
        }
    }

    [NUnit.Framework.Ignore("Requires RavenDB")]
    public class RavenDbAuthRepositoryTests : AuthRepositoryTestsBase
    {
        public override void ConfigureAuthRepo(Container container)
        {
            var store = new DocumentStore
            {
#if NETCORE_SUPPORT
                Urls = new[]                        // URL to the Server,
                {                                   // or list of URLs 
                    "http://localhost:8080"         // to all Cluster Servers (Nodes)
                },
                Database = "test",                  // Default database that DocumentStore will interact with
#else
                DefaultDatabase = "test",
                Url = "http://localhost:8080",
#endif
                Conventions = {
                }
            };
            store.Conventions.FindIdentityProperty = p => {
                var attr = p.DeclaringType.FirstAttribute<IndexAttribute>();
                return attr != null
                    ? p.Name == attr.Name
                    : p.Name == "Id";
            };
            
            container.AddSingleton(store.Initialize());

            container.Register<IAuthRepository>(c =>
                new RavenDbUserAuthRepository<AppUser, AppUserDetails>(c.Resolve<IDocumentStore>()));

            var response = $"http://localhost:8080/databases/test/queries?allowStale=False&maxOpsPerSec=&details=False"
                .SendStringToUrl(HttpMethods.Delete, "{\"Query\":\"from AppUsers\",\"QueryParameters\":null}");
        }
    }

    [NUnit.Framework.Ignore("Requires MongoDB")]
    public class MongoDbAuthRepositoryTests : AuthRepositoryTestsBase
    {
        public override void ConfigureAuthRepo(Container container)
        {
            var mongoClient = new MongoClient();
            mongoClient.DropDatabase("MyApp");
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase("MyApp");

            container.AddSingleton(mongoDatabase);
            container.AddSingleton<IAuthRepository>(c => 
                new MongoDbAuthRepository(c.Resolve<IMongoDatabase>(), createMissingCollections:true));
        }
    }
    
    public abstract class AuthRepositoryTestsBase
    {
        private ServiceStackHost appHost;

        public abstract void ConfigureAuthRepo(Container container); 
        
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BasicAppHost(typeof(AuthRepositoryQueryTestsBase).Assembly) 
            {
                ConfigureAppHost = host =>
                {
                    host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), new IAuthProvider[] {
                        new CredentialsAuthProvider(), 
                    })
                    {
                        IncludeRegistrationService = true,
                    });
                    
                    host.Plugins.Add(new SharpPagesFeature());
                },
                ConfigureContainer = container => {
                    ConfigureAuthRepo(container);
                    var authRepo = container.Resolve<IAuthRepository>();
                    authRepo.InitSchema();
                }
            }.Init();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        void SeedData(IAuthRepository authRepo)
        {
            var newUser = authRepo.CreateUserAuth(new UserAuth
            {
                DisplayName = "Test User",
                Email = "user@gmail.com",
                FirstName = "Test",
                LastName = "User",
            }, "p@55wOrd");

            newUser = authRepo.CreateUserAuth(new UserAuth
            {
                DisplayName = "Test Manager",
                Email = "manager@gmail.com",
                FirstName = "Test",
                LastName = "Manager",
            }, "p@55wOrd");
            authRepo.AssignRoles(newUser, roles:new[]{ "Manager" });

            newUser = authRepo.CreateUserAuth(new UserAuth
            {
                DisplayName = "Admin User",
                Email = "admin@gmail.com",
                FirstName = "Admin",
                LastName = "Super User",
            }, "p@55wOrd");
            authRepo.AssignRoles(newUser, roles:new[]{ "Admin" });
        }

        [Test]
        public void Can_CreateUserAuth()
        {
            var authRepo = appHost.TryResolve<IAuthRepository>();
            
            var newUser = authRepo.CreateUserAuth(new AppUser
            {
                DisplayName = "Test User",
                Email = "user@gmail.com",
                FirstName = "Test",
                LastName = "User",
            }, "p@55wOrd");
            
            Assert.That(newUser.Email, Is.EqualTo("user@gmail.com"));

            var fromDb = authRepo.GetUserAuth((newUser as AppUser)?.Key ?? newUser.Id.ToString());
            Assert.That(fromDb.Email, Is.EqualTo("user@gmail.com"));
        }
    }
}
