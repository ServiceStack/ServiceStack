#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite;

/// <summary>
/// The mandatory filters and write rules of a connection, applied to every statement OrmLite creates for a table
/// they apply to. Immutable, registering a filter or rule creates a new instance.
/// </summary>
public sealed class OrmLiteConnectionFilters
{
    public static readonly OrmLiteConnectionFilters Empty = new([], []);

    private static readonly ILog Log = LogManager.GetLogger(typeof(OrmLiteConnectionFilters));

    private readonly EnsureFilterDef[] ensureFilters;
    private readonly ConcurrentDictionary<Type, Func<LambdaExpression?>[]> ensureFiltersByTable = new();

    private readonly WriteRuleDef[] writeRules;
    private readonly ConcurrentDictionary<Type, TableWriteRules?> writeRulesByTable = new();

    private OrmLiteConnectionFilters(EnsureFilterDef[] ensureFilters, WriteRuleDef[] writeRules)
    {
        this.ensureFilters = ensureFilters;
        this.writeRules = writeRules;
    }

    public bool IsEmpty => ensureFilters.Length == 0 && writeRules.Length == 0;

    /// <summary>
    /// Whether the connection has any EnsureWrites, OnInsert or OnUpdate rules
    /// </summary>
    public bool HasWriteRules => writeRules.Length > 0;

    /// <summary>
    /// Adds the filter, unless the same filter with the same captured values is already registered
    /// </summary>
    internal OrmLiteConnectionFilters AddEnsureFilter(Type type, LambdaExpression predicate)
    {
        var filter = new EnsureFilterDef(type, predicate, null);
        foreach (var existing in ensureFilters)
        {
            if (existing.Type != type || existing.Predicate == null || existing.Shape != filter.Shape)
                continue;

            var comparison = CapturedValues.Compare(existing.Values, filter.Values);
            if (comparison == CapturedValuesComparison.Same)
                return this;

            // The same filter with different values is likely registered twice, e.g. for 2 different tenants
            if (comparison == CapturedValuesComparison.Different)
                Log.Warn($"Connection already has the filter {type.Name}: {predicate} with different values, " +
                         "rows need to match both filters");
        }
        return Add(filter);
    }

    /// <summary>
    /// Adds the filter function, unless the same function is already registered
    /// </summary>
    internal OrmLiteConnectionFilters AddEnsureFilter(Type type, Func<LambdaExpression?> predicateFn)
    {
        foreach (var existing in ensureFilters)
        {
            if (existing.Type == type && Equals(existing.PredicateFn, predicateFn))
                return this;
        }
        return Add(new EnsureFilterDef(type, null, predicateFn));
    }

    private OrmLiteConnectionFilters Add(EnsureFilterDef filter)
    {
        var filters = new EnsureFilterDef[ensureFilters.Length + 1];
        ensureFilters.CopyTo(filters, 0);
        filters[ensureFilters.Length] = filter;
        return new OrmLiteConnectionFilters(filters, writeRules);
    }

    /// <summary>
    /// Adds the rule, unless the same rule with the same value or function is already registered.
    /// A different EnsureWrites value for the same column throws.
    /// </summary>
    internal OrmLiteConnectionFilters AddWriteRule(WriteRuleDef rule)
    {
        foreach (var existing in writeRules)
        {
            if (existing.RuleType != rule.RuleType || existing.MemberName != rule.MemberName)
                continue;

            var sameType = existing.Type == rule.Type;
            if (sameType && existing.HasSameValue(rule))
                return this;

            // Rules with values that can't be compared, e.g. from different functions, are all applied
            var appliesToSameTables = sameType || existing.Type.IsAssignableFrom(rule.Type) || rule.Type.IsAssignableFrom(existing.Type);
            if (!appliesToSameTables || !existing.HasValue || !rule.HasValue || Equals(existing.Value, rule.Value))
                continue;

            if (rule.RuleType == WriteRuleType.EnsureWrites)
                throw new InvalidOperationException(
                    $"Connection already ensures {existing.Type.Name}.{rule.MemberName} is '{existing.Value}', " +
                    $"it can't also be '{rule.Value}'");

            Log.Warn($"Connection already has an {rule.RuleType} rule setting {existing.Type.Name}.{rule.MemberName} " +
                     $"to '{existing.Value}', which is replaced by '{rule.Value}'");
        }

        var rules = new WriteRuleDef[writeRules.Length + 1];
        writeRules.CopyTo(rules, 0);
        rules[writeRules.Length] = rule;
        return new OrmLiteConnectionFilters(ensureFilters, rules);
    }

