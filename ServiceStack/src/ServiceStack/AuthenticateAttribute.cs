#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// Protect access to this API to Authenticated Users only
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class AuthenticateAttribute : RequestFilterAsyncAttribute
{
    /// <summary>
    /// Restrict authentication to a specific <see cref="IAuthProvider"/>.
    /// For example, if this attribute should only permit access
    /// if the user is authenticated with <see cref="BasicAuthProvider"/>,
    /// you should set this property to <see cref="BasicAuthProvider.Name"/>.
    /// </summary>
    public string Provider { get; set; }

    /// <summary>
    /// Redirect the client to a specific URL if authentication failed.
    /// If this property is null, simply `401 Unauthorized` is returned.
    /// </summary>
    public string HtmlRedirect { get; set; }

    public AuthenticateAttribute(ApplyTo applyTo)
        : base(applyTo)
    {
        this.Priority = (int)RequestFilterPriority.Authenticate;
    }

    public AuthenticateAttribute()
        : this(ApplyTo.All) {}

    public AuthenticateAttribute(string provider)
        : this(ApplyTo.All)
    {
        this.Provider = provider;
    }

    public AuthenticateAttribute(ApplyTo applyTo, string provider)
        : this(applyTo)
    {
        this.Provider = provider;
    }

    public override async Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
    {
        if (AuthenticateService.AuthProviders == null)
            throw new InvalidOperationException(
                "The AuthService must be initialized by calling AuthService.Init to use an authenticate attribute");

        if (HostContext.HasValidAuthSecret(req))
            return;

        var authProviders = AuthenticateService.GetAuthProviders(this.Provider);
        if (authProviders.Length == 0)
        {
            await res.WriteError(req, requestDto, $"No registered Auth Providers found matching {this.Provider ?? "any"} provider").ConfigAwait();
            res.EndRequest();
            return;
        }
            
        req.PopulateFromRequestIfHasSessionId(requestDto);

        await PreAuthenticateAsync(req, authProviders).ConfigAwait();

        if (res.IsClosed)
            return;

        var session = await req.GetSessionAsync().ConfigAwait();
        if (session == null || !authProviders.Any(x => session.IsAuthorized(x.Provider)))
        {
            await authProviders[0].HandleFailedAuth(session, req, res).ConfigAwait();
        }
    }

    [Obsolete("Use AuthenticateAsync")]
    public static bool Authenticate(IRequest req, object requestDto=null, IAuthSession session=null, IAuthProvider[] authProviders=null)
    {
        if (HostContext.HasValidAuthSecret(req))
            return true;

        session ??= (req ?? throw new ArgumentNullException(nameof(req))).GetSession();
        authProviders ??= AuthenticateService.GetAuthProviders();
        var authValidate = HostContext.GetPlugin<AuthFeature>()?.OnAuthenticateValidate;
        var ret = authValidate?.Invoke(req);
        if (ret != null)
            return false;

        req.PopulateFromRequestIfHasSessionId(requestDto);

        if (!req.Items.ContainsKey(Keywords.HasPreAuthenticated))
        {
            var mockResponse = new BasicRequest().Response;
            req.Items[Keywords.HasPreAuthenticated] = true;
            foreach (var authWithRequest in authProviders.OfType<IAuthWithRequest>())
            {
                authWithRequest.PreAuthenticateAsync(req, mockResponse).Wait();
                if (mockResponse.IsClosed)
                    return false;
            }
            foreach (var authWithRequest in authProviders.OfType<IAuthWithRequestSync>())
            {
                authWithRequest.PreAuthenticate(req, mockResponse);
                if (mockResponse.IsClosed)
                    return false;
            }
        }
            
        return session != null && (authProviders.Length > 0
            ? authProviders.Any(x => session.IsAuthorized(x.Provider))
            : session.IsAuthenticated);
    }

    public static async Task<bool> AuthenticateAsync(IRequest req, object requestDto=null, IAuthSession session=null, IAuthProvider[] authProviders=null)
    {
        if (HostContext.HasValidAuthSecret(req))
            return true;

        session ??= await (req ?? throw new ArgumentNullException(nameof(req))).GetSessionAsync().ConfigAwait();
        authProviders ??= AuthenticateService.GetAuthProviders();
        var authValidate = HostContext.GetPlugin<AuthFeature>()?.OnAuthenticateValidate;
        var ret = authValidate?.Invoke(req);
        if (ret != null)
            return false;

        req.PopulateFromRequestIfHasSessionId(requestDto);

        if (!req.Items.ContainsKey(Keywords.HasPreAuthenticated))
        {
            //Unauthorized or invalid requests will terminate the response and return false
            var mockResponse = new BasicRequest().Response;
            req.Items[Keywords.HasPreAuthenticated] = true;
            foreach (var authWithRequest in authProviders.OfType<IAuthWithRequest>())
            {
                await authWithRequest.PreAuthenticateAsync(req, mockResponse).ConfigAwait();
                if (mockResponse.IsClosed)
                    return false;
            }
            foreach (var authWithRequest in authProviders.OfType<IAuthWithRequestSync>())
            {
                authWithRequest.PreAuthenticate(req, mockResponse);
                if (mockResponse.IsClosed)
                    return false;
            }
        }

        var sessionIsAuthenticated = session != null && (authProviders.Length > 0
            ? authProviders.Any(x => session.IsAuthorized(x.Provider))
            : session.IsAuthenticated);
        return sessionIsAuthenticated;
    }

    [Obsolete("Use AuthenticateAsync")]
    public static void AssertAuthenticated(IRequest req, object requestDto=null, IAuthSession session=null, IAuthProvider[] authProviders=null)
    {
        if (Authenticate(req, requestDto:requestDto, session:session))
            return;

        ThrowNotAuthenticated(req);
    }

    public static async Task AssertAuthenticatedAsync(IRequest req, object requestDto=null, IAuthSession session=null, IAuthProvider[] authProviders=null)
    {
        if (await AuthenticateAsync(req, requestDto:requestDto, session:session).ConfigAwait())
            return;

        ThrowNotAuthenticated(req);
    }

    public static void ThrowNotAuthenticated(IRequest req=null) => 
        throw new HttpError(401, nameof(HttpStatusCode.Unauthorized), ErrorMessages.NotAuthenticated.Localize(req));

    public static void ThrowInvalidRole(IRequest req=null) => 
        throw new HttpError(403, nameof(HttpStatusCode.Forbidden), ErrorMessages.InvalidRole.Localize(req));

    public static void ThrowInvalidPermission(IRequest req=null) => 
        throw new HttpError(403, nameof(HttpStatusCode.Forbidden), ErrorMessages.InvalidPermission.Localize(req));

    internal static async Task PreAuthenticateAsync(IRequest req, IEnumerable<IAuthProvider> authProviders)
    {
        var authValidate = HostContext.GetPlugin<AuthFeature>()?.OnAuthenticateValidate;
        var ret = authValidate?.Invoke(req);
        if (ret != null)
        {
            await req.Response.WriteToResponse(req, ret).ConfigAwait();
            return;
        }

        //Call before GetSession so Exceptions can bubble
        if (!req.Items.ContainsKey(Keywords.HasPreAuthenticated))
        {
            req.Items[Keywords.HasPreAuthenticated] = true;
            foreach (var authWithRequest in authProviders.OfType<IAuthWithRequest>())
            {
                await authWithRequest.PreAuthenticateAsync(req, req.Response).ConfigAwait();
                if (req.Response.IsClosed)
                    return;
            }
            foreach (var authWithRequest in authProviders.OfType<IAuthWithRequestSync>())
            {
                authWithRequest.PreAuthenticate(req, req.Response);
                if (req.Response.IsClosed)
                    return;
            }
        }
    }

    protected virtual Task HandleShortCircuitedErrors(IRequest req, IResponse res, object requestDto, 
        HttpStatusCode statusCode, string statusDescription=null)
    {
        if (HtmlRedirect != null)
            req.Items[nameof(AuthFeature.HtmlRedirect)] = HtmlRedirect;

        return HostContext.AppHost.HandleShortCircuitedErrors(req, res, requestDto, statusCode, statusDescription);
    }
        
    protected bool Equals(AuthenticateAttribute other)
    {
        return base.Equals(other) && string.Equals(Provider, other.Provider) && string.Equals(HtmlRedirect, other.HtmlRedirect);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AuthenticateAttribute)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * 397) ^ (Provider?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (HtmlRedirect?.GetHashCode() ?? 0);
            return hashCode;
        }
    }
}