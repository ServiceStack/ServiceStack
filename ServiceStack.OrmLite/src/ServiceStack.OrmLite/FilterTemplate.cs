#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;

namespace ServiceStack.OrmLite;

/// <summary>
/// A filter of a FilterSet rule for a table, with the scope of the connection it's applied to
/// </summary>
internal sealed class ConnectionFilter<T>(FilterTemplate<T> template, object? scope)
{
    private Expression<Func<T, bool>>? bound;

    /// <summary>
    /// The filter with the values of the scope, as it's translated when its SQL isn't reused
    /// </summary>
    public Expression<Func<T, bool>> Bound => bound ??= template.Bind(scope);

    /// <summary>
    /// The filter's SQL for the query, with its db params added to the query, or null if it doesn't apply to rows
    /// with the values of the scope, e.g. f.Filter&lt;T&gt;((x, s) =&gt; s.IsAdmin || x.OwnerId == s.UserId) for admins
    /// </summary>
    public string? ToSql(SqlExpression<T> q) => template.ToSql(q, scope, this);
}

/// <summary>
/// The filter of a FilterSet rule for a table, translated to SQL once for each combination of what its SQL depends on:
/// the dialect, the table's alias, the conditions that only read the scope, and which of its values are null.
/// Each statement reads its values from the scope and adds them as db params.
/// <para>Parts of the filter that don't read the row are evaluated for each statement, either as a condition that
/// only reads the scope, which is decided in C#, e.g. s.WorkspaceId == null, or a value that becomes a db param.
/// Filters whose SQL can't be reused are translated for each statement, see FilterSet.NotCachedReasons.</para>
/// </summary>
internal sealed class FilterTemplate<T>
{
    // Params of the SQL that's kept, which are renamed for each query
    private const string ParamPrefix = "__ssf";
    private const int MaxGuards = 32;
    private const int MaxRendered = 64;

    private readonly FilterRule rule;
    private readonly LambdaExpression condition; // (T x, TScope s) => ... or (T x) => ...
    private readonly ParameterExpression row;
    private readonly ParameterExpression? scopeParam;
    private readonly LogicNode? root;           // null when the filter is translated for each statement
    private readonly List<Slot> slots = [];
    private readonly ConcurrentDictionary<ulong, Variant> variants = new();
    private int rendered;

    internal FilterTemplate(FilterRule rule)
    {
        this.rule = rule;
        condition = rule.Condition!;
        if (typeof(T) != rule.Type)
            condition = TableTypeRebinder.Rebind(condition, typeof(T));
        row = condition.Parameters[0];
        scopeParam = condition.Parameters.Count > 1 ? condition.Parameters[1] : null;

        try
        {
            var guards = 0;
            root = Analyze(condition.Body, ref guards);
            if (guards > MaxGuards)
            {
                root = null;
                NotCached($"It has more than {MaxGuards} conditions that only read the scope");
            }
        }
        catch (Exception e)
        {
            root = null;
            NotCached(e.Message);
        }
    }

    private void NotCached(string reason) => rule.NotCachedReason = $"for {typeof(T).Name}: {reason}";

    /// <summary>
    /// The filter with the scope's value in place of its parameter: x => condition
    /// </summary>
    internal Expression<Func<T, bool>> Bind(object? scope)
    {
        if (scopeParam == null)
            return (Expression<Func<T, bool>>)condition;
        var body = new ReplaceParameter(scopeParam, Expression.Constant(scope, scopeParam.Type)).Visit(condition.Body);
        return Expression.Lambda<Func<T, bool>>(body!, row);
    }

