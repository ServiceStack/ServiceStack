using System;
using System.Linq;

namespace ServiceStack.OrmLite.PostgreSQL;

public class PostgreSqlExpression<T> : SqlExpression<T>
{
    public PostgreSqlExpression(IOrmLiteDialectProvider dialectProvider)
        : base(dialectProvider) {}

    protected override string GetQuotedColumnName(ModelDefinition tableDef, string memberName)
    {
        if (useFieldName)
        {
            var fieldDef = tableDef.FieldDefinitions.FirstOrDefault(x => x.Name == memberName);
            if (fieldDef is { IsRowVersion: true } && !PrefixFieldWithTableName)
                return PostgreSqlDialectProvider.RowVersionFieldComparer;

            return base.GetQuotedColumnName(tableDef, memberName);
        }
        return memberName;
    }

    private static string JsonItem(object json, JsonPathExpression path) =>
        $"jsonb_path_query_first(CAST({json} AS jsonb), CAST({path} AS jsonpath))";

    protected override object VisitIsJsonMethod(object json) =>
        new PartialSqlString($"({json} IS JSON)");

    protected override object VisitJsonValueMethod(object json, JsonPathExpression path, Type returnType)
    {
        var item = JsonItem(json, path);
        var text = $"({item} #>> '{{}}')";
        var type = Nullable.GetUnderlyingType(returnType) ?? returnType;
        var jsonType = $"jsonb_typeof({item})";

        if (type == typeof(string) || type.IsEnum)
            return JsonScalar($"CASE WHEN {jsonType} NOT IN ('object','array') THEN {text} END", returnType);

        var requiredType = type == typeof(bool) ? "boolean"
            : type == typeof(char) || type == typeof(Guid) || type == typeof(DateTime)
              || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) ? "string"
            : "number";
        var cast = $"CAST({text} AS {GetJsonDbType(type)})";
        return JsonScalar($"CASE WHEN {jsonType} = '{requiredType}' THEN {cast} END", returnType);
    }

    protected override object VisitJsonQueryMethod(object json, JsonPathExpression path, Type returnType)
    {
        var item = JsonItem(json, path);
        return new PartialSqlString(
            $"CASE WHEN jsonb_typeof({item}) IN ('object','array') THEN CAST({item} AS text) END");
    }

    protected override object VisitJsonExistsMethod(object json, JsonPathExpression path) =>
        new PartialSqlString($"jsonb_path_exists(CAST({json} AS jsonb), CAST({path} AS jsonpath))");

    protected override object VisitJsonTypeMethod(object json, JsonPathExpression path)
    {
        var item = JsonItem(json, path);
        return JsonValueType($"CASE jsonb_typeof({item}) " +
            "WHEN 'null' THEN 'Null' WHEN 'string' THEN 'String' " +
            "WHEN 'number' THEN 'Number' WHEN 'boolean' THEN 'Boolean' " +
            "WHEN 'array' THEN 'Array' WHEN 'object' THEN 'Object' END");
    }

    protected override object VisitJsonArrayLengthMethod(object json, JsonPathExpression path)
    {
        var item = JsonItem(json, path);
        return new PartialSqlString(
            $"CASE WHEN jsonb_typeof({item}) = 'array' THEN jsonb_array_length({item}) END");
    }

    protected override object VisitJsonArrayContainsMethod(object json, JsonPathExpression path, object value, Type valueType)
    {
        var item = JsonItem(json, path);
        if (value.ToString() == "null")
            return new PartialSqlString($"({item} @> CAST('[null]' AS jsonb))");
        return new PartialSqlString($"({item} @> jsonb_build_array(to_jsonb({value})))");
    }

    protected override object VisitJsonContainsMethod(object json, JsonPathExpression path, object candidateJson)
    {
        var item = JsonItem(json, path);
        return new PartialSqlString($"({item} @> CAST({candidateJson} AS jsonb))");
    }
}
