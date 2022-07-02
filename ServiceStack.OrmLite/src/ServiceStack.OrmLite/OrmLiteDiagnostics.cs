using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ServiceStack.OrmLite;

public class OrmLiteDiagnosticEvent : IMeta
{
    public Guid OperationId { get; internal set; }
    public string Operation { get; internal set; }
    public Guid ConnectionId { get; internal set; }
    public IDbConnection Connection { get; internal set; }
    public IDbCommand Command { get; internal set; }
    public IsolationLevel IsolationLevel { get; internal set; }
    public string TransactionName { get; internal set; }
    public Exception Exception { get; internal set; }
    public long Timestamp { get; internal set; }
    public Dictionary<string, string> Meta { get; set; }
}

internal static class OrmLiteDiagnostics
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteCommandBefore(this DiagnosticListener listener, IDbCommand dbCmd, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteCommandBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.OrmLite.WriteCommandBefore, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                ConnectionId = dbCmd.GetConnectionId(),
                Command = dbCmd,
                Timestamp = Stopwatch.GetTimestamp()
            });
            return operationId;
        }
        return Guid.Empty;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteCommandAfter(this DiagnosticListener listener, Guid operationId, IDbCommand dbCmd, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteCommandAfter))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteCommandAfter, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                ConnectionId = dbCmd.GetConnectionId(),
                Command = dbCmd,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteCommandError(this DiagnosticListener listener, Guid operationId, IDbCommand dbCmd,
        Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteCommandError))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteCommandError, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                ConnectionId = dbCmd.GetConnectionId(),
                Command = dbCmd,
                Exception = ex,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteConnectionOpenBefore(this DiagnosticListener listener, IDbConnection dbConn, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteConnectionOpenBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.OrmLite.WriteConnectionOpenBefore, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                Connection = dbConn,
                Timestamp = Stopwatch.GetTimestamp()
            });

            return operationId;
        }
        return Guid.Empty;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteConnectionOpenAfter(this DiagnosticListener listener, Guid operationId, IDbConnection dbConn, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteConnectionOpenAfter))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteConnectionOpenAfter, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                ConnectionId = dbConn.GetConnectionId(),
                Connection = dbConn,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteConnectionOpenError(this DiagnosticListener listener, Guid operationId, IDbConnection dbConn,
        Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteConnectionOpenError))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteConnectionOpenError, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                ConnectionId = dbConn.GetConnectionId(),
                Connection = dbConn,
                Exception = ex,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteConnectionCloseBefore(this DiagnosticListener listener, IDbConnection dbConn, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteConnectionCloseBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.OrmLite.WriteConnectionCloseBefore, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                ConnectionId = dbConn.GetConnectionId(),
                Connection = dbConn,
                Timestamp = Stopwatch.GetTimestamp()
            });
            return operationId;
        }
        return Guid.Empty;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteConnectionCloseAfter(this DiagnosticListener listener, Guid operationId,
        Guid clientConnectionId, IDbConnection dbConn, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteConnectionCloseAfter))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteConnectionCloseAfter, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                ConnectionId = clientConnectionId,
                Connection = dbConn,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteConnectionCloseError(this DiagnosticListener listener, Guid operationId,
        Guid clientConnectionId, IDbConnection dbConn, Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteConnectionCloseError))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteConnectionCloseError, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                ConnectionId = clientConnectionId,
                Connection = dbConn,
                Exception = ex,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteTransactionCommitBefore(this DiagnosticListener listener, IsolationLevel isolationLevel,
        IDbConnection dbConn, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteTransactionCommitBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.OrmLite.WriteTransactionCommitBefore, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                Timestamp = Stopwatch.GetTimestamp()
            });
            return operationId;
        }
        return Guid.Empty;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTransactionCommitAfter(this DiagnosticListener listener, Guid operationId,
        IsolationLevel isolationLevel, IDbConnection dbConn, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteTransactionCommitAfter))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteTransactionCommitAfter, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTransactionCommitError(this DiagnosticListener listener, Guid operationId,
        IsolationLevel isolationLevel, IDbConnection dbConn, Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteTransactionCommitError))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteTransactionCommitError, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                Exception = ex,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteTransactionRollbackBefore(this DiagnosticListener listener, IsolationLevel isolationLevel,
        IDbConnection dbConn, string transactionName, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteTransactionRollbackBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.OrmLite.WriteTransactionRollbackBefore, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                TransactionName = transactionName,
                Timestamp = Stopwatch.GetTimestamp()
            });
            return operationId;
        }
        return Guid.Empty;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTransactionRollbackAfter(this DiagnosticListener listener, Guid operationId,
        IsolationLevel isolationLevel, IDbConnection dbConn, string transactionName,
        [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteTransactionRollbackAfter))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteTransactionRollbackAfter, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                TransactionName = transactionName,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTransactionRollbackError(this DiagnosticListener listener, Guid operationId,
        IsolationLevel isolationLevel, IDbConnection dbConn, string transactionName, Exception ex,
        [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteTransactionRollbackError))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteTransactionRollbackError, new OrmLiteDiagnosticEvent {
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                TransactionName = transactionName,
                Exception = ex,
                Timestamp = Stopwatch.GetTimestamp()
            });
        }
    }
    
}

