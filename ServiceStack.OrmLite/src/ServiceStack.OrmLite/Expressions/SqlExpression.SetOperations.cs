using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    public abstract partial class SqlExpression<T>
    {
        internal sealed class SetOperation(string op, ISqlExpression query)
        {
            public string Op { get; } = op;
            public ISqlExpression Query { get; } = query;
        }

        private List<SetOperation> setOperations;
        private List<IDbDataParameter> setOperationParams;

        /// <summary>
        /// Whether this query is combined with other queries with Union(), UnionAll(), Intersect() or Except()
        /// </summary>
        public bool HasSetOperations => setOperations is { Count: > 0 };

        /// <summary>
        /// Combine the distinct rows of this query with the results of another query, e.g:
        /// <para>db.From&lt;Customer&gt;().Select(x =&gt; x.Email).Union(db.From&lt;Lead&gt;().Select(x =&gt; x.Email))</para>
        /// Both queries must select the same number of compatible columns. ORDER BY, Skip() and Take() on this
        /// query apply to the combined results.
        /// </summary>
        public virtual SqlExpression<T> Union(ISqlExpression query) => AddSetOperation("UNION", query);

        /// <summary>
        /// Combine all rows of this query with the results of another query, including duplicates
        /// </summary>
        public virtual SqlExpression<T> UnionAll(ISqlExpression query) => AddSetOperation("UNION ALL", query);

        /// <summary>
        /// Only return distinct rows returned by both this query and another query
        /// </summary>
        public virtual SqlExpression<T> Intersect(ISqlExpression query) => AddSetOperation("INTERSECT", query);

        /// <summary>
        /// Only return distinct rows of this query that aren't returned by another query
        /// </summary>
        public virtual SqlExpression<T> Except(ISqlExpression query) => AddSetOperation("EXCEPT", query);

        protected virtual SqlExpression<T> AddSetOperation(string op, ISqlExpression query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
            if (ReferenceEquals(query, this))
                throw new ArgumentException("A query can't be combined with itself, use a Clone() instead", nameof(query));

            GetSetOperator(op); // fail fast if the RDBMS doesn't support it
            (setOperations ??= new List<SetOperation>()).Add(new SetOperation(op, query));
            return this;
        }

        /// <summary>
        /// The dialect's SQL for a set operation, e.g. Oracle uses MINUS for EXCEPT
        /// </summary>
        protected virtual string GetSetOperator(string op) => op;

        public virtual string ToSetOperandStatement(string alias)
        {
            if (HasSetOperations || Offset != null || Rows != null)
                return "SELECT * FROM (" + ToSelectStatement(QueryType.Select) + ") " + alias;

            SelectFilter?.Invoke(this);
            OrmLiteConfig.SqlExpressionSelectFilter?.Invoke(GetUntyped());

            var sql = DialectProvider.ToSelectStatement(QueryType.Select, modelDef, SelectExpression, BodyExpression);
            return SqlFilter != null
                ? SqlFilter(sql)
                : sql;
        }

        private void RemoveSetOperationParams()
        {
            if (setOperationParams == null || setOperationParams.Count == 0)
                return;
            foreach (var p in setOperationParams)
                Params.Remove(p);
            setOperationParams.Clear();
        }

        /// <summary>
        /// Returns this query's SELECT combined with its set operations, without any ORDER BY or limits. The params
        /// of the other queries are renamed and added to this query's params, replacing any added previously.
        /// </summary>
        private string ToSetOperationsSql()
        {
            RemoveSetOperationParams();

            var sb = new StringBuilder(
                DialectProvider.ToSelectStatement(QueryType.Select, modelDef, SelectExpression, BodyExpression));

            for (var i = 0; i < setOperations.Count; i++)
            {
                var setOp = setOperations[i];
                var alias = "q" + (i + 1);
                var sql = setOp.Query.ToSetOperandStatement(alias);

                sql = AddRenamedParams(setOp.Query.Params, sql, setOperationParams ??= new List<IDbDataParameter>());

                sb.Append('\n').Append(GetSetOperator(setOp.Op)).Append('\n').Append(sql);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Adds renamed copies of another query's params to this query, e.g. a sub query or combined query, replacing
        /// their names in its SQL in a single pass so a renamed param can't be renamed again, e.g. @0 =&gt; @1 and
        /// @1 =&gt; @2, and only whole names are replaced, e.g. renaming @min doesn't affect @minRating.
        /// </summary>
        /// <param name="addedTo">Also records the added params, e.g. to remove them when regenerating the SQL</param>
        protected string AddRenamedParams(List<IDbDataParameter> otherParams, string sql, List<IDbDataParameter> addedTo = null)
        {
            if (otherParams == null || otherParams.Count == 0)
                return sql;

            // Keyed by name without its prefix as params can be referenced with '@' or the dialect's ParamString, e.g. ':'
            var renames = new Dictionary<string, string>();
            foreach (var p in otherParams)
            {
                var pClone = DialectProvider.CreateParam().PopulateWith(p);
                pClone.ParameterName = DialectProvider.GetParam(Params.Count.ToString());
                renames[StripParamPrefix(p.ParameterName)] = pClone.ParameterName;
                Params.Add(pClone);
                addedTo?.Add(pClone);
            }

            // Match quoted literals first so param-like text within them is left as-is, e.g. '10:30'
            var prefixes = Regex.Escape("@") + (DialectProvider.ParamString == "@" ? "" : "|" + Regex.Escape(DialectProvider.ParamString));
            var pattern = "'(?:[^']|'')*'|(?<![\\w@:?])(?:" + prefixes + ")(\\w+)";
            return Regex.Replace(sql, pattern, m => m.Groups[1].Success && renames.TryGetValue(m.Groups[1].Value, out var newName) 
                ? newName 
                : m.Value);
        }

        private string StripParamPrefix(string paramName)
        {
            if (paramName.StartsWith(DialectProvider.ParamString, StringComparison.Ordinal))
                return paramName.Substring(DialectProvider.ParamString.Length);
            return paramName.StartsWith("@", StringComparison.Ordinal) ? paramName.Substring(1) : paramName;
        }

        private string ToSetOperationsSelectStatement(QueryType forType)
        {
            var sql = ToSetOperationsSql();

            var orderBy = OrderByExpression;
            if (string.IsNullOrEmpty(orderBy) && Offset == null && Rows == null && Tags.Count == 0)
                return sql;

            // Offset paging requires an ORDER BY in some RDBMS, e.g. SQL Server, order by the first column if unspecified
            if (string.IsNullOrEmpty(orderBy) && Offset is > 0)
                orderBy = "\nORDER BY 1";

            return DialectProvider.ToSelectStatement(forType, modelDef, "SELECT *", " \nFROM (" + sql + ") q",
                orderBy, offset: Offset, rows: Rows, Tags);
        }

        private string ToSetOperationsCountStatement() =>
            "SELECT COUNT(*) FROM (" + ToSetOperationsSql() + ") q";

        private void DumpSetOperations(StringBuilder sb, bool includeParams)
        {
            if (!HasSetOperations)
                return;
            foreach (var setOp in setOperations)
            {
                sb.AppendLine(setOp.Op);
                sb.AppendLine(setOp.Query.Dump(includeParams));
            }
        }
    }
}
