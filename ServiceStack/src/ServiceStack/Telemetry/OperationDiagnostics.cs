#nullable enable
using System;
using System.Diagnostics;
#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;
using System.Threading;
#endif
using ServiceStack.Web;

namespace ServiceStack.Telemetry;

/// <summary>Framework operation telemetry. Exporters and sampling belong to the host application.</summary>
public static class OperationDiagnostics
{
    public const string Name = "ServiceStack";
    public const string DurationName = "servicestack.operation.duration";
    public const string ActiveName = "servicestack.operation.active";
    public const string ErrorsName = "servicestack.operation.errors";

    public const string PreRequestFiltersPhase = "ServiceStack pre-request filters";
    public const string RequestFiltersPhase = "ServiceStack request filters";
    public const string ServiceInvocationPhase = "ServiceStack service invocation";
#if NET8_0_OR_GREATER
    public static readonly ActivitySource ActivitySource = new(Name, typeof(OperationDiagnostics).Assembly.GetName().Version?.ToString());
    public static readonly Meter Meter = new(Name, typeof(OperationDiagnostics).Assembly.GetName().Version?.ToString());
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(DurationName, "s");
    private static readonly UpDownCounter<long> Active = Meter.CreateUpDownCounter<long>(ActiveName);
    private static readonly Counter<long> Errors = Meter.CreateCounter<long>(ErrorsName);
    public static bool HasListeners => ActivitySource.HasListeners();
    /// <summary>Opt in to additional per-request phase spans.</summary>
    public static bool EnableDetailedSpans { get; set; }

    /// <summary>True when an Activity listener or any operation instrument is being collected.</summary>
    public static bool IsEnabled => ActivitySource.HasListeners() || Duration.Enabled || Active.Enabled || Errors.Enabled;

    /// <summary>True when detailed phase spans are enabled and something is listening.</summary>
    public static bool IsPhaseEnabled => EnableDetailedSpans && ActivitySource.HasListeners();

    public static Activity? StartPhase(string name) =>
        IsPhaseEnabled ? ActivitySource.StartActivity(name, ActivityKind.Internal) : null;

    /// <summary>
    /// Starts telemetry for an API operation. Returns <see cref="OperationScope.Noop"/>, without allocating,
    /// when nothing is listening.
    /// </summary>
    public static OperationScope Start(IRequest request, string operation, string? route = null)
    {
        if (!IsEnabled)
            return OperationScope.Noop;

        var tags = new TagList {
            { "servicestack.operation", operation },
            { "http.request.method", request.Verb },
        };
        if (route != null)
            tags.Add("http.route", route);
        Activity? activity = null;
        if (ActivitySource.HasListeners())
        {
            var incoming = request.GetHeader("traceparent");
            activity = Activity.Current == null &&
                ActivityContext.TryParse(incoming, request.GetHeader("tracestate"), isRemote: true, out var parent)
                ? ActivitySource.StartActivity("ServiceStack " + operation, ActivityKind.Server, parent)
                : ActivitySource.StartActivity("ServiceStack " + operation, ActivityKind.Internal);
        }
        if (activity != null)
        {
            activity.SetTag("servicestack.operation", operation);
            activity.SetTag("http.request.method", request.Verb);
            if (route != null)
                activity.SetTag("http.route", route);
        }
        Active.Add(1, tags);
        return new OperationScope(activity, tags);
    }

    public sealed class OperationScope : IDisposable
    {
        /// <summary>Shared scope returned when nothing is listening. Records nothing.</summary>
        public static readonly OperationScope Noop = new();

        private readonly Activity? activity;
        private readonly TagList tags;
        private readonly long start;
        private int completed;

        private OperationScope() => completed = 1;

        internal OperationScope(Activity? activity, TagList tags)
        {
            this.activity = activity;
            this.tags = tags;
            this.start = Stopwatch.GetTimestamp();
        }

        public bool IsNoop => ReferenceEquals(this, Noop);

        /// <summary>The operation span, if one was started.</summary>
        public Activity? Activity => activity;

        /// <summary>Records the operation's outcome. Only the first call is recorded.</summary>
        public void Complete(int statusCode, Exception? exception = null)
        {
            var outcome = exception is OperationCanceledException ? "cancelled"
                : statusCode >= 500 || (exception != null && statusCode < 400) ? "error"
                : statusCode >= 400 ? "client_error" : "success";
            Finish(outcome, statusCode, exception);
        }

        private void Finish(string outcome, int? statusCode, Exception? exception)
        {
            if (Interlocked.Exchange(ref completed, 1) != 0)
                return;
            var completedTags = tags;
            completedTags.Add("servicestack.outcome", outcome);
            if (outcome == "error")
            {
                Errors.Add(1, completedTags);
                if (exception != null)
                    activity?.SetTag("exception.type", exception.GetType().FullName);
            }
            activity?.SetStatus(outcome == "error" ? ActivityStatusCode.Error
                : outcome == "success" || outcome == "client_error" ? ActivityStatusCode.Ok
                : ActivityStatusCode.Unset);
            activity?.SetTag("servicestack.outcome", outcome);
            if (statusCode != null)
                activity?.SetTag("http.response.status_code", statusCode.Value);
            Duration.Record((Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency, completedTags);
            Active.Add(-1, tags);
            activity?.Dispose();
        }

        /// <summary>
        /// Ends the scope without a known result, recording the outcome as <c>unknown</c>.
        /// Use <see cref="Complete"/> to record a real outcome.
        /// </summary>
        public void Dispose() => Finish("unknown", null, null);
    }
#else
    public static bool HasListeners => false;
    public static bool IsEnabled => false;

    /// <summary>Opt in to additional per-request phase spans.</summary>
    public static bool EnableDetailedSpans { get; set; }

    public static bool IsPhaseEnabled => false;

    public static Activity? StartPhase(string name) => null;

    public static OperationScope Start(IRequest request, string operation, string? route = null) => OperationScope.Noop;

    public sealed class OperationScope : IDisposable
    {
        public static readonly OperationScope Noop = new();

        private OperationScope() { }

        public bool IsNoop => true;
        public Activity? Activity => null;
        public void Complete(int statusCode, Exception? exception = null) { }
        public void Dispose() { }
    }
#endif
}
