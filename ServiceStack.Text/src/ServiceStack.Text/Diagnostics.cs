#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using ServiceStack.Text;

namespace ServiceStack;

public class Diagnostics
{
    private static readonly Diagnostics Instance = new();
    private Diagnostics(){}

    private bool includeStackTrace;
    public static bool IncludeStackTrace
    {
        get => Instance.includeStackTrace;
        set => Instance.includeStackTrace = value;
    }
    
    public static class Listeners
    {
        public const string ServiceStack = "ServiceStack";
        public const string Client = "ServiceStack.Client";
        public const string OrmLite = "ServiceStack.OrmLite";
        public const string Redis = "ServiceStack.Redis";

        // HttpClient Listener
        public const string HttpClient = "HttpHandlerDiagnosticListener";
    }
    
    public static class Keys
    {
        public const string OperationId = nameof(OperationId);
        public const string Request = nameof(Request);
        public const string ResponseType = nameof(ResponseType);

        public const string Response = nameof(Response);
        public const string LoggingRequestId = nameof(LoggingRequestId);
        public const string Timestamp = nameof(Timestamp);
        public const string Date = nameof(Date);
#if NET6_0_OR_GREATER
        public static readonly System.Net.Http.HttpRequestOptionsKey<Guid> HttpRequestOperationId = new(OperationId);
        public static readonly System.Net.Http.HttpRequestOptionsKey<object> HttpRequestRequest = new(Request);
        public static readonly System.Net.Http.HttpRequestOptionsKey<object> HttpRequestResponseType = new(ResponseType);
#endif
    }
    
    public static class Events
    {
        public static class ServiceStack
        {
            private const string Prefix = Listeners.ServiceStack + ".";
            
            public const string WriteRequestBefore = Prefix + nameof(WriteRequestBefore);
            public const string WriteRequestAfter = Prefix + nameof(WriteRequestAfter);
            public const string WriteRequestError = Prefix + nameof(WriteRequestError);
            
            public const string WriteGatewayBefore = Prefix + nameof(WriteGatewayBefore);
            public const string WriteGatewayAfter = Prefix + nameof(WriteGatewayAfter);
            public const string WriteGatewayError = Prefix + nameof(WriteGatewayError);
            
            public const string WriteMqRequestBefore = Prefix + nameof(WriteMqRequestBefore);
            public const string WriteMqRequestAfter = Prefix + nameof(WriteMqRequestAfter);
            public const string WriteMqRequestError = Prefix + nameof(WriteMqRequestError);
            public const string WriteMqRequestPublish = Prefix + nameof(WriteMqRequestPublish);
        }
        
        public static class Client
        {
            private const string Prefix = Listeners.Client + ".";
            
            public const string WriteRequestBefore = Prefix + nameof(WriteRequestBefore);
            public const string WriteRequestAfter = Prefix + nameof(WriteRequestAfter);
            public const string WriteRequestError = Prefix + nameof(WriteRequestError);
        }
        
        public static class HttpClient
        {
            private const string Prefix = "System.Net.Http.";
            
            public const string Request = Prefix + nameof(Request);
            public const string Response = Prefix + nameof(Response);

            public const string OutStart = Prefix + "HttpRequestOut.Start";
            public const string OutStop = Prefix + "HttpRequestOut.Stop";
        }
        
        public static class OrmLite
        {
            private const string Prefix = Listeners.OrmLite + ".";
            
            public const string WriteCommandBefore = Prefix + nameof(WriteCommandBefore);
            public const string WriteCommandAfter = Prefix + nameof(WriteCommandAfter);
            public const string WriteCommandError = Prefix + nameof(WriteCommandError);
            
            public const string WriteConnectionOpenBefore = Prefix + nameof(WriteConnectionOpenBefore);
            public const string WriteConnectionOpenAfter = Prefix + nameof(WriteConnectionOpenAfter);
            public const string WriteConnectionOpenError = Prefix + nameof(WriteConnectionOpenError);
            
            public const string WriteConnectionCloseBefore = Prefix + nameof(WriteConnectionCloseBefore);
            public const string WriteConnectionCloseAfter = Prefix + nameof(WriteConnectionCloseAfter);
            public const string WriteConnectionCloseError = Prefix + nameof(WriteConnectionCloseError);

            public const string WriteTransactionOpen = Prefix + nameof(WriteTransactionOpen);

            public const string WriteTransactionCommitBefore = Prefix + nameof(WriteTransactionCommitBefore);
            public const string WriteTransactionCommitAfter = Prefix + nameof(WriteTransactionCommitAfter);
            public const string WriteTransactionCommitError = Prefix + nameof(WriteTransactionCommitError);

            public const string WriteTransactionRollbackBefore = Prefix + nameof(WriteTransactionRollbackBefore);
            public const string WriteTransactionRollbackAfter = Prefix + nameof(WriteTransactionRollbackAfter);
            public const string WriteTransactionRollbackError = Prefix + nameof(WriteTransactionRollbackError);
        }
        
        public static class Redis
        {
            private const string Prefix = Listeners.Redis + ".";
            
