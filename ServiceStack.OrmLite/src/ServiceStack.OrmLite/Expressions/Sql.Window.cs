using System;

namespace ServiceStack.OrmLite
{
    /// <summary>
    /// The window a window function is calculated over, i.e. its OVER (...) clause, e.g:
    /// <para>w =&gt; w.PartitionBy(x.CustomerId).OrderByDescending(x.Total)</para>
    /// Only used in expressions, it's translated to SQL.
    /// </summary>
    public sealed class SqlWindow
    {
        private SqlWindow() {}

        /// <summary>Calculate the function separately for each group of rows with the same values</summary>
        public SqlWindow PartitionBy(params object[] columns) => this;

        /// <summary>Order of rows within each partition</summary>
        public SqlWindow OrderBy(object column) => this;

        /// <summary>Order of rows within each partition, in descending order</summary>
        public SqlWindow OrderByDescending(object column) => this;

        /// <summary>Additional order of rows within each partition</summary>
        public SqlWindow ThenBy(object column) => this;

        /// <summary>Additional order of rows within each partition, in descending order</summary>
        public SqlWindow ThenByDescending(object column) => this;

        /// <summary>
        /// Only calculate the function over the rows from <paramref name="preceding"/> rows before to
        /// <paramref name="following"/> rows after the current row, e.g. RowsBetween(6, 0) for a 7 row moving average.
        /// Use 0 for the current row and null for all rows before or after it.
        /// </summary>
        public SqlWindow RowsBetween(int? preceding, int? following) => this;
    }

    /// <summary>
    /// Window functions, which calculate a value for each row from other rows in its window, e.g. its rank or a
    /// running total, without grouping rows like GROUP BY. Use them in Select() expressions, e.g:
    /// <para>Rank = Sql.RowNumber(w =&gt; w.PartitionBy(x.CustomerId).OrderByDescending(x.Total))</para>
    /// </summary>
    public static partial class Sql
    {
        /// <summary>ROW_NUMBER(): the row's sequential number in its window, starting at 1</summary>
        public static long RowNumber(Func<SqlWindow, SqlWindow> over) => 0;

        /// <summary>RANK(): the row's rank in its window, with gaps after rows with equal values, e.g. 1, 1, 3</summary>
        public static long Rank(Func<SqlWindow, SqlWindow> over) => 0;

        /// <summary>DENSE_RANK(): the row's rank in its window, without gaps after rows with equal values, e.g. 1, 1, 2</summary>
        public static long DenseRank(Func<SqlWindow, SqlWindow> over) => 0;

        /// <summary>NTILE(buckets): the number of the bucket the row is in, after dividing its window into buckets</summary>
        public static long Ntile(int buckets, Func<SqlWindow, SqlWindow> over) => 0;

        /// <summary>LAG(value): the value of the previous row in the window, or null for the first row</summary>
        public static T Lag<T>(T value, Func<SqlWindow, SqlWindow> over) => value;

        /// <summary>LAG(value, offset): the value of the row <paramref name="offset"/> rows before in the window</summary>
        public static T Lag<T>(T value, int offset, Func<SqlWindow, SqlWindow> over) => value;

        /// <summary>LEAD(value): the value of the next row in the window, or null for the last row</summary>
        public static T Lead<T>(T value, Func<SqlWindow, SqlWindow> over) => value;

        /// <summary>LEAD(value, offset): the value of the row <paramref name="offset"/> rows after in the window</summary>
        public static T Lead<T>(T value, int offset, Func<SqlWindow, SqlWindow> over) => value;

        /// <summary>FIRST_VALUE(value): the value of the first row in the window</summary>
        public static T FirstValue<T>(T value, Func<SqlWindow, SqlWindow> over) => value;

        /// <summary>
        /// LAST_VALUE(value): the value of the last row in the window. With an ORDER BY the window ends at the current
        /// row by default, use RowsBetween(null, null) for the last row of the partition.
        /// </summary>
        public static T LastValue<T>(T value, Func<SqlWindow, SqlWindow> over) => value;

        /// <summary>SUM(value) OVER (...), e.g. a running total when the window has an ORDER BY</summary>
        public static T Sum<T>(T value, Func<SqlWindow, SqlWindow> over) => value;

        /// <summary>COUNT(value) OVER (...), use Sql.Count("*", over) to count all rows</summary>
        public static long Count<T>(T value, Func<SqlWindow, SqlWindow> over) => 0;

        /// <summary>MIN(value) OVER (...)</summary>
        public static T Min<T>(T value, Func<SqlWindow, SqlWindow> over) => value;

        /// <summary>MAX(value) OVER (...)</summary>
        public static T Max<T>(T value, Func<SqlWindow, SqlWindow> over) => value;

        /// <summary>AVG(value) OVER (...), e.g. a moving average with RowsBetween()</summary>
        public static T Avg<T>(T value, Func<SqlWindow, SqlWindow> over) => value;
    }
}