    /// <summary>
    /// The write rules that apply to the table, or null if there are none
    /// </summary>
    internal TableWriteRules? GetWriteRules(Type tableType)
    {
        if (writeRules.Length == 0)
            return null;

        return writeRulesByTable.GetOrAdd(tableType, type => {
            var modelDef = type.GetModelDefinition();
            var ensureWrites = new List<WriteRule>();
            var onInsert = new List<WriteRule>();
            var onUpdate = new List<WriteRule>();
            foreach (var rule in writeRules)
            {
                if (!rule.Type.IsAssignableFrom(type))
                    continue;
                var fieldDef = modelDef.GetFieldDefinition(rule.MemberName)
                    ?? throw new NotSupportedException(
                        $"Can't apply rule on {rule.Type.Name}.{rule.MemberName} to {type.Name}, which needs a public " +
                        $"{rule.MemberName} column");
                var to = rule.RuleType switch {
                    WriteRuleType.EnsureWrites => ensureWrites,
                    WriteRuleType.OnInsert => onInsert,
                    _ => onUpdate,
                };
                to.Add(new WriteRule(fieldDef, rule.ValueFn));
            }
            return ensureWrites.Count + onInsert.Count + onUpdate.Count > 0
                ? new TableWriteRules(ensureWrites.ToArray(), onInsert.ToArray(), onUpdate.ToArray())
                : null;
        });
    }

    /// <summary>
    /// The filters that apply to the table, with interface filters rebound to the table's properties.
    /// Filters registered with a function are resolved on each call.
    /// </summary>
    public Expression<Func<T, bool>>[] GetEnsureFilters<T>()
    {
        if (ensureFilters.Length == 0)
            return [];

        var filterFns = ensureFiltersByTable.GetOrAdd(typeof(T), _ => {
            var to = new List<Func<LambdaExpression?>>();
            foreach (var filter in ensureFilters)
            {
                if (filter.Type.IsAssignableFrom(typeof(T)))
                    to.Add(filter.For(typeof(T)));
            }
            return to.ToArray();
        });
        if (filterFns.Length == 0)
            return [];

        var typed = new List<Expression<Func<T, bool>>>(filterFns.Length);
        foreach (var filterFn in filterFns)
        {
            if (filterFn() is Expression<Func<T, bool>> filter)
                typed.Add(filter);
        }
        return typed.ToArray();
    }

    /// <summary>
    /// Whether any filters currently apply to the table
    /// </summary>
    public bool HasEnsureFilters<T>() => GetEnsureFilters<T>().Length > 0;

    private sealed class EnsureFilterDef
    {
        private readonly LambdaExpression? predicate;
        private readonly Func<LambdaExpression?>? predicateFn;

        public EnsureFilterDef(Type type, LambdaExpression? predicate, Func<LambdaExpression?>? predicateFn)
        {
            Type = type;
            this.predicate = predicate;
            this.predicateFn = predicateFn;
            if (predicate != null)
            {
                // Closures are shown by their type, so the same filter from 2 calls has the same shape
                Shape = predicate.ToString();
                Values = CapturedValues.Of(predicate);
            }
        }

        public Type Type { get; }
        public LambdaExpression? Predicate => predicate;
        public Func<LambdaExpression?>? PredicateFn => predicateFn;

        /// <summary>
        /// The filter's expression and the values it captured when it was registered, to detect duplicates
        /// </summary>
        public string? Shape { get; }
        public List<object?> Values { get; } = [];

        /// <summary>
        /// Resolves the filter for the table: fixed filters are rebound once, filters from a function are rebound
        /// each time as the function can return a different filter, or null for no filter
        /// </summary>
        public Func<LambdaExpression?> For(Type tableType)
        {
            if (predicateFn != null)
            {
                return () => predicateFn() is { } filter
                    ? Rebind(filter, tableType)
                    : null;
            }

            var rebound = Rebind(predicate!, tableType);
            return () => rebound;
        }

        private LambdaExpression Rebind(LambdaExpression filter, Type tableType) => tableType == Type
            ? filter
            : TableTypeRebinder.Rebind(filter, tableType);
    }
}

internal enum WriteRuleType
{
    EnsureWrites,
    OnInsert,
    OnUpdate,
}

internal sealed class WriteRuleDef(Type type, WriteRuleType ruleType, string memberName, Func<object?> valueFn)
{
    public Type Type { get; } = type;
    public WriteRuleType RuleType { get; } = ruleType;
    public string MemberName { get; } = memberName;
    public Func<object?> ValueFn { get; } = valueFn;

    /// <summary>
    /// The rule's value when it's not from a function, to detect duplicate and conflicting rules
    /// </summary>
    public bool HasValue { get; init; }
    public object? Value { get; init; }

    public bool HasSameValue(WriteRuleDef other) => HasValue
        ? other.HasValue && Equals(Value, other.Value)
        : !other.HasValue && Equals(ValueFn, other.ValueFn);
}

internal enum CapturedValuesComparison
{
    Same,
    Different,
    Unknown,
}

/// <summary>
/// The values an expression captures, e.g. the tenantId in x =&gt; x.TenantId == tenantId, read when it's registered
/// </summary>
internal sealed class CapturedValues : ExpressionVisitor
{
    private readonly List<object?> values = [];

