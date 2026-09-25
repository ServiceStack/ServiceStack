#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;
using ServiceStack.Telemetry;

namespace ServiceStack.Extensions.Tests;

[Route("/otel/sales/{Id}")]
public class GetOtelSales : IReturn<GetOtelSalesResponse>
{
    public int? Id { get; set; }
}

public class GetOtelSalesResponse
{
    public bool HadOperationScope { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

public class OtelMqRequest : IReturnVoid
{
    public string? Name { get; set; }
}

public class OtelServices : Service
{
    // Untagged connections, so OrmLite events inherit the Profiling Tag from the request or message
    private void Query()
    {
        using var db = TryResolve<IDbConnectionFactory>().OpenDbConnection();
        db.SqlScalar<int>("SELECT 1");
    }

    public object Any(GetOtelSales request)
    {
        Query();
        return new GetOtelSalesResponse { HadOperationScope = Request!.Items.ContainsKey(Keywords.OperationScope) };
    }

    public void Any(OtelMqRequest request) => Query();
}

/// <summary>
/// End-to-end OpenTelemetry and Profiling behaviour through a real ASP.NET Core AppHost.
/// </summary>
[NonParallelizable]
public class OpenTelemetryAppHostTests
{
    private const string BaseUrl = "http://localhost:20021";
    private const string UserId = "42";
    private const string Tag = "tenant-a";

    private readonly ProfilingFeature profiling = new() {
        TagResolver = _ => Tag,
    };
    private HttpClient client = null!;
    private IMessageService mqServer = null!;

    class AppHost() : AppHostBase(nameof(OpenTelemetryAppHostTests), typeof(OtelServices).Assembly)
    {
        public override void Configure()
        {
            var mqServer = Resolve<IMessageService>();
            mqServer.RegisterHandler<OtelMqRequest>(ExecuteMessage);
            mqServer.Start();
        }
    }

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        var appHost = new AppHost();
        var contentRootPath = "~/../../../".MapServerPath();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        // The AppHost scans the whole assembly, which includes services from other fixtures
        // that depend on services this host doesn't register. They're never resolved here.
        builder.Host.UseDefaultServiceProvider(o => {
            o.ValidateOnBuild = false;
            o.ValidateScopes = false;
        });
        var services = builder.Services;
        services.AddLogging(o => o.ClearProviders());
        services.AddSingleton<IDbConnectionFactory>(
            new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));
        mqServer = new BackgroundMqService();
        services.AddSingleton(mqServer);
        services.AddPlugin(profiling);
        services.AddServiceStack(typeof(OtelServices).Assembly);

        var app = builder.Build();
        app.UseServiceStack(appHost);
        app.StartAsync(BaseUrl).Wait();

        client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        client.Dispose();
        AppHostBase.DisposeApp();
    }

    private sealed class Telemetry : IDisposable
    {
        public readonly ConcurrentBag<Activity> Spans = new();
        public readonly ConcurrentBag<(string Name, Dictionary<string, object?> Tags)> Measurements = new();
        private readonly ActivityListener? activityListener;
        private readonly MeterListener meterListener = new();

        public Telemetry(bool traces = true)
        {
            if (traces)
            {
                activityListener = new ActivityListener {
                    ShouldListenTo = source => source.Name == OperationDiagnostics.Name,
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                    ActivityStopped = Spans.Add,
                };
                ActivitySource.AddActivityListener(activityListener);
            }
            meterListener.InstrumentPublished = (instrument, listener) => {
                if (instrument.Meter.Name == OperationDiagnostics.Name)
                    listener.EnableMeasurementEvents(instrument);
            };
            meterListener.SetMeasurementEventCallback<long>((instrument, _, tags, _) => Record(instrument, tags));
            meterListener.SetMeasurementEventCallback<double>((instrument, _, tags, _) => Record(instrument, tags));
            meterListener.Start();
        }

        private void Record(Instrument instrument, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var map = new Dictionary<string, object?>();
            foreach (var tag in tags)
                map[tag.Key] = tag.Value;
            Measurements.Add((instrument.Name, map));
        }

        public void Dispose()
        {
            activityListener?.Dispose();
            meterListener.Dispose();
        }
    }

    private async Task<HttpResponseMessage> GetAsync(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", $"{HttpHeaders.XUserAuthId}={UserId}");
        return await client.SendAsync(request);
    }

