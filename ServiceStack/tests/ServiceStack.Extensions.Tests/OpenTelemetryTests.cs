#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.Telemetry;

namespace ServiceStack.Extensions.Tests;

[NonParallelizable]
public class OpenTelemetryTests
{
    [Test]
    public void Profiling_uses_the_same_TraceId_field_with_and_without_telemetry()
    {
        var observer = new ProfilerDiagnosticObserver(new ProfilingFeature());
        var diagnosticEvent = new RequestDiagnosticEvent { TraceId = "legacy-request-id" };

        using var platform = new Activity("platform request").Start();
        Assert.That(observer.CreateDiagnosticEntry(diagnosticEvent).TraceId, Is.EqualTo("legacy-request-id"));

        using var listener = new ActivityListener {
            ShouldListenTo = source => source.Name == OperationDiagnostics.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        using var operation = OperationDiagnostics.Start(new BasicRequest { Verb = "GET" }, "GetOrders", "/orders");
        Assert.That(observer.CreateDiagnosticEntry(diagnosticEvent).TraceId,
            Is.EqualTo(Activity.Current!.TraceId.ToString()));
    }

    [Test]
    public void Client_errors_cancellation_and_remote_parent_have_distinct_outcomes()
    {
        var spans = new ConcurrentBag<Activity>();
        var errors = new ConcurrentBag<long>();
        using var activityListener = new ActivityListener {
            ShouldListenTo = source => source.Name == OperationDiagnostics.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Add,
        };
        ActivitySource.AddActivityListener(activityListener);
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) => {
            if (instrument.Meter.Name == OperationDiagnostics.Name)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, _, _) => {
            if (instrument.Name == OperationDiagnostics.ErrorsName)
                errors.Add(value);
        });
        meterListener.Start();

        var request = new BasicRequest {
            Verb = "POST",
            Headers = new NameValueCollection {
                ["traceparent"] = "00-44556677889900aabbccddeeff112233-0123456789abcdef-01",
            },
        };
        using (var validation = OperationDiagnostics.Start(request, "SaveOrder", "/orders"))
        {
            Assert.That(Activity.Current?.Kind, Is.EqualTo(ActivityKind.Server));
            Assert.That(Activity.Current?.ParentSpanId.ToString(), Is.EqualTo("0123456789abcdef"));
            validation.Complete(400);
        }
        using (var cancelled = OperationDiagnostics.Start(request, "SaveOrder", "/orders"))
            cancelled.Complete(499, new OperationCanceledException());

        Assert.That(spans, Has.Count.EqualTo(2));
        Assert.That(spans.All(x => x.TraceId.ToString() == "44556677889900aabbccddeeff112233"), Is.True);
        Assert.That(spans.All(x => x.Status != ActivityStatusCode.Error), Is.True);
        Assert.That(spans.Any(x => (string?)x.GetTagItem("servicestack.outcome") == "client_error"), Is.True);
        Assert.That(spans.Any(x => (string?)x.GetTagItem("servicestack.outcome") == "cancelled"), Is.True);
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Operation_span_and_metrics_record_success_and_error_once()
    {
        var spans = new ConcurrentBag<Activity>();
        var counts = new ConcurrentBag<(string Name, long Value)>();
        var durations = new ConcurrentBag<double>();
        using var activityListener = new ActivityListener {
            ShouldListenTo = source => source.Name is OperationDiagnostics.Name or "TelemetryTests.Parent",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Add,
        };
        ActivitySource.AddActivityListener(activityListener);
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) => {
            if (instrument.Meter.Name == OperationDiagnostics.Name)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            counts.Add((instrument.Name, value)));
        meterListener.SetMeasurementEventCallback<double>((instrument, value, _, _) => {
            if (instrument.Name == OperationDiagnostics.DurationName)
                durations.Add(value);
        });
        meterListener.Start();

        using var source = new ActivitySource("TelemetryTests.Parent");
        using var parent = source.StartActivity("HTTP request", ActivityKind.Server);
        Assert.That(parent, Is.Not.Null);
        var request = new BasicRequest { Verb = "GET" };

        var success = OperationDiagnostics.Start(request, "GetOrders", "/orders/{Id}");
        Assert.That(Activity.Current?.ParentSpanId, Is.EqualTo(parent!.SpanId));
        success.Complete(200);
        success.Complete(500, new Exception("must not record twice"));

        var failure = OperationDiagnostics.Start(request, "GetOrders", "/orders/{Id}");
        failure.Complete(500, new InvalidOperationException("failure"));

