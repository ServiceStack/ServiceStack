using System;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    [NonParallelizable]
    public class DateTimeTests : OrmLiteProvidersTestBase
    {
        public DateTimeTests(DialectContext context) : base(context) {}
        
        [Test]
        public void Can_insert_and_query_with_Unspecified_DateStyle()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<DateTimeObject>();

            DateTime dateTime;
            DateTimeObject x;

            dateTime = new DateTime(2012, 1, 1, 1, 1, 1, DateTimeKind.Local);
            x = InsertAndSelectDateTime(db, dateTime);
            Assert.AreEqual(x.Test, dateTime);
            Assert.AreEqual(x.Test, x.TestNullable.Value);
            x = db.Select<DateTimeObject>(d => d.Test == dateTime).FirstOrDefault();
            Assert.IsNotNull(x);

            dateTime = new DateTime(2012, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            x = InsertAndSelectDateTime(db, dateTime);
            Assert.AreEqual(x.Test, dateTime);
            Assert.AreEqual(x.Test, x.TestNullable.Value);
            x = db.Select<DateTimeObject>(d => d.Test == dateTime).FirstOrDefault();
            Assert.IsNotNull(x);

            dateTime = new DateTime(2012, 1, 1, 1, 1, 1, DateTimeKind.Unspecified);
            x = InsertAndSelectDateTime(db, dateTime);
            Assert.AreEqual(x.Test, dateTime);
            Assert.AreEqual(x.Test, x.TestNullable.Value);
            x = db.Select<DateTimeObject>(d => d.Test == dateTime).FirstOrDefault();
            Assert.IsNotNull(x);
        }

        [Test]
        public void Does_return_Local_Dates_with_Local_DateStyle()
        {
            using var db = OpenDbConnection();
            var dialectProvider = DialectProvider;
            var hold = dialectProvider.GetDateTimeConverter().DateStyle;
            dialectProvider.GetDateTimeConverter().DateStyle = DateTimeKind.Local;

            db.DropAndCreateTable<DateTimeObject>();

            DateTime dateTime;
            DateTimeObject x;

            dateTime = new DateTime(2012, 1, 1, 1, 1, 1, DateTimeKind.Local);
            x = InsertAndSelectDateTime(db, dateTime);
            Assert.AreEqual(DateTimeKind.Local, x.Test.Kind);
            Assert.AreEqual(DateTimeKind.Local, x.TestNullable.Value.Kind);
            Assert.AreEqual(x.Test, x.TestNullable.Value);
            Assert.AreEqual(x.Test.ToUniversalTime(), dateTime.ToUniversalTime());
            Assert.AreEqual(x.Test.ToLocalTime(), dateTime.ToLocalTime());
            x = db.Select<DateTimeObject>(d => d.Test == dateTime).FirstOrDefault();
            Assert.IsNotNull(x);

            dateTime = new DateTime(2012, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            x = InsertAndSelectDateTime(db, dateTime);
            Assert.AreEqual(DateTimeKind.Local, x.Test.Kind);
            Assert.AreEqual(DateTimeKind.Local, x.TestNullable.Value.Kind);
            Assert.AreEqual(x.Test, x.TestNullable.Value);
            Assert.AreEqual(x.Test.ToUniversalTime(), dateTime.ToUniversalTime());
            Assert.AreEqual(x.Test.ToLocalTime(), dateTime.ToLocalTime());
            x = db.Select<DateTimeObject>(d => d.Test == dateTime).FirstOrDefault();
            Assert.IsNotNull(x);

            dateTime = new DateTime(2012, 1, 1, 1, 1, 1, DateTimeKind.Unspecified);
            x = InsertAndSelectDateTime(db, dateTime);
            Assert.AreEqual(DateTimeKind.Local, x.Test.Kind);
            Assert.AreEqual(DateTimeKind.Local, x.TestNullable.Value.Kind);
            Assert.AreEqual(x.Test, x.TestNullable.Value);
            Assert.AreEqual(x.Test.ToUniversalTime(), dateTime);
            Assert.AreEqual(x.Test.ToLocalTime(), dateTime.ToLocalTime());
            x = db.Select<DateTimeObject>(d => d.Test == dateTime).FirstOrDefault();
            db.GetLastSql().Print();
            Assert.IsNotNull(x);

            dialectProvider.GetDateTimeConverter().DateStyle = hold;
        }

        [Test]
        public void Does_return_UTC_Dates_with_UTC_DateStyle()
        {
            using var db = OpenDbConnection();
            var dialectProvider = DialectProvider;
            var hold = dialectProvider.GetDateTimeConverter().DateStyle;
            dialectProvider.GetDateTimeConverter().DateStyle = DateTimeKind.Utc;

            db.DropAndCreateTable<DateTimeObject>();

            DateTime dateTime;
            DateTimeObject x;

            dateTime = new DateTime(2012, 1, 1, 1, 1, 1, DateTimeKind.Local);
            x = InsertAndSelectDateTime(db, dateTime);
            Assert.That(x.Test.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(x.TestNullable.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(x.Test, Is.EqualTo(x.TestNullable.Value));
            Assert.That(x.Test.ToUniversalTime(), Is.EqualTo(dateTime.ToUniversalTime()));
            Assert.That(x.Test.ToLocalTime(), Is.EqualTo(dateTime.ToLocalTime()));
            x = db.Select<DateTimeObject>(d => d.Test == dateTime).FirstOrDefault();
            Assert.That(x, Is.Not.Null);

            dateTime = new DateTime(2012, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            x = InsertAndSelectDateTime(db, dateTime);
            Assert.That(x.Test.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(x.TestNullable.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(x.Test, Is.EqualTo(x.TestNullable.Value));
            Assert.That(x.Test.ToUniversalTime(), Is.EqualTo(dateTime.ToUniversalTime()));
            Assert.That(x.Test.ToLocalTime(), Is.EqualTo(dateTime.ToLocalTime()));
            x = db.Select<DateTimeObject>(d => d.Test == dateTime).FirstOrDefault();
            Assert.That(x, Is.Not.Null);

            dateTime = new DateTime(2012, 1, 1, 1, 1, 1, DateTimeKind.Unspecified);
            x = InsertAndSelectDateTime(db, dateTime);
            Assert.That(x.Test.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(x.TestNullable.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(x.Test, Is.EqualTo(x.TestNullable.Value));
            Assert.That(x.Test.ToUniversalTime(), Is.EqualTo(dateTime));
            Assert.That(x.Test.ToLocalTime(), Is.EqualTo(dateTime.ToLocalTime()));
            x = db.Select<DateTimeObject>(d => d.Test == dateTime).FirstOrDefault();
            Assert.That(x, Is.Not.Null);

            dialectProvider.GetDateTimeConverter().DateStyle = hold;
        }

        [Test]
        public void Log_dialect_behavior()
        {
            Dialect.ToString().Print();

            using var db = OpenDbConnection();
            db.DropAndCreateTable<DateTimeObject>();

            var dateStyles = new[] { DateTimeKind.Local, DateTimeKind.Utc, DateTimeKind.Unspecified };

            foreach (var dateStyle in dateStyles)
            {
                db.DeleteAll<DateTimeObject>();

                var dateTime = new DateTime(2012, 1, 1, 1, 1, 1, dateStyle);

                "#1 IN: {0} ({1}), UTC: {2}, Local: {3}".Print(
                    dateTime.Kind,
                    dateTime,
                    dateTime.ToUniversalTime(),
                    dateTime.ToLocalTime());

                using (var cmd = db.OpenCommand())
                {
                    cmd.CommandText = "INSERT INTO {0} VALUES({1}, {2})"
                        .Fmt(nameof(DateTimeObject).SqlTable(DialectProvider),
                            DialectProvider.GetParam("p1"),
                            DialectProvider.GetParam("p2"));

                    cmd.Parameters.Add(cmd.CreateParam("p1", dateTime));
                    cmd.Parameters.Add(cmd.CreateParam("p2", dateTime));

                    cmd.ExecuteNonQuery();
                }

                using (var cmd = db.OpenCommand())
                {
                    cmd.CommandText = "SELECT * FROM {0}".Fmt(nameof(DateTimeObject).SqlTable(DialectProvider));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var dbDateTime = reader.GetDateTime(0);
                            "#1 IN: {0} ({1}), OUT: {2} ({3})".Print(
                                dateTime.Kind,
                                dateTime,
                                dbDateTime.Kind,
                                dbDateTime);
                        }
                    }
                }
            }
        }

        [Test]
        public void Can_Select_Date_with_SqlList()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<DateTimeObject>();

            var dateTime = new DateTime(2001, 1, 1, 1, 1, 1);
            db.DeleteAll<DateTimeObject>();
            db.Insert(new DateTimeObject { Test = dateTime, TestNullable = dateTime });

            var row = db.SqlList<DateTimeObject>(
                "SELECT * FROM {0} WHERE Test = @dateTime".Fmt("DateTimeObject".SqlTable(DialectProvider)), new { dateTime });
            Assert.That(dateTime, Is.EqualTo(row[0].Test));

            row = db.SqlList<DateTimeObject>(
                "SELECT * FROM {0} WHERE {1} = @dateTime".Fmt("DateTimeObject".SqlTable(DialectProvider), "TestNullable".SqlColumn(DialectProvider)), new { dateTime });
            Assert.That(dateTime, Is.EqualTo(row[0].TestNullable));

            DateTime? nullableDate = dateTime;
            row = db.SqlList<DateTimeObject>(
                "SELECT * FROM {0} WHERE {1} = @nullableDate".Fmt("DateTimeObject".SqlTable(DialectProvider), "TestNullable".SqlColumn(DialectProvider)), new { nullableDate });
            Assert.That(dateTime, Is.EqualTo(row[0].TestNullable));
        }

        private static DateTimeObject InsertAndSelectDateTime(IDbConnection db, DateTime dateTime)
        {
            db.DeleteAll<DateTimeObject>();
            db.Insert(new DateTimeObject { Test = dateTime, TestNullable = dateTime });
            var x = db.Select<DateTimeObject>().First();
            return x;
        }

        private class DateTimeObject
        {
            public DateTime Test { get; set; }
            public DateTime? TestNullable { get; set; }
        }
    }
}
