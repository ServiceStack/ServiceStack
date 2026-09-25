# End-to-end OpenTelemetry instrumentation

## Goal

Make a ServiceStack API request traceable through service execution, outbound HTTP calls, messages, and background jobs, with useful low-cardinality metrics. Applications should be able to export traces and metrics through their chosen OpenTelemetry SDK/exporter. The existing `ProfilingFeature` and Admin UI must remain useful and gain a trace-oriented view of the new data.

Implement the framework instrumentation primarily in `ServiceStack` using .NET `ActivitySource` and `Meter`. Keep exporters, sampling policy, resource identity, and collector configuration in the host application. Do not make the core package require an OpenTelemetry SDK or exporter. Target the modern .NET hosts first (`net8.0` and `net10.0`); preserve the existing `DiagnosticListener` and MiniProfiler behavior on older targets. New work that belongs to jobs, clients, messaging providers, or the UI is called out below.

## What exists already

| Area | Current implementation | Gap to close |
| --- | --- | --- |
| API requests | `ServiceStackHost.InitRequest()` creates a legacy `Activity` only when `ProfilingFeature` is active; `EndRequest()` emits `DiagnosticListener` completion/error events. ASP.NET Core creates its own HTTP server span. | A ServiceStack `ActivitySource` operation span independent of Profiling, accurate completion/status, and operation metrics without a duplicate HTTP server span. |
| Profiling | `ProfilingFeature` subscribes to ServiceStack, client, OrmLite, Redis, and jobs diagnostic listeners and stores bounded `DiagnosticEntry` rows. `AdminProfilingService` filters them. | W3C trace/span identity and a trace view while preserving existing event rows, access checks, and limits. |
| Jobs | `JobsDiagnostics` already exposes `ServiceStack.Jobs` `ActivitySource` and `Meter`, and captures `Activity.Current?.Id` in `BackgroundJob.TraceId` on enqueue. | Verify continuation across queues/processes, document setup, and avoid double recording jobs in the Admin UI. |
| Messaging | `IMessage.TraceId` and legacy diagnostic events provide correlation; message producers and handlers do not yet provide a consistent W3C producer/consumer span model. | Propagate context through all transports and trace publish/process/retry/dead-letter paths. |
| Clients and data | `JsonApiClient` and `JsonHttpClient` have `DiagnosticListener` events; OrmLite and Redis have their own events. Modern `HttpClient` also supplies HTTP client spans. | Preserve DTO-level correlation without duplicating `System.Net.Http` spans; add safe optional data spans only where not already instrumented. |

Existing identifiers have different formats: `IRequest.GetTraceId()` can generate a GUID; `DiagnosticsUtils.GetTraceId(Activity)` returns the root activity's `ParentId`; `BackgroundJob.TraceId` stores `Activity.Current?.Id` (a traceparent-style value). Profiling continues to expose one string `TraceId`: use the W3C `Activity.TraceId` when ServiceStack tracing is registered, and the existing request identifier otherwise. Preserve the existing `TraceId` filter in both modes.

The Admin UI component exists in both `ServiceStack/modules/admin-ui/components/Profiling.mjs` and `tests/NorthwindAuto/admin-ui/components/Profiling.mjs`. They currently differ mainly in whitespace. Change the core component first, carry the functional changes into the NorthwindAuto copy, and verify both before release. The NorthwindAuto app configures `ProfilingFeature` in `tests/NorthwindAuto/Configure.Profiling.cs`; use it as the integration example.

## Trace model and naming

