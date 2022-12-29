using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using ServiceStack.OrmLite.MySql.Converters;

namespace ServiceStack.OrmLite.MySql
{
    public class MySqlDialectProvider : MySqlDialectProviderBase<MySqlDialectProvider>
    {
        public static MySqlDialectProvider Instance = new MySqlDialectProvider();

        private const string TextColumnDefinition = "TEXT";

        public override IDbConnection CreateConnection(string connectionString, Dictionary<string, string> options)
        {
            return new MySqlConnection(connectionString);
        }

        public MySqlDialectProvider()
        {
            RegisterConverter<DateTime>(new MySqlDateTimeConverter());
        }

        public override IDbDataParameter CreateParam()
        {
            return new MySqlParameter();
        }
    }
    
    public class MySql55DialectProvider : MySqlDialectProviderBase<MySqlDialectProvider>
    {
        public static MySql55DialectProvider Instance = new MySql55DialectProvider();

        private const string TextColumnDefinition = "TEXT";

        public override IDbConnection CreateConnection(string connectionString, Dictionary<string, string> options)
        {
            return new MySqlConnection(connectionString);
        }

        public MySql55DialectProvider()
        {
            RegisterConverter<DateTime>(new MySql55DateTimeConverter());
            RegisterConverter<string>(new MySql55StringConverter());
            RegisterConverter<char[]>(new MySql55CharArrayConverter());
        }

        public override IDbDataParameter CreateParam()
        {
            return new MySqlParameter();
        }
    }
}
