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
using ServiceStack.Admin;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.NativeTypes;
using ServiceStack.Web;

namespace ServiceStack;

public class AdminProfiling : IReturn<AdminProfilingResponse>
{
    public string? Source { get; set; }
    public string? EventType { get; set; }
    public int? ThreadId { get; set; }
    public string? TraceId { get; set; }
    public string? UserAuthId { get; set; }
    public string? SessionId { get; set; }
    public string? Tag { get; set; }
    public int Skip { get; set; }
    public int? Take { get; set; }
    public string? OrderBy { get; set; }
    public bool? WithErrors { get; set; }
    public bool? Pending { get; set; }
}

public class AdminProfilingResponse
{
    public List<DiagnosticEntry> Results { get; set; }
    public int Total { get; set; }
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

        var snapshot = request.Pending != true 
            ? feature.Observer.GetLatestEntries(null)
            : feature.Observer.GetPendingEntries(null);
        
        var logs = snapshot.AsQueryable();
        
        if (!request.Source.IsNullOrEmpty())
            logs = logs.Where(x => x.Source == request.Source);
        if (!request.EventType.IsNullOrEmpty())
            logs = logs.Where(x => x.EventType == request.EventType);
        if (!request.TraceId.IsNullOrEmpty())
            logs = logs.Where(x => x.TraceId == request.TraceId);
        if (request.ThreadId != null)
            logs = logs.Where(x => x.ThreadId == request.ThreadId.Value);
        if (!request.UserAuthId.IsNullOrEmpty())
            logs = logs.Where(x => x.UserAuthId == request.UserAuthId);
        if (!request.SessionId.IsNullOrEmpty())
            logs = logs.Where(x => x.SessionId == request.SessionId);
        if (!request.Tag.IsNullOrEmpty())
            logs = logs.Where(x => x.Tag == request.Tag);
        if (request.WithErrors.HasValue)
            logs = request.WithErrors.Value
                ? logs.Where(x => x.Error != null)
                : logs.Where(x => x.Error == null);

        var query = string.IsNullOrEmpty(request.OrderBy)
            ? logs.OrderByDescending(x => x.Id)
            : logs.OrderBy(request.OrderBy);

        var results = query.Skip(request.Skip);
        results = results.Take(request.Take.GetValueOrDefault(feature.DefaultLimit));
        
        return new AdminProfilingResponse
        {
            Results = results.ToList(),
            Total = snapshot.Count,
        };
    }
}

public class ProfilingFeature : IPlugin, Model.IHasStringId, IPreInitPlugin
{
    public string Id => Plugins.Profiling;
    public const int DefaultCapacity = 10000;

    public string AccessRole { get; set; } = RoleNames.Admin;

    /// <summary>
    /// Don't log requests of these types. By default Profiling/Metadata requests are excluded
    /// </summary>
    public List<Type> ExcludeRequestDtoTypes { get; set; } = new();

    /// <summary>
    /// Don't log requests from these path infos prefixes
    /// </summary>
    public List<string> ExcludeRequestPathInfoStartingWith { get; set; } = new();
    
    /// <summary>
    /// Turn On/Off Tracking of Responses per-request
    /// </summary>
    public Func<IRequest, bool>? ExcludeRequestsFilter { get; set; }

    /// <summary>
    /// Don't log request body's for services with sensitive information.
    /// By default Auth and Registration requests are hidden.
    /// </summary>
    public List<Type> HideRequestBodyForRequestDtoTypes { get; set; } = new();

    /// <summary>
    /// Don't log Response DTO Types
    /// </summary>
    public List<Type> ExcludeResponseTypes { get; set; } = new();
    
    /// <summary>
    /// Turn On/Off Tracking of Responses per-request
    /// </summary>
    public Func<IRequest, bool>? ResponseTrackingFilter { get; set; }

    /// <summary>
    /// Which features to Profile, default all
    /// </summary>
    public ProfileSource Profile { get; set; } = ProfileSource.All;
    
    /// <summary>
    /// Whether to include CallStack StackTrace 
    /// </summary>
    public bool? IncludeStackTrace { get; set; }

    /// <summary>
    /// Size of circular buffer of profiled events
    /// </summary>
    public int Capacity { get; set; } = DefaultCapacity;
    
    /// <summary>
    /// Default take, if none is specified
    /// </summary>
    public int DefaultLimit { get; set; } = 50;
    
    public List<string> SummaryFields { get; set; }

