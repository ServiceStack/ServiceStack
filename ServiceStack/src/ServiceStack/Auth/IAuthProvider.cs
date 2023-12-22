using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public interface IAuthProvider
{
    string Type { get; }
    Dictionary<string, string> Meta { get; }
    string AuthRealm { get; set; }
    string Provider { get; set; }
    string CallbackUrl { get; set; }

    /// <summary>
    /// Remove the Users Session
    /// </summary>
    Task<object> LogoutAsync(IServiceBase service, Authenticate request, CancellationToken token = default);

    /// <summary>
    /// The entry point for all AuthProvider providers. Runs inside the AuthService so exceptions are treated normally.
    /// Overridable so you can provide your own Auth implementation.
    /// </summary>
    public Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default);

    /// <summary>
    /// Determine if the current session is already authenticated with this AuthProvider
    /// </summary>
    bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null);
}

public interface IOAuthProvider : IAuthProvider
{
    IAuthHttpGateway AuthHttpGateway { get; set; }
    string ConsumerKey { get; set; }
    string ConsumerSecret { get; set; }
    string RequestTokenUrl { get; set; }
    string AuthorizeUrl { get; set; }
    string AccessTokenUrl { get; set; }
}

[Obsolete("Use IAuthWithRequestAsync")]
public interface IAuthWithRequestSync
{
    void PreAuthenticate(IRequest req, IResponse res);
}

public interface IAuthWithRequest
{
    Task PreAuthenticateAsync(IRequest req, IResponse res);
}

public interface IAuthResponseFilter
{
    /// <summary>
    /// Intercept successful Authenticate Request DTO requests 
    /// </summary>
    Task ExecuteAsync(AuthFilterContext authContext);

    /// <summary>
    /// Intercept successful OAuth redirect requests 
    /// </summary>
    Task ResultFilterAsync(AuthResultContext authContext, CancellationToken token=default);
}

[Obsolete("Use IUserSessionSourceAsync")]
public interface IUserSessionSource
{
    IAuthSession GetUserSession(string userAuthId);
}

public interface IUserSessionSourceAsync
{
    Task<IAuthSession> GetUserSessionAsync(string userAuthId, CancellationToken token=default);
}

public class AuthFilterContext
{
    /// <summary>
    /// Instance of AuthenticateService
    /// </summary>
    public AuthenticateService AuthService { get; internal set; }
    /// <summary>
    /// The current HTTP Request 
    /// </summary>
    public IRequest Request => AuthService.Request; 
    /// <summary>
    /// Selected Auth Provider for Request
    /// </summary>
    public IAuthProvider AuthProvider { get; internal set; }
    /// <summary>
    /// Authenticated Users Session
    /// </summary>
    public IAuthSession Session { get; internal set; }
    /// <summary>
    /// Authenticate Request DTO
    /// </summary>
    public Authenticate AuthRequest { get; internal set; }
    /// <summary>
    /// Authenticate Response DTO
    /// </summary>
    public AuthenticateResponse AuthResponse { get; internal set; }
    /// <summary>
    /// Optimal Session Referrer URL to use redirects
    /// </summary>
    public string ReferrerUrl { get; internal set; }
    /// <summary>
    /// If User was already authenticated
    /// </summary>
    public bool AlreadyAuthenticated { get; internal set; }
    /// <summary>
    /// If User Authenticated in this request
    /// </summary>
    public bool DidAuthenticate { get; internal set; }
    /// <summary>
    /// Original User Source (if exists) 
    /// </summary>
    public object UserSource { get; set; }
}

public class RegisterFilterContext
{
    public RegisterServiceBase RegisterService { get; internal set; }
    /// <summary>
    /// The current HTTP Request 
    /// </summary>
    public IRequest Request => RegisterService.Request; 
    /// <summary>
    /// Authenticated Users Session
    /// </summary>
    public IAuthSession Session { get; internal set; }
    /// <summary>
    /// Register Request DTO
    /// </summary>
    public Register Register { get; internal set; }
    /// <summary>
    /// RegisterResponse DTO
    /// </summary>
    public RegisterResponse RegisterResponse { get; internal set; }
    /// <summary>
    /// Optimal Session Referrer URL to use redirects
    /// </summary>
    public string ReferrerUrl { get; internal set; }
}

public class AuthResultContext
{
    /// <summary>
    /// The Response returned for this successful Auth Request 
    /// </summary>
    public IHttpResult Result { get; set; }
    /// <summary>
    /// Instance of Service used in this Request
    /// </summary>
    public IServiceBase Service { get; internal set; }
    /// <summary>
    /// Current HTTP Request Context
    /// </summary>
    public IRequest Request { get; internal set; }
    /// <summary>
    /// Authenticated Users Session
    /// </summary>
    public IAuthSession Session { get; internal set; }
}