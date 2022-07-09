using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ServiceStack.Web;

namespace ServiceStack;

public class RequestDiagnosticEvent : DiagnosticEvent
{
    public override string Source => "ServiceStack";
    public string TraceId { get; set; }
    public IRequest Request { get; set; }
}

internal static class ServiceStackDiagnostics
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteRequestBefore(this DiagnosticListener listener, IRequest req, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.ServiceStack.WriteRequestBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.ServiceStack.WriteRequestBefore, new RequestDiagnosticEvent {
                EventType = Diagnostics.Events.ServiceStack.WriteRequestBefore,
                OperationId = operationId,
                Operation = operation,
            }.Init(req));
            return operationId;
        }
        return Guid.Empty;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteRequestAfter(this DiagnosticListener listener, Guid operationId, IRequest req, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.ServiceStack.WriteRequestAfter))
        {
            listener.Write(Diagnostics.Events.ServiceStack.WriteRequestAfter, new RequestDiagnosticEvent {
                EventType = Diagnostics.Events.ServiceStack.WriteRequestAfter,
                OperationId = operationId,
                Operation = operation,
            }.Init(req));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteRequestError(this DiagnosticListener listener, Guid operationId, IRequest req,
        Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.ServiceStack.WriteRequestError))
        {
            listener.Write(Diagnostics.Events.ServiceStack.WriteRequestError, new RequestDiagnosticEvent {
                EventType = Diagnostics.Events.ServiceStack.WriteRequestError,
                OperationId = operationId,
                Operation = operation,
                Exception = ex,
            }.Init(req));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteGatewayBefore(this DiagnosticListener listener, IRequest req, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.ServiceStack.WriteGatewayBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.ServiceStack.WriteGatewayBefore, new RequestDiagnosticEvent {
                EventType = Diagnostics.Events.ServiceStack.WriteGatewayBefore,
                OperationId = operationId,
                Operation = operation,
                StackTrace = Diagnostics.IncludeStackTrace ? Environment.StackTrace : null,
            }.Init(req));
            return operationId;
        }
        return Guid.Empty;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteGatewayAfter(this DiagnosticListener listener, Guid operationId, IRequest req, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.ServiceStack.WriteGatewayAfter))
        {
            listener.Write(Diagnostics.Events.ServiceStack.WriteGatewayAfter, new RequestDiagnosticEvent {
                EventType = Diagnostics.Events.ServiceStack.WriteGatewayAfter,
                OperationId = operationId,
                Operation = operation,
            }.Init(req));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteGatewayError(this DiagnosticListener listener, Guid operationId, IRequest req,
        Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.ServiceStack.WriteGatewayError))
        {
            listener.Write(Diagnostics.Events.ServiceStack.WriteGatewayError, new RequestDiagnosticEvent {
                EventType = Diagnostics.Events.ServiceStack.WriteGatewayError,
                OperationId = operationId,
                Operation = operation,
                Exception = ex,
            }.Init(req));
        }
    }
}

public static class ServiceStackDiagnosticsUtils
{
    public static RequestDiagnosticEvent Init(this RequestDiagnosticEvent evt, IRequest req)
    {
        if (req != null)
        {
            var appHost = HostContext.AppHost;
            evt.Request = req;
            evt.TraceId ??= req.GetTraceId();
            evt.UserAuthId ??= appHost.TryGetUserId(req);

            if (evt.Tag == null)
            {
                var feature = appHost.AssertPlugin<ProfilingFeature>();
                evt.Tag = feature.TagResolver?.Invoke(req);
            }
        }
        evt.Timestamp = Stopwatch.GetTimestamp();
        return evt;
    }
}

public class ServiceStackActivityArgs
{
    public IRequest Request { get; set; }
    public Activity Activity { get; set; }
}