    internal string? ToSql(SqlExpression<T> q, object? scope, ConnectionFilter<T> filter)
    {
        if (root == null || OrmLiteConfig.SqlExpressionInitFilter != null)
            return q.RenderConnectionFilter(filter.Bound);

        ulong bits = 0;
        var result = root.Eval(scope, ref bits);
        if (result == true)
            return null;
        if (result == false)
            return SqlExpression<T>.FalseLiteral;

        var variant = variants.GetOrAdd(bits, b => new Variant(this, b));
        var values = new object?[slots.Count];
        ulong nulls = 0;
        for (var i = 0; i < variant.Slots.Length; i++)
        {
            var slot = variant.Slots[i];
            if ((values[slot] = slots[slot].Value(scope)) == null)
                nulls |= 1UL << i;
        }

        var dialect = q.DialectProvider;
        var key = (dialect, q.TableAlias, nulls);
        if (!variant.Rendered.TryGetValue(key, out var sql))
        {
            if (rendered >= MaxRendered)
            {
                NotCached($"It has more than {MaxRendered} SQL statements");
                sql = RenderedSql.NotCached;
            }
            else
            {
                sql = Render(variant, dialect, q.TableAlias, values, scope) ?? RenderedSql.NotCached;
                if (variant.Rendered.TryAdd(key, sql))
                    System.Threading.Interlocked.Increment(ref rendered);
            }
        }

        return sql.Apply(q, values)
            ?? q.RenderConnectionFilter(Expression.Lambda<Func<T, bool>>(variant.WithScope(scope), row));
    }

    // Translates the variant with the values of the scope, then again with the values as the arguments of a compiled
    // query, to find their db params. Its SQL is kept when both are the same.
    private RenderedSql? Render(Variant variant, IOrmLiteDialectProvider dialect, string? alias, object?[] values,
        object? scope)
    {
        var hold = CompiledQueryBuild.Current;
        try
        {
            CompiledQueryBuild.Current = null;
            var normal = NewQuery(dialect, alias);
            var normalSql = normal.RenderConnectionFilter(Expression.Lambda<Func<T, bool>>(variant.WithScope(scope), row));

            var args = new object?[values.Length];
            values.CopyTo(args, 0);
            var build = new CompiledQueryBuild(args!, new ArgMode[args.Length], bind: true);
            var body = new ReplaceSlots(slot => values[slot] == null
                ? Expression.Constant(null, slots[slot].Type)
                : slots[slot].Arg).Visit(variant.Folded);
            var q = NewQuery(dialect, alias);
            CompiledQueryBuild.Current = build;
            var sql = q.RenderConnectionFilter(Expression.Lambda<Func<T, bool>>(body!, row));
            CompiledQueryBuild.Current = null;

            var reason = RenderedSql.Create(build, dialect, sql, q.Params, normalSql, normal.Params, out var to);
            if (reason != null)
                NotCached(reason);
            return to;
        }
        catch (Exception e)
        {
            NotCached($"The SQL can't be generated without the values of the scope: {e.Message}");
            return null;
        }
        finally
        {
            CompiledQueryBuild.Current = hold;
        }
    }

    private static SqlExpression<T> NewQuery(IOrmLiteDialectProvider dialect, string? alias)
    {
        var q = dialect.SqlExpression<T>();
        q.ParamPrefix = ParamPrefix;
        if (alias != null)
            q.SetTableAlias(alias);
        return q;
    }

    // The structure of the filter's conditions, whose conditions that only read the scope are decided in C#
    private LogicNode Analyze(Expression e, ref int guards)
    {
        if (e.Type == typeof(bool) && IsEvaluated(e))
        {
            if (e is ConstantExpression { Value: bool value })
                return new ConstNode(value);
            return new GuardNode(guards++, CompileValue(e));
        }

        switch (e)
        {
            case BinaryExpression { NodeType: ExpressionType.AndAlso, Method: null } and:
                return new AndNode(Analyze(and.Left, ref guards), Analyze(and.Right, ref guards));
            case BinaryExpression { NodeType: ExpressionType.OrElse, Method: null } or:
                return new OrNode(Analyze(or.Left, ref guards), Analyze(or.Right, ref guards));
            case UnaryExpression { NodeType: ExpressionType.Not, Method: null } not when not.Type == typeof(bool):
                return new NotNode(Analyze(not.Operand, ref guards));
            case ConditionalExpression cond when cond.Type == typeof(bool) && IsEvaluated(cond.Test):
                return new CondNode(Analyze(cond.Test, ref guards), Analyze(cond.IfTrue, ref guards),
                    Analyze(cond.IfFalse, ref guards));
            default:
                return new LeafNode(new SlotFinder(this).Visit(e)!);
        }
    }

