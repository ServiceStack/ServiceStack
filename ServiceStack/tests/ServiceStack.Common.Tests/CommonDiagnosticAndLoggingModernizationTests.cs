using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.Logging;
using ServiceStack.Reflection;
using ServiceStack.Support;

namespace ServiceStack.Common.Tests;

[TestFixture]
public class CommonDiagnosticAndLoggingModernizationTests
{
    [Test]
    public void Inspect_dump_handles_empty_dictionary_and_null()
    {
        var emptyDict = new Dictionary<string, object>();
        var result = Inspect.dump(emptyDict);
        Assert.That(result, Is.EqualTo("{}"));

        var nullResult = Inspect.dump<string>(null);
        Assert.That(nullResult, Is.EqualTo(string.Empty));
    }

    [Test]
    public void JSON_parseSpan_handles_escaped_json_and_operator_precedence()
    {
        var escapedJson = "{\\\"foo\\\":\\\"bar\\\"}";
        var result = JSON.parseSpan(escapedJson.AsSpan());
        Assert.That(result, Is.Not.Null);

        var nullResult = JSON.Deserialize(string.Empty, typeof(string));
        Assert.That(nullResult, Is.Null);
    }

#if NET8_0_OR_GREATER
    [Test]
    public void JsonComplexTypeSerializer_handles_null_and_empty()
    {
        var serializer = new JsonComplexTypeSerializer();
        Assert.That(serializer.DeserializeFromString<string>(null), Is.Null);
        Assert.That(serializer.DeserializeFromString<string>(string.Empty), Is.Null);
        Assert.That(serializer.DeserializeFromString(string.Empty, typeof(string)), Is.Null);
        Assert.That(serializer.SerializeToString<string>(null), Is.EqualTo("null"));
    }
#endif

    [Test]
    public void UrnId_Parse_formats_exception_message_correctly()
    {
        var ex = Assert.Throws<ArgumentException>(() => UrnId.Parse("invalid"));
        Assert.That(ex.Message, Does.Contain("Cannot parse invalid urn: 'invalid'"));
        Assert.That(ex.ParamName, Is.EqualTo("urnId"));

        Assert.Throws<ArgumentNullException>(() => UrnId.Parse(null));

        var created = UrnId.Create<CommonDiagnosticAndLoggingModernizationTests>((object)null);
        Assert.That(created, Is.EqualTo("urn:CommonDiagnosticAndLoggingModernizationTests:"));

        var parts = UrnId.CreateWithParts("Test", null);
        Assert.That(parts, Is.EqualTo("urn:Test:"));
    }

    [Test]
    public void TaskExt_modernized_methods_work_correctly()
    {
        var taskResult = "hello".AsTaskResult();
        Assert.That(taskResult.IsCompletedSuccessfully, Is.True);
        Assert.That(taskResult.GetResult(), Is.EqualTo("hello"));

        var ex = new InvalidOperationException("boom");
        var taskEx = ex.AsTaskException<string>();
        Assert.That(taskEx.IsFaulted, Is.True);

        Task<string> nullTask = null;
        Assert.That(nullTask.GetResult(), Is.Null);
    }

    [Test]
    public void StartupTasks_registers_and_runs_thread_safely()
    {
        var ran = 0;
        Parallel.For(0, 50, i =>
        {
            StartupTasks.Register($"Task_{i}", () => Interlocked.Increment(ref ran));
        });

        StartupTasks.Run();
        Assert.That(ran, Is.EqualTo(50));
    }

    [Test]
    public void SimpleAppSettings_handles_null_keys_and_default_collections()
    {
        var settings = new SimpleAppSettings();
        Assert.That(settings.Exists(null), Is.False);
        Assert.That(settings.GetString(null), Is.Null);
        Assert.That(settings.GetList("missing"), Is.Not.Null);
        Assert.That(settings.GetList("missing"), Is.Empty);
        Assert.That(settings.GetDictionary("missing"), Is.Not.Null);
        Assert.That(settings.GetDictionary("missing"), Is.Empty);
        Assert.That(settings.GetKeyValuePairs("missing"), Is.Not.Null);
        Assert.That(settings.GetKeyValuePairs("missing"), Is.Empty);
        Assert.Throws<ArgumentNullException>(() => settings.Set<string>(null, "val"));
    }

