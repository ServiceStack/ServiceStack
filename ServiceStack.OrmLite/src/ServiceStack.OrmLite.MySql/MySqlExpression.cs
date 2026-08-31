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

    private static string JsonItem(object json, JsonPathExpression path) => $"JSON_EXTRACT({json}, {path})";

    protected override object VisitIsJsonMethod(object json) =>
        new PartialSqlString($"JSON_VALID({json})");

    protected override object VisitJsonValueMethod(object json, JsonPathExpression path, Type returnType)
    {
        var item = JsonItem(json, path);
        var text = $"JSON_UNQUOTE({item})";
        var type = Nullable.GetUnderlyingType(returnType) ?? returnType;
        var jsonType = $"JSON_TYPE({item})";

        if (type == typeof(string) || type.IsEnum)
            return JsonScalar($"CASE WHEN {jsonType} NOT IN ('OBJECT','ARRAY','NULL') THEN {text} END", returnType);

        if (type == typeof(bool))
            return JsonScalar(
                $"CASE WHEN {jsonType} = 'BOOLEAN' THEN CASE {text} WHEN 'true' THEN TRUE WHEN 'false' THEN FALSE END END",
                returnType);

        var requiredTypes = type == typeof(char) || type == typeof(Guid) || type == typeof(DateTime)
                            || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
            ? "('STRING')"
            : "('INTEGER','DOUBLE','DECIMAL')";
        var cast = $"CAST({text} AS {GetJsonCastType(type)})";
        return JsonScalar($"CASE WHEN {jsonType} IN {requiredTypes} THEN {cast} END", returnType);
    }

    private string GetJsonCastType(Type type)
    {
        if (type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long))
            return "SIGNED";
        if (type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong))
            return "UNSIGNED";
        if (type == typeof(float) || type == typeof(double))
            return "DOUBLE";
        if (type == typeof(decimal))
            return "DECIMAL(65,30)";

        return GetJsonDbType(type);
    }

    protected override object VisitJsonQueryMethod(object json, JsonPathExpression path, Type returnType)
    {
        var item = JsonItem(json, path);
        return new PartialSqlString(
            $"CASE WHEN JSON_TYPE({item}) IN ('OBJECT','ARRAY') THEN {item} END");
    }

    protected override object VisitJsonExistsMethod(object json, JsonPathExpression path) =>
        new PartialSqlString($"JSON_CONTAINS_PATH({json}, 'one', {path})");

    protected override object VisitJsonTypeMethod(object json, JsonPathExpression path)
    {
        var item = JsonItem(json, path);
        return JsonValueType($"CASE JSON_TYPE({item}) " +
            "WHEN 'NULL' THEN 'Null' WHEN 'STRING' THEN 'String' " +
            "WHEN 'INTEGER' THEN 'Number' WHEN 'DOUBLE' THEN 'Number' WHEN 'DECIMAL' THEN 'Number' " +
            "WHEN 'BOOLEAN' THEN 'Boolean' WHEN 'ARRAY' THEN 'Array' WHEN 'OBJECT' THEN 'Object' END");
    }

    protected override object VisitJsonArrayLengthMethod(object json, JsonPathExpression path)
    {
        var item = JsonItem(json, path);
        return new PartialSqlString(
            $"CASE WHEN JSON_TYPE({item}) = 'ARRAY' THEN JSON_LENGTH({item}) END");
    }

    protected override object VisitJsonArrayContainsMethod(object json, JsonPathExpression path, object value, Type valueType)
    {
        var item = JsonItem(json, path);
        return new PartialSqlString($"JSON_CONTAINS({item}, JSON_ARRAY({value}))");
    }

    protected override object VisitJsonContainsMethod(object json, JsonPathExpression path, object candidateJson)
    {
        var item = JsonItem(json, path);
        return new PartialSqlString($"JSON_CONTAINS({item}, JSON_EXTRACT({candidateJson}, '$'))");
    }
}
