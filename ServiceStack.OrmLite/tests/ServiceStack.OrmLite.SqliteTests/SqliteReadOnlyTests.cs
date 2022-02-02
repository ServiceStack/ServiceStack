using System;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqliteTests
{
    public class DisableWrites
    {
        public int Id { get; set; }
    }

    class SqliteReadOnlyTests : OrmLiteTestBase
    {
        [Test]
        public void Can_open_readonly_connection_to_file_database()
        {
            var dbPath = "~/App_Data/northwind.sqlite".MapProjectPlatformPath();
            var connectionString = $"Data Source={dbPath};Read Only=true";

            connectionString.Print();
            var dbFactory = new OrmLiteConnectionFactory(connectionString, SqliteDialect.Provider);

            using (var db = dbFactory.OpenDbConnection())
            {
                var count = db.Count<Customer>();
                Assert.That(count, Is.GreaterThan(1));

                try
                {
                    db.DropAndCreateTable<DisableWrites>();
                    Assert.Fail("should thow");
                }
                catch (Exception ex)
                {
                    Assert.That(ex.Message, Does.Contain("attempt to write a readonly database"));
                }
            }
        }

    }
}
