using System;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ServiceStack.OrmLite;

internal static class OrmLiteDiagnostics
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteCommandBefore(this DiagnosticListener listener, IDbCommand dbCmd, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteCommandBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.OrmLite.WriteCommandBefore, new OrmLiteDiagnosticEvent {
                EventType = Diagnostics.Events.OrmLite.WriteCommandBefore,
                OperationId = operationId,
                Operation = operation,
                Command = dbCmd,
                ConnectionId = dbCmd.GetConnectionId(),
                Tag = dbCmd.GetTag(),
            }.Init(Activity.Current));
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
                EventType = Diagnostics.Events.OrmLite.WriteCommandAfter,
                OperationId = operationId,
                Operation = operation,
                Command = dbCmd,
                ConnectionId = dbCmd.GetConnectionId(),
                Tag = dbCmd.GetTag(),
            }.Init(Activity.Current));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteCommandError(this DiagnosticListener listener, Guid operationId, IDbCommand dbCmd,
        Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteCommandError))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteCommandError, new OrmLiteDiagnosticEvent {
                EventType = Diagnostics.Events.OrmLite.WriteCommandError,
                OperationId = operationId,
                Operation = operation,
                Command = dbCmd,
                Exception = ex,
                ConnectionId = dbCmd.GetConnectionId(),
                Tag = dbCmd.GetTag(),
            }.Init(Activity.Current));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteConnectionOpenBefore(this DiagnosticListener listener, IDbConnection dbConn, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteConnectionOpenBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.OrmLite.WriteConnectionOpenBefore, new OrmLiteDiagnosticEvent {
                EventType = Diagnostics.Events.OrmLite.WriteConnectionOpenBefore,
                OperationId = operationId,
                Operation = operation,
                Connection = dbConn,
                ConnectionId = dbConn.GetConnectionId(),
                Tag = dbConn.GetTag(),
                StackTrace = Diagnostics.IncludeStackTrace ? Environment.StackTrace : null,
            }.Init(Activity.Current));

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
                EventType = Diagnostics.Events.OrmLite.WriteConnectionOpenAfter,
                OperationId = operationId,
                Operation = operation,
                Connection = dbConn,
                ConnectionId = dbConn.GetConnectionId(),
                Tag = dbConn.GetTag(),
            }.Init(Activity.Current));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteConnectionOpenError(this DiagnosticListener listener, Guid operationId, IDbConnection dbConn,
        Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteConnectionOpenError))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteConnectionOpenError, new OrmLiteDiagnosticEvent {
                EventType = Diagnostics.Events.OrmLite.WriteConnectionOpenError,
                OperationId = operationId,
                Operation = operation,
                Connection = dbConn,
                Exception = ex,
                ConnectionId = dbConn.GetConnectionId(),
                Tag = dbConn.GetTag(),
            }.Init(Activity.Current));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteConnectionCloseBefore(this DiagnosticListener listener, IDbConnection dbConn, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteConnectionCloseBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.OrmLite.WriteConnectionCloseBefore, new OrmLiteDiagnosticEvent {
                EventType = Diagnostics.Events.OrmLite.WriteConnectionCloseBefore,
                OperationId = operationId,
                Operation = operation,
                Connection = dbConn,
                ConnectionId = dbConn.GetConnectionId(),
                Tag = dbConn.GetTag(),
            }.Init(Activity.Current));
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
                EventType = Diagnostics.Events.OrmLite.WriteConnectionCloseAfter,
                OperationId = operationId,
                Operation = operation,
                Connection = dbConn,
                ConnectionId = clientConnectionId,
                Tag = dbConn.GetTag(),
            }.Init(Activity.Current));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteConnectionCloseError(this DiagnosticListener listener, Guid operationId,
        Guid clientConnectionId, IDbConnection dbConn, Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteConnectionCloseError))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteConnectionCloseError, new OrmLiteDiagnosticEvent {
                EventType = Diagnostics.Events.OrmLite.WriteConnectionCloseError,
                OperationId = operationId,
                Operation = operation,
                Connection = dbConn,
                Exception = ex,
                ConnectionId = clientConnectionId,
                Tag = dbConn.GetTag(),
            }.Init(Activity.Current));
        }
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteTransactionOpen(this DiagnosticListener listener, IsolationLevel isolationLevel,
        IDbConnection dbConn, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteTransactionOpen))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.OrmLite.WriteTransactionOpen, new OrmLiteDiagnosticEvent {
                EventType = Diagnostics.Events.OrmLite.WriteTransactionOpen,
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                ConnectionId = dbConn.GetConnectionId(),
                Tag = dbConn.GetTag(),
            }.Init(Activity.Current));
            return operationId;
        }
        return Guid.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid WriteTransactionCommitBefore(this DiagnosticListener listener, IsolationLevel isolationLevel,
        IDbConnection dbConn, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteTransactionCommitBefore))
        {
            var operationId = Guid.NewGuid();
            listener.Write(Diagnostics.Events.OrmLite.WriteTransactionCommitBefore, new OrmLiteDiagnosticEvent {
                EventType = Diagnostics.Events.OrmLite.WriteTransactionCommitBefore,
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                ConnectionId = dbConn.GetConnectionId(),
                Tag = dbConn.GetTag(),
            }.Init(Activity.Current));
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
                EventType = Diagnostics.Events.OrmLite.WriteTransactionCommitAfter,
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                ConnectionId = dbConn.GetConnectionId(),
                Tag = dbConn.GetTag(),
            }.Init(Activity.Current));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTransactionCommitError(this DiagnosticListener listener, Guid operationId,
        IsolationLevel isolationLevel, IDbConnection dbConn, Exception ex, [CallerMemberName] string operation = "")
    {
        if (listener.IsEnabled(Diagnostics.Events.OrmLite.WriteTransactionCommitError))
        {
            listener.Write(Diagnostics.Events.OrmLite.WriteTransactionCommitError, new OrmLiteDiagnosticEvent {
                EventType = Diagnostics.Events.OrmLite.WriteTransactionCommitError,
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                Exception = ex,
                ConnectionId = dbConn.GetConnectionId(),
                Tag = dbConn.GetTag(),
            }.Init(Activity.Current));
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
                EventType = Diagnostics.Events.OrmLite.WriteTransactionRollbackBefore,
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                TransactionName = transactionName,
                ConnectionId = dbConn.GetConnectionId(),
                Tag = dbConn.GetTag(),
                StackTrace = Diagnostics.IncludeStackTrace ? Environment.StackTrace : null,
            }.Init(Activity.Current));
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
                EventType = Diagnostics.Events.OrmLite.WriteTransactionRollbackAfter,
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                TransactionName = transactionName,
                ConnectionId = dbConn.GetConnectionId(),
                Tag = dbConn.GetTag(),
            }.Init(Activity.Current));
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
                EventType = Diagnostics.Events.OrmLite.WriteTransactionRollbackError,
                OperationId = operationId,
                Operation = operation,
                IsolationLevel = isolationLevel,
                Connection = dbConn,
                TransactionName = transactionName,
                ConnectionId = dbConn.GetConnectionId(),
                Tag = dbConn.GetTag(),
                Exception = ex,
            }.Init(Activity.Current));
        }
    }
    
}

