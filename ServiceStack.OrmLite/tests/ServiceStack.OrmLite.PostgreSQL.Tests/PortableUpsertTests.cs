using System;
using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests;

[TestFixture]
[NonParallelizable]
public class PortableUpsertTests : PortableUpsertTestsBase
{
    protected override string NativeUpsertSqlFragment => "ON CONFLICT";

    protected override IDbConnection OpenUpsertDbConnection()
    {
        var connectionString = Environment.GetEnvironmentVariable("PGSQL_CONNECTION")
            ?? PostgreSqlDb.DefaultConnection;
        return new OrmLiteConnectionFactory(connectionString, PostgreSqlDialect.Provider).OpenDbConnection();
    }
}
