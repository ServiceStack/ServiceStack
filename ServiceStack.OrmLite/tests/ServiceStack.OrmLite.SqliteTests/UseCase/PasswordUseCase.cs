namespace ServiceStack.OrmLite.Tests.UseCase
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using NUnit.Framework;
    using DataAnnotations;
    using Sqlite;

    [TestFixture]
    public class PasswordUseCase
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            //Inject your database provider here
            //OrmLiteConfig.DialectProvider = new SqliteOrmLiteDialectProvider();
        }

        public class User
        {
            public long Id { get; set; }

            [Index]
            public string Name { get; set; }

            public DateTime CreatedDate { get; set; }
        }

        [Alias("Users")]
        public class User2
        {
            [AutoIncrement]
            public long Id { get; set; }

            public long Value { get; set; }
        }

        [Test]
#if NETCORE
        [NUnit.Framework.Ignore("Microsoft.Data.Sqlite provider does not support `password` keyword")]
#endif
        public void Simple_CRUD_example()
        {
            var path = Config.SqliteFileDb;
            if (File.Exists(path))
                File.Delete(path);

            var connectionFactory = new OrmLiteConnectionFactory(path, SqliteDialect.Provider.Configure(password: "bob"));
            using (var db = connectionFactory.OpenDbConnection())
            {
                db.CreateTable<User>(true);

                db.Insert(new User { Id = 1, Name = "A", CreatedDate = DateTime.Now });
                db.Insert(new User { Id = 2, Name = "B", CreatedDate = DateTime.Now });
                db.Insert(new User { Id = 3, Name = "B", CreatedDate = DateTime.Now });

                var rowsB = db.Select<User>("Name = @name", new { name = "B" });
                var rowsB1 = db.Select<User>(user => user.Name == "B");

                Assert.That(rowsB, Has.Count.EqualTo(2));
                Assert.That(rowsB1, Has.Count.EqualTo(2));

                var rowIds = rowsB.ConvertAll(x => x.Id);
                Assert.That(rowIds, Is.EquivalentTo(new List<long> { 2, 3 }));

                rowsB.ForEach(x => db.Delete(x));

                rowsB = db.Select<User>("Name = @name", new { name = "B" });
                Assert.That(rowsB, Has.Count.EqualTo(0));

                var rowsLeft = db.Select<User>();
                Assert.That(rowsLeft, Has.Count.EqualTo(1));

                Assert.That(rowsLeft[0].Name, Is.EqualTo("A"));
            }
            File.Delete(path);

        }
    }
}