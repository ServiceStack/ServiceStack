using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public class SessionFeature : IPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.Session;
    public const string SessionId = "ss-id";
    public const string PermanentSessionId = "ss-pid";
    public const string SessionOptionsKey = "ss-opt";
    public const string XUserAuthId = HttpHeaders.XUserAuthId;

    public static TimeSpan DefaultSessionExpiry = TimeSpan.FromDays(7 * 2); //2 weeks
    public static TimeSpan DefaultPermanentSessionExpiry = TimeSpan.FromDays(7 * 4); //4 weeks

    public TimeSpan? SessionExpiry { get; set; }
    public TimeSpan? SessionBagExpiry { get; set; }
    public TimeSpan? PermanentSessionExpiry { get; set; }

    private static bool alreadyConfigured;

    public void Register(IAppHost appHost)
    {
        if (alreadyConfigured) return;
        alreadyConfigured = true;

        //Add permanent and session cookies if not already set.
        appHost.GlobalRequestFilters.Add(AddSessionIdToRequestFilter);
    }

    public static void AddSessionIdToRequestFilter(IRequest req, IResponse res, object requestDto)
    {
        if (req.PopulateFromRequestIfHasSessionId(requestDto)) 
            return;

        if (req.GetTemporarySessionId() == null)
        {
            res.CreateTemporarySessionId(req);
        }
        if (req.GetPermanentSessionId() == null)
        {
            res.CreatePermanentSessionId(req);
        }
    }

    public static string CreateSessionIds(IRequest httpReq = null, IResponse httpRes = null)
    {
        httpReq ??= HostContext.GetCurrentRequest();
        httpRes ??= httpReq.Response;
        return httpRes.CreateSessionIds(httpReq);
    }

    public static string GetSessionKey(IRequest httpReq = null)
    {
        var sessionId = httpReq.GetSessionId();
        return sessionId == null ? null : GetSessionKey(sessionId);
    }

    public static string GetSessionKey(string sessionId)
    {
        return sessionId == null ? null : IdUtils.CreateUrn<IAuthSession>(sessionId);
    }

    public static T GetOrCreateSession<T>(ICacheClient cache = null, IRequest httpReq = null, IResponse httpRes = null)
    {
        if (httpReq == null)
            httpReq = HostContext.GetCurrentRequest();

        var iSession = httpReq.GetSession(reload:false);
        if (iSession is T variable)
            return variable;

        var sessionId = httpReq.GetSessionId();
        var sessionKey = GetSessionKey(sessionId);
        if (sessionKey != null)
        {
            var session = (cache ?? httpReq.GetCacheClient()).Get<T>(sessionKey);
            if (!Equals(session, default(T)))
                return (T)HostContext.AppHost.OnSessionFilter(httpReq, (IAuthSession)session, sessionId);
        }

        return (T)CreateNewSession(httpReq, sessionId);
    }

    public static async  Task<T> GetOrCreateSessionAsync<T>(ICacheClientAsync cache = null, IRequest httpReq = null, IResponse httpRes = null, CancellationToken token=default)
    {
        if (httpReq == null)
            httpReq = HostContext.GetCurrentRequest();

        var iSession = await httpReq.GetSessionAsync(reload:false, token).ConfigAwait();
        if (iSession is T variable)
            return variable;

        var sessionId = httpReq.GetSessionId();
        var sessionKey = GetSessionKey(sessionId);
        if (sessionKey != null)
        {
            var session = await (cache ?? httpReq.GetCacheClientAsync()).GetAsync<T>(sessionKey, token).ConfigAwait();
            if (!Equals(session, default(T)))
                return (T)HostContext.AppHost.OnSessionFilter(httpReq, (IAuthSession)session, sessionId);
        }

        return (T)CreateNewSession(httpReq, sessionId);
    }

    /// <summary>
    /// Creates a new Session without an Id
    /// </summary>
    public static IAuthSession CreateNewSession(IRequest httpReq)
    {
        var session = AuthenticateService.CurrentSessionFactory();
        session.CreatedAt = session.LastModified = DateTime.UtcNow;
        session.OnCreated(httpReq);

        var authEvents = HostContext.TryResolve<IAuthEvents>();
        authEvents?.OnCreated(httpReq, session);

        return session;
    }

    /// <summary>
    /// Creates a new Session with the specified sessionId otherwise it's populated with a new
    /// generated Session Id that's 
    /// </summary>
    public static IAuthSession CreateNewSession(IRequest httpReq, string sessionId)
    {
        var session = AuthenticateService.CurrentSessionFactory();

        var appHost = HostContext.AppHost;
        if (appHost.HasPlugin<SessionFeature>())
        {
            session.Id = sessionId ?? CreateSessionIds(httpReq);
        }
        
        session.CreatedAt = session.LastModified = DateTime.UtcNow;
        session.OnCreated(httpReq);

        var authEvents = appHost.TryResolve<IAuthEvents>();
        authEvents?.OnCreated(httpReq, session);

        return session;
    }
}

public static class SessionFeatureUtils
{
    public static IAuthSession CreateNewSession(this IUserAuth user, IRequest httpReq)
    {
        var sessionId = HostContext.AppHost.CreateSessionId();
        var newSession = SessionFeature.CreateNewSession(httpReq, sessionId);
        var session = HostContext.AppHost.OnSessionFilter(httpReq, newSession, sessionId) ?? newSession;
        session.PopulateSession(user);
        return session;
    }
}