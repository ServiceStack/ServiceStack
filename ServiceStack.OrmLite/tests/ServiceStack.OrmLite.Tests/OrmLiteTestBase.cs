using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite.Tests
{
//    public class Config
//    {
//        public static Dialect DefaultDialect = Dialect.Sqlite;
//        public const bool EnableDebugLogging = false;
//
//        public static string SqliteFileDir = "~/App_Data/".MapAbsolutePath();
//        public static string SqliteFileDb = "~/App_Data/db.sqlite".MapAbsolutePath();
//        public static string SqlServerDb = "~/App_Data/Database1.mdf".MapAbsolutePath();
//        //public static string SqlServerBuildDb = "Data Source=localhost;Initial Catalog=TestDb;Integrated Security=SSPI;Connect Timeout=120;MultipleActiveResultSets=True";
//
//        public static string SqliteMemoryDb = Environment.GetEnvironmentVariable("SQLITE_CONNECTION") ?? ":memory:";
//        public static string SqlServerBuildDb = Environment.GetEnvironmentVariable("MSSQL_CONNECTION") ?? "Data Source=tcp:localhost,48501\\SQLExpress;Initial Catalog=master;User Id=sa;Password=Test!tesT;Connect Timeout=120;MultipleActiveResultSets=True;";
//        public static string OracleDb = Environment.GetEnvironmentVariable("ORACLE_CONNECTION") ?? "Data Source=localhost:48401/XE;User ID=system;Password=test";
//        public static string MySqlDb_5_5 = Environment.GetEnvironmentVariable("MYSQL_CONNECTION") ?? "Server=localhost;Port=48201;Database=test;UID=root;Password=test;SslMode=none";
//        public static string MySqlDb_10_1 = Environment.GetEnvironmentVariable("MYSQL_CONNECTION") ?? "Server=localhost;Port=48202;Database=test;UID=root;Password=test;SslMode=none";
//        public static string MySqlDb_10_2 = Environment.GetEnvironmentVariable("MYSQL_CONNECTION") ?? "Server=localhost;Port=48203;Database=test;UID=root;Password=test;SslMode=none";
//        public static string MySqlDb_10_3 = Environment.GetEnvironmentVariable("MYSQL_CONNECTION") ?? "Server=localhost;Port=48204;Database=test;UID=root;Password=test;SslMode=none";
//        public static string MySqlDb_10_4 = Environment.GetEnvironmentVariable("MYSQL_CONNECTION") ?? "Server=localhost;Port=48205;Database=test;UID=root;Password=test;SslMode=none";
//        public static string PostgresDb_9 = Environment.GetEnvironmentVariable("PGSQL_CONNECTION") ?? "Server=localhost;Port=48301;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200";
//        public static string PostgresDb_10 = Environment.GetEnvironmentVariable("PGSQL_CONNECTION") ?? "Server=localhost;Port=48302;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200";
//        public static string PostgresDb_11 = Environment.GetEnvironmentVariable("PGSQL_CONNECTION") ?? "Server=localhost;Port=48303;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200";
//        public static string FirebirdDb_3 = Environment.GetEnvironmentVariable("FIREBIRD_CONNECTION") ?? @"User=SYSDBA;Password=masterkey;Database=/firebird/data/test.gdb;DataSource=localhost;Port=48101;Dialect=3;charset=ISO8859_1;MinPoolSize=0;MaxPoolSize=100;";
//
//        public static IOrmLiteDialectProvider DefaultProvider = SqlServerDialect.Provider;
//        public static string DefaultConnection = SqlServerBuildDb;
//        
//        public static string GetDefaultConnection()
//        {
//            OrmLiteConfig.DialectProvider = DefaultProvider;
//            return DefaultConnection;
//        }
//
//        public static IDbConnection OpenDbConnection()
//        {
//            return GetDefaultConnection().OpenDbConnection();
//        }
//    }

    public class OrmLiteTestBase
    {
        protected virtual string ConnectionString { get; set; }

        public OrmLiteTestBase() { }

        public OrmLiteTestBase(Dialect dialect)
        {
            Dialect = dialect;
            Init();
        }

        protected string GetConnectionString()
        {
            return GetFileConnectionString();
        }

        public static OrmLiteConnectionFactory CreateSqliteMemoryDbFactory()
        {
            var dbFactory = new OrmLiteConnectionFactory(SqliteDb.MemoryConnection, SqliteDialect.Provider);
            return dbFactory;
        }

        public static OrmLiteConnectionFactory CreateSqlServerDbFactory()
        {
            var dbFactory = new OrmLiteConnectionFactory(SqlServerDb.DefaultConnection, SqlServerDialect.Provider);
            return dbFactory;
        }

        public static OrmLiteConnectionFactory CreateMySqlDbFactory()
        {
            var dbFactory = new OrmLiteConnectionFactory(MySqlDb.DefaultConnection, MySqlDialect.Provider);
            return dbFactory;
        }

        public static OrmLiteConnectionFactory CreatePostgreSqlDbFactory()
        {
            var dbFactory = new OrmLiteConnectionFactory(PostgreSqlDb.DefaultConnection, PostgreSqlDialect.Provider);
            return dbFactory;
        }

        protected virtual string GetFileConnectionString()
        {
            var connectionString = SqliteDb.FileConnection;
            if (File.Exists(connectionString))
                File.Delete(connectionString);

            return connectionString;
        }

        protected void CreateNewDatabase()
        {
            if (ConnectionString.Contains(".sqlite"))
                ConnectionString = GetFileConnectionString();
        }

        public Dialect Dialect = TestConfig.Dialects;
        protected OrmLiteConnectionFactory DbFactory;

        OrmLiteConnectionFactory Init(string connStr, IOrmLiteDialectProvider dialectProvider)
        {
            ConnectionString = connStr;
            OrmLiteConfig.DialectProvider = dialectProvider;
            DbFactory = new OrmLiteConnectionFactory(ConnectionString, dialectProvider);
            return DbFactory;
        }

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            Init();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            OrmLiteContext.Instance.ClearItems();
        }

        private OrmLiteConnectionFactory Init()
        {
            //OrmLiteConfig.UseParameterizeSqlExpressions = false;

            //OrmLiteConfig.DeoptimizeReader = true;
            LogManager.LogFactory = new ConsoleLogFactory(debugEnabled: TestConfig.EnableDebugLogging);
            
            switch (Dialect)
            {
                case Dialect.Sqlite:
                    var dbFactory = Init(SqliteDb.MemoryConnection, SqliteDialect.Provider);
                    dbFactory.AutoDisposeConnection = false;
                    return dbFactory;
                case Dialect.SqlServer:
                    return Init(SqlServerDb.DefaultConnection, SqlServerDialect.Provider);
                case Dialect.SqlServer2012:
                    return Init(SqlServerDb.DefaultConnection, SqlServer2012Dialect.Provider);
                case Dialect.SqlServer2014:
                    return Init(SqlServerDb.DefaultConnection, SqlServer2014Dialect.Provider);
                case Dialect.SqlServer2016:
                    return Init(SqlServerDb.DefaultConnection, SqlServer2016Dialect.Provider);
                case Dialect.SqlServer2017:
                    return Init(SqlServerDb.DefaultConnection, SqlServer2017Dialect.Provider);
                case Dialect.SqlServer2019:
                    return Init(SqlServerDb.DefaultConnection, SqlServer2019Dialect.Provider);
                case Dialect.MySql:
                    return Init(MySqlDb.DefaultConnection, MySqlDialect.Provider);
                case Dialect.MySqlConnector:
                    return Init(MySqlDb.DefaultConnection, MySqlConnectorDialect.Provider);
                case Dialect.PostgreSql9:
                case Dialect.PostgreSql10:
                case Dialect.PostgreSql11:
                    return Init(PostgreSqlDb.DefaultConnection, PostgreSqlDialect.Provider);
//                case Dialect.SqlServerMdf:
//                    return Init(Config.SqlServerDb, SqlServerDialect.Provider);
                case Dialect.Oracle:
                    return Init(OracleDb.DefaultConnection, OracleDialect.Provider);
                case Dialect.Firebird:
                    return Init(FirebirdDb.DefaultConnection, FirebirdDialect.Provider);
                case Dialect.Firebird4:
                    return Init(FirebirdDb.V4Connection, Firebird4Dialect.Provider);
            }

            throw new NotImplementedException("{0}".Fmt(Dialect));
        }

        public void Log(string text)
        {
            Console.WriteLine(text);
        }

        public IDbConnection InMemoryDbConnection { get; set; }

        public virtual IDbConnection OpenDbConnection()
        {
            if (ConnectionString == ":memory:")
            {
                if (InMemoryDbConnection == null || DbFactory.AutoDisposeConnection)
                {
                    InMemoryDbConnection = new OrmLiteConnection(DbFactory);
                    InMemoryDbConnection.Open();
                }
                return InMemoryDbConnection;
            }

            return DbFactory.OpenDbConnection();
        }

        public virtual Task<IDbConnection> OpenDbConnectionAsync()
        {
            if (ConnectionString == ":memory:")
            {
                if (InMemoryDbConnection == null || DbFactory.AutoDisposeConnection)
                {
                    InMemoryDbConnection = new OrmLiteConnection(DbFactory);
                    InMemoryDbConnection.Open();
                }
                return Task.FromResult(InMemoryDbConnection);
            }

            return DbFactory.OpenDbConnectionAsync();
        }

        protected void SuppressIfOracle(string reason, params object[] args)
        {
            // Not Oracle if this base class used
        }
    }
}
