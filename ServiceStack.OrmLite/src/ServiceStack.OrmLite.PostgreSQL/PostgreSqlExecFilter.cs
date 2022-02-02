using System;
using System.Data;
using Npgsql;

namespace ServiceStack.OrmLite.PostgreSQL
{
    public class PostgreSqlExecFilter : OrmLiteExecFilter 
    {
        public Action<NpgsqlCommand> OnCommand { get; set; }

        public override IDbCommand CreateCommand(IDbConnection dbConn)
        {
            var cmd = base.CreateCommand(dbConn);
            if (OnCommand != null)
            {
                if (cmd.ToDbCommand() is NpgsqlCommand psqlCmd)
                {
                    OnCommand?.Invoke(psqlCmd);
                }
            }
            return cmd;
        }
    }
}