        var operations = spans.Where(x => x.Source.Name == OperationDiagnostics.Name).ToList();
        Assert.That(operations, Has.Count.EqualTo(2));
        Assert.That(operations.All(x => x.Kind == ActivityKind.Internal && x.TraceId == parent.TraceId), Is.True);
        Assert.That(operations.Count(x => x.Status == ActivityStatusCode.Error), Is.EqualTo(1));
        Assert.That(counts.Count(x => x.Name == OperationDiagnostics.ActiveName && x.Value == 1), Is.EqualTo(2));
        Assert.That(counts.Count(x => x.Name == OperationDiagnostics.ActiveName && x.Value == -1), Is.EqualTo(2));
        Assert.That(counts.Count(x => x.Name == OperationDiagnostics.ErrorsName && x.Value == 1), Is.EqualTo(1));
        Assert.That(durations, Has.Count.EqualTo(2));
        Assert.That(durations.All(x => x >= 0), Is.True);
    }

    [Test]
    public void Message_context_survives_serialization_and_malformed_context_is_ignored()
    {
        using var listener = new ActivityListener {
            ShouldListenTo = source => source.Name is MessagingDiagnostics.Name or "TelemetryTests.Parent",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource("TelemetryTests.Parent");
        using var request = source.StartActivity("request", ActivityKind.Server);
        Assert.That(request, Is.Not.Null);
        var message = new Message<string>("hello") {
            Meta = new Dictionary<string, string> { ["custom"] = "preserved" },
        };
        ActivitySpanId publishSpanId;
        using (var publish = MessagingDiagnostics.StartPublish(MessagingDiagnostics.Systems.Redis, "mq:Foo.inq", message))
        {
            Assert.That(publish.Activity, Is.Not.Null);
            publishSpanId = publish.Activity!.SpanId;
            Assert.That(publish.Activity.DisplayName, Is.EqualTo("send mq:Foo.inq"));
            Assert.That(publish.Activity.GetTagItem("messaging.system"), Is.EqualTo("redis"));
            Assert.That(publish.Activity.GetTagItem("messaging.operation.type"), Is.EqualTo("send"));
            Assert.That(publish.Activity.GetTagItem("messaging.destination.name"), Is.EqualTo("mq:Foo.inq"));
            Assert.That(publish.Activity.GetTagItem("messaging.message.id"), Is.EqualTo(message.Id.ToString()));
            MessagingDiagnostics.Inject(message);
        }

        var bytes = MessageSerializer.Instance.ToBytes(message);
        var received = MessageSerializer.Instance.ToMessage<string>(bytes);
        Assert.That(received.Meta!["custom"], Is.EqualTo("preserved"));
        Assert.That(received.Meta[MessagingDiagnostics.TraceParentKey], Is.EqualTo(message.Meta![MessagingDiagnostics.TraceParentKey]));

        Activity.Current = null; // Simulate a different worker/process with no ambient request.
        try
        {
            using (var scope = MessagingDiagnostics.StartProcess(received, MessagingDiagnostics.Systems.Redis, "mq:Foo.inq"))
            {
                var consumer = scope.Activity;
                Assert.That(consumer, Is.Not.Null);
                Assert.That(consumer!.DisplayName, Is.EqualTo("process mq:Foo.inq"));
                Assert.That(consumer.Kind, Is.EqualTo(ActivityKind.Consumer));
                Assert.That(consumer.TraceId, Is.EqualTo(request!.TraceId));
                Assert.That(consumer.ParentSpanId, Is.EqualTo(publishSpanId));
                Assert.That(consumer.HasRemoteParent, Is.True);
                Assert.That(consumer.GetTagItem("messaging.operation.type"), Is.EqualTo("process"));
                Assert.That(consumer.GetTagItem("messaging.message.id"), Is.EqualTo(message.Id.ToString()));
            }

            received.Meta[MessagingDiagnostics.TraceParentKey] = "invalid";
            using var malformed = MessagingDiagnostics.StartProcess(received, MessagingDiagnostics.Systems.Redis, "mq:Foo.inq");
            Assert.That(malformed.Activity, Is.Not.Null);
            Assert.That(malformed.Activity!.TraceId, Is.Not.EqualTo(request!.TraceId));
        }
        finally
        {
            Activity.Current = request;
        }
    }

    [Test]
    public void Messaging_metrics_record_sent_consumed_and_process_duration()
    {
        var measurements = new ConcurrentBag<(string Name, Dictionary<string, object?> Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) => {
            if (instrument.Meter.Name == MessagingDiagnostics.Name)
                listener.EnableMeasurementEvents(instrument);
        };
        void Record(Instrument instrument, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var map = new Dictionary<string, object?>();
            foreach (var tag in tags)
                map[tag.Key] = tag.Value;
            measurements.Add((instrument.Name, map));
        }
        meterListener.SetMeasurementEventCallback<long>((instrument, _, tags, _) => Record(instrument, tags));
        meterListener.SetMeasurementEventCallback<double>((instrument, _, tags, _) => Record(instrument, tags));
        meterListener.Start();

        var message = new Message<string>("hello");
        using (MessagingDiagnostics.StartPublish(MessagingDiagnostics.Systems.Background, "mq:Foo.inq", message)) { }
        using (var failed = MessagingDiagnostics.StartProcess(message, MessagingDiagnostics.Systems.Background, "mq:Foo.inq"))
            failed.RecordError(new InvalidOperationException());

        var sent = measurements.Single(x => x.Name == MessagingDiagnostics.SentMessagesName);
        Assert.That(sent.Tags["messaging.system"], Is.EqualTo("servicestack.background"));
        Assert.That(sent.Tags["messaging.destination.name"], Is.EqualTo("mq:Foo.inq"));
        Assert.That(sent.Tags.ContainsKey("error.type"), Is.False);
        Assert.That(measurements.Count(x => x.Name == MessagingDiagnostics.ConsumedMessagesName), Is.EqualTo(1));
        var duration = measurements.Single(x => x.Name == MessagingDiagnostics.ProcessDurationName);
        Assert.That(duration.Tags["error.type"], Is.EqualTo(typeof(InvalidOperationException).FullName));
    }

    [Test]
    public void Operation_scope_is_a_shared_noop_when_nothing_is_listening()
    {
        Assert.That(OperationDiagnostics.IsEnabled, Is.False);
        var scope = OperationDiagnostics.Start(new BasicRequest { Verb = "GET" }, "GetOrders");
        Assert.That(scope, Is.SameAs(OperationDiagnostics.OperationScope.Noop));
        Assert.That(MessagingDiagnostics.StartPublish("redis", "mq:Foo.inq"), Is.SameAs(MessagingDiagnostics.MessagingScope.Noop));
    }

    [Test]
    public void Disposing_an_operation_scope_records_an_unknown_outcome()
    {
        var spans = new ConcurrentBag<Activity>();
        using var listener = new ActivityListener {
            ShouldListenTo = source => source.Name == OperationDiagnostics.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Add,
        };
        ActivitySource.AddActivityListener(listener);

        var scope = OperationDiagnostics.Start(new BasicRequest { Verb = "GET" }, "GetOrders");
        scope.Dispose();
        scope.Complete(500, new Exception("must not record after dispose"));

        var span = spans.Single();
        Assert.That(span.GetTagItem("servicestack.outcome"), Is.EqualTo("unknown"));
        Assert.That(span.Status, Is.EqualTo(ActivityStatusCode.Unset));
    }

    [Test]
    public void Root_W3C_activity_has_no_ParentSpanId()
    {
        using var listener = new ActivityListener {
            ShouldListenTo = source => source.Name == OperationDiagnostics.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        var observer = new ProfilerDiagnosticObserver(new ProfilingFeature());
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            using var root = OperationDiagnostics.ActivitySource.StartActivity("root");
            Assert.That(root!.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
            var entry = observer.CreateDiagnosticEntry(new RequestDiagnosticEvent { TraceId = "legacy-request-id" });
            Assert.That(entry.TraceId, Is.EqualTo(root.TraceId.ToString()));
            Assert.That(entry.SpanId, Is.EqualTo(root.SpanId.ToString()));
            Assert.That(entry.ParentSpanId, Is.Null);
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    [Test]
    public void Hierarchical_activity_keeps_the_legacy_TraceId()
    {
        using var listener = new ActivityListener {
            ShouldListenTo = source => source.Name == OperationDiagnostics.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        var observer = new ProfilerDiagnosticObserver(new ProfilingFeature());
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            using var hierarchical = new Activity("jobs poll").SetParentId(Guid.NewGuid().ToString()).Start();
            Assert.That(hierarchical.IdFormat, Is.EqualTo(ActivityIdFormat.Hierarchical));
            var entry = observer.CreateDiagnosticEntry(new RequestDiagnosticEvent { TraceId = "legacy-request-id" });
            Assert.That(entry.TraceId, Is.EqualTo("legacy-request-id"));
            Assert.That(entry.SpanId, Is.Null);
            Assert.That(entry.ParentSpanId, Is.Null);
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    [TestCase("https://jaeger.example.org/trace/{traceId}", "https://jaeger.example.org/trace/0123456789abcdef0123456789abcdef")]
    [TestCase("http://localhost:16686/trace/{traceId}", "http://localhost:16686/trace/0123456789abcdef0123456789abcdef")]
    [TestCase("http://127.0.0.1:18888/traces/detail/{traceId}", "http://127.0.0.1:18888/traces/detail/0123456789abcdef0123456789abcdef")]
    [TestCase("http://jaeger.example.org/trace/{traceId}", null)]
    [TestCase("https://user:pass@jaeger.example.org/trace/{traceId}", null)]
    [TestCase("https://jaeger.example.org/trace/", null)]
    [TestCase("/trace/{traceId}", null)]
    public void External_trace_url_allows_https_or_http_localhost(string template, string? expected)
    {
        Assert.That(ProfilingFeature.GetExternalTraceUrl(template, "0123456789abcdef0123456789abcdef"), Is.EqualTo(expected));
        Assert.That(ProfilingFeature.GetExternalTraceUrl(template, "not-a-trace-id"), Is.Null);
    }
}
