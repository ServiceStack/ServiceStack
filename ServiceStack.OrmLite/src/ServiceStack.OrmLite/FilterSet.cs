#nullable enable
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite;

/// <summary>
/// Mandatory filters and write rules that are declared once, then used by connections with db.UseFilters(), e.g:
/// <code>
/// public static readonly FilterSet&lt;WorkspaceScope&gt; WorkspaceFilters = FilterSet.Create&lt;WorkspaceScope&gt;(f => {
///     f.Ensure&lt;IHasWorkspaceId&gt;(x => x.WorkspaceId, s => s.WorkspaceId);
///     f.Filter&lt;Workspace&gt;((x, s) => x.Id == s.WorkspaceId);
/// });
///
/// db.UseFilters(WorkspaceFilters.For(scope));
/// </code>
/// Their values are read from the scope each time they're used, so a rule can't capture other values. Filters and
/// rules without a scope, e.g. for soft deletes, are a FilterSet that connections use with db.UseFilters(set).
/// </summary>
public sealed class FilterSet
{
    internal FilterRule[] Rules { get; }

    internal FilterSet(FilterRule[] rules) => Rules = rules;

    /// <summary>
    /// Filters and rules that read their values from a scope, which each connection provides with set.For(scope)
    /// </summary>
    public static FilterSet<TScope> Create<TScope>(Action<FilterSetBuilder<TScope>> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));
        var builder = new FilterSetBuilder<TScope>();
        configure(builder);
        return new FilterSet<TScope>(builder.Rules.ToArray());
    }

    /// <summary>
    /// Filters and rules without a scope, e.g. for soft deletes, which connections use with db.UseFilters(set)
    /// </summary>
    public static FilterSet Create(Action<FilterSetBuilder> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));
        var builder = new FilterSetBuilder();
        configure(builder);
        return new FilterSet(builder.Rules.ToArray());
    }
}

/// <summary>
/// Mandatory filters and write rules that read their values from a TScope, see FilterSet.Create()
/// </summary>
public sealed class FilterSet<TScope>
{
    internal FilterRule[] Rules { get; }

    internal FilterSet(FilterRule[] rules) => Rules = rules;

    /// <summary>
    /// The filters and rules with the scope they read their values from, to use with db.UseFilters()
    /// </summary>
    public BoundFilterSet For(TScope scope) => scope == null
        ? throw new ArgumentNullException(nameof(scope))
        : new BoundFilterSet(this, scope, Rules);
}

/// <summary>
/// A FilterSet with the scope it reads its values from, returned by set.For(scope)
/// </summary>
public sealed class BoundFilterSet
{
    internal object Set { get; }
    internal object? Scope { get; }
    internal FilterRule[] Rules { get; }

    internal BoundFilterSet(object set, object? scope, FilterRule[] rules)
    {
        Set = set;
        Scope = scope;
        Rules = rules;
    }
}

internal enum FilterRuleType
{
    Filter,
    Ensure,
    OnInsert,
    OnUpdate,
}

/// <summary>
/// A filter or write rule for a table, or every table that implements an interface or base class
/// </summary>
internal sealed class FilterRule(Type type, FilterRuleType ruleType)
{
    public Type Type { get; } = type;
    public FilterRuleType RuleType { get; } = ruleType;

    /// <summary>
    /// The filter of Filter and Ensure rules, with the table and scope as its parameters: (x, s) => condition
    /// </summary>
    public LambdaExpression? Condition { get; set; }

    /// <summary>
    /// The column of Ensure, OnInsert and OnUpdate rules
    /// </summary>
    public string? MemberName { get; set; }

    /// <summary>
    /// The value of Ensure, OnInsert and OnUpdate rules, read from the scope
    /// </summary>
    public Func<object?, object?>? ValueFn { get; set; }

    /// <summary>
    /// The filter with the scope in place of its parameter: x => condition
    /// </summary>
    public LambdaExpression? BindCondition(object? scope)
    {
        if (Condition == null)
            return null;
        if (Condition.Parameters.Count == 1)
            return Condition;
        var scopeParam = Condition.Parameters[1];
        var body = new ReplaceParameter(scopeParam, Expression.Constant(scope, scopeParam.Type)).Visit(Condition.Body);
        return Expression.Lambda(body!, Condition.Parameters[0]);
    }

    private sealed class ReplaceParameter(ParameterExpression from, Expression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}

/// <summary>
/// Declares the filters and rules of a FilterSet with a scope
/// </summary>
public sealed class FilterSetBuilder<TScope>
{
    internal readonly List<FilterRule> Rules = [];

