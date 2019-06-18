using System;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Script;
using ServiceStack.Testing;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class MemoryAuthRepositoryQueryTests : AuthRepositoryQueryTestsBase
    {
        protected override void ConfigureAuthRepo(Container container)
        {
            container.Register<IAuthRepository>(c => new InMemoryAuthRepository());
        }
    }
    
    public class RedisAuthRepositoryQueryTests : AuthRepositoryQueryTestsBase
    {
        protected override void ConfigureAuthRepo(Container container)
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
    
    public class OrmLiteAuthRepositoryQueryTests : AuthRepositoryQueryTestsBase
    {
        protected override void ConfigureAuthRepo(Container container)
        {
            container.Register<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider) {
                    AutoDisposeConnection = false,
                });

            container.Register<IAuthRepository>(c => new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));
        }
    }
    
    [TestFixture]
    public abstract class AuthRepositoryQueryTestsBase
    {
        private ServiceStackHost appHost;

        protected abstract void ConfigureAuthRepo(Container container); 
        
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
                    SeedData(authRepo);
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
        public void Can_QueryUserAuth_GetUserAuths()
        {
            var authRepo = appHost.GetAuthRepository();
            using (authRepo as IDisposable)
            {
                var allUsers = authRepo.GetUserAuths();
                Assert.That(allUsers.Count, Is.EqualTo(3));
                Assert.That(allUsers.All(x => x.Id > 0));
                Assert.That(allUsers.All(x => x.Email != null));
                
                allUsers = authRepo.GetUserAuths(skip:1);
                Assert.That(allUsers.Count, Is.EqualTo(2));
                allUsers = authRepo.GetUserAuths(take:2);
                Assert.That(allUsers.Count, Is.EqualTo(2));
                allUsers = authRepo.GetUserAuths(skip:1,take:2);
                Assert.That(allUsers.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void Can_QueryUserAuth_GetUserAuths_OrderBy()
        {
            var authRepo = appHost.GetAuthRepository();
            using (authRepo as IDisposable)
            {
                var allUsers = authRepo.GetUserAuths(orderBy:nameof(UserAuth.Id));
                Assert.That(allUsers.Count, Is.EqualTo(3));
                Assert.That(allUsers[0].Email, Is.EqualTo("user@gmail.com"));
                
                allUsers = authRepo.GetUserAuths(orderBy:nameof(UserAuth.Id) + " DESC");
                Assert.That(allUsers.Count, Is.EqualTo(3));
                Assert.That(allUsers[0].Email, Is.EqualTo("admin@gmail.com"));
                
                allUsers = authRepo.GetUserAuths(orderBy:nameof(UserAuth.DisplayName));
                Assert.That(allUsers.Count, Is.EqualTo(3));
                Assert.That(allUsers[0].DisplayName, Is.EqualTo("Admin User"));
                
                allUsers = authRepo.GetUserAuths(orderBy:nameof(UserAuth.Email));
                Assert.That(allUsers.Count, Is.EqualTo(3));
                Assert.That(allUsers[0].Email, Is.EqualTo("admin@gmail.com"));
                
                allUsers = authRepo.GetUserAuths(orderBy:nameof(UserAuth.CreatedDate) + " DESC");
                Assert.That(allUsers.Count, Is.EqualTo(3));
                Assert.That(allUsers[0].DisplayName, Is.EqualTo("Admin User"));
            }
        }

        [Test]
        public void Can_QueryUserAuth_SearchUserAuths()
        {
            var authRepo = appHost.GetAuthRepository();
            using (authRepo as IDisposable)
            {
                var allUsers = authRepo.SearchUserAuths("gmail");
                Assert.That(allUsers.Count, Is.EqualTo(3));
                Assert.That(allUsers.All(x => x.Id > 0));
                Assert.That(allUsers.All(x => x.Email != null));
                
                allUsers = authRepo.SearchUserAuths(query:"gmail",skip:1);
                Assert.That(allUsers.Count, Is.EqualTo(2));
                allUsers = authRepo.SearchUserAuths(query:"gmail",take:2);
                Assert.That(allUsers.Count, Is.EqualTo(2));
                allUsers = authRepo.SearchUserAuths(query:"gmail",skip:1,take:2);
                Assert.That(allUsers.Count, Is.EqualTo(2));

                allUsers = authRepo.SearchUserAuths(query:"Test");
                Assert.That(allUsers.Count, Is.EqualTo(2));

                allUsers = authRepo.SearchUserAuths(query:"Admin");
                Assert.That(allUsers.Count, Is.EqualTo(1));

                allUsers = authRepo.SearchUserAuths(query:"Test",skip:1,take:1,orderBy:nameof(UserAuth.Email));
                Assert.That(allUsers.Count, Is.EqualTo(1));
                Assert.That(allUsers[0].Email, Is.EqualTo("user@gmail.com"));
            }
        }

        [Test]
        public void Can_QueryUserAuth_in_Script()
        {
            var context = appHost.AssertPlugin<SharpPagesFeature>();
            Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths() | count }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths() | map => it.Id | join }}"), Is.EqualTo("1,2,3"));
            Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths({ skip:1, orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("2,3"));
            Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths({ take:2, orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("1,2"));
            Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths({ skip:1, take:2, orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("2,3"));

            Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'gmail'}) | map => it.Id | join }}"), Is.EqualTo("1,2,3"));
            Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'gmail', orderBy:'LastName DESC' }) | map => it.Id | join }}"), Is.EqualTo("1,3,2"));
            Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'gmail',skip:1,take:1}) | map => it.Id | join }}"), Is.EqualTo("2"));
            Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'Test', orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("1,2"));
            Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'Test', orderBy:'Email' }) | map => it.Id | join }}"), Is.EqualTo("2,1"));
        }
    }
}