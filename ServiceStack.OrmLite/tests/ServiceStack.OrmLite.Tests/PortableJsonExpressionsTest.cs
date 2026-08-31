using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite.Tests;

[TestFixture]
[NonParallelizable]
public class PortableJsonExpressionsTest : PortableJsonExpressionsTestBase
{
    protected override IDbConnection OpenJsonDbConnection()
    {
        var dialect = new SqliteOrmLiteDialectProvider { UseJson = true };
        return new OrmLiteConnectionFactory(":memory:", dialect).OpenDbConnection();
    }
}
