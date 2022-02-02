using System.Linq.Expressions;

namespace ServiceStack.OrmLite.Sqlite
{
    public class SqliteExpression<T> : SqlExpression<T>
    {
        public SqliteExpression(IOrmLiteDialectProvider dialectProvider)
            : base(dialectProvider) {}

        protected override object VisitColumnAccessMethod(MethodCallExpression m)
        {
            var args = this.VisitExpressionList(m.Arguments);
            var quotedColName = Visit(m.Object);
            string statement;

            switch (m.Method.Name)
            {
                case "Substring":
                    var startIndex = int.Parse(args[0].ToString()) + 1;
                    if (args.Count == 2)
                    {
                        var length = int.Parse(args[1].ToString());
                        statement = $"substr({quotedColName}, {startIndex}, {length})";
                    }
                    else
                        statement = $"substr({quotedColName}, {startIndex})";
                    break;
                default:
                    return base.VisitColumnAccessMethod(m);
            }
            return new PartialSqlString(statement);
        }

        protected override object VisitSqlMethodCall(MethodCallExpression m)
        {
            var args = this.VisitInSqlExpressionList(m.Arguments);
            object quotedColName = args[0];
            args.RemoveAt(0);

            var statement = "";

            switch (m.Method.Name)
            {
                case "In":
                    statement = ConvertInExpressionToSql(m, quotedColName);
                    break;
                case "Desc":
                    statement = $"{quotedColName} DESC";
                    break;
                case "As":
                    statement = $"{quotedColName} AS {base.DialectProvider.GetQuotedColumnName(RemoveQuoteFromAlias(args[0].ToString()))}";
                    break;
                case "Sum":
                case "Count":
                case "Min":
                case "Max":
                case "Avg":
                    statement = string.Format("{0}({1}{2})",
                        m.Method.Name,
                        quotedColName,
                        args.Count == 1 ? $",{args[0]}" : "");
                    break;
                case "CountDistinct":
                    statement = $"COUNT(DISTINCT {quotedColName})";
                    break;
                default:
                    return base.VisitSqlMethodCall(m);
            }

            return new PartialSqlString(statement);
        }

        protected override PartialSqlString ToLengthPartialString(object arg)
        {
            return new PartialSqlString($"LENGTH({arg})");
        }
    }
}