    internal FilterSetBuilder() {}

    /// <summary>
    /// Rows of the table, or every table implementing the interface, have the value. Queries, updates and deletes
    /// only see rows with it, inserts set it when it isn't set, and writing a different value throws, e.g:
    /// <para>f.Ensure&lt;IHasTenantId&gt;(x =&gt; x.TenantId, s =&gt; s.TenantId);</para>
    /// </summary>
    public FilterSetBuilder<TScope> Ensure<T>(Expression<Func<T, object?>> field, Expression<Func<TScope, object?>> value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        FilterRules.AssertNoCapturedValues(value, nameof(value));
        var member = FilterRules.ToMember(field);
        var valueBody = FilterRules.WithoutConvert(value.Body);
        var memberType = member.Type;
        var condition = Expression.Lambda<Func<T, TScope, bool>>(
            Expression.Equal(member, valueBody.Type == memberType ? valueBody : Expression.Convert(valueBody, memberType)),
            (ParameterExpression)member.Expression!, value.Parameters[0]);
        var valueFn = value.Compile();
        Rules.Add(new FilterRule(typeof(T), FilterRuleType.Ensure) {
            Condition = condition,
            MemberName = member.Member.Name,
            ValueFn = s => valueFn((TScope)s!),
        });
        return this;
    }

    /// <summary>
    /// Queries, updates and deletes only see rows of the table, or every table implementing the interface, that
    /// match the condition, e.g:
    /// <para>f.Filter&lt;Workspace&gt;((x, s) =&gt; x.Id == s.WorkspaceId);</para>
    /// Conditions that only read the scope decide if the rest of the condition applies, e.g:
    /// <para>f.Filter&lt;ApiKey&gt;((x, s) =&gt; s.WorkspaceId == null || x.RefIdStr == s.WorkspaceId);</para>
    /// </summary>
    public FilterSetBuilder<TScope> Filter<T>(Expression<Func<T, TScope, bool>> condition)
    {
        if (condition == null)
            throw new ArgumentNullException(nameof(condition));
        FilterRules.AssertNoCapturedValues(condition, nameof(condition));
        Rules.Add(new FilterRule(typeof(T), FilterRuleType.Filter) { Condition = condition });
        return this;
    }

    /// <summary>
    /// Set the column of rows inserted, e.g:
    /// <para>f.OnInsert&lt;IAudit&gt;(x =&gt; x.CreatedBy, s =&gt; s.UserId);</para>
    /// </summary>
    public FilterSetBuilder<TScope> OnInsert<T>(Expression<Func<T, object?>> field, Func<TScope, object?> value) =>
        AddWriteRule(FilterRuleType.OnInsert, field, value);

    /// <summary>
    /// Set the column of rows updated, e.g:
    /// <para>f.OnUpdate&lt;IAudit&gt;(x =&gt; x.ModifiedDate, _ =&gt; DateTime.UtcNow);</para>
    /// </summary>
    public FilterSetBuilder<TScope> OnUpdate<T>(Expression<Func<T, object?>> field, Func<TScope, object?> value) =>
        AddWriteRule(FilterRuleType.OnUpdate, field, value);

    /// <summary>
    /// Set the column of rows inserted and updated, the same as both OnInsert and OnUpdate
    /// </summary>
    public FilterSetBuilder<TScope> OnWrite<T>(Expression<Func<T, object?>> field, Func<TScope, object?> value) =>
        OnInsert(field, value).OnUpdate(field, value);

    private FilterSetBuilder<TScope> AddWriteRule<T>(FilterRuleType ruleType, Expression<Func<T, object?>> field,
        Func<TScope, object?> value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        Rules.Add(new FilterRule(typeof(T), ruleType) {
            MemberName = FilterRules.ToMember(field).Member.Name,
            ValueFn = s => value((TScope)s!),
        });
        return this;
    }
}

/// <summary>
/// Declares the filters and rules of a FilterSet without a scope
/// </summary>
public sealed class FilterSetBuilder
{
    internal readonly List<FilterRule> Rules = [];

    internal FilterSetBuilder() {}

