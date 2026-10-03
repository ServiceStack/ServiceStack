using System;
using System.Data;
using Microsoft.Data.Sqlite;
using ServiceStack.OrmLite.Sqlite.Converters;

namespace ServiceStack.OrmLite.Sqlite;

public class SqliteOrmLiteDialectProvider : SqliteOrmLiteDialectProviderBase
{
    public override DbKind Kind => DbKind.Sqlite;

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

    /// <summary>
    /// SQLITE_BUSY and SQLITE_LOCKED, where the database was locked by another connection and the statement
    /// wasn't applied
    /// </summary>
    public override TransientError GetTransientError(Exception ex) =>
        ex is SqliteException { SqliteErrorCode: 5 or 6 }
            ? TransientError.NotApplied
            : base.GetTransientError(ex);

    public override IDbDataParameter CreateParam()
    {
        return new SqliteParameter();
    }
}