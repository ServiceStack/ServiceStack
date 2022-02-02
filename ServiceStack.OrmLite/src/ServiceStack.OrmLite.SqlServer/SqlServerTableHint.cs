namespace ServiceStack.OrmLite.SqlServer
{
    public class SqlServerTableHint
    {
        public static JoinFormatDelegate ReadUncommitted = (dialect, tableDef, expr) => $"{dialect.GetQuotedTableName(tableDef)} WITH (READUNCOMMITTED) {expr}";
        public static JoinFormatDelegate ReadCommitted = (dialect, tableDef, expr) => $"{dialect.GetQuotedTableName(tableDef)} WITH (READCOMMITTED) {expr}";
        public static JoinFormatDelegate ReadPast = (dialect, tableDef, expr) => $"{dialect.GetQuotedTableName(tableDef)} WITH (READPAST) {expr}";
        public static JoinFormatDelegate Serializable = (dialect, tableDef, expr) => $"{dialect.GetQuotedTableName(tableDef)} WITH (SERIALIZABLE) {expr}";
        public static JoinFormatDelegate RepeatableRead = (dialect, tableDef, expr) => $"{dialect.GetQuotedTableName(tableDef)} WITH (REPEATABLEREAD) {expr}";
        public static JoinFormatDelegate NoLock = (dialect, tableDef, expr) => $"{dialect.GetQuotedTableName(tableDef)} WITH (NOLOCK) {expr}";
    }
}
