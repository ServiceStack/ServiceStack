#if NETCORE        
using ServiceStack.Host;
#else
using System.Web;
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Auth;
using ServiceStack.DataAnnotations;
using ServiceStack.Host.Handlers;
using ServiceStack.Internal;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
#endif

namespace ServiceStack;

public class ServerEventsFeature : IPlugin, IConfigureServices, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.ServerEvents;
    public string StreamPath { get; set; }
    public string HeartbeatPath { get; set; }
    public string SubscribersPath { get; set; }
    public string UpdateSubscribersPath { get; set; }
    public string UnRegisterPath { get; set; }

    public TimeSpan IdleTimeout { get; set; }
    public TimeSpan HeartbeatInterval { get; set; }
    public TimeSpan HouseKeepingInterval { get; set; }

    public Action<IRequest> OnInit { get; set; }
    public Action<IRequest> OnHeartbeatInit { get; set; }
    public Action<IEventSubscription, IRequest> OnCreated { get; set; }
    public Action<IEventSubscription, Dictionary<string, string>> OnConnect { get; set; }
    public Action<IEventSubscription> OnSubscribe { get; set; }
    public Action<IEventSubscription, IRequest> OnDispose { get; set; }
    /// <summary>
    /// Calls sync OnSubscribe if exists by default
    /// </summary>
    public Func<IEventSubscription, Task> OnSubscribeAsync { get; set; }
    public Action<IEventSubscription> OnUnsubscribe { get; set; }
    /// <summary>
    /// Calls sync OnUnsubscribe if exists by default
    /// </summary>
    public Func<IEventSubscription,Task> OnUnsubscribeAsync { get; set; }
    /// <summary>
    /// Fired for both Sync/Async Notifications 
    /// </summary>
    public Action<IEventSubscription, IResponse, string> OnPublish { get; set; }
    /// <summary>
    /// Only fired for async notifications. Calls sync OnPublish if exists by default.
    /// </summary>
    public Func<IEventSubscription, IResponse, string, Task> OnPublishAsync { get; set; }
    /// <summary>
    /// Fired when a subscription is updated
    /// </summary>
    public Func<IEventSubscription, Task> OnUpdateAsync { get; set; }
    /// <summary>
    /// Replace the default ServiceStack.Text JSON Serializer with an alternative JSON Serializer
    /// </summary>
    public Func<object, string> Serialize { get; set; }
    public Action<IResponse, string> WriteEvent { get; private set; }
    public Func<IResponse, string, CancellationToken, Task> WriteEventAsync { get; private set; }
    /// <summary>
    /// Invoked when a connection 
    /// </summary>
    public Action<IEventSubscription> OnHungConnection { get; set; }

    public Action<IEventSubscription, Exception> OnError { get; set; }
    public bool NotifyChannelOfSubscriptions { get; set; }
    public bool LimitToAuthenticatedUsers { get; set; }
    public bool ValidateUserAddress { get; set; }

    public int ThrottlePublisherAfterBufferExceedsBytes { get; set; } = 1000 * 1024; 

    internal readonly ConcurrentDictionary<string, long> Counters = new();
    public void IncrementCounter(string name)
    {
        Counters.AddOrUpdate(name, 1, (key, oldValue) => oldValue + 1);
    }

    public ServerEventsFeature()
    {
        StreamPath = "/event-stream";
        HeartbeatPath = "/event-heartbeat";
        UnRegisterPath = "/event-unregister";
        SubscribersPath = "/event-subscribers";
        UpdateSubscribersPath = "/event-subscribers/{Id}";

        WriteEvent = (res, frame) =>
        {
            if (res is IWriteEvent writeEvent)
            {
                writeEvent.WriteEvent(frame);
            }
            else
            {
                MemoryProvider.Instance.Write(res.AllowSyncIO().OutputStream, frame.AsMemory());
                res.Flush();
            }
        };

        WriteEventAsync = async (res, frame, token) => 
        {
            if (res is IWriteEventAsync writeEvent)
            {
                await writeEvent.WriteEventAsync(frame, token).ConfigAwait();
            }
            else
            {
                await MemoryProvider.Instance.WriteAsync(res.OutputStream, frame.AsMemory(), token).ConfigAwait();
                await res.FlushAsync(token).ConfigAwait();
            }
        };

        OnHungConnection = sub => {
            var mse = HostContext.Resolve<IServerEvents>().GetMemoryServerEvents();
            mse?.RegisterHungConnection(sub);
            OnError?.Invoke(sub, new TimeoutException("Hung connection was detected"));
        };

        Serialize = JsonSerializer.SerializeToString;

        IdleTimeout = TimeSpan.FromSeconds(30);
        HeartbeatInterval = TimeSpan.FromSeconds(10);
        HouseKeepingInterval = TimeSpan.FromSeconds(5);

        NotifyChannelOfSubscriptions = true;
        ValidateUserAddress = true;

        OnSubscribeAsync = sub => {
            OnSubscribe?.Invoke(sub);
            return TypeConstants.EmptyTask;
        };

        OnUnsubscribeAsync = sub => {
            OnUnsubscribe?.Invoke(sub);
            return TypeConstants.EmptyTask;
        };

        OnPublishAsync = (sub, res, msg) => {
            OnPublish?.Invoke(sub, res, msg);
            return TypeConstants.EmptyTask;
        };
    }

    public void Configure(IServiceCollection services)
    {
        if (!services.Exists<IServerEvents>())
        {
            services.AddSingleton<IServerEvents>(new MemoryServerEvents
            {
                IdleTimeout = IdleTimeout,
                HouseKeepingInterval = HouseKeepingInterval,
                OnSubscribeAsync = OnSubscribeAsync,
                OnUnsubscribeAsync = OnUnsubscribeAsync,
                OnUpdateAsync = OnUpdateAsync,
                NotifyChannelOfSubscriptions = NotifyChannelOfSubscriptions,
                Serialize = Serialize,
                OnError = OnError,
            });
        }
        
        if (UnRegisterPath != null)
            services.RegisterService<ServerEventsUnRegisterService>(UnRegisterPath);

        if (SubscribersPath != null)
            services.RegisterService<ServerEventsSubscribersService>(SubscribersPath);
        
        if (UpdateSubscribersPath != null)
            services.RegisterService<UpdateEventSubscriberService>(UpdateSubscribersPath);
    }

    public void Register(IAppHost appHost)
    {
        appHost.RawHttpHandlers.Add(httpReq =>
            httpReq.PathInfo.EndsWith(StreamPath)
                ? new ServerEventsHandler()
                : httpReq.PathInfo.EndsWith(HeartbeatPath)
                    ? new ServerEventsHeartbeatHandler()
                    : null);

        appHost.OnDisposeCallbacks.Add(host => appHost.Resolve<IServerEvents>().Stop());
        
#if NET8_0_OR_GREATER
        (appHost as IAppHostNetCore).MapEndpoints(routeBuilder =>
        {
            routeBuilder.MapGet(StreamPath, httpContext => httpContext.ProcessRequestAsync(new ServerEventsHandler()))
                .WithMetadata<string>(nameof(StreamPath), tag:GetType().Name);
            routeBuilder.MapPost(HeartbeatPath, httpContext => httpContext.ProcessRequestAsync(new ServerEventsHeartbeatHandler()))
                .WithMetadata<string>(nameof(HeartbeatPath), tag:GetType().Name);
        });
#endif
    }

    internal bool CanAccessSubscription(IRequest req, SubscriptionInfo sub)
    {
        if (!ValidateUserAddress)
            return true;

        return sub.UserAddress == null || sub.UserAddress == req.UserHostAddress;
    }
}

public class ServerEventsHandler : HttpAsyncTaskHandler
{
    public override bool RunAsAsync()
    {
        return true;
    }

    /// <summary>
    /// Call IServerEvents.RemoveExpiredSubscriptions() after every count
    /// </summary>
    public static int RemoveExpiredSubscriptionsEvery { get; } = 1000;
    private static int ConnectionsCount = 0;

    public const string EventStreamDenyNoAuth = nameof(EventStreamDenyNoAuth);
    public const string EventStreamDenyOnInit = nameof(EventStreamDenyOnInit);
    public const string EventStreamDenyOnCreated = nameof(EventStreamDenyOnCreated);

