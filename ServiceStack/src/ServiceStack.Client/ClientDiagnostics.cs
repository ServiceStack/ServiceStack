#if NET6_0_OR_GREATER

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using ServiceStack.Web;

namespace ServiceStack;

public class HttpClientDiagnosticEvent : DiagnosticEvent
{
    public override string Source => "Client";
    public HttpRequestMessage HttpRequest { get; set; }
    public object Request { get; set; }
    public HttpResponseMessage HttpResponse { get; set; }
    public object Response { get; set; }
    public Type ResponseType { get; set; }
}


public static class ClientDiagnostics
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteRequestBefore(this DiagnosticListener listener, HttpRequestMessage httpReq, object request, Type responseType, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Client.WriteRequestBefore))
        {
            var operationId = Guid.NewGuid();
            httpReq.Options.Set(Diagnostics.Keys.HttpRequestOperationId, operationId);
            httpReq.Options.Set(Diagnostics.Keys.HttpRequestRequest, request);
            httpReq.Options.Set(Diagnostics.Keys.HttpRequestResponseType, responseType);
            listener.Write(Diagnostics.Events.Client.WriteRequestBefore, new HttpClientDiagnosticEvent {
                EventType = Diagnostics.Events.Client.WriteRequestBefore,
                OperationId = operationId,
                Operation = operation,
                HttpRequest = httpReq,
                Request = request,
                ResponseType = responseType,
            }.Init(Activity.Current));
            return operationId;
        }
        return Guid.Empty;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteRequestAfter(this DiagnosticListener listener, Guid operationId, HttpRequestMessage httpReq,
        object response, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Client.WriteRequestAfter))
        {
            listener.Write(Diagnostics.Events.Client.WriteRequestAfter, new HttpClientDiagnosticEvent {
                EventType = Diagnostics.Events.Client.WriteRequestAfter,
                OperationId = operationId,
                Operation = operation,
                HttpRequest = httpReq,
                Response = response,
            }.Init(Activity.Current));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteRequestError(this DiagnosticListener listener, Guid operationId, HttpRequestMessage httpReq,
        Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Client.WriteRequestError))
        {
            listener.Write(Diagnostics.Events.Client.WriteRequestError, new HttpClientDiagnosticEvent {
                EventType = Diagnostics.Events.Client.WriteRequestError,
                OperationId = operationId,
                Operation = operation,
                HttpRequest = httpReq,
                Exception = ex,
                StackTrace = ex?.StackTrace ?? (Diagnostics.IncludeStackTrace ? Environment.StackTrace : null),
            }.Init(Activity.Current));
        }
    }
}

#endif