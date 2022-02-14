using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace ServiceStack.OrmLite
{
    public static partial class Sql
    {
	    public static string VARCHAR = nameof(VARCHAR); 
	    
        public static List<object> Flatten(IEnumerable list)
        {
            var ret = new List<object>();
            if (list == null) return ret;

            foreach (var item in list)
            {
                if (item == null) continue;

                if (item is IEnumerable arr && !(item is string))
                {
                    ret.AddRange(arr.Cast<object>());
                }
                else
                {
                    ret.Add(item);
                }
            }
            return ret;
        }

        public static bool In<T, TItem>(T value, params TItem[] list) => value != null && Flatten(list).Any(obj => obj.ToString() == value.ToString());

        public static bool In<T, TItem>(T value, SqlExpression<TItem> query) => value != null && query != null;

        public static string Asc<T>(T value) => value == null ? "" : value + " ASC";
        public static string Desc<T>(T value) => value == null ? "" : value + " DESC";

        public static string As<T>(T value, object asValue) => value == null ? "" : $"{value} AS {asValue}";

        public static T Sum<T>(T value) => value;

        public static string Sum(string value) => $"SUM({value})";

        public static T Count<T>(T value) => value;

        public static T CountDistinct<T>(T value) => value;

        public static string Count(string value) => $"COUNT({value})";

        public static T Min<T>(T value) => value;

        public static string Min(string value) => $"MIN({value})";

        public static T Max<T>(T value) => value;

        public static string Max(string value) => $"MAX({value})";

        public static T Avg<T>(T value) => value;

        public static string Avg(string value) => $"AVG({value})";

        public static T AllFields<T>(T item) => item;

	    [Obsolete("Use TableAlias")]
	    public static string JoinAlias(string property, string tableAlias) => tableAlias;

	    public static string TableAlias(string property, string tableAlias) => tableAlias;

	    [Obsolete("Use TableAlias")]
	    public static T JoinAlias<T>(T property, string tableAlias) => default(T);

	    public static T TableAlias<T>(T property, string tableAlias) => default(T);

        public static string Custom(string customSql) => customSql;

        public static T Custom<T>(string customSql) => default(T);

	    public static string Cast(object value, string castAs) => $"CAST({value} AS {castAs})";

        public const string EOT= "0 EOT";
    }

    /// <summary>
    /// SQL Server 2016 specific features
    /// </summary>
    public static partial class Sql
    {
        /// <summary>Tests whether a string contains valid JSON.</summary>
        /// <param name="expression">The string to test.</param>
        /// <returns>Returns True if the string contains valid JSON; otherwise, returns False. Returns null if expression is null.</returns>
        /// <remarks>ISJSON does not check the uniqueness of keys at the same level.</remarks>
        /// See <a href="https://docs.microsoft.com/en-us/sql/t-sql/functions/isjson-transact-sql">ISJSON (Transact-SQL)</a>
        public static bool? IsJson(string expression) => null;

        /// <summary>Extracts a scalar value from a JSON string.</summary>
        /// <param name="expression">
        /// An expression. Typically the name of a variable or a column that contains JSON text.<br/><br/>
        /// If <b>JSON_VALUE</b> finds JSON that is not valid in expression before it finds the value identified by <i>path</i>, the function returns an error. If <b>JSON_VALUE</b> doesn't find the value identified by <i>path</i>, it scans the entire text and returns an error if it finds JSON that is not valid anywhere in <i>expression</i>.
        /// </param>
        /// <param name="path">
        /// A JSON path that specifies the property to extract. For more info, see <a href="https://docs.microsoft.com/en-us/sql/relational-databases/json/json-path-expressions-sql-server">JSON Path Expressions (SQL Server)</a>.<br/><br/>
        /// In SQL Server 2017 and in Azure SQL Database, you can provide a variable as the value of <i>path</i>.<br/><br/> 
        /// If the format of path isn't valid, <b>JSON_VALUE</b> returns an error.<br/><br/>
        /// </param>
        /// <returns>
        /// Returns a single text value of type nvarchar(4000). The collation of the returned value is the same as the collation of the input expression.
        /// If the value is greater than 4000 characters: <br/><br/>
        /// <ul>
        /// <li>In lax mode, <b>JSON_VALUE</b> returns null.</li>
        /// <li>In strict mode, <b>JSON_VALUE</b> returns an error.</li>
        /// </ul>
        /// <br/>
        /// If you have to return scalar values greater than 4000 characters, use <b>OPENJSON</b> instead of <b>JSON_VALUE</b>. For more info, see <a href="https://docs.microsoft.com/en-us/sql/t-sql/functions/openjson-transact-sql">OPENJSON (Transact-SQL)</a>.
        /// </returns>
        /// <a href="https://docs.microsoft.com/en-us/sql/t-sql/functions/json-value-transact-sql">JSON_VALUE (Transact-SQL)</a>
        public static T JsonValue<T>(string expression, string path) => default(T);

        /// <summary>Extracts a scalar value from a JSON string.</summary>
        /// <param name="expression">
        /// An expression. Typically the name of a variable or a column that contains JSON text.<br/><br/>
        /// If <b>JSON_VALUE</b> finds JSON that is not valid in expression before it finds the value identified by <i>path</i>, the function returns an error. If <b>JSON_VALUE</b> doesn't find the value identified by <i>path</i>, it scans the entire text and returns an error if it finds JSON that is not valid anywhere in <i>expression</i>.
        /// </param>
        /// <param name="path">
        /// A JSON path that specifies the property to extract. For more info, see <a href="https://docs.microsoft.com/en-us/sql/relational-databases/json/json-path-expressions-sql-server">JSON Path Expressions (SQL Server)</a>.<br/><br/>
        /// In SQL Server 2017 and in Azure SQL Database, you can provide a variable as the value of <i>path</i>.<br/><br/> 
        /// If the format of path isn't valid, <b>JSON_VALUE</b> returns an error.<br/><br/>
        /// </param>
        /// <returns>
        /// Returns a single text value of type nvarchar(4000). The collation of the returned value is the same as the collation of the input expression.
        /// If the value is greater than 4000 characters: <br/><br/>
        /// <ul>
        /// <li>In lax mode, <b>JSON_VALUE</b> returns null.</li>
        /// <li>In strict mode, <b>JSON_VALUE</b> returns an error.</li>
        /// </ul>
        /// <br/>
        /// If you have to return scalar values greater than 4000 characters, use <b>OPENJSON</b> instead of <b>JSON_VALUE</b>. For more info, see <a href="https://docs.microsoft.com/en-us/sql/t-sql/functions/openjson-transact-sql">OPENJSON (Transact-SQL)</a>.
        /// </returns>
        /// <a aref="https://docs.microsoft.com/en-us/sql/t-sql/functions/json-value-transact-sql">JSON_VALUE (Transact-SQL)</a>
        public static string JsonValue(string expression, string path) => string.Empty;


		/// <summary>
		/// Extracts an object or an array from a JSON string.<br/><br/>
		/// To extract a scalar value from a JSON string instead of an object or an array, see <a href="https://docs.microsoft.com/en-us/sql/t-sql/functions/json-value-transact-sql">JSON_VALUE(Transact-SQL)</a>. 
		/// For info about the differences between <b>JSON_VALUE</b> and <b>JSON_QUERY</b>, see <a href="https://docs.microsoft.com/en-us/sql/relational-databases/json/validate-query-and-change-json-data-with-built-in-functions-sql-server#JSONCompare">Compare JSON_VALUE and JSON_QUERY</a>.
		/// </summary>
		/// <param name="expression">
		/// An expression. Typically the name of a variable or a column that contains JSON text.<br/><br/>
		/// If <b>JSON_QUERY</b> finds JSON that is not valid in <i>expression</i> before it finds the value identified by <i>path</i>, the function returns an error. If <b>JSON_QUERY</b> doesn't find the value identified by <i>path</i>, it scans the entire text and returns an error if it finds JSON that is not valid anywhere in <i>expression</i>.
		/// </param>
		/// <returns>
		/// Returns a JSON fragment of type T. The collation of the returned value is the same as the collation of the input expression.<br/><br/>
		/// If the value is not an object or an array:
		/// <ul>
		/// <li>In lax mode, <b>JSON_QUERY</b> returns null.</li>
		/// <li>In strict mode, <b>JSON_QUERY</b> returns an error.</li>
		/// </ul>
		/// </returns>
		public static string JsonQuery(string expression) => string.Empty;

		public static T JsonQuery<T>(string expression) => default(T);

		// SQL Server 2017+
		public static string JsonQuery(string expression, string path) => string.Empty;

		// SQL Server 2017+
		public static T JsonQuery<T>(string expression, string path) => default(T);
	}
}

