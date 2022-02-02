using System;
using System.Configuration;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.OrmLite.SqlServer.Converters;

namespace ServiceStack.OrmLite.SqlServerTests.Converters
{
    public class SqlServer2012ConvertersOrmLiteTestBase : OrmLiteTestBase
    {
        [OneTimeSetUp]
        public override void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();

            // Appending the Sql Server Type System Version to use SqlServerSpatial110.dll (2012) assembly
            // Sql Server defaults to SqlServerSpatial100.dll (2008 R2) even for versions greater
            // https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlconnection.connectionstring.aspx
            ConnectionString = GetConnectionString() + "Type System Version=SQL Server 2012;";

            var dialectProvider = SqlServerConverters.Configure(SqlServer2012Dialect.Provider);

            Db = new OrmLiteConnectionFactory(ConnectionString, dialectProvider).OpenDbConnection();
        }
    }

    public class SqlServer2014ConvertersOrmLiteTestBase : SqlServer2012ConvertersOrmLiteTestBase
    {
        [OneTimeSetUp]
        public override void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();

            // Appending the Sql Server Type System Version to use SqlServerSpatial110.dll (2012) assembly
            // Sql Server defaults to SqlServerSpatial100.dll (2008 R2) even for versions greater
            // https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlconnection.connectionstring.aspx
            ConnectionString = GetConnectionString() + "Type System Version=SQL Server 2012;";

            var dialectProvider = SqlServerConverters.Configure(SqlServer2014Dialect.Provider);

            Db = new OrmLiteConnectionFactory(ConnectionString, dialectProvider).OpenDbConnection();
        }
    }

    public class SqlServer2016ConvertersOrmLiteTestBase : SqlServer2014ConvertersOrmLiteTestBase
    {
        [OneTimeSetUp]
        public override void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();

            // Appending the Sql Server Type System Version to use SqlServerSpatial110.dll (2012) assembly
            // Sql Server defaults to SqlServerSpatial100.dll (2008 R2) even for versions greater
            // https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlconnection.connectionstring.aspx
            ConnectionString = GetConnectionString() + "Type System Version=SQL Server 2012;";

            var dialectProvider = SqlServerConverters.Configure(SqlServer2016Dialect.Provider);

            Db = new OrmLiteConnectionFactory(ConnectionString, dialectProvider).OpenDbConnection();
        }
    }
}
