#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ServiceStack;

public class Diagnostics
{
    private static readonly Diagnostics Instance = new();
    private Diagnostics(){}
    
    public static class Listeners
    {
        public const string ServiceStack = "ServiceStack";
        public const string OrmLite = "ServiceStack.OrmLite";
        public const string Redis = "ServiceStack.Redis";
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

            public const string WriteTransactionCommitBefore = Prefix + nameof(WriteTransactionCommitBefore);
            public const string WriteTransactionCommitAfter = Prefix + nameof(WriteTransactionCommitAfter);
            public const string WriteTransactionCommitError = Prefix + nameof(WriteTransactionCommitError);

            public const string WriteTransactionRollbackBefore = Prefix + nameof(WriteTransactionRollbackBefore);
            public const string WriteTransactionRollbackAfter = Prefix + nameof(WriteTransactionRollbackAfter);
            public const string WriteTransactionRollbackError = Prefix + nameof(WriteTransactionRollbackError);
        }
    }
    
    public static class Activity
    {
        public const string HttpBegin = nameof(HttpBegin);
        public const string HttpEnd = nameof(HttpEnd);
        public const string OperationId = nameof(OperationId);
    }

    private DiagnosticListener servicestack { get; set; } = new(Listeners.ServiceStack);
    private DiagnosticListener ormlite { get; set; } = new(Listeners.OrmLite);
    private DiagnosticListener redis { get; set; } = new(Listeners.Redis);

    public static DiagnosticListener ServiceStack => Instance.servicestack;
    public static DiagnosticListener OrmLite => Instance.ormlite;
    public static DiagnosticListener Redis => Instance.redis;
}

public abstract class DiagnosticEvent
{
    public Guid OperationId { get; set; }
    public string Operation { get; set; }
    public string? TraceId { get; set; }
    public Exception Exception { get; set; }
    public long Timestamp { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

public static class DiagnosticsUtils
{
    public static string? GetTraceId(this Activity? activity)
    {
        if (activity == null)
            return null;
        while (activity.Parent != null)
        {
            activity = activity.Parent;
        }
        return activity.ParentId;
    }
}