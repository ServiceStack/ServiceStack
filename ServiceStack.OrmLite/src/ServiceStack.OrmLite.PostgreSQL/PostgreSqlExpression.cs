using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.OrmLite.PostgreSQL;

public class PostgreSqlExpression<T> : SqlExpression<T>
{
    public PostgreSqlExpression(IOrmLiteDialectProvider dialectProvider)
        : base(dialectProvider) {}

    private static readonly HashSet<Type> ArrayParamTypes = [
        typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal), typeof(string),
    ];

    /// <summary>
    /// Uses a single array param, i.e. "col = ANY(@0)", for IN lists larger than MaxInListParams
    /// </summary>
    protected override string CreateInListSql(object quotedColName, List<object> values)
    {
        if (DialectProvider.MaxInListParams > 0 && values.Count > DialectProvider.MaxInListParams && ToArrayParam(values) is { } array)
            return $"{quotedColName} = ANY({ConvertToParam(array)})";

        return base.CreateInListSql(quotedColName, values);
    }

    // Returns null if values aren't all non-null values of the same natively supported array type
    private static Array ToArrayParam(List<object> values)
    {
        var type = values[0]?.GetType();
        if (type == null || !ArrayParamTypes.Contains(type))
            return null;

        var array = Array.CreateInstance(type, values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value == null || value.GetType() != type)
                return null;
            array.SetValue(value, i);
        }
        return array;
    }

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

    // Arrays of strings and numbers are stored in PostgreSQL's array types, e.g. text[] of a string[]
    protected override bool IsArrayColumn(FieldDefinition fieldDef) =>
        (fieldDef.CustomFieldDefinition ?? DialectProvider.GetConverter(fieldDef.FieldType)?.ColumnDefinition)?.EndsWith("[]") == true;

    protected override object VisitArrayContains(object column, FieldDefinition fieldDef, object value) =>
        value.ToString() == "null"
            ? new PartialSqlString($"(array_position({column}, NULL) IS NOT NULL)")
            : new PartialSqlString($"({value} = ANY({column}))");

    protected override object VisitArrayLength(object column, FieldDefinition fieldDef) =>
        new PartialSqlString($"cardinality({column})");

    protected override string ArrayItemsFrom(object column, FieldDefinition fieldDef, string alias) =>
        $"unnest({column}) AS {alias}({DialectProvider.GetQuotedName("value")})";

    private static string JsonItem(object json, JsonPathExpression path) =>
        $"jsonb_path_query_first(CAST({json} AS jsonb), CAST({path} AS jsonpath))";

    // The rows of the items of an array, which are jsonb
    protected override string JsonArrayItemsFrom(object json, JsonPathExpression path, string alias) =>
        $"jsonb_path_query(CAST({json} AS jsonb), CAST({JsonArrayItemsPath(path)} AS jsonpath)) " +
        $"AS {alias}({DialectProvider.GetQuotedName("value")})";

    private string JsonArrayItemsPath(JsonPathExpression path) => path.Value != null
        ? QuoteJsonPath(path.Value + "[*]")
        : throw new NotSupportedException("Conditions on the items of JSON arrays need a constant JSON path.");

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
