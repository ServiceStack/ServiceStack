#if !NETCORE
using System.Configuration;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

/// <summary>
/// Inject logic into existing services by introspecting the request and injecting your own
/// validation logic. Exceptions thrown will have the same behaviour as if the service threw it.
/// 
/// If a non-null object is returned the request will short-circuit and return that response.
/// </summary>
/// <param name="service">The instance of the service</param>
/// <param name="httpMethod">GET,POST,PUT,DELETE</param>
/// <param name="requestDto"></param>
/// <returns>Response DTO; non-null will short-circuit execution and return that response</returns>
public delegate object ValidateFn(IServiceBase service, string httpMethod, object requestDto);
public delegate Task<object> ValidateAsyncFn(IServiceBase service, string httpMethod, object requestDto);
public delegate Task<object> ValidateRequestAsyncFn(IRequest service, object requestDto);

[DefaultRequest(typeof(Authenticate))]
[ErrorView(nameof(ServiceStack.Authenticate.ErrorView))]
public class AuthenticateService : Service
{
    public const string BasicProvider = "basic";
    public const string ApiKeyProvider = "apikey";
    public const string JwtProvider = "jwt";
    public const string CredentialsProvider = "credentials";
    public const string WindowsAuthProvider = "windowsauth";
    public const string CredentialsAliasProvider = "login";
    public const string LogoutAction = "logout";
    public const string DigestProvider = "digest";
    public const string IdentityProvider = "identity";

    public static Func<IAuthSession> CurrentSessionFactory { get; set; }
    public static ValidateFn ValidateFn { get; set; }

    public static string DefaultOAuthProvider { get; private set; }
    public static string DefaultOAuthRealm { get; private set; }
    public static string HtmlRedirect { get; internal set; }
        
    public static string HtmlRedirectAccessDenied { get; internal set; }
    public static string HtmlRedirectReturnParam { get; internal set; }
    public static bool HtmlRedirectReturnPathOnly { get; internal set; }
        
    public static Func<AuthFilterContext, object> AuthResponseDecorator { get; internal set; }
    internal static IAuthProvider[] AuthProviders;
    internal static IAuthWithRequest[] AuthWithRequestProviders;
#pragma warning disable CS0618
    internal static IAuthWithRequestSync[] AuthWithRequestSyncProviders;
#pragma warning restore CS0618
    internal static IAuthResponseFilter[] AuthResponseFilters;

    static AuthenticateService()
    {
        Reset();
    }

    internal static void Reset()
    {
        CurrentSessionFactory = () => new AuthUserSession();
        AuthProviders = TypeConstants<IAuthProvider>.EmptyArray;
        AuthResponseFilters = TypeConstants<IAuthResponseFilter>.EmptyArray;
    }

    /// <summary>
    /// Get AuthProviders Registered in AuthFeature Plugin.
    /// </summary>
    /// <param name="provider">specific provider, or null for all providers</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static IAuthProvider[] GetAuthProviders(string provider = null)
    {
        if (AuthProviders == null || AuthProviders.Length == 0)
            return TypeConstants<IAuthProvider>.EmptyArray;

        if (provider != null)
        {
            var matchingOAuthProviders = AuthProviders.Where(x =>
                string.IsNullOrEmpty(provider)
                || x.Provider == provider).ToArray();

            return matchingOAuthProviders;
        }

        return AuthProviders;
    }

#pragma warning disable 618
    [Obsolete("Use GetUserSessionSourceAsync()")]
    public static IUserSessionSource GetUserSessionSource()
    {
        var userSessionSource = HostContext.TryResolve<IUserSessionSource>();
        if (userSessionSource != null)
            return userSessionSource;

        if (AuthProviders != null)
        {
            foreach (var authProvider in AuthProviders)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (authProvider is IUserSessionSource sessionSource) //don't remove
                    return sessionSource;
            }
        }

        return null;
    }

    public static IUserSessionSourceAsync GetUserSessionSourceAsync()
    {
        var userSessionSource = HostContext.TryResolve<IUserSessionSourceAsync>();
        if (userSessionSource != null)
            return userSessionSource;

        if (AuthProviders != null)
        {
            foreach (var authProvider in AuthProviders)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (authProvider is IUserSessionSourceAsync sessionSource) //don't remove
                    return sessionSource;
            }
        }

        var sync = GetUserSessionSource();
        if (sync != null)
            return new UserSessionSourceSyncWrapper(sync);

        return null;
    }

    class UserSessionSourceSyncWrapper(IUserSessionSource source) : IUserSessionSourceAsync
    {
        public Task<IAuthSession> GetUserSessionAsync(string userAuthId, IRequest request=null, CancellationToken token = default)
        {
            return source.GetUserSession(userAuthId, request).InTask();
        }
    }