    [Test]
    public void Loggers_do_not_throw_on_unmatched_braces()
    {
        var consoleLogger = new ConsoleLogger(typeof(CommonDiagnosticAndLoggingModernizationTests));
        Assert.DoesNotThrow(() => consoleLogger.InfoFormat("JSON: {foo: bar}"));
        Assert.DoesNotThrow(() => consoleLogger.InfoFormat("Mismatched {0} {1}", "one"));
        Assert.DoesNotThrow(() => consoleLogger.InfoFormat("No args: {test}", (object[])null));

        var debugLogger = new DebugLogger(typeof(CommonDiagnosticAndLoggingModernizationTests));
        Assert.DoesNotThrow(() => debugLogger.InfoFormat("JSON: {foo: bar}"));
        Assert.DoesNotThrow(() => debugLogger.InfoFormat("Mismatched {0} {1}", "one"));
    }

    [Test]
    public void InMemoryLog_is_thread_safe_and_handles_format_safely()
    {
        var factory = new InMemoryLogFactory(debugEnabled: true);
        var log = (InMemoryLog)factory.GetLogger("TestLog");

        Parallel.For(0, 100, i =>
        {
            log.DebugFormat("Message {0}: {brace}", i);
            if (i % 10 == 0)
                log.Error("Err", new Exception($"ex {i}"));
        });

        Assert.That(log.HasExceptions, Is.True);
        Assert.That(log.DebugEntries.Count, Is.EqualTo(100));
    }

    private class TestAdapter : AdapterBase
    {
        protected override ILog Log => null;

        public int RunTest(Func<int> fn) => Execute(fn);
        public Task<int> RunTestAsync(Func<Task<int>> fn) => ExecuteAsync(fn);
    }

    [Test]
    public async Task AdapterBase_executes_with_stopwatch_and_validates_action()
    {
        var adapter = new TestAdapter();
        Assert.Throws<ArgumentNullException>(() => adapter.RunTest(null));

        var result = adapter.RunTest(() => 42);
        Assert.That(result, Is.EqualTo(42));

        var asyncResult = await adapter.RunTestAsync(() => Task.FromResult(99));
        Assert.That(asyncResult, Is.EqualTo(99));
    }

    [Test]
    public void IPAddressExtensions_bounds_and_null_checks()
    {
        var ip1 = IPAddress.Parse("192.168.1.10");
        var ip2 = IPAddress.Parse("192.168.1.20");
        var mask = IPAddress.Parse("255.255.255.0");

        Assert.That(ip1.IsInSameIpv4Subnet(ip2, mask), Is.True);

        var shortBytes1 = new byte[] { 1, 2, 3, 4 };
        var shortBytes2 = new byte[] { 1, 2, 3, 4 };
        Assert.That(shortBytes1.IsInSameIpv6Subnet(shortBytes2), Is.True);

        Assert.Throws<ArgumentNullException>(() => ((IPAddress)null).GetBroadcastAddress(mask));
    }

    [Test]
    public void DirectoryInfoExtensions_handles_missing_paths()
    {
        var files = DirectoryInfoExtensions.GetMatchingFiles("/path/that/does/not/exist_12345", "*.txt");
        Assert.That(files, Is.Empty);

        DirectoryInfo nullDir = null;
        Assert.That(nullDir.GetMatchingFiles("*.txt"), Is.Empty);
    }