1. Publish stable source names and constants: `ServiceStack` for API/service operations, `ServiceStack.Messaging` for queue work, and retain the existing `ServiceStack.Jobs`. Keep `System.Net.Http` and `Microsoft.AspNetCore` owned by .NET. Give each custom source an assembly version but do not change its name when package versions change.
2. On ASP.NET Core, the platform's HTTP `SERVER` span is the parent. Create **one** `INTERNAL` span such as `ServiceStack GetOrders` when a ServiceStack operation is identified. Do not create another HTTP `SERVER` span. In a non-ASP.NET Core host, create a `SERVER` span only if no platform server span exists and valid incoming W3C context is available; keep this path separately tested.
3. Use `ActivityKind.PRODUCER` for message publication and `ActivityKind.CONSUMER` for processing. Jobs retain their consumer spans. Treat queued work as causally connected; use the propagated creation context, and use links when a batch contains several messages or multiple independent parents.
4. Add low-cardinality attributes such as `servicestack.operation`, request DTO name, route template, HTTP method, message destination, job queue, and outcome. Use standard HTTP/messaging semantic keys where they apply. Do not put raw URLs, user IDs, tenant IDs, API keys, request/response bodies, SQL parameters, or exception messages in metric dimensions. Trace attributes may include a configured, privacy-reviewed user/tenant tag, disabled by default.
5. Mark spans as errors for unhandled exceptions and failed service responses according to a documented rule; record exception type and a sanitized exception event when enabled. Avoid marking expected validation/authorization responses as server failures by default. Set span status exactly once, including cancellation and early-filter responses.
6. Build all custom metrics using `Meter`, with seconds as the duration unit for new instruments. Keep existing `ServiceStack.Jobs` metric names and millisecond units unchanged for compatibility. Proposed core instruments: `servicestack.operation.duration` histogram, `servicestack.operation.active` up/down counter, and `servicestack.operation.errors` counter. Use bounded DTO/operation names and outcome categories; do not duplicate ASP.NET Core's generic HTTP request metrics.

## Implementation plan

### 1. Central API operation instrumentation in `ServiceStack`

Add a small static diagnostics class owning the API `ActivitySource`, `Meter`, instrument names, and a disposable operation scope. Start that scope at the point where the request operation is known, covering both endpoint routing and the ServiceStack middleware route. Audit `InitRequest`, `RestHandler`, `ServiceRunner<T>`, `HostContext`, and `EndRequest` to find one shared start/stop boundary. Ensure the scope is stopped once for normal response, exception, cancellation, one-way request, filter short circuit, streaming response, and disposal failure. Use the actual operation/route template, not a path containing IDs.

`ProfilingFeature` must no longer be a prerequisite for OpenTelemetry activities. Keep its existing `DiagnosticListener` emissions and UI behavior, but make them observers of the same request execution. Do not remove legacy events during the first release. The older `Activity` created inside `InitRequest` should be retired or limited to legacy targets when the new source is active; verify there is one ServiceStack operation span per request and no duplicate root. Avoid storing sensitive request DTOs on `Activity` tags.

Instrument two or three high-value internal phases first: service invocation, validation/filter execution, and AutoQuery execution. Make detailed phase spans opt-in if they materially increase per-request span count. Use `ActivitySource.HasListeners()` and `Meter` patterns appropriately so disabled tracing is cheap; metrics still work independently of trace sampling. Do not make success metrics depend on an `Activity` being sampled.

### 2. Propagate context through jobs and messages

Keep the existing `BackgroundJob.TraceId` column readable. On enqueue, store valid W3C `Activity.Current.Id`/traceparent and, if needed, add an optional tracestate field through the existing additive job schema migration mechanism. On execution, parse parent context robustly; malformed or legacy IDs start a new trace rather than throwing. Verify the existing `JobsDiagnostics.StartActivity()` parent behavior with a remote parent and with an unsampled parent. Keep queue wait, attempt, retry, and failure metrics already emitted by jobs.

For `IMessage`, prefer its existing `IMeta.Meta` dictionary for `traceparent` and `tracestate`, avoiding a breaking change to the `IMessage` interface. Inject on publish and extract on consume in shared message helpers so Background MQ, Redis MQ, and RabbitMQ follow the same contract. Keep `IMessage.TraceId` for older consumers. The producer span should cover enqueue/send, and the consumer span should cover handler execution; a retry/dead-letter action should be visible without exposing message bodies. Preserve user-defined metadata keys and avoid propagating arbitrary baggage or credentials by default. Test cross-process propagation, not only an in-memory queue where `Activity.Current` may survive by accident.

### 3. Outbound HTTP and data dependencies

