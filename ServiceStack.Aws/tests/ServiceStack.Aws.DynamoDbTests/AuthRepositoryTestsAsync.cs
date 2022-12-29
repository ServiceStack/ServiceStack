using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.DataAnnotations;
using ServiceStack.Script;
using ServiceStack.Testing;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class NewDynamoDbAuthRepositoryTestsAsync : AuthRepositoryTestsBaseAsync
    {
        public override void ConfigureAuthRepo(Container container)
        {
            var db = DynamoTestBase.CreatePocoDynamo();
            container.AddSingleton(db);
            container.Register<IAuthRepository>(c => 
                new DynamoDbAuthRepository<AppUser,AppUserDetails>(c.Resolve<IPocoDynamo>()));
        }
    }
    
    public class DynamoDbAuthRepositoryQueryTestsAsync : AuthRepositoryQueryTestsBaseAsync
    {
        public override void ConfigureAuthRepo(Container container) =>
            new NewDynamoDbAuthRepositoryTests().ConfigureAuthRepo(container);
    }
    
    public abstract class AuthRepositoryTestsBaseAsync
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
        public async Task Can_CreateUserAuth()
        {
            var authRepo = appHost.GetAuthRepositoryAsync();

            var newUser = await authRepo.CreateUserAuthAsync(new AppUser
            {
                DisplayName = "Test User",
                Email = "user@gmail.com",
                FirstName = "Test",
                LastName = "User",
            }, Password);
            
            Assert.That(newUser.Email, Is.EqualTo("user@gmail.com"));

            var fromDb = await authRepo.GetUserAuthAsync((newUser as AppUser)?.Key ?? newUser.Id.ToString());
            Assert.That(fromDb.Email, Is.EqualTo("user@gmail.com"));

            newUser.FirstName = "Updated";
            await authRepo.SaveUserAuthAsync(newUser);
            
            var newSession = SessionFeature.CreateNewSession(null, "SESSION_ID");
            newSession.PopulateSession(newUser);

            var updatedUser = await authRepo.GetUserAuthAsync(newSession.UserAuthId);
            Assert.That(updatedUser, Is.Not.Null);
            Assert.That(updatedUser.FirstName, Is.EqualTo("Updated"));

            var authUser = await authRepo.TryAuthenticateAsync(newUser.Email, Password);
            Assert.That(authUser, Is.Not.Null);
            Assert.That(authUser.FirstName, Is.EqualTo(updatedUser.FirstName));
            
            await authRepo.DeleteUserAuthAsync(newSession.UserAuthId);
            var deletedUserAuth = await authRepo.GetUserAuthAsync(newSession.UserAuthId);
            Assert.That(deletedUserAuth, Is.Null);
        }

        [Test]
        public async Task Can_AddUserAuthDetails()
        {
            var authRepo = appHost.GetAuthRepositoryAsync();
            
            var newUser = await authRepo.CreateUserAuthAsync(new AppUser
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
            
            var userAuthDetails = await authRepo.CreateOrMergeAuthSessionAsync(newSession, fbAuthTokens);
            Assert.That(userAuthDetails.Email, Is.EqualTo("user@fb.com"));

            var userAuthDetailsList = await authRepo.GetUserAuthDetailsAsync(newSession.UserAuthId);
            Assert.That(userAuthDetailsList.Count, Is.EqualTo(1));
            Assert.That(userAuthDetailsList[0].Email, Is.EqualTo("user@fb.com"));
            
            await authRepo.DeleteUserAuthAsync(newSession.UserAuthId);
            userAuthDetailsList = await authRepo.GetUserAuthDetailsAsync(newSession.UserAuthId);
            Assert.That(userAuthDetailsList, Is.Empty);
            var deletedUserAuth = await authRepo.GetUserAuthAsync(newSession.UserAuthId);
            Assert.That(deletedUserAuth, Is.Null);
        }
    }
    
    [TestFixture]
    public abstract class AuthRepositoryQueryTestsBaseAsync
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

        const string Password = "p@55wOrd";

        void SeedData(IAuthRepository authRepo)
        {
            var newUser = authRepo.CreateUserAuth(new AppUser
            {
                Id = 1,
                DisplayName = "Test User",
                Email = "user@gmail.com",
                FirstName = "Test",
                LastName = "User",
            }, Password);

            newUser = authRepo.CreateUserAuth(new AppUser
            {
                Id = 2,
                DisplayName = "Test Manager",
                Email = "manager@gmail.com",
                FirstName = "Test",
                LastName = "Manager",
            }, Password);
            authRepo.AssignRoles(newUser, roles:new[]{ "Manager" });

            newUser = authRepo.CreateUserAuth(new AppUser
            {
                Id = 3,
                DisplayName = "Admin User",
                Email = "admin@gmail.com",
                FirstName = "Admin",
                LastName = "Super User",
            }, Password);
            authRepo.AssignRoles(newUser, roles:new[]{ "Admin" });
        }

        private static bool IsRavenDb(IAuthRepositoryAsync authRepo) => authRepo.GetType().Name.StartsWith("Raven");

        private static void AssertHasIdentity(IAuthRepositoryAsync authRepo, List<IUserAuth> allUsers)
        {
            Assert.That(IsRavenDb(authRepo)
                ? allUsers.Cast<AppUser>().All(x => x.Key != null)
                : allUsers.All(x => x.Id > 0));
        }

        [Test]
        public async Task Can_QueryUserAuth_GetUserAuths()
        {
            var authRepo = appHost.GetAuthRepositoryAsync();
            using (authRepo as IDisposable)
            {
                var allUsers = await authRepo.GetUserAuthsAsync();
                Assert.That(allUsers.Count, Is.EqualTo(3));
                AssertHasIdentity(authRepo, allUsers);
                
                Assert.That(allUsers.All(x => x.Email != null));
                
                allUsers = await authRepo.GetUserAuthsAsync(skip:1);
                Assert.That(allUsers.Count, Is.EqualTo(2));
                allUsers = await authRepo.GetUserAuthsAsync(take:2);
                Assert.That(allUsers.Count, Is.EqualTo(2));
                allUsers = await authRepo.GetUserAuthsAsync(skip:1,take:2);
                Assert.That(allUsers.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task Can_QueryUserAuth_GetUserAuths_OrderBy()
        {
            var authRepo = appHost.GetAuthRepositoryAsync();
            using (authRepo as IDisposable)
            {
                var allUsers = await authRepo.GetUserAuthsAsync(orderBy:nameof(UserAuth.Id));
                Assert.That(allUsers.Count, Is.EqualTo(3));
                Assert.That(allUsers[0].Email, Is.EqualTo("user@gmail.com"));

                var idField = IsRavenDb(authRepo)
                    ? nameof(AppUser.Key)
                    : nameof(UserAuth.Id);
                allUsers = await authRepo.GetUserAuthsAsync(orderBy: idField + " DESC");
                Assert.That(allUsers.Count, Is.EqualTo(3));
                Assert.That(allUsers[0].Email, Is.EqualTo("admin@gmail.com"));
                
                allUsers = await authRepo.GetUserAuthsAsync(orderBy:nameof(UserAuth.DisplayName));
                Assert.That(allUsers.Count, Is.EqualTo(3));
                Assert.That(allUsers[0].DisplayName, Is.EqualTo("Admin User"));
                
                allUsers = await authRepo.GetUserAuthsAsync(orderBy:nameof(UserAuth.Email));
                Assert.That(allUsers.Count, Is.EqualTo(3));
                Assert.That(allUsers[0].Email, Is.EqualTo("admin@gmail.com"));
                
                allUsers = await authRepo.GetUserAuthsAsync(orderBy:nameof(UserAuth.CreatedDate) + " DESC");
                Assert.That(allUsers.Count, Is.EqualTo(3));
                Assert.That(allUsers[0].DisplayName, Is.EqualTo("Admin User"));
            }
        }

        [Test]
        public async Task Can_QueryUserAuth_SearchUserAuths()
        {
            var authRepo = appHost.GetAuthRepositoryAsync();
            using (authRepo as IDisposable)
            {
                var allUsers = await authRepo.SearchUserAuthsAsync("gmail.com");
                Assert.That(allUsers.Count, Is.EqualTo(3));
                AssertHasIdentity(authRepo, allUsers);
                Assert.That(allUsers.All(x => x.Email != null));
                
                allUsers = await authRepo.SearchUserAuthsAsync(query:"gmail.com",skip:1);
                Assert.That(allUsers.Count, Is.EqualTo(2));
                allUsers = await authRepo.SearchUserAuthsAsync(query:"gmail.com",take:2);
                Assert.That(allUsers.Count, Is.EqualTo(2));
                allUsers = await authRepo.SearchUserAuthsAsync(query:"gmail.com",skip:1,take:2);
                Assert.That(allUsers.Count, Is.EqualTo(2));

                if (!IsRavenDb(authRepo)) // RavenDB only searches UserName/Email and only StartsWith/EndsWith
                {
                    allUsers = await authRepo.SearchUserAuthsAsync(query:"Test");
                    Assert.That(allUsers.Count, Is.EqualTo(2));

                    allUsers = await authRepo.SearchUserAuthsAsync(query:"Admin");
                    Assert.That(allUsers.Count, Is.EqualTo(1));

                    allUsers = await authRepo.SearchUserAuthsAsync(query:"Test",skip:1,take:1,orderBy:nameof(UserAuth.Email));
                    Assert.That(allUsers.Count, Is.EqualTo(1));
                    Assert.That(allUsers[0].Email, Is.EqualTo("user@gmail.com"));
                }
            }
        }
    }
}    