    /// <summary>
    /// Attach custom data to request profiling summary fields
    /// </summary>
    public Func<IRequest,string?>? TagResolver { get; set; }
    /// <summary>
    /// Label to show for custom tag
    /// </summary>
    public string? TagLabel { get; set; }
    
    /// <summary>
    /// Customize DiagnosticEntry that gets captured
    /// </summary>
    public Action<DiagnosticEntry, DiagnosticEvent>? DiagnosticEntryFilter { get; set; }

    public ProfilerDiagnosticObserver Observer { get; set; }

    public ProfilingFeature()
    {
        this.ExcludeRequestPathInfoStartingWith = new[] {
            "/js/petite-vue.js",
            "/js/servicestack-client.js",
            "/js/require.js",
            "/js/servicestack-client.js",
            "/admin-ui",
        }.ToList();
        // Sync with RequestLogsFeature
        this.ExcludeRequestDtoTypes = new[]
        {
            typeof(RequestLogs),
            typeof(HotReloadFiles),
            typeof(TypesCommonJs),
            typeof(MetadataApp),
            typeof(AdminDashboard),
            typeof(AdminProfiling),
            typeof(NativeTypesBase),
        }.ToList();
        this.HideRequestBodyForRequestDtoTypes = new[] 
        {
            typeof(Authenticate), 
            typeof(Register),
        }.ToList();
        this.ExcludeResponseTypes = new[]
        {
            typeof(AppMetadata),
            typeof(MetadataTypes),
            typeof(byte[]),
            typeof(string),
        }.ToList();
        this.SummaryFields = new List<string> {
            nameof(DiagnosticEntry.Id),
            nameof(DiagnosticEntry.TraceId),
            nameof(DiagnosticEntry.Source),
            nameof(DiagnosticEntry.EventType),
            nameof(DiagnosticEntry.Message),
            nameof(DiagnosticEntry.ThreadId),
            nameof(DiagnosticEntry.UserAuthId),
            nameof(DiagnosticEntry.Duration),
            nameof(DiagnosticEntry.Timestamp),
        };
    }

    public void Register(IAppHost appHost)
    {
        if (IncludeStackTrace != null)
            Diagnostics.IncludeStackTrace = IncludeStackTrace.Value;
        
        appHost.RegisterService(typeof(AdminProfilingService));
        
        Observer = new ProfilerDiagnosticObserver(this);
        var subscription = DiagnosticListener.AllListeners.Subscribe(Observer);
        appHost.OnDisposeCallbacks.Add(host => subscription.Dispose());
        
        if (!SummaryFields.Contains(nameof(DiagnosticEntry.Tag)) && TagResolver != null)
            SummaryFields.Add(nameof(DiagnosticEntry.Tag));
        
        appHost.AddToAppMetadata(meta => {
            meta.Plugins.Profiling = new ProfilingInfo {
                AccessRole = AccessRole,
                SummaryFields = SummaryFields,
                TagLabel = TagLabel,
                DefaultLimit = DefaultLimit,
            };
        });
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
        if ((feature.Profile.HasFlag(ProfileSource.ServiceStack) && diagnosticListener.Name is Diagnostics.Listeners.ServiceStack)
            || (feature.Profile.HasFlag(ProfileSource.OrmLite) && diagnosticListener.Name is Diagnostics.Listeners.OrmLite)
            || (feature.Profile.HasFlag(ProfileSource.Redis) && diagnosticListener.Name is Diagnostics.Listeners.Redis))
        {
            var subscription = diagnosticListener.Subscribe(this);
            subscriptions.Add(subscription);
        }
    }

