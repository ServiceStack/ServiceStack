#nullable enable

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Admin;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Model;
using ServiceStack.NativeTypes;
using ServiceStack.Web;

namespace ServiceStack;

public class ProfilingFeature : IPlugin, IConfigureServices, Model.IHasStringId, IPreInitPlugin
{
    public string Id => Plugins.Profiling;
    public const int DefaultCapacity = 10000;

    /// <summary>
    /// Limit API access to users in role
    /// </summary>
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
    /// Whether to include CallStack StackTrace 
    /// </summary>
    public bool? IncludeStackTrace { get; set; }

    /// <summary>
    /// Attach custom data to request profiling summary fields
    /// </summary>
    public Func<IRequest,string?>? TagResolver { get; set; }
    /// <summary>
    /// Label to show for custom tag
    /// </summary>
    public string? TagLabel { get; set; }
    
    /// <summary>
    /// The properties displayed in Profiling UI results grid
    /// </summary>
    public List<string> SummaryFields { get; set; }
    
    /// <summary>
    /// Default take, if none is specified
    /// </summary>
    public int DefaultLimit { get; set; } = 50;
    
    /// <summary>
    /// Customize DiagnosticEntry that gets captured
    /// </summary>
    public Action<DiagnosticEntry, DiagnosticEvent>? DiagnosticEntryFilter { get; set; }

    /// <summary>
    /// Maximum char/byte length of string response body
    /// </summary>
    public int MaxBodyLength { get; set; } = 10 * 10 * 1024;

    protected internal ProfilerDiagnosticObserver Observer { get; set; }
    protected internal long startTick = Stopwatch.GetTimestamp();
    protected internal DateTime startDateTime = DateTime.UtcNow;

    public ProfilingFeature()
    {
        ExcludeRequestPathInfoStartingWith = [
            "/js/petite-vue.js",
            "/js/servicestack-client.js",
            "/js/require.js",
            "/js/highlight.js",
            "/admin-ui",
        ];
        // Sync with RequestLogsFeature
        ExcludeRequestDtoTypes = [
            typeof(RequestLogs),
            typeof(HotReloadFiles),
            typeof(TypesCommonJs),
            typeof(MetadataApp),
            typeof(AdminDashboard),
            typeof(AdminProfiling),
            typeof(AdminRedis),
            typeof(NativeTypesBase),
        ];
        HideRequestBodyForRequestDtoTypes = [
            typeof(Authenticate), 
            typeof(Register),
        ];
        ExcludeResponseTypes = [
            typeof(AppMetadata),
            typeof(MetadataTypes),
            typeof(byte[]),
            typeof(string),
        ];
        SummaryFields = [
            nameof(DiagnosticEntry.Id),
            nameof(DiagnosticEntry.TraceId),
            nameof(DiagnosticEntry.Source),
            nameof(DiagnosticEntry.EventType),
            nameof(DiagnosticEntry.Message),
            nameof(DiagnosticEntry.ThreadId),
            nameof(DiagnosticEntry.UserAuthId),
            nameof(DiagnosticEntry.Duration),
            nameof(DiagnosticEntry.Date),
        ];
    }

    public void Configure(IServiceCollection services)
    {
        services.RegisterService(typeof(AdminProfilingService));
    }

    public void Register(IAppHost appHost)
    {
        if (IncludeStackTrace != null)
            Diagnostics.IncludeStackTrace = IncludeStackTrace.Value;
        
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
            feature.AddAdminLink(AdminUiFeature.Profiling, new LinkInfo {
                Id = "profiling",
                Label = "Profiling",
                Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Profiling)),
                Show = $"role:{AccessRole}",
            });
        });
    }
}