#pragma warning restore 618

    /// <summary>
    /// Get specific AuthProvider
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static IAuthProvider GetAuthProvider(string provider)
    {
        if (string.IsNullOrEmpty(provider))
            throw new ArgumentNullException(nameof(provider));
            
        if (AuthProviders.Length == 0)
            return null;
        if (provider == LogoutAction)
            return AuthProviders[0];

        foreach (var authConfig in AuthProviders)
        {
            if (string.Compare(authConfig.Provider, provider, StringComparison.OrdinalIgnoreCase) == 0)
                return authConfig;
        }

        return null;
    }

    public static JwtAuthProviderReader GetJwtAuthProvider() => GetAuthProvider(JwtAuthProviderReader.Name) as JwtAuthProviderReader;

    public static JwtAuthProviderReader GetRequiredJwtAuthProvider()
    {
        var jwtProvider = GetJwtAuthProvider();
        if (jwtProvider == null)
            throw new NotSupportedException("JwtAuthProvider is required but was not registered in AuthFeature's AuthProviders");

        return jwtProvider;
    }

    public static void Init(Func<IAuthSession> sessionFactory, params IAuthProvider[] authProviders)
    {
        if (authProviders.Length == 0)
            throw new ArgumentNullException(nameof(authProviders));

        DefaultOAuthProvider = authProviders[0].Provider;
        DefaultOAuthRealm = authProviders[0].AuthRealm;

        AuthProviders = authProviders;
        AuthWithRequestProviders = authProviders.OfType<IAuthWithRequest>().ToArray();
#pragma warning disable CS0618
        AuthWithRequestSyncProviders = authProviders.OfType<IAuthWithRequestSync>().ToArray();
#pragma warning restore CS0618
        AuthResponseFilters = authProviders.OfType<IAuthResponseFilter>().ToArray();

        if (sessionFactory != null)
            CurrentSessionFactory = sessionFactory;
    }

    private void AssertAuthProviders()
    {
        if (AuthProviders == null || AuthProviders.Length == 0)
            throw new ConfigurationErrorsException("No OAuth providers have been registered in your AppHost.");
    }

    public void Options(Authenticate request) { }

    public Task<object> GetAsync(Authenticate request)
    {
        var allowGetAuthRequests = HostContext.AssertPlugin<AuthFeature>().AllowGetAuthenticateRequests;

        // null == allow all Auth Requests or 
        if (allowGetAuthRequests != null && !allowGetAuthRequests(Request))
            throw new NotSupportedException("GET Authenticate requests are disabled, to enable set AuthFeature.AllowGetAuthenticateRequests = req => true");
            
        return PostAsync(request);
    }

    [Obsolete("Use PostAsync")]
    public object Post(Authenticate request)
    {
        try
        {
            var task = PostAsync(request);
            var response = task.GetResult();
            return response;
        }
        catch (Exception e)
        {
            throw e.UnwrapIfSingleException();
        }
    }

    public Task<object> AnyAsync(AuthenticateLogout request)
    {
        return PostAsync(new Authenticate { provider = LogoutAction });
    }

    public async Task<object> PostAsync(Authenticate request)
    {
        AssertAuthProviders();

        if (ValidateFn != null)
        {
            var validationResponse = ValidateFn(this, Request.Verb, request);
            if (validationResponse != null) return validationResponse;
        }

        var authFeature = GetPlugin<AuthFeature>();

        if (request.RememberMe.HasValue)
        {
            var opt = request.RememberMe.GetValueOrDefault(false)
                ? SessionOptions.Permanent
                : SessionOptions.Temporary;

            Request.AddSessionOptions(opt);
        }

        var provider = request.provider ?? AuthProviders[0].Provider;
        if (provider == CredentialsAliasProvider)
            provider = CredentialsProvider;

        var authProvider = GetAuthProvider(provider);
        if (authProvider == null)
            throw HttpError.NotFound(ErrorMessages.UnknownAuthProviderFmt.LocalizeFmt(Request, provider.SafeInput()));

        if (LogoutAction.EqualsIgnoreCase(request.provider))
        {
            return await HandleLogoutAsync(request, authProvider);
        }

        if (authProvider is IAuthWithRequest && !base.Request.IsInProcessRequest())
        {
            //IAuthWithRequest normally doesn't call Authenticate directly, but they can to return Auth Info
            //But as AuthenticateService doesn't have [Authenticate] we need to call it manually
            await new AuthenticateAttribute().ExecuteAsync(base.Request, base.Response, request);
            if (base.Response.IsClosed)
                return null;
        }

        var session = await this.GetSessionAsync().ConfigAwait();

        var isHtml = base.Request.ResponseContentType.MatchesContentType(MimeTypes.Html);
        try
        {
            var response = await AuthenticateAsync(request, provider, session, authProvider).ConfigAwait();

            // The above Authenticate call may end an existing session and create a new one so we need
            // to refresh the current session reference.
            session = await this.GetSessionAsync().ConfigAwait();

            if (request.provider == null && !session.IsAuthenticated)
                throw HttpError.Unauthorized(ErrorMessages.NotAuthenticated.Localize(Request));

            var returnUrl = Request.GetReturnUrl();
            var referrerUrl = returnUrl
                              ?? session.ReferrerUrl
                              ?? this.Request.GetHeader(HttpHeaders.Referer)
                              ?? authProvider.CallbackUrl;

            if (authFeature != null)
            {
                if (!string.IsNullOrEmpty(returnUrl))
                    authFeature.ValidateRedirectLinks(Request, referrerUrl);
            }

            var manageRoles = AuthRepositoryAsync as IManageRolesAsync;

            var alreadyAuthenticated = response == null;
            response ??= new AuthenticateResponse {
                UserId = session.UserAuthId,
                UserName = session.UserAuthName,
                DisplayName = session.DisplayName
                    ?? session.UserName 
                    ?? $"{session.FirstName} {session.LastName}".Trim(),
                SessionId = session.Id,
                ReferrerUrl = referrerUrl,
                AuthProvider = session.AuthProvider,
            };

            if (response is AuthenticateResponse authResponse)
            {
                authResponse.ProfileUrl ??= session.GetProfileUrl();

                if (session.UserAuthId != null && authFeature != null)
                {
                    if (authFeature.IncludeRolesInAuthenticateResponse)
                    {
                        var authSession = authFeature.AuthSecretSession;
                        if (authSession != null && session.UserAuthName == authSession.UserAuthName && session.UserAuthId == authSession.UserAuthId)
                        {
                            authResponse.Roles = session.Roles;
                            authResponse.Permissions = session.Permissions;
                        }
                            
                        authResponse.Roles ??= (manageRoles != null
                            ? (await manageRoles.GetRolesAsync(session.UserAuthId).ConfigAwait())?.ToList()
                            : session.Roles);
                        authResponse.Permissions ??= (manageRoles != null
                            ? (await manageRoles.GetPermissionsAsync(session.UserAuthId).ConfigAwait())?.ToList()
                            : session.Permissions);

                        if (authResponse.Roles?.Contains(RoleNames.Admin) == true)
                            authResponse.Roles.AddDistinctRange(HostContext.Metadata.GetAllRoles());
                    }
                    if (authFeature.IncludeOAuthTokensInAuthenticateResponse && AuthRepositoryAsync != null)
                    {
                        var authDetails = await AuthRepositoryAsync.GetUserAuthDetailsAsync(session.UserAuthId).ConfigAwait();
                        if (authDetails?.Count > 0)
                        {
                            authResponse.Meta ??= new Dictionary<string, string>();
                            foreach (var authDetail in authDetails.Where(x => x.AccessTokenSecret != null))
                            {
                                authResponse.Meta[authDetail.Provider + "-tokens"] = authDetail.AccessTokenSecret + 
                                    (authDetail.AccessToken != null ? ':' + authDetail.AccessToken : ""); 
                            }
                        }
                    }
                }

                var authCtx = new AuthFilterContext {
                    AuthService = this,
                    AuthProvider = authProvider,
                    AuthRequest = request,
                    AuthResponse = authResponse,
                    ReferrerUrl = referrerUrl,
                    Session = session,
                    AlreadyAuthenticated = alreadyAuthenticated,
                    DidAuthenticate = Request.Items.ContainsKey(Keywords.DidAuthenticate),
                };

                foreach (var responseFilter in AuthResponseFilters)
                {
                    await responseFilter.ExecuteAsync(authCtx);
                }

                if (AuthResponseDecorator != null)
                {
                    var authDecoratorResponse = AuthResponseDecorator(authCtx);
                    if (authDecoratorResponse != response)
                        return authDecoratorResponse;                        
                }
            }

            if (isHtml && request.provider != null)
            {
                if (alreadyAuthenticated)
                    return this.Redirect(referrerUrl.SetParam("s", "0"));

                if (response is not IHttpResult && !string.IsNullOrEmpty(referrerUrl))
                {
                    return new HttpResult(response) {
                        Location = referrerUrl
                    };
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            if (isHtml && Request.GetErrorView() != null)
                return ex;

            if (ex is HttpError)
            {
                var errorReferrerUrl = this.Request.GetReturnUrl() ?? this.Request.GetHeader(HttpHeaders.Referer);
                if (isHtml && errorReferrerUrl != null && Request.GetParam(Keywords.NoRedirect) == null)
                {
                    errorReferrerUrl = errorReferrerUrl.SetParam("f", ex.Message.Localize(Request));
                    return HttpResult.Redirect(errorReferrerUrl);
                }
            }

            throw;
        }
    }

    private async Task<object> HandleLogoutAsync(Authenticate request, IAuthProvider authProvider)
    {
        var feature = AssertPlugin<AuthFeature>();
        foreach (var handler in feature.OnLogoutAsync)
        {
            try
            {
                await handler(Request).ConfigAwait();
            }
            catch (Exception e)
            {
                LogManager.GetLogger(GetType()).Error("Error in OnLogoutAsync: " + e.Message, e);
            }
        }
        return await authProvider.LogoutAsync(this, request).ConfigAwait();
    }

    [Obsolete("Use AuthenticateAsync")]
    public AuthenticateResponse Authenticate(Authenticate request)
    {
        var task = AuthenticateAsync(request);
        var ret = task.GetResult();
        return ret;
    }

    /// <summary>
    /// Public API entry point to authenticate via code
    /// </summary>
    /// <returns>null; if already authenticated otherwise a populated instance of AuthResponse</returns>
    public async Task<AuthenticateResponse> AuthenticateAsync(Authenticate request, CancellationToken token=default)
    {
        //Remove HTML Content-Type to avoid auth providers issuing browser re-directs
        var hold = this.Request.ResponseContentType;
        try
        {
            this.Request.ResponseContentType = MimeTypes.PlainText;

            if (request.RememberMe.HasValue)
            {
                var opt = request.RememberMe.GetValueOrDefault(false)
                    ? SessionOptions.Permanent
                    : SessionOptions.Temporary;

                base.Request.AddSessionOptions(opt);
            }

            var provider = request.provider ?? AuthProviders[0].Provider;
            var oAuthConfig = GetAuthProvider(provider);
            if (oAuthConfig == null)
                throw HttpError.NotFound(ErrorMessages.UnknownAuthProviderFmt.LocalizeFmt(Request,provider.SafeInput()));

            if (LogoutAction.EqualsIgnoreCase(request.provider))
            {
                return await HandleLogoutAsync(request, oAuthConfig).ConfigAwait() as AuthenticateResponse;
            }

            var result = await AuthenticateAsync(request, provider, 
                await this.GetSessionAsync(token: token).ConfigAwait(), oAuthConfig, token).ConfigAwait();
            if (result is HttpError httpError)
                throw httpError;

            return result as AuthenticateResponse;
        }
        finally
        {
            this.Request.ResponseContentType = hold;
        }
    }

    /// <summary>
    /// The specified <paramref name="session"/> may change as a side-effect of this method. If
    /// subsequent code relies on current <see cref="IAuthSession"/> data be sure to reload
    /// the session instance via <see cref="ServiceExtensions.GetSession(IServiceBase,bool)"/>.
    /// </summary>
    private async Task<object> AuthenticateAsync(Authenticate request, string provider, IAuthSession session, IAuthProvider oAuthConfig, CancellationToken token=default)
    {
        if (request.provider == null && request.UserName == null)
            return null; //Just return sessionInfo if no provider or username is given

        var authFeature = GetPlugin<AuthFeature>();
        if (authFeature?.HasSessionFeature == true && oAuthConfig is not IAuthWithRequest)
        {
            var generateNewCookies = authFeature.GenerateNewSessionCookiesOnAuthentication
                 //keep existing session during OAuth flow
                 && string.IsNullOrEmpty(Request.QueryString["oauth_token"]) 
                 && string.IsNullOrEmpty(Request.QueryString["state"]);

            if (generateNewCookies)
                await Request.GenerateNewSessionCookiesAsync(session, token).ConfigAwait();
        }

        var response = await oAuthConfig.AuthenticateAsync(this, session, request, token).ConfigAwait();

        return response;
    }
}