using System;
using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests;

[TestFixture]
[NonParallelizable]
public class PortableJsonExpressionsTest : PortableJsonExpressionsTestBase
{
    protected override bool SupportsJsonContains => true;

    protected override IDbConnection OpenJsonDbConnection()
    {
        var connectionString = Environment.GetEnvironmentVariable("PGSQL_CONNECTION")
            ?? PostgreSqlDb.DefaultConnection;
        var dialect = new PostgreSqlDialectProvider { UseJson = true };
        return new OrmLiteConnectionFactory(connectionString, dialect).OpenDbConnection();
    }
}
