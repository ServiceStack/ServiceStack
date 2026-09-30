#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ServiceStack.OrmLite;

/// <summary>
/// The mandatory filters of a connection, applied to every query OrmLite creates for a table they apply to.
/// Immutable, registering a filter creates a new instance.
/// </summary>
public sealed class OrmLiteConnectionFilters
{
    public static readonly OrmLiteConnectionFilters Empty = new([]);

    private readonly EnsureFilterDef[] ensureFilters;
    private readonly ConcurrentDictionary<Type, LambdaExpression[]> ensureFiltersByTable = new();

    private OrmLiteConnectionFilters(EnsureFilterDef[] ensureFilters) => this.ensureFilters = ensureFilters;

    public bool IsEmpty => ensureFilters.Length == 0;

    internal OrmLiteConnectionFilters AddEnsureFilter(Type type, LambdaExpression predicate)
    {
        var filters = new EnsureFilterDef[ensureFilters.Length + 1];
        ensureFilters.CopyTo(filters, 0);
        filters[ensureFilters.Length] = new EnsureFilterDef(type, predicate);
        return new OrmLiteConnectionFilters(filters);
    }

    /// <summary>
    /// The filters that apply to the table, with interface filters rebound to the table's properties
    /// </summary>
    public Expression<Func<T, bool>>[] GetEnsureFilters<T>()
    {
        if (ensureFilters.Length == 0)
            return [];

        var filters = ensureFiltersByTable.GetOrAdd(typeof(T), _ => {
            var to = new List<LambdaExpression>();
            foreach (var filter in ensureFilters)
            {
                if (filter.Type.IsAssignableFrom(typeof(T)))
                    to.Add(filter.For(typeof(T)));
            }
            return to.ToArray();
        });

        var typed = new Expression<Func<T, bool>>[filters.Length];
        for (var i = 0; i < filters.Length; i++)
            typed[i] = (Expression<Func<T, bool>>)filters[i];
        return typed;
    }

    private sealed class EnsureFilterDef(Type type, LambdaExpression predicate)
    {
        public Type Type { get; } = type;

        public LambdaExpression For(Type tableType) => tableType == Type
            ? predicate
            : TableTypeRebinder.Rebind(predicate, tableType);
    }
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