    private readonly ConcurrentDictionary<Guid, object> refs = new();

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
        var log = LogManager.GetLogger(typeof(ProfilerDiagnosticObserver));
        log.Error(error.Message, error);
    }
    
    public List<DiagnosticEntry> GetLatestEntries(int? take)
    {
        return take != null 
            ? entries.Where(x => !x.Deleted).Take(take.Value).ToList()
            : entries.Where(x => !x.Deleted).ToList();
    }

    public List<DiagnosticEntry> GetPendingEntries(int? take)
    {
        var to = new List<DiagnosticEntry>();
        var i = 0;
        foreach (var entry in refs)
        {
            if (i++ > take)
                break;
            if (entry.Value is DiagnosticEvent origEvent)
            {
                var origEntry = origEvent switch {
                    RequestDiagnosticEvent ssEvent => ToDiagnosticEntry(ssEvent),
                    OrmLiteDiagnosticEvent dbEvent => ToDiagnosticEntry(dbEvent),
                    RedisDiagnosticEvent redisEvent => ToDiagnosticEntry(redisEvent),
                    _ => CreateDiagnosticEntry(origEvent)
                };
                to.Add(origEntry);
            }
        }
        return to;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    DiagnosticEntry AddEntry(DiagnosticEntry entry)
    {
        entries.Enqueue(entry);
        if (entries.Count > capacity)
            entries.TryDequeue(out _);
        return entry;
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
            UserAuthId = e.UserAuthId,
            Tag = e.Tag,
            Timestamp = e.Timestamp,
            ThreadId = Thread.CurrentThread.ManagedThreadId,
            StackTrace = e.StackTrace,
        };
        if (e.Exception != null)
        {
            to.StackTrace ??= e.Exception.StackTrace;
            if (to.Error?.StackTrace != null)
                to.Error.StackTrace = null;
        }

        if (e.Exception != null)
        {
            to.Error = DtoUtils.CreateResponseStatus(e.Exception, request:null, debugMode:true);
        }

        if (orig != null)
        {
            to.Duration = TimeSpan.FromTicks(e.Timestamp - orig.Timestamp);
        }

        return to;
    }

    public DiagnosticEntry Filter(DiagnosticEntry entry, DiagnosticEvent e)
    {
        feature.DiagnosticEntryFilter?.Invoke(entry, e);
        return entry;
    }

    public DiagnosticEntry ToDiagnosticEntry(RequestDiagnosticEvent e, RequestDiagnosticEvent? orig = null)
    {
        var to = CreateDiagnosticEntry(e, orig);

        to.TraceId ??= e.Request.GetTraceId();

        var requestType = e.Request.Dto?.GetType();
        var responseDto = e.Request.Response.Dto.GetResponseDto();
        var responseType = responseDto?.GetType();
        to.Command = requestType != null 
            ? requestType.Name 
            : e.Request.OperationName;
        to.Message = e.Request.PathInfo;
        to.SessionId = e.Request.GetSessionId();
        to.UserAuthId = HostContext.AppHost.TryGetUserId(e.Request);
        
        if (orig == null)
        {
            if (IncludeRequestDto(requestType))
                to.NamedArgs = e.Request.Dto.ToObjectDictionary();
        }
        else
        {
            // Need to update original request since Request DTO isn't known at start of request
            if (orig.DiagnosticEntry is DiagnosticEntry entry)
            {
                entry.Command = to.Command;
                entry.Message = to.Message;
                entry.SessionId ??= to.SessionId;
                entry.UserAuthId ??= to.UserAuthId;
                if (IncludeRequestDto(requestType))
                    entry.NamedArgs = e.Request.Dto.ToObjectDictionary();
            }
            
            if (responseType != null && (feature.ExcludeRequestsFilter == null || feature.ExcludeRequestsFilter.Invoke(e.Request) == false))
            {
                if (!feature.ExcludeResponseTypes.Any(x => x.IsInstanceOfType(responseDto)))
                {
                    to.NamedArgs = responseDto.ToObjectDictionary();
                }
            }
        }

        return Filter(to, e);
    }

    private bool IncludeRequestDto(Type? requestType) => requestType != null && 
        !feature.HideRequestBodyForRequestDtoTypes.Any(x => x.IsAssignableFrom(requestType));

    bool ShouldTrack(RequestDiagnosticEvent e)
    {
        if (feature.ExcludeRequestPathInfoStartingWith.Any(x => e.Request.PathInfo.StartsWith(x)))
            return false;
        
        var requestType = e.Request.Dto?.GetType();
        if (requestType != null)
        {
            if (feature.ExcludeRequestDtoTypes.Any(x => x.IsAssignableFrom(requestType)))
                return false;
        }
        else
        {
            var requestName = e.Request.OperationName; // Request DTO Type isn't created at WriteRequestBefore
            if (feature.ExcludeRequestDtoTypes.Any(x => x.Name == requestName))
                return false;
        }
        if (feature.ExcludeRequestsFilter != null && feature.ExcludeRequestsFilter(e.Request))
            return false;
        
        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddServiceStack(RequestDiagnosticEvent before)
    {
        // Request DTO isn't known at WriteRequestBefore
        if (!ShouldTrack(before))
        {
            LogNotTracked(before);
            return;
        }
        
        refs[before.OperationId] = before;
        before.DiagnosticEntry = AddEntry(ToDiagnosticEntry(before));
    }

    private static readonly ILog log = LogManager.GetLogger(typeof(ProfilerDiagnosticObserver));
    
    [Conditional("DEBUG")]
    private static void LogNotTracked(RequestDiagnosticEvent before)
    {
        if (log.IsDebugEnabled)
            log.Debug($"!{before.EventType}.ShouldTrack({before.Request.OperationName}, {before.Request.PathInfo})");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddServiceStack(RequestDiagnosticEvent before, RequestDiagnosticEvent after)
    {
        if (!ShouldTrack(before) || !ShouldTrack(after))
        {
            // Mark entry already in queue for deletion
            if (before.DiagnosticEntry is DiagnosticEntry entry)
                entry.Deleted = true;
            
            LogNotTracked(before);
            return;
        }

        after.DiagnosticEntry = AddEntry(ToDiagnosticEntry(after, before));
    }

    public DiagnosticEntry ToDiagnosticEntry(OrmLiteDiagnosticEvent e, OrmLiteDiagnosticEvent? orig = null)
    {
        var to = CreateDiagnosticEntry(e, orig);
        if (e.Command != null)
        {
            to.Command = e.Command.CommandText;
            to.Message = to.Command.LeftPart(' ');
            to.NamedArgs = new();
            foreach (IDbDataParameter p in e.Command.Parameters)
            {
                to.NamedArgs[p.ParameterName] = p.Value is DBNull
                    ? "null"
                    : p.Value;
            }
        }
        else
        {
            if (to.EventType.Contains("ConnectionOpen"))
                to.Message = "Open Connection";
            else if (to.EventType.Contains("ConnectionClose"))
                to.Message = "Close Connection";
            else if (to.EventType.Contains("TransactionOpen"))
                to.Message = "Open Transaction";
            else if (to.EventType.Contains("TransactionCommit"))
                to.Message = "Commit Transaction";
            else if (to.EventType.Contains("TransactionRollback"))
                to.Message = "Rollback Transaction";
        }
        return Filter(to, e);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddOrmLite(OrmLiteDiagnosticEvent before)
    {
        refs[before.OperationId] = before;
        before.DiagnosticEntry = AddEntry(ToDiagnosticEntry(before));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddOrmLite(OrmLiteDiagnosticEvent before, OrmLiteDiagnosticEvent after)
    {
        after.DiagnosticEntry = AddEntry(ToDiagnosticEntry(after, before));
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
            to.Message = to.Args.FirstOrDefault() ?? "";
            to.Command = string.Join(" ", to.Args);
        }
        else
        {
            if (to.EventType.Contains("ConnectionOpen"))
                to.Message = "Open Connection";
            else if (to.EventType.Contains("ConnectionClose"))
                to.Message = "Close Connection";
            else if (to.EventType == Diagnostics.Events.Redis.WritePoolRent)
                to.Message = "Rent from Pool";
            else if (to.EventType == Diagnostics.Events.Redis.WritePoolReturn)
                to.Message = "Return to Pool";
        }
        return Filter(to, e);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddRedis(RedisDiagnosticEvent before)
    {
        refs[before.OperationId] = before;
        before.DiagnosticEntry = AddEntry(ToDiagnosticEntry(before));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddRedis(RedisDiagnosticEvent before, RedisDiagnosticEvent after)
    {
        before.DiagnosticEntry = AddEntry(ToDiagnosticEntry(after, before));
    }

    public void OnNext(KeyValuePair<string, object> kvp)
    {
        Console.WriteLine();
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
        
        if (kvp.Key == Diagnostics.Events.OrmLite.WriteTransactionOpen && kvp.Value is OrmLiteDiagnosticEvent commitOpen)
        {
            AddOrmLite(commitOpen);
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
    public string? UserAuthId { get; set; }
    public string? SessionId { get; set; }
    /// <summary>
    /// Redis positional Args
    /// </summary>
    public List<string>? Args { get; set; }
    public List<long>? ArgLengths { get; set; }
    /// <summary>
    /// OrmLite Args
    /// </summary>
    public Dictionary<string, object?>? NamedArgs { get; set; }
    public TimeSpan? Duration { get; set; }
    public long Timestamp { get; set; }
    /// <summary>
    /// Custom data that can be attached with ProfilingFeature.TagResolver  
    /// </summary>
    public string? Tag { get; set; }
    public string? StackTrace { get; set; }
    public Dictionary<string, string> Meta { get; set; }
    internal bool Deleted { get; set; }
}
