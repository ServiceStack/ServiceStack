namespace ServiceStack.OrmLite.MySql;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
public class MySqlExpression<T>(IOrmLiteDialectProvider dialectProvider) : SqlExpression<T>(dialectProvider)
{
    protected override string ToCast(string quotedColName)
    {
        return $"cast({quotedColName} as char(1000))";
    }

    public override string ToDeleteRowStatement()
    {
        return base.tableDefs.Count > 1
            ? $"DELETE {DialectProvider.GetQuotedTableName(modelDef)} {FromExpression} {WhereExpression}"
            : base.ToDeleteRowStatement();
    }

    protected override object VisitColumnAccessMethod(MethodCallExpression m)
    {
        if (m.Method.Name == nameof(ToString) && m.Object?.Type == typeof(DateTime))
        {
            List<object> args = this.VisitExpressionList(m.Arguments);
            var quotedColName = Visit(m.Object);
            if (!IsSqlClass(quotedColName))
                quotedColName = ConvertToParam(quotedColName);

            var statement = "";
            var arg = args.Count > 0 ? args[0] : null;
            if (arg == null) statement = ToCast(quotedColName.ToString());
            else statement = $"DATE_FORMAT({quotedColName.ToString()},'{arg.ToString()}')";
            return new PartialSqlString(statement);
        }
        return base.VisitColumnAccessMethod(m);
    }

    protected override string CreateInSubQuerySql(object quotedColName, string subSelect)
    {
        return $"{quotedColName} IN (SELECT * FROM ({subSelect})  SubQuery)";
    }
}