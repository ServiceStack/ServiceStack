using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;

namespace ServiceStack.OrmLite
{
    public abstract partial class SqlExpression<T>
    {
        // Each common table expression (CTE) of the WITH clause by its quoted name, in the order they were added
        private List<KeyValuePair<string, string>> commonTableExpressions;
        private bool hasRecursiveCte;

        private string withClause
        {
            get
            {
                if (commonTableExpressions == null || commonTableExpressions.Count == 0)
                    return null;

                var sb = new StringBuilder(hasRecursiveCte ? WithRecursiveKeyword : "WITH").Append(' ');
                for (var i = 0; i < commonTableExpressions.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",\n");
                    sb.Append(commonTableExpressions[i].Value);
                }
                return sb.Append('\n').ToString();
            }
        }

        /// <summary>
        /// Whether this query has a common table expression (CTE), e.g. from With() or WithRecursive()
        /// </summary>
        public bool HasCommonTableExpression => commonTableExpressions is { Count: > 0 };

        private void AddCommonTableExpression(string quotedName, string definition)
        {
            commonTableExpressions ??= [];
            if (commonTableExpressions.Exists(x => string.Equals(x.Key, quotedName, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"This query already has a common table expression named {quotedName}");
            commonTableExpressions.Add(new(quotedName, definition));
        }

        /// <summary>
        /// Name a sub query as a common table expression (CTE) that's read like the table of <typeparamref name="TCte"/>,
        /// so this query can join it, filter by it and select from it with typed APIs, as often as it's needed, e.g:
        /// <para>db.From&lt;Book&gt;().With&lt;AuthorTotal&gt;(totals).Join&lt;AuthorTotal&gt;((b, t) =&gt; b.Author == t.Author)</para>
        /// </summary>
        /// <typeparam name="TCte">A class with a property for each column of the sub query, in the order it selects them</typeparam>
        /// <param name="subQuery">The query the CTE returns the rows of</param>
        public virtual SqlExpression<T> With<TCte>(ISqlExpression subQuery)
        {
            var cteDef = typeof(TCte).GetModelDefinition();
            if (cteDef.Schema != null)
                throw new NotSupportedException($"{typeof(TCte).Name} can't be a common table expression as it has a [Schema]");

            // Naming the columns matches them to the sub query's by position, whatever they're named or aliased as
            var columnNames = new StringBuilder();
            foreach (var fieldDef in cteDef.FieldDefinitions)
            {
                if (fieldDef.CustomSelect != null)
                    throw new NotSupportedException(
                        $"With() doesn't support {typeof(TCte).Name}.{fieldDef.Name} with a [CustomSelect]");
                if (columnNames.Length > 0)
                    columnNames.Append(", ");
                columnNames.Append(DialectProvider.GetQuotedColumnName(fieldDef));
            }
            return With(DialectProvider.GetQuotedTableName(cteDef), columnNames.ToString(), subQuery);
        }

        /// <summary>
        /// Name a sub query as a common table expression (CTE) that custom SQL in this query can read by its name, e.g:
        /// <para>db.From&lt;Book&gt;().With("recent", db.From&lt;Book&gt;().Where(x =&gt; x.Year &gt; 2000)).From("recent")</para>
        /// Use With&lt;TCte&gt;() to read it with typed APIs.
        /// </summary>
        public virtual SqlExpression<T> With(string name, ISqlExpression subQuery)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            return With(DialectProvider.GetQuotedName(name), null, subQuery);
        }

        private SqlExpression<T> With(string quotedName, string columnNames, ISqlExpression subQuery)
        {
            if (subQuery == null)
                throw new ArgumentNullException(nameof(subQuery));
            if (ReferenceEquals(subQuery, this))
                throw new ArgumentException("The sub query can't be this query, use a new query or Clone()", nameof(subQuery));

            var sql = subQuery.ToSelectStatement(QueryType.Select);
            if (sql.TrimStart().StartsWith("WITH ", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException(
                    "The sub query of a common table expression can't have its own, add them to this query before it instead");
            sql = AddRenamedParams(subQuery.Params, sql);

            AddCommonTableExpression(quotedName,
                quotedName + (columnNames != null ? $" ({columnNames})" : "") + $" AS (\n{sql}\n)");
            return this;
        }

        /// <summary>
        /// The WITH keyword for recursive CTEs, SQL Server and Oracle don't use RECURSIVE
        /// </summary>
        protected virtual string WithRecursiveKeyword => "WITH RECURSIVE";

        /// <summary>
        /// Query hierarchical data, e.g. a category and all its descendants, by reading from a recursive common table
        /// expression (CTE) that starts with the rows of <paramref name="seed"/> and repeatedly adds the rows matching
        /// <paramref name="recurse"/>, e.g:
        /// <para>db.From&lt;Category&gt;().WithRecursive(db.From&lt;Category&gt;().Where(x =&gt; x.Id == 1), (parent, child) =&gt; child.ParentId == parent.Id)</para>
        /// The rest of the query, e.g. Where(), OrderBy() and Select(), applies to all rows found.
        /// </summary>
        /// <param name="seed">The rows to start with, which must select all columns of T</param>
        /// <param name="recurse">Matches the next rows to add, where the first param is a row already found and the
        /// second is a row to add, e.g. descendants: (parent, child) =&gt; child.ParentId == parent.Id,
        /// ancestors: (child, parent) =&gt; parent.Id == child.ParentId</param>
        public virtual SqlExpression<T> WithRecursive(SqlExpression<T> seed, Expression<Func<T, T, bool>> recurse)
        {
            if (seed == null)
                throw new ArgumentNullException(nameof(seed));
            if (recurse == null)
                throw new ArgumentNullException(nameof(recurse));
            if (ReferenceEquals(seed, this))
                throw new ArgumentException("The seed query can't be this query, use a new query or Clone()", nameof(seed));
            if (hasRecursiveCte)
                throw new NotSupportedException("A query can only have one recursive common table expression");

            const string CteName = "cte";
            const string ChildAlias = "c";
            var quotedCte = DialectProvider.GetQuotedName(CteName);
            var quotedChild = DialectProvider.GetQuotedName(ChildAlias);

            // Rows to start with, selecting all columns in the same order as the recursive step
            var seedQuery = seed.Clone();
            seedQuery.Select(GetCteColumns(seedQuery.PrefixFieldWithTableName ? DialectProvider.GetQuotedTableName(modelDef) : null));
            var seedSql = seedQuery.ToSetOperandStatement("seed");
            seedSql = AddRenamedParams(seedQuery.Params, seedSql);

            // Next rows to add: SELECT c.* FROM Table c INNER JOIN cte ON {recurse}
            var joinQuery = DialectProvider.SqlExpression<T>();
            joinQuery.TableAlias = CteName;
            joinQuery.PrefixFieldWithTableName = true;
            joinQuery.joinAlias = new TableOptions {
                Alias = ChildAlias,
                ModelDef = modelDef,
                ParamName = recurse.Parameters[1].Name,
            };
            joinQuery.Reset(); // render column names, as InternalJoin() does
            var onCondition = joinQuery.InternalCreateSqlFromExpression(recurse, isCrossJoin: false);

            // Filter the rows added by the recursive step with the connection's filters, so it can't walk into
            // filtered out rows
            List<IDbDataParameter> recursiveFilterParams = null;
            var recursiveFilter = ConnectionFilters?.ToFilterCondition(DialectProvider, typeof(T), ChildAlias,
                paramPrefix: "", out recursiveFilterParams);
            if (recursiveFilter != null)
                onCondition += " WHERE " + AddRenamedParams(recursiveFilterParams, recursiveFilter);

            var columnNames = new StringBuilder();
            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (columnNames.Length > 0)
                    columnNames.Append(", ");
                columnNames.Append(DialectProvider.GetQuotedColumnName(fieldDef));
            }

            AddCommonTableExpression(quotedCte, $"{quotedCte} ({columnNames}) AS (\n" +
                         $"{seedSql}\n" +
                         "UNION ALL\n" +
                         $"SELECT {GetCteColumns(quotedChild)} FROM {DialectProvider.GetQuotedTableName(modelDef)} {quotedChild} " +
                         $"INNER JOIN {quotedCte} {onCondition}\n)");
            hasRecursiveCte = true;

            // Read from the CTE aliased as the model's table so the rest of the query is unchanged
            FromExpression = " \nFROM " + quotedCte + " " + DialectProvider.GetQuotedName(DialectProvider.NamingStrategy.GetTableName(modelDef));
            return this;
        }

        /// <summary>
        /// All columns of the model in definition order, optionally prefixed with a quoted table or alias
        /// </summary>
        private string GetCteColumns(string quotedPrefix)
        {
            var sb = new StringBuilder();
            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (fieldDef.CustomSelect != null)
                    throw new NotSupportedException(
                        $"WithRecursive() doesn't support {typeof(T).Name}.{fieldDef.Name} with a [CustomSelect]");
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(quotedPrefix != null
                    ? quotedPrefix + "." + DialectProvider.GetQuotedColumnName(fieldDef)
                    : DialectProvider.GetQuotedColumnName(fieldDef));
            }
            return sb.ToString();
        }

        private string PrefixWithClause(string sql) => withClause is { } with ? with + sql : sql;
    }
}
