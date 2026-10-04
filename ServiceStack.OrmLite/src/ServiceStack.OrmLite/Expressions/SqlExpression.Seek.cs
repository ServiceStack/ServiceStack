using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.OrmLite
{
    public abstract partial class SqlExpression<T>
    {
        /// <summary>
        /// Keyset (seek) pagination: only return rows after the last row of the previous page, based on the query's
        /// ORDER BY, which must be specified first and should end with a unique column like the primary key, e.g:
        /// <para>db.From&lt;Order&gt;().OrderByDescending(x =&gt; x.CreatedDate).ThenBy(x =&gt; x.Id).SeekAfter(lastRow).Take(50)</para>
        /// Unlike Skip(), rows before the page aren't read, so pages are fast at any depth and don't skip or repeat
        /// rows when rows are added or removed between requests.
        /// </summary>
        /// <param name="lastRow">The last row of the previous page, its ORDER BY column values are used</param>
        public virtual SqlExpression<T> SeekAfter(T lastRow)
        {
            if (lastRow == null)
                throw new ArgumentNullException(nameof(lastRow));

            var terms = GetSeekOrderByTerms();
            var values = new object[terms.Count];
            for (var i = 0; i < terms.Count; i++)
            {
                var fieldDef = FindOrderByField(terms[i].Column)
                    ?? throw new NotSupportedException(
                        $"Can't resolve ORDER BY '{terms[i].Column}' to a {typeof(T).Name} property, use SeekAfter(params object[] values) instead");
                values[i] = fieldDef.GetValue(lastRow);
            }
            return AddSeekCondition(terms, values);
        }

        /// <summary>
        /// Keyset (seek) pagination: only return rows after the specified values of the query's ORDER BY columns, e.g:
        /// <para>db.From&lt;Order&gt;().OrderByDescending(x =&gt; x.CreatedDate).ThenBy(x =&gt; x.Id).SeekAfter(lastCreatedDate, lastId)</para>
        /// </summary>
        /// <param name="values">The last row's values for each ORDER BY column, in the same order</param>
        public virtual SqlExpression<T> SeekAfter(params object[] values)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("SeekAfter() requires a value for each ORDER BY column", nameof(values));

            var terms = GetSeekOrderByTerms();
            if (values.Length != terms.Count)
                throw new ArgumentException(
                    $"SeekAfter() requires {terms.Count} values for ORDER BY {orderBy.Substring("ORDER BY ".Length)}, but received {values.Length}",
                    nameof(values));

            return AddSeekCondition(terms, values);
        }

        internal readonly struct SeekTerm(string column, bool descending)
        {
            public string Column { get; } = column;
            public bool Descending { get; } = descending;
        }

        /// <summary>
        /// Adds (a &gt; @0) OR (a = @0 AND b &gt; @1) ... for each ORDER BY column, using &lt; for descending columns.
        /// This form is supported by all RDBMS (unlike row value comparisons) and works with mixed sort directions.
        /// With more than one column it starts with a &gt;= bound on the first column (&lt;= when descending), which
        /// matches the same rows but lets RDBMS like PostgreSQL and SQL Server seek to it in an index instead of scanning
        /// the rows before it.
        /// </summary>
        private SqlExpression<T> AddSeekCondition(List<SeekTerm> terms, object[] values)
        {
            var paramNames = new string[terms.Count];
            for (var i = 0; i < terms.Count; i++)
            {
                if (values[i] == null)
                    throw new ArgumentNullException(nameof(values),
                        $"SeekAfter() values can't be null as NULLs can't be compared, ORDER BY '{terms[i].Column}' should be a non-nullable column");
                paramNames[i] = AddParam(values[i]).ParameterName;
            }

            var sb = new StringBuilder("(");
            // The rows after the last row are all on or after its value of the first column
            if (terms.Count > 1)
            {
                sb.Append('(').Append(terms[0].Column)
                    .Append(terms[0].Descending ? " <= " : " >= ")
                    .Append(paramNames[0])
                    .Append(") AND (");
            }
            for (var i = 0; i < terms.Count; i++)
            {
                if (i > 0)
                    sb.Append(" OR ");
                sb.Append('(');
                for (var j = 0; j < i; j++)
                {
                    sb.Append(terms[j].Column).Append(" = ").Append(paramNames[j]).Append(" AND ");
                }
                sb.Append(terms[i].Column)
                    .Append(terms[i].Descending ? " < " : " > ")
                    .Append(paramNames[i])
                    .Append(')');
            }
            sb.Append(terms.Count > 1 ? "))" : ")");

            return AppendToWhere("AND", sb.ToString());
        }

        /// <summary>
        /// Parses the query's ORDER BY clause into its columns and sort directions
        /// </summary>
        private List<SeekTerm> GetSeekOrderByTerms()
        {
            const string OrderByPrefix = "ORDER BY ";
            if (string.IsNullOrEmpty(orderBy) || !orderBy.StartsWith(OrderByPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("SeekAfter() requires the query to be ordered, call OrderBy() first");

            var terms = new List<SeekTerm>();
            foreach (var part in SplitTopLevelCommas(orderBy.Substring(OrderByPrefix.Length)))
            {
                var term = part.Trim();
                if (term.EndsWith(" NULLS FIRST", StringComparison.OrdinalIgnoreCase)
                    || term.EndsWith(" NULLS LAST", StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException($"SeekAfter() doesn't support ORDER BY {term}");

                var descending = false;
                if (term.EndsWith(" DESC", StringComparison.OrdinalIgnoreCase))
                {
                    descending = true;
                    term = term.Substring(0, term.Length - " DESC".Length).TrimEnd();
                }
                else if (term.EndsWith(" ASC", StringComparison.OrdinalIgnoreCase))
                {
                    term = term.Substring(0, term.Length - " ASC".Length).TrimEnd();
                }

                if (term.Length == 0 || int.TryParse(term, out _))
                    throw new NotSupportedException($"SeekAfter() doesn't support ordering by column position '{part.Trim()}'");

                terms.Add(new SeekTerm(term, descending));
            }
            return terms;
        }

        private static List<string> SplitTopLevelCommas(string sql)
        {
            var parts = new List<string>();
            var depth = 0;
            char quote = default;
            var start = 0;
            for (var i = 0; i < sql.Length; i++)
            {
                var c = sql[i];
                if (quote != default)
                {
                    if (c == quote) quote = default;
                    continue;
                }
                switch (c)
                {
                    case '\'': case '"': case '`': quote = c; break;
                    case '[': quote = ']'; break;
                    case '(': depth++; break;
                    case ')': depth--; break;
                    case ',' when depth == 0:
                        parts.Add(sql.Substring(start, i - start));
                        start = i + 1;
                        break;
                }
            }
            parts.Add(sql.Substring(start));
            return parts;
        }

        /// <summary>
        /// Resolves an ORDER BY column, e.g. "Year" or "Book"."Year", to a field of the queried model
        /// </summary>
        private FieldDefinition FindOrderByField(string column)
        {
            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                var quotedColumn = DialectProvider.GetQuotedColumnName(fieldDef);
                if (column == quotedColumn
                    || column == DialectProvider.GetQuotedColumnName(modelDef, fieldDef)
                    || (TableAlias != null && column == DialectProvider.GetQuotedColumnName(modelDef, TableAlias, fieldDef))
                    || string.Equals(column, fieldDef.FieldName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(column, fieldDef.Name, StringComparison.OrdinalIgnoreCase))
                    return fieldDef;
            }
            return null;
        }
    }
}
