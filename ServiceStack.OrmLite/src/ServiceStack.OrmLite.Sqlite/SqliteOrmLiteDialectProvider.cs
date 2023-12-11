using System.Data;
using System.Data.SQLite;

namespace ServiceStack.OrmLite.Sqlite
{
    public class SqliteOrmLiteDialectProvider : SqliteOrmLiteDialectProviderBase
    {
        public static SqliteOrmLiteDialectProvider Instance = new();

        public SqliteOrmLiteDialectProvider()
        {
#if NETFX
            ConnectionStringFilter = sb => sb.Append("Version=3;New=True;Compress=True");
#endif
        }

        protected override IDbConnection CreateConnection(string connectionString)
        {
            return new SQLiteConnection(connectionString);
        }

        public override IDbDataParameter CreateParam()
        {
            return new SQLiteParameter();
        }
    }
}