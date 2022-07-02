#nullable enable

using System.Diagnostics;

namespace ServiceStack;

public class Diagnostics
{
    private static readonly Diagnostics Instance = new();
    private Diagnostics(){}
    
    public static class Listeners
    {
        public const string ServiceStack = nameof(ServiceStack);
        public const string OrmLite = "ServiceStack.OrmLite";
        public const string Redis = "ServiceStack.Redis";
    }
    
    public static class Events
    {
        public static class OrmLite
        {
            private const string Prefix = "ServiceStack.OrmLite.";
            
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

    private DiagnosticListener servicestack { get; set; } = new(Listeners.ServiceStack);
    private DiagnosticListener ormlite { get; set; } = new(Listeners.OrmLite);
    private DiagnosticListener redis { get; set; } = new(Listeners.Redis);

    public static DiagnosticListener ServiceStack => Instance.servicestack;
    public static DiagnosticListener OrmLite => Instance.ormlite;
    public static DiagnosticListener Redis => Instance.redis;
}
