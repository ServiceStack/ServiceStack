using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.SqlServer;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.SqlServerTests.UseCase
{
    [TestFixture, NUnit.Framework.Ignore("")]
    public class SimpleUseCase
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            //Inject your database provider here
            OrmLiteConfig.DialectProvider = new SqlServerOrmLiteDialectProvider();
        }

        public class User
        {
            public long Id { get; set; }

            [Index]
            public string Name { get; set; }

            public DateTime CreatedDate { get; set; }

            public bool IsAdmin { get; set; }
        }

        public class Dual
        {
            [AutoIncrement]
            public int Id { get; set; }

            public string Name { get; set; }
        }

        [Test]
        public void Simple_CRUD_example()
        {
            //using (IDbConnection db = ":memory:".OpenDbConnection())

            var connStr = ConfigurationManager.ConnectionStrings["TestDb"].ConnectionString;
            var sqlServerFactory = new OrmLiteConnectionFactory(connStr, SqlServerOrmLiteDialectProvider.Instance);

            using (IDbConnection db = sqlServerFactory.OpenDbConnection())
            {
                db.CreateTable<Dual>(true);
                db.CreateTable<User>(true);

                db.Insert(new User { Id = 1, Name = "A", CreatedDate = DateTime.Now });
                db.Insert(new User { Id = 2, Name = "B", CreatedDate = DateTime.Now });
                db.Insert(new User { Id = 3, Name = "B", CreatedDate = DateTime.Now, IsAdmin = true});

                db.Insert(new Dual { Name = "Dual" });
                var lastInsertId = db.LastInsertId();
                Assert.That(lastInsertId, Is.GreaterThan(0));

                var rowsB = db.Select<User>("Name = @name", new { name = "B" });

                Assert.That(rowsB, Has.Count.EqualTo(2));

                var admin = db.Select<User>("IsAdmin = @isAdmin", new { isAdmin = true });
                Assert.That(admin[0].Id, Is.EqualTo(3));

                var rowIds = rowsB.ConvertAll(x => x.Id);
                Assert.That(rowIds, Is.EquivalentTo(new List<long> { 2, 3 }));

                rowsB.ForEach(x => db.Delete(x));

                rowsB = db.Select<User>("Name = @name", new { name = "B" });
                Assert.That(rowsB, Has.Count.EqualTo(0));

                var rowsLeft = db.Select<User>();
                Assert.That(rowsLeft, Has.Count.EqualTo(1));

                Assert.That(rowsLeft[0].Name, Is.EqualTo("A"));
            }
        }

    }

}