    public override async Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
    {
        if (HostContext.ApplyCustomHandlerRequestFilters(req, res))
            return;

        var feature = HostContext.AssertPlugin<ServerEventsFeature>();

        var session = await req.GetSessionAsync().ConfigAwait();
        if (feature.LimitToAuthenticatedUsers && !session.IsAuthenticated)
        {
            feature.IncrementCounter(EventStreamDenyNoAuth);
            await session.ReturnFailedAuthentication(req).ConfigAwait();
            return;
        }

        var serverEvents = req.TryResolve<IServerEvents>();
        if ((Interlocked.Increment(ref ConnectionsCount) % RemoveExpiredSubscriptionsEvery) == 0)
        {
            await serverEvents.RemoveExpiredSubscriptionsAsync().ConfigAwait();
        }

        EventSubscription subscription = null;
        try
        {
            res.ContentType = MimeTypes.ServerSentEvents;
            res.AddHeader(HttpHeaders.CacheControl, "no-cache");
            res.AddHeader(HttpHeaders.XAccelBuffering, "no");
            res.ApplyGlobalResponseHeaders();
            res.UseBufferedStream = false;
            res.KeepAlive = true;

            feature.OnInit?.Invoke(req);

            if (req.Response.IsClosed)
            {
                feature.IncrementCounter(EventStreamDenyOnInit);
                return; //Allow short-circuiting in OnInit callback
            }

            await res.FlushAsync().ConfigAwait();

            var userAuthId = session?.UserAuthId;
            var anonUserId = serverEvents.GetNextSequence("anonUser");
            var userId = userAuthId ?? ("-" + anonUserId);
            var displayName = session.GetSafeDisplayName()
                              ?? "user" + anonUserId;

            var now = DateTime.UtcNow;
            var subscriptionId = HostContext.AppHost.CreateSessionId();

            //Handle both ?channel=A,B,C or ?channels=A,B,C
            var channels = new List<string>();
            var channel = req.QueryString["channel"];
            if (!string.IsNullOrEmpty(channel))
                channels.AddRange(channel.Split(','));
            channel = req.QueryString["channels"];
            if (!string.IsNullOrEmpty(channel))
                channels.AddRange(channel.Split(','));

            if (channels.Count == 0)
                channels = EventSubscription.UnknownChannel.ToList();

            subscription = new EventSubscription(res)
            {
                CreatedAt = now,
                LastPulseAt = now,
                Channels = channels.ToArray(),
                SubscriptionId = subscriptionId,
                UserId = userId,
                UserName = session?.UserName,
                DisplayName = displayName,
                SessionId = req.GetSessionId(),
                IsAuthenticated = session != null && session.IsAuthenticated,
                UserAddress = req.UserHostAddress,
                OnPublish = feature.OnPublish,
                OnPublishAsync = feature.OnPublishAsync,
                OnError = feature.OnError,
                Meta = new Dictionary<string, string> {
                    { "userId", userId },
                    { "isAuthenticated", session is { IsAuthenticated: true } ? "true": "false" },
                    { "displayName", displayName },
                    { "channels", string.Join(",", channels) },
                    { "createdAt", now.ToUnixTimeMs().ToString() },
                    { AuthMetadataProvider.ProfileUrlKey, session.GetProfileUrl() ?? JwtClaimTypes.DefaultProfileUrl },
                }.ToConcurrentDictionary(),
                ServerArgs = new Dictionary<string, string>(),
            };
            subscription.ConnectArgs = subscription.Meta.ToDictionary();

            feature.OnCreated?.Invoke(subscription, req);

            if (req.Response.IsClosed)
            {
                feature.IncrementCounter(EventStreamDenyOnCreated);
                return; //Allow short-circuiting in OnCreated callback
            }

            var heartbeatUrl = feature.HeartbeatPath != null
                ? req.ResolveAbsoluteUrl("~/".CombineWith(feature.HeartbeatPath)).AddQueryParam("id", subscriptionId)
                : null;

            var unRegisterUrl = feature.UnRegisterPath != null
                ? req.ResolveAbsoluteUrl("~/".CombineWith(feature.UnRegisterPath)).AddQueryParam("id", subscriptionId)
                : null;

            heartbeatUrl = AddSessionParamsIfAny(heartbeatUrl, req);
            unRegisterUrl = AddSessionParamsIfAny(unRegisterUrl, req);

            subscription.ConnectArgs = new Dictionary<string, string>(subscription.ConnectArgs) {
                {"id", subscriptionId },
                {"unRegisterUrl", unRegisterUrl},
                {"heartbeatUrl", heartbeatUrl},
                {"updateSubscriberUrl", req.ResolveAbsoluteUrl("~/".CombineWith(feature.SubscribersPath, subscriptionId)) },
                {"heartbeatIntervalMs", ((long)feature.HeartbeatInterval.TotalMilliseconds).ToString(CultureInfo.InvariantCulture) },
                {"idleTimeoutMs", ((long)feature.IdleTimeout.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)}
            };

            feature.OnConnect?.Invoke(subscription, subscription.ConnectArgs);                
        }
        catch (Exception e)
        {
            feature.IncrementCounter("Error.EventStream." + e.GetType().Name);
            res.StatusCode = 500;
            throw;
        }


        await serverEvents.RegisterAsync(subscription, subscription.ConnectArgs).ConfigAwait();

        if (req.Response is IWriteEventAsync) // gRPC
        {
            subscription.OnDispose = sub =>
            {
                try
                {
                    feature.OnDispose?.Invoke(sub, req);
                } catch { }
            };
            return;
        }
            
        var tcs = new TaskCompletionSource<bool>();

        //Only invoked by subscription.Dispose() 
        subscription.OnDispose = sub =>
        {
            try
            {
                feature.OnDispose?.Invoke(sub, req);
            } catch { }

            if (!res.IsClosed)
                System.Diagnostics.Debug.Fail("Should already be closed");

            if (!tcs.Task.IsCompleted)
                tcs.SetResult(true);
        };

        await tcs.Task.ConfigAwait();
    }

    static string AddSessionParamsIfAny(string url, IRequest req)
    {
        if (url != null && HostContext.Config.AllowSessionIdsInHttpParams)
        {
            var sessionKeys = new[] { "ss-id", "ss-pid", "ss-opt" };
            foreach (var key in sessionKeys)
            {
                var value = req.QueryString[key];
                if (value != null)
                    url = url.AddQueryParam(key, value);
            }
        }

        return url;
    }
}

public class ServerEventsHeartbeatHandler : HttpAsyncTaskHandler
{
    public override bool RunAsAsync() { return true; }

    private const string HeartbeatSubNotExists = nameof(HeartbeatSubNotExists);
    private const string HeartbeatInvalidAccess = nameof(HeartbeatInvalidAccess);

    public override async Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
    {
        if (HostContext.ApplyCustomHandlerRequestFilters(req, res))
            return;

        res.ApplyGlobalResponseHeaders();

        var serverEvents = req.TryResolve<IServerEvents>();

        await serverEvents.RemoveExpiredSubscriptionsAsync().ConfigAwait();

        var feature = HostContext.GetPlugin<ServerEventsFeature>();
        feature.OnHeartbeatInit?.Invoke(req);

        if (req.Response.IsClosed)
            return;

        var subscriptionId = req.QueryString["id"];
        var subscription = serverEvents.GetSubscriptionInfo(subscriptionId);
        if (subscription == null)
        {
            res.StatusCode = 404;
            res.StatusDescription = ErrorMessages.SubscriptionNotExistsFmt.LocalizeFmt(req, subscriptionId.SafeInput());
            feature.IncrementCounter(HeartbeatSubNotExists);
        }
        else if (!feature.CanAccessSubscription(req, subscription))
        {
            res.StatusCode = 403;
            res.StatusDescription = "Invalid User Address";
            feature.IncrementCounter(HeartbeatInvalidAccess);
        }
        else if (!await serverEvents.PulseAsync(subscriptionId).ConfigAwait())
        {
            res.StatusCode = 404;
            res.StatusDescription = ErrorMessages.SubscriptionNotExistsFmt.LocalizeFmt(req, subscriptionId.SafeInput());
            feature.IncrementCounter(HeartbeatSubNotExists);
        }
            
        await res.EndHttpHandlerRequestAsync(skipHeaders: true).ConfigAwait();
    }
}

