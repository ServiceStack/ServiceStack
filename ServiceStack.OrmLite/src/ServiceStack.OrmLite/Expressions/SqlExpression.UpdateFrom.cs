using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ServiceStack.OrmLite
{
    public abstract partial class SqlExpression<T>
    {
        /// <summary>
        /// The UPDATE statement that sets the columns in the set expression on the rows of the query's table matching
        /// its joins and filters, where values can reference columns of any joined table, e.g:
        /// <para>(Order o, Customer c) =&gt; new Order { Region = c.Region }</para>
        /// Values of the set expression are added to the query's params.
        /// </summary>
        public virtual string ToUpdateFromStatement(LambdaExpression set)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));
            if (HasSetOperations || HasCommonTableExpression || HasTopPerGroup || IsForUpdate)
                throw new NotSupportedException("UpdateFrom() doesn't support set operations, CTEs, TopPerGroup() or ForUpdate()");
            if (!string.IsNullOrEmpty(GroupByExpression) || Offset != null || Rows != null)
                throw new NotSupportedException("UpdateFrom() doesn't support GroupBy(), Skip() or Take()");
            if (modelDef.PrimaryKey == null)
                throw new NotSupportedException($"UpdateFrom() requires {typeof(T).Name} to have a primary key");

            var values = GetUpdateFromValues(set);
            AddUpdateFromRuleValues(values);
            var sql = ToUpdateFromStatement(values);
            return SqlFilter != null
                ? SqlFilter(sql)
                : sql;
        }

        /// <summary>
        /// The connection's write rules for the table, whose OnUpdate values are added to an UpdateFrom()
        /// </summary>
        internal TableWriteRules UpdateFromRules { get; set; }

        private void AddUpdateFromRuleValues(List<KeyValuePair<FieldDefinition, string>> values)
        {
            if (UpdateFromRules == null)
                return;

            // Values from other columns can't be verified
            foreach (var rule in UpdateFromRules.EnsureWrites)
            {
                if (values.Exists(x => x.Key == rule.Field))
                    throw new NotSupportedException(
                        $"UpdateFrom() can't update '{rule.Field.Name}', which has an EnsureWrites rule on this connection");
            }
            foreach (var rule in UpdateFromRules.OnUpdate)
            {
                values.RemoveAll(x => x.Key == rule.Field);
                var value = DialectProvider.GetFieldValue(rule.Field, rule.GetValue());
                values.Add(new(rule.Field, AddParam(value).ParameterName));
            }
        }

        /// <summary>
        /// The SQL of the value of each column in the set expression, e.g. new Order { Region = c.Region }
        /// </summary>
        protected List<KeyValuePair<FieldDefinition, string>> GetUpdateFromValues(LambdaExpression set)
        {
            var body = set.Body;
            while (body is UnaryExpression { NodeType: ExpressionType.Convert } convert)
                body = convert.Operand;
            if (body is not MemberInitExpression { NewExpression.Arguments.Count: 0 } init || init.Type != typeof(T)
                || init.Bindings.Count == 0)
                throw new ArgumentException($"Expected columns to update like: new {typeof(T).Name} {{ Name = ... }}", nameof(set));

            var holdLambda = originalLambda;
            var holdSelect = isSelectExpression;
            originalLambda = set;
            isSelectExpression = false;
            try
            {
                var values = new List<KeyValuePair<FieldDefinition, string>>();
                foreach (var binding in init.Bindings)
                {
                    if (binding is not MemberAssignment assign)
                        throw new ArgumentException($"Expected columns to update like: new {typeof(T).Name} {{ Name = ... }}", nameof(set));
                    var fieldDef = modelDef.GetFieldDefinition(assign.Member.Name)
                        ?? throw new ArgumentException($"'{assign.Member.Name}' isn't a column of {typeof(T).Name}", nameof(set));
                    if (fieldDef.IsPrimaryKey)
                        throw new ArgumentException($"UpdateFrom() can't update the primary key '{fieldDef.Name}'", nameof(set));

                    var value = Visit(assign.Expression);
                    var sql = value is PartialSqlString or SelectItem
                        ? value.ToString()
                        : AddParam(DialectProvider.GetFieldValue(fieldDef, value)).ParameterName; // a constant or captured value
                    values.Add(new(fieldDef, sql));
                }
                return values;
            }
            finally
            {
                originalLambda = holdLambda;
                isSelectExpression = holdSelect;
            }
        }

        /// <summary>
        /// UPDATE from a derived table of the query's rows with their new values, joined on the primary key, which keeps
        /// the query's joins and filters as-is, e.g. for PostgreSQL and SQLite 3.33+:
        /// <para>UPDATE t SET a = u.v0 FROM (SELECT t.Id AS id, c.A AS v0 FROM t JOIN c ... WHERE ...) u WHERE t.Id = u.id</para>
        /// </summary>
        protected virtual string ToUpdateFromStatement(List<KeyValuePair<FieldDefinition, string>> values)
        {
            var table = DialectProvider.GetQuotedTableName(modelDef);
            var tableRef = TableAlias != null ? DialectProvider.GetQuotedName(TableAlias) : table;
            var pk = DialectProvider.GetQuotedColumnName(modelDef.PrimaryKey);
            var derived = DialectProvider.GetQuotedName("_u");
            var derivedId = DialectProvider.GetQuotedName("_id");

            var select = new StringBuilder().Append("SELECT ").Append(tableRef).Append('.').Append(pk).Append(" AS ").Append(derivedId);
            var set = new StringBuilder();
            for (var i = 0; i < values.Count; i++)
            {
                var column = DialectProvider.GetQuotedName("_v" + i);
                select.Append(", ").Append(values[i].Value).Append(" AS ").Append(column);
                set.Append(i > 0 ? ", " : "").Append(DialectProvider.GetQuotedColumnName(values[i].Key))
                    .Append(" = ").Append(derived).Append('.').Append(column);
            }

            return $"UPDATE {table} SET {set}\nFROM ({select}{BodyExpression}) {derived}\n" +
                   $"WHERE {table}.{pk} = {derived}.{derivedId}";
        }
    }
}
