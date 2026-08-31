using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.MySql.Tests;

[TestFixture]
[NonParallelizable]
public class PortableJsonExpressionsTest : PortableJsonExpressionsTestBase
{
    protected override bool SupportsJsonContains => true;

    protected override IDbConnection OpenJsonDbConnection()
    {
        var dialect = new MySqlDialectProvider { UseJson = true };
        return new OrmLiteConnectionFactory(MySqlConfig.ConnectionString, dialect).OpenDbConnection();
    }
}
