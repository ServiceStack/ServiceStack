using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Admin;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class MemoryAdminUsersFeatureTests : AdminUsersFeatureTests
    {
        protected override void Configure(ServiceStackHost appHost)
        {
            appHost.Register<IAuthRepository>(new InMemoryAuthRepository());
        }
    }
    
    public class OrmLiteAdminUsersFeatureTests : AdminUsersFeatureTests
    {
        protected override void Configure(ServiceStackHost appHost)
        {
            var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
            
            appHost.Register<IDbConnectionFactory>(dbFactory);
            appHost.Register<IAuthRepository>(new OrmLiteAuthRepository(dbFactory));
            appHost.Resolve<IAuthRepository>().InitSchema();
        }
    }
    
    public abstract class AdminUsersFeatureTests
    {
        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost();
            Configure(appHost);
            appHost
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        protected abstract void Configure(ServiceStackHost appHost); 
        
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(AdminUsersFeatureTests), typeof(AdminUsersFeatureTests).Assembly) { }
            
            public override void Configure(Container container)
            {
                SetConfig(new HostConfig {
                    AdminAuthSecret = "secretz"
                });
                Plugins.Add(new AdminUsersFeature());
            }
        }

        [SetUp]
        public void SetUp()
        {
            var authRepo = appHost.GetAuthRepository();
            using (authRepo as IDisposable)
            {
                ((IClearable)authRepo).Clear();
            }
        }
        
        JsonServiceClient client = new JsonServiceClient(Config.ListeningOn) {
            Headers = {
                [HttpHeaders.XParamOverridePrefix + Keywords.AuthSecret] = "secretz"
            }
        };

        private static AdminCreateUser CreateUserRequest() => new() {
            FirstName = "First",
            LastName = "Last",
            Email = "user@email.com",
            Password = "p@ss",
            ProfileUrl = Svg.Images[Svg.Icons.MaleBusiness],
            Roles = new List<string> {"TheRole"}
        };

        private static void AssertFirstLastUser(AdminUserResponse response)
        {
            Assert.That(response.Id, Is.Not.Null);
            Assert.That(response.Result[nameof(UserAuth.FirstName)], Is.EqualTo("First"));
            Assert.That(response.Result[nameof(UserAuth.LastName)], Is.EqualTo("Last"));
            Assert.That(response.Result[nameof(UserAuth.DisplayName)], Is.EqualTo("First Last"));
            Assert.That(response.Result[nameof(UserAuth.Email)], Is.EqualTo("user@email.com"));
            Assert.That(response.Result[nameof(UserAuth.PrimaryEmail)], Is.EqualTo("user@email.com"));
            Assert.That(response.Result[nameof(UserAuth.Roles)], Is.EqualTo(new List<string> {"TheRole"}));
            Assert.That(response.Result[nameof(IAuthSession.ProfileUrl)], Is.EqualTo(Svg.Images[Svg.Icons.MaleBusiness]));
            Assert.That(response.Result[nameof(IAuthSession.Roles)], Is.EquivalentTo(new[]{ "TheRole" }));
        }
        
        [Test]
        public async Task Can_AdminCreateUser()
        {
            var createUserRequest = CreateUserRequest();

            var response = client.Post(createUserRequest);
            AssertFirstLastUser(response);

            var authRepo = appHost.GetAuthRepository();
            using (authRepo as IDisposable)
            {
                Assert.That(authRepo.TryAuthenticate("user@email.com", "p@ss", out _));
            }

            try
            {
                client.Post(createUserRequest);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.ErrorCode, Is.EqualTo("AlreadyExists"));
            }

            response = client.Get(new AdminGetUser {
                Id = response.Id
            });
            AssertFirstLastUser(response);
        }

        [Test]
        public async Task Can_AdminUpdateUser()
        {
            var createUserRequest = CreateUserRequest();

            var response = client.Post(createUserRequest);
            AssertFirstLastUser(response);
            
            var updateUserRequest = new AdminUpdateUser {
                Id = response.Id,
                FirstName = "Given",
                LastName = "Surname",
                DisplayName = "Display Name",
                Email = "newuser@email.com",
                Password = "newp@ss",
                ProfileUrl = Svg.Images[Svg.Icons.FemaleBusiness],
                RemoveRoles = new List<string> {"TheRole"},
                AddRoles = new List<string> {"NewRole"},
                AddPermissions = new List<string> {"ThePermission"},
            };

            var updated = client.Put(updateUserRequest);
            Assert.That(updated.Id, Is.EqualTo(response.Id));
            Assert.That(updated.Result[nameof(UserAuth.FirstName)], Is.EqualTo("Given"));
            Assert.That(updated.Result[nameof(UserAuth.LastName)], Is.EqualTo("Surname"));
            Assert.That(updated.Result[nameof(UserAuth.DisplayName)], Is.EqualTo("Display Name"));
            Assert.That(updated.Result[nameof(UserAuth.Email)], Is.EqualTo("newuser@email.com"));
            Assert.That(updated.Result[nameof(UserAuth.PrimaryEmail)], Is.EqualTo("newuser@email.com"));
            Assert.That(updated.Result[nameof(UserAuth.Roles)], Is.EqualTo(new List<string> {"NewRole"}));
            Assert.That(updated.Result[nameof(UserAuth.Permissions)], Is.EqualTo(new List<string> {"ThePermission"}));
            Assert.That(updated.Result[nameof(IAuthSession.ProfileUrl)], Is.EqualTo(Svg.Images[Svg.Icons.FemaleBusiness]));
 
            var authRepo = appHost.GetAuthRepository();
            using (authRepo as IDisposable)
            {
                Assert.That(authRepo.TryAuthenticate("newuser@email.com", "newp@ss", out _));
            }
        }

        [Test]
        public async Task Can_AdminDeleteUser()
        {
            var createUserRequest = CreateUserRequest();

            var response = client.Post(createUserRequest);

            client.Delete(new AdminDeleteUser { Id = response.Id });

            try
            {
                response = client.Get(new AdminGetUser {
                    Id = response.Id
                });
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
            }
        }

        [Test]
        public async Task Can_AdminQueryUsers()
        {
            var createUserRequest = CreateUserRequest();

            var response = client.Post(createUserRequest);

            var searchResults = client.Get(new AdminQueryUsers());
            Assert.That(searchResults.Results.Count, Is.GreaterThan(0));

            searchResults = client.Get(new AdminQueryUsers {
                Query = "First"
            });
            Assert.That(searchResults.Results.Count, Is.GreaterThan(0));

            searchResults = client.Get(new AdminQueryUsers {
                Query = "Unknown"
            });
            Assert.That(searchResults.Results.Count, Is.EqualTo(0));
        }

    }
}