    // Whether the expression doesn't read the row, so it's evaluated in C#
    private bool IsEvaluated(Expression e) => FreeParameters.OnlyUses(e, scopeParam);

    private Func<object?, object?> CompileValue(Expression e)
    {
        var scope = Expression.Parameter(typeof(object), "scope");
        var body = scopeParam != null
            ? new ReplaceParameter(scopeParam, Expression.Convert(scope, scopeParam.Type)).Visit(e)!
            : e;
        return Expression.Lambda<Func<object?, object?>>(Expression.Convert(body, typeof(object)), scope).Compile();
    }

    // A value of the filter that's evaluated for each statement
    private sealed class Slot(Expression original, Func<object?, object?> value, Expression marker, Expression arg)
    {
        public Expression Original { get; } = original;
        public Func<object?, object?> Value { get; } = value;
        public Expression Marker { get; } = marker;
        public Expression Arg { get; } = arg; // the argument of a compiled query that's being built
        public Type Type => Original.Type;
    }

    private sealed class SlotMarker(int index)
    {
        public readonly int Index = index;
    }

    // Replaces the values of the filter with slots
    private sealed class SlotFinder(FilterTemplate<T> template) : ExpressionVisitor
    {
        public override Expression? Visit(Expression? node)
        {
            if (node == null || !IsSlot(node))
                return base.Visit(node);

            var index = template.slots.Count;
            var marker = Expression.Field(Expression.Constant(new SlotMarker(index)), nameof(SlotMarker.Index));
            var argType = typeof(CompiledQueryArg<>).MakeGenericType(node.Type);
            var arg = Expression.Property(Expression.Constant(Activator.CreateInstance(argType, index), argType),
                nameof(CompiledQueryArg<int>.Value));
            template.slots.Add(new Slot(node, template.CompileValue(node), marker, arg));
            // Typed as the value it's in place of, see ReplaceSlots
            return Expression.Convert(Expression.Convert(marker, typeof(object)), node.Type);
        }

        // A value that doesn't read the row, and reads the scope or something that can change, unlike 1 + 2.
        // The methods of Sql are translated to SQL, not evaluated.
        private bool IsSlot(Expression node)
        {
            if (node is ConstantExpression or LambdaExpression || node.NodeType == ExpressionType.Quote
                || node.Type == typeof(void))
                return false;
            if (node is ParameterExpression && node != template.scopeParam)
                return false;
            if (node is MethodCallExpression call && call.Method.DeclaringType == typeof(Sql))
                return false;
            return template.IsEvaluated(node) && new ValueFinder(template.scopeParam).Find(node);
        }
    }

    private sealed class ValueFinder(ParameterExpression? scope) : ExpressionVisitor
    {
        private bool found;

        public bool Find(Expression e)
        {
            Visit(e);
            return found;
        }

        public override Expression? Visit(Expression? node)
        {
            if (found || node == null)
                return node;
            if (node is MemberExpression or MethodCallExpression or InvocationExpression or NewExpression
                || node == scope)
            {
                found = true;
                return node;
            }
            return base.Visit(node);
        }
    }

    private static int? SlotIndex(Expression node) =>
        node is UnaryExpression { NodeType: ExpressionType.Convert, Operand: UnaryExpression {
            NodeType: ExpressionType.Convert,
            Operand: MemberExpression { Expression: ConstantExpression { Value: SlotMarker marker } } } }
            ? marker.Index
            : null;

    private sealed class ReplaceSlots(Func<int, Expression> replace) : ExpressionVisitor
    {
        public override Expression? Visit(Expression? node) =>
            node != null && SlotIndex(node) is { } index ? replace(index) : base.Visit(node);
    }

    private sealed class SlotsFinder : ExpressionVisitor
    {
        public readonly List<int> Slots = [];

