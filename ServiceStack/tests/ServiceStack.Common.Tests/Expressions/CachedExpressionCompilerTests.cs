using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
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
        public int Id { get; set; }
        public int Count { get; set; }
    }

    private class ValueEntity<T>
    {
        public T Value { get; set; }
    }

    private sealed class BoundValue(int value)
    {
        public int Value { get; } = value;

        public override string ToString() => nameof(BoundValue);
    }

    private readonly struct ImmutableValue(int value)
    {
        public int Value { get; } = value;

        public override string ToString() => nameof(ImmutableValue);
    }

    private enum TestEnum
    {
        First,
        Second,
    }

    private sealed class InstanceEvaluator(int count)
    {
        public Entity Evaluate()
        {
            Expression<Func<Entity>> expr = () => new Entity { Count = count };
            return (Entity)CachedExpressionCompiler.Evaluate(expr.Body);
        }

        public bool IsExpressionCacheable()
        {
            Expression<Func<Entity>> expr = () => new Entity { Count = count };
            return ExpressionCacheKey.CanCache(expr);
        }
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

    [Test]
    public void Evaluate_member_init_does_not_reuse_captured_instance()
    {
        var firstEvaluator = new InstanceEvaluator(7);
        var first = firstEvaluator.Evaluate();
        var second = new InstanceEvaluator(99).Evaluate();

        Assert.That(firstEvaluator.IsExpressionCacheable(), Is.False);
        Assert.That(first.Count, Is.EqualTo(7));
        Assert.That(second.Count, Is.EqualTo(99));
    }

    [Test]
    public void Evaluate_member_init_does_not_reuse_different_member_binding()
    {
        var first = EvaluateMemberBinding(nameof(Entity.Id), 7);
        var second = EvaluateMemberBinding(nameof(Entity.Count), 7);

        Assert.That(first.Id, Is.EqualTo(7));
        Assert.That(second.Count, Is.EqualTo(7));
    }

    [Test]
    public void Compile_member_init_does_not_reuse_manually_bound_reference()
    {
        var firstExpression = CreateBoundValueExpression(new BoundValue(7));
        var first = CachedExpressionCompiler.Compile(firstExpression)(null);
        var second = CachedExpressionCompiler.Compile(CreateBoundValueExpression(new BoundValue(99)))(null);

        Assert.That(ExpressionCacheKey.CanCache(firstExpression), Is.False);
        Assert.That(first.Count, Is.EqualTo(7));
        Assert.That(second.Count, Is.EqualTo(99));
    }

    [Test]
    public void Compile_member_init_does_not_reuse_manually_bound_value_type()
    {
        var firstExpression = CreateBoundValueExpression(new ImmutableValue(7));
        var first = CachedExpressionCompiler.Compile(firstExpression)(null);
        var second = CachedExpressionCompiler.Compile(CreateBoundValueExpression(new ImmutableValue(99)))(null);

        Assert.That(ExpressionCacheKey.CanCache(firstExpression), Is.False);
        Assert.That(first.Count, Is.EqualTo(7));
        Assert.That(second.Count, Is.EqualTo(99));
    }

    [Test]
    public void Evaluate_member_init_does_not_reuse_values_previously_classified_as_immutable()
    {
        AssertCapturedValuesAreNotReused(7, 99);
        AssertCapturedValuesAreNotReused(7.5m, 99.5m);
        AssertCapturedValuesAreNotReused(
            new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc),
            new DateTime(2026, 9, 11, 12, 13, 14, DateTimeKind.Utc));
        AssertCapturedValuesAreNotReused(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("99999999-9999-9999-9999-999999999999"));
        AssertCapturedValuesAreNotReused(TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(99));
        AssertCapturedValuesAreNotReused(TestEnum.First, TestEnum.Second);
        AssertCapturedValuesAreNotReused<int?>(7, 99);
        AssertCapturedValuesAreNotReused(new ImmutableValue(7), new ImmutableValue(99));
    }

    [Test]
    public void Evaluate_member_init_does_not_share_bound_state_between_threads()
    {
        const int count = 32;
        var results = new int[count];

        Parallel.For(0, count, i =>
            results[i] = new InstanceEvaluator(i).Evaluate().Count);

        Assert.That(results, Is.EqualTo(Enumerable.Range(0, count).ToArray()));
    }

    [Test]
    public async Task Evaluate_member_init_does_not_reuse_async_state()
    {
        var first = await EvaluateAsync(7);
        var second = await EvaluateAsync(99);

        Assert.That(first.Count, Is.EqualTo(7));
        Assert.That(second.Count, Is.EqualTo(99));
    }

    [Test]
    public void Evaluate_member_init_does_not_reuse_iterator_state()
    {
        var first = EvaluateIterator(7).Single();
        var second = EvaluateIterator(99).Single();

        Assert.That(first.Count, Is.EqualTo(7));
        Assert.That(second.Count, Is.EqualTo(99));
    }

    // Evaluate() interprets expressions that can't be served from a cache instead of JIT compiling
    // them. These cover the non-fingerprintable shapes OrmLite evaluates on every query.

    [Test]
    public void Evaluate_new_expression_with_captured_arguments()
    {
        Assert.That(EvaluateNewDateTime(2020, 1, 7), Is.EqualTo(new DateTime(2020, 1, 7)));
        Assert.That(EvaluateNewDateTime(2026, 9, 11), Is.EqualTo(new DateTime(2026, 9, 11)));
    }

    [Test]
    public void Evaluate_list_init_with_captured_values()
    {
        Assert.That(EvaluateListInit(1, 2), Is.EqualTo(new List<int> { 1, 2 }));
        Assert.That(EvaluateListInit(3, 4), Is.EqualTo(new List<int> { 3, 4 }));
    }

    [Test]
    public void Evaluate_object_array_with_mixed_captured_and_literal_values()
    {
        Assert.That(EvaluateObjectArray(7), Is.EqualTo(new object[] { 7, 0, "x" }));
        Assert.That(EvaluateObjectArray(99), Is.EqualTo(new object[] { 99, 0, "x" }));
    }

    [Test]
    public void Evaluate_strips_implicit_span_conversion_and_returns_source_array()
    {
        // C# 14 binds array.Contains(x) to MemoryExtensions.Contains(ReadOnlySpan<T>, T); callers
        // that evaluate the span argument end up boxing a ref struct, which the compiler can't do.
        var opImplicit = typeof(ReadOnlySpan<int>).GetMethod("op_Implicit", [typeof(int[])])
            ?? throw new InvalidOperationException("ReadOnlySpan<int>.op_Implicit(int[]) not found");
        var source = new[] { 1, 2, 3 };
        var spanExpr = Expression.Call(opImplicit, Expression.Constant(source));

        var result = CachedExpressionCompiler.Evaluate(spanExpr);

        Assert.That(result, Is.SameAs(source));
    }

    [Test]
    public void Compile_slow_path_returns_reusable_delegate()
    {
        Expression<Func<object, Entity>> expr = model => new Entity { Count = (int)model, Id = 1 };
        var fn = CachedExpressionCompiler.Compile(expr);

        Assert.That(fn(7).Count, Is.EqualTo(7));
        Assert.That(fn(99).Count, Is.EqualTo(99));
        Assert.That(fn(99).Id, Is.EqualTo(1));
    }

    private static DateTime EvaluateNewDateTime(int year, int month, int day)
    {
        Expression<Func<DateTime>> expr = () => new DateTime(year, month, day);
        return (DateTime)CachedExpressionCompiler.Evaluate(expr.Body);
    }

    private static List<int> EvaluateListInit(int a, int b)
    {
        Expression<Func<List<int>>> expr = () => new List<int> { a, b };
        return (List<int>)CachedExpressionCompiler.Evaluate(expr.Body);
    }

    private static object[] EvaluateObjectArray(int value)
    {
        Expression<Func<object[]>> expr = () => new object[] { value, 0, "x" };
        return (object[])CachedExpressionCompiler.Evaluate(expr.Body);
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

    private static Entity EvaluateMemberBinding(string memberName, int value)
    {
        var member = typeof(Entity).GetProperty(memberName)
            ?? throw new ArgumentException($"Unknown Entity member '{memberName}'", nameof(memberName));
        var binding = Expression.Bind(member, Expression.Constant(value));
        var expr = Expression.MemberInit(Expression.New(typeof(Entity)), binding);
        return (Entity)CachedExpressionCompiler.Evaluate(expr);
    }

    private static Expression<Func<object, Entity>> CreateBoundValueExpression(BoundValue value)
    {
        var model = Expression.Parameter(typeof(object), "model");
        var valueExpression = Expression.Property(Expression.Constant(value), nameof(BoundValue.Value));
        return CreateBoundValueExpression(model, valueExpression);
    }

    private static Expression<Func<object, Entity>> CreateBoundValueExpression(ImmutableValue value)
    {
        var model = Expression.Parameter(typeof(object), "model");
        var valueExpression = Expression.Property(Expression.Constant(value), nameof(ImmutableValue.Value));
        return CreateBoundValueExpression(model, valueExpression);
    }

    private static Expression<Func<object, Entity>> CreateBoundValueExpression(
        ParameterExpression model, Expression valueExpression)
    {
        var member = typeof(Entity).GetProperty(nameof(Entity.Count))
            ?? throw new InvalidOperationException($"Could not find {nameof(Entity.Count)} property");
        var binding = Expression.Bind(member, valueExpression);
        var body = Expression.MemberInit(Expression.New(typeof(Entity)), binding);
        return Expression.Lambda<Func<object, Entity>>(body, model);
    }

    private static void AssertCapturedValuesAreNotReused<T>(T first, T second)
    {
        Assert.That(EvaluateCapturedValue(first), Is.EqualTo(first));
        Assert.That(EvaluateCapturedValue(second), Is.EqualTo(second));
    }

    private static T EvaluateCapturedValue<T>(T value)
    {
        Expression<Func<ValueEntity<T>>> expr = () => new ValueEntity<T> { Value = value };
        return ((ValueEntity<T>)CachedExpressionCompiler.Evaluate(expr.Body)).Value;
    }

    private static async Task<Entity> EvaluateAsync(int value)
    {
        await Task.Yield();
        Expression<Func<Entity>> expr = () => new Entity { Count = value };
        return (Entity)CachedExpressionCompiler.Evaluate(expr.Body);
    }

    private static IEnumerable<Entity> EvaluateIterator(int value)
    {
        Expression<Func<Entity>> expr = () => new Entity { Count = value };
        yield return (Entity)CachedExpressionCompiler.Evaluate(expr.Body);
    }
}

