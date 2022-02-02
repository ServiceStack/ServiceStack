using System;
using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Converters;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite.Tests.Issues
{
    // also see ServiceStack.OrmLite.Tests.DateTimeTests, running that test against Sqlite should
    // expose the same problem?

    // ***************************************************************************
    // Failing Tests - 4.0.50 and 4.0.52

    [TestFixture]
    public class When_DateTimeConverter_DateStyle_is_Utc
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            OrmLiteConfig.DialectProvider = new SqliteOrmLiteDialectProvider();
            DateTimeConverter dates = OrmLiteConfig.DialectProvider.GetDateTimeConverter();
            dates.DateStyle = DateTimeKind.Utc;
        }

        public class User
        {
            public int Id { get; set; }

            public DateTime Date { get; set; }
        }

        [Test]
        public void Insert_and_select_utc_DateTime()
        {
            using (IDbConnection db = ":memory:".OpenDbConnection())
            {
                db.CreateTable<User>(true);

                var utcDate = DateTime.UtcNow;

                db.Insert(new User { Id = 1, Date = utcDate });

                var actual = db.SingleById<User>(1);

                Assert.AreEqual(actual.Date.Kind, DateTimeKind.Utc); // passes
                Assert.AreEqual(actual.Date, utcDate);  // fails
            }
        }

        [Test]
        public void Insert_and_select_local_DateTime()
        {
            using (IDbConnection db = ":memory:".OpenDbConnection())
            {
                db.CreateTable<User>(true);

                var localDate = DateTime.Now;

                db.Insert(new User { Id = 2, Date = localDate });

                var actual = db.SingleById<User>(2);
                var utcDate = localDate.ToUniversalTime();

                Assert.AreEqual(actual.Date.Kind, DateTimeKind.Utc); // passes
                Assert.AreEqual(actual.Date, utcDate); // fails
            }
        }
    }

    // ***************************************************************************
    // Failing Tests, but only if UseParameterizeSqlExpressions is false - 4.0.52

    [TestFixture]
    public class _DateTimeQueryTests
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
        }

        [Test]
        public void Count_using_utc_DateTime_in_where_clause()
        {
            using (IDbConnection db = ":memory:".OpenDbConnection())
            {
                db.CreateTable<User>(true);

                var utcDate = DateTime.UtcNow;

                db.Insert(new User { Id = 1, Date = utcDate });
                db.Insert(new User { Id = 2, Date = utcDate.AddDays(-1) });
                db.Insert(new User { Id = 3, Date = utcDate.AddDays(-2) });
                db.Insert(new User { Id = 4, Date = utcDate.AddDays(1) });

                var actualCount = db.Count<User>(u => u.Date <= utcDate);

                Assert.That(actualCount, Is.EqualTo(3));
            }
        }

        [Test]
        public void Count_using_local_DateTime_in_where_clause()
        {
            using (IDbConnection db = ":memory:".OpenDbConnection())
            {
                db.CreateTable<User>(true);

                var localDate = DateTime.Now;

                db.Insert(new User { Id = 1, Date = localDate });
                db.Insert(new User { Id = 2, Date = localDate.AddDays(-1) });
                db.Insert(new User { Id = 3, Date = localDate.AddDays(-2) });
                db.Insert(new User { Id = 4, Date = localDate.AddDays(1) });

                var utcDate = localDate.ToUniversalTime();
                var actualCount = db.Count<User>(u => u.Date <= localDate);

                Assert.That(actualCount, Is.EqualTo(3));
            }
        }
    }

    // ***************************************************************************
    // Passing Tests: 4.0.50 and 4.0.52

    [TestFixture]
    public class When_DateTimeConverter_DateStyle_is_default
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            OrmLiteConfig.DialectProvider = new SqliteOrmLiteDialectProvider();
            DateTimeConverter dates = OrmLiteConfig.DialectProvider.GetDateTimeConverter();
            dates.DateStyle = DateTimeKind.Unspecified;  // OR:
            //dates.DateStyle = DateTimeKind.Local;
        }

        public class User
        {
            public int Id { get; set; }

            public DateTime Date { get; set; }
        }

        [Test]
        public void Insert_and_select_utc_DateTime()
        {
            using (IDbConnection db = ":memory:".OpenDbConnection())
            {
                db.CreateTable<User>(true);

                var utcDate = DateTime.UtcNow;
                var localDate = utcDate.ToLocalTime();

                db.Insert(new User { Id = 1, Date = utcDate });

                var actual = db.SingleById<User>(1);

                Assert.That(actual.Date.Kind, Is.EqualTo(DateTimeKind.Local).Or.EqualTo(DateTimeKind.Unspecified));
                Assert.That(actual.Date, Is.EqualTo(localDate));
            }
        }

        [Test]
        public void Insert_and_select_local_DateTime()
        {
            using (IDbConnection db = ":memory:".OpenDbConnection())
            {
                db.CreateTable<User>(true);

                var localDate = DateTime.Now;

                db.Insert(new User { Id = 2, Date = localDate });

                var actual = db.SingleById<User>(2);

                Assert.That(actual.Date.Kind, Is.EqualTo(DateTimeKind.Local).Or.EqualTo(DateTimeKind.Unspecified));
                Assert.That(actual.Date, Is.EqualTo(localDate));
            }
        }
    }
}