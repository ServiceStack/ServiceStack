#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
#if NET8_0_OR_GREATER
using System.Collections.Generic;
using System.Diagnostics.Metrics;
#endif

namespace ServiceStack.Messaging;

/// <summary>W3C message context, spans and metrics shared by queue implementations.</summary>
public static class MessagingDiagnostics
{
    public const string Name = "ServiceStack.Messaging";
    public const string TraceParentKey = "traceparent";
    public const string TraceStateKey = "tracestate";

    public const string SentMessagesName = "messaging.client.sent.messages";
    public const string ConsumedMessagesName = "messaging.client.consumed.messages";
    public const string ProcessDurationName = "messaging.process.duration";

    /// <summary>Values for the <c>messaging.system</c> attribute</summary>
    public static class Systems
    {
        public const string Background = "servicestack.background";
        public const string InMemory = "servicestack.memory";
        public const string Redis = "redis";
        public const string RabbitMq = "rabbitmq";
    }

    /// <summary>Resolve the <c>messaging.system</c> of an MQ Server</summary>
    public static string GetSystem(IMessageService? mqService)
    {
        var typeName = mqService?.GetType().Name;
        return typeName == null ? "servicestack"
            : typeName.IndexOf("Rabbit", StringComparison.OrdinalIgnoreCase) >= 0 ? Systems.RabbitMq
            : typeName.IndexOf("Redis", StringComparison.OrdinalIgnoreCase) >= 0 ? Systems.Redis
            : typeName.IndexOf("Background", StringComparison.OrdinalIgnoreCase) >= 0 ? Systems.Background
            : typeName.IndexOf("InMemory", StringComparison.OrdinalIgnoreCase) >= 0 ? Systems.InMemory
            : typeName;
    }

#if NET8_0_OR_GREATER
    public static readonly ActivitySource ActivitySource = new(Name, typeof(MessagingDiagnostics).Assembly.GetName().Version?.ToString());
    public static readonly Meter Meter = new(Name, typeof(MessagingDiagnostics).Assembly.GetName().Version?.ToString());
    private static readonly Counter<long> SentMessages =
        Meter.CreateCounter<long>(SentMessagesName, "{message}", "Messages a producer attempted to send");
    private static readonly Counter<long> ConsumedMessages =
        Meter.CreateCounter<long>(ConsumedMessagesName, "{message}", "Messages delivered to a consumer");
    private static readonly Histogram<double> ProcessDuration =
        Meter.CreateHistogram<double>(ProcessDurationName, "s", "Duration of processing a message");

    /// <summary>Starts a <c>send {destination}</c> producer span. Returns <see cref="MessagingScope.Noop"/> when nothing is listening.</summary>
    public static MessagingScope StartPublish(string system, string destination, IMessage? message = null)
    {
        var hasListeners = ActivitySource.HasListeners();
        if (!hasListeners && !SentMessages.Enabled)
            return MessagingScope.Noop;

        var activity = hasListeners
            ? ActivitySource.StartActivity("send " + destination, ActivityKind.Producer)
            : null;
        SetTags(activity, system, "send", destination, message);
        return new MessagingScope(activity, CreateTags(system, destination), isProcess: false);
    }

    public static void Inject(IMessage message)
    {
        var current = Activity.Current;
        if (current?.IdFormat != ActivityIdFormat.W3C || current.Id == null)
            return;
        message.Meta ??= new Dictionary<string, string>();
        message.Meta[TraceParentKey] = current.Id;
        if (current.TraceStateString != null)
            message.Meta[TraceStateKey] = current.TraceStateString;
        else
            message.Meta.Remove(TraceStateKey);
    }

    /// <summary>
    /// Starts a <c>process {destination}</c> consumer span, continuing the trace in the message's <c>traceparent</c>.
    /// When nothing is listening and <paramref name="profilingArgs"/> is provided (i.e. Profiling is enabled),
    /// starts a local, non-exported Activity instead so the message's Profiling events share its TraceId.
    /// </summary>
    public static MessagingScope StartProcess(IMessage message, string system, string destination,
        Func<Activity, IMessage, object>? profilingArgs = null)
    {
        var hasListeners = ActivitySource.HasListeners();
        var metricsEnabled = ConsumedMessages.Enabled || ProcessDuration.Enabled;
        if (!hasListeners && !metricsEnabled)
            return StartProfilingScope(message, profilingArgs);

        Activity? activity = null;
        if (hasListeners)
        {
            if (message.Meta != null && message.Meta.TryGetValue(TraceParentKey, out var parent) &&
                ActivityContext.TryParse(parent,
                    message.Meta.TryGetValue(TraceStateKey, out var state) ? state : null,
                    isRemote: true, out var context))
                activity = ActivitySource.StartActivity("process " + destination, ActivityKind.Consumer, context);
            activity ??= ActivitySource.StartActivity("process " + destination, ActivityKind.Consumer);
        }
        SetTags(activity, system, "process", destination, message);

        var tags = CreateTags(system, destination);
        ConsumedMessages.Add(1, tags);
        if (activity == null)
        {
            var profiling = StartProfilingScope(message, profilingArgs);
            if (!profiling.IsNoop)
                return new MessagingScope(profiling.Activity, tags, isProcess: true, message, profilingArgs);
        }
        return new MessagingScope(activity, tags, isProcess: true);
    }

