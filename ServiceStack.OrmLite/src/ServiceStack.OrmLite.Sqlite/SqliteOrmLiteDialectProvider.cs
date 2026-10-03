using System;
using System.Data;
using System.Data.SQLite;

namespace ServiceStack.OrmLite.Sqlite
{
    public class SqliteOrmLiteDialectProvider : SqliteOrmLiteDialectProviderBase
    {
        public override DbKind Kind => DbKind.Sqlite;

        public static SqliteOrmLiteDialectProvider Instance = new();

        public SqliteOrmLiteDialectProvider()
        {
#if NETFX
            ConnectionStringFilter = sb =>
            {
                if (sb.ToString().IndexOf("Version=3", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    sb.Append("Version=3;New=True;Compress=True");
                }
            };
#endif
        }

        protected override IDbConnection CreateConnection(string connectionString)
        {
            return new SQLiteConnection(connectionString);
        }

        /// <summary>
        /// SQLITE_BUSY and SQLITE_LOCKED, where the database was locked by another connection and the statement
        /// wasn't applied
        /// </summary>
        public override TransientError GetTransientError(Exception ex) =>
            ex is SQLiteException { ResultCode: var code } && ((int)code & 0xFF) is 5 or 6
                ? TransientError.NotApplied
                : base.GetTransientError(ex);

        public override IDbDataParameter CreateParam()
        {
            return new SQLiteParameter();
        }
    }
}