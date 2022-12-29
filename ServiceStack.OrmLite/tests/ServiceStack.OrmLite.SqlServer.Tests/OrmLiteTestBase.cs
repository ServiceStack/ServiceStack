using System;
using System.Configuration;
using System.Data;
using NUnit.Framework;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite.SqlServerTests
{
    public class OrmLiteTestBase
    {
        protected virtual string ConnectionString { get; set; }

        public IDbConnection Db { get; set; }

        [OneTimeSetUp]
        public virtual void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            ConnectionString = GetConnectionString();
            OrmLiteConfig.DialectProvider = SqlServerDialect.Provider;
        }

        public void Log(string text)
        {
            Console.WriteLine(text);
        }

        // public static string GetConnectionString() => "Data Source=tcp:localhost,48501\\SQLExpress;Initial Catalog=master;User Id=sa;Password=Test!tesT;Connect Timeout=120;MultipleActiveResultSets=True;";
        public static string GetConnectionString() => "Server=localhost;Database=test;User Id=test;Password=test;MultipleActiveResultSets=True;";
        public virtual IDbConnection OpenDbConnection(string connString = null, IOrmLiteDialectProvider dialectProvider = null)
        {
            dialectProvider ??= OrmLiteConfig.DialectProvider;
            connString ??= ConnectionString;
            return new OrmLiteConnectionFactory(connString, dialectProvider).OpenDbConnection();
        }
    }
}
