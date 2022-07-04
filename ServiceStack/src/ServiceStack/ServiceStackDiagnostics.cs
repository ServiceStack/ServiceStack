using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ServiceStack.Web;

namespace ServiceStack;

public class RequestDiagnosticEvent : DiagnosticEvent
{
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
                OperationId = operationId,
                Operation = operation,
                TraceId = req.GetTraceId(),
                Request = req,
                Timestamp = Stopwatch.GetTimestamp()
            });
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
                OperationId = operationId,
                Operation = operation,
                TraceId = req.GetTraceId(),
                Request = req,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteRequestError(this DiagnosticListener listener, Guid operationId, IRequest req,
        Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.ServiceStack.WriteRequestError))
        {
            listener.Write(Diagnostics.Events.ServiceStack.WriteRequestError, new RequestDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                TraceId = req.GetTraceId(),
                Request = req,
                Exception = ex,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteGatewayBefore(this DiagnosticListener listener, IRequest req, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.ServiceStack.WriteGatewayBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.ServiceStack.WriteGatewayBefore, new RequestDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                TraceId = req.GetTraceId(),
                Request = req,
                Timestamp = Stopwatch.GetTimestamp()
            });
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
                OperationId = operationId,
                Operation = operation,
                TraceId = req.GetTraceId(),
                Request = req,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteGatewayError(this DiagnosticListener listener, Guid operationId, IRequest req,
        Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.ServiceStack.WriteGatewayError))
        {
            listener.Write(Diagnostics.Events.ServiceStack.WriteGatewayError, new RequestDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                TraceId = req.GetTraceId(),
                Request = req,
                Exception = ex,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }
    
}

public class ServiceStackActivityArgs
{
    public IRequest Request { get; set; }
    public Activity Activity { get; set; }
}