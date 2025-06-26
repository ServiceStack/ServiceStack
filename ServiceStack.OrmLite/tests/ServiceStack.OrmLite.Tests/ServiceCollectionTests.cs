using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.Data;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class ServiceCollectionTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
    }
#if NETCORE
    [Test]
    public void Can_configure_OrmLite_with_ServiceCollection_Extensions()
    {
        var services = new ServiceCollection();
        
        services.AddOrmLite(options => {
                options.UseSqlite(":memory:");
            })
            .AddSqlite("db1", "db1.sqlite")
            .AddSqlite("db2", "db2.sqlite")
            .AddPostgres("postgres", Environment.GetEnvironmentVariable("PGSQL_CONNECTION"))
            .AddSqlServer("sqlserver", Environment.GetEnvironmentVariable("MSSQL_CONNECTION"))
            .AddSqlServer<SqlServer.SqlServer2016OrmLiteDialectProvider>("sqlserver2016", Environment.GetEnvironmentVariable("MSSQL_CONNECTION"))
            .AddMySql("mysql", Environment.GetEnvironmentVariable("MYSQL_CONNECTION"))
            .AddMySqlConnector("mysqlconnector", Environment.GetEnvironmentVariable("MYSQL_CONNECTION"))
            .AddOracle("oracle", Environment.GetEnvironmentVariable("ORACLE_CONNECTION") ?? "")
            .AddFirebird("firebird", Environment.GetEnvironmentVariable("FIREBIRD_CONNECTION") ?? "");
        
        var provider = services.BuildServiceProvider();
        var dbFactory = provider.GetService<IDbConnectionFactory>();
        Assert.That(dbFactory, Is.Not.Null);
        
        using var app = dbFactory.Open();
        Assert.That(app.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
        using var db1 = dbFactory.Open("db1");
        Assert.That(db1.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
        using var db2 = dbFactory.Open("db2");
        Assert.That(db2.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));

        if (Dialect.HasFlag(Dialect.SqlServer))
        {
            using var sqlServer = dbFactory.Open("sqlserver");
            Assert.That(sqlServer.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
            using var sqlServer2016 = dbFactory.Open("sqlserver2016");
            Assert.That(sqlServer.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
        }
        if (Dialect.HasFlag(Dialect.AnyPostgreSql))
        {
            using var postgres = dbFactory.Open("postgres");
            Assert.That(postgres.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
        }
        if (Dialect.HasFlag(Dialect.MySql))
        {
            using var mysql = dbFactory.Open("mysql");
            Assert.That(mysql.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
        }
        if (Dialect.HasFlag(Dialect.MySqlConnector))
        {
            using var mysql = dbFactory.Open("mysqlconnector");
            Assert.That(mysql.Scalar<string>("SELECT 'Hello World'"), Is.EqualTo("Hello World"));
        }
    }
#endif
}