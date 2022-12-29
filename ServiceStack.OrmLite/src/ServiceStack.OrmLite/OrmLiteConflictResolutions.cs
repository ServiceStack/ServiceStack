using System;
using System.Data;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    public class ConflictResolution
    {
        public const string Ignore = "IGNORE";
    }

    public static class OrmLiteConflictResolutions
    {
        public static void OnConflictIgnore(this IDbCommand dbCmd) => dbCmd.OnConflict(ConflictResolution.Ignore);

        public static void OnConflict(this IDbCommand dbCmd, string conflictResolution)
        {
            var pos = dbCmd.CommandText?.IndexOf(' ') ?? -1;
            if (pos == -1)
                throw new NotSupportedException("Cannot specify ON CONFLICT resolution on Invalid SQL starting with: " + dbCmd.CommandText.SubstringWithEllipsis(0, 50));

            var sqlConflict = dbCmd.GetDialectProvider().SqlConflict(dbCmd.CommandText, conflictResolution);
            dbCmd.CommandText = sqlConflict;
        }
    }
}