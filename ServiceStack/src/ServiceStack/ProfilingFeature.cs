#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;

namespace ServiceStack;


public class AdminProfiling
{
}
public class AdminProfilingResponse
{
    public List<DiagnosticEntry> Results { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[DefaultRequest(typeof(AdminProfiling))]
[Restrict(VisibilityTo = RequestAttributes.Localhost)]
public class AdminProfilingService : Service
{
    public async Task<object> Any(AdminProfiling request)
    {
        var feature = HostContext.AppHost.AssertPlugin<ProfilingFeature>();
        if (!HostContext.DebugMode)
            await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AccessRole);

        var snapshot = feature.Observer.GetLatestEntries(null);
        var logs = snapshot.AsQueryable();

        logs = logs.Take(feature.DefaultLimit);
        
        return new AdminProfilingResponse
        {
            Results = logs.ToList(),
        };
    }
}

public class ProfilingFeature : IPlugin, Model.IHasStringId, IPreInitPlugin
{
    public string Id => Plugins.Profiling;
    public const int DefaultCapacity = 10000;

    public string AccessRole { get; set; } = RoleNames.Admin;
    
    /// <summary>
    /// Which features to Profile, default all
    /// </summary>
    public ProfileSource Profile { get; set; } = ProfileSource.All;

    /// <summary>
    /// Size of circular buffer of profiled events
    /// </summary>
    public int Capacity { get; set; } = DefaultCapacity;
    
    /// <summary>
    /// Default take, if none is specified
    /// </summary>
    public int DefaultLimit { get; set; } = 100;

    public ProfilerDiagnosticObserver Observer { get; set; }
    
    public void Register(IAppHost appHost)
    {
        appHost.RegisterService(typeof(AdminProfilingService));
        
        Observer = new ProfilerDiagnosticObserver(this);
        var subscription = DiagnosticListener.AllListeners.Subscribe(Observer);
        appHost.OnDisposeCallbacks.Add(host => subscription.Dispose());
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<UiFeature>(feature =>
        {
            feature.AddAdminLink(AdminUi.Logging, new LinkInfo {
                Id = "profiling",
                Label = "Profiling",
                Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Profiling)),
                Show = $"role:{AccessRole}",
            });
        });
    }
}

