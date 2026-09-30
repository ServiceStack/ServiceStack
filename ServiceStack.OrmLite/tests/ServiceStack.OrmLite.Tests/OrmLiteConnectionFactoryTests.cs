using System;
using System.Threading.Tasks;
using System.Data;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class OrmLiteConnectionFactoryTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void AutoDispose_ConnectionFactory_disposes_connection()
    {
        OrmLiteConfig.DialectProvider = SqliteDialect.Provider;
        var factory = new OrmLiteConnectionFactory(":memory:")
        {
            AutoDisposeConnection = true
        };

        using (var db = factory.OpenDbConnection())
        {
            db.CreateTable<Shipper>(false);
            db.Insert(new Shipper { CompanyName = "I am shipper" });
        }

        using (var db = factory.OpenDbConnection())
        {
            db.CreateTable<Shipper>(false);
            Assert.That(db.Select<Shipper>(), Has.Count.EqualTo(0));
        }
    }

    [Test]
    public void NonAutoDispose_ConnectionFactory_reuses_connection()
    {
        var factory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider)
        {
            AutoDisposeConnection = false,
        };

        using (var db = factory.OpenDbConnection())
        {
            db.CreateTable<Shipper>(false);
            db.Insert(new Shipper { CompanyName = "I am shipper" });
        }

        using (var db = factory.OpenDbConnection())
        {
            db.CreateTable<Shipper>(false);
            Assert.That(db.Select<Shipper>(), Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void Default_inmemory_ConnectionFactory_reuses_connection()
    {
        OrmLiteConfig.DialectProvider = SqliteDialect.Provider;
        var factory = new OrmLiteConnectionFactory(":memory:");

        using (var db = factory.OpenDbConnection())
        {
            db.CreateTable<Shipper>(false);
            db.Insert(new Shipper { CompanyName = "I am shipper" });
        }

        using (var db = factory.OpenDbConnection())
        {
            db.CreateTable<Shipper>(false);
            Assert.That(db.Select<Shipper>(), Has.Count.EqualTo(1));
        }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Test]
    public void Can_open_after_close_connection()
    {
        OrmLiteConfig.DialectProvider = SqliteDialect.Provider;
        var factory = new OrmLiteConnectionFactory(":memory:");
        using (var db = factory.OpenDbConnection())
        {
            Assert.That(db.State, Is.EqualTo(ConnectionState.Open));
            db.Close();
            Assert.That(db.State, Is.EqualTo(ConnectionState.Closed));
            db.Open();
            Assert.That(db.State, Is.EqualTo(ConnectionState.Open));
        }
    }

    [Test]
    public void Can_open_different_ConnectionString_with_DbFactory()
    {
        var factory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        using var db = factory.OpenDbConnection();
        Assert.That(db.State, Is.EqualTo(ConnectionState.Open));
        Assert.That(db.ConnectionString, Is.EqualTo(":memory:"));

        var dbFilePath = "~/db.sqlite".MapAbsolutePath();
        using var dbFile = factory.OpenDbConnectionString(dbFilePath);
        Assert.That(dbFile.State, Is.EqualTo(ConnectionState.Open));
        Assert.That(dbFile.ConnectionString, Is.EqualTo(dbFilePath));
    }

    [Test]
    public void Can_open_different_ConnectionString_with_DbFactory_DataSource()
    {
        var factory = new OrmLiteConnectionFactory("DataSource=:memory:", SqliteDialect.Provider);
        using var db = factory.OpenDbConnection();
        Assert.That(db.State, Is.EqualTo(ConnectionState.Open));
        Assert.That(db.ConnectionString, Is.EqualTo(":memory:"));

        var dbFilePath = "~/db.sqlite".MapAbsolutePath();
        using var dbFile = factory.OpenDbConnectionString(dbFilePath);
        Assert.That(dbFile.State, Is.EqualTo(ConnectionState.Open));
        Assert.That(dbFile.ConnectionString, Is.EqualTo(dbFilePath));
    }

    [Test]
    public async Task Disposes_connection_if_opening_it_fails()
    {
        var disposed = 0;
        var factory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider) { OnDispose = _ => disposed++ };

        Assert.Throws<NotSupportedException>(() => factory.OpenDbConnection(_ => throw new NotSupportedException()));
        Assert.ThrowsAsync<NotSupportedException>(async () => 
            await factory.OpenDbConnectionAsync(_ => throw new NotSupportedException()));
        Assert.That(disposed, Is.EqualTo(2));

        factory.RegisterConnection(nameof(Disposes_connection_if_opening_it_fails), ":memory:", SqliteDialect.Provider);
        OrmLiteConnectionFactory.NamedConnections[nameof(Disposes_connection_if_opening_it_fails)].OnDispose = _ => disposed++;

        Assert.Throws<NotSupportedException>(() => 
            factory.OpenDbConnection(nameof(Disposes_connection_if_opening_it_fails), _ => throw new NotSupportedException()));
        Assert.ThrowsAsync<NotSupportedException>(async () => 
            await factory.OpenDbConnectionAsync(nameof(Disposes_connection_if_opening_it_fails), _ => throw new NotSupportedException()));
        Assert.That(disposed, Is.EqualTo(4));

        // Connections are still usable afterwards
        using var db = await factory.OpenDbConnectionAsync();
        Assert.That(db.State, Is.EqualTo(ConnectionState.Open));
    }
}