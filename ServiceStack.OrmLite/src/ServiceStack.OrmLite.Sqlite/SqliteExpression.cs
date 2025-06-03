using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite.Sqlite;

public class SqliteExpression<T>(IOrmLiteDialectProvider dialectProvider) : SqlExpression<T>(dialectProvider)
{
    protected override object VisitColumnAccessMethod(MethodCallExpression m)
    {
        List<object> args = this.VisitExpressionList(m.Arguments);
        var quotedColName = Visit(m.Object);
        if (!IsSqlClass(quotedColName))
            quotedColName = ConvertToParam(quotedColName);
        string statement;

        if (m.Method.Name == nameof(string.ToString) && m.Object?.Type == typeof(DateTime))
        {
            var arg = args.Count > 0 ? args[0] : null;
            if (arg == null) statement = ToCast(quotedColName.ToString());
            else statement = $"strftime('{arg}',{quotedColName})";
            return new PartialSqlString(statement);
        }

        if (m.Method.Name == nameof(string.Substring))
        {
            var startIndex = int.Parse(args[0].ToString()) + 1;
            if (args.Count == 2)
            {
                var length = int.Parse(args[1].ToString());
                statement = $"substr({quotedColName}, {startIndex}, {length})";
            }
            else
                statement = $"substr({quotedColName}, {startIndex})";

            return new PartialSqlString(statement);
        }

        return base.VisitColumnAccessMethod(m);
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
                statement =
                    $"{quotedColName} AS {base.DialectProvider.GetQuotedColumnName(RemoveQuoteFromAlias(args[0].ToString()))}";
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