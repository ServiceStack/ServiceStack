using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class DynamoDbAuthRepositoryCustomTestsAsync : DynamoTestBase
    {
        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            DynamoMetadata.Reset();
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));

            appHost = new BasicAppHost()
                .Init();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        private IUserAuthRepositoryAsync CreateAuthRepo(IPocoDynamo db)
        {
            var authRepo = new DynamoDbAuthRepository<CustomUserAuth, CustomUserAuthDetails>(db);
            authRepo.InitSchema();
            return authRepo;
        }

        [Test]
        public async Task Does_create_Custom_Auth_Tables()
        {
            var db = CreatePocoDynamo();
            var authRepo = CreateAuthRepo(db);
            authRepo.InitSchema();

            Assert.That(await db.GetTableNamesAsync(), Is.EquivalentTo(new[] {
                nameof(ApiKey),
                nameof(Seq),
                nameof(CustomUserAuth),
                nameof(CustomUserAuthDetails),
                nameof(UserAuthRole)
            }));

            var userAuth = AssertTable(db, typeof(CustomUserAuth), "Id");
            AssertIndex(userAuth.GlobalIndexes[0], "UsernameUserAuthIndex", "UserName", "Id");

            var userAuthDetails = AssertTable(db, typeof(CustomUserAuthDetails), "UserAuthId", "Id");
            AssertIndex(userAuthDetails.GlobalIndexes[0], "UserIdUserAuthDetailsIndex", "UserId", "Provider");

            AssertTable(db, typeof(UserAuthRole), "UserAuthId", "Id");
        }

        [Test]
        public async Task Can_Create_CustomUserAuth()
        {
            var db = CreatePocoDynamo();
            var authRepo = CreateAuthRepo(db);
            authRepo.InitSchema();

            var user1 = new CustomUserAuth
            {
                Custom = "CustomUserAuth",
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                Email = "demis.bellot@gmail.com",
            };
            await authRepo.CreateUserAuthAsync(user1, "test");
            Assert.That(user1.Id, Is.GreaterThan(0));

            var user2 = new CustomUserAuth
            {
                Custom = "CustomUserAuth",
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                UserName = "mythz",
            };
            await authRepo.CreateUserAuthAsync(user2, "test");
            Assert.That(user2.Id, Is.GreaterThan(0));

            var dbUser1 = db.GetItem<CustomUserAuth>(user1.Id);
            Assert.That(dbUser1.Email, Is.EqualTo(user1.Email));
            dbUser1 = (CustomUserAuth)await authRepo.GetUserAuthAsync(user1.Id.ToString());
            Assert.That(dbUser1.Email, Is.EqualTo(user1.Email));
            Assert.That(dbUser1.UserName, Is.Null);

            var dbUser2 = db.GetItem<CustomUserAuth>(user2.Id);
            Assert.That(dbUser2.UserName, Is.EqualTo(user2.UserName));
            dbUser2 = (CustomUserAuth)await authRepo.GetUserAuthAsync(user2.Id.ToString());
            Assert.That(dbUser2.UserName, Is.EqualTo(user2.UserName));
            Assert.That(dbUser2.Email, Is.Null);
        }
    }
}