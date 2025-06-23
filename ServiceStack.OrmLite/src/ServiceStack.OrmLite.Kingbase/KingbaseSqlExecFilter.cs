using System;
using System.Data;
using Kdbndp;

// ReSharper disable ConvertToPrimaryConstructor

namespace ServiceStack.OrmLite.Kingbase;

public sealed class KingbaseSqlExecFilter : OrmLiteExecFilter
{
    public readonly DbMode DbMode;

    public KingbaseSqlExecFilter(DbMode dbMode)
    {
        DbMode = dbMode;
    }

    public override IDbCommand CreateCommand(IDbConnection dbConn)
    {
        var ormLiteConn = dbConn as OrmLiteConnection;

        var dbCmd = dbConn.CreateCommand();

        if (dbCmd is KdbndpCommand kdbndpCommand)
        {
            kdbndpCommand.DbModeType = DbMode;
        }

        dbCmd.Transaction = ormLiteConn?.Transaction;

        dbCmd.CommandTimeout = ormLiteConn != null
            ? (ormLiteConn.CommandTimeout ?? OrmLiteConfig.CommandTimeout)
            : OrmLiteConfig.CommandTimeout;

        ormLiteConn.SetLastCommand(null);
        ormLiteConn.SetLastCommandText(null);

        return ormLiteConn != null
            ? new OrmLiteCommand(ormLiteConn, dbCmd)
            : dbCmd;
    }
}