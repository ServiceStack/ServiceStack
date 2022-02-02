using System.Collections.Generic;
using System.Text;

namespace ServiceStack.OrmLite.SqlServer 
{
    public class SqlServer2008OrmLiteDialectProvider : SqlServerOrmLiteDialectProvider
    {
        public new static SqlServer2008OrmLiteDialectProvider Instance = new SqlServer2008OrmLiteDialectProvider();

        public override string SqlConcat(IEnumerable<object> args)
        {
            var sb = new StringBuilder();
            foreach (var arg in args)
            {
                if (sb.Length > 0)
                    sb.Append(" + ");

                var argType = arg.GetType();
                if (argType.IsValueType)
                {
                    sb.AppendFormat("'{0}'", arg);
                }
                else if (arg is string s && s.StartsWith("'") || arg is PartialSqlString p)
                {
                    sb.Append(arg);
                }
                else
                {
                    sb.Append($"CAST({arg} AS VARCHAR(MAX))");
                }
            }
            
            return sb.ToString();
        }
    }
}