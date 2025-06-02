using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLiteDialects(Dialect.Sqlite)]

public class SqliteWriterLockTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public OrmLiteConnectionFactory CreateDbFactory()
    {
        var dbFactory = new OrmLiteConnectionFactory(
            "DataSource=./App_Data/app.db;Cache=Shared", SqliteDialect.Provider);

        dbFactory.RegisterConnection("db1", "DataSource=./App_Data/db1.db;Cache=Shared", SqliteDialect.Provider);
        dbFactory.RegisterConnection("db2", "DataSource=./App_Data/db2.db;Cache=Shared", SqliteDialect.Provider);
        
        return dbFactory;
    }
    
    [Test]
    public void Does_use_WriteLock()
    {
        var dbFactory = CreateDbFactory();
        
        using var db = dbFactory.OpenDbConnection();
        Assert.That(((OrmLiteConnection)db).WriteLock, Is.EqualTo(Locks.AppDb));
        db.DropAndCreateTable<Person>();
        db.Insert(new Person { Id = 1, FirstName = "app.db" });

        using var db1 = dbFactory.OpenDbConnection("db1");
        Assert.That(((OrmLiteConnection)db1).WriteLock, Is.EqualTo(Locks.GetDbLock("db1")));
        db1.DropAndCreateTable<Person>();
        db1.Insert(new Person { Id = 1, FirstName = "db1.db" });
        
        using var db2 = dbFactory.OpenDbConnection("db2");
        Assert.That(((OrmLiteConnection)db2).WriteLock, Is.EqualTo(Locks.GetDbLock("db2")));
        db2.DropAndCreateTable<Person>();
        db2.Insert(new Person { Id = 1, FirstName = "db2.db" });
    }

    [Test]
    public void Can_execute_multiple_writes_on_multiple_threads_and_same_connection()
    {
        var dbFactory = CreateDbFactory();
        
        using var db = dbFactory.OpenDbConnection();
        db.DropAndCreateTable<PersonWithAutoId>();
        db.Insert(new PersonWithAutoId { FirstName = "app.db" });

        using var db1 = dbFactory.OpenDbConnection("db1");
        db1.DropAndCreateTable<PersonWithAutoId>();
        db1.Insert(new PersonWithAutoId { FirstName = "db1.db" });
        
        using var db2 = dbFactory.OpenDbConnection("db2");
        db2.DropAndCreateTable<PersonWithAutoId>();
        db2.Insert(new PersonWithAutoId { FirstName = "db2.db" });

        var threads = new List<Thread>();
        for (var i = 0; i < 10; i++)
        {
            threads.Add(new Thread(() =>
            {
                db.Insert(new PersonWithAutoId { FirstName = "app.db" });
            }));
            threads.Add(new Thread(() =>
            {
                db1.Insert(new PersonWithAutoId { FirstName = "db1.db" });
            }));
            threads.Add(new Thread(() =>
            {
                db2.Insert(new PersonWithAutoId { FirstName = "db2.db" });
            }));
        }

        threads.ForEach(x => x.Start());
        threads.ForEach(x => x.Join());
        
        Assert.That(db.Count<PersonWithAutoId>(), Is.EqualTo(11));
        Assert.That(db1.Count<PersonWithAutoId>(), Is.EqualTo(11));
        Assert.That(db2.Count<PersonWithAutoId>(), Is.EqualTo(11));
    }

    [Test]
    public void Can_execute_multiple_writes_on_multiple_threads_and_connections()
    {
        var dbFactory = CreateDbFactory();
        
        using var db = dbFactory.OpenDbConnection();
        db.DropAndCreateTable<PersonWithAutoId>();
        db.Insert(new PersonWithAutoId { FirstName = "app.db" });

        using var db1 = dbFactory.OpenDbConnection("db1");
        db1.DropAndCreateTable<PersonWithAutoId>();
        db1.Insert(new PersonWithAutoId { FirstName = "db1.db" });
        
        using var db2 = dbFactory.OpenDbConnection("db2");
        db2.DropAndCreateTable<PersonWithAutoId>();
        db2.Insert(new PersonWithAutoId { FirstName = "db2.db" });

        var threads = new List<Thread>();
        for (var i = 0; i < 10; i++)
        {
            threads.Add(new Thread(() =>
            {
                using var db = dbFactory.OpenDbConnection();
                db.Insert(new PersonWithAutoId { FirstName = "app.db" });
            }));
            threads.Add(new Thread(() =>
            {
                using var db1 = dbFactory.OpenDbConnection("db1");
                db1.Insert(new PersonWithAutoId { FirstName = "db1.db" });
            }));
            threads.Add(new Thread(() =>
            {
                using var db2 = dbFactory.OpenDbConnection("db2");
                db2.Insert(new PersonWithAutoId { FirstName = "db2.db" });
            }));
        }

        threads.ForEach(x => x.Start());
        threads.ForEach(x => x.Join());
        
        Assert.That(db.Count<PersonWithAutoId>(), Is.EqualTo(11));
        Assert.That(db1.Count<PersonWithAutoId>(), Is.EqualTo(11));
        Assert.That(db2.Count<PersonWithAutoId>(), Is.EqualTo(11));
    }

    [Test]
    public void Can_execute_multiple_reads_and_writes_on_multiple_threads_and_connections()
    {
        var dbFactory = CreateDbFactory();
        
        using var db = dbFactory.OpenDbConnection();
        db.DropAndCreateTable<Person>();
        db.Insert(new Person { Id = 1, FirstName = "app.db" });

        using var db1 = dbFactory.OpenDbConnection("db1");
        db1.DropAndCreateTable<Person>();
        db1.Insert(new Person { Id = 1, FirstName = "db1.db" });
        
        using var db2 = dbFactory.OpenDbConnection("db2");
        db2.DropAndCreateTable<Person>();
        db2.Insert(new Person { Id = 1, FirstName = "db2.db" });

        var threads = new List<Thread>();
        for (var i = 0; i < 10; i++)
        {
            var localIndex = i;
            threads.Add(new Thread(() =>
            {
                db.Insert(new Person { Id = localIndex + 2, FirstName = "app.db" });
                Console.WriteLine($"db count: {db.Count<Person>()}");
            }));
            threads.Add(new Thread(() =>
            {
                db1.Insert(new Person { Id = localIndex + 2, FirstName = "db1.db" });
                Console.WriteLine($"db1 count: {db1.Count<Person>()}");
            }));
            threads.Add(new Thread(() =>
            {
                db2.Insert(new Person { Id = localIndex + 2, FirstName = "db2.db" });
                Console.WriteLine($"db2 count: {db2.Count<Person>()}");
            }));
        }

        threads.ForEach(x => x.Start());
        threads.ForEach(x => x.Join());
        
        Assert.That(db.Count<Person>(), Is.EqualTo(11));
        Assert.That(db1.Count<Person>(), Is.EqualTo(11));
        Assert.That(db2.Count<Person>(), Is.EqualTo(11));
    }

    [Test]
    public void Can_execute_multiple_reads_and_writes_in_transactions_on_multiple_threads_and_connections()
    {
        var dbFactory = CreateDbFactory();

        using var db = dbFactory.OpenDbConnection();
        db.DropAndCreateTable<Person>();
        db.Insert(new Person { Id = 1, FirstName = "app.db" });

        using var db1 = dbFactory.OpenDbConnection("db1");
        db1.DropAndCreateTable<Person>();
        db1.Insert(new Person { Id = 1, FirstName = "db1.db" });
        
        using var db2 = dbFactory.OpenDbConnection("db2");
        db2.DropAndCreateTable<Person>();
        db2.Insert(new Person { Id = 1, FirstName = "db2.db" });

        var threads = new List<Thread>();
        for (var i = 0; i < 10; i++)
        {
            var localIndex = i;
            threads.Add(new Thread(() =>
            {
                using var dbLocal = dbFactory.OpenDbConnection();
                using var tx = dbLocal.OpenTransaction();
                Assert.That(((OrmLiteTransaction)tx).WriteLock, Is.EqualTo(Locks.AppDb));
                dbLocal.Insert(new Person { Id = localIndex + 2, FirstName = "app.db" });
                dbLocal.Insert(new Person { Id = localIndex + 12, FirstName = "app.db" });
                Console.WriteLine($"db count: {dbLocal.Count<Person>()}");
                tx.Commit();
            }));
            threads.Add(new Thread(() =>
            {
                using var db1Local = dbFactory.OpenDbConnection("db1");
                using var tx = db1Local.OpenTransaction();
                Assert.That(((OrmLiteTransaction)tx).WriteLock, Is.EqualTo(Locks.GetDbLock("db1")));
                db1Local.Insert(new Person { Id = localIndex + 2, FirstName = "db1.db" });
                db1Local.Insert(new Person { Id = localIndex + 12, FirstName = "db1.db" });
                Console.WriteLine($"db1 count: {db1Local.Count<Person>()}");
                tx.Commit();
            }));
            threads.Add(new Thread(() =>
            {
                using var db2Local = dbFactory.OpenDbConnection("db2");
                using var tx = db2Local.OpenTransaction();
                Assert.That(((OrmLiteTransaction)tx).WriteLock, Is.EqualTo(Locks.GetDbLock("db2")));
                db2Local.Insert(new Person { Id = localIndex + 2, FirstName = "db2.db" });
                db2Local.Insert(new Person { Id = localIndex + 12, FirstName = "db2.db" });
                Console.WriteLine($"db2 count: {db2Local.Count<Person>()}");
                tx.Commit();
            }));
        }

        threads.ForEach(x => x.Start());
        threads.ForEach(x => x.Join());

        Assert.That(db.Count<Person>(), Is.EqualTo(21));
        Assert.That(db1.Count<Person>(), Is.EqualTo(21));
        Assert.That(db2.Count<Person>(), Is.EqualTo(21));
    }
}