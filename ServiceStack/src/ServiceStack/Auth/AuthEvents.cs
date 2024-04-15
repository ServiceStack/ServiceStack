using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public interface IAuthEvents
{
    /// <summary>
    /// Fired when a new Session is created
    /// </summary>
    void OnCreated(IRequest httpReq, IAuthSession session);

    /// <summary>
    /// Called when the user is registered or on the first OAuth login 
    /// </summary>
    void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase registrationService);

    /// <summary>
    /// Override with Custom Validation logic to Assert if User is allowed to Authenticate. 
    /// Returning a non-null response invalidates Authentication with IHttpResult response returned to client.
    /// </summary>
    IHttpResult Validate(IServiceBase authService, IAuthSession session, IAuthTokens tokens,
        Dictionary<string, string> authInfo);
        
    /// <summary>
    /// Called after the user has successfully authenticated 
    /// </summary>
    void OnAuthenticated(IRequest httpReq, IAuthSession session, IServiceBase authService, 
        IAuthTokens tokens, Dictionary<string, string> authInfo);
        
    /// <summary>
    /// Fired before the session is removed after the /auth/logout Service is called
    /// </summary>
    void OnLogout(IRequest httpReq, IAuthSession session, IServiceBase authService);
}

public interface IAuthEventsAsync
{
    /// <summary>
    /// Called when the user is registered or on the first OAuth login 
    /// </summary>
    Task OnRegisteredAsync(IRequest httpReq, IAuthSession session, IServiceBase registrationService, CancellationToken token=default);
        
    /// <summary>
    /// Override with Custom Validation logic to Assert if User is allowed to Authenticate. 
    /// Returning a non-null response invalidates Authentication with IHttpResult response returned to client.
    /// </summary>
    Task<IHttpResult> ValidateAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens,
        Dictionary<string, string> authInfo, CancellationToken token=default);

    /// <summary>
    /// Called after the user has successfully authenticated 
    /// </summary>
    Task OnAuthenticatedAsync(IRequest httpReq, IAuthSession session, IServiceBase authService, 
        IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token=default);

    /// <summary>
    /// Fired before the session is removed after the /auth/logout Service is called
    /// </summary>
    Task OnLogoutAsync(IRequest httpReq, IAuthSession session, IServiceBase authService, CancellationToken token=default);
}

/// <summary>
/// Convenient base class with empty virtual methods so subclasses only need to override the hooks they need.
/// </summary>
public class AuthEvents : IAuthEvents, IAuthEventsAsync
{
    public virtual void OnCreated(IRequest httpReq, IAuthSession session) {}
    public virtual Task OnCreatedAsync(IRequest httpReq, IAuthSession session, CancellationToken token = default) =>
        TypeConstants.EmptyTask;
    public virtual void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase registrationService) {}
    public virtual Task OnRegisteredAsync(IRequest httpReq, IAuthSession session, IServiceBase registrationService,
        CancellationToken token = default) => TypeConstants.EmptyTask;
    public virtual IHttpResult Validate(IServiceBase authService, IAuthSession session, IAuthTokens tokens,
        Dictionary<string, string> authInfo) => null;
    public virtual Task<IHttpResult> ValidateAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo,
        CancellationToken token = default) => ((IHttpResult) null).InTask();
    public virtual void OnAuthenticated(IRequest httpReq, IAuthSession session, IServiceBase authService, 
        IAuthTokens tokens, Dictionary<string, string> authInfo) {}
    public virtual Task OnAuthenticatedAsync(IRequest httpReq, IAuthSession session, IServiceBase authService, IAuthTokens tokens,
        Dictionary<string, string> authInfo, CancellationToken token = default) => TypeConstants.EmptyTask;
    public virtual void OnLogout(IRequest httpReq, IAuthSession session, IServiceBase authService) {}
    public virtual Task OnLogoutAsync(IRequest httpReq, IAuthSession session, IServiceBase authService, CancellationToken token = default) => TypeConstants.EmptyTask;
}

