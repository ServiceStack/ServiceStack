# OpenTelemetry host setup

ServiceStack emits .NET `ActivitySource` and `Meter` signals on .NET 8 and later. The host owns sampling, resource identity, collector credentials, and exporters. The core package does not depend on an OpenTelemetry SDK.

Install `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, and `OpenTelemetry.Exporter.OpenTelemetryProtocol` in the application. In an ASP.NET Core application's service configuration:

```csharp
services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("northwind-auto",
        serviceVersion: typeof(ConfigureProfiling).Assembly.GetName().Version?.ToString()))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(ServiceStack.Telemetry.OperationDiagnostics.Name,
            ServiceStack.Messaging.MessagingDiagnostics.Name,
            ServiceStack.Jobs.JobsDiagnostics.Name)
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter(ServiceStack.Telemetry.OperationDiagnostics.Name,
            ServiceStack.Messaging.MessagingDiagnostics.Name,
            ServiceStack.Jobs.JobsDiagnostics.Name)
        .AddOtlpExporter());
```

Use the standard `OTEL_EXPORTER_OTLP_ENDPOINT` and exporter environment variables to configure a collector. Set `service.name`, version, and deployment environment in the host's resource configuration. Configure sampling in the host's tracing builder; it controls exported spans, while `ProfilingFeature.Capacity` controls the separate bounded Admin UI event store.

The `ServiceStack` source emits one `INTERNAL` operation span beneath the ASP.NET Core HTTP server span. Its metrics are `servicestack.operation.duration` (seconds), `servicestack.operation.active`, and `servicestack.operation.errors`. Metric attributes are operation name, HTTP method, route template where known, and outcome. `ServiceStack.Messaging` emits producer and consumer spans, propagating W3C `traceparent` and `tracestate` in message metadata. `ServiceStack.Jobs` retains its existing metric names and millisecond units.

For non-ASP.NET Core hosts, a valid incoming `traceparent` with no active platform span starts a ServiceStack `SERVER` span. Malformed context is ignored. A response with HTTP status 500 or higher is an operation error; 400-level validation and authorization responses have a `client_error` outcome. Set `OperationDiagnostics.EnableDetailedSpans = true` to add request filter, service invocation, and AutoQuery phase spans. This is disabled by default to keep trace volume low.

Redis MQ, RabbitMQ, and Background MQ inject context in `IMessage.Meta` on publish; the shared message handler extracts it on consume. Existing `IMessage.TraceId` and `BackgroundJob.TraceId` remain readable for older consumers. Current OrmLite and Redis diagnostic events are correlated in the local Profiling view using `Activity.Current`; no provider spans are added by this package.

On the Admin Profiling page, filter by Trace Id to see locally retained events. When ServiceStack tracing is registered, profiling entries use the W3C trace ID; otherwise they keep the existing request identifier. `ProfilingFeature.ExternalTraceUrlTemplate` can point to an HTTPS trace viewer URL containing `{traceId}`; the Admin API returns a link only for a valid W3C trace ID.
