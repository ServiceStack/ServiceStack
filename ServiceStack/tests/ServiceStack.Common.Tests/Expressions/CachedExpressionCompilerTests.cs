using System;
using System.Linq.Expressions;
using NUnit.Framework;
using ServiceStack.ExpressionUtil;

namespace ServiceStack.Common.Tests.Expressions;

[TestFixture]
public class CachedExpressionCompilerTests
{
    private class TypedBox<T>
    {
    }

    private class Entity
    {
        public int Count { get; set; }
    }

    [Test]
    public void Expression_cache_keys_include_expression_types()
    {
        Expression<Func<object, object>> intExpr = _ => new TypedBox<int>();
        Expression<Func<object, object>> stringExpr = _ => new TypedBox<string>();

        Assert.That(ExpressionCacheKey.TryGetKey(intExpr, out var intKey), Is.True);
        Assert.That(ExpressionCacheKey.TryGetKey(stringExpr, out var stringKey), Is.True);
        Assert.That(intKey, Is.Not.EqualTo(stringKey));
    }

    [Test]
    public void Evaluate_does_not_reuse_cached_delegate_for_different_expression_types()
    {
        var intExpr = Expression.New(typeof(TypedBox<int>));
        var stringExpr = Expression.New(typeof(TypedBox<string>));

        var intResult = CachedExpressionCompiler.Evaluate(intExpr);
        var stringResult = CachedExpressionCompiler.Evaluate(stringExpr);

        Assert.That(intResult, Is.TypeOf<TypedBox<int>>());
        Assert.That(stringResult, Is.TypeOf<TypedBox<string>>());
    }

    [Test]
    public void Evaluate_member_init_does_not_reuse_captured_primitive_value()
    {
        var first = EvaluateMemberInit(7);
        var second = EvaluateMemberInit(99);

        Assert.That(first.Count, Is.EqualTo(7));
        Assert.That(second.Count, Is.EqualTo(99));
    }

    [Test]
    public void Evaluate_new_array_does_not_reuse_captured_primitive_value()
    {
        var first = EvaluateNewArray(1);
        var second = EvaluateNewArray(2);

        Assert.That(first, Is.EqualTo(new[] { 1, 0 }));
        Assert.That(second, Is.EqualTo(new[] { 2, 0 }));
    }

    private static Entity EvaluateMemberInit(int count)
    {
        Expression<Func<Entity>> expr = () => new Entity { Count = count };
        return (Entity)CachedExpressionCompiler.Evaluate(expr.Body);
    }

    private static int[] EvaluateNewArray(int value)
    {
        Expression<Func<int[]>> expr = () => new[] { value, 0 };
        return (int[])CachedExpressionCompiler.Evaluate(expr.Body);
    }
}

