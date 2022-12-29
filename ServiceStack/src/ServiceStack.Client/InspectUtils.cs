#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ServiceStack.Html;

public static class InspectUtils
{
    public static Expression? FindMember(Expression e) => e switch {
        UnaryExpression ue => FindMember(ue.Operand),
        MemberExpression me => me,
        _ => null
    };
    
    public static PropertyInfo? PropertyFromExpression<TModel>(Expression<Func<TModel, object?>> expr)
    {
        var memberExpr = FindMember(expr.Body) as MemberExpression;
        return memberExpr?.Member is PropertyInfo pi ? pi : null;
    }

    // Evaluates an expression (not a LambdaExpression), e.g. 2 + 2.
    public static object? Evaluate(Expression arg)
    {
        if (arg == null)
            throw new ArgumentNullException(nameof(arg));

        var func = Wrap(arg);
        return func(null);
    }

    private static readonly ParameterExpression _unusedParameterExpr = Expression.Parameter(typeof(object), "_unused");
    private static Func<object?, object?> Wrap(Expression arg)
    {
        var lambdaExpr = Expression.Lambda<Func<object?, object?>>(Expression.Convert(arg, typeof(object)), _unusedParameterExpr);
        return lambdaExpr.Compile();
    }

    public static string[] GetFieldNames<T>(this Expression<Func<T, object?>> expr)
    {
        if (expr.Body is MemberExpression member)
        {
            if (member.Member?.DeclaringType?.IsAssignableFrom(typeof(T)) == true)
                return new[] { member.Member.Name };

            var array = Evaluate(member);
            if (array is IEnumerable<string> strEnum)
                return strEnum.ToArray();
        }

        if (expr.Body is NewExpression newExpr)
            return newExpr.Arguments.OfType<MemberExpression>().Select(x => x.Member.Name).ToArray();

        if (expr.Body is MemberInitExpression init)
            return init.Bindings.Select(x => x.Member.Name).ToArray();

        if (expr.Body is NewArrayExpression newArray)
        {
            var constantExprs = newArray.Expressions.OfType<ConstantExpression>().ToList();
            if (newArray.Expressions.Count == constantExprs.Count)
                return constantExprs.Select(x => $"{x.Value}").ToArray();

            var array = Evaluate(newArray);
            if (array is string[] strArray)
                return strArray;

            return array.ConvertTo<string[]>();
        }

        if (expr.Body is UnaryExpression unary)
        {
            var unaryMember = unary.Operand as MemberExpression;
            if (unaryMember != null)
                return new[] { unaryMember.Member.Name };
        }

        throw new ArgumentException("Invalid Fields List Expression: " + expr);
    }
}