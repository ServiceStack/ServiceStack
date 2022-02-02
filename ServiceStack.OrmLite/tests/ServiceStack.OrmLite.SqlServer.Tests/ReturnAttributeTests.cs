using System.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.OrmLite.SqlServerTests;

namespace ReturnAttributeTests
{
    public class UserSequence
    {
        [Sequence("Gen_UserSequence_Id"), ReturnOnInsert]
        public int Id { get; set; }

        public string Name { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }

    public class UserSequence3
    {
        [Sequence("Gen_UserSequence_Id"), ReturnOnInsert]
        public int Id { get; set; }

        public string Name { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }

        [Sequence("Gen_Counter")]
        public int Counter { get; set; }

        [Sequence("Gen_Counter_Return"), ReturnOnInsert]
        public int CounterReturn { get; set; }
    }

    public class TestsBase
    {
        private OrmLiteConnectionFactory dbFactory;

        protected string ConnectionString { get; set; }
        protected IOrmLiteDialectProvider DialectProvider { get; set; }
        protected OrmLiteConnectionFactory DbFactory => dbFactory ??= new OrmLiteConnectionFactory(ConnectionString, DialectProvider);

        public TestsBase()
        {
        }

        protected void Init()
        {
            LogManager.LogFactory = new ConsoleLogFactory(debugEnabled:true);
        }

        public IDbConnection OpenDbConnection()
        {
            Init();
            return DbFactory.OpenDbConnection();
        }
    }

    public class ReturnAttributeTests : TestsBase
    {
        public ReturnAttributeTests()
        {
            ConnectionString = OrmLiteTestBase.GetConnectionString();
            DialectProvider = SqlServer2012Dialect.Provider;
        }

        [Test]
        public void Does_use_and_return_Sequence_on_Insert()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UserSequence>();

                var user = new UserSequence { Name = "me", Email = "me@mydomain.com" };
                user.UserName = user.Email;

                db.Insert(user);               
                Assert.That(user.Id, Is.GreaterThan(0), "normal Insert");
            }
        }

        [Test]
        public void Does_use_and_return_Sequence_on_Save()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UserSequence>();

                var user = new UserSequence { Name = "me", Email = "me@mydomain.com" };
                user.UserName = user.Email;

                db.Save(user);
                Assert.That(user.Id, Is.GreaterThan(0), "normal Insert");
            }
        }

        [Test]
        public void Can_use_3_Sequences_on_Insert()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UserSequence3>();

                var user = new UserSequence3 { Name = "me", Email = "me@mydomain.com" };
                user.UserName = user.Email;

                db.Insert(user);
                Assert.That(user.Id, Is.GreaterThan(0), "normal Insert");
                Assert.That(user.CounterReturn, Is.GreaterThan(0), "counter sequence ok");
                Assert.That(user.Counter, Is.EqualTo(0));

                var dbUser = db.SingleById<UserSequence3>(user.Id);
                Assert.That(dbUser.Counter, Is.GreaterThan(0));
            }
        }

        [Test]
        public void Does_generate_Sql_with_Sequence()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UserSequence>();

                var user = new UserSequence { Name = "me", Email = "me@mydomain.com" };
                user.UserName = user.Email;

                var id = db.Insert(user);
                var sql = db.GetLastSql();
                Assert.That(sql, Is.EqualTo("INSERT INTO \"UserSequence\" (\"Id\",\"Name\",\"UserName\",\"Email\") OUTPUT INSERTED.\"Id\" VALUES (NEXT VALUE FOR \"Gen_UserSequence_Id\",@Name,@UserName,@Email)"), "normal Insert");
            }
        }

        [Test]
        public void Does_drop_and_create_tables_with_sequences()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<UserSequence>();
                Assert.That(!db.TableExists<UserSequence>());

                db.CreateTable<UserSequence>();
                Assert.That(db.TableExists<UserSequence>());

                db.DropTable<UserSequence>();
                Assert.That(!db.TableExists<UserSequence>());

                db.CreateTable<UserSequence>();
                Assert.That(db.TableExists<UserSequence>());
            }
        }

    }
}
