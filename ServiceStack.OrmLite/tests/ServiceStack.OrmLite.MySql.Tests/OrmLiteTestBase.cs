using System;
using System.Data;
using ServiceStack.Configuration;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite.MySql.Tests
{
    public class MySqlConfig
    {
        public static readonly IOrmLiteDialectProvider DialectProvider =
#if !MYSQLCONNECTOR
            MySqlDialectProvider.Instance;
#else
            MySqlConnectorDialectProvider.Instance;
#endif
        public static string ConnectionString = "Server=localhost;Database=test;UID=root;Password=test";
    }

    public class OrmLiteTestBase
    {
        protected string ConnectionString { get; set; }

        public OrmLiteTestBase()
		{
			LogManager.LogFactory = new ConsoleLogFactory();

		    OrmLiteConfig.DialectProvider = MySqlConfig.DialectProvider;
            ConnectionString = ConfigUtils.GetAppSetting("AppDb", MySqlConfig.ConnectionString);
		}

		public void Log(string text)
		{
			Console.WriteLine(text);
		}

        public virtual IDbConnection OpenDbConnection(string connString = null)
        {
            connString = connString ?? ConnectionString;
            return connString.OpenDbConnection();
        }
    }
}