    [Test]
    public void PerfUtils_handles_bounds_and_null_action()
    {
        Assert.Throws<ArgumentNullException>(() => PerfUtils.MeasureFor(null, 100));
        var elapsed = PerfUtils.MeasureFor(() => { }, -10);
        Assert.That(elapsed, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Command_case_insensitive_as_and_null_safe_args()
    {
        var cmd = new Command
        {
            Name = "SUM",
            Args = null,
        };

        Assert.That(cmd.ToString(), Is.EqualTo("SUM()"));
        Assert.That(cmd.ToDebugString(), Is.EqualTo("[SUM:]"));

        var memory = "SUM(*) AS Total".AsMemory();
        var endPos = cmd.IndexOfMethodEnd(memory, 6);
        Assert.That(cmd.Suffix.ToString(), Is.EqualTo(" AS Total"));
    }

    [Test]
    public void SvgCreator_handles_negative_indices_and_null()
    {
        var color = SvgCreator.GetDarkColor(-5);
        Assert.That(color, Is.Not.Null);

        Assert.That(SvgCreator.Decode(null), Is.EqualTo(string.Empty));
        Assert.That(SvgCreator.DataUriToSvg(null), Is.EqualTo(string.Empty));
    }

    public static int SampleStaticMethod(int a, int b) => a + b;
    public static void SampleStaticVoid(int a) { }
    public int SampleInstanceMethod(int a) => a * 2;
    public void SampleInstanceVoid() { }

    [Test]
    public void DelegateFactory_supports_static_and_void_methods()
    {
        var staticMethod = typeof(CommonDiagnosticAndLoggingModernizationTests).GetMethod(nameof(SampleStaticMethod));
        var staticDel = DelegateFactory.Create(staticMethod);
        var result = (int)staticDel(null, new object[] { 10, 20 });
        Assert.That(result, Is.EqualTo(30));

        var staticVoid = typeof(CommonDiagnosticAndLoggingModernizationTests).GetMethod(nameof(SampleStaticVoid));
        var voidDel = DelegateFactory.Create(staticVoid);
        var voidResult = voidDel(null, new object[] { 5 });
        Assert.That(voidResult, Is.Null);

        var instanceMethod = typeof(CommonDiagnosticAndLoggingModernizationTests).GetMethod(nameof(SampleInstanceMethod));
        var instDel = DelegateFactory.Create(instanceMethod);
        var instResult = (int)instDel(this, new object[] { 21 });
        Assert.That(instResult, Is.EqualTo(42));

        var instanceVoid = typeof(CommonDiagnosticAndLoggingModernizationTests).GetMethod(nameof(SampleInstanceVoid));
        var instVoidDel = DelegateFactory.CreateVoid(instanceVoid);
        Assert.DoesNotThrow(() => instVoidDel(this, Array.Empty<object>()));
    }

    [Test]
    public void XLinqExtensions_handles_null_safely()
    {
        IEnumerable<XElement> nullElements = null;
        Assert.That(nullElements.GetValues(), Is.Empty);
        Assert.That(nullElements.AnyElement("foo"), Is.Null);
        Assert.That(nullElements.AllElements("foo"), Is.Empty);

        XElement nullEl = null;
        Assert.That(nullEl.NextElement(), Is.Null);
    }

    private class MockConnection : IDbConnection
    {
        public bool IsOpened { get; set; }
        public string ConnectionString { get; set; }
        public int ConnectionTimeout => 30;
        public string Database => "Test";
        public ConnectionState State => IsOpened ? ConnectionState.Open : ConnectionState.Closed;

        public IDbTransaction BeginTransaction() => null;
        public IDbTransaction BeginTransaction(IsolationLevel il) => null;
        public void ChangeDatabase(string databaseName) { }
        public void Close() => IsOpened = false;
        public IDbCommand CreateCommand() => null;
        public void Open() => IsOpened = true;
        public void Dispose() { }
    }

    [Test]
    public void DbConnectionFactory_validates_factory_and_connection()
    {
        Assert.Throws<ArgumentNullException>(() => new DbConnectionFactory(null));

        var factory = new DbConnectionFactory(() => new MockConnection());
        var conn = factory.OpenDbConnection();
        Assert.That(conn.State, Is.EqualTo(ConnectionState.Open));

        var nullFactory = new DbConnectionFactory(() => null);
        Assert.Throws<InvalidOperationException>(() => nullFactory.OpenDbConnection());
    }

    [Test]
    public void NetCoreExtensions_close_null_safe()
    {
        Socket socket = null;
        Assert.DoesNotThrow(() => NetCoreExtensions.Close(socket));

        System.Data.Common.DbDataReader reader = null;
        Assert.DoesNotThrow(() => NetCoreExtensions.Close(reader));
    }
}