        public override Expression? Visit(Expression? node)
        {
            if (node != null && SlotIndex(node) is { } index)
            {
                if (!Slots.Contains(index))
                    Slots.Add(index);
                return node;
            }
            return base.Visit(node);
        }
    }

    /// <summary>
    /// The filter with the conditions that only read the scope decided
    /// </summary>
    private sealed class Variant
    {
        private readonly FilterTemplate<T> template;
        public readonly Expression Folded;
        public readonly int[] Slots; // the values of the scope it uses
        public readonly ConcurrentDictionary<(IOrmLiteDialectProvider, string?, ulong), RenderedSql> Rendered = new();

        public Variant(FilterTemplate<T> template, ulong bits)
        {
            this.template = template;
            Folded = template.root!.Fold(bits);
            var finder = new SlotsFinder();
            finder.Visit(Folded);
            Slots = finder.Slots.ToArray();
        }

        // The filter as it's normally translated, with the values read from the scope
        public Expression WithScope(object? scope)
        {
            var slots = template.slots;
            var scopeParam = template.scopeParam;
            return new ReplaceSlots(index => scopeParam == null
                ? slots[index].Original
                : new ReplaceParameter(scopeParam, Expression.Constant(scope, scopeParam.Type))
                    .Visit(slots[index].Original)!).Visit(Folded)!;
        }
    }

    private abstract class LogicNode
    {
        /// <summary>
        /// Evaluates the conditions that only read the scope, as C# would, recording their values in bits.
        /// Returns whether rows always or never match, or null if it depends on the row.
        /// </summary>
        public abstract bool? Eval(object? scope, ref ulong bits);

        /// <summary>
        /// The condition with the values recorded in bits
        /// </summary>
        public abstract Expression Fold(ulong bits);

        protected static bool? Const(Expression e) => e is ConstantExpression { Value: bool value } ? value : null;
    }

    private sealed class ConstNode(bool value) : LogicNode
    {
        public override bool? Eval(object? scope, ref ulong bits) => value;
        public override Expression Fold(ulong bits) => Expression.Constant(value);
    }

    private sealed class GuardNode(int index, Func<object?, object?> fn) : LogicNode
    {
        public override bool? Eval(object? scope, ref ulong bits)
        {
            var value = (bool)fn(scope)!;
            bits |= (value ? 2UL : 1UL) << (index * 2);
            return value;
        }

        public override Expression Fold(ulong bits) => ((bits >> (index * 2)) & 3) switch {
            2 => Expression.Constant(true),
            1 => Expression.Constant(false),
            _ => throw new InvalidOperationException("The condition wasn't evaluated"),
        };
    }

    private sealed class LeafNode(Expression expression) : LogicNode
    {
        public override bool? Eval(object? scope, ref ulong bits) => null;
        public override Expression Fold(ulong bits) => expression;
    }

    private sealed class AndNode(LogicNode left, LogicNode right) : LogicNode
    {
        public override bool? Eval(object? scope, ref ulong bits)
        {
            var l = left.Eval(scope, ref bits);
            if (l == false)
                return false;
            var r = right.Eval(scope, ref bits);
            if (r == false)
                return false;
            return l == true ? r : r == true ? l : null;
        }

        public override Expression Fold(ulong bits)
        {
            var l = left.Fold(bits);
            if (Const(l) == false)
                return l;
            var r = right.Fold(bits);
            if (Const(r) == false)
                return r;
            return Const(l) == true ? r : Const(r) == true ? l : Expression.AndAlso(l, r);
        }
    }

    private sealed class OrNode(LogicNode left, LogicNode right) : LogicNode
    {
        public override bool? Eval(object? scope, ref ulong bits)
        {
            var l = left.Eval(scope, ref bits);
            if (l == true)
                return true;
            var r = right.Eval(scope, ref bits);
            if (r == true)
                return true;
            return l == false ? r : r == false ? l : null;
        }

        public override Expression Fold(ulong bits)
        {
            var l = left.Fold(bits);
            if (Const(l) == true)
                return l;
            var r = right.Fold(bits);
            if (Const(r) == true)
                return r;
            return Const(l) == false ? r : Const(r) == false ? l : Expression.OrElse(l, r);
        }
    }

