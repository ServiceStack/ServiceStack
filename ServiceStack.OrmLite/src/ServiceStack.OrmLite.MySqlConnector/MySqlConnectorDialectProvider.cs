using System;
using System.Collections.Generic;
using System.Data;
using MySqlConnector;
using ServiceStack.OrmLite.MySql.Converters;

namespace ServiceStack.OrmLite.MySql
{
    public class MySqlConnectorDialectProvider : MySqlDialectProviderBase<MySqlConnectorDialectProvider>
    {
        public static MySqlConnectorDialectProvider Instance = new MySqlConnectorDialectProvider();

        private const string TextColumnDefinition = "TEXT";

	    public MySqlConnectorDialectProvider()
	    {
            base.RegisterConverter<DateTime>(new MySqlConnectorDateTimeConverter());
        }

        public override IDbConnection CreateConnection(string connectionString, Dictionary<string, string> options)
        {
            return new MySqlConnection(connectionString);
        }

        public override IDbDataParameter CreateParam()
        {
            return new MySqlParameter();
        }

    }
}
