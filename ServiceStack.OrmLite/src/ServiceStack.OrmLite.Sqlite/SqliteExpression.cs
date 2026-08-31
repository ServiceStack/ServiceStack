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

    protected override object VisitIsJsonMethod(object json) =>
        new PartialSqlString($"json_valid({json})");

    protected override object VisitJsonValueMethod(object json, JsonPathExpression path, Type returnType)
    {
        var extract = $"json_extract({json}, {path})";
        var jsonType = $"json_type({json}, {path})";
        var type = Nullable.GetUnderlyingType(returnType) ?? returnType;

        if (type == typeof(string) || type.IsEnum)
            return JsonScalar(
                $"CASE WHEN {jsonType} NOT IN ('object','array','null') THEN {extract} END", returnType);

        if (type == typeof(bool))
            return JsonScalar(
                $"CASE WHEN {jsonType} IN ('true','false') THEN {extract} END", returnType);

        if (type == typeof(char) || type == typeof(Guid) || type == typeof(DateTime)
            || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
            return JsonScalar(
                $"CASE WHEN {jsonType} = 'text' THEN {extract} END", returnType);

        return JsonScalar(
            $"CAST(CASE WHEN {jsonType} IN ('integer','real') THEN {extract} END AS {GetJsonDbType(type)})", returnType);
    }

    protected override object VisitJsonQueryMethod(object json, JsonPathExpression path, Type returnType)
    {
        var extract = $"json_extract({json}, {path})";
        return new PartialSqlString(
            $"CASE WHEN json_type({json}, {path}) IN ('object','array') THEN {extract} END");
    }

    protected override object VisitJsonExistsMethod(object json, JsonPathExpression path) =>
        new PartialSqlString($"(json_type({json}, {path}) IS NOT NULL)");

    protected override object VisitJsonTypeMethod(object json, JsonPathExpression path) => JsonValueType(
        $"CASE json_type({json}, {path}) " +
        "WHEN 'null' THEN 'Null' WHEN 'text' THEN 'String' " +
        "WHEN 'integer' THEN 'Number' WHEN 'real' THEN 'Number' " +
        "WHEN 'true' THEN 'Boolean' WHEN 'false' THEN 'Boolean' " +
        "WHEN 'array' THEN 'Array' WHEN 'object' THEN 'Object' END");

    protected override object VisitJsonArrayLengthMethod(object json, JsonPathExpression path) =>
        new PartialSqlString(
            $"CASE WHEN json_type({json}, {path}) = 'array' THEN json_array_length({json}, {path}) END");

    protected override object VisitJsonArrayContainsMethod(object json, JsonPathExpression path, object value, Type valueType)
    {
        valueType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        string typePredicate;
        if (value.ToString() == "null")
            return new PartialSqlString(
                $"EXISTS (SELECT 1 FROM json_each({json}, {path}) j WHERE j.type = 'null')");
        if (valueType == typeof(bool))
            typePredicate = "j.type IN ('true','false')";
        else if (valueType == typeof(string) || valueType == typeof(char) || valueType == typeof(Guid)
                 || valueType == typeof(DateTime) || valueType == typeof(DateTimeOffset) || valueType.IsEnum)
            typePredicate = "j.type = 'text'";
        else
            typePredicate = "j.type IN ('integer','real')";

        return new PartialSqlString(
            $"EXISTS (SELECT 1 FROM json_each({json}, {path}) j WHERE {typePredicate} AND j.atom = {value})");
    }

    protected override PartialSqlString ToLengthPartialString(object arg)
    {
        return new PartialSqlString($"LENGTH({arg})");
    }
}
