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

        // The parts of the recursive CTE, which has more columns when the query uses its depth or detects cycles
        private sealed class RecursiveCte
        {
            public string Name;
            public string ColumnNames;
            /// <summary>The seed's SELECT, with SeedColumnsMarker after its columns</summary>
            public string SeedSql;
            public string SeedPrimaryKey;
            public string StepSelect;
            public string StepFrom;
            public string StepFilter;
            public string StepPrimaryKey;
            public int? MaxDepth;
            public bool DetectCycles;
        }
        private RecursiveCte recursiveCte;
        private bool usesRecursiveDepth;
        private bool hasRecursiveCte => recursiveCte != null;

        private const string RecursiveDepthColumn = "cte_depth";
        private const string RecursivePathColumn = "cte_path";
        private const string SeedColumnsMarker = "/*cte_seed*/";

        // Sql.RecursiveDepth(): how many levels a row is from the rows the recursive query started with
        private PartialSqlString VisitRecursiveDepth()
        {
            usesRecursiveDepth = true;
            return new PartialSqlString(DialectProvider.GetQuotedName(RecursiveDepthColumn));
        }

        private string ToRecursiveCteSql(RecursiveCte cte)
        {
            var extraColumns = "";
            var seedColumns = "";
            var stepColumns = "";
            var stepConditions = new List<string>();
            if (cte.StepFilter != null)
                stepConditions.Add(cte.StepFilter);

            if (cte.MaxDepth != null || usesRecursiveDepth)
            {
                // Rows the query starts with are at depth 0, and each step adds the rows a level below them
                var depth = DialectProvider.GetQuotedName(RecursiveDepthColumn);
                extraColumns += ", " + depth;
                seedColumns += $", 0 AS {depth}";
                stepColumns += $", {cte.Name}.{depth} + 1";
                if (cte.MaxDepth != null)
                    stepConditions.Add($"{cte.Name}.{depth} < {cte.MaxDepth.Value}");
            }

            if (cte.DetectCycles)
            {
                // Each row has the path of ids that lead to it, e.g. /1/4/9/, and a row already in it isn't added
                // again. Both parts of the CTE cast the path to the same type, as some RDBMS require.
                var path = DialectProvider.GetQuotedName(RecursivePathColumn);
                string Text(string sql) => DialectProvider.SqlCast(sql, Sql.VARCHAR);
                var stepId = Text(cte.StepPrimaryKey);
                extraColumns += ", " + path;
                seedColumns += ", " + Text(DialectProvider.SqlConcat(new object[] { "'/'", Text(cte.SeedPrimaryKey), "'/'" })) + " AS " + path;
                stepColumns += ", " + Text(DialectProvider.SqlConcat(new object[] { $"{cte.Name}.{path}", stepId, "'/'" }));
                stepConditions.Add($"{cte.Name}.{path} NOT LIKE " + DialectProvider.SqlConcat(new object[] { "'%/'", stepId, "'/%'" }));
            }

            return $"{cte.Name} ({cte.ColumnNames}{extraColumns}) AS (\n" +
                   $"{cte.SeedSql.Replace(SeedColumnsMarker, seedColumns)}\n" +
                   "UNION ALL\n" +
                   $"{cte.StepSelect}{stepColumns}{cte.StepFrom}" +
                   (stepConditions.Count > 0 ? " WHERE " + string.Join(" AND ", stepConditions) : "") +
                   "\n)";
        }

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
                    // The recursive CTE is rendered when it's used, as the rest of the query decides its columns
                    sb.Append(commonTableExpressions[i].Value ?? ToRecursiveCteSql(recursiveCte));
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
        /// <param name="maxDepth">How many levels of rows to add to the seed rows, e.g. 1 for only their children.
        /// Use Sql.RecursiveDepth() in the rest of the query for the level of each row.</param>
        /// <param name="detectCycles">Stop when a row is reached again by the rows that lead to it, for data that
        /// can have loops, e.g. a category that's its own ancestor</param>
        public virtual SqlExpression<T> WithRecursive(SqlExpression<T> seed, Expression<Func<T, T, bool>> recurse,
            int? maxDepth = null, bool detectCycles = false)
        {
            if (maxDepth < 0)
                throw new ArgumentOutOfRangeException(nameof(maxDepth), "maxDepth can't be negative");
            if (detectCycles && modelDef.PrimaryKey == null)
                throw new NotSupportedException($"{typeof(T).Name} needs a primary key to detect cycles");

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
            var seedPrefix = seedQuery.PrefixFieldWithTableName ? DialectProvider.GetQuotedTableName(modelDef) : null;
            seedQuery.UnsafeSelect(GetCteColumns(seedPrefix) + SeedColumnsMarker);
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
            CompiledQueryBuild.FilterTable(typeof(T));
            var recursiveFilter = ConnectionFilters?.ToFilterCondition(DialectProvider, typeof(T), ChildAlias,
                paramPrefix: "", out recursiveFilterParams);
            if (recursiveFilter != null)
                recursiveFilter = AddRenamedParams(recursiveFilterParams, recursiveFilter);

            var columnNames = new StringBuilder();
            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (columnNames.Length > 0)
                    columnNames.Append(", ");
                columnNames.Append(DialectProvider.GetQuotedColumnName(fieldDef));
            }

            string PrimaryKey(string quotedPrefix) => modelDef.PrimaryKey == null
                ? null
                : (quotedPrefix != null ? quotedPrefix + "." : "") + DialectProvider.GetQuotedColumnName(modelDef.PrimaryKey);

            AddCommonTableExpression(quotedCte, definition: null);
            recursiveCte = new RecursiveCte {
                Name = quotedCte,
                ColumnNames = columnNames.ToString(),
                SeedSql = seedSql,
                SeedPrimaryKey = PrimaryKey(seedPrefix),
                StepSelect = $"SELECT {GetCteColumns(quotedChild)}",
                StepFrom = $" FROM {DialectProvider.GetQuotedTableName(modelDef)} {quotedChild} INNER JOIN {quotedCte} {onCondition}",
                StepFilter = recursiveFilter,
                StepPrimaryKey = PrimaryKey(quotedChild),
                MaxDepth = maxDepth,
                DetectCycles = detectCycles,
            };

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

        private string PrefixWithClause(string sql)
        {
            if (usesRecursiveDepth && recursiveCte == null)
                throw new InvalidOperationException("Sql.RecursiveDepth() can only be used in a query with WithRecursive()");
            return withClause is { } with ? with + sql : sql;
        }
    }
}
