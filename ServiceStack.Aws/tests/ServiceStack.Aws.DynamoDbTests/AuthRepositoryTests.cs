using System;
using System.Collections.Generic;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.DataAnnotations;
using ServiceStack.Script;
using ServiceStack.Testing;

namespace ServiceStack.Aws.DynamoDbTests
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

    public class NewDynamoDbAuthRepositoryTests : AuthRepositoryTestsBase
    {
        public override void ConfigureAuthRepo(Container container)
        {
            var db = DynamoTestBase.CreatePocoDynamo();
            container.AddSingleton(db);
            container.Register<IAuthRepository>(c => 
                new DynamoDbAuthRepository<AppUser,AppUserDetails>(c.Resolve<IPocoDynamo>()));
        }
    }
    
    public class DynamoDbAuthRepositoryQueryTests : AuthRepositoryQueryTestsBase
    {
        public override void ConfigureAuthRepo(Container container) =>
            new NewDynamoDbAuthRepositoryTests().ConfigureAuthRepo(container);
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

        const string Password = "p@55wOrd";

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
            }, Password);

            Assert.That(newUser.Email, Is.EqualTo("user@gmail.com"));

            var fromDb = authRepo.GetUserAuth((newUser as AppUser)?.Key ?? newUser.Id.ToString());
            Assert.That(fromDb.Email, Is.EqualTo("user@gmail.com"));

            newUser.FirstName = "Updated";
            authRepo.SaveUserAuth(newUser);

            var newSession = SessionFeature.CreateNewSession(null, "SESSION_ID");
            newSession.PopulateSession(newUser);

            var updatedUser = authRepo.GetUserAuth(newSession.UserAuthId);
            Assert.That(updatedUser, Is.Not.Null);
            Assert.That(updatedUser.FirstName, Is.EqualTo("Updated"));

            var authUser = authRepo.TryAuthenticate(newUser.Email, Password, out var ret)
                ? ret 
                : null;
            Assert.That(authUser, Is.Not.Null);
            Assert.That(authUser.FirstName, Is.EqualTo(updatedUser.FirstName));
            
            authRepo.DeleteUserAuth(newSession.UserAuthId);
            var deletedUserAuth = authRepo.GetUserAuth(newSession.UserAuthId);
            Assert.That(deletedUserAuth, Is.Null);
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
            }, Password);
            
            var newSession = SessionFeature.CreateNewSession(null, "SESSION_ID");
            newSession.PopulateSession(newUser);
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

            authRepo.DeleteUserAuth(newSession.UserAuthId);
            userAuthDetailsList = authRepo.GetUserAuthDetails(newSession.UserAuthId);
            Assert.That(userAuthDetailsList, Is.Empty);
            var deletedUserAuth = authRepo.GetUserAuth(newSession.UserAuthId);
            Assert.That(deletedUserAuth, Is.Null);
        }
    }
    
    [TestFixture]
    public abstract class AuthRepositoryQueryTestsBase
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
                    
                    SeedData(authRepo);
                }
            }.Init();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose(); 

        void SeedData(IAuthRepository authRepo)
        {
            var newUser = authRepo.CreateUserAuth(new AppUser
            {
                Id = 1,
                DisplayName = "Test User",
                Email = "user@gmail.com",
                FirstName = "Test",
                LastName = "User",
            }, "p@55wOrd");

            newUser = authRepo.CreateUserAuth(new AppUser
            {
                Id = 2,
                DisplayName = "Test Manager",
                Email = "manager@gmail.com",
                FirstName = "Test",
                LastName = "Manager",
            }, "p@55wOrd");
            authRepo.AssignRoles(newUser, roles:new[]{ "Manager" });

            newUser = authRepo.CreateUserAuth(new AppUser
            {
                Id = 3,
                DisplayName = "Admin User",
                Email = "admin@gmail.com",
                FirstName = "Admin",
                LastName = "Super User",
            }, "p@55wOrd");
            authRepo.AssignRoles(newUser, roles:new[]{ "Admin" });
        }

        private static bool IsRavenDb(IAuthRepository authRepo) => authRepo.GetType().Name.StartsWith("Raven");

        private static void AssertHasIdentity(IAuthRepository authRepo, List<IUserAuth> allUsers)
        {
            Assert.That(IsRavenDb(authRepo)
                ? allUsers.Cast<AppUser>().All(x => x.Key != null)
                : allUsers.All(x => x.Id > 0));
        }

        [Test]
        public void Can_QueryUserAuth_GetUserAuths()
        {
            var authRepo = appHost.GetAuthRepository();
            using (authRepo as IDisposable)
            {
                var allUsers = authRepo.GetUserAuths();
                Assert.That(allUsers.Count, Is.EqualTo(3));
                AssertHasIdentity(authRepo, allUsers);
                
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

                var idField = IsRavenDb(authRepo)
                    ? nameof(AppUser.Key)
                    : nameof(UserAuth.Id);
                allUsers = authRepo.GetUserAuths(orderBy: idField + " DESC");
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
                var allUsers = authRepo.SearchUserAuths("gmail.com");
                Assert.That(allUsers.Count, Is.EqualTo(3));
                AssertHasIdentity(authRepo, allUsers);
                Assert.That(allUsers.All(x => x.Email != null));
                
                allUsers = authRepo.SearchUserAuths(query:"gmail.com",skip:1);
                Assert.That(allUsers.Count, Is.EqualTo(2));
                allUsers = authRepo.SearchUserAuths(query:"gmail.com",take:2);
                Assert.That(allUsers.Count, Is.EqualTo(2));
                allUsers = authRepo.SearchUserAuths(query:"gmail.com",skip:1,take:2);
                Assert.That(allUsers.Count, Is.EqualTo(2));

                if (!IsRavenDb(authRepo)) // RavenDB only searches UserName/Email and only StartsWith/EndsWith
                {
                    allUsers = authRepo.SearchUserAuths(query:"Test");
                    Assert.That(allUsers.Count, Is.EqualTo(2));

                    allUsers = authRepo.SearchUserAuths(query:"Admin");
                    Assert.That(allUsers.Count, Is.EqualTo(1));

                    allUsers = authRepo.SearchUserAuths(query:"Test",skip:1,take:1,orderBy:nameof(UserAuth.Email));
                    Assert.That(allUsers.Count, Is.EqualTo(1));
                    Assert.That(allUsers[0].Email, Is.EqualTo("user@gmail.com"));
                }
            }
        }

        [Test]
        public void Can_QueryUserAuth_in_Script()
        {
            var context = appHost.AssertPlugin<SharpPagesFeature>();
            Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths() | count }}"), Is.EqualTo("3"));
            Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths({ orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("1,2,3"));
            Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths({ skip:1, orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("2,3"));
            Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths({ take:2, orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("1,2"));
            Assert.That(context.EvaluateScript("{{ authRepo.getUserAuths({ skip:1, take:2, orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("2,3"));

            Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'gmail.com',orderBy:'Id'}) | map => it.Id | join }}"), Is.EqualTo("1,2,3"));
            Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'gmail.com',skip:1,take:1,orderBy:'Id'}) | map => it.Id | join }}"), Is.EqualTo("2"));

            if (!IsRavenDb(appHost.TryResolve<IAuthRepository>()))
            {
                Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'gmail.com', orderBy:'LastName DESC' }) | map => it.Id | join }}"), Is.EqualTo("1,3,2"));
                Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'Test', orderBy:'Email' }) | map => it.Id | join }}"), Is.EqualTo("2,1"));
                Assert.That(context.EvaluateScript("{{ authRepo.searchUserAuths({ query:'Test', orderBy:'Id' }) | map => it.Id | join }}"), Is.EqualTo("1,2"));
            }
        }
    }
}    