    private sealed class NotNode(LogicNode operand) : LogicNode
    {
        public override bool? Eval(object? scope, ref ulong bits) => !operand.Eval(scope, ref bits);

        public override Expression Fold(ulong bits)
        {
            var e = operand.Fold(bits);
            return Const(e) is { } value ? Expression.Constant(!value) : Expression.Not(e);
        }
    }

    private sealed class CondNode(LogicNode test, LogicNode ifTrue, LogicNode ifFalse) : LogicNode
    {
        public override bool? Eval(object? scope, ref ulong bits) =>
            test.Eval(scope, ref bits) == true ? ifTrue.Eval(scope, ref bits) : ifFalse.Eval(scope, ref bits);

        public override Expression Fold(ulong bits) =>
            Const(test.Fold(bits)) == true ? ifTrue.Fold(bits) : ifFalse.Fold(bits);
    }
}

/// <summary>
/// The SQL of a filter that's kept, with the values of its db params read from the scope for each statement
/// </summary>
internal sealed class RenderedSql
{
    // Kept so SQL that can't be reused isn't checked again each time
    public static readonly RenderedSql NotCached = new([], []);

    private readonly string[] parts;   // the SQL between its db params
    private readonly int[] paramAt;    // the db param after each part
    private FilterParam[] dbParams = [];
    private bool isMySqlConnector;

    private RenderedSql(string[] parts, int[] paramAt)
    {
        this.parts = parts;
        this.paramAt = paramAt;
    }

    private sealed class FilterParam
    {
        public IDbDataParameter? Param;                      // a db param that's the same each time
        public Func<object[], List<object>[], object>? GetValue; // or a db param for a value of the scope
        public Type? ValueType;
        public DbType? DbType;
        public int? Size;
        public byte? Precision;
        public byte? Scale;
    }

    /// <summary>
    /// Adds the filter's SQL to the query, with db params for its values, or returns null if the values can't be
    /// used with the SQL, e.g. a value of a different type
    /// </summary>
    public string? Apply<T>(SqlExpression<T> q, object?[] values)
    {
        if (ReferenceEquals(this, NotCached))
            return null;

        var dialect = q.DialectProvider;
        var created = new IDbDataParameter[dbParams.Length];
        var start = q.Params.Count;
        for (var i = 0; i < dbParams.Length; i++)
        {
            var template = dbParams[i];
            var name = dialect.GetParam(q.ParamPrefix + (start + i));
            if (template.Param != null)
            {
                var p = dialect.CreateParam();
                p.PopulateWith(template.Param);
                p.ParameterName = name;
                created[i] = p;
                continue;
            }

            var value = template.GetValue!(values!, null!);
            if (value == null || value.GetType() != template.ValueType)
                return null;
            created[i] = CreateParam(dialect, name, value, template);
        }

        q.Params.AddRange(created);
        if (paramAt.Length == 0)
            return parts[0];
        var sb = new StringBuilder(parts[0]);
        for (var i = 0; i < paramAt.Length; i++)
            sb.Append(created[paramAt[i]].ParameterName).Append(parts[i + 1]);
        return sb.ToString();
    }

    // As SqlExpression.AddParam() creates them
    private IDbDataParameter CreateParam(IOrmLiteDialectProvider dialect, string name, object value,
        FilterParam? template)
    {
        var p = dialect.CreateParam();
        p.ParameterName = name;
        p.Direction = ParameterDirection.Input;
        if (!isMySqlConnector)
            p.SourceVersion = DataRowVersion.Default;
        dialect.ConfigureParam(p, value, null);
        dialect.InitQueryParam(p);
        if (template?.DbType != null)
            p.DbType = template.DbType.Value;
        if (template?.Size != null)
            p.Size = template.Size.Value;
        if (template?.Precision != null)
            p.Precision = template.Precision.Value;
        if (template?.Scale != null)
            p.Scale = template.Scale.Value;
        return p;
    }

