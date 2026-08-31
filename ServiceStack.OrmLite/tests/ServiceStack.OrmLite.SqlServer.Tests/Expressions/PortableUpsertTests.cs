using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.SqlServerTests.Expressions;

[TestFixture]
[NonParallelizable]
public class PortableUpsertTests : PortableUpsertTestsBase
{
    protected override string NativeUpsertSqlFragment => "MERGE INTO";

    protected override IDbConnection OpenUpsertDbConnection() =>
        new OrmLiteConnectionFactory(OrmLiteTestBase.GetConnectionString(), SqlServerDialect.Provider).OpenDbConnection();
}
