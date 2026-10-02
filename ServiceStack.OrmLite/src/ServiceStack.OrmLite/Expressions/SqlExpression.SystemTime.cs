using System;

namespace ServiceStack.OrmLite
{
    public abstract partial class SqlExpression<T>
    {
        // FOR SYSTEM_TIME ... of the query's table, which reads previous versions of its rows
        private string systemTimeClause;

        /// <summary>
        /// Whether this query reads previous versions of a system-versioned table, e.g. with AsOf()
        /// </summary>
        public bool HasSystemTime => systemTimeClause != null;

        /// <summary>
        /// Query a system-versioned table as it was at a time, e.g:
        /// <para>db.From&lt;Product&gt;().AsOf(new DateTime(2026, 1, 1))</para>
        /// The time is in UTC for SQL Server, and the time zone of the connection for MariaDB.
        /// </summary>
        public virtual SqlExpression<T> AsOf(DateTime time) =>
            ForSystemTime("AS OF " + AddSystemTimeParam(time));

        /// <summary>
        /// Query every version of the rows of a system-versioned table that were current at any time between two
        /// times, e.g. to see the changes that were made to a row:
        /// <para>db.From&lt;Product&gt;().VersionsBetween(from, to).Where(x => x.Id == 1)</para>
        /// </summary>
        public virtual SqlExpression<T> VersionsBetween(DateTime from, DateTime to) =>
            ForSystemTime("BETWEEN " + AddSystemTimeParam(from) + " AND " + AddSystemTimeParam(to));

        /// <summary>
        /// Query every version of the rows of a system-versioned table, current and previous
        /// </summary>
        public virtual SqlExpression<T> AllVersions() => ForSystemTime("ALL");

        private string AddSystemTimeParam(DateTime time) => ConvertToParam(DialectProvider.ToSystemTime(time));

        private SqlExpression<T> ForSystemTime(string condition)
        {
            if (systemTimeClause != null)
                throw new InvalidOperationException("This query already reads the versions of its table as of a time");

            var clause = DialectProvider.ToSystemTimeClause(condition);

            // Follows the query's table, before its alias and any joins
            if (!string.IsNullOrEmpty(fromExpression))
            {
                var fromTable = " \nFROM " + DialectProvider.GetQuotedTableName(modelDef);
                if (!fromExpression.StartsWith(fromTable, StringComparison.Ordinal))
                    throw new NotSupportedException("The versions of a table can't be queried with a custom FROM expression");

                fromExpression = fromTable + " " + clause + fromExpression.Substring(fromTable.Length);
            }

            systemTimeClause = clause;
            return this;
        }
    }
}
