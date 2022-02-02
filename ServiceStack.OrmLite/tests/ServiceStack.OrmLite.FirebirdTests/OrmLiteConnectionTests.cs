using System;
using System.IO;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.FirebirdTests
{
    [TestFixture]
    public class OrmLiteConnectionTests 
        : OrmLiteTestBase
    {
        [Test]//[Ignore("")]
        public void Can_create_connection_to_blank_database()
        {
            var connString =
                $"User=SYSDBA;Password=masterkey;Database=/ormlite-tests/ormlite-tests.fdb;DataSource=localhost;Dialect=3;charset=utf8;";
            using (var db = connString.OpenDbConnection())
            {
            }
        }

        [Test]
        public void Can_create_connection()
        {
            using (var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open())
            {
            }
        }

        [Test]
        public void Can_create_ReadOnly_connection()
        {
            using (var db = ConnectionString.OpenReadOnlyDbConnection())
            {
            }
        }

        [Test][Ignore("")]
        public void Can_create_table_with_ReadOnly_connection()
        {
            using (var db = ConnectionString.OpenReadOnlyDbConnection())
            {
                try
                {
                    db.CreateTable<ModelWithIdAndName>(true);
                    db.Insert(new ModelWithIdAndName(0));
                }
                catch (Exception ex)
                {
                    Log(ex.Message);
                    return;
                }
                Assert.Fail("Should not be able to create a table with a readonly connection");
            }
        }

        [Test]
        public void Can_open_two_ReadOnlyConnections_to_same_database()
        {
            var db = ConnectionString.OpenReadOnlyDbConnection();
            db.CreateTable<ModelWithIdAndName>(true);
            db.Insert(new ModelWithIdAndName(1));

            var dbReadOnly = ConnectionString.OpenReadOnlyDbConnection();
            dbReadOnly.Insert(new ModelWithIdAndName(2));
            var rows = dbReadOnly.Select<ModelWithIdAndName>();
            Assert.That(rows, Has.Count.EqualTo(2));

            dbReadOnly.Dispose();
            db.Dispose();
        }

    }
}