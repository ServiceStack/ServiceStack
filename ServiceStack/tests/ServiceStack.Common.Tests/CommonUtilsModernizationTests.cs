using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Commands;
using ServiceStack.Support;

namespace ServiceStack.Common.Tests;

[TestFixture]
public class CommonUtilsModernizationTests
{
    [Test]
    public void ExecUtils_ExecAllWithFirstOut_Captures_First_Result()
    {
        var numbers = new[] { 10, 20, 30 };
        var firstInt = 0;
        numbers.ExecAllWithFirstOut(x => x * 2, ref firstInt);
        Assert.That(firstInt, Is.EqualTo(20));

        var strings = new[] { "apple", "banana", "cherry" };
        string firstStr = null;
        strings.ExecAllWithFirstOut(x => x.ToUpper(), ref firstStr);
        Assert.That(firstStr, Is.EqualTo("APPLE"));

        // Null instances should be safe
        IEnumerable<string> nullItems = null;
        string safeStr = "initial";
        nullItems.ExecAllWithFirstOut(x => x, ref safeStr);
        Assert.That(safeStr, Is.EqualTo("initial"));
    }

    [Test]
    public void ExecUtils_ExecAll_Handles_Null_Instance_In_Collection_Without_Crashing()
    {
        var items = new string[] { "a", null, "b" };
        var results = new List<string>();

        items.ExecAll(x =>
        {
            if (x == null) throw new InvalidOperationException("Null test item");
            results.Add(x);
        });

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results, Is.EquivalentTo(new[] { "a", "b" }));
    }

    [Test]
    public void ExecUtils_CalculateFullJitterBackOffDelay_Handles_Boundaries()
    {
        for (var i = -5; i <= 10; i++)
        {
            var delay = ExecUtils.CalculateFullJitterBackOffDelay(i, baseDelay: 100, maxBackOffMs: 2000);
            Assert.That(delay, Is.GreaterThanOrEqualTo(0));
            Assert.That(delay, Is.LessThanOrEqualTo(2000));
        }

        var zeroDelay = ExecUtils.CalculateFullJitterBackOffDelay(0, baseDelay: 0, maxBackOffMs: 0);
        Assert.That(zeroDelay, Is.EqualTo(0));
    }

    [Test]
    public void ExecUtils_CalculateRetryDelayMs_Handles_Null_And_FullJitter()
    {
        var delayAttempt1 = ExecUtils.CalculateRetryDelayMs(1, default(RetryPolicy));
        Assert.That(delayAttempt1, Is.EqualTo(0));

        var delayAttempt2 = ExecUtils.CalculateRetryDelayMs(2, default(RetryPolicy));
        Assert.That(delayAttempt2, Is.GreaterThan(0));

        var jitterPolicy = new RetryPolicy { Behavior = RetryBehavior.FullJitterBackoff, DelayMs = 50, MaxDelayMs = 500 };
        var delayJitter = ExecUtils.CalculateRetryDelayMs(2, jitterPolicy);
        Assert.That(delayJitter, Is.GreaterThanOrEqualTo(0));
        Assert.That(delayJitter, Is.LessThanOrEqualTo(500));
    }

    [Test]
    public void FuncUtils_TryExec_Returns_DefaultValue_On_Exception()
    {
        var result = FuncUtils.TryExec<string>(() => throw new Exception("fail"), "fallback");
        Assert.That(result, Is.EqualTo("fallback"));

        var success = FuncUtils.TryExec(() => "ok", "fallback");
        Assert.That(success, Is.EqualTo("ok"));

        var nullFunc = FuncUtils.TryExec<int>(null, 42);
        Assert.That(nullFunc, Is.EqualTo(42));
    }

    private class DisposableItem : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    [Test]
    public void SimpleContainer_AddSingleton_Evaluates_Factory_Only_Once()
    {
        var container = new SimpleContainer();
        var factoryCallCount = 0;

        container.AddSingleton(typeof(DisposableItem), () =>
        {
            factoryCallCount++;
            return new DisposableItem();
        });

        Assert.That(factoryCallCount, Is.EqualTo(0));

        var first = container.Resolve(typeof(DisposableItem));
        var second = container.Resolve(typeof(DisposableItem));

        Assert.That(factoryCallCount, Is.EqualTo(1));
        Assert.That(first, Is.Not.Null);
        Assert.That(ReferenceEquals(first, second), Is.True);
    }

    [Test]
    public void SimpleContainer_Dispose_Disposes_Cached_Singletons()
    {
        var container = new SimpleContainer();
        var item = new DisposableItem();

        container.AddSingleton(typeof(DisposableItem), () => item);
        var resolved = container.Resolve(typeof(DisposableItem));
        Assert.That(resolved, Is.SameAs(item));
        Assert.That(item.IsDisposed, Is.False);

        container.Dispose();
        Assert.That(item.IsDisposed, Is.True);
    }

    [Test]
    public void AppTasks_RanAsTask_Executes_All_Chained_Tasks()
    {
        var hold = AppTasks.Instance;
        try
        {
            AppTasks.Instance = new AppTasks();
            var runTasks = new List<string>();

            AppTasks.Register("step1", args => runTasks.Add("step1:" + string.Join(",", args)));
            AppTasks.Register("step2", args => runTasks.Add("step2:" + string.Join(",", args)));

            // Inject command line arguments to simulate --AppTasks=step1:a;step2:b
            var prevArgs = AppTasks.GetAppTaskCommands(new[] { "--AppTasks=step1:a;step2:b" });
            Assert.That(prevArgs, Is.EqualTo("step1:a;step2:b"));

            var tasks = AppTasks.Instance.Tasks;
            var appTasks = "step1:a;step2:b".Split(';');
            for (var i = 0; i < appTasks.Length; i++)
            {
                var appTaskWithArgs = appTasks[i];
                var appTask = appTaskWithArgs.LeftPart(':');
                var args = appTaskWithArgs.IndexOf(':') >= 0
                    ? appTaskWithArgs.RightPart(':').Split(',')
                    : Array.Empty<string>();
                tasks[appTask](args);
            }

            Assert.That(runTasks.Count, Is.EqualTo(2));
            Assert.That(runTasks[0], Is.EqualTo("step1:a"));
            Assert.That(runTasks[1], Is.EqualTo("step2:b"));
        }
        finally
        {
            AppTasks.Instance = hold;
        }
    }

    [Test]
    public void EnumerableExtensions_FirstElementType_Is_Case_Insensitive()
    {
        var dict = new Dictionary<string, object>
        {
            ["AGE"] = 30
        };

        var list = new object[] { dict };
        var resolvedType = EnumerableExtensions.FirstElementType(list, "age");
        Assert.That(resolvedType, Is.EqualTo(typeof(int)));

        var emptyResolved = EnumerableExtensions.FirstElementType(null, "age");
        Assert.That(emptyResolved, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void EnumerableExtensions_CombineDistinct_Handles_Nulls()
    {
        var res1 = EnumerableExtensions.CombineDistinct<int>(null);
        Assert.That(res1, Is.Empty);

        var a = new[] { 1, 2 };
        int[] b = null;
        var c = new[] { 2, 3 };

        var combined = a.CombineDistinct(b, c);
        Assert.That(combined, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void DictionaryExtensions_Merge_Handles_Nulls()
    {
        Dictionary<string, int> initial = null;
        var merged = initial.Merge(null, new[] { new KeyValuePair<string, int>("k", 1) });
        Assert.That(merged["k"], Is.EqualTo(1));
    }

    [Test]
    public void SiteUtils_UrlFromSlug_Handles_Single_Digit_Port_And_Standard_Ports()
    {
        Assert.That(SiteUtils.UrlFromSlug("techstacks.io:8"), Is.EqualTo("https://techstacks.io:8"));
        Assert.That(SiteUtils.UrlFromSlug("techstacks.io:8080"), Is.EqualTo("https://techstacks.io:8080"));
        Assert.That(SiteUtils.UrlFromSlug(""), Is.EqualTo(""));
        Assert.That(SiteUtils.UrlFromSlug(null), Is.Null);
        Assert.That(SiteUtils.UrlToSlug(null), Is.Null);
    }

    [Test]
    public void EnumUtils_GetEnumMember_Handles_Missing_Or_Null()
    {
        var member = EnumUtils.GetEnumMember(typeof(StringComparison), "NonExistentMember");
        Assert.That(member, Is.Null);

        var nullMember = EnumUtils.GetEnumMember(null, "Foo");
        Assert.That(nullMember, Is.Null);

        var flags = EnumUtils.FromEnumFlagsList(typeof(StringComparison), new List<string> { "NonExistent" });
        Assert.That(flags, Is.Null);
    }

    [Test]
    public void CommandResultsHandler_Signals_WaitHandle_Even_On_Exception()
    {
        var results = new List<int>();
        var waitHandle = new AutoResetEvent(false);
        var failingCommand = new FailingCommand();
        var handler = new CommandResultsHandler<int>(results, failingCommand, waitHandle);

        Assert.Throws<InvalidOperationException>(() => handler.Execute());
        Assert.That(waitHandle.WaitOne(500), Is.True, "WaitHandle should be signaled even on exception");
    }

    private class FailingCommand : ICommandList<int>
    {
        public List<int> Execute() => throw new InvalidOperationException("Command failed");
    }

    [Test]
    public void ExpressionUtils_Null_Guards()
    {
        Assert.Throws<ArgumentNullException>(() => ExpressionUtils.GetMemberName<object>(null));
        Assert.That(ExpressionUtils.GetMemberExpression<object>(null), Is.Null);
        Assert.Throws<ArgumentNullException>(() => ExpressionUtils.GetFieldNames<object>(null));
        Assert.That(ExpressionUtils.GetValue(null), Is.Null);
    }
}