    private List<DiagnosticEntry> GetEntries() =>
        ((ProfilerDiagnosticObserver)typeof(ProfilingFeature)
            .GetProperty("Observer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(profiling)!).GetLatestEntries(null);

    private long LastEntryId() => GetEntries().Select(x => x.Id).DefaultIfEmpty(0).Max();

    private List<DiagnosticEntry> EntriesAfter(long id) => GetEntries().Where(x => x.Id > id).ToList();

    [Test]
    public async Task Unknown_api_names_record_no_operation_metrics_or_spans()
    {
        using var telemetry = new Telemetry();

        var response = await GetAsync("/api/NotARealOperation");

        Assert.That(response.IsSuccessStatusCode, Is.False);
        Assert.That(telemetry.Measurements.Where(x => x.Name.StartsWith("servicestack.operation")), Is.Empty);
        Assert.That(telemetry.Spans.Where(x => x.Source.Name == OperationDiagnostics.Name), Is.Empty);
    }

    [Test]
    public async Task Api_format_extensions_are_stripped_from_the_operation_name()
    {
        using var telemetry = new Telemetry();

        var response = await GetAsync("/api/GetOtelSales.json");

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var operations = telemetry.Measurements
            .Where(x => x.Name == OperationDiagnostics.DurationName)
            .Select(x => x.Tags["servicestack.operation"])
            .ToList();
        Assert.That(operations, Is.EqualTo(new[] { "GetOtelSales" }));
    }

    [Test]
    public async Task RestHandler_operation_span_has_its_route()
    {
        using var telemetry = new Telemetry();

        var response = await GetAsync("/otel/sales/1");

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var span = telemetry.Spans.Single(x => x.DisplayName == "ServiceStack GetOtelSales");
        Assert.That(span.GetTagItem("http.route"), Is.EqualTo("/otel/sales/{Id}"));
        var duration = telemetry.Measurements.Single(x => x.Name == OperationDiagnostics.DurationName);
        Assert.That(duration.Tags["http.route"], Is.EqualTo("/otel/sales/{Id}"));
        Assert.That(duration.Tags["servicestack.outcome"], Is.EqualTo("success"));
    }

    [Test]
    public async Task GenericHandler_operation_span_has_its_route_and_detailed_phase_spans()
    {
        using var telemetry = new Telemetry();
        OperationDiagnostics.EnableDetailedSpans = true;
        try
        {
            var response = await GetAsync("/api/GetOtelSales");
            Assert.That(response.IsSuccessStatusCode, Is.True);
        }
        finally
        {
            OperationDiagnostics.EnableDetailedSpans = false;
        }

        var span = telemetry.Spans.Single(x => x.DisplayName == "ServiceStack GetOtelSales");
        Assert.That(span.GetTagItem("http.route"), Is.EqualTo("/api/{Request}"));
        var phases = telemetry.Spans.Where(x => x.ParentSpanId == span.SpanId).Select(x => x.DisplayName).ToList();
        Assert.That(phases, Is.EquivalentTo(new[] {
            OperationDiagnostics.PreRequestFiltersPhase,
            OperationDiagnostics.RequestFiltersPhase,
            OperationDiagnostics.ServiceInvocationPhase,
        }));
    }

    [Test]
    public async Task No_operation_scope_is_stored_when_nothing_is_listening()
    {
        Assert.That(OperationDiagnostics.IsEnabled, Is.False);

        var response = await client.GetStringAsync("/api/GetOtelSales");

        Assert.That(response.FromJson<GetOtelSalesResponse>().HadOperationScope, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OrmLite_events_have_the_requests_UserId_and_Tag(bool withListener)
    {
        using var telemetry = withListener ? new Telemetry() : null;
        var lastId = LastEntryId();

        var response = await GetAsync("/api/GetOtelSales");

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var entries = EntriesAfter(lastId);
        var request = entries.First(x => x.EventType == Diagnostics.Events.ServiceStack.WriteRequestBefore);
        var ormLite = entries.Where(x => x.Source == "OrmLite").ToList();
        Assert.That(ormLite, Is.Not.Empty);
        Assert.That(ormLite.All(x => x.Tag == Tag), Is.True);
        Assert.That(ormLite.All(x => x.UserAuthId == UserId), Is.True);
        Assert.That(ormLite.All(x => x.TraceId == request.TraceId), Is.True);
    }

    [Test]
    public async Task Exported_operation_span_has_no_UserId_or_Tag()
    {
        using var telemetry = new Telemetry();

        await GetAsync("/api/GetOtelSales");

        var span = telemetry.Spans.Single(x => x.DisplayName == "ServiceStack GetOtelSales");
        var keys = span.TagObjects.Select(x => x.Key).ToList();
        Assert.That(keys, Does.Not.Contain(Diagnostics.Activity.UserId));
        Assert.That(keys, Does.Not.Contain(Diagnostics.Activity.Tag));
        Assert.That(span.TagObjects.Select(x => x.Value), Does.Not.Contain(UserId));
        Assert.That(span.TagObjects.Select(x => x.Value), Does.Not.Contain(Tag));
    }

    [Test]
    public async Task Mq_events_share_a_TraceId_without_OpenTelemetry()
    {
        Assert.That(MessagingDiagnostics.ActivitySource.HasListeners(), Is.False);
        var lastId = LastEntryId();
        const string traceId = "mq-trace-id";

        using (var mqClient = mqServer.CreateMessageQueueClient())
        {
            mqClient.Publish(new Message<OtelMqRequest>(new OtelMqRequest { Name = "test" }) {
                TraceId = traceId,
                Tag = Tag,
            });
        }

        List<DiagnosticEntry> entries = [];
        var timeout = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < timeout)
        {
            entries = EntriesAfter(lastId);
            if (entries.Any(x => x.EventType == Diagnostics.Events.ServiceStack.WriteMqRequestAfter))
                break;
            await Task.Delay(20);
        }

        var mqEntries = entries.Where(x => x.EventType is Diagnostics.Events.ServiceStack.WriteMqRequestBefore or Diagnostics.Events.ServiceStack.WriteMqRequestAfter).ToList();
        var ormLite = entries.Where(x => x.Source == "OrmLite").ToList();
        Assert.That(mqEntries, Is.Not.Empty);
        Assert.That(ormLite, Is.Not.Empty);
        Assert.That(mqEntries.All(x => x.TraceId == traceId), Is.True);
        Assert.That(ormLite.All(x => x.TraceId == traceId), Is.True);
        Assert.That(ormLite.All(x => x.Tag == Tag), Is.True);
    }
}
