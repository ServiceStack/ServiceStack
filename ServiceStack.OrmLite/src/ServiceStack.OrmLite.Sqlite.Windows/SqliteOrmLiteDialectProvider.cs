using System;
using System.Data;
using System.Data.SQLite;
using ServiceStack.OrmLite.Sqlite.Converters;

namespace ServiceStack.OrmLite.Sqlite
{
    //Alias
    public class SqliteWindowsOrmLiteDialectProvider : SqliteOrmLiteDialectProvider {}

    public class SqliteOrmLiteDialectProvider : SqliteOrmLiteDialectProviderBase
    {
        public static SqliteOrmLiteDialectProvider Instance = new SqliteOrmLiteDialectProvider();

        public SqliteOrmLiteDialectProvider() : base()
        {
            OrmLiteConfig.DeoptimizeReader = true;
            base.RegisterConverter<DateTime>(new SqliteWindowsDateTimeConverter());
        }

        protected override IDbConnection CreateConnection(string connectionString)
        {
            return new SQLiteConnection(connectionString, parseViaFramework: ParseViaFramework);
        }

        public override IDbDataParameter CreateParam()
        {
            return new SQLiteParameter();
        }
    }
}