For modern `HttpClient`-based clients, rely on .NET/`AddHttpClientInstrumentation()` for the network `CLIENT` span and W3C header injection. Preserve the ServiceStack client `DiagnosticListener` events for Profiling. If a DTO-level logical client span is useful, label it `INTERNAL`, make it opt-in, and ensure it encloses rather than duplicates the network span. Verify refresh-token retries and streaming calls maintain context and do not leak response bodies or authorization headers.

OrmLite and Redis live in sibling repositories and already emit diagnostic events consumed by Profiling. Audit their current instrumentation before adding spans: where a provider already emits database spans, enrich or link them rather than producing duplicates. Put any required provider changes in their owning repositories. The first ServiceStack release can correlate their existing `DiagnosticEntry` rows with `Activity.Current` even before provider-owned spans are available. Apply the same check to gRPC and RabbitMQ integrations.

### 4. Host configuration and public documentation

Document an opt-in host setup that registers `AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation`, `AddSource("ServiceStack", "ServiceStack.Messaging", "ServiceStack.Jobs")`, and `AddMeter("ServiceStack", "ServiceStack.Messaging", "ServiceStack.Jobs")` as applicable. Provide a runnable NorthwindAuto example with an OTLP exporter configured by the application, not by the core library. Include resource attributes (`service.name`, version, environment), sampling, endpoint configuration, and how to inspect the same trace in an external collector. Do not package an exporter into `ServiceStack`.

An illustrative host registration (with the OpenTelemetry hosting, ASP.NET Core, HTTP client, and OTLP packages installed in the **application**) is:

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("northwind-auto"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("ServiceStack", "ServiceStack.Messaging", "ServiceStack.Jobs")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("ServiceStack", "ServiceStack.Messaging", "ServiceStack.Jobs")
        .AddOtlpExporter());
```

Keep exporter endpoint and credentials in environment/application configuration. The exact package versions and final source constants should be verified while implementing the sample.

Make it explicit which metrics are framework-specific versus already emitted by ASP.NET Core, `HttpClient`, and the jobs package. Document source/instrument names and attribute cardinality as a compatibility contract. Explain that sampling controls exported traces while the bounded Profiling event store has its own retention and access policy.

### 5. Enrich `ProfilingFeature` and its Admin API

Extend `DiagnosticEntry` with nullable `SpanId` (16 hex), `ParentSpanId`, and a span kind/status or outcome field where known. Populate `TraceId` from `Activity.TraceId` when ServiceStack tracing is registered, and retain its legacy value otherwise. Populate span fields from `Activity.Current` in tracing mode; this makes current ServiceStack, client, OrmLite, Redis, and job rows correlate through the existing filter. Preserve old rows. Do not use the legacy `DiagnosticsUtils.GetTraceId()` helper for the W3C value. Correct duration conversion from `Stopwatch.GetTimestamp()` using `Stopwatch.Frequency` rather than treating stopwatch ticks as `TimeSpan` ticks.

Use the existing `TraceId` filter for both W3C and legacy trace IDs, and add optional filters for span ID, source, and errors. Keep the existing role check, bounded `Capacity`, page limit, and request/body redaction behavior. Return a trace-focused response or view model with rows ordered by actual start time and enough parent IDs to build a tree; do not assume every parent span is retained locally. If a local `ActivityListener` is added to show completed spans, subscribe only to configured ServiceStack sources, bound its memory/retention, and deduplicate against existing diagnostic rows. Do not turn the Admin UI into a general OTLP collector or force a listener that changes production sampling unexpectedly.

Expose only safe, explicit metadata for the UI, such as whether W3C fields are available and an optional external trace URL template. Validate that template on the server and encode the trace ID before substitution. Keep the UI read-only; no exporter secrets or raw headers belong in app metadata or the browser.

### 6. Update the Profiling Admin UI and NorthwindAuto copy

Update `ServiceStack/modules/admin-ui/components/Profiling.mjs` and `tests/NorthwindAuto/admin-ui/components/Profiling.mjs` together. The existing table, event details, `TraceId` link, filters, and keyboard navigation should still work for old diagnostic rows. Use the existing Trace Id filter and add a trace view showing a compact span/event waterfall or parent-child list, operation names, durations, statuses, and gaps for spans not retained locally. Let a user open a W3C trace in the configured external backend. Do not truncate the actual value used for filtering or linking.

Keep the `traceId` route query key and `linkFields` so filters survive pagination and back/forward navigation in both modes. Keep `SummaryFields` configurable; add new columns only when present in metadata or make them opt-in. Ensure empty and partially instrumented traces show a useful state rather than an empty tree. Mirror functional changes into the NorthwindAuto copy and compare both files, allowing only intentional app-specific differences.

Use `tests/NorthwindAuto/Configure.Profiling.cs` to turn on the demo view, then verify the Admin UI against a request that calls an outbound API, writes to OrmLite, and enqueues a job. Test the trace link, pagination, filters, accessibility labels, and older rows that have no span fields. A browser check is warranted here because the user-facing trace layout and route-state behavior are central to the feature.

## Implementation rules from the release review

A review before v10.3 found issues in the first implementation, and they have been fixed. The rules below
record how the implementation now behaves, so later changes keep it that way. They apply alongside the plan
above:

- Keep `net472` and `net6.0` behaviour unchanged. New code goes behind `#if NET8_0_OR_GREATER`.
- When nothing is listening, telemetry costs close to nothing. Check `ActivitySource.HasListeners()` or
  `Instrument.Enabled` before allocating anything.
