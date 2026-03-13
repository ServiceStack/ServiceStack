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
}