            public const string WriteCommandBefore = Prefix + nameof(WriteCommandBefore);
            public const string WriteCommandAfter = Prefix + nameof(WriteCommandAfter);
            public const string WriteCommandRetry = Prefix + nameof(WriteCommandRetry);
            public const string WriteCommandError = Prefix + nameof(WriteCommandError);
            
            public const string WriteConnectionOpenBefore = Prefix + nameof(WriteConnectionOpenBefore);
            public const string WriteConnectionOpenAfter = Prefix + nameof(WriteConnectionOpenAfter);
            public const string WriteConnectionOpenError = Prefix + nameof(WriteConnectionOpenError);
            
            public const string WriteConnectionCloseBefore = Prefix + nameof(WriteConnectionCloseBefore);
            public const string WriteConnectionCloseAfter = Prefix + nameof(WriteConnectionCloseAfter);
            public const string WriteConnectionCloseError = Prefix + nameof(WriteConnectionCloseError);
            
            public const string WritePoolRent = Prefix + nameof(WritePoolRent);
            public const string WritePoolReturn = Prefix + nameof(WritePoolReturn);
        }
    }
    
    public static class Activity
    {
        public const string HttpBegin = nameof(HttpBegin);
        public const string HttpEnd = nameof(HttpEnd);
        public const string OperationId = nameof(OperationId);
        public const string UserId = nameof(UserId);
        public const string Tag = nameof(Tag);
        public const string MqBegin = nameof(MqBegin);
        public const string MqEnd = nameof(MqEnd);
    }

    private DiagnosticListener servicestack { get; set; } = new(Listeners.ServiceStack);
    private DiagnosticListener client { get; set; } = new(Listeners.Client);
    private DiagnosticListener ormlite { get; set; } = new(Listeners.OrmLite);
    private DiagnosticListener redis { get; set; } = new(Listeners.Redis);

    public static DiagnosticListener ServiceStack => Instance.servicestack;
    public static DiagnosticListener Client => Instance.client;
    public static DiagnosticListener OrmLite => Instance.ormlite;
    public static DiagnosticListener Redis => Instance.redis;
    
    public static string? CreateStackTrace(Exception? e)
    {
        if (e?.StackTrace == null)
            return null;

        var sb = StringBuilderCache.Allocate();
        sb.AppendLine(e.StackTrace);
            
        var innerEx = e.InnerException;
        while (innerEx != null)
        {
            sb.AppendLine("");
            sb.AppendLine(innerEx.ToString());
            innerEx = innerEx.InnerException;
        }
        return StringBuilderCache.ReturnAndFree(sb);
    }
}

[Flags]
public enum ProfileSource
{
    None = 0,
    ServiceStack = 1 << 0,
    Client = 1 << 1,
    Redis = 1 << 2,
    OrmLite = 1 << 3,
    All = ServiceStack | Client | OrmLite | Redis,
}

public abstract class DiagnosticEvent
{
    public virtual string Source => GetType().Name.Replace(nameof(DiagnosticEvent), "");
    public string EventType { get; set; }
    public Guid OperationId { get; set; }
    public string Operation { get; set; }
    public string? TraceId { get; set; }
    public string? UserAuthId { get; set; }
    public Exception? Exception { get; set; }
    public long Timestamp { get; set; }
    public DateTime Date { get; set; }
    public object DiagnosticEntry { get; set; }
    public string? Tag { get; set; }
    public string? StackTrace { get; set; }
    public Guid? ClientOperationId { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

public class OrmLiteDiagnosticEvent : DiagnosticEvent
{
    public override string Source => "OrmLite";
    public Guid? ConnectionId { get; set; }
    public IDbConnection? Connection { get; set; }
    public IDbCommand? Command { get; set; }
    public IsolationLevel? IsolationLevel { get; set; }
    public string? TransactionName { get; set; }
}

public class RedisDiagnosticEvent : DiagnosticEvent
{
    public override string Source => "Redis";
    public byte[][]? Command { get; set; }
    /// <summary>
    /// RedisNativeClient instance, late-bound object to decouple 
    /// </summary>
    public object Client { get; set; }
    public Socket Socket { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
}

public static class DiagnosticsUtils
{
    public static Activity? GetRoot(Activity? activity)
    {
        if (activity == null)
            return null;
        while (activity.Parent != null)
        {
            activity = activity.Parent;
        }
        return activity;
    }

    public static string? GetTraceId(this Activity? activity) => GetRoot(activity)?.ParentId;
    public static string? GetUserId(this Activity? activity) => 
        GetRoot(activity)?.GetTagItem(Diagnostics.Activity.UserId) as string;
    public static string? GetTag(this Activity? activity) => 
        GetRoot(activity)?.GetTagItem(Diagnostics.Activity.Tag) as string;

    public static T Init<T>(this T evt, Activity? activity)
        where T : DiagnosticEvent
    {
        var rootActivity = GetRoot(activity);
        if (rootActivity != null)
        {
            evt.TraceId ??= rootActivity.GetTraceId();
            evt.UserAuthId ??= rootActivity.GetUserId();
            evt.Tag ??= rootActivity.GetTag();
        }
        evt.Timestamp = Stopwatch.GetTimestamp();
        evt.Date = DateTime.UtcNow;
        return evt;
    }
}