- Profiling works the same with or without OpenTelemetry, apart from the W3C Trace Id and Span Id fields.

### API operations

- **Only known operations get telemetry.** `ServiceStackHost.GetTelemetryOperationName()` resolves the name
  before a scope starts. `RestHandler` uses `RestPath.RequestType.GetOperationName()`. Other
  `IServiceStackHandler`s have any format extension stripped (`/api/GetOrders.json` becomes `GetOrders`),
  and the name is used only if `Metadata.GetOperationType(name)` finds it. Static files, redirects,
  not-found handlers and unknown `/api/{anything}` names get no operation scope, so requests to random
  paths can't create unlimited metric series.
- **The route is set once, in `InitRequest`.** `RestHandler` requests use `RestPath.Path`. `GenericHandler`
  requests created by `ApiHandlers` use the new `GenericHandler.RouteTemplate`, e.g. `/api/{Request}`.
  `SetRoute` was removed so that span and metric tags can't differ.
- **Phase spans are shared.** `ServiceStackHandlerBase` has `ApplyPreRequestFilters`,
  `ApplyRequestFiltersAsync` and `InvokeServiceAsync` helpers, used by both `RestHandler` and
  `GenericHandler`. They skip the extra async state machine unless
  `OperationDiagnostics.IsPhaseEnabled` is true.
- **`OperationScope`:**
  - `Start` returns the shared `OperationScope.Noop` when `OperationDiagnostics.IsEnabled` is false, and
    `InitRequest` doesn't store it in `Items`.
  - `Complete(...)` is the only way to record a real outcome. `Dispose()` records `unknown`, so disposing
    the scope early can't record a failure as a success.
  - Item keys are `Keywords.OperationScope` and `Keywords.ProfilingOperationId`.
- **Incoming `traceparent`** is parsed with `isRemote: true`, as are message and Job parents.

### Profiling correlation

- **User Id and Tag.** When there's no ServiceStack span, `InitRequest` starts the local Profiling Activity
  with its `UserId`, `Tag` and `OperationId` tags, as before. When the span is active and `ProfilingFeature`
  is registered, User Id and Tag are stored with `SetCustomProperty`, which isn't exported, so user ids stay
  out of the tracing backend.
- On .NET 8+, `DiagnosticsUtils.GetUserId`/`GetTag` (`ServiceStack.Text/Diagnostics.cs`) walk up from the
  current Activity and return the first value found. They check custom properties first, then tags, because
  the root Activity may belong to ASP.NET Core. OrmLite's own connection tag, e.g. the operation name set by
  `Service.Db`, still takes precedence over the Profiling Tag.
- **W3C Ids only.** `ProfilerDiagnosticObserver.CreateDiagnosticEntry` copies Trace, Span and Parent Span Ids
  only from W3C Activities. A root span gets `ParentSpanId = null`. With a hierarchical Activity, the entry
  falls back to the original entry's TraceId, then to the legacy request id, and leaves `SpanId` null.

### Messaging