public sealed class ProfilerDiagnosticObserver : 
    IObserver<DiagnosticListener>, 
    IObserver<KeyValuePair<string, object>>
{
    public static int AnalyzeCommandLength { get; set; } = 100;
    
    private readonly ProfilingFeature feature;
    private readonly int capacity;
    public ProfilerDiagnosticObserver(ProfilingFeature feature)
    {
        this.feature = feature;
        this.capacity = feature.Capacity;
    }

    protected readonly ConcurrentQueue<DiagnosticEntry> entries = new();
    private long idCounter = 0;
    
    private readonly List<IDisposable> subscriptions = new();

    void IObserver<DiagnosticListener>.OnNext(DiagnosticListener diagnosticListener)
    {
        Console.WriteLine($@"diagnosticListener: {diagnosticListener.Name}");
        if ((feature.Profile.HasFlag(ProfileSource.ServiceStack) && diagnosticListener.Name is Diagnostics.Listeners.ServiceStack)
            || (feature.Profile.HasFlag(ProfileSource.OrmLite) && diagnosticListener.Name is Diagnostics.Listeners.OrmLite)
            || (feature.Profile.HasFlag(ProfileSource.Redis) && diagnosticListener.Name is Diagnostics.Listeners.Redis))
        {
            var subscription = diagnosticListener.Subscribe(this);
            subscriptions.Add(subscription);
        }
    }

    private ConcurrentDictionary<Guid, object> refs = new();

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }
    
    public List<DiagnosticEntry> GetLatestEntries(int? take)
    {
        return take.HasValue
            ? new List<DiagnosticEntry>(entries.Take(take.Value))
            : new List<DiagnosticEntry>(entries);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Add(DiagnosticEntry entry)
    {
        entries.Enqueue(entry);
        if (entries.Count > capacity)
            entries.TryDequeue(out _);
    }

    public DiagnosticEntry CreateDiagnosticEntry(DiagnosticEvent e, DiagnosticEvent? orig = null)
    {
        var to = new DiagnosticEntry
        {
            Id = Interlocked.Increment(ref idCounter),
            Source = e.Source,
            EventType = e.EventType,
            Operation = e.Operation,
            TraceId = e.TraceId,
            Timestamp = e.Timestamp,
        };

        if (e.Exception != null)
        {
            to.Error = DtoUtils.CreateResponseStatus(e.Exception, request:null, debugMode:true);
        }

        if (orig != null)
        {
            to.Duration = TimeSpan.FromTicks(e.Timestamp - orig.Timestamp);
            //$"{after.Operation} {after.Command.CommandText}".Print();
            Console.WriteLine($@"Took: {(e.Timestamp - orig.Timestamp) / (double)Stopwatch.Frequency}s");
        }

        return to;
    }

    public DiagnosticEntry ToDiagnosticEntry(RequestDiagnosticEvent e, RequestDiagnosticEvent? orig = null)
    {
        var to = CreateDiagnosticEntry(e, orig);

        to.Command = e.Request.PathInfo;
        to.NamedArgs = orig == null 
            ? e.Request.Dto.ToObjectDictionary() 
            : e.Request.Response.Dto.ToObjectDictionary();

        return to;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddServiceStack(RequestDiagnosticEvent before)
    {
        refs[before.OperationId] = before;
        Add(ToDiagnosticEntry(before));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddServiceStack(RequestDiagnosticEvent before, RequestDiagnosticEvent after)
    {
        Add(ToDiagnosticEntry(before, after));
    }

    public DiagnosticEntry ToDiagnosticEntry(OrmLiteDiagnosticEvent e, OrmLiteDiagnosticEvent? orig = null)
    {
        var to = CreateDiagnosticEntry(e, orig);
        if (e.Command != null)
        {
            to.Command = e.Command.CommandText;
            to.NamedArgs = new();
            foreach (IDbDataParameter p in e.Command.Parameters)
            {
                to.NamedArgs[p.ParameterName] = p.Value;
            }
        }
        
        return to;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddOrmLite(OrmLiteDiagnosticEvent before)
    {
        refs[before.OperationId] = before;
        Add(ToDiagnosticEntry(before));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddOrmLite(OrmLiteDiagnosticEvent before, OrmLiteDiagnosticEvent after)
    {
        Add(ToDiagnosticEntry(before, after));
    }

    public DiagnosticEntry ToDiagnosticEntry(RedisDiagnosticEvent e, RedisDiagnosticEvent? orig = null)
    {
        var to = CreateDiagnosticEntry(e, orig);

        if (e.Command != null)
        {
            to.Args = e.Command.TakeWhile(bytes => bytes.Length <= AnalyzeCommandLength)
                .Map(bytes => {
                    try
                    {
                        return bytes.FromUtf8Bytes();
                    }
                    catch
                    {
                        return bytes.ToBase64UrlSafe();
                    }
                });
        
            to.ArgLengths = e.Command.Map(x => x.LongLength);
            to.Command = string.Join(" ", to.Args);
            to.Key = to.Args.Count > 1 ? to.Args[0] : null;
        }

        return to;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddRedis(RedisDiagnosticEvent before)
    {
        refs[before.OperationId] = before;
        Add(ToDiagnosticEntry(before));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddRedis(RedisDiagnosticEvent before, RedisDiagnosticEvent after)
    {
        Add(ToDiagnosticEntry(before, after));
    }

    public void OnNext(KeyValuePair<string, object> kvp)
    {
        Console.WriteLine(kvp.Key);
        Console.WriteLine(kvp.Value);

        /** ServiceStack */
        if (kvp.Key == Diagnostics.Events.ServiceStack.WriteRequestBefore && kvp.Value is RequestDiagnosticEvent reqBefore)
        {
            AddServiceStack(reqBefore);
        }
        if (kvp.Key == Diagnostics.Events.ServiceStack.WriteRequestAfter && kvp.Value is RequestDiagnosticEvent reqAfter)
        {
            if (refs.TryRemove(reqAfter.OperationId, out var orig) && orig is RequestDiagnosticEvent reqOrig)
                AddServiceStack(reqOrig, reqAfter);
        }
        if (kvp.Key == Diagnostics.Events.ServiceStack.WriteRequestError && kvp.Value is RequestDiagnosticEvent reqError)
        {
            if (refs.TryRemove(reqError.OperationId, out var orig) && orig is RequestDiagnosticEvent reqOrig)
                AddServiceStack(reqOrig, reqError);
        }

        if (kvp.Key == Diagnostics.Events.ServiceStack.WriteGatewayBefore && kvp.Value is RequestDiagnosticEvent gatewayBefore)
        {
            AddServiceStack(gatewayBefore);
        }
        if (kvp.Key == Diagnostics.Events.ServiceStack.WriteGatewayAfter && kvp.Value is RequestDiagnosticEvent gatewayAfter)
        {
            if (refs.TryRemove(gatewayAfter.OperationId, out var orig) && orig is RequestDiagnosticEvent reqOrig)
                AddServiceStack(reqOrig, gatewayAfter);
        }
        if (kvp.Key == Diagnostics.Events.ServiceStack.WriteGatewayError && kvp.Value is RequestDiagnosticEvent gatewayError)
        {
            if (refs.TryRemove(gatewayError.OperationId, out var orig) && orig is RequestDiagnosticEvent reqOrig)
                AddServiceStack(reqOrig, gatewayError);
        }
        
        
        /** OrmLite */
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteCommandBefore && kvp.Value is OrmLiteDiagnosticEvent dbBefore)
        {
            AddOrmLite(dbBefore);
        }
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteCommandAfter && kvp.Value is OrmLiteDiagnosticEvent dbAfter)
        {
            if (refs.TryRemove(dbAfter.OperationId, out var orig) && orig is OrmLiteDiagnosticEvent dbOrig)
                AddOrmLite(dbOrig, dbAfter);
        }
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteCommandError && kvp.Value is OrmLiteDiagnosticEvent dbError)
        {
            if (refs.TryRemove(dbError.OperationId, out var orig) && orig is OrmLiteDiagnosticEvent dbOrig)
                AddOrmLite(dbOrig, dbError);
        }
        
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteConnectionOpenBefore && kvp.Value is OrmLiteDiagnosticEvent dbOpenBefore)
        {
            AddOrmLite(dbOpenBefore);
        }
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteConnectionOpenAfter && kvp.Value is OrmLiteDiagnosticEvent dbOpenAfter)
        {
            if (refs.TryRemove(dbOpenAfter.OperationId, out var orig) && orig is OrmLiteDiagnosticEvent dbOrig)
                AddOrmLite(dbOrig, dbOpenAfter);
        }
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteConnectionOpenError && kvp.Value is OrmLiteDiagnosticEvent dbOpenError)
        {
            if (refs.TryRemove(dbOpenError.OperationId, out var orig) && orig is OrmLiteDiagnosticEvent dbOrig)
                AddOrmLite(dbOrig, dbOpenError);
        }
        
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteConnectionCloseBefore && kvp.Value is OrmLiteDiagnosticEvent dbCloseBefore)
        {
            AddOrmLite(dbCloseBefore);
        }
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteConnectionCloseAfter && kvp.Value is OrmLiteDiagnosticEvent dbCloseAfter)
        {
            if (refs.TryRemove(dbCloseAfter.OperationId, out var orig) && orig is OrmLiteDiagnosticEvent dbOrig)
                AddOrmLite(dbOrig, dbCloseAfter);
        }
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteConnectionCloseError && kvp.Value is OrmLiteDiagnosticEvent dbCloseError)
        {
            if (refs.TryRemove(dbCloseError.OperationId, out var orig) && orig is OrmLiteDiagnosticEvent dbOrig)
                AddOrmLite(dbOrig, dbCloseError);
        }
        
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteTransactionCommitBefore && kvp.Value is OrmLiteDiagnosticEvent commitBefore)
        {
            AddOrmLite(commitBefore);
        }
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteTransactionCommitAfter && kvp.Value is OrmLiteDiagnosticEvent commitAfter)
        {
            if (refs.TryRemove(commitAfter.OperationId, out var orig) && orig is OrmLiteDiagnosticEvent dbOrig)
                AddOrmLite(dbOrig, commitAfter);
        }
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteTransactionCommitError && kvp.Value is OrmLiteDiagnosticEvent commitError)
        {
            if (refs.TryRemove(commitError.OperationId, out var orig) && orig is OrmLiteDiagnosticEvent dbOrig)
                AddOrmLite(dbOrig, commitError);
        }
        
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteTransactionRollbackBefore && kvp.Value is OrmLiteDiagnosticEvent rollbackBefore)
        {
            AddOrmLite(rollbackBefore);
        }
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteTransactionRollbackAfter && kvp.Value is OrmLiteDiagnosticEvent rollbackAfter)
        {
            if (refs.TryRemove(rollbackAfter.OperationId, out var orig) && orig is OrmLiteDiagnosticEvent dbOrig)
                AddOrmLite(dbOrig, rollbackAfter);
        }
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteTransactionRollbackError && kvp.Value is OrmLiteDiagnosticEvent rollbackError)
        {
            if (refs.TryRemove(rollbackError.OperationId, out var orig) && orig is OrmLiteDiagnosticEvent dbOrig)
                AddOrmLite(dbOrig, rollbackError);
        }

        
        /** Redis */
        if (kvp.Key == Diagnostics.Events.Redis.WriteCommandBefore && kvp.Value is RedisDiagnosticEvent redisBefore)
        {
            AddRedis(redisBefore);
        }
        if (kvp.Key == Diagnostics.Events.Redis.WriteCommandRetry && kvp.Value is RedisDiagnosticEvent redisRetry)
        {
            if (refs.TryGetValue(redisRetry.OperationId, out var orig) && orig is RedisDiagnosticEvent redisOrig)
                AddRedis(redisOrig, redisRetry);
        }
        if (kvp.Key == Diagnostics.Events.Redis.WriteCommandAfter && kvp.Value is RedisDiagnosticEvent redisAfter)
        {
            if (refs.TryRemove(redisAfter.OperationId, out var orig) && orig is RedisDiagnosticEvent redisOrig)
                AddRedis(redisOrig, redisAfter);
        }
        if (kvp.Key == Diagnostics.Events.Redis.WriteCommandError && kvp.Value is RedisDiagnosticEvent redisError)
        {
            if (refs.TryRemove(redisError.OperationId, out var orig) && orig is RedisDiagnosticEvent redisOrig)
                AddRedis(redisOrig, redisError);
        }
        
        if (kvp.Key == Diagnostics.Events.Redis.WriteConnectionOpenBefore && kvp.Value is RedisDiagnosticEvent redisOpenBefore)
        {
            AddRedis(redisOpenBefore);
        }
        if (kvp.Key == Diagnostics.Events.Redis.WriteConnectionOpenAfter && kvp.Value is RedisDiagnosticEvent redisOpenAfter)
        {
            if (refs.TryRemove(redisOpenAfter.OperationId, out var orig) && orig is RedisDiagnosticEvent redisOrig)
                AddRedis(redisOrig, redisOpenAfter);
        }
        if (kvp.Key == Diagnostics.Events.Redis.WriteConnectionOpenError && kvp.Value is RedisDiagnosticEvent redisOpenError)
        {
            if (refs.TryRemove(redisOpenError.OperationId, out var orig) && orig is RedisDiagnosticEvent redisOrig)
                AddRedis(redisOrig, redisOpenError);
        }
        
        if (kvp.Key == Diagnostics.Events.Redis.WriteConnectionCloseBefore && kvp.Value is RedisDiagnosticEvent redisCloseBefore)
        {
            AddRedis(redisCloseBefore);
        }
        if (kvp.Key == Diagnostics.Events.Redis.WriteConnectionCloseAfter && kvp.Value is RedisDiagnosticEvent redisCloseAfter)
        {
            if (refs.TryRemove(redisCloseAfter.OperationId, out var orig) && orig is RedisDiagnosticEvent redisOrig)
                AddRedis(redisOrig, redisCloseAfter);
        }
        if (kvp.Key == Diagnostics.Events.Redis.WriteConnectionCloseError && kvp.Value is RedisDiagnosticEvent redisCloseError)
        {
            if (refs.TryRemove(redisCloseError.OperationId, out var orig) && orig is RedisDiagnosticEvent redisOrig)
                AddRedis(redisOrig, redisCloseError);
        }
        
        if (kvp.Key == Diagnostics.Events.Redis.WritePoolRent && kvp.Value is RedisDiagnosticEvent redisPoolBefore)
        {
            AddRedis(redisPoolBefore);
        }
        if (kvp.Key == Diagnostics.Events.Redis.WritePoolReturn && kvp.Value is RedisDiagnosticEvent redisPoolAfter)
        {
            if (refs.TryRemove(redisPoolAfter.OperationId, out var orig) && orig is RedisDiagnosticEvent redisOrig)
                AddRedis(redisOrig, redisPoolAfter);
        }
        
        Console.WriteLine();
    }

    void IObserver<DiagnosticListener>.OnError(Exception error)
    {
    }

    void IObserver<DiagnosticListener>.OnCompleted()
    {
        subscriptions.ForEach(x => x.Dispose());
        subscriptions.Clear();
    }
}

