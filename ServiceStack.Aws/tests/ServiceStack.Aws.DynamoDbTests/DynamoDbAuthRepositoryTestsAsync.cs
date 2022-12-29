using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class DynamoDbAuthRepositoryTestsAsync : DynamoTestBase
    {
        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            CleanUp();

            appHost = new BasicAppHost {
                    ConfigureContainer = c => {
                        var db = CreatePocoDynamo();
                        c.AddSingleton(db);
                        c.AddSingleton((IAuthRepository)CreateAuthRepo(db));
                        
                        var authRepo = c.Resolve<IAuthRepository>();
                    
                        if (authRepo is IClearable clearable)
                        {
                            try { clearable.Clear(); } catch {}
                        }

                        authRepo.InitSchema();
                    }
                }
                .Init();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();

            CleanUp();
        }

        void CleanUp()
        {
            DynamoMetadata.Reset();
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
        }

        IUserAuthRepositoryAsync CreateAuthRepo(IPocoDynamo db)
        {
            var authRepo = new DynamoDbAuthRepository(db);
            authRepo.InitSchema();
            return authRepo;
        }

        [Test]
        public async Task Does_create_Auth_Tables()
        {
            var db = appHost.Resolve<IPocoDynamo>();

            Assert.That(await db.GetTableNamesAsync(), Is.EquivalentTo(new[] {
                nameof(ApiKey),
                nameof(Seq),
                nameof(UserAuth),
                nameof(UserAuthDetails),
                nameof(UserAuthRole),
            }));

            var userAuth = AssertTable(db, typeof(UserAuth), "Id");
            AssertIndex(userAuth.GlobalIndexes[0], "UsernameUserAuthIndex", "UserName", "Id");

            var userAuthDetails = AssertTable(db, typeof(UserAuthDetails), "UserAuthId", "Id");
            AssertIndex(userAuthDetails.GlobalIndexes[0], "UserIdUserAuthDetailsIndex", "UserId", "Provider");

            AssertTable(db, typeof(UserAuthRole), "UserAuthId", "Id");
        }

        [Test]
        public async Task Can_Create_UserAuth()
        {
            var db = appHost.Resolve<IPocoDynamo>();
            var authRepo = appHost.GetAuthRepositoryAsync();

            var user1 = new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                Email = "demis.bellot@gmail.com",
            };
            await authRepo.CreateUserAuthAsync(user1, "test");
            Assert.That(user1.Id, Is.GreaterThan(0));

            var user2 = new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                UserName = "mythz",
            };

            await authRepo.CreateUserAuthAsync(user2, "test");
            Assert.That(user2.Id, Is.GreaterThan(0));

            var dbUser1 = db.GetItem<UserAuth>(user1.Id);
            Assert.That(dbUser1.Email, Is.EqualTo(user1.Email));
            dbUser1 = (UserAuth)await authRepo.GetUserAuthAsync(user1.Id.ToString());
            Assert.That(dbUser1.Email, Is.EqualTo(user1.Email));
            Assert.That(dbUser1.UserName, Is.Null);

            var dbUser2 = db.GetItem<UserAuth>(user2.Id);
            Assert.That(dbUser2.UserName, Is.EqualTo(user2.UserName));
            dbUser2 = (UserAuth)await authRepo.GetUserAuthAsync(user2.Id.ToString());
            Assert.That(dbUser2.UserName, Is.EqualTo(user2.UserName));
            Assert.That(dbUser2.Email, Is.Null);
        }

        [Test]
        public async Task Can_GetUserAuthByUserName_Username()
        {
            var authRepo = appHost.GetAuthRepositoryAsync();

            var dbUser = await authRepo.GetUserAuthByUserNameAsync("testusername");
            Assert.That(dbUser, Is.Null);

            await authRepo.CreateUserAuthAsync(new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                Email = "testusername",
            }, "test");

            dbUser = await authRepo.GetUserAuthByUserNameAsync("testusername");
            Assert.That(dbUser.FullName, Is.EqualTo("First Last"));
        }

        [Test]
        public async Task Can_GetUserAuthByUserName_Email()
        {
            var authRepo = appHost.GetAuthRepositoryAsync();

            var dbUser = await authRepo.GetUserAuthByUserNameAsync("testemail@gmail.com");
            Assert.That(dbUser, Is.Null);

            await authRepo.CreateUserAuthAsync(new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                Email = "testemail@gmail.com",
            }, "test");

            dbUser = await authRepo.GetUserAuthByUserNameAsync("testemail@gmail.com");
            Assert.That(dbUser.FullName, Is.EqualTo("First Last"));
        }

        [Test]
        public async Task Can_put_UserAuthRole()
        {
            var db = CreatePocoDynamo();

            await db.PutItemAsync(new UserAuthRole
            {
                UserAuthId = 1,
                Id = 2,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            });

            var userAuthRole = await db.GetItemAsync<UserAuthRole>(1, 2);
            Assert.That(userAuthRole, Is.Not.Null);

            userAuthRole.PrintDump();
        }

        [Test]
        public async Task Can_Assign_and_Unassign_Roles()
        {
            var authRepo = (DynamoDbAuthRepository)appHost.GetAuthRepositoryAsync();

            var userAuth = await authRepo.CreateUserAuthAsync(new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                Email = "testrole@gmail.com",
            }, "test");

            Assert.That(!await authRepo.HasRoleAsync(userAuth.Id.ToString(), "TheRole"));
            Assert.That(!await authRepo.HasPermissionAsync(userAuth.Id.ToString(), "ThePermission"));

            await authRepo.AssignRolesAsync(userAuth.Id.ToString(),
                roles: new[] { "TheRole" });

            Assert.That(await authRepo.HasRoleAsync(userAuth.Id.ToString(), "TheRole"));

            await authRepo.AssignRolesAsync(userAuth.Id.ToString(),
                permissions: new[] { "ThePermission" });

            Assert.That(await authRepo.HasPermissionAsync(userAuth.Id.ToString(), "ThePermission"));

            var dbPermissions = (await authRepo.GetPermissionsAsync(userAuth.Id.ToString())).ToList();
            Assert.That(dbPermissions[0], Is.EqualTo("ThePermission"));
            var dbRoles = (await authRepo.GetRolesAsync(userAuth.Id.ToString())).ToList();
            Assert.That(dbRoles[0], Is.EqualTo("TheRole"));

            await authRepo.UnAssignRolesAsync(userAuth.Id.ToString(), roles: new[] { "TheRole" });
            Assert.That(!await authRepo.HasRoleAsync(userAuth.Id.ToString(), "TheRole"));

            await authRepo.UnAssignRolesAsync(userAuth.Id.ToString(), permissions: new[] { "ThePermission" });
            Assert.That(!await authRepo.HasPermissionAsync(userAuth.Id.ToString(), "ThePermission"));
        }

        [Test]
        public async Task Does_clear_all_roles()
        {
            var db = appHost.Resolve<IPocoDynamo>();
            var authRepo = (DynamoDbAuthRepository)appHost.GetAuthRepositoryAsync();

            var userAuth = await authRepo.CreateUserAuthAsync(new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                Email = "testrole2@gmail.com",
            }, "test");

            await authRepo.AssignRolesAsync(userAuth.Id.ToString(),
                roles: new[] { "TheRole" });

            await authRepo.AssignRolesAsync(userAuth.Id.ToString(),
                permissions: new[] { "ThePermission" });

            var userAuths = await db.ScanAllAsync<UserAuth>().ToListAsync();
            Assert.That(userAuths.Count, Is.GreaterThan(0));

            var userRoles = await db.ScanAllAsync<UserAuthRole>().ToListAsync();
            Assert.That(userRoles.Count, Is.GreaterThan(0));

            await authRepo.ClearAsync();

            userAuths = await db.ScanAllAsync<UserAuth>().ToListAsync();
            Assert.That(userAuths.Count, Is.EqualTo(0));

            userRoles = await db.ScanAllAsync<UserAuthRole>().ToListAsync();
            Assert.That(userRoles.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task Can_AddUserAuthDetails()
        {
            var db = appHost.Resolve<IPocoDynamo>();
            var authRepo = appHost.GetAuthRepositoryAsync();
            
            var newUser = await authRepo.CreateUserAuthAsync(new AppUser
            {
                DisplayName = "Facebook User",
                Email = "user@fb.com",
                FirstName = "Face",
                LastName = "Book",
            }, "p@55wOrd");
            
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
}