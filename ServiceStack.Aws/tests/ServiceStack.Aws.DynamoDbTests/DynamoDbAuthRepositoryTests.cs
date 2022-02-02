using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class DynamoDbAuthRepositoryTests : DynamoTestBase
    {
        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            CleanUp();

            appHost = new BasicAppHost()
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

        IUserAuthRepository CreateAuthRepo(IPocoDynamo db)
        {
            var authRepo = new DynamoDbAuthRepository(db);
            authRepo.InitSchema();
            return authRepo;
        }

        [Test]
        public void Does_create_Auth_Tables()
        {
            var db = CreatePocoDynamo();
            var authRepo = CreateAuthRepo(db);
            authRepo.InitSchema();

            db.GetTableNames().PrintDump();

            Assert.That(db.GetTableNames(), Is.EquivalentTo(new[] {
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
        public void Can_Create_UserAuth()
        {
            var db = CreatePocoDynamo();
            var authRepo = CreateAuthRepo(db);

            var user1 = new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                Email = "demis.bellot@gmail.com",
            };
            authRepo.CreateUserAuth(user1, "test");
            Assert.That(user1.Id, Is.GreaterThan(0));

            var user2 = new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                UserName = "mythz",
            };

            authRepo.CreateUserAuth(user2, "test");
            Assert.That(user2.Id, Is.GreaterThan(0));

            var dbUser1 = db.GetItem<UserAuth>(user1.Id);
            Assert.That(dbUser1.Email, Is.EqualTo(user1.Email));
            dbUser1 = (UserAuth)authRepo.GetUserAuth(user1.Id);
            Assert.That(dbUser1.Email, Is.EqualTo(user1.Email));
            Assert.That(dbUser1.UserName, Is.Null);

            var dbUser2 = db.GetItem<UserAuth>(user2.Id);
            Assert.That(dbUser2.UserName, Is.EqualTo(user2.UserName));
            dbUser2 = (UserAuth)authRepo.GetUserAuth(user2.Id);
            Assert.That(dbUser2.UserName, Is.EqualTo(user2.UserName));
            Assert.That(dbUser2.Email, Is.Null);
        }

        [Test]
        public void Can_GetUserAuthByUserName_Username()
        {
            var db = CreatePocoDynamo();
            var authRepo = (DynamoDbAuthRepository)CreateAuthRepo(db);

            var dbUser = authRepo.GetUserAuthByUserName("testusername");
            Assert.That(dbUser, Is.Null);

            authRepo.CreateUserAuth(new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                Email = "testusername",
            }, "test");

            dbUser = authRepo.GetUserAuthByUserName("testusername");
            Assert.That(dbUser.FullName, Is.EqualTo("First Last"));
        }

        [Test]
        public void Can_GetUserAuthByUserName_Email()
        {
            var db = CreatePocoDynamo();
            var authRepo = (DynamoDbAuthRepository)CreateAuthRepo(db);

            var dbUser = authRepo.GetUserAuthByUserName("testemail@gmail.com");
            Assert.That(dbUser, Is.Null);

            authRepo.CreateUserAuth(new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                Email = "testemail@gmail.com",
            }, "test");

            dbUser = authRepo.GetUserAuthByUserName("testemail@gmail.com");
            Assert.That(dbUser.FullName, Is.EqualTo("First Last"));
        }

        [Test]
        public void Can_put_UserAuthRole()
        {
            var db = CreatePocoDynamo();
            var authRepo = (DynamoDbAuthRepository)CreateAuthRepo(db);

            db.PutItem(new UserAuthRole
            {
                UserAuthId = 1,
                Id = 2,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            });

            var userAuthRole = db.GetItem<UserAuthRole>(1, 2);
            Assert.That(userAuthRole, Is.Not.Null);

            userAuthRole.PrintDump();
        }

        [Test]
        public void Can_Assign_and_Unassign_Roles()
        {
            var db = CreatePocoDynamo();
            var authRepo = (DynamoDbAuthRepository)CreateAuthRepo(db);

            var userAuth = authRepo.CreateUserAuth(new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                Email = "testrole@gmail.com",
            }, "test");

            Assert.That(!authRepo.HasRole(userAuth.Id, "TheRole"));
            Assert.That(!authRepo.HasPermission(userAuth.Id, "ThePermission"));

            authRepo.AssignRoles(userAuth.Id.ToString(),
                roles: new[] { "TheRole" });

            Assert.That(authRepo.HasRole(userAuth.Id, "TheRole"));

            authRepo.AssignRoles(userAuth.Id.ToString(),
                permissions: new[] { "ThePermission" });

            Assert.That(authRepo.HasPermission(userAuth.Id, "ThePermission"));

            var dbPermissions = authRepo.GetPermissions(userAuth.Id).ToList();
            Assert.That(dbPermissions[0], Is.EqualTo("ThePermission"));
            var dbRoles = authRepo.GetRoles(userAuth.Id).ToList();
            Assert.That(dbRoles[0], Is.EqualTo("TheRole"));

            authRepo.UnAssignRoles(userAuth.Id, roles: new[] { "TheRole" });
            Assert.That(!authRepo.HasRole(userAuth.Id, "TheRole"));

            authRepo.UnAssignRoles(userAuth.Id, permissions: new[] { "ThePermission" });
            Assert.That(!authRepo.HasPermission(userAuth.Id, "ThePermission"));
        }

        [Test]
        public void Does_clear_all_roles()
        {
            var db = CreatePocoDynamo();
            var authRepo = (DynamoDbAuthRepository)CreateAuthRepo(db);

            var userAuth = authRepo.CreateUserAuth(new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                Email = "testrole2@gmail.com",
            }, "test");

            authRepo.AssignRoles(userAuth.Id.ToString(),
                roles: new[] { "TheRole" });

            authRepo.AssignRoles(userAuth.Id.ToString(),
                permissions: new[] { "ThePermission" });

            var userAuths = db.ScanAll<UserAuth>().ToList();
            Assert.That(userAuths.Count, Is.GreaterThan(0));

            var userRoles = db.ScanAll<UserAuthRole>().ToList();
            Assert.That(userRoles.Count, Is.GreaterThan(0));

            authRepo.Clear();

            userAuths = db.ScanAll<UserAuth>().ToList();
            Assert.That(userAuths.Count, Is.EqualTo(0));

            userRoles = db.ScanAll<UserAuthRole>().ToList();
            Assert.That(userRoles.Count, Is.EqualTo(0));
        }
    }
}