- `MessagingDiagnostics.StartPublish(system, destination, message)` and
  `StartProcess(message, system, destination, profilingArgs)` return a `MessagingScope`, or the shared
  `MessagingScope.Noop`. Record failures with `scope.RecordError(ex)`, and dispose the scope to end the span
  and record metrics.
- Spans are named `send {queue}` and `process {queue}`. The consumer uses `QueueNames<T>.In`, so both spans
  use the same destination. Both are tagged with `messaging.system` (`servicestack.background`, `redis`,
  `rabbitmq` or `servicestack.memory`), `messaging.operation.type`, `messaging.destination.name` and
  `messaging.message.id`.
- The `ServiceStack.Messaging` meter records `messaging.client.sent.messages`,
  `messaging.client.consumed.messages` and `messaging.process.duration` (s). They're tagged with
  `messaging.system`, `messaging.destination.name` and, on failure, `error.type`.
- **MQ Profiling without OpenTelemetry.** When nothing is listening, `MessageHandler` passes
  `profilingArgs` if `Diagnostics.ServiceStack.IsEnabled(WriteMqRequestBefore)`. `StartProcess` then starts
  the local `MqBegin` Activity whose parent is `message.TraceId`, tagged with `message.Tag`. It emits the same
  DiagnosticListener start and stop events as before. This is the only MQ code path on every target.

### Background Jobs

- `jobs poll` uses `JobsDiagnostics.StartProfilingScope(name)`. It never uses the exported
  `ActivitySource`, so ticks don't export a root trace each, but the poll's OrmLite events are still grouped
  in Profiling. `jobs startup` and `jobs shutdown` still use `StartInternalActivity`, because each happens
  only once.
- Deferred: an exported `jobs dispatch` span, linked to each claimed Job's trace, is only worth adding when a
  tick claims Jobs. It needs `DispatchPendingJobs` to return what it claimed.

### Admin API and UI

- `AdminProfilingService` returns up to `feature.Capacity` rows when `TraceId` is set and `Take` isn't given,
  so a trace is read in full. Ordering by date uses the same `IsNullOrEmpty` check as the filter.
- `ProfilingFeature.GetExternalTraceUrl` accepts HTTPS, or HTTP when `uri.IsLoopback`, e.g. local Jaeger or
  the Aspire Dashboard. It still requires 32 hex characters, no user info and an absolute URL. An invalid
  `ExternalTraceUrlTemplate` logs a warning when the plugin is registered.
- The Trace view shows one row per step: its After or Error event, or its Before event marked **pending**
  when no After or Error event has the same `OperationId`. When `total > results.length`, it shows "Showing N
  of total events" with a **Load all** link.
- The Trace or Details tab is stored in the `view` route query key, which defaults to `details`, so
  back/forward and shared links keep it.
- The Args and Error JSON panes have `CopyIcon` buttons. `Profiling.mjs` and `app.mjs` are identical in
  `modules/admin-ui` and `tests/NorthwindAuto/admin-ui`.
- Still to do: the `Sidebar.mjs` text-size changes and the `css/ui.css` rebuild aren't part of OpenTelemetry
  and should be committed separately.

### Tests

`OpenTelemetryAppHostTests` runs a real AppHost to cover:

- unknown API names, and `.json` extensions being stripped
- RestHandler and GenericHandler routes, including the phase spans
- no scope stored in `Items` when nothing is listening
- OrmLite User Id and Tag, with and without a listener
- no user data on the exported span
- MQ Trace Id grouping without OpenTelemetry

`OpenTelemetryTests` covers messaging span names, attributes and metrics, no-op scopes, the `unknown`
outcome, W3C and hierarchical Ids, and external trace URLs. `DbJobsTests` checks that a tick exports no spans
of its own and still groups its Profiling events.

## Verification matrix