[DefaultRequest(typeof(GetEventSubscribers))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class ServerEventsSubscribersService(IServerEvents serverEvents) : Service
{
    public object Any(GetEventSubscribers request)
    {
        var channels = new List<string>();

        var deprecatedChannels = Request.QueryString["channel"];
        if (!string.IsNullOrEmpty(deprecatedChannels))
            channels.AddRange(deprecatedChannels.Split(','));

        if (request.Channels != null)
            channels.AddRange(request.Channels);

        return channels.Count > 0
            ? serverEvents.GetSubscriptionsDetails(channels.ToArray())
            : serverEvents.GetAllSubscriptionsDetails();
    }
}

[ExcludeMetadata]
public class UnRegisterEventSubscriber : IReturn<Dictionary<string, string>>
{
    public string Id { get; set; }
}

[DefaultRequest(typeof(UnRegisterEventSubscriber))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class ServerEventsUnRegisterService(IServerEvents serverEvents) : Service
{
    public const string UnRegisterSubNotExists = nameof(UnRegisterSubNotExists);
    private const string UnRegisterInvalidAccess = nameof(UnRegisterInvalidAccess);
    private const string UnRegisterApi = nameof(UnRegisterApi);

    [AddHeader(ContentType = MimeTypes.Json)]
    public async Task<object> Any(UnRegisterEventSubscriber request)
    {
        var subscription = serverEvents.GetSubscriptionInfo(request.Id);

        var feature = HostContext.GetPlugin<ServerEventsFeature>();
        if (subscription == null)
        {
            feature.IncrementCounter(UnRegisterSubNotExists);
            throw HttpError.NotFound(ErrorMessages.SubscriptionNotExistsFmt.LocalizeFmt(Request, request.Id).SafeInput());
        }

        if (!feature.CanAccessSubscription(base.Request, subscription))
        {
            feature.IncrementCounter(UnRegisterInvalidAccess);
            throw HttpError.Forbidden(ErrorMessages.SubscriptionForbiddenFmt.LocalizeFmt(Request, request.Id.SafeInput()));
        }

        feature.IncrementCounter(UnRegisterApi);
        await serverEvents.UnRegisterAsync(subscription.SubscriptionId).ConfigAwait();

        return subscription.Meta;
    }
}

[DefaultRequest(typeof(UpdateEventSubscriber))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class UpdateEventSubscriberService(IServerEvents serverEvents) : Service
{
    public const string UpdateEventSubNotExists = nameof(UpdateEventSubNotExists);
    private const string UpdateEventInvalidAccess = nameof(UpdateEventInvalidAccess);

    public async Task<object> Any(UpdateEventSubscriber request)
    {
        var subscription = serverEvents.GetSubscriptionInfo(request.Id);

        var feature = HostContext.GetPlugin<ServerEventsFeature>();
        if (subscription == null)
        {
            feature.IncrementCounter(UpdateEventSubNotExists);
            throw HttpError.NotFound(ErrorMessages.SubscriptionNotExistsFmt.LocalizeFmt(Request, request.Id).SafeInput());
        }

        if (!feature.CanAccessSubscription(base.Request, subscription))
        {
            feature.IncrementCounter(UpdateEventInvalidAccess);
            throw HttpError.Forbidden(ErrorMessages.SubscriptionForbiddenFmt.LocalizeFmt(Request, request.Id.SafeInput()));
        }

        if (request.UnsubscribeChannels != null)
            await serverEvents.UnsubscribeFromChannelsAsync(subscription.SubscriptionId, request.UnsubscribeChannels).ConfigAwait();
        if (request.SubscribeChannels != null)
            await serverEvents.SubscribeToChannelsAsync(subscription.SubscriptionId, request.SubscribeChannels).ConfigAwait();

        return new UpdateEventSubscriberResponse();
    }
}

/*
# Commands
cmd.announce This is your captain speaking ...
cmd.toggle$#channels

# CSS
css.background #eceff1
css.background$#top #673ab7
css.background$#right #fffde7
css.background$#bottom #0091ea
css.color$#me #ff0
css.display$img none
css.display$img inline

# Receivers
document.title New Window Title
window.location http://google.com
cmd.removeReceiver window
cmd.addReceiver window
tv.watch http://youtu.be/518XP8prwZo
tv.watch https://servicestack.net/img/logo-220.png
tv.off

# Triggers
trigger.customEvent arg
*/

public class EventSubscription : SubscriptionInfo, IEventSubscription, IServiceStackAsyncDisposable
{
    private static ILog Log = LogManager.GetLogger(typeof(EventSubscription));
    public static string[] UnknownChannel = ["*"];

    private long LastPulseAtTicks = DateTime.UtcNow.Ticks;
    public DateTime LastPulseAt
    {
        get =>
            // assume gRPC connection is always active unless response is closed
            !response.IsClosed && (response is IWriteEvent || response is IWriteEventAsync)
                ? DateTime.UtcNow 
                : new DateTime(Interlocked.Read(ref LastPulseAtTicks), DateTimeKind.Utc);
        set => Interlocked.Exchange(ref LastPulseAtTicks, value.Ticks);
    }

    /// <summary>
    /// How long to wait to obtain lock before force disposing subscription connection
    /// </summary>
    public static int DisposeMaxWaitMs { get; set; } = 30 * 1000;

    private long subscribed = 1;
    private long isDisposed = 0;
    private long disposing = 0;

    public bool IsDisposed => Interlocked.Read(ref isDisposed) != 0;
    private bool Disposing => Interlocked.Read(ref disposing) != 0;

    private readonly IResponse response;
    private long msgId;

    public IResponse Response => this.response;
    public IRequest Request => this.response.Request;

    public long LastMessageId => Interlocked.Read(ref msgId);
    public string[] MergedChannels { set; get; }

    public EventSubscription(IResponse response)
    {
        this.response = response;
        this.Meta = new ConcurrentDictionary<string, string>();
        this.feature = HostContext.GetPlugin<ServerEventsFeature>();
        this.WriteEvent = feature.WriteEvent;
        this.WriteEventAsync = feature.WriteEventAsync;
        this.OnHungConnection = feature.OnHungConnection;
    }

    public void UpdateChannels(string[] channels)
    {
        // combine old and new channels
        var mergedChannels = new HashSet<string>(this.Channels);
        channels.Each(x => mergedChannels.Add(x));
            
        this.Channels = channels;
        this.MergedChannels = mergedChannels.ToArray();
        this.Meta["channels"] = string.Join(",", channels);
        jsonArgs = null; //refresh
    }

    public Func<IEventSubscription, Task> OnUnsubscribeAsync { get; set; }
    public Action<IEventSubscription> OnUnsubscribe { get; set; }
    public Action<IEventSubscription, IResponse, string> OnPublish { get; set; }
    public Func<IEventSubscription, IResponse, string, Task> OnPublishAsync { get; set; }
    public Action<IEventSubscription> OnHungConnection { get; set; }
    public Action<IEventSubscription> OnDispose { get; set; }
    private ServerEventsFeature feature;
    public Action<IResponse, string> WriteEvent { get; set; }
    public Func<IResponse, string, CancellationToken, Task> WriteEventAsync { get; set; }
    public Action<IEventSubscription, Exception> OnError { get; set; }
    public bool IsClosed => this.response.IsClosed;

    private readonly StringBuilder buffer = new();
        
    public void Pulse()
    {
        LastPulseAt = DateTime.UtcNow;
    }

    private string jsonArgs;
    public string JsonArgs => jsonArgs ??= SerializeDictionary(Meta);

    string CreateFrame(string selector, string message)
    {
        var msg = message ?? "";
        var frame = "id: " + Interlocked.Increment(ref msgId) + "\n"
                    + "data: " + selector + " " + msg + "\n\n";
        return frame;
    }

    public void Publish(string selector)
    {
        Publish(selector, null);
    }

    private long semaphore = 0;
    public bool IsLocked => Interlocked.Read(ref semaphore) != 0;

    int GetThrottleMs()
    {
        // throttle publisher if buffer gets too full
        lock (buffer)
            return buffer.Length < this.feature.ThrottlePublisherAfterBufferExceedsBytes 
                ? 0 
                : (int) Math.Ceiling(buffer.Length / 1000d);
    }
        
#if DEBUG
    private int threadIdWithLock = 0;
#endif
    internal static long NotificationsSent = 0;

    public Task PublishAsync(string selector, string message, CancellationToken token = default) => 
        PublishRawAsync(CreateFrame(selector, message), token);
    public async Task PublishRawAsync(string frame, CancellationToken token = default)
    {
        if (Disposing)
            return;
        if (response.IsClosed)
        {
            await DisposeAsync().ConfigAwait();
            return;
        }

        Exception writeEx = null;
        var hasLock = Interlocked.CompareExchange(ref semaphore, 1, 0) == 0;
        try
        {
            if (hasLock)
            {
#if DEBUG
                Interlocked.Exchange(ref threadIdWithLock, Thread.CurrentThread.ManagedThreadId);
#endif        
                if (CanWrite())
                {
                    try
                    {
                        var pendingWrites = GetAndResetBuffer();
                        if (pendingWrites != null)
                            frame = pendingWrites + frame;

                        if (OnPublishAsync != null)
                            await OnPublishAsync(this, response, frame).ConfigAwait();

                        Interlocked.Increment(ref NotificationsSent);
                        await WriteEventAsync(response, frame, token).ConfigAwait();
                    }
                    catch (Exception ex)
                    {
                        writeEx = ex;
                        this.feature.IncrementCounter("Error.Sub." + ex.GetType().Name);
                    }
                }
            }
            else
            {
                var waitMs = GetThrottleMs();
                if (waitMs > 0)
                    await Task.Delay(waitMs, token).ConfigAwait();

                lock (buffer)
                    buffer.Append(frame);
            }
        }
        finally
        {
            if (hasLock)
            {
                Interlocked.CompareExchange(ref semaphore, 0, semaphore);
#if DEBUG
                Interlocked.Exchange(ref threadIdWithLock, 0);
#endif        

                if (writeEx != null)
                    await HandleWriteExceptionAsync(frame, writeEx, token).ConfigAwait();
            }
        }

        if (response.IsClosed)
            await DisposeAsync().ConfigAwait();
    }

    public void Publish(string selector, string message) => PublishRaw(CreateFrame(selector, message));

    public void PublishRaw(string frame)
    {
        if (Disposing)
            return;
        if (response.IsClosed)
        {
            Dispose();
            return;
        }

        Exception writeEx = null;
        var hasLock = Interlocked.CompareExchange(ref semaphore, 1, 0) == 0;
        try
        {
            if (hasLock)
            {
                if (CanWrite())
                {
                    try
                    {
                        var pendingWrites = GetAndResetBuffer();
                        if (pendingWrites != null)
                            frame = pendingWrites + frame;

                        OnPublish?.Invoke(this, response, frame);

                        Interlocked.Increment(ref NotificationsSent);
                        WriteEvent(response, frame);
                    }
                    catch (Exception ex)
                    {
                        writeEx = ex;
                        this.feature.IncrementCounter("Error.Sub." + ex.GetType().Name);
                    }
                }
            }
            else
            {
                var waitMs = GetThrottleMs();
                if (waitMs > 0)
                    Thread.Sleep(waitMs);

                lock (buffer)
                    buffer.Append(frame);
            }
        }
        finally
        {
            if (hasLock)
            {
                Interlocked.CompareExchange(ref semaphore, 0, semaphore);

                if (writeEx != null)
                    TaskExt.RunSync(() => HandleWriteExceptionAsync(frame, writeEx));
            }
        }

        if (response.IsClosed)
            Dispose();
    }

    string GetAndResetBuffer()
    {
        lock (buffer)
        {
            if (buffer.Length == 0)
                return null;

            var ret = buffer.ToString();
            buffer.Length = 0;
            return ret;
        }
    }

    Task HandleWriteExceptionAsync(string frame, Exception ex, CancellationToken token=default)
    {
        if (ex != null)
        {
            Log.Warn("Could not publish notification to: " + frame.SafeSubstring(0, 50), ex);
            OnError?.Invoke(this, ex);
        }

        if (Env.IsMono)
        {
            // Mono: If we explicitly close OutputStream after the error socket wont leak (response.Close() doesn't work)
            try
            {
                // This will throw an exception, but on Mono (Linux/OSX) the socket will leak if we not close the OutputStream
                response.OutputStream.Close();
            }
            catch (Exception innerEx)
            {
                Log.Error("OutputStream.Close()", innerEx);
                this.feature.IncrementCounter("Error.SubClose." + innerEx.GetType().Name);
            }
        }

        return UnsubscribeAsync();
    }

    private bool CanWrite() => !Disposing && !response.IsClosed;

    [Obsolete("Use UnsubscribeAsync. Will be removed in future.")]
    public void Unsubscribe()
    {
        if (Interlocked.CompareExchange(ref subscribed, 0, 1) == 1)
        {
            var fn = OnUnsubscribeAsync;
            OnUnsubscribeAsync = null;
            response.Request.TryResolve<IServerEvents>()?.QueueAsyncTask(() => fn(this));
        }
        Dispose();
    }

    public Task UnsubscribeAsync()
    {
        if (Interlocked.CompareExchange(ref subscribed, 0, 1) == 1)
        {
            var fn = OnUnsubscribeAsync;
            OnUnsubscribeAsync = null;
            if (fn != null)
                return fn(this);
        }
        return DisposeAsync();
    }

    public static string SerializeDictionary(IDictionary<string, string> map)
    {
        if (map == null)
            return null;
        var sw = new System.IO.StringWriter();
        sw.Write('{');
        var i = 0;
        foreach (var entry in map)
        {
            if (entry.Value == null)
                continue;
            if (i++ > 0)
                sw.Write(',');
            Text.Json.JsonUtils.WriteString(sw, entry.Key);
            sw.Write(':');
            Text.Json.JsonUtils.WriteString(sw, entry.Value);
        }
        sw.Write('}');
        var json = sw.ToString();
        return json;
    }

    Task IServiceStackAsyncDisposable.DisposeAsync() => DisposeAsync();
    private async Task DisposeAsync()
    {
        if (Disposing)
            return;

        Interlocked.CompareExchange(ref disposing, 1, 0);

        if (Interlocked.CompareExchange(ref isDisposed, 1, 0) == 0)
        {
            var totalWaitMs = 0;
            var retries = 0;
            while (Interlocked.CompareExchange(ref semaphore, 1, 0) != 0)
            {
                var waitMs = ExecUtils.CalculateMemoryLockDelay(++retries);
#if DEBUG
                var msg = $"threadIdWithLock: {threadIdWithLock}, Current: {Thread.CurrentThread.ManagedThreadId}, waitMs: {waitMs}, total: {totalWaitMs}";
                Log.Debug(msg);
#endif
                await Task.Delay(waitMs).ConfigAwait();
                totalWaitMs += waitMs;
                if (DisposeMaxWaitMs >= 0 && totalWaitMs >= DisposeMaxWaitMs)
                    break;
            }

            var hasLock = DisposeMaxWaitMs < 0 || totalWaitMs < DisposeMaxWaitMs;
            if (!hasLock)
            {
                Log.Error($"Hung connection detected: Could not acquire semaphore within {DisposeMaxWaitMs}ms.");
                OnHungConnection(this);
                return;
            }

            Release();
        }
    }

    public void Dispose()
    {
        if (Disposing)
            return;

        Interlocked.CompareExchange(ref disposing, 1, 0);

        if (Interlocked.CompareExchange(ref isDisposed, 1, 0) == 0)
        {
            var totalWaitMs = 0;
            var retries = 0;
            while (Interlocked.CompareExchange(ref semaphore, 1, 0) != 0)
            {
                var waitMs = ExecUtils.CalculateMemoryLockDelay(++retries);
                Thread.Sleep(waitMs);
                totalWaitMs += waitMs;
                if (DisposeMaxWaitMs >= 0 && totalWaitMs >= DisposeMaxWaitMs)
                    break;
            }

            var hasLock = DisposeMaxWaitMs < 0 || totalWaitMs < DisposeMaxWaitMs;
            if (!hasLock)
            {
                Log.Error($"Hung connection detected: Could not acquire semaphore within {DisposeMaxWaitMs}ms.");
                OnHungConnection(this);
                return;
            }

            Release();
        }
    }

    public void Release()
    {
        try
        {
            response.EndHttpHandlerRequest(skipHeaders: true);
        }
        catch (Exception ex)
        {
            Log.Error("Error ending subscription response", ex);
            this.feature.IncrementCounter("Error.SubRelease." + ex.GetType().Name);
        }
            
        var fn = OnDispose;
        fn?.Invoke(this);
        OnDispose = null;
    }
}
    
public interface IEventSubscription : IDisposable
{
    DateTime CreatedAt { get; set; }
    DateTime LastPulseAt { get; set; }
    long LastMessageId { get; }

    string[] Channels { get; }
    string[] MergedChannels { get; } //both current and previous channels
    string UserId { get; }
    string UserName { get; }
    string DisplayName { get; }
    string SessionId { get; }
    string SubscriptionId { get; }
    string UserAddress { get; set; }
    bool IsAuthenticated { get; set; }
    bool IsClosed { get; }

    void UpdateChannels(string[] channels);

    Func<IEventSubscription, Task> OnUnsubscribeAsync { get; set; }
    Action<IEventSubscription> OnUnsubscribe { get; set; }
    [Obsolete("Use UnsubscribeAsync. Will be removed in future.")]
    void Unsubscribe();
    Task UnsubscribeAsync();

    void Publish(string selector, string message);
    Task PublishAsync(string selector, string message, CancellationToken token=default);
    void PublishRaw(string frame);
    Task PublishRawAsync(string frame, CancellationToken token=default);
    void Pulse();

    ConcurrentDictionary<string,string> Meta { get; set; }
    Dictionary<string,string> ServerArgs { get; set; }
    Dictionary<string,string> ConnectArgs { get; set; }
        
    string JsonArgs { get; } 
}

public class SubscriptionInfo
{
    public DateTime CreatedAt { get; set; }

    public string[] Channels { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string DisplayName { get; set; }
    public string SessionId { get; set; }
    public string SubscriptionId { get; set; }
    public string UserAddress { get; set; }
    public bool IsAuthenticated { get; set; }

    public ConcurrentDictionary<string, string> Meta { get; set; }
    public Dictionary<string, string> ConnectArgs { get; set; }
    public Dictionary<string, string> ServerArgs { get; set; }
}

public class MemoryServerEvents : IServerEvents
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(MemoryServerEvents));
    public static bool FlushNopOnSubscription = true;

    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan HouseKeepingInterval { get; set; } = TimeSpan.FromSeconds(5);

    public Func<IEventSubscription, Task> OnSubscribeAsync { get; set; }
    public Func<IEventSubscription,Task> OnUnsubscribeAsync { get; set; }
    public Func<IEventSubscription,Task> OnUpdateAsync { get; set; }
        
    public Func<IEventSubscription,Task> OnRemoveSubscriptionAsync { get; set; }

    public Func<IEventSubscription, Task> NotifyJoinAsync { get; set; }
    public Func<IEventSubscription, Task> NotifyLeaveAsync { get; set; }
    public Func<IEventSubscription, Task> NotifyUpdateAsync { get; set; }
    public Func<IEventSubscription, Task> NotifyHeartbeatAsync { get; set; }
    public Func<object, string> Serialize { get; set; }
    public bool NotifyChannelOfSubscriptions { get; set; }

    public ConcurrentDictionary<string, IEventSubscription> Subscriptions;
    public ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> ChannelSubscriptions;
    public ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> UserIdSubscriptions;
    public ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> UserNameSubscriptions;
    public ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> SessionSubscriptions;

    public Action<IEventSubscription, Exception> OnError { get; set; }

    private bool isDisposed;
    private ServerEventsFeature feature;

    public MemoryServerEvents()
    {
        Reset();

        NotifyJoinAsync = s => NotifyChannelsAsync(s.Channels, "cmd.onJoin", s.JsonArgs);
        NotifyLeaveAsync = s => NotifyChannelsAsync(s.Channels, "cmd.onLeave", s.JsonArgs);
        NotifyUpdateAsync = s => NotifyChannelsAsync(s.MergedChannels, "cmd.onUpdate", s.JsonArgs);
        NotifyHeartbeatAsync = s => NotifyRawAsync(Subscriptions, s.SubscriptionId, "cmd.onHeartbeat", s.JsonArgs);
        Serialize = JsonSerializer.SerializeToString;

        var appHost = HostContext.AppHost;
        this.feature = appHost?.GetPlugin<ServerEventsFeature>();
        if (this.feature != null)
        {
            IdleTimeout = feature.IdleTimeout;
            HouseKeepingInterval = feature.HouseKeepingInterval;
            OnSubscribeAsync = feature.OnSubscribeAsync;
            OnUnsubscribeAsync = feature.OnUnsubscribeAsync;
            OnUpdateAsync = feature.OnUpdateAsync;
            NotifyChannelOfSubscriptions = feature.NotifyChannelOfSubscriptions;
        }
    }

    public void Reset()
    {
        Subscriptions = new ConcurrentDictionary<string, IEventSubscription>();
        ChannelSubscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>>();
        UserIdSubscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>>();
        UserNameSubscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>>();
        SessionSubscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>>();
    }

    public void Start()
    {
    }

    public void Stop()
    {
        foreach (var sub in Subscriptions.ValuesWithoutLock())
        {
            try
            {
                sub.Dispose();
            }
            catch (Exception e)
            {
                Log.Warn($"Error disposing sub {sub.SessionId}", e);
                this.feature?.IncrementCounter("Error.MemStop." + e.GetType().Name);
            }
        }
        Reset();
    }

    public async Task StopAsync()
    {
        foreach (var sub in Subscriptions.ValuesWithoutLock())
        {
            try
            {
                await sub.DisposeAsync();
            }
            catch (Exception e)
            {
                Log.Warn($"Error disposing sub {sub.SessionId}", e);
                this.feature?.IncrementCounter("Error.MemStopAsync." + e.GetType().Name);
            }
        }
        Reset();
    }

    public void NotifyAll(string selector, object message)
    {
        if (isDisposed) 
            return;

        var body = Serialize(message);
        foreach (var sub in Subscriptions.ValuesWithoutLock())
        {
            sub.Publish(selector, body);
        }
    }

    public async Task NotifyAllAsync(string selector, object message, CancellationToken token = default)
    {
        if (isDisposed) 
            return;

        var body = Serialize(message);
        foreach (var sub in Subscriptions.ValuesWithoutLock())
        {
            await sub.PublishAsync(selector, body, token).ConfigAwait();
        }
    }

    public async Task NotifyAllJsonAsync(string selector, string json, CancellationToken token = default)
    {
        if (isDisposed) 
            return;

        foreach (var sub in Subscriptions.ValuesWithoutLock())
        {
            await sub.PublishAsync(selector, json, token).ConfigAwait();
        }
    }

    public async Task NotifyChannelsAsync(string[] channels, string selector, string body, CancellationToken token = default)
    {
        if (isDisposed) 
            return;

        foreach (var channel in channels)
        {
            await NotifyRawAsync(ChannelSubscriptions, channel, channel.AssertChannel() + "@" + selector.AssertSelector(), body, channel, token).ConfigAwait();
        }
    }

    public void NotifySubscription(string subscriptionId, string selector, object message, string channel = null) =>
        Notify(Subscriptions, subscriptionId, selector, message, channel);

    public Task NotifySubscriptionAsync(string subscriptionId, string selector, object message, string channel = null, CancellationToken token = default) =>
        NotifyAsync(Subscriptions, subscriptionId, selector, message, channel, token);

    public Task NotifySubscriptionJsonAsync(string subscriptionId, string selector, string json, string channel = null, CancellationToken token = default) =>
        NotifyRawAsync(Subscriptions, subscriptionId, selector, json, channel, token);

    public void NotifyChannel(string channel, string selector, object message) =>
        Notify(ChannelSubscriptions, channel, channel.AssertChannel() + "@" + selector.AssertSelector(), message, channel);

    public Task NotifyChannelAsync(string channel, string selector, object message, CancellationToken token = default) =>
        NotifyAsync(ChannelSubscriptions, channel, channel.AssertChannel() + "@" + selector.AssertSelector(), message, channel, token);

    public Task NotifyChannelJsonAsync(string channel, string selector, string json, CancellationToken token = default) =>
        NotifyRawAsync(ChannelSubscriptions, channel, channel.AssertChannel() + "@" + selector.AssertSelector(), json, channel, token);

    public void NotifyUserId(string userId, string selector, object message, string channel = null) =>
        Notify(UserIdSubscriptions, userId, selector, message, channel);

    public Task NotifyUserIdAsync(string userId, string selector, object message, string channel = null, CancellationToken token = default) =>
        NotifyAsync(UserIdSubscriptions, userId, selector, message, channel, token);
    public Task NotifyUserIdJsonAsync(string userId, string selector, string json, string channel = null, CancellationToken token = default) =>
        NotifyRawAsync(UserIdSubscriptions, userId, selector, json, channel, token);


    public void NotifyUserName(string userName, string selector, object message, string channel = null) =>
        Notify(UserNameSubscriptions, userName, selector, message, channel);

    public Task NotifyUserNameAsync(string userName, string selector, object message, string channel = null, CancellationToken token = default) =>
        NotifyAsync(UserNameSubscriptions, userName, selector, message, channel, token);
    public Task NotifyUserNameJsonAsync(string userName, string selector, string json, string channel = null, CancellationToken token = default) =>
        NotifyRawAsync(UserNameSubscriptions, userName, selector, json, channel, token);

    public void NotifySession(string sessionId, string selector, object message, string channel = null) => 
        Notify(SessionSubscriptions, sessionId, selector, message, channel);

    public Task NotifySessionAsync(string sessionId, string selector, object message, string channel = null, CancellationToken token = default) =>
        NotifyAsync(SessionSubscriptions, sessionId, selector, message, channel, token);
    public Task NotifySessionJsonAsync(string sessionId, string selector, string json, string channel = null, CancellationToken token = default) =>
        NotifyRawAsync(SessionSubscriptions, sessionId, selector, json, channel, token);

    public Dictionary<string, string> GetStats()
    {
        var to = new Dictionary<string, string> {
            {nameof(TotalConnections), Interlocked.Read(ref TotalConnections).ToString()},
            {nameof(TotalUnsubscriptions), Interlocked.Read(ref TotalUnsubscriptions).ToString()}, {
                nameof(EventSubscription.NotificationsSent),
                Interlocked.Read(ref EventSubscription.NotificationsSent).ToString()
            },
            {nameof(HungConnectionsDetected), Interlocked.Read(ref HungConnectionsDetected).ToString()},
            {nameof(HungConnectionsReleased), Interlocked.Read(ref HungConnectionsReleased).ToString()},
        };
        if (this.feature != null)
        {
            foreach (var entry in this.feature.Counters)
            {
                to[entry.Key] = entry.Value.ToString();
            }
        }
        return to;
    }

    private long TotalConnections = 0;
    private long TotalUnsubscriptions = 0;
    private long HungConnectionsDetected;
    private long HungConnectionsReleased;

    public void RegisterHungConnection(IEventSubscription sub)
    {
        Interlocked.Increment(ref HungConnectionsDetected);
        hungConnections.Add(sub);
    }

    // Send Update Notification
    readonly ConcurrentBag<IEventSubscription> pendingSubscriptionUpdates = new();

    // Full Unsubscription + Notifications
    readonly ConcurrentBag<IEventSubscription> pendingUnSubscriptions = new();
        
    // Just Unsubscribe
    readonly ConcurrentBag<IEventSubscription> expiredSubs = new();
        
    // Generic Async Tasks
    readonly ConcurrentBag<Func<Task>> pendingAsyncTasks = new();
        
    // Connections that are hung on their last write/flush, move here to free-up disposing thread 
    readonly ConcurrentBag<IEventSubscription> hungConnections = new();
        
    public void QueueAsyncTask(Func<Task> task)
    {
        pendingAsyncTasks.Add(task);
    }

    public MemoryServerEvents GetMemoryServerEvents() => this;

    private long taskCounter;
    async Task DoAsyncTasks(CancellationToken token = default)
    {
        var doLongLastingTasks = Interlocked.Increment(ref taskCounter) % 20 == 0; 
            
        if (pendingAsyncTasks.IsEmpty && pendingSubscriptionUpdates.IsEmpty && pendingUnSubscriptions.IsEmpty && expiredSubs.IsEmpty 
            && (!doLongLastingTasks || hungConnections.IsEmpty))
            return;
            
        var sw = Stopwatch.StartNew();
        while (!pendingAsyncTasks.IsEmpty)
        {
            if (pendingAsyncTasks.TryTake(out var asyncTask))
            {
                await asyncTask().ConfigAwait();

                this.feature?.IncrementCounter("MemDoAsyncTasks");
            }
        }
            
        while (!pendingSubscriptionUpdates.IsEmpty)
        {
            if (pendingSubscriptionUpdates.TryTake(out var sub))
            {
                if (OnUpdateAsync != null)
                    await OnUpdateAsync(sub).ConfigAwait();
                    
                if (NotifyUpdateAsync != null)
                    await NotifyUpdateAsync(sub).ConfigAwait();
                    
                this.feature?.IncrementCounter("MemDoSubUpdates");
            }
        }
            
        while (!pendingUnSubscriptions.IsEmpty)
        {
            if (pendingUnSubscriptions.TryTake(out var sub))
            {
                if (OnUnsubscribeAsync != null)
                    await OnUnsubscribeAsync(sub).ConfigAwait();

                await sub.DisposeAsync().ConfigAwait();

                if (NotifyChannelOfSubscriptions && sub.Channels != null && NotifyLeaveAsync != null)
                    await NotifyLeaveAsync(sub).ConfigAwait();
                    
                if (OnRemoveSubscriptionAsync != null)
                    await OnRemoveSubscriptionAsync(sub).ConfigAwait();

                this.feature?.IncrementCounter("MemDoUnSubs");
            }
        }
            
        while (!expiredSubs.IsEmpty)
        {
            if (expiredSubs.TryTake(out var sub))
            {
                await sub.UnsubscribeAsync().ConfigAwait();
                this.feature?.IncrementCounter("MemDoExpiredSubs");
            }
        }

        if (doLongLastingTasks && !hungConnections.IsEmpty)
        {
            // It's unlikely that a hung connection will be freed by ASP.NET, but we'll check it periodically and release it just in case
            var stillHung = new List<IEventSubscription>();
            while (!hungConnections.IsEmpty)
            {
                if (hungConnections.TryTake(out var sub) && sub is EventSubscription eventSub)
                {
                    if (eventSub.IsLocked)
                    {
                        stillHung.Add(sub);
                    }
                    else
                    {
                        Interlocked.Increment(ref HungConnectionsReleased);
                        Log.Info("Operation causing hung connection eventually completed, releasing connection...");
                        eventSub.Release();
                    }
                }
            }

            foreach (var sub in stillHung)
            {
                hungConnections.Add(sub);
            }
        }

        var elapsedMs = sw.ElapsedMilliseconds;
        if (elapsedMs > 30*1000)
            this.feature?.IncrementCounter("MemDoDuration30s");
        else if (elapsedMs > 10*1000)
            this.feature?.IncrementCounter("MemDoDuration10s");
        else if (elapsedMs > 5*1000)
            this.feature?.IncrementCounter("MemDoDuration5s");
    }

    protected void NotifyRaw(ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map, string key, 
        string selector, string body, string channel = null)
    {
        if (isDisposed) 
            return;

        var subs = map.TryGet(key);
        if (subs == null)
            return;

        var now = DateTime.UtcNow;

        foreach (var sub in subs.KeysWithoutLock())
        {
            if (sub.HasChannel(channel))
            {
                if (now - sub.LastPulseAt > IdleTimeout || sub.IsClosed)
                {
                    if (Log.IsDebugEnabled)
                        Log.DebugFormat("[SSE-SERVER] Expired {0} Sub {1} on ({2})", selector, sub.SubscriptionId,
                            string.Join(", ", sub.Channels));

                    if (sub.IsClosed) this.feature?.IncrementCounter("MemSubClosed");
                    expiredSubs.Add(sub);
                    continue;
                }

                if (Log.IsDebugEnabled)
                    Log.DebugFormat("[SSE-SERVER] Sending {0} msg to {1} on ({2})", selector, sub.SubscriptionId,
                        string.Join(", ", sub.Channels));

                sub.Publish(selector, body);
            }
        }
    }

    protected void Notify(ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map,
        string key, string selector, object message, string channel = null) =>
        NotifyRaw(map, key, selector, Serialize(message), channel);

    protected void Notify(ConcurrentDictionary<string, IEventSubscription> map, string key, 
        string selector, object message, string channel = null)
    {
        if (isDisposed) return;

        var sub = map.TryGet(key);
        if (sub == null || !sub.HasChannel(channel))
            return;

        var now = DateTime.UtcNow;
        if (now - sub.LastPulseAt > IdleTimeout || sub.IsClosed)
        {
            if (Log.IsDebugEnabled)
                Log.DebugFormat("[SSE-SERVER] Expired {0} Sub {1} on ({2})", selector, sub.SubscriptionId,
                    string.Join(", ", sub.Channels));

            if (sub.IsClosed) this.feature?.IncrementCounter("MemSubClosed");
            expiredSubs.Add(sub);
            return;
        }

        if (Log.IsDebugEnabled)
            Log.DebugFormat("[SSE-SERVER] Sending {0} msg to {1} on ({2})", selector, sub.SubscriptionId,
                string.Join(", ", sub.Channels));

        var body = Serialize(message);
        sub.Publish(selector, body);
    }

    protected async Task NotifyRawAsync(ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map, string key,
        string selector, string body, string channel = null, CancellationToken token = default)
    {
        if (isDisposed) 
            return;

        var subs = map.TryGet(key);
        if (subs == null || subs.Count == 0)
            return;

        var now = DateTime.UtcNow;

        foreach (var sub in subs.KeysWithoutLock())
        {
            if (sub.HasChannel(channel))
            {
                if (now - sub.LastPulseAt > IdleTimeout || sub.IsClosed)
                {
                    if (Log.IsDebugEnabled)
                        Log.DebugFormat("[SSE-SERVER] Expired {0} Sub {1} on ({2})", selector, sub.SubscriptionId,
                            string.Join(", ", sub.Channels));

                    if (sub.IsClosed) this.feature?.IncrementCounter("MemSubClosed");
                    expiredSubs.Add(sub);
                    continue;
                }

                if (Log.IsDebugEnabled)
                    Log.DebugFormat("[SSE-SERVER] Sending {0} msg to {1} on ({2})", selector, sub.SubscriptionId,
                        string.Join(", ", sub.Channels));

                await sub.PublishAsync(selector, body, token).ConfigAwait();
            }
        }
        await DoAsyncTasks(token).ConfigAwait();
    }

    protected Task NotifyAsync(ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map, string key,
        string selector, object message, string channel = null, CancellationToken token = default) =>
        NotifyRawAsync(map, key, selector, Serialize(message), channel, token);

    protected async Task NotifyRawAsync(ConcurrentDictionary<string, IEventSubscription> map, string key, 
        string selector, string body, string channel = null, CancellationToken token=default)
    {
        if (isDisposed) 
            return;

        var sub = map.TryGet(key);
        if (sub == null || !sub.HasChannel(channel))
            return;

        var now = DateTime.UtcNow;
        if (now - sub.LastPulseAt > IdleTimeout || sub.IsClosed)
        {
            if (Log.IsDebugEnabled)
                Log.DebugFormat("[SSE-SERVER] Expired {0} Sub {1} on ({2})", selector, sub.SubscriptionId,
                    string.Join(", ", sub.Channels));

            if (sub.IsClosed) this.feature?.IncrementCounter("MemSubClosed");
            expiredSubs.Add(sub);
            return;
        }

        if (Log.IsDebugEnabled)
            Log.DebugFormat("[SSE-SERVER] Sending {0} msg to {1} on ({2})", selector, sub.SubscriptionId,
                string.Join(", ", sub.Channels));

        await sub.PublishAsync(selector, body, token).ConfigAwait();
        await DoAsyncTasks(token).ConfigAwait();
    }

    protected Task NotifyAsync(ConcurrentDictionary<string, IEventSubscription> map, string key,
        string selector, object message, string channel = null, CancellationToken token = default) =>
        NotifyRawAsync(map, key, selector, Serialize(message), channel, token);

    protected async Task FlushNopAsync(ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map,
        string key, string channel = null, CancellationToken token=default)
    {
        var subs = map.TryGet(key);
        if (subs == null)
            return;

        var now = DateTime.UtcNow;

        foreach (var sub in subs.KeysWithoutLock())
        {
            if (sub.HasChannel(channel))
            {
                if (now - sub.LastPulseAt > IdleTimeout)
                {
                    expiredSubs.Add(sub);
                }
                await sub.PublishRawAsync("\n", token).ConfigAwait();
            }
        }
        await DoAsyncTasks(token).ConfigAwait();
    }

    public bool Pulse(string id)
    {
        if (isDisposed) 
            return false;

        var sub = GetSubscription(id);
        if (sub == null)
            return false;
        sub.Pulse();

        if (NotifyHeartbeatAsync != null)
            pendingAsyncTasks.Add(() => NotifyHeartbeatAsync(sub));

        return true;
    }

    public async Task<bool> PulseAsync(string id, CancellationToken token=default)
    {
        if (isDisposed) return false;

        var sub = GetSubscription(id);
        if (sub == null)
            return false;
        sub.Pulse();

        if (NotifyHeartbeatAsync != null)
            await NotifyHeartbeatAsync(sub).ConfigAwait();

        return true;
    }

    public IEventSubscription GetSubscription(string id)
    {
        if (id == null)
            return null;

        var sub = Subscriptions.TryGet(id);
        return sub;
    }

    public SubscriptionInfo GetSubscriptionInfo(string id)
    {
        return GetSubscription(id)?.GetInfo();
    }

    public List<SubscriptionInfo> GetSubscriptionInfosByUserId(string userId)
    {
        var subInfos = new List<SubscriptionInfo>();
        if (userId == null) return subInfos;

        var subs = UserIdSubscriptions.TryGet(userId);
        if (subs == null)
        {
            return subInfos;
        }

        foreach (var sub in subs.KeysWithoutLock())
        {
            var info = sub.GetInfo();
            if (info != null)
                subInfos.Add(info);
        }

        return subInfos;
    }

    readonly ConcurrentDictionary<string, long> SequenceCounters = new();

    public long GetNextSequence(string sequenceId)
    {
        return SequenceCounters.AddOrUpdate(sequenceId, 1, (id, count) => count + 1);
    }

    private long lastCleanAtTicks = DateTime.UtcNow.Ticks;
    private DateTime LastCleanAt
    {
        get => new DateTime(Interlocked.Read(ref lastCleanAtTicks), DateTimeKind.Utc);
        set => Interlocked.Exchange(ref lastCleanAtTicks, value.Ticks);
    }

    public int RemoveExpiredSubscriptions()
    {
        var now = DateTime.UtcNow;
        if (now - LastCleanAt <= HouseKeepingInterval)
            return -1;

        LastCleanAt = now;
        var count = 0;
        foreach (var sub in Subscriptions.ValuesWithoutLock())
        {
            if (now - sub.LastPulseAt > IdleTimeout)
            {
                count++;
                expiredSubs.Add(sub);
            }
        }

        return count;
    }

    public async Task<int> RemoveExpiredSubscriptionsAsync(CancellationToken token = default)
    {
        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
        var count = RemoveExpiredSubscriptions();
        await DoAsyncTasks(token).ConfigAwait();
        return count;
    }

    public void SubscribeToChannels(string subscriptionId, string[] channels)
    {
        if (isDisposed) return;

        if (subscriptionId == null)
            throw new ArgumentNullException(nameof(subscriptionId));
        if (channels == null)
            throw new ArgumentNullException(nameof(channels));

        var sub = GetSubscription(subscriptionId);
        if (sub == null || channels.Length == 0)
            return;

        lock (sub)
        {
            var subChannels = sub.Channels.ToList();
            foreach (var channel in channels)
            {
                if (subChannels.Contains(channel))
                    continue;

                subChannels.Add(channel);
                RegisterSubscription(sub, channel, ChannelSubscriptions);
            }

            sub.UpdateChannels(subChannels.ToArray());
        }

        if (NotifyChannelOfSubscriptions && NotifyUpdateAsync != null)
            pendingSubscriptionUpdates.Add(sub);
    }

    public Task SubscribeToChannelsAsync(string subscriptionId, string[] channels, CancellationToken token = default)
    {
        SubscribeToChannels(subscriptionId, channels);
            
        return DoAsyncTasks(token);
    }

    public void UnsubscribeFromChannels(string subscriptionId, string[] channels)
    {
        if (isDisposed) return;

        if (subscriptionId == null)
            throw new ArgumentNullException(nameof(subscriptionId));
        if (channels == null)
            throw new ArgumentNullException(nameof(channels));

        var sub = GetSubscription(subscriptionId);
        if (sub == null || channels.Length == 0)
            return;

        lock (sub)
        {
            foreach (var channel in channels)
            {
                if (!sub.Channels.Contains(channel))
                    continue;

                UnRegisterSubscription(sub, channel, ChannelSubscriptions);
            }

            var subChannels = sub.Channels.ToList();
            subChannels.RemoveAll(channels.Contains);

            sub.UpdateChannels(subChannels.ToArray());

            if (NotifyChannelOfSubscriptions && NotifyUpdateAsync != null)
                pendingSubscriptionUpdates.Add(sub);
        }
    }

    public Task UnsubscribeFromChannelsAsync(string subscriptionId, string[] channels, CancellationToken token = default)
    {
        UnsubscribeFromChannels(subscriptionId, channels);

        return DoAsyncTasks(token);
    }

    public List<Dictionary<string, string>> GetSubscriptionsDetails(params string[] channels)
    {
        var ret = new List<Dictionary<string, string>>();
        var alreadyAdded = new HashSet<string>();

        foreach (var channel in channels)
        {
            var subs = ChannelSubscriptions.TryGet(channel);
            if (subs == null)
                continue;

            foreach (var sub in subs.KeysWithoutLock())
            {
                if (alreadyAdded.Contains(sub.SubscriptionId))
                    continue;

                ret.Add(sub.Meta.ToDictionary());
                alreadyAdded.Add(sub.SubscriptionId);
            }
        }

        return ret;
    }

    public List<Dictionary<string, string>> GetAllSubscriptionsDetails()
    {
        var ret = new List<Dictionary<string, string>>();
        foreach (var sub in Subscriptions.ValuesWithoutLock())
        {
            ret.Add(sub.Meta.ToDictionary());
        }
        return ret;
    }

    public List<SubscriptionInfo> GetAllSubscriptionInfos()
    {
        var ret = new List<SubscriptionInfo>();
        foreach (var sub in Subscriptions.ValuesWithoutLock())
        {
            ret.Add(sub.GetInfo());
        }
        return ret;
    }

    public async Task RegisterAsync(IEventSubscription subscription, Dictionary<string, string> connectArgs = null, CancellationToken token=default)
    {
        if (isDisposed) 
            return;

        try
        {
            var connectJson = EventSubscription.SerializeDictionary(connectArgs);
            var asyncTasks = new List<Func<Task>>();
                
            lock (subscription)
            {
                if (connectArgs != null)
                {
                    asyncTasks.Add(() => subscription.PublishAsync("cmd.onConnect", connectJson, token));
                }

                subscription.OnUnsubscribeAsync = HandleUnsubscriptionAsync;
                foreach (string channel in subscription.Channels ?? EventSubscription.UnknownChannel)
                {
                    RegisterSubscription(subscription, channel, ChannelSubscriptions);
                }
                RegisterSubscription(subscription, subscription.SubscriptionId, Subscriptions);
                RegisterSubscription(subscription, subscription.UserId, UserIdSubscriptions);
                RegisterSubscription(subscription, subscription.UserName, UserNameSubscriptions);
                RegisterSubscription(subscription, subscription.SessionId, SessionSubscriptions);

                if (OnSubscribeAsync != null)
                    asyncTasks.Add(() => OnSubscribeAsync(subscription));

                if (NotifyChannelOfSubscriptions && subscription.Channels != null && NotifyJoinAsync != null)
                {
                    asyncTasks.Add(() => NotifyJoinAsync(subscription));
                }
                else if (FlushNopOnSubscription)
                {
                    //NOOP on gRPC Stream closes connection resulting in https://forums.servicestack.net/t/serverevents-not-received-on-client/10427/2
                    if (!subscription.IsGrpc())
                        asyncTasks.Add(() => FlushNopToChannelsAsync(subscription.Channels, token));
                }
            }

            Interlocked.Increment(ref TotalConnections);

            foreach (var asyncTask in asyncTasks)
            {
                await asyncTask().ConfigAwait();
            }
        }
        catch (Exception ex)
        {
            Log.Error("Register: " + ex.Message, ex);
            OnError?.Invoke(subscription, ex);
            this.feature?.IncrementCounter("Error.RegisterAsync." + ex.GetType().Name);

            throw;
        }
    }

    public async Task FlushNopToChannelsAsync(string[] channels, CancellationToken token=default)
    {
        if (isDisposed) return;

        //For some yet-to-be-determined reason we need to send something to all channels to determine
        //which subscriptions are no longer connected so we can dispose of them right then and there.
        //Failing to do this for 10 simultaneous requests on Local IIS will hang the entire Website instance
        //ref: https://forums.servicestack.net/t/serversentevents-with-notifychannelofsubscriptions-set-to-false-leaks-requests/2552/2
        foreach (var channel in channels)
        {
            await FlushNopAsync(ChannelSubscriptions, channel, channel, token).ConfigAwait();
        }
    }

    void RegisterSubscription(IEventSubscription subscription, string key,
        ConcurrentDictionary<string, IEventSubscription> map)
    {
        if (key == null || subscription == null)
            return;

        map.TryAdd(key, subscription);
    }

    void RegisterSubscription(IEventSubscription subscription, string key,
        ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map)
    {
        if (key == null || subscription == null)
            return;

        var subs = map.GetOrAdd(key, k => new ConcurrentDictionary<IEventSubscription, bool>());
        subs.TryAdd(subscription, true);
    }

    public void UnRegister(string subscriptionId, CancellationToken token=default)
    {
        var subscription = GetSubscription(subscriptionId);
        if (subscription == null)
            return;

        HandleUnsubscription(subscription);
    }
        
    void HandleUnsubscription(IEventSubscription subscription)
    {
        if (isDisposed) 
            return;

        lock (subscription)
        {
            foreach (var channel in subscription.Channels ?? EventSubscription.UnknownChannel)
            {
                UnRegisterSubscription(subscription, channel, ChannelSubscriptions);
            }
                
            UnRegisterSubscription(subscription, subscription.SubscriptionId, Subscriptions);
            UnRegisterSubscription(subscription, subscription.UserId, UserIdSubscriptions);
            UnRegisterSubscription(subscription, subscription.UserName, UserNameSubscriptions);
            UnRegisterSubscription(subscription, subscription.SessionId, SessionSubscriptions);
        }

        Interlocked.Increment(ref TotalUnsubscriptions);
            
        pendingUnSubscriptions.Add(subscription);
    }

    public Task UnRegisterAsync(string subscriptionId, CancellationToken token=default)
    {
        var subscription = GetSubscription(subscriptionId);
        if (subscription == null)
            return TypeConstants.EmptyTask;

        return HandleUnsubscriptionAsync(subscription, token);
    }

    Task HandleUnsubscriptionAsync(IEventSubscription subscription) => HandleUnsubscriptionAsync(subscription, CancellationToken.None);
    async Task HandleUnsubscriptionAsync(IEventSubscription subscription, CancellationToken token)
    {
        if (isDisposed) 
            return;

        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
        HandleUnsubscription(subscription);

        await DoAsyncTasks(token).ConfigAwait();
    }

    void UnRegisterSubscription(IEventSubscription subscription, string key,
        ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map)
    {
        if (key == null || subscription == null)
            return;

        try
        {
            var subs = map.TryGet(key);
            if (subs == null)
                return;

            subs.TryRemove(subscription, out bool _);
        }
        catch (Exception ex)
        {
            Log.Error("UnRegisterSubscription: " + ex.Message, ex);
            OnError?.Invoke(subscription, ex);
            this.feature?.IncrementCounter("Error.UnRegisterSub." + ex.GetType().Name);
            throw;
        }
    }

    void UnRegisterSubscription(IEventSubscription subscription, string key,
        ConcurrentDictionary<string, IEventSubscription> map)
    {
        if (key == null || subscription == null)
            return;

        map.TryRemove(key, out _);
    }

    public async Task DisposeAsync()
    {
        if (isDisposed) return;
        isDisposed = true;

        var allSubs = Subscriptions.ValuesWithoutLock().ToArray();
        foreach (var sub in allSubs)
        {
            await sub.UnsubscribeAsync().ConfigAwait();
        }

        Reset();
    }
        
    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;

        TaskExt.RunSync(DisposeAsync);
    }
}

public interface IWriteEvent
{
    void WriteEvent(string msg);
}

public interface IWriteEventAsync
{
    Task WriteEventAsync(string msg, CancellationToken token = default);
}

public interface IServerEvents : IDisposable
{
    // External API's
    void NotifyAll(string selector, object message);
    Task NotifyAllAsync(string selector, object message, CancellationToken token=default);
    Task NotifyAllJsonAsync(string selector, string json, CancellationToken token=default);

    void NotifyChannel(string channel, string selector, object message);
    Task NotifyChannelAsync(string channel, string selector, object message, CancellationToken token=default);
    Task NotifyChannelJsonAsync(string channel, string selector, string json, CancellationToken token=default);

    void NotifySubscription(string subscriptionId, string selector, object message, string channel = null);
    Task NotifySubscriptionAsync(string subscriptionId, string selector, object message, string channel = null, CancellationToken token=default);
    Task NotifySubscriptionJsonAsync(string subscriptionId, string selector, string json, string channel = null, CancellationToken token=default);

    void NotifyUserId(string userId, string selector, object message, string channel = null);
    Task NotifyUserIdAsync(string userId, string selector, object message, string channel = null, CancellationToken token=default);
    Task NotifyUserIdJsonAsync(string userId, string selector, string json, string channel = null, CancellationToken token=default);

    void NotifyUserName(string userName, string selector, object message, string channel = null);
    Task NotifyUserNameAsync(string userName, string selector, object message, string channel = null, CancellationToken token=default);
    Task NotifyUserNameJsonAsync(string userName, string selector, string json, string channel = null, CancellationToken token=default);

    void NotifySession(string sessionId, string selector, object message, string channel = null);
    Task NotifySessionAsync(string sessionId, string selector, object message, string channel = null, CancellationToken token=default);
    Task NotifySessionJsonAsync(string sessionId, string selector, string json, string channel = null, CancellationToken token=default);

    SubscriptionInfo GetSubscriptionInfo(string id);

    List<SubscriptionInfo> GetSubscriptionInfosByUserId(string userId);

    List<SubscriptionInfo> GetAllSubscriptionInfos();

    // Admin API's
    Task RegisterAsync(IEventSubscription subscription, Dictionary<string, string> connectArgs = null, CancellationToken token=default);

    Task UnRegisterAsync(string subscriptionId, CancellationToken token=default);

    long GetNextSequence(string sequenceId);

    int RemoveExpiredSubscriptions();
    Task<int> RemoveExpiredSubscriptionsAsync(CancellationToken token = default);

    void SubscribeToChannels(string subscriptionId, string[] channels);
    Task SubscribeToChannelsAsync(string subscriptionId, string[] channels, CancellationToken token=default);

    void UnsubscribeFromChannels(string subscriptionId, string[] channels);
    Task UnsubscribeFromChannelsAsync(string subscriptionId, string[] channels, CancellationToken token=default);

    void QueueAsyncTask(Func<Task> task);

    MemoryServerEvents GetMemoryServerEvents();

    // Client API's
    List<Dictionary<string, string>> GetSubscriptionsDetails(params string[] channels);

    List<Dictionary<string, string>> GetAllSubscriptionsDetails();

    Task<bool> PulseAsync(string subscriptionId, CancellationToken token=default);

    // Clear all Registrations
    void Reset();
    void Start();
    void Stop();
    Task StopAsync();
        
    // Observation APIs
    Dictionary<string, string> GetStats();
}

public static class Selector
{
    public static string Id(Type type) => "cmd." + type.Name;

    public static string Id<T>() => "cmd." + typeof(T).Name;
}

public static class ServerEventExtensions
{
    public static SubscriptionInfo GetInfo(this IEventSubscription sub)
    {
        if (sub == null)
            return null;

        return new SubscriptionInfo
        {
            CreatedAt = sub.CreatedAt,
            Channels = sub.Channels,
            UserId = sub.UserId,
            UserName = sub.UserName,
            DisplayName = sub.DisplayName,
            SessionId = sub.SessionId,
            SubscriptionId = sub.SubscriptionId,
            UserAddress = sub.UserAddress,
            IsAuthenticated = sub.IsAuthenticated,
            Meta = sub.Meta,
            ConnectArgs = sub.ConnectArgs,
            ServerArgs = sub.ServerArgs,
        };
    }

    public static bool HasChannel(this IEventSubscription sub, string channel)
    {
        return sub != null && (channel == null || Array.IndexOf(sub.Channels, channel) >= 0);
    }

    public static bool HasAnyChannel(this IEventSubscription sub, string[] channels)
    {
        if (sub == null || channels == null)
            return false;

        foreach (var channel in channels)
        {
            if (sub.HasChannel(channel))
                return true;
        }

        return false;
    }

    public static void NotifyAll(this IServerEvents server, object message) => 
        server.NotifyAll(Selector.Id(message.GetType()), message);
    public static Task NotifyAllAsync(this IServerEvents server, object message, CancellationToken token=default) => 
        server.NotifyAllAsync(Selector.Id(message.GetType()), message, token);

    public static void NotifyChannel(this IServerEvents server, string channel, object message) => 
        server.NotifyChannel(channel, Selector.Id(message.GetType()), message);

    public static Task NotifyChannelAsync(this IServerEvents server, string channel, object message, CancellationToken token=default) => 
        server.NotifyChannelAsync(channel, Selector.Id(message.GetType()), message, token);

    public static void NotifySubscription(this IServerEvents server, string subscriptionId, object message, string channel = null) => 
        server.NotifySubscription(subscriptionId, Selector.Id(message.GetType()), message, channel);

    public static Task NotifySubscriptionAsync(this IServerEvents server, string subscriptionId, object message, string channel = null, CancellationToken token=default) => 
        server.NotifySubscriptionAsync(subscriptionId, Selector.Id(message.GetType()), message, channel, token);

    public static void NotifyUserId(this IServerEvents server, string userId, object message, string channel = null) => 
        server.NotifyUserId(userId, Selector.Id(message.GetType()), message, channel);

    public static Task NotifyUserIdAsync(this IServerEvents server, string userId, object message, string channel = null, CancellationToken token=default) => 
        server.NotifyUserIdAsync(userId, Selector.Id(message.GetType()), message, channel, token);

    public static void NotifyUserName(this IServerEvents server, string userName, object message, string channel = null) => 
        server.NotifyUserName(userName, Selector.Id(message.GetType()), message, channel);

    public static Task NotifyUserNameAsync(this IServerEvents server, string userName, object message, string channel = null, CancellationToken token=default) => 
        server.NotifyUserNameAsync(userName, Selector.Id(message.GetType()), message, channel, token);

    public static void NotifySession(this IServerEvents server, string sspid, object message, string channel = null) => 
        server.NotifySession(sspid, Selector.Id(message.GetType()), message, channel);

    public static Task NotifySessionAsync(this IServerEvents server, string sspid, object message, string channel = null, CancellationToken token=default) => 
        server.NotifySessionAsync(sspid, Selector.Id(message.GetType()), message, channel, token);

    internal static TElement TryGet<TKey, TElement>(this ConcurrentDictionary<TKey, TElement> dic, TKey key)
    {
        if (dic == null || key == null)
            return default(TElement);

        dic.TryGetValue(key, out var res);
        return res;
    }

    internal static IEnumerable<TElement> ValuesWithoutLock<TKey, TElement>(this ConcurrentDictionary<TKey, TElement> source)
    {
        foreach (var item in source)
        {
            if (item.Value != null)
                yield return item.Value;
        }
    }

    internal static IEnumerable<TKey> KeysWithoutLock<TKey, TElement>(this ConcurrentDictionary<TKey, TElement> source)
    {
        foreach (var item in source)
        {
            yield return item.Key;
        }
    }

    internal static string AssertChannel(this string channel) => channel == null || channel.IndexOf('@') == -1
        ? channel
        : throw new ArgumentException(@"Illegal '@' used in name", nameof(channel));

    internal static string AssertSelector(this string selector) => selector == null || selector.IndexOf('@') == -1
        ? selector
        : throw new ArgumentException(@"Illegal '@' used in name", nameof(selector));

    public static bool IsGrpc(this IEventSubscription sub) =>
        ((EventSubscription)sub).Request.RequestAttributes.HasFlag(RequestAttributes.Grpc);
}