    /// <summary>
    /// The SQL to keep, or null with the reason when the SQL and db params with the values of the scope as arguments
    /// aren't the same as when the filter is normally translated
    /// </summary>
    internal static string? Create(CompiledQueryBuild build, IOrmLiteDialectProvider dialect, string sql,
        List<IDbDataParameter> sqlParams, string normalSql, List<IDbDataParameter> normalParams, out RenderedSql? to)
    {
        to = null;
        for (var i = 0; i < build.Args.Length; i++)
        {
            if (build.UsedInSql[i])
                return "A value of the scope is used in its SQL instead of a db param";
            if (build.UsedAsList[i])
                return "A collection of the scope has a db param for each of its values";
        }
        if (build.HasNullValue)
            return "A value converted from the scope is null";
        if (sql != normalSql)
            return "The SQL changes with the values of the scope";
        if (sqlParams.Count != normalParams.Count)
            return "The db params change with the values of the scope";

        var rendered = new RenderedSql([], []) { isMySqlConnector = dialect.IsMySqlConnector() };
        var templates = new FilterParam[sqlParams.Count];
        var bound = 0;
        for (var i = 0; i < sqlParams.Count; i++)
        {
            var p = sqlParams[i];
            var normal = normalParams[i];
            if (p.ParameterName != normal.ParameterName || p.DbType != normal.DbType || !Equals(p.Value, normal.Value))
                return $"The db param {p.ParameterName} isn't the same each time";

            if (!build.Bound.TryGetValue(p, out var value))
            {
                templates[i] = new FilterParam { Param = p };
                continue;
            }

            bound++;
            var template = new FilterParam { GetValue = value.GetValue, ValueType = value.Sample.GetType() };
            // Changes made to the db param after it was created are made each time
            var created = rendered.CreateParam(dialect, p.ParameterName, value.Sample, null);
            if (!Equals(created.Value, p.Value))
                return $"The db param {p.ParameterName} isn't created from the value of the scope";
            if (created.DbType != p.DbType)
                template.DbType = p.DbType;
            if (created.Size != p.Size)
                template.Size = p.Size;
            if (created.Precision != p.Precision)
                template.Precision = p.Precision;
            if (created.Scale != p.Scale)
                template.Scale = p.Scale;
            templates[i] = template;
        }
        if (bound != build.Bound.Count)
            return "The db params of the scope's values are changed by its SQL";

        // Each db param's name is replaced by the name of the db param the query gives it
        var marked = sql;
        for (var i = sqlParams.Count - 1; i >= 0; i--)
            marked = marked.Replace(sqlParams[i].ParameterName, "\0" + i + "\0");
        var split = marked.Split('\0');
        var parts = new string[split.Length / 2 + 1];
        var paramAt = new int[split.Length / 2];
        for (var i = 0; i < split.Length; i++)
        {
            if (i % 2 == 0)
                parts[i / 2] = split[i];
            else
                paramAt[i / 2] = int.Parse(split[i]);
        }

        to = new RenderedSql(parts, paramAt) { dbParams = templates, isMySqlConnector = rendered.isMySqlConnector };
        return null;
    }
}

/// <summary>
/// Whether an expression only uses the parameter, or no parameters, apart from those of lambdas within it
/// </summary>
internal sealed class FreeParameters : ExpressionVisitor
{
    private readonly ParameterExpression? allowed;
    private readonly HashSet<ParameterExpression> declared = [];
    private bool usesOther;

    private FreeParameters(ParameterExpression? allowed) => this.allowed = allowed;

    public static bool OnlyUses(Expression e, ParameterExpression? allowed)
    {
        var finder = new FreeParameters(allowed);
        finder.Visit(e);
        return !finder.usesOther;
    }

    protected override Expression VisitLambda<TDelegate>(Expression<TDelegate> node)
    {
        foreach (var p in node.Parameters)
            declared.Add(p);
        return base.VisitLambda(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node != allowed && !declared.Contains(node))
            usesOther = true;
        return node;
    }
}

internal sealed class ReplaceParameter(ParameterExpression from, Expression to) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
}
