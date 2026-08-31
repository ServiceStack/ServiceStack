using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite.SqlServer
{
    public class SqlServer2016Expression<T> : SqlServerExpression<T>
    {
        public SqlServer2016Expression(IOrmLiteDialectProvider dialectProvider)
            : base(dialectProvider) {}

        protected override object VisitSqlMethodCall(MethodCallExpression m)
        {
            List<object> args = VisitInSqlExpressionList(m.Arguments);
            object quotedColName = args[0];
            args.RemoveAt(0);

            string statement;

            switch (m.Method.Name)
            {
                case nameof(Sql.In):
                    statement = ConvertInExpressionToSql(m, quotedColName);
                    break;
                case nameof(Sql.Desc):
                    statement = $"{quotedColName} DESC";
                    break;
                case nameof(Sql.As):
                    statement = $"{quotedColName} AS {DialectProvider.GetQuotedColumnName(RemoveQuoteFromAlias(args[0].ToString()))}";
                    break;
                case nameof(Sql.Cast):
                    statement = DialectProvider.SqlCast(quotedColName, args[0].ToString());
                    break;
                case nameof(Sql.Sum):
                case nameof(Sql.Count):
                case nameof(Sql.Min):
                case nameof(Sql.Max):
                case nameof(Sql.Avg):
                    statement = $"{m.Method.Name}({quotedColName}{(args.Count == 1 ? $",{args[0]}" : "")})";
                    break;
                case nameof(Sql.CountDistinct):
                    statement = $"COUNT(DISTINCT {quotedColName})";
                    break;
                case nameof(Sql.AllFields):
                    var argDef = m.Arguments[0].Type.GetModelMetadata();
                    statement = DialectProvider.GetQuotedTableName(argDef) + ".*";
                    break;
                case nameof(Sql.JoinAlias):
                case nameof(Sql.TableAlias):
                    statement = args[0] + "." + quotedColName.ToString().LastRightPart('.');
                    break;
                case nameof(Sql.Custom):
                    statement = quotedColName.ToString();
                    break;
                default:
                    return base.VisitSqlMethodCall(m);
            }

            return new PartialSqlString(statement);
        }

        private object BooleanJsonExpression(string predicate, string scalar)
        {
            return new PartialSqlString(isSelectExpression
                ? scalar
                : predicate);
        }

        protected override object VisitIsJsonMethod(object json)
        {
            var scalar = DialectProvider is SqlServer2022OrmLiteDialectProvider
                ? $"ISJSON({json}, VALUE)"
                : $"ISJSON({json})";
            return BooleanJsonExpression($"({scalar} = 1)", scalar);
        }

        protected override object VisitJsonValueMethod(object json, JsonPathExpression path, Type returnType)
        {
            var value = $"JSON_VALUE({json}, {path})";
            var type = Nullable.GetUnderlyingType(returnType) ?? returnType;
            if (type == typeof(string) || type.IsEnum)
                return JsonScalar(value, returnType);

            if (type == typeof(bool))
            {
                var boolean = $"CASE {value} WHEN 'true' THEN CAST(1 AS bit) WHEN 'false' THEN CAST(0 AS bit) END";
                return JsonScalar(boolean, returnType);
            }

            return JsonScalar($"TRY_CONVERT({GetJsonDbType(type)}, {value})", returnType);
        }

        protected override object VisitJsonQueryMethod(object json, JsonPathExpression path, Type returnType) =>
            new PartialSqlString($"JSON_QUERY({json}, {path})");

        protected override object VisitJsonExistsMethod(object json, JsonPathExpression path)
        {
            if (DialectProvider is not SqlServer2022OrmLiteDialectProvider)
                throw new NotSupportedException("Sql.JsonExists() requires SQL Server 2022 or later.");

            var scalar = $"JSON_PATH_EXISTS({json}, {path})";
            return BooleanJsonExpression($"({scalar} = 1)", scalar);
        }

        protected override object VisitJsonTypeMethod(object json, JsonPathExpression path)
        {
            var typeCode = JsonTypeCode(json, path);
            var sql = "(SELECT CASE " + typeCode + " " +
                      "WHEN 0 THEN 'Null' WHEN 1 THEN 'String' WHEN 2 THEN 'Number' " +
                      "WHEN 3 THEN 'Boolean' WHEN 4 THEN 'Array' WHEN 5 THEN 'Object' END)";
            return JsonValueType(sql);
        }

        private string JsonTypeCode(object json, JsonPathExpression path)
        {
            if (path.Value == null)
                throw new NotSupportedException("SQL Server Sql.JsonType() requires a constant or captured JSON path.");

            if (IsRootJsonPath(path.Value))
                return $"(SELECT TOP 1 [type] FROM OPENJSON(CONCAT('[', {json}, ']')))";

            if (!TrySplitJsonPath(path.Value, out var parentPath, out var key))
                throw new NotSupportedException($"SQL Server Sql.JsonType() does not support JSON path '{path.Value}'.");

            var parent = QuoteJsonPath(parentPath);
            var quotedKey = DialectProvider.GetQuotedValue(key, typeof(string));
            return $"(SELECT TOP 1 [type] FROM OPENJSON({json}, {parent}) WHERE [key] = {quotedKey})";
        }

        private static bool IsRootJsonPath(string path)
        {
            path = path.Trim();
            return path == "$" || path == "lax $" || path == "strict $";
        }

        private static bool TrySplitJsonPath(string path, out string parentPath, out string key)
        {
            parentPath = null;
            key = null;
            path = path.Trim();

            var mode = "";
            if (path.StartsWith("lax ", StringComparison.OrdinalIgnoreCase))
            {
                mode = path.Substring(0, 4);
                path = path.Substring(4).TrimStart();
            }
            else if (path.StartsWith("strict ", StringComparison.OrdinalIgnoreCase))
            {
                mode = path.Substring(0, 7);
                path = path.Substring(7).TrimStart();
            }

            if (!path.StartsWith("$", StringComparison.Ordinal))
                return false;

            var inQuotes = false;
            var escaped = false;
            var delimiter = -1;
            for (var i = 1; i < path.Length; i++)
            {
                var c = path[i];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }
                if (inQuotes && c == '\\')
                {
                    escaped = true;
                    continue;
                }
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                if (!inQuotes && (c == '.' || c == '['))
                    delimiter = i;
            }

            if (inQuotes || delimiter < 1)
                return false;

            var token = path.Substring(delimiter);
            if (token[0] == '.')
            {
                token = token.Substring(1);
                if (token.Length >= 2 && token[0] == '"' && token[token.Length - 1] == '"')
                    token = token.Substring(1, token.Length - 2)
                        .Replace("\\\"", "\"")
                        .Replace("\\\\", "\\");
                else if (token.Length == 0)
                    return false;
            }
            else
            {
                if (token[token.Length - 1] != ']')
                    return false;
                token = token.Substring(1, token.Length - 2);
                if (!int.TryParse(token, out var index) || index < 0)
                    return false;
                token = index.ToString();
            }

            parentPath = mode + path.Substring(0, delimiter);
            key = token;
            return true;
        }

        protected override object VisitJsonArrayLengthMethod(object json, JsonPathExpression path)
        {
            var query = $"JSON_QUERY({json}, {path})";
            return new PartialSqlString(
                $"CASE WHEN LEFT(LTRIM({query}), 1) = '[' THEN (SELECT COUNT(*) FROM OPENJSON({json}, {path})) END");
        }

        protected override object VisitJsonArrayContainsMethod(object json, JsonPathExpression path, object value, Type valueType)
        {
            valueType = Nullable.GetUnderlyingType(valueType) ?? valueType;
            string comparison;
            if (value.ToString() == "null")
            {
                comparison = "j.[type] = 0";
            }
            else if (valueType == typeof(bool))
            {
                comparison = $"j.[type] = 3 AND j.[value] = CASE WHEN {value} = 1 THEN 'true' ELSE 'false' END";
            }
            else if (valueType == typeof(string) || valueType == typeof(char) || valueType == typeof(Guid)
                     || valueType == typeof(DateTime) || valueType == typeof(DateTimeOffset) || valueType.IsEnum)
            {
                comparison = $"j.[type] = 1 AND j.[value] = CONVERT(nvarchar(max), {value})";
            }
            else
            {
                comparison = $"j.[type] = 2 AND TRY_CONVERT({GetJsonDbType(valueType)}, j.[value]) = {value}";
            }

            var predicate = $"EXISTS (SELECT 1 FROM OPENJSON({json}, {path}) j WHERE {comparison})";
            var scalar = $"CASE WHEN {predicate} THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END";
            return BooleanJsonExpression(predicate, scalar);
        }
    }
}