    private static TagList CreateTags(string system, string destination) => new() {
        { "messaging.system", system },
        { "messaging.destination.name", destination },
    };

    private static void SetTags(Activity? activity, string system, string operationType, string destination, IMessage? message)
    {
        if (activity == null)
            return;
        activity.SetTag("messaging.system", system);
        activity.SetTag("messaging.operation.type", operationType);
        activity.SetTag("messaging.destination.name", destination);
        if (message != null && message.Id != Guid.Empty)
            activity.SetTag("messaging.message.id", message.Id.ToString());
    }

    public static void RecordError(MessagingScope scope, Exception error) => scope.RecordError(error);

    public sealed class MessagingScope : IDisposable
    {
        /// <summary>Shared scope returned when nothing is listening. Records nothing.</summary>
        public static readonly MessagingScope Noop = new();

        private readonly Activity? activity;
        private readonly TagList tags;
        private readonly bool isProcess;
        private readonly bool recordMetrics;
        private readonly long start;
        private readonly IMessage? profilingMessage;
        private readonly Func<Activity, IMessage, object>? profilingArgs;
        private string? errorType;
        private int completed;

        private MessagingScope() => completed = 1;

        internal MessagingScope(Activity? activity, IMessage message, Func<Activity, IMessage, object> profilingArgs)
        {
            this.activity = activity;
            this.profilingMessage = message;
            this.profilingArgs = profilingArgs;
        }

        internal MessagingScope(Activity? activity, TagList tags, bool isProcess,
            IMessage? profilingMessage = null, Func<Activity, IMessage, object>? profilingArgs = null)
        {
            this.activity = activity;
            this.tags = tags;
            this.isProcess = isProcess;
            this.recordMetrics = true;
            this.start = Stopwatch.GetTimestamp();
            this.profilingMessage = profilingMessage;
            this.profilingArgs = profilingArgs;
        }

        public bool IsNoop => ReferenceEquals(this, Noop);

        /// <summary>The messaging span, or the local Profiling Activity</summary>
        public Activity? Activity => activity;

        public void RecordError(Exception error)
        {
            if (IsNoop)
                return;
            errorType = error.GetType().FullName;
            if (profilingArgs != null)
                return; // Local Profiling Activity isn't exported
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.SetTag("exception.type", errorType);
            activity?.SetTag("error.type", errorType);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref completed, 1) != 0)
                return;
            if (recordMetrics)
            {
                var completedTags = tags;
                if (errorType != null)
                    completedTags.Add("error.type", errorType);
                if (isProcess)
                    ProcessDuration.Record((Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency, completedTags);
                else
                    SentMessages.Add(1, completedTags);
            }
            if (activity == null)
                return;
            if (profilingArgs != null && profilingMessage != null)
                Diagnostics.ServiceStack.StopActivity(activity, profilingArgs(activity, profilingMessage));
            else
                activity.Dispose();
        }
    }
#else
    public static MessagingScope StartPublish(string system, string destination, IMessage? message = null) => MessagingScope.Noop;
    public static void Inject(IMessage message) { }

    /// <summary>
    /// Starts a local, non-exported Activity when <paramref name="profilingArgs"/> is provided (i.e. Profiling is enabled)
    /// so the message's Profiling events share its TraceId.
    /// </summary>
    public static MessagingScope StartProcess(IMessage message, string system, string destination,
        Func<Activity, IMessage, object>? profilingArgs = null) => StartProfilingScope(message, profilingArgs);

    public static void RecordError(MessagingScope scope, Exception error) { }

    public sealed class MessagingScope : IDisposable
    {
        public static readonly MessagingScope Noop = new();

        private readonly Activity? activity;
        private readonly IMessage? profilingMessage;
        private readonly Func<Activity, IMessage, object>? profilingArgs;
        private int completed;

        private MessagingScope() => completed = 1;

        internal MessagingScope(Activity? activity, IMessage message, Func<Activity, IMessage, object> profilingArgs)
        {
            this.activity = activity;
            this.profilingMessage = message;
            this.profilingArgs = profilingArgs;
        }

        public bool IsNoop => ReferenceEquals(this, Noop);
        public Activity? Activity => activity;
        public void RecordError(Exception error) { }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref completed, 1) != 0)
                return;
            if (activity != null && profilingArgs != null && profilingMessage != null)
                Diagnostics.ServiceStack.StopActivity(activity, profilingArgs(activity, profilingMessage));
        }
    }
#endif

    private static MessagingScope StartProfilingScope(IMessage message, Func<Activity, IMessage, object>? profilingArgs)
    {
        if (profilingArgs == null || Activity.Current != null || message.TraceId == null)
            return MessagingScope.Noop;

        var activity = new Activity(Diagnostics.Activity.MqBegin);
        activity.SetParentId(message.TraceId);
        if (message.Tag != null)
            activity.AddTag(Diagnostics.Activity.Tag, message.Tag);
        Diagnostics.ServiceStack.StartActivity(activity, profilingArgs(activity, message));
        return new MessagingScope(activity, message, profilingArgs);
    }
}
