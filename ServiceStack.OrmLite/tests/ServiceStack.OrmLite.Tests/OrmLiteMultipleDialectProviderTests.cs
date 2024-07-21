using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixture]
[NonParallelizable]
public class OrmLiteMultipleDialectProviderTests
{
    public class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    [Test]
    public void Can_open_multiple_dialectprovider_with_execfilter() {
        //global
        OrmLiteConfig.DialectProvider = new SqliteOrmLiteDialectProvider();
        var factory = new OrmLiteConnectionFactory(SqliteDb.MemoryConnection);

        var sqlServerDialectProvider = SqlServerDialect.Provider;
        sqlServerDialectProvider.ExecFilter = new MockExecFilter1();
        factory.RegisterConnection("sqlserver", $"{SqlServerDb.DefaultConnection};Connection Timeout=1", sqlServerDialectProvider);

        var sqliteDialectProvider = SqliteDialect.Provider;
        sqliteDialectProvider.ExecFilter = new MockExecFilter2();
        factory.RegisterConnection("sqlite-file", SqliteDb.FileConnection, sqliteDialectProvider);

        try
        {
            var results = new List<Person>();
            using (var db = factory.OpenDbConnection())
            {
                db.DropAndCreateTable<Person>();
                db.Insert(new Person {Id = 1, Name = "1) :memory:"});
                db.Insert(new Person {Id = 2, Name = "2) :memory:"});

                using (var db2 = factory.OpenDbConnection("sqlserver"))
                {
                    db2.CreateTable<Person>(true);
                    db2.Insert(new Person {Id = 3, Name = "3) Database1.mdf"});
                    db2.Insert(new Person {Id = 4, Name = "4) Database1.mdf"});

                    using (var db3 = factory.OpenDbConnection("sqlite-file"))
                    {
                        db3.CreateTable<Person>(true);
                        db3.Insert(new Person {Id = 5, Name = "5) db.sqlite"});
                        db3.Insert(new Person {Id = 6, Name = "6) db.sqlite"});

                        results.AddRange(db.Select<Person>());
                        results.AddRange(db2.Select<Person>());
                        results.AddRange(db3.Select<Person>());

                        Assert.AreEqual(db.GetLastSql(), "SELECT \"Id\", \"Name\" FROM \"Person\"");
                        Assert.AreEqual(db2.GetLastSql(), "MockExecFilter1");
                        Assert.AreEqual(db3.GetLastSql(), "MockExecFilter2");
                    }
                }
            }

            results.PrintDump();
            var ids = results.ConvertAll(x => x.Id);
            Assert.AreEqual(new[] {1, 2, 3, 4, 5, 6}, ids);
        }
        finally
        {
            SqlServerDialect.Provider.ExecFilter = null;
            SqliteDialect.Provider.ExecFilter = null;
        }
    }

    public class MockExecFilter1 : OrmLiteExecFilter
    {
        public override T Exec<T>(IDbConnection dbConn, Func<IDbCommand, T> filter) {
#if NETCORE                
                var isCmd = System.Reflection.TypeExtensions.IsAssignableFrom(typeof(IDbCommand), typeof(T));
#else
            var isCmd = typeof(IDbCommand).IsAssignableFrom(typeof(T));
#endif
            var dbCmd = CreateCommand(dbConn);
            Stopwatch watch = Stopwatch.StartNew();
            try {
                var ret = filter(dbCmd);
                return ret;
            } finally {
                if (!isCmd) {
                    watch.Stop();
                    DisposeCommand(dbCmd, dbConn);
                    dbConn.SetLastCommandText("MockExecFilter1");
                }
            }
        }
    }

    public class MockExecFilter2 : OrmLiteExecFilter
    {
        public override T Exec<T>(IDbConnection dbConn, Func<IDbCommand, T> filter) {
#if NETCORE                
                var isCmd = System.Reflection.TypeExtensions.IsAssignableFrom(typeof(IDbCommand), typeof(T));
#else
            var isCmd = typeof(IDbCommand).IsAssignableFrom(typeof(T));
#endif
            var dbCmd = CreateCommand(dbConn);
            Stopwatch watch = Stopwatch.StartNew();
            try {
                var ret = filter(dbCmd);
                return ret;
            } finally {
                if (!isCmd) {
                    watch.Stop();
                    DisposeCommand(dbCmd, dbConn);
                    dbConn.SetLastCommandText("MockExecFilter2");
                }
            }
        }
    }
}