public class DiagnosticEntry
{
    /// <summary>
    /// Unique Id
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Request Id
    /// </summary>
    public string? TraceId { get; set; }
    /// <summary>
    /// ServiceStack, OrmLite, Redis
    /// </summary>
    public string Source { get; set; }
    /// <summary>
    /// Connection Open/Close, Command, Pool, Transaction
    /// </summary>
    public string EventType { get; set; }
    /// <summary>
    /// Human Message describing entry
    /// </summary>
    public string Message { get; set; }
    /// <summary>
    /// Method name
    /// </summary>
    public string Operation { get; set; }
    /// <summary>
    /// Managed Thread Id
    /// </summary>
    public int ThreadId { get; set; }
    /// <summary>
    /// Error Info if any
    /// </summary>
    public ResponseStatus? Error { get; set; }
    /// <summary>
    /// INSERT/SELECT, GET/SET
    /// </summary>
    public string CommandType { get; set; }
    /// <summary>
    /// SQL, Redis
    /// </summary>
    public string Command { get; set; }
    /// <summary>
    /// Primary or Target Key
    /// </summary>
    public string? Key { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    /// <summary>
    /// Redis positional Args
    /// </summary>
    public List<string> Args { get; set; }
    public List<long> ArgLengths { get; set; }
    /// <summary>
    /// OrmLite Args
    /// </summary>
    public Dictionary<string, object?> NamedArgs { get; set; }
    public TimeSpan? Duration { get; set; }
    public long Timestamp { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}
