using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;

namespace ServiceStack.OrmLite
{
    public abstract partial class SqlExpression<T>
    {
        private string withClause;

        /// <summary>
        /// Whether this query reads from a common table expression (CTE), e.g. from WithRecursive()
        /// </summary>
        public bool HasCommonTableExpression => withClause != null;

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
            if (withClause != null)
                throw new NotSupportedException("A query can only have one common table expression");

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

            withClause = $"{WithRecursiveKeyword} {quotedCte} ({columnNames}) AS (\n" +
                         $"{seedSql}\n" +
                         "UNION ALL\n" +
                         $"SELECT {GetCteColumns(quotedChild)} FROM {DialectProvider.GetQuotedTableName(modelDef)} {quotedChild} " +
                         $"INNER JOIN {quotedCte} {onCondition}\n)\n";

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

        private string PrefixWithClause(string sql) => withClause != null ? withClause + sql : sql;
    }
}
