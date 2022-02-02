using System;
using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Converters;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixture]
    public class InsertTests
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            //OrmLiteConfig.UseParameterizeSqlExpressions = false;  //Removed in OrmLite in future
            OrmLiteConfig.DialectProvider = new SqliteOrmLiteDialectProvider();
            DateTimeConverter dates = OrmLiteConfig.DialectProvider.GetDateTimeConverter();
            dates.DateStyle = DateTimeKind.Utc;
        }

        public class User
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public string Value { get; set; }
            public bool Bool { get; set; }

        }

        [Test]
        public void InsertValues_values_via_insertonly()
        {
            using (IDbConnection db = ":memory:".OpenDbConnection())
            {
                db.CreateTable<User>(true);

                var utcDate = DateTime.UtcNow;

                db.InsertOnly(new User { Id = 1, Date = utcDate, Bool = true}, new string[0]);
                db.InsertOnly(new User { Id = 2, Date = utcDate.AddDays(-1) }, new string[0]);
                db.InsertOnly(new User { Id = 3, Date = utcDate.AddDays(-2) }, new string[0]);
                db.Insert(new User { Id = 4, Date = utcDate.AddDays(1), Bool = true});

                var actualCount = db.Count<User>(u => u.Bool);

                Assert.That(actualCount, Is.EqualTo(2));
            }
        }
    }
}