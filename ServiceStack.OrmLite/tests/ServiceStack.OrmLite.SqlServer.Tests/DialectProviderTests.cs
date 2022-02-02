using System.Configuration;
using System.Data;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.SqlServerTests
{
    public class DialectProviderTests
    {
        public string ConnectionString => OrmLiteTestBase.GetConnectionString();

        public void DbPocoTest(IDbConnection db)
        {
            db.DropAndCreateTable<Poco>();
            db.Insert(new Poco {Id = 1, Name = "foo"});
            var row = db.SingleById<Poco>(1);
            Assert.That(row.Name, Is.EqualTo("foo"));
        }

        [Test]
        public void Can_use_SqlServerDialectProvider()
        {
            var dbFactory = new OrmLiteConnectionFactory(ConnectionString, SqlServerDialect.Provider);

            using (var db = dbFactory.Open())
            {
                DbPocoTest(db);
            }
        }

        [Test]
        public void Can_use_SqlServer2012DialectProvider()
        {
            var dbFactory = new OrmLiteConnectionFactory(ConnectionString, SqlServer2012Dialect.Provider);
            using (var db = dbFactory.Open())
            {
                DbPocoTest(db);
            }
        }

        [Test]
        public void Can_use_SqlServer2014DialectProvider()
        {
            var dbFactory = new OrmLiteConnectionFactory(ConnectionString, SqlServer2014Dialect.Provider);
            using (var db = dbFactory.Open())
            {
                DbPocoTest(db);
            }
        }

        [Test]
        public void Can_use_SqlServer2016DialectProvider()
        {
            var dbFactory = new OrmLiteConnectionFactory(ConnectionString, SqlServer2016Dialect.Provider);
            using (var db = dbFactory.Open())
            {
                DbPocoTest(db);
            }
        }

        [Test]
        public void Can_use_SqlServer2017DialectProvider()
        {
            var dbFactory = new OrmLiteConnectionFactory(ConnectionString, SqlServer2017Dialect.Provider);
            using (var db = dbFactory.Open())
            {
                DbPocoTest(db);
            }
        }

        [Test]
        public void Can_use_SqlServer2019DialectProvider()
        {
            var dbFactory = new OrmLiteConnectionFactory(ConnectionString, SqlServer2019Dialect.Provider);
            using (var db = dbFactory.Open())
            {
                DbPocoTest(db);
            }
        }
    }
}