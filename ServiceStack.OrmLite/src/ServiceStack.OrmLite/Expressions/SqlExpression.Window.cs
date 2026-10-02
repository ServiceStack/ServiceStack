using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    public abstract partial class SqlExpression<T>
    {
        private string topPerGroupPartitionBy;
        private int topPerGroupTake;

        /// <summary>
        /// Whether this query only returns the top rows of each group, from TopPerGroup()
        /// </summary>
        public bool HasTopPerGroup => topPerGroupPartitionBy != null;

        /// <summary>
        /// Only return the first <paramref name="take"/> rows of each group of rows with the same
        /// <paramref name="partitionBy"/> values, in the order of the query's OrderBy(), e.g. each customer's latest 3 orders:
        /// <para>db.From&lt;Order&gt;().OrderByDescending(x =&gt; x.CreatedDate).TopPerGroup(x =&gt; x.CustomerId, take: 3)</para>
        /// Where() conditions are applied before rows are ranked.
        /// </summary>
        public virtual SqlExpression<T> TopPerGroup(Expression<Func<T, object>> partitionBy, int take)
        {
            if (partitionBy == null)
                throw new ArgumentNullException(nameof(partitionBy));
            if (take < 1)
                throw new ArgumentOutOfRangeException(nameof(take), "take must be at least 1");

            Reset(sep = string.Empty);
            var partitionExpr = Visit(partitionBy);
            if (IsSqlClass(partitionExpr))
            {
                StripAliases(partitionExpr as SelectList);
                topPerGroupPartitionBy = partitionExpr.ToString();
            }
            else
            {
                topPerGroupPartitionBy = partitionExpr is string strExpr
                    ? ListExpression(partitionBy, strExpr)
                    : partitionExpr.ToString();
            }
            topPerGroupTake = take;
            return this;
        }

        /// <summary>
        /// The body of the query that reads the top rows of each group from a derived table, aliased as the model's
        /// table so the query's SELECT and ORDER BY are unchanged:
        /// FROM (SELECT t.*, ROW_NUMBER() OVER (PARTITION BY ... ORDER BY ...) AS rn FROM t WHERE ...) t WHERE rn &lt;= take
        /// </summary>
        private string GetTopPerGroupBodyExpression()
        {
            if (HasSetOperations)
                throw new NotSupportedException("TopPerGroup() can't be used with set operations like Union()");
            if (HasCommonTableExpression)
                throw new NotSupportedException("TopPerGroup() can't be used with common table expressions like WithRecursive()");
            if (HasSystemTime)
                throw new NotSupportedException("TopPerGroup() can't be used with previous versions of a table, e.g. AsOf()");
            if (!string.IsNullOrEmpty(GroupByExpression))
                throw new NotSupportedException("TopPerGroup() can't be used with GroupBy()");
            if (string.IsNullOrEmpty(orderBy))
                throw new NotSupportedException("TopPerGroup() requires an OrderBy() to rank the rows of each group");
            if (TableAlias == null && modelDef.Schema != null)
                throw new NotSupportedException("TopPerGroup() on a table with a schema requires a TableAlias");

            var alias = DialectProvider.GetQuotedName(TableAlias ?? DialectProvider.NamingStrategy.GetTableName(modelDef));
            var tableRef = TableAlias != null
                ? DialectProvider.GetQuotedName(TableAlias)
                : DialectProvider.GetQuotedTableName(modelDef);
            var rowNumber = DialectProvider.GetQuotedName(TopPerGroupRowNumber);

            var sb = StringBuilderCache.Allocate()
                .Append(" \nFROM (SELECT ").Append(tableRef).Append(".*, ROW_NUMBER() OVER (PARTITION BY ")
                .Append(topPerGroupPartitionBy).Append(' ').Append(orderBy).Append(") AS ").Append(rowNumber)
                .Append(BodyExpression)
                .Append(") ").Append(alias)
                .Append("\nWHERE ").Append(alias).Append('.').Append(rowNumber).Append(" <= ").Append(topPerGroupTake);
            return StringBuilderCache.ReturnAndFree(sb);
        }

        /// <summary>
        /// Name of the row number column added to the rows of TopPerGroup() queries
        /// </summary>
        public const string TopPerGroupRowNumber = "_rn";

        /// <summary>
        /// Whether a Sql method call is a window function, i.e. its last argument is the OVER (...) window
        /// </summary>
        private static bool IsWindowFunctionCall(MethodCallExpression m) =>
            m.Arguments.Count > 0
            && m.Arguments[m.Arguments.Count - 1] is LambdaExpression { Parameters.Count: 1 } lambda
            && lambda.Parameters[0].Type == typeof(SqlWindow);

        /// <summary>
        /// Translates a window function like Sql.RowNumber(w =&gt; w.PartitionBy(x.CustomerId)) into
        /// ROW_NUMBER() OVER (PARTITION BY "CustomerId")
        /// </summary>
        protected virtual object VisitWindowFunctionCall(MethodCallExpression m)
        {
            var args = m.Arguments;
            var window = (LambdaExpression)args[args.Count - 1];

            string function;
            switch (m.Method.Name)
            {
                case nameof(Sql.RowNumber):
                    function = "ROW_NUMBER()";
                    break;
                case nameof(Sql.Rank):
                    function = "RANK()";
                    break;
                case nameof(Sql.DenseRank):
                    function = "DENSE_RANK()";
                    break;
                case nameof(Sql.Ntile):
                    function = $"NTILE({EvaluateWindowInt(args[0], "buckets")})";
                    break;
                case nameof(Sql.Lag):
                case nameof(Sql.Lead):
                    function = m.Method.Name.ToUpperInvariant() + "(" + VisitWindowColumn(args[0])
                        + (args.Count == 3 ? ", " + EvaluateWindowInt(args[1], "offset") : "") + ")";
                    break;
                case nameof(Sql.FirstValue):
                    function = $"FIRST_VALUE({VisitWindowColumn(args[0])})";
                    break;
                case nameof(Sql.LastValue):
                    function = $"LAST_VALUE({VisitWindowColumn(args[0])})";
                    break;
                case nameof(Sql.Sum):
                case nameof(Sql.Count):
                case nameof(Sql.Min):
                case nameof(Sql.Max):
                case nameof(Sql.Avg):
                    function = m.Method.Name.ToUpperInvariant() + "(" + VisitWindowColumn(args[0]) + ")";
                    break;
                default:
                    throw new NotSupportedException($"Sql.{m.Method.Name}() isn't a supported window function");
            }

            return new PartialSqlString(function + " OVER (" + VisitWindow(window) + ")");
        }

        private object VisitWindowColumn(Expression expr)
        {
            while (expr.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
                expr = ((UnaryExpression)expr).Operand;

            if (expr is ConstantExpression { Value: "*" })
                return "*";

            var hold = isSelectExpression;
            isSelectExpression = false; // column names without aliases
            try
            {
                var column = expr.NodeType == ExpressionType.MemberAccess
                    ? VisitMemberAccess((MemberExpression)expr)
                    : Visit(expr);
                if (column is PartialSqlString or SelectItem or EnumMemberAccess)
                    return column;
                if (expr is MemberExpression && IsLambdaArg(GetRootParameter(expr)))
                    return column;
                throw new NotSupportedException($"Window functions only support columns, not '{expr}'");
            }
            finally
            {
                isSelectExpression = hold;
            }
        }

        private static ParameterExpression GetRootParameter(Expression expr)
        {
            while (expr is MemberExpression me)
                expr = me.Expression;
            return expr as ParameterExpression;
        }

        private int EvaluateWindowInt(Expression expr, string name)
        {
            // Inlined into the SQL, which is safe as it's an int
            var value = EvaluateExpression(expr);
            if (value is not int i || i < 0)
                throw new ArgumentException($"{name} must be a non-negative int", name);
            return i;
        }

        /// <summary>
        /// Translates the calls on a SqlWindow, e.g. w =&gt; w.PartitionBy(x.A).OrderBy(x.B), into the OVER (...) clause
        /// </summary>
        private string VisitWindow(LambdaExpression window)
        {
            var calls = new List<MethodCallExpression>();
            var expr = window.Body;
            while (expr is MethodCallExpression { Object: not null } call && call.Method.DeclaringType == typeof(SqlWindow))
            {
                calls.Add(call);
                expr = call.Object;
            }
            if (expr != window.Parameters[0])
                throw new NotSupportedException($"Unsupported window expression '{window.Body}'");
            calls.Reverse();

            var partitionBy = new List<string>();
            var orderBy = new List<string>();
            string frame = null;
            foreach (var call in calls)
            {
                switch (call.Method.Name)
                {
                    case nameof(SqlWindow.PartitionBy):
                        var columns = call.Arguments[0] is NewArrayExpression array
                            ? (IEnumerable<Expression>)array.Expressions
                            : [call.Arguments[0]];
                        foreach (var column in columns)
                            partitionBy.Add(VisitWindowColumn(column).ToString());
                        break;
                    case nameof(SqlWindow.OrderBy):
                    case nameof(SqlWindow.ThenBy):
                        orderBy.Add(VisitWindowColumn(call.Arguments[0]).ToString());
                        break;
                    case nameof(SqlWindow.OrderByDescending):
                    case nameof(SqlWindow.ThenByDescending):
                        orderBy.Add(VisitWindowColumn(call.Arguments[0]) + " DESC");
                        break;
                    case nameof(SqlWindow.RowsBetween):
                        var preceding = (int?)EvaluateExpression(call.Arguments[0]);
                        var following = (int?)EvaluateExpression(call.Arguments[1]);
                        if (preceding < 0 || following < 0)
                            throw new ArgumentException("RowsBetween() values must be non-negative");
                        frame = "ROWS BETWEEN "
                            + (preceding == null ? "UNBOUNDED PRECEDING" : preceding == 0 ? "CURRENT ROW" : preceding + " PRECEDING")
                            + " AND "
                            + (following == null ? "UNBOUNDED FOLLOWING" : following == 0 ? "CURRENT ROW" : following + " FOLLOWING");
                        break;
                }
            }

            var sb = new StringBuilder();
            if (partitionBy.Count > 0)
                sb.Append("PARTITION BY ").Append(string.Join(", ", partitionBy));
            if (orderBy.Count > 0)
                sb.Append(sb.Length > 0 ? " " : "").Append("ORDER BY ").Append(string.Join(", ", orderBy));
            if (frame != null)
                sb.Append(sb.Length > 0 ? " " : "").Append(frame);
            return sb.ToString();
        }
    }
}
