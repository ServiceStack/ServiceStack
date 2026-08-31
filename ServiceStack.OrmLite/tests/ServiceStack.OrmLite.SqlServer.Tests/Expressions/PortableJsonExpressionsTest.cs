using System.Data;
using NUnit.Framework;
using ServiceStack.OrmLite.SqlServer;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.SqlServerTests.Expressions;

[TestFixture]
[NonParallelizable]
public class PortableJsonExpressionsTest : PortableJsonExpressionsTestBase
{
    protected override IDbConnection OpenJsonDbConnection()
    {
        var dialect = new SqlServer2022OrmLiteDialectProvider { UseJson = true };
        return new OrmLiteConnectionFactory(OrmLiteTestBase.GetConnectionString(), dialect).OpenDbConnection();
    }
}