public class MultiAuthEvents : IAuthEvents, IAuthEventsAsync
{
    public MultiAuthEvents(IEnumerable<IAuthEvents> authEvents=null)
    {
        ChildEvents = [..authEvents ?? TypeConstants<IAuthEvents>.EmptyArray];
        ChildEventsAsync = ChildEvents.OfType<IAuthEventsAsync>().ToList();
    }

    public List<IAuthEvents> ChildEvents { get; private set; }
    public List<IAuthEventsAsync> ChildEventsAsync { get; private set; }

    public void OnCreated(IRequest httpReq, IAuthSession session)
    {
        ChildEvents.Each(x => x.OnCreated(httpReq, session));
    }

    public void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase registrationService)
    {
        ChildEvents.Each(x => x.OnRegistered(httpReq, session, registrationService));
    }

    public async Task OnRegisteredAsync(IRequest httpReq, IAuthSession session, IServiceBase registrationService,
        CancellationToken token = default)
    {
        foreach (var childEvent in ChildEventsAsync)
        {
            await childEvent.OnRegisteredAsync(httpReq, session, registrationService, token).ConfigAwait();
        }
    }

    public IHttpResult Validate(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo)
    {
        foreach (var authEvent in ChildEvents)
        {
            var ret = authEvent.Validate(authService, session, tokens, authInfo);
            if (ret != null)
                return ret;
        }
        return null;
    }

    public async Task<IHttpResult> ValidateAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo,
        CancellationToken token = default)
    {
        foreach (var authEvent in ChildEventsAsync)
        {
            var ret = await authEvent.ValidateAsync(authService, session, tokens, authInfo, token).ConfigAwait();
            if (ret != null)
                return ret;
        }
        return null;
    }

    public void OnAuthenticated(IRequest httpReq, IAuthSession session, IServiceBase authService, 
        IAuthTokens tokens, Dictionary<string, string> authInfo)
    {
        ChildEvents.Each(x => x.OnAuthenticated(httpReq, session, authService, tokens, authInfo));
    }

    public async Task OnAuthenticatedAsync(IRequest httpReq, IAuthSession session, IServiceBase authService, IAuthTokens tokens,
        Dictionary<string, string> authInfo, CancellationToken token = default)
    {
        foreach (var childEvent in ChildEventsAsync)
        {
            await childEvent.OnAuthenticatedAsync(httpReq, session, authService, tokens, authInfo, token).ConfigAwait();
        }
    }

    public void OnLogout(IRequest httpReq, IAuthSession session, IServiceBase authService)
    {
        ChildEvents.Each(x => x.OnLogout(httpReq, session, authService));
    }

    public async Task OnLogoutAsync(IRequest httpReq, IAuthSession session, IServiceBase authService, CancellationToken token = default)
    {
        foreach (var childEvent in ChildEventsAsync)
        {
            await childEvent.OnLogoutAsync(httpReq, session, authService, token).ConfigAwait();
        }
    }
}

public static class AuthEventsUtils
{
    public static async Task ExecuteOnRegisteredUserEventsAsync(this IAuthEvents authEvents, IAuthSession session, IServiceBase service)
    {
        if (authEvents == null)
            throw new ArgumentNullException(nameof(authEvents));
        if (session == null)
            throw new ArgumentNullException(nameof(session));
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        var httpReq = service.Request;
        session.OnRegistered(httpReq, session, service);
        if (session is IAuthSessionExtended sessionExt)
            await sessionExt.OnRegisteredAsync(httpReq, session, service).ConfigAwait();
        authEvents?.OnRegistered(httpReq, session, service);
        if (authEvents is IAuthEventsAsync asyncEvents)
            await asyncEvents.OnRegisteredAsync(httpReq, session, service).ConfigAwait();
    }
}