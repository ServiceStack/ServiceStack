using System;
using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    public class LogTest
    {
        [AutoIncrement]
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public string Name { get; set; }
    }

    [TestFixtureOrmLite, NUnit.Framework.Ignore("Initializing LogFactory needs to run stand-alone")]
    public class LoggingTests : OrmLiteProvidersTestBase
    {
        public LoggingTests(DialectContext context) : base(context) {}

        [NUnit.Framework.Ignore(""), Test]
        public void Does_log_all_statements()
        {
            var sbLogFactory = new StringBuilderLogFactory();
            var testLogger = TestLogger.GetLogs().Count;

            using var db = OpenDbConnection();
            db.DropTable<LogTest>();
            db.CreateTable<LogTest>();

            db.Insert(new LogTest
            {
                CustomerId = 2,
                Name = "Foo"
            });

            var test = db.Single<LogTest>(x => x.CustomerId == 2);

            test.Name = "Bar";

            db.Update(test);

            test = db.Single<LogTest>(x => x.CustomerId == 2);

            db.DeleteById<LogTest>(test.Id);

            var logs = sbLogFactory.GetLogs();
            logs.Print();

            //var logs = TestLogger.GetLogs();

            Assert.That(logs, Does.Contain("CREATE TABLE"));
            Assert.That(logs, Does.Contain("INSERT INTO"));
            Assert.That(logs, Does.Contain("SELECT"));
            Assert.That(logs, Does.Contain("UPDATE"));
            Assert.That(logs, Does.Contain("DELETE FROM"));
        }

        [NUnit.Framework.Ignore(""), Test]
        public void Can_handle_sql_exceptions()
        {
            string lastSql = null;
            IDbDataParameter lastParam = null;
            OrmLiteConfig.ExceptionFilter = (dbCmd, ex) =>
            {
                lastSql = dbCmd.CommandText;
                lastParam = (IDbDataParameter)dbCmd.Parameters[0];
            };

            using var db = OpenDbConnection();
            db.DropAndCreateTable<LogTest>();

            try
            {
                var results = db.Select<LogTest>("Unknown = @arg", new { arg = "foo" });
                Assert.Fail("Should throw");
            }
            catch (Exception)
            {
                Assert.That(lastSql, Does.Contain("Unknown"));
                Assert.That(lastParam.Value, Is.EqualTo("foo"));
            }
        }
    }
}