    public static List<object?> Of(Expression expression)
    {
        var visitor = new CapturedValues();
        visitor.Visit(expression);
        return visitor.values;
    }

    /// <summary>
    /// Whether the values of 2 expressions with the same shape are the same. Different values that aren't simple
    /// values, e.g. collections, can't be compared.
    /// </summary>
    public static CapturedValuesComparison Compare(List<object?> a, List<object?> b)
    {
        if (a.Count != b.Count)
            return CapturedValuesComparison.Unknown;

        var result = CapturedValuesComparison.Same;
        for (var i = 0; i < a.Count; i++)
        {
            if (Equals(a[i], b[i]))
                continue;
            if (!IsSimple(a[i]) || !IsSimple(b[i]))
                return CapturedValuesComparison.Unknown;
            result = CapturedValuesComparison.Different;
        }
        return result;
    }

    private static bool IsSimple(object? value) =>
        value == null || value is string || value.GetType().IsValueType;

    protected override Expression VisitMember(MemberExpression node)
    {
        if (TryEvaluate(node, out var value))
        {
            values.Add(value);
            return node;
        }
        return base.VisitMember(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        values.Add(node.Value);
        return node;
    }

    // A captured variable is a member of a closure, e.g. value(Closure).tenantId or value(Closure).user.TenantId
    private static bool TryEvaluate(Expression? expression, out object? value)
    {
        value = null;
        if (expression is ConstantExpression constant)
        {
            value = constant.Value;
            return true;
        }
        if (expression is not MemberExpression { Expression: not null } member
            || !TryEvaluate(member.Expression, out var target) || target == null)
            return false;

        try
        {
            switch (member.Member)
            {
                case FieldInfo field:
                    value = field.GetValue(target);
                    return true;
                case PropertyInfo property:
                    value = property.GetValue(target);
                    return true;
            }
        }
        catch (Exception)
        {
            // treated as a value that can't be compared
        }
        return false;
    }
}

/// <summary>
/// A rule for a table's column, with its value converted to the column's type
/// </summary>
internal sealed class WriteRule(FieldDefinition field, Func<object?> valueFn)
{
    public FieldDefinition Field { get; } = field;

    public object? GetValue() => ToFieldValue(valueFn());

    public object? ToFieldValue(object? value)
    {
        if (value == null || Field.FieldType.IsInstanceOfType(value))
            return value;
        return value.ConvertTo(Nullable.GetUnderlyingType(Field.FieldType) ?? Field.FieldType);
    }

    public bool IsDefault(object? value) =>
        value == null || value.Equals(Field.FieldType.GetDefaultValue());
}

/// <summary>
/// The write rules of a connection that apply to a table
/// </summary>
internal sealed class TableWriteRules(WriteRule[] ensureWrites, WriteRule[] onInsert, WriteRule[] onUpdate)
{
    public WriteRule[] EnsureWrites { get; } = ensureWrites;
    public WriteRule[] OnInsert { get; } = onInsert;
    public WriteRule[] OnUpdate { get; } = onUpdate;
}

/// <summary>
/// Rebinds a lambda over an interface or base type, e.g. (IHasTenantId x) =&gt; x.TenantId == id, to a table type that
/// implements it, e.g. (Order x) =&gt; x.TenantId == id, so its members resolve to the table's columns
/// </summary>
internal sealed class TableTypeRebinder : ExpressionVisitor
{
    private readonly ParameterExpression from;
    private readonly ParameterExpression to;

    private TableTypeRebinder(ParameterExpression from, ParameterExpression to)
    {
        this.from = from;
        this.to = to;
    }

    public static LambdaExpression Rebind(LambdaExpression lambda, Type tableType)
    {
        if (lambda.Parameters.Count != 1)
            throw new ArgumentException("Expected a filter with a single parameter", nameof(lambda));

        var from = lambda.Parameters[0];
        var to = Expression.Parameter(tableType, from.Name);
        var body = new TableTypeRebinder(from, to).Visit(lambda.Body);
        var delegateType = typeof(Func<,>).MakeGenericType(tableType, lambda.ReturnType);
        return Expression.Lambda(delegateType, body!, to);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression == from)
        {
            var property = to.Type.GetProperty(node.Member.Name, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new NotSupportedException(
                    $"Can't apply filter on {from.Type.Name}.{node.Member.Name} to {to.Type.Name}, which needs a public " +
                    $"{node.Member.Name} property");
            return Expression.Property(to, property);
        }
        return base.VisitMember(node);
    }

    protected override Expression VisitParameter(ParameterExpression node) => node == from
        ? throw new NotSupportedException(
            $"Filters on {from.Type.Name} can only use its properties, e.g. x => x.TenantId == tenantId")
        : base.VisitParameter(node);
}
