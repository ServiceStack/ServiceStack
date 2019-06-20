using System;
using System.Collections.Generic;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Testing;
using ServiceStack.DataAnnotations;

using MongoDB.Driver;
using ServiceStack.Authentication.MongoDb;

using ServiceStack.Authentication.RavenDb;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Text;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Queries;

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

    [Index(Name = nameof(Key))]
    public class AppUserDetails : UserAuthDetails
    {
        public string Key { get; set; }
    }

    public class MemoryAuthRepositoryTests : AuthRepositoryTestsBase
    {
        public override void ConfigureAuthRepo(Container container)
        {
            container.Register<IAuthRepository>(c => new InMemoryAuthRepository<AppUser,AppUserDetails>());
        }
    }
    
    public class RedisAuthRepositoryTests : AuthRepositoryTestsBase
    {
        public override void ConfigureAuthRepo(Container container)
        {
            container.Register<IRedisClientsManager>(c => new RedisManagerPool());
            container.Register<IAuthRepository>(c => 
                new RedisAuthRepository<AppUser,AppUserDetails>(c.Resolve<IRedisClientsManager>()));
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

            container.Register<IAuthRepository>(c => 
                new OrmLiteAuthRepository<AppUser,AppUserDetails>(c.Resolve<IDbConnectionFactory>()));
        }
    }

    [NUnit.Framework.Ignore("Requires RavenDB")]
    public class RavenDbAuthRepositoryTests : AuthRepositoryTestsBase
    {
        public override void ConfigureAuthRepo(Container container)
        {
            var store = new DocumentStore
            {
                Urls = new[]                        // URL to the Server,
                {                                   // or list of URLs 
                    "http://localhost:8080"         // to all Cluster Servers (Nodes)
                },
                Database = "test",                  // Default database that DocumentStore will interact with
                Conventions = {
                }
            };
            store.Conventions.FindIdentityProperty = RavenDbUserAuthRepository.FindIdentityProperty;
            
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
                    
                    if (authRepo is IClearable clearable)
                    {
                        try { clearable.Clear(); } catch {}
                    }

                    authRepo.InitSchema();
                }
            }.Init();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

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

            newUser.FirstName = "Updated";
            authRepo.SaveUserAuth(newUser);
            
            var newSession = SessionFeature.CreateNewSession(null, "SESSION_ID");
            newSession.PopulateSession(newUser, new List<IAuthTokens>());

            var updatedUser = authRepo.GetUserAuth(newSession.UserAuthId);
            Assert.That(updatedUser, Is.Not.Null);
            Assert.That(updatedUser.FirstName, Is.EqualTo("Updated"));
        }

        [Test]
        public void Can_AddUserAuthDetails()
        {
            var authRepo = appHost.TryResolve<IAuthRepository>();
            
            var newUser = authRepo.CreateUserAuth(new AppUser
            {
                DisplayName = "Facebook User",
                Email = "user@fb.com",
                FirstName = "Face",
                LastName = "Book",
            }, "p@55wOrd");
            
            var newSession = SessionFeature.CreateNewSession(null, "SESSION_ID");
            newSession.PopulateSession(newUser, new List<IAuthTokens>());
            Assert.That(newSession.Email, Is.EqualTo("user@fb.com"));

            var fbAuthTokens = new AuthTokens
            {
                Provider = FacebookAuthProvider.Name,
                AccessTokenSecret = "AAADDDCCCoR848BAMkQIZCRIKnVWZAvcKWqo7Ibvec8ebV9vJrfZAz8qVupdu5EbjFzmMmbwUFDbcNDea9H6rOn5SVn8es7KYZD",
                UserId = "123456",
                DisplayName = "FB User",
                FirstName = "FB",
                LastName = "User",
                Email = "user@fb.com",
            };
            
            var userAuthDetails = authRepo.CreateOrMergeAuthSession(newSession, fbAuthTokens);
            Assert.That(userAuthDetails.Email, Is.EqualTo("user@fb.com"));

            var userAuthDetailsList = authRepo.GetUserAuthDetails(newSession.UserAuthId);
            Assert.That(userAuthDetailsList.Count, Is.EqualTo(1));
            Assert.That(userAuthDetailsList[0].Email, Is.EqualTo("user@fb.com"));
        }

    }
}
