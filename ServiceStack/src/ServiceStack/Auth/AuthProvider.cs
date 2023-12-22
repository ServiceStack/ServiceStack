#pragma warning disable CS0618

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public abstract class AuthProvider : IAuthProvider, IAuthPlugin
{
    protected ILog Log;

    public virtual string Type => GetType().Name;
    public virtual Dictionary<string, string> Meta => null;

    public TimeSpan? SessionExpiry { get; set; }
    public string AuthRealm { get; set; }
    public string Provider { get; set; }
    public string CallbackUrl { get; set; }
    public string RedirectUrl { get; set; }

    public bool PersistSession { get; set; }
    public bool SaveExtendedUserInfo { get; set; }
        
    public bool? RestoreSessionFromState { get; set; }

    public Action<AuthUserSession, IAuthTokens, Dictionary<string, string>> LoadUserAuthFilter { get; set; }
    public Func<AuthUserSession, IAuthTokens, Dictionary<string, string>, CancellationToken, Task> LoadUserAuthInfoFilterAsync { get; set; }

    public Func<AuthContext, IHttpResult> CustomValidationFilter { get; set; }

    public Func<AuthContext, string, string> PreAuthUrlFilter = UrlFilter;
    public Func<AuthContext, string, string> AccessTokenUrlFilter = UrlFilter;
    public Func<AuthContext, string, string> SuccessRedirectUrlFilter = UrlFilter;
    public Func<AuthContext, string, string> FailedRedirectUrlFilter = UrlFilter;
    public Func<AuthContext, string, string> LogoutUrlFilter = UrlFilter;
        
    public Func<IAuthRepository, IUserAuth, IAuthTokens, bool> AccountLockedValidator { get; set; }

    public static string UrlFilter(AuthContext provider, string url) => url;

    public NavItem NavItem { get; set; }
        
    public int Sort { get; set; }
    public string Label { get; set; }
    public ImageInfo Icon { get; set; }

    public List<InputInfo> FormLayout { get; set; }

    public HashSet<string> ExcludeAuthInfoItems { get; set; } = new(new[]{ "user_id", "email", "username", "name", "first_name", "last_name", "email" }, StringComparer.OrdinalIgnoreCase);

    protected AuthProvider()
    {
        PersistSession = !(GetType().HasInterface(typeof(IAuthWithRequest)) || GetType().HasInterface(typeof(IAuthWithRequestSync)));
        Log = LogManager.GetLogger(GetType());
    }

    protected AuthProvider(IAppSettings appSettings, string authRealm, string authProvider)
        : this()
    {
        // Enhancement per https://github.com/ServiceStack/ServiceStack/issues/741
        this.AuthRealm = appSettings != null ? appSettings.Get("OAuthRealm", authRealm) : authRealm;

        this.Provider = authProvider;
        if (appSettings != null)
        {
            this.CallbackUrl = appSettings.GetString($"oauth.{authProvider}.CallbackUrl")
                               ?? FallbackConfig(appSettings.GetString("oauth.CallbackUrl"));
            this.RedirectUrl = appSettings.GetString($"oauth.{authProvider}.RedirectUrl")
                               ?? FallbackConfig(appSettings.GetString("oauth.RedirectUrl"));
        }
    }

    public IAuthEvents AuthEvents => HostContext.TryResolve<IAuthEvents>() ?? new AuthEvents();

    /// <summary>
    /// Allows specifying a global fallback config that if exists is formatted with the Provider as the first arg.
    /// E.g. this appSetting with the TwitterAuthProvider: 
    /// oauth.CallbackUrl="http://localhost:11001/auth/{0}"
    /// Would result in: 
    /// oauth.CallbackUrl="http://localhost:11001/auth/twitter"
    /// </summary>
    /// <returns></returns>
    protected string FallbackConfig(string fallback)
    {
        return fallback?.Fmt(Provider);
    }

    protected virtual AuthContext CreateAuthContext(IServiceBase authService=null, IAuthSession session=null, IAuthTokens tokens=null)
    {
        return new AuthContext {
            AuthProvider = this,
            Service = authService,
            Request = authService?.Request,
            Session = session,
            AuthTokens = tokens,
        };
    }

    /// <summary>
    /// Remove the Users Session
    /// </summary>
    public virtual async Task<object> LogoutAsync(IServiceBase service, Authenticate request, CancellationToken token=default)
    {
        var feature = HostContext.GetPlugin<AuthFeature>();

        var session = await service.GetSessionAsync(token: token).ConfigAwait();
        var referrerUrl = service.Request.GetReturnUrl()
                          ?? request.ReturnUrl 
                          ?? (feature.HtmlLogoutRedirect != null ? service.Request.ResolveAbsoluteUrl(feature.HtmlLogoutRedirect) : null)
                          ?? session.ReferrerUrl
                          ?? service.Request.GetHeader("Referer").NotLogoutUrl()
                          ?? this.RedirectUrl;

        session.OnLogout(service);
        if (service is IAuthSessionExtended sessionExt)
            await sessionExt.OnLogoutAsync(service, token).ConfigAwait();
        AuthEvents.OnLogout(service.Request, session, service);
        if (AuthEvents is IAuthEventsAsync asyncEvents)
            await asyncEvents.OnLogoutAsync(service.Request, session, service, token).ConfigAwait();

        await service.RemoveSessionAsync(token).ConfigAwait();

        if (feature is { DeleteSessionCookiesOnLogout: true })
        {
            service.Request.Response.DeleteSessionCookies();
            service.Request.Response.DeleteJwtCookie();
        }

        if (service.Request.ResponseContentType == MimeTypes.Html && !string.IsNullOrEmpty(referrerUrl))
            return service.Redirect(LogoutUrlFilter(CreateAuthContext(service,session), referrerUrl));

        return new AuthenticateResponse();
    }
        
    public virtual async Task<IHttpResult> OnAuthenticatedAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token=default)
    {
        session.AuthProvider = Provider;
        var asyncEvents = AuthEvents as IAuthEventsAsync;

        if (session is AuthUserSession userSession)
        {
            await LoadUserAuthInfoAsync(userSession, tokens, authInfo, token).ConfigAwait();
            HostContext.TryResolve<IAuthMetadataProvider>().SafeAddMetadata(tokens, authInfo);

            LoadUserAuthFilter?.Invoke(userSession, tokens, authInfo);
            if (LoadUserAuthInfoFilterAsync != null)
                await LoadUserAuthInfoFilterAsync(userSession, tokens, authInfo, token);
        }

        var hasTokens = tokens != null && authInfo != null;
        if (hasTokens && SaveExtendedUserInfo)
        {
            tokens.Items ??= new();

            foreach (var entry in authInfo)
            {
                if (ExcludeAuthInfoItems.Contains(entry.Key)) 
                    continue;

                tokens.Items[entry.Key] = entry.Value;
            }
        }

        var oauthRoles = tokens.GetRoles();
        session.Roles ??= new List<string>();
        if (oauthRoles.Length > 0)
            session.Roles.AddRange(oauthRoles);

        if (session is IAuthSessionExtended authSession)
        {
            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
            var failed = authSession.Validate(authService, session, tokens, authInfo)
                         ?? await authSession.ValidateAsync(authService, session, tokens, authInfo, token).ConfigAwait() 
                         ?? AuthEvents.Validate(authService, session, tokens, authInfo)
                         ?? (asyncEvents != null ? await asyncEvents.ValidateAsync(authService, session, tokens, authInfo, token).ConfigAwait() : null);
            if (failed != null)
            {
                await authService.RemoveSessionAsync(token).ConfigAwait();
                return failed;
            }
        }
            
        var authRepo = GetAuthRepositoryAsync(authService.Request);
        await using (authRepo as IAsyncDisposable)
        {
            if (CustomValidationFilter != null)
            {
                var ctx = new AuthContext
                {
                    Request = authService.Request,
                    Service = authService,
                    AuthProvider = this,
                    Session = session,
                    AuthTokens = tokens,
                    AuthInfo = authInfo,
                    AuthRepositoryAsync = authRepo,
                    AuthRepository = authRepo as IAuthRepository,
                };
                var response = CustomValidationFilter(ctx);
                if (response != null)
                {
                    await authService.RemoveSessionAsync(token).ConfigAwait();
                    return response;
                }
            }

            if (authRepo != null)
            {
                var failed = await ValidateAccountAsync(authService, authRepo, session, tokens, token).ConfigAwait();
                if (failed != null)
                {
                    await authService.RemoveSessionAsync(token).ConfigAwait();
                    return failed;
                }

                if (hasTokens)
                {
                    var authDetails = await authRepo.CreateOrMergeAuthSessionAsync(session, tokens, token).ConfigAwait();
                    session.UserAuthId = authDetails.UserAuthId.ToString();

                    var firstTimeAuthenticated = authDetails.CreatedDate == authDetails.ModifiedDate;
                    if (firstTimeAuthenticated)
                    {
                        session.OnRegistered(authService.Request, session, authService);
                        if (session is IAuthSessionExtended sessionExt)
                            await sessionExt.OnRegisteredAsync(authService.Request, session, authService, token).ConfigAwait();
                        AuthEvents.OnRegistered(authService.Request, session, authService);
                        if (asyncEvents != null)
                            await asyncEvents.OnRegisteredAsync(authService.Request, session, authService, token).ConfigAwait();
                    }
                }

                await authRepo.LoadUserAuthAsync(session, tokens, token).ConfigAwait();

                foreach (var oAuthToken in session.GetAuthTokens())
                {
                    var authProvider = AuthenticateService.GetAuthProvider(oAuthToken.Provider);
                    var userAuthProvider = authProvider as OAuthProvider;
                    userAuthProvider?.LoadUserOAuthProviderAsync(session, oAuthToken);
                }

                var httpRes = authService.Request.Response as IHttpResponse;
                if (session.UserAuthId != null)
                {
                    httpRes?.Cookies.AddPermanentCookie(HttpHeaders.XUserAuthId, session.UserAuthId);
                }
            }
            else
            {
                if (hasTokens)
                {
                    session.UserAuthId = CreateOrMergeAuthSession(session, tokens);
                }
            }

            // If OAuth Providers have their own roles, merge them and tag them with the Provider name
            if (oauthRoles.Length > 0)
                await authRepo.MergeRolesAsync(session.UserAuthId, Provider, oauthRoles, token: token).ConfigAwait();  
        }

        try
        {
            session.IsAuthenticated = true;
            session.OnAuthenticated(authService, session, tokens, authInfo);
            if (session is IAuthSessionExtended sessionExt)
                await sessionExt.OnAuthenticatedAsync(authService, session, tokens, authInfo, token).ConfigAwait();
            AuthEvents.OnAuthenticated(authService.Request, session, authService, tokens, authInfo);
            if (asyncEvents != null)
                await asyncEvents.OnAuthenticatedAsync(authService.Request, session, authService, tokens, authInfo, token).ConfigAwait();
        }
        finally
        {
            await this.SaveSessionAsync(authService, session, SessionExpiry, token).ConfigAwait();
            authService.Request.CompletedAuthentication();
        }

        return null;
    }

    protected virtual IAuthRepository GetAuthRepository(IRequest req)
    {
        return HostContext.AppHost.GetAuthRepository(req);
    }

    protected virtual IAuthRepositoryAsync GetAuthRepositoryAsync(IRequest req)
    {
        return HostContext.AppHost.GetAuthRepositoryAsync(req);
    }

    // Keep in-memory map of userAuthId's when no IAuthRepository exists 
    private static long transientUserAuthId;
    static readonly ConcurrentDictionary<string, long> transientUserIdsMap = new();

    // Merge tokens into session when no IAuthRepository exists
    public virtual string CreateOrMergeAuthSession(IAuthSession session, IAuthTokens tokens)
    {
        if (session.UserName.IsNullOrEmpty())
            session.UserName = tokens.UserName;
        if (session.DisplayName.IsNullOrEmpty())
            session.DisplayName = tokens.DisplayName;
        if (session.Email.IsNullOrEmpty())
            session.Email = tokens.Email;

        var oAuthTokens = session.GetAuthTokens(tokens.Provider);
        if (oAuthTokens != null && oAuthTokens.UserId == tokens.UserId)
        {
            if (!oAuthTokens.UserName.IsNullOrEmpty())
                session.UserName = oAuthTokens.UserName;
            if (!oAuthTokens.DisplayName.IsNullOrEmpty())
                session.DisplayName = oAuthTokens.DisplayName;
            if (!oAuthTokens.Email.IsNullOrEmpty())
                session.Email = oAuthTokens.Email;
            if (!oAuthTokens.FirstName.IsNullOrEmpty())
                session.FirstName = oAuthTokens.FirstName;
            if (!oAuthTokens.LastName.IsNullOrEmpty())
                session.LastName = oAuthTokens.LastName;
        }

        var key = tokens.Provider + ":" + (tokens.UserId ?? tokens.UserName);
        return transientUserIdsMap.GetOrAdd(key,
            k => Interlocked.Increment(ref transientUserAuthId)).ToString(CultureInfo.InvariantCulture);
    }

    [Obsolete("Use LoadUserAuthInfoAsync")]
    protected void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo) { }

    protected virtual Task LoadUserAuthInfoAsync(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token=default)
    {
        return TypeConstants.EmptyTask;
    }

    protected static bool LoginMatchesSession(IAuthSession session, string userName)
    {
        if (session == null || userName == null) return false;
        var isEmail = userName.IndexOf('@') >= 0;
        if (isEmail)
        {
            if (!userName.EqualsIgnoreCase(session.Email))
                return false;
        }
        else
        {
            if (!userName.EqualsIgnoreCase(session.UserAuthName))
                return false;
        }
        return true;
    }

    public abstract bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null);

    //public virtual object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request) {}
    public abstract Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default);

    public virtual Task OnFailedAuthentication(IAuthSession session, IRequest httpReq, IResponse httpRes)
    {
        httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
        httpRes.AddHeader(HttpHeaders.WwwAuthenticate, "{0} realm=\"{1}\"".Fmt(this.Provider, this.AuthRealm));
        return HostContext.AppHost.HandleShortCircuitedErrors(httpReq, httpRes, httpReq.Dto);
    }

    protected virtual async Task<bool> UserNameAlreadyExistsAsync(IAuthRepositoryAsync authRepo, IUserAuth userAuth, IAuthTokens tokens = null, CancellationToken token=default)
    {
        if (tokens?.UserName != null)
        {
            var userWithUserName = await authRepo.GetUserAuthByUserNameAsync(tokens.UserName, token).ConfigAwait();
            if (userWithUserName == null)
                return false;

            var isAnotherUser = userAuth == null || (userAuth.Id != userWithUserName.Id);
            return isAnotherUser;
        }
        return false;
    }

    protected virtual async Task<bool> EmailAlreadyExistsAsync(IAuthRepositoryAsync authRepo, IUserAuth userAuth, IAuthTokens tokens = null, CancellationToken token=default)
    {
        if (tokens?.Email != null)
        {
            var userWithEmail = await authRepo.GetUserAuthByUserNameAsync(tokens.Email, token).ConfigAwait();
            if (userWithEmail == null) 
                return false;

            var isAnotherUser = userAuth == null || (userAuth.Id != userWithEmail.Id);
            return isAnotherUser;
        }
        return false;
    }

    protected virtual string GetAuthRedirectUrl(IServiceBase authService, IAuthSession session)
    {
        return session.ReferrerUrl;
    }

    public virtual Task<bool> IsAccountLockedAsync(IAuthRepositoryAsync authRepoAsync, IUserAuth userAuth, IAuthTokens tokens=null, CancellationToken token=default)
    {
        if (authRepoAsync is IAuthRepository authRepo && AccountLockedValidator != null)
            return AccountLockedValidator(authRepo, userAuth, tokens).InTask();
            
        return (userAuth?.LockedDate != null).InTask();
    }
        
    protected virtual async Task<IHttpResult> ValidateAccountAsync(IServiceBase authService, IAuthRepositoryAsync authRepo, IAuthSession session, IAuthTokens tokens, CancellationToken token=default)
    {
        var userAuth = await authRepo.GetUserAuthAsync(session, tokens, token).ConfigAwait();
        var ctx = CreateAuthContext(authService, session, tokens);

        var authFeature = HostContext.GetPlugin<AuthFeature>();

        if (authFeature is { ValidateUniqueUserNames: true } && await UserNameAlreadyExistsAsync(authRepo, userAuth, tokens, token).ConfigAwait())
        {
            return authService.Redirect(FailedRedirectUrlFilter(ctx, GetReferrerUrl(authService, session).SetParam("f", "UserNameAlreadyExists")));
        }

        if (authFeature is { ValidateUniqueEmails: true } && await EmailAlreadyExistsAsync(authRepo, userAuth, tokens, token).ConfigAwait())
        {
            return authService.Redirect(FailedRedirectUrlFilter(ctx, GetReferrerUrl(authService, session).SetParam("f", "EmailAlreadyExists")));
        }

        if (await IsAccountLockedAsync(authRepo, userAuth, tokens, token).ConfigAwait())
        {
            return authService.Redirect(FailedRedirectUrlFilter(ctx, GetReferrerUrl(authService, session).SetParam("f", "AccountLocked")));
        }

        return null;
    }

    protected virtual string GetReferrerUrl(IServiceBase authService, IAuthSession session, Authenticate request = null)
    {
        if (request == null)
            request = authService.Request.Dto as Authenticate;

        var referrerUrl = authService.Request.GetReturnUrl() ?? session.ReferrerUrl;
        if (!string.IsNullOrEmpty(referrerUrl))
            return referrerUrl;

        referrerUrl = authService.Request.GetHeader("Referer");
        if (!string.IsNullOrEmpty(referrerUrl))
            return referrerUrl;

        var requestUri = authService.Request.AbsoluteUri;
        if (requestUri.IndexOf("/auth", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            referrerUrl = this.RedirectUrl
                          ?? authService.Request.GetBaseUrl()
                          ?? requestUri.InferBaseUrl();
        }

        return referrerUrl;
    }
        
    protected virtual object ConvertToClientError(object failedResult, bool isHtml)
    {
        if (isHtml) 
            return failedResult;
        if (failedResult is not IHttpResult httpRes) 
            return failedResult;
        if (!httpRes.Headers.TryGetValue(HttpHeaders.Location, out var location)) 
            return failedResult;
            
        var parts = location.SplitOnLast("f=");
        if (parts.Length == 2)
            return new HttpError(HttpStatusCode.BadRequest, parts[1], parts[1].SplitCamelCase());
            
        return failedResult;
    }

    public virtual void Register(IAppHost appHost, AuthFeature feature)
    {
        RestoreSessionFromState ??= appHost.Config.UseSameSiteCookies == true;
    }

    public IUserAuthRepositoryAsync GetUserAuthRepositoryAsync(IRequest request)
    {
        var authRepo = (IUserAuthRepositoryAsync)HostContext.AppHost.GetAuthRepositoryAsync(request);
        if (authRepo == null)
            throw new Exception(ErrorMessages.AuthRepositoryNotExists);

        return authRepo;
    }
}

public class AuthContext : IMeta
{
    public IRequest Request { get; set; }
    public IServiceBase Service { get; set; }
    public AuthProvider AuthProvider { get; set; }
    public AuthProviderSync AuthProviderSync { get; set; }
    public IAuthSession Session { get; set; }
    public IAuthTokens AuthTokens { get; set; }
    public Dictionary<string, string> AuthInfo { get; set; }
    public IAuthRepository AuthRepository { get; set; }
    public IAuthRepositoryAsync AuthRepositoryAsync { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}