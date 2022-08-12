using System;
using System.Data.Common;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.OrmLite;
using ServiceStack.Testing;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class MiniProfilerTests
{
    [Test]
    public async Task Can_query_with_ProfiledDbConnection()
    {
        var appHost = new BasicAppHost {
            ConfigureAppHost = host => host.Plugins.Add(new MiniProfilerFeature())
        }.Init();
        var dbFactory = CreateConnectionFactoryFactory();

        using var db = await dbFactory.OpenDbConnectionAsync();

        var d = (await db.SelectAsync<Result>("select @category as Category", 
                new { category = "some text"}))
            .ToArray();

        Assert.That(d, Is.Not.Empty);
        Assert.That(d[0].Category, Is.EqualTo("some text"));
    }
    
    private static OrmLiteConnectionFactory CreateConnectionFactoryFactory()
    {
        return new OrmLiteConnectionFactory(Environment.GetEnvironmentVariable("PGSQL_CONNECTION"), PostgreSqlDialect.Provider) {   
            ConnectionFilter = connection => {
                // This forces a specific datestyle when communicating with PGSQL, and ensures that we
                // use our datetime format even if the database has a separate format.
                connection.ExecuteNonQuery(@"SET datestyle TO ""ISO, DMY""");
                return new ProfiledDbConnection((DbConnection)connection, MiniProfiler.Profiler.Current);
            }
        };
    }
    
    public class Result
    {
        public string Category { get; set; }
    }    
}