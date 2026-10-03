#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ServiceStack.OrmLite;

/// <summary>
/// The mandatory filters and write rules of a connection, from the FilterSets it uses, applied to every statement
/// OrmLite creates for a table they apply to. Immutable, using a FilterSet creates a new instance.
/// </summary>
public sealed class OrmLiteConnectionFilters
{
    public static readonly OrmLiteConnectionFilters Empty = new([], [], []);

    private readonly BoundFilterSet[] sets;
    private readonly EnsureFilterDef[] ensureFilters;
    private readonly ConcurrentDictionary<Type, Func<LambdaExpression?>[]> ensureFiltersByTable = new();

    private readonly WriteRuleDef[] writeRules;
    private readonly ConcurrentDictionary<Type, TableWriteRules?> writeRulesByTable = new();

    private OrmLiteConnectionFilters(BoundFilterSet[] sets, EnsureFilterDef[] ensureFilters, WriteRuleDef[] writeRules)
    {
        this.sets = sets;
        this.ensureFilters = ensureFilters;
        this.writeRules = writeRules;
    }

    public bool IsEmpty => ensureFilters.Length == 0 && writeRules.Length == 0;

    /// <summary>
    /// Whether the connection has any Ensure, OnInsert or OnUpdate rules
    /// </summary>
    public bool HasWriteRules => writeRules.Length > 0;

    /// <summary>
    /// Adds the filters and rules of the set with its scope. Using a set again with the same scope is ignored,
    /// and with a different scope throws, as rows would need to match both.
    /// </summary>
    internal OrmLiteConnectionFilters Add(BoundFilterSet bound)
    {
        foreach (var existing in sets)
        {
            if (!ReferenceEquals(existing.Set, bound.Set))
                continue;
            if (ReferenceEquals(existing.Scope, bound.Scope) || Equals(existing.Scope, bound.Scope))
                return this;
            throw new InvalidOperationException(
                "The connection already uses this FilterSet with a different scope, rows would need to match both");
        }

        var filters = new List<EnsureFilterDef>(ensureFilters);
        var rules = new List<WriteRuleDef>(writeRules);
        foreach (var rule in bound.Rules)
        {
            if (rule.BindCondition(bound.Scope) is { } condition)
                filters.Add(new EnsureFilterDef(rule.Type, condition));

            if (rule.ValueFn == null)
                continue;
            var scope = bound.Scope;
            var valueFn = rule.ValueFn;
            Func<object?> value = () => valueFn(scope);
            if (rule.RuleType == FilterRuleType.Ensure)
                rules.Add(new WriteRuleDef(rule.Type, WriteRuleType.EnsureWrites, rule.MemberName!, value));
            else
                rules.Add(new WriteRuleDef(rule.Type, rule.RuleType == FilterRuleType.OnInsert
                    ? WriteRuleType.OnInsert : WriteRuleType.OnUpdate, rule.MemberName!, value));
        }
        return new OrmLiteConnectionFilters([..sets, bound], filters.ToArray(), rules.ToArray());
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
    /// The filters that apply to the table, with interface filters rebound to the table's properties
    /// </summary>
    public Expression<Func<T, bool>>[] GetEnsureFilters<T>()
    {
        if (ensureFilters.Length == 0)
            return [];

        var filterFns = GetFilterFns(typeof(T));
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

    private Func<LambdaExpression?>[] GetFilterFns(Type tableType) =>
        ensureFiltersByTable.GetOrAdd(tableType, _ => {
            var to = new List<Func<LambdaExpression?>>();
            foreach (var filter in ensureFilters)
            {
                if (filter.Type.IsAssignableFrom(tableType))
                    to.Add(filter.For(tableType));
            }
            return to.ToArray();
        });

    /// <summary>
    /// Whether filters are registered for the table
    /// </summary>
    internal bool MayFilter(Type tableType) => ensureFilters.Length > 0 && GetFilterFns(tableType).Length > 0;

    /// <summary>
    /// Whether any filters apply to the table
    /// </summary>
    public bool HasEnsureFilters<T>() => GetEnsureFilters<T>().Length > 0;

    private sealed class EnsureFilterDef(Type type, LambdaExpression predicate)
    {
        public Type Type { get; } = type;

        /// <summary>
        /// The filter for the table, which is rebound once for tables of an interface or base class
        /// </summary>
        public Func<LambdaExpression?> For(Type tableType)
        {
            var rebound = tableType == Type ? predicate : TableTypeRebinder.Rebind(predicate, tableType);
            return () => rebound;
        }
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

    // An empty string isn't set, e.g. a property initialized with: string TenantId { get; set; } = ""
    public bool IsDefault(object? value) =>
        value == null || value is string { Length: 0 } || value.Equals(Field.FieldType.GetDefaultValue());
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