public sealed class ProfilerDiagnosticObserver(ProfilingFeature feature) :
    IObserver<DiagnosticListener>,
    IObserver<KeyValuePair<string, object>>
{
    public static int AnalyzeCommandLength { get; set; } = 100;

    private readonly int capacity = feature.Capacity;

    protected readonly ConcurrentQueue<DiagnosticEntry> entries = new();
    private long idCounter = 0;
    
    private readonly List<IDisposable> subscriptions = new();

    void IObserver<DiagnosticListener>.OnNext(DiagnosticListener diagnosticListener)
    {
        // Console.WriteLine(diagnosticListener.Name);
        if ((feature.Profile.HasFlag(ProfileSource.ServiceStack) && diagnosticListener.Name is Diagnostics.Listeners.ServiceStack)
            || (feature.Profile.HasFlag(ProfileSource.Client) &&
                diagnosticListener.Name is Diagnostics.Listeners.HttpClient or Diagnostics.Listeners.Client)
            || (feature.Profile.HasFlag(ProfileSource.OrmLite) && diagnosticListener.Name is Diagnostics.Listeners.OrmLite)
            || (feature.Profile.HasFlag(ProfileSource.Redis) && diagnosticListener.Name is Diagnostics.Listeners.Redis)
            // || diagnosticListener.Name == "Microsoft.AspNetCore") Server
            )
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

    private static readonly ILog log = LogManager.GetLogger(typeof(ProfilerDiagnosticObserver));
    
    [Conditional("DEBUG")]
    private static void LogNotTracked(RequestDiagnosticEvent before)
    {
        if (log.IsDebugEnabled)
            log.Debug($"!{before.EventType}.ShouldTrack({before.Request.OperationName}, {before.Request.PathInfo})");
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
            Date = e.Date,
            ThreadId = Thread.CurrentThread.ManagedThreadId,
            OperationId = e.OperationId,
        };
        SetException(to, e.Exception);

        if (orig != null)
        {
            to.Duration = TimeSpan.FromTicks(e.Timestamp - orig.Timestamp);
        }

        return to;
    }

    public static void SetException(DiagnosticEntry to, Exception? ex)
    {
        if (ex != null)
            to.StackTrace ??= Diagnostics.CreateStackTrace(ex);

        if (ex != null)
            to.Error = DtoUtils.CreateResponseStatus(ex, request: null, debugMode: true);

        if (to.StackTrace != null && to.Error?.StackTrace != null && ex is not IResponseStatusConvertible)
            to.Error.StackTrace = null;
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

    public DiagnosticEntry ToDiagnosticEntry(MqRequestDiagnosticEvent e, MqRequestDiagnosticEvent? orig = null)
    {
        var to = CreateDiagnosticEntry(e, orig);
        
        var dto = e.Message?.Body ?? e.Body;
        if (IncludeRequestDto(dto.GetType()))
            to.NamedArgs = dto.ToObjectDictionary();

        to.Command = dto.GetType().Name;
        to.Message = e.ReplyTo != null
            ? "reply:" + e.ReplyTo
            : e.Message != null
                ? "to:" + e.Message.Id
                : "";

        // Original MQ Request populates TraceId + Tag in IMessage
        if (orig != null)
        {
            to.TraceId ??= orig.TraceId;
            to.Tag ??= orig.Tag;
        }

        to.TraceId ??= e.Message?.TraceId ?? to.TraceId;
        to.Tag ??= e.Message?.Tag;

        return Filter(to, e);
    }


    private bool IncludeRequestDto(Type? requestType) => requestType != null && 
        !feature.HideRequestBodyForRequestDtoTypes.Any(x => x.IsAssignableFrom(requestType));

    bool ShouldTrack(RequestDiagnosticEvent e)
    {
        if (feature.ExcludeRequestPathInfoStartingWith.Any(x => e.Request.PathInfo?.StartsWith(x) == true))
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
    
    bool ShouldTrack(MqRequestDiagnosticEvent e)
    {
        var dto = e.Message?.Body ?? e.Body; 
        var requestType = dto?.GetType();
        if (requestType != null)
        {
            if (feature.ExcludeRequestDtoTypes.Any(x => x.IsAssignableFrom(requestType)))
                return false;
        }
        
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
    
#if NET6_0_OR_GREATER

    bool ShouldTrack(HttpClientDiagnosticEvent e)
    {
        var requestType = e.Request?.GetType();
        if (requestType != null)
        {
            if (feature.ExcludeRequestDtoTypes.Any(x => x.IsAssignableFrom(requestType)))
                return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddClient(HttpClientDiagnosticEvent before)
    {
        // Request DTO isn't known at WriteRequestBefore
        if (!ShouldTrack(before))
            return;
        
        refs[before.OperationId] = before;
        before.DiagnosticEntry = AddEntry(ToDiagnosticEntry(before));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddClient(HttpClientDiagnosticEvent before, HttpClientDiagnosticEvent after)
    {
        if (!ShouldTrack(before) || !ShouldTrack(after))
        {
            // Mark entry already in queue for deletion
            if (before.DiagnosticEntry is DiagnosticEntry entry)
                entry.Deleted = true;
            return;
        }

        after.DiagnosticEntry = AddEntry(ToDiagnosticEntry(after, before));
    }

    const string Unknown = "(unknown)";

    readonly NativeTypes.CSharp.CSharpGenerator csharpGen = new(new MetadataTypesConfig());
    public DiagnosticEntry ToDiagnosticEntry(HttpClientDiagnosticEvent e, HttpClientDiagnosticEvent? orig = null)
    {
        var to = CreateDiagnosticEntry(e, orig);

        var httpReq = e.HttpRequest ?? orig?.HttpRequest;
        if (httpReq != null)
            to.Message = $"{httpReq.Method} {httpReq.RequestUri?.ToString().LeftPart('?')}";

        if (orig == null)
        {
            to.Command = (httpReq != null
                ? httpReq.Method.Method + " " + httpReq.RequestUri?.AbsolutePath
                : null) ?? Unknown;
            var requestType = e.Request?.GetType();
            if (requestType == typeof(string) || requestType?.IsValueType == true)
            {
                to.Arg = e.Request?.ToString() ?? Unknown;
            }
            else
            {
                if (requestType != null && e.Request is not IDictionary)
                    to.Command = csharpGen.Type(requestType.Name, requestType.GetGenericArguments().Select(x => x.Name).ToArray());
                if (IncludeRequestDto(requestType))
                    to.NamedArgs = e.Request.ToObjectDictionary();
            }
        }
        else
        {
            var responseType = e.Response?.GetType() ?? orig.ResponseType;
            to.Command = responseType != null
                 ? csharpGen.Type(responseType.Name,responseType.GetGenericArguments().Select(x => x.Name).ToArray())
                 : httpReq?.RequestUri?.AbsolutePath ?? Unknown;
        }

        return Filter(to, e);
    }

    private object ReadHttpContent(System.Net.Http.HttpContent? content)
    {
        content.LoadIntoBufferAsync().Wait(); //TODO find better way to buffer HttpContent so doesn't fail when reread from client
        
        var requestBody = content?.ReadAsString();
        if (requestBody == null) 
            return StringBody(requestBody);

        if (content is System.Net.Http.StringContent sc)
        {
            if (MimeTypes.MatchesContentType(sc.Headers.ContentType?.MediaType, MimeTypes.Json))
            {
                try
                {
                    return JSON.parse(requestBody);
                }
                catch (Exception e)
                {
                    log.Error($"Could not parse JSON", e);
                }
            }
            if (MimeTypes.MatchesContentType(sc.Headers.ContentType?.MediaType, MimeTypes.FormUrlEncoded))
            {
                try
                {
                    return Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(requestBody);
                }
                catch (Exception e)
                {
                    log.Error($"Could not parse Form " + MimeTypes.FormUrlEncoded, e);
                }
            }
        }
        return StringBody(requestBody);
    }
#endif

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
    
    /**
     * MQ
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddServiceStack(MqRequestDiagnosticEvent before)
    {
        // Request DTO isn't known at WriteRequestBefore
        if (!ShouldTrack(before))
            return;
        
        refs[before.OperationId] = before;
        before.DiagnosticEntry = AddEntry(ToDiagnosticEntry(before));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddServiceStack(MqRequestDiagnosticEvent before, MqRequestDiagnosticEvent after)
    {
        if (!ShouldTrack(before) || !ShouldTrack(after))
        {
            // Mark entry already in queue for deletion
            if (before.DiagnosticEntry is DiagnosticEntry entry)
                entry.Deleted = true;
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
        // Console.WriteLine();
        // Console.WriteLine(kvp.Key);
        // Console.WriteLine(kvp.Value);

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

        /** Gateway */
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

#if NET6_0_OR_GREATER
        /** Client */
        
        /** HttpClient */
        // if (kvp.Key == Diagnostics.Events.Client.WriteRequestBefore && kvp.Value is HttpClientDiagnosticEvent clientBefore)
        // {
        //     AddServiceStack(clientBefore);
        // }
        if (kvp.Key == Diagnostics.Events.HttpClient.OutStart)
        {
            var obj = kvp.Value.ToObjectDictionary();
            // Console.WriteLine($"<{kvp.Value.GetType().Name}> = {string.Join(',', obj.Keys)} :: {string.Join(',', obj.Values.Select(x => x.GetType()?.Name))}");
        }
        if (kvp.Key == Diagnostics.Events.HttpClient.Request)
        {
            var obj = kvp.Value.ToObjectDictionary();
            // Console.WriteLine($"<{kvp.Value.GetType().Name}> = {string.Join(',', obj.Keys)} :: {string.Join(',', obj.Values.Select(x => x.GetType()?.Name))}");
            if (obj.TryGetValue(Diagnostics.Keys.Request, out var oValue) && oValue is System.Net.Http.HttpRequestMessage httpReq
                && obj.TryGetValue(Diagnostics.Keys.LoggingRequestId, out var oGuid) && oGuid is Guid loggingRequestId
                && obj.TryGetValue(Diagnostics.Keys.Timestamp, out var oLong) && oLong is long timestamp)
            {
                var entry = new HttpClientDiagnosticEvent {
                    EventType = Diagnostics.Events.HttpClient.Request,
                    OperationId = loggingRequestId,
                    HttpRequest = httpReq,
                }.Init(Activity.Current);
                entry.Timestamp = timestamp;
                entry.Date = feature.startDateTime + TimeSpan.FromTicks(timestamp - feature.startTick);
                entry.ClientOperationId = httpReq.Options.TryGetValue(Diagnostics.Keys.HttpRequestOperationId, out var operationId)
                        ? operationId
                        : null;
                entry.Request = httpReq.Options.TryGetValue(Diagnostics.Keys.HttpRequestRequest, out var request)
                    ? request
                    : httpReq.RequestUri != null && !HttpUtils.HasRequestBody(httpReq.Method.Method)
                        ? Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(httpReq.RequestUri.Query)
                        : null;
                entry.ResponseType = httpReq.Options.TryGetValue(Diagnostics.Keys.HttpRequestResponseType, out var oType) && oType is Type responseType
                    ? responseType
                    : null;

                entry.Request ??= ReadHttpContent(httpReq.Content);
                
                AddClient(entry);
            }
        }
        // if (kvp.Key == Diagnostics.Events.HttpClient.OutStop)
        // {
        //     var obj = kvp.Value.ToObjectDictionary();
        // }
        if (kvp.Key == Diagnostics.Events.HttpClient.Response)
        {
            var obj = kvp.Value.ToObjectDictionary();
            if (obj.TryGetValue(Diagnostics.Keys.Response, out var oValue) && oValue is System.Net.Http.HttpResponseMessage httpRes
               && obj.TryGetValue(Diagnostics.Keys.LoggingRequestId, out var oGuid) && oGuid is Guid loggingRequestId
               && obj.TryGetValue(Diagnostics.Keys.Timestamp, out var oLong) && oLong is long timestamp)
            {
                var entry = new HttpClientDiagnosticEvent {
                    EventType = Diagnostics.Events.HttpClient.Response,
                    OperationId = loggingRequestId,
                    HttpResponse = httpRes,
                    Exception = (int)httpRes.StatusCode >= 400
                        ? new HttpError(httpRes.StatusCode, httpRes.ReasonPhrase) {
                            StackTrace = Diagnostics.IncludeStackTrace ? Environment.StackTrace : null,
                        }
                        : null,
                }.Init(Activity.Current);
                entry.Timestamp = timestamp;
                entry.Date = feature.startDateTime + TimeSpan.FromTicks(timestamp - feature.startTick);

                // JsonApiClient
                if (refs.TryRemove(loggingRequestId, out var orig) && orig is HttpClientDiagnosticEvent reqOrig)
                {
                    if (reqOrig.ClientOperationId != null)
                        refs[reqOrig.ClientOperationId.Value] = entry;

                    AddClient(reqOrig, entry);
                }
                else
                {
                    // Normal HttpClient
                    AddClient(entry);
                    entry.Response ??= ReadHttpContent(httpRes.Content);
                }
            }
        }
        if (kvp.Key == Diagnostics.Events.Client.WriteRequestAfter && kvp.Value is HttpClientDiagnosticEvent clientAfter)
        {
            if (refs.TryRemove(clientAfter.OperationId, out var httpEntry) && httpEntry is HttpClientDiagnosticEvent httpAfter)
            {
                httpAfter.Response = clientAfter.Response;
                if (httpAfter.DiagnosticEntry is DiagnosticEntry entry)
                {
                    if (httpAfter.Response != null && !feature.ExcludeResponseTypes.Any(x => x.IsInstanceOfType(httpAfter.Response)))
                    {
                        httpAfter.ResponseType = httpAfter.Response.GetType();
                        entry.Arg = clientAfter.Response switch {
                            string s => StringBody(s),
                            byte[] => "(bytes)",
                            System.IO.Stream => "(stream)",
                            System.Net.Http.HttpResponseMessage => "(HttpResponseMessage)",
                            _ => null,
                        };
                        if (entry.Arg == null)
                            entry.NamedArgs = clientAfter.Response.ToObjectDictionary();
                    }
                }
            }
        }
        if (kvp.Key == Diagnostics.Events.Client.WriteRequestError && kvp.Value is HttpClientDiagnosticEvent clientError)
        {
            if (refs.TryRemove(clientError.OperationId, out var httpEntry) && httpEntry is HttpClientDiagnosticEvent httpAfter)
            {
                httpAfter.Exception = clientError.Exception;
                if (httpAfter.DiagnosticEntry is DiagnosticEntry entry)
                {
                    entry.StackTrace = null; // Clear low quality HttpClient Error
                    SetException(entry, clientError.Exception);
                }
            }
        }
#endif
        
        /** MQ */
        if (kvp.Key == Diagnostics.Events.ServiceStack.WriteMqRequestBefore && kvp.Value is MqRequestDiagnosticEvent mqReqBefore)
        {
            AddServiceStack(mqReqBefore);
        }
        if (kvp.Key == Diagnostics.Events.ServiceStack.WriteMqRequestAfter && kvp.Value is MqRequestDiagnosticEvent mqReqAfter)
        {
            if (refs.TryRemove(mqReqAfter.OperationId, out var orig) && orig is MqRequestDiagnosticEvent reqOrig)
                AddServiceStack(reqOrig, mqReqAfter);
        }
        if (kvp.Key == Diagnostics.Events.ServiceStack.WriteMqRequestError && kvp.Value is MqRequestDiagnosticEvent mqReqError)
        {
            if (refs.TryRemove(mqReqError.OperationId, out var orig) && orig is MqRequestDiagnosticEvent reqOrig)
                AddServiceStack(reqOrig, mqReqError);
        }
        if (kvp.Key == Diagnostics.Events.ServiceStack.WriteMqRequestPublish && kvp.Value is MqRequestDiagnosticEvent mqReqPublish)
        {
            var orig = refs.TryRemove(mqReqPublish.OperationId, out var refOrig)
                ? refOrig
                : entries.FirstOrDefault(x => x.OperationId == mqReqPublish.OperationId); 
            if (orig is MqRequestDiagnosticEvent reqOrig)
                AddServiceStack(reqOrig, mqReqPublish);
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
        
        /*** OrmLite Connections */
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
        
        /** OrmLite Transactions */
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
        
        /*** Redis Connections */
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
        
        /*** Redis Pools */
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

    private string StringBody(string? s)
    {
        return s == null 
            ? string.Empty 
            : s.Length < feature.MaxBodyLength 
                ? s 
                : s.Substring(0, feature.MaxBodyLength) + "...";
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
    /// Single Arg
    /// </summary>
    public string? Arg { get; set; }
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
    public DateTime Date { get; set; }
    /// <summary>
    /// Custom data that can be attached with ProfilingFeature.TagResolver  
    /// </summary>
    public string? Tag { get; set; }
    public string? StackTrace { get; set; }
    public Dictionary<string, string?> Meta { get; set; }
    internal bool Deleted { get; set; }
    internal Guid? OperationId { get; set; }
}
