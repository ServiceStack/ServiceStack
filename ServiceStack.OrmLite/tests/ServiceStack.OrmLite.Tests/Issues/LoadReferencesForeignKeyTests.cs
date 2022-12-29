using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    public class Root
    {
        [PrimaryKey]
        public int RootId { get; set; }

        [Reference]
        public List<RootItem> Items { get; set; }
    }

    public class RootItem
    {
        [PrimaryKey]
        public int RootItemId { get; set; }

        public int RootId { get; set; } //`{Parent}Id` convention to refer to Client

        public string MyValue { get; set; }
    }

    [TestFixtureOrmLite]
    public class LoadReferencesForeignKeyTests : OrmLiteProvidersTestBase
    {
        public LoadReferencesForeignKeyTests(DialectContext context) : base(context) {}

        [Test]
        public void Does_populate_Ref_Ids_of_non_convention_PrimaryKey_Tables()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Root>();
                db.DropAndCreateTable<RootItem>();

                var root = new Root {
                    RootId = 1,
                    Items = new List<RootItem> {
                        new RootItem { RootItemId = 2, MyValue = "x" }
                    }
                };

                db.Save(root, references: true);

                Assert.That(root.Items[0].RootId, Is.EqualTo(root.RootId));
            }
        }
        
        [Alias("Users")]
        public class User
        {
            [AutoId]
            public Guid Id { get; set; }

            [Reference]
            public List<UserBranch> Branches { get; set; }

            [Reference]
            public UserMeta Meta { get; set; }

            [Reference]
            public List<UserAddress> Addresses { get; set; }
        }

        [Alias("UserMetas")]
        public class UserMeta
        {
            [PrimaryKey]
            [ForeignKey(typeof(User), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
            [References(typeof(User))]
            public Guid UserId { get; set; }
        }

        [Alias("UserBranches")]
        public class UserBranch
        {
            [AutoId]
            public Guid Id { get; set; }

            [ForeignKey(typeof(User), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
            public Guid UserId { get; set; }

            public string Details { get; set; }
        }

        [Alias("UserAddresses")]
        public class UserAddress
        {
            [AutoId]
            public Guid Id { get; set; }

            [ForeignKey(typeof(User), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
            public Guid UserId { get; set; }

            public string Details { get; set; }
        }

        private static void InitDb(IDbConnection db)
        {
            db.DropTable<UserMeta>();
            db.DropTable<UserAddress>();
            db.DropTable<UserBranch>();
            db.DropTable<User>();

            db.CreateTable<User>();
            db.CreateTable<UserBranch>();
            db.CreateTable<UserAddress>();
            db.CreateTable<UserMeta>();
        }

        [Test]
        public void Can_create_tables_with_multiple_references()
        {
            using (var db = OpenDbConnection())
            {
                InitDb(db);
            }
            
            var userMeta = new UserMeta();
            var user = new User
            {
                Meta = userMeta
            };

            using (var db = OpenDbConnection())
            {
                user.Branches = new List<UserBranch> { new() { UserId = user.Id }};
                user.Addresses = new List<UserAddress> { new() { UserId = user.Id }};

                db.Save(user, references: true);

                var fromDb = db.LoadSingleById<User>(user.Id);
                fromDb.Dump().Print();
            }
        }

        [Test]
        public async Task Can_create_tables_with_multiple_references_async()
        {
            using (var db = await OpenDbConnectionAsync())
            {
                InitDb(db);
            }
            
            var userMeta = new UserMeta();
            var user = new User
            {
                Meta = userMeta
            };

            using (var db = await OpenDbConnectionAsync())
            {
                user.Branches = new List<UserBranch> { new() { UserId = user.Id }};
                user.Addresses = new List<UserAddress> { new() { UserId = user.Id }};

                await db.SaveAsync(user, references: true);

                var fromDb = await db.LoadSingleByIdAsync<User>(user.Id);
                fromDb.Dump().Print();
            }
        }
    }
}