1. **Activity hierarchy:** with an in-memory `ActivityListener`, an HTTP request produces one platform `SERVER` span and one ServiceStack `INTERNAL` child. Nested service work, outbound HTTP, message publication, and job execution share the W3C trace ID with correct parent/link relationships. Verify both endpoint routing and middleware routing where supported.
2. **Failure paths:** validation error, auth rejection, thrown service exception, cancellation, streaming disconnect, failed queue publish, job retry, and dead-letter outcomes close scopes once and record correct status/metrics.
3. **Propagation:** inject/extract through RabbitMQ and Redis or a serialized cross-process fixture; malformed trace context is ignored safely. Existing jobs/messages lacking W3C metadata continue to execute.
4. **Metrics:** use `MeterListener` to check names, units, count, duration, and bounded dimensions with tracing disabled and enabled. Run a small benchmark on a hot API path to quantify overhead when no OpenTelemetry listener is registered.
5. **Compatibility:** existing `ProfilingFeature` event counts, old `TraceId` filtering, MiniProfiler, Request Logs, and jobs diagnostics still work; exporters do not need `ProfilingFeature` enabled.
6. **Admin API/UI:** role enforcement, redaction, result limits, W3C filters, trace tree ordering, missing-parent display, external link encoding, and NorthwindAuto browser behavior pass. Existing uninstrumented rows remain readable.
7. **Builds:** build all affected modern targets and ensure conditional compilation leaves `net472` and `net6.0` unchanged unless a deliberate compatibility slice is added.
8. **End to end with a backend:** run NorthwindAuto against the Aspire Dashboard
   (`docker run --rm -it -p 18888:18888 -p 4317:18889 mcr.microsoft.com/dotnet/aspire-dashboard:latest`) and
   check one request that calls an outbound API, writes to OrmLite, publishes an MQ message and queues a Job:
   - It appears as one trace, with no `jobs poll` root traces.
   - `/api/NotARealOperation` adds no new metric series.
   - The Profiling UI shows the same Trace Id, a complete Trace view, User Id and Tag on OrmLite rows, and a
     working **Open in trace backend** link to `http://localhost:18888/...`.
9. **Without OpenTelemetry:** repeat check 8 without the OpenTelemetry registration. Profiling rows for
   requests, MQ messages and Jobs are grouped by Trace Id as they were before OpenTelemetry was added.

## Delivery order and release criteria

Deliver API operation spans/metrics and app configuration first; then context propagation through jobs/messages; then client/data correlation; then Admin API and both UI components. The release is complete when a NorthwindAuto request can be followed from the ASP.NET Core server span through its ServiceStack operation and queued work in an external OpenTelemetry backend **and** from the Profiling Admin UI by W3C trace ID, without duplicate HTTP spans or a regression in existing profiling views.

## References

- [OpenTelemetry .NET instrumentation guidance](https://opentelemetry.io/docs/languages/dotnet/instrumentation/)
- [OpenTelemetry HTTP span semantic conventions](https://opentelemetry.io/docs/specs/semconv/http/http-spans/)
- [OpenTelemetry messaging context and spans](https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/)
- [.NET built-in tracing concepts](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-concepts)

## Feature Benefits

**Follow a request from the API to the work it starts.** ServiceStack's OpenTelemetry support connects API execution, outbound HTTP calls, message queues, and background jobs in a single distributed trace. When a request triggers work on another server, the trace shows how that work relates to the original call.

**Find slow operations faster.** ServiceStack operation spans and duration metrics show where time is spent in service execution, validation, AutoQuery, and queued work. Teams can distinguish a slow API from a slow dependency or a job waiting in a queue, then focus their investigation on the right component.

**Understand failures in context.** Trace and span IDs connect failed API calls with downstream requests, message processing, retries, and job failures. Error status and timing make it easier to reconstruct what happened without piecing together unrelated log entries.

**Explore traces in the familiar Profiling Admin UI.** The Profiling view adds trace filtering and a timeline of related operations, while retaining its existing event details. Developers can inspect a trace locally, copy its full ID, or open it in a configured external tracing backend.

**Use the observability platform you already have.** ServiceStack emits standard OpenTelemetry traces and metrics, while your application chooses its collector, exporter, sampling settings, and dashboards. There is no ServiceStack-specific telemetry service to deploy.

**Adopt it without losing existing diagnostics.** Request Logs, MiniProfiler, the current Profiling events, and job diagnostics continue to work. Applications can add OpenTelemetry incrementally and keep control over sampling, data retention, and which details are exposed in the Admin UI.