    /// <summary>
    /// Rows of the table, or every table implementing the interface, have the value. Queries, updates and deletes
    /// only see rows with it, inserts set it when it isn't set, and writing a different value throws
    /// </summary>
    public FilterSetBuilder Ensure<T>(Expression<Func<T, object?>> field, object? value)
    {
        if (value is Delegate)
            throw new ArgumentException("Use a FilterSet with a scope for values that change", nameof(value));
        var member = FilterRules.ToMember(field);
        var memberValue = FilterRules.ToMemberValue(member.Type, value);
        var condition = Expression.Lambda<Func<T, bool>>(
            Expression.Equal(member, Expression.Constant(memberValue, member.Type)),
            (ParameterExpression)member.Expression!);
        Rules.Add(new FilterRule(typeof(T), FilterRuleType.Ensure) {
            Condition = condition,
            MemberName = member.Member.Name,
            ValueFn = _ => memberValue,
        });
        return this;
    }

    /// <summary>
    /// Queries, updates and deletes only see rows of the table, or every table implementing the interface, that
    /// match the condition, e.g:
    /// <para>f.Filter&lt;ISoftDelete&gt;(x =&gt; !x.IsDeleted);</para>
    /// </summary>
    public FilterSetBuilder Filter<T>(Expression<Func<T, bool>> condition)
    {
        if (condition == null)
            throw new ArgumentNullException(nameof(condition));
        FilterRules.AssertNoCapturedValues(condition, nameof(condition));
        Rules.Add(new FilterRule(typeof(T), FilterRuleType.Filter) { Condition = condition });
        return this;
    }

    /// <summary>
    /// Set the column of rows inserted to the value returned by the function, e.g:
    /// <para>f.OnInsert&lt;IAudit&gt;(x =&gt; x.CreatedDate, () =&gt; DateTime.UtcNow);</para>
    /// </summary>
    public FilterSetBuilder OnInsert<T>(Expression<Func<T, object?>> field, Func<object?> value) =>
        AddWriteRule(FilterRuleType.OnInsert, field, value);

    /// <summary>
    /// Set the column of rows updated to the value returned by the function
    /// </summary>
    public FilterSetBuilder OnUpdate<T>(Expression<Func<T, object?>> field, Func<object?> value) =>
        AddWriteRule(FilterRuleType.OnUpdate, field, value);

    /// <summary>
    /// Set the column of rows inserted and updated, the same as both OnInsert and OnUpdate
    /// </summary>
    public FilterSetBuilder OnWrite<T>(Expression<Func<T, object?>> field, Func<object?> value) =>
        OnInsert(field, value).OnUpdate(field, value);

    private FilterSetBuilder AddWriteRule<T>(FilterRuleType ruleType, Expression<Func<T, object?>> field,
        Func<object?> value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        Rules.Add(new FilterRule(typeof(T), ruleType) {
            MemberName = FilterRules.ToMember(field).Member.Name,
            ValueFn = _ => value(),
        });
        return this;
    }
}

internal static class FilterRules
{
    internal static Expression WithoutConvert(Expression body)
    {
        while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
            body = convert.Operand;
        return body;
    }

    internal static MemberExpression ToMember<T>(Expression<Func<T, object?>> field)
    {
        if (field == null)
            throw new ArgumentNullException(nameof(field));
        if (WithoutConvert(field.Body) is not MemberExpression member || member.Expression != field.Parameters[0])
            throw new ArgumentException("Expected a column like: x => x.TenantId", nameof(field));
        return member;
    }

    // The value as the column's type, e.g. 1 for a long column
    internal static object? ToMemberValue(Type memberType, object? value)
    {
        if (value == null)
            return null;
        var type = Nullable.GetUnderlyingType(memberType) ?? memberType;
        return type.IsInstanceOfType(value) ? value : value.ConvertTo(type);
    }

    /// <summary>
    /// Values have to be read from the scope, so a rule is the same for every connection that uses it
    /// </summary>
    internal static void AssertNoCapturedValues(LambdaExpression lambda, string paramName)
    {
        if (CapturedValueFinder.Find(lambda) is { } captured)
        {
            throw new ArgumentException($"FilterSet rules can't use the variable '{captured}' from outside the rule. " +
                "Read the value from the scope instead, e.g: (x, s) => x.TenantId == s.TenantId", paramName);
        }
    }

    private sealed class CapturedValueFinder : ExpressionVisitor
    {
        private string? captured;

        public static string? Find(Expression expression)
        {
            var finder = new CapturedValueFinder();
            finder.Visit(expression);
            return finder.captured;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            // A variable the lambda captures is a field of the closure the compiler generates for it, and a field of
            // the class it's declared in is a member of 'this'. Static members aren't captured.
            if (node.Expression is ConstantExpression { Value: not null })
                captured ??= node.Member.Name;
            return base.VisitMember(node);
        }
    }
}
