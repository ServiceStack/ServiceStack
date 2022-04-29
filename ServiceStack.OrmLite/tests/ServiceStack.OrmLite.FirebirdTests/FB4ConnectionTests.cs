using System;
using System.IO;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.OrmLite.Firebird;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.FirebirdTests
{
    [TestFixture]
    public class FB4ConnectionTests 
        : OrmLiteTestBase
    {
        protected override string GetFileConnectionString() => FirebirdDb.V4Connection;
        protected override IOrmLiteDialectProvider GetDialectProvider() => Firebird4OrmLiteDialectProvider.Instance;
        
        [Test]//[Ignore("")]
        public void Can_create_connection_to_blank_database()
        {
            var connString =
                $"User=SYSDBA;Password=masterkey;Database=C:\\ORMLITE-TESTS\\FIREBIRD\\TEST.fdb;DataSource=127.0.0.1;Dialect=3;charset=utf8;";
            using (var db = connString.OpenDbConnection())
            {
            }
        }

        [Test]
        public void Can_connect_to_database()
        {
            using (var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection())
            {
            }
        }

        [Test]
        public void Can_create_connection()
        {
            using (var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).CreateDbConnection())
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