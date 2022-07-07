using System;
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace ServiceStack.Redis;

public static class RedisDiagnostics
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteCommandBefore(this DiagnosticListener listener, byte[][] command, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Redis.WriteCommandBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.Redis.WriteCommandBefore, new RedisDiagnosticEvent {
                EventType = Diagnostics.Events.Redis.WriteCommandBefore,
                OperationId = operationId,
                Operation = operation,
                Command = command,
            }.Init(Activity.Current));
            return operationId;
        }
        return Guid.Empty;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteCommandAfter(this DiagnosticListener listener, Guid operationId, byte[][] command, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Redis.WriteCommandAfter))
        {
            listener.Write(Diagnostics.Events.Redis.WriteCommandAfter, new RedisDiagnosticEvent {
                EventType = Diagnostics.Events.Redis.WriteCommandAfter,
                OperationId = operationId,
                Operation = operation,
                Command = command,
                Timestamp = Stopwatch.GetTimestamp()
            }.Init(Activity.Current));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteCommandRetry(this DiagnosticListener listener, Guid operationId, byte[][] command, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Redis.WriteCommandRetry))
        {
            listener.Write(Diagnostics.Events.Redis.WriteCommandRetry, new RedisDiagnosticEvent {
                EventType = Diagnostics.Events.Redis.WriteCommandRetry,
                OperationId = operationId,
                Operation = operation,
                Command = command,
            }.Init(Activity.Current));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteCommandError(this DiagnosticListener listener, Guid operationId, byte[][] command,
        Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Redis.WriteCommandError))
        {
            listener.Write(Diagnostics.Events.Redis.WriteCommandError, new RedisDiagnosticEvent {
                EventType = Diagnostics.Events.Redis.WriteCommandError,
                OperationId = operationId,
                Operation = operation,
                Command = command,
                Exception = ex,
            }.Init(Activity.Current));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteConnectionOpenBefore(this DiagnosticListener listener, RedisNativeClient client, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Redis.WriteConnectionOpenBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.Redis.WriteConnectionOpenBefore, new RedisDiagnosticEvent {
                EventType = Diagnostics.Events.Redis.WriteConnectionOpenBefore,
                OperationId = operationId,
                Operation = operation,
                Client = client,
                Socket = client.Socket,
                Host = client.Host,
                Port = client.Port,
            }.Init(Activity.Current));

            return operationId;
        }
        return Guid.Empty;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteConnectionOpenAfter(this DiagnosticListener listener, Guid operationId, RedisNativeClient client, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Redis.WriteConnectionOpenAfter))
        {
            listener.Write(Diagnostics.Events.Redis.WriteConnectionOpenAfter, new RedisDiagnosticEvent {
                EventType = Diagnostics.Events.Redis.WriteConnectionOpenAfter,
                OperationId = operationId,
                Operation = operation,
                Client = client,
                Socket = client.Socket,
                Host = client.Host,
                Port = client.Port,
            }.Init(Activity.Current));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteConnectionOpenError(this DiagnosticListener listener, Guid operationId, RedisNativeClient client, Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Redis.WriteConnectionOpenError))
        {
            listener.Write(Diagnostics.Events.Redis.WriteConnectionOpenError, new RedisDiagnosticEvent {
                EventType = Diagnostics.Events.Redis.WriteConnectionOpenError,
                OperationId = operationId,
                Operation = operation,
                Client = client,
                Socket = client.Socket,
                Host = client.Host,
                Port = client.Port,
                Exception = ex,
            }.Init(Activity.Current));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteConnectionCloseBefore(this DiagnosticListener listener, RedisNativeClient client, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Redis.WriteConnectionCloseBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.Redis.WriteConnectionCloseBefore, new RedisDiagnosticEvent {
                EventType = Diagnostics.Events.Redis.WriteConnectionCloseBefore,
                OperationId = operationId,
                Operation = operation,
                TraceId = Activity.Current.GetTraceId(),
                Client = client,
                Socket = client.Socket,
                Host = client.Host,
                Port = client.Port,
                Timestamp = Stopwatch.GetTimestamp()
            });

            return operationId;
        }
        return Guid.Empty;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteConnectionCloseAfter(this DiagnosticListener listener, Guid operationId, RedisNativeClient client, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Redis.WriteConnectionCloseAfter))
        {
            listener.Write(Diagnostics.Events.Redis.WriteConnectionCloseAfter, new RedisDiagnosticEvent {
                EventType = Diagnostics.Events.Redis.WriteConnectionCloseAfter,
                OperationId = operationId,
                Operation = operation,
                TraceId = Activity.Current.GetTraceId(),
                Client = client,
                Socket = client.Socket,
                Host = client.Host,
                Port = client.Port,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteConnectionCloseError(this DiagnosticListener listener, Guid operationId, RedisNativeClient client, Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Redis.WriteConnectionCloseError))
        {
            listener.Write(Diagnostics.Events.Redis.WriteConnectionCloseError, new RedisDiagnosticEvent {
                EventType = Diagnostics.Events.Redis.WriteConnectionCloseError,
                OperationId = operationId,
                Operation = operation,
                TraceId = Activity.Current.GetTraceId(),
                Client = client,
                Socket = client.Socket,
                Host = client.Host,
                Port = client.Port,
                Exception = ex,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WritePoolRent(this DiagnosticListener listener, RedisNativeClient client, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Redis.WritePoolRent))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.Redis.WritePoolRent, new RedisDiagnosticEvent {
                EventType = Diagnostics.Events.Redis.WritePoolRent,
                OperationId = operationId,
                Operation = operation,
                TraceId = Activity.Current.GetTraceId(),
                Client = client,
                Socket = client.Socket,
                Host = client.Host,
                Port = client.Port,
                Timestamp = Stopwatch.GetTimestamp()
            });

            return operationId;
        }
        return Guid.Empty;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WritePoolReturn(this DiagnosticListener listener, Guid operationId, RedisNativeClient client, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.Redis.WritePoolReturn))
        {
            listener.Write(Diagnostics.Events.Redis.WritePoolReturn, new RedisDiagnosticEvent {
                EventType = Diagnostics.Events.Redis.WritePoolReturn,
                OperationId = operationId,
                Operation = operation,
                TraceId = Activity.Current.GetTraceId(),
                Client = client,
                Socket = client.Socket,
                Host = client.Host,
                Port = client.Port,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }
}