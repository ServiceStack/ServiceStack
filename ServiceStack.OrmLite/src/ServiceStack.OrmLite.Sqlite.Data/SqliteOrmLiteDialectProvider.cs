using System;
using System.Data;
using Microsoft.Data.Sqlite;
using ServiceStack.OrmLite.Sqlite.Converters;

namespace ServiceStack.OrmLite.Sqlite
{
    public class SqliteOrmLiteDialectProvider : SqliteOrmLiteDialectProviderBase
    {
        public static SqliteOrmLiteDialectProvider Instance = new();

        public SqliteOrmLiteDialectProvider()
        {
            base.RegisterConverter<DateTime>(new SqliteDataDateTimeConverter());
            base.RegisterConverter<Guid>(new SqliteDataGuidConverter());
        }

        protected override IDbConnection CreateConnection(string connectionString)
        {
            return new SqliteConnection(connectionString);
        }

        public override IDbDataParameter CreateParam()
        {
            return new SqliteParameter();
        }
    }
}