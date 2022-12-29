using System;
using System.Data;
using ServiceStack.OrmLite.SqlServer.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServer
{
    public class SqlServerExpression<T> : SqlExpression<T>
    {
        public SqlServerExpression(IOrmLiteDialectProvider dialectProvider)
            : base(dialectProvider) {}

        public override void PrepareUpdateStatement(IDbCommand dbCmd, T item, bool excludeDefaults = false)
        {
            SqlServerExpressionUtils.PrepareSqlServerUpdateStatement(dbCmd, this, item, excludeDefaults);
        }

        public override string GetSubstringSql(object quotedColumn, int startIndex, int? length = null)
        {
            return length != null
                ? $"substring({quotedColumn}, {startIndex}, {length.Value})"
                : $"substring({quotedColumn}, {startIndex}, LEN({quotedColumn}) - {startIndex} + 1)";
        }

        protected override PartialSqlString ToLengthPartialString(object arg)
        {
            return new PartialSqlString($"LEN({arg})");
        }

        protected override void ConvertToPlaceholderAndParameter(ref object right)
        {
            var paramName = Params.Count.ToString();
            var paramValue = right;
            var parameter = CreateParam(paramName, paramValue);

            // Prevents a new plan cache for each different string length. Every string is parameterized as NVARCHAR(max) 
            if (parameter.DbType == System.Data.DbType.String)
                parameter.Size = -1;

            Params.Add(parameter);

            right = parameter.ParameterName;
        }

        protected override void VisitFilter(string operand, object originalLeft, object originalRight, ref object left, ref object right)
        {
            base.VisitFilter(operand, originalLeft, originalRight, ref left, ref right);

            if (originalRight is TimeSpan && DialectProvider.GetConverter<TimeSpan>() is SqlServerTimeConverter)
            {
                right = $"CAST({right} AS TIME)";
            }
        }

        public override string ToDeleteRowStatement()
        {
            return base.tableDefs.Count > 1
                ? $"DELETE {DialectProvider.GetQuotedTableName(modelDef)} {FromExpression} {WhereExpression}"
                : base.ToDeleteRowStatement();
        }
    }

    internal class SqlServerExpressionUtils
    {
        internal static void PrepareSqlServerUpdateStatement<T>(IDbCommand dbCmd, SqlExpression<T> q, T item, bool excludeDefaults = false)
        {
            q.CopyParamsTo(dbCmd);

            var modelDef = q.ModelDef;
            var dialectProvider = q.DialectProvider;

            var setFields = StringBuilderCache.Allocate();

            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (fieldDef.ShouldSkipUpdate()) continue;
                if (fieldDef.IsRowVersion) continue;
                if (q.UpdateFields.Count > 0
                    && !q.UpdateFields.Contains(fieldDef.Name)
                    || fieldDef.AutoIncrement)
                    continue; // added

                var value = fieldDef.GetValue(item);
                if (excludeDefaults
                    && (value == null || (!fieldDef.IsNullable && value.Equals(value.GetType().GetDefaultValue()))))
                    continue;

                if (setFields.Length > 0)
                    setFields.Append(", ");

                setFields
                    .Append(dialectProvider.GetQuotedColumnName(fieldDef.FieldName))
                    .Append("=")
                    .Append(dialectProvider.GetUpdateParam(dbCmd, value, fieldDef));
            }

            var strFields = StringBuilderCache.ReturnAndFree(setFields);
            if (strFields.Length == 0)
                throw new ArgumentException($"No non-null or non-default values were provided for type: {typeof(T).Name}");

            dbCmd.CommandText = $"UPDATE {dialectProvider.GetQuotedTableName(modelDef)} SET {strFields} {q.WhereExpression}";
        }
    }
}