using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public abstract class OAuthProvider : AuthProvider, IOAuthProvider
{
    public override string Type => "oauth";

    public Func<string, AuthContext, Task<bool>> VerifyAccessTokenAsync { get; set; }

    public override Dictionary<string, string> Meta => VerifyAccessTokenAsync != null
        ? new Dictionary<string, string> {
            [Keywords.Allows] = Keywords.AccessTokenAuth,
        }
        : null;

        
    public OAuthProvider() { }

    public OAuthProvider(IAppSettings appSettings, string authRealm, string oAuthProvider,
        string consumerKeyName = nameof(ConsumerKey), string consumerSecretName = nameof(ConsumerSecret))
    {
        this.ConsumerKeyName = consumerKeyName;
        this.ConsumerSecretName = consumerSecretName;

        this.AuthRealm = appSettings.Get("OAuthRealm", authRealm);

        this.Provider = oAuthProvider;
        this.RedirectUrl = appSettings.GetString($"oauth.{Provider}.{nameof(RedirectUrl)}")
                           ?? FallbackConfig(appSettings.GetString($"oauth.{nameof(RedirectUrl)}"));
        this.CallbackUrl = appSettings.GetString($"oauth.{Provider}.{nameof(CallbackUrl)}")
                           ?? FallbackConfig(appSettings.GetString($"oauth.{nameof(CallbackUrl)}"));
        this.ConsumerKey = appSettings.GetString($"oauth.{Provider}.{consumerKeyName}");
        this.ConsumerSecret = appSettings.GetString($"oauth.{Provider}.{consumerSecretName}");

        this.RequestTokenUrl = appSettings.Get($"oauth.{Provider}.{nameof(RequestTokenUrl)}", authRealm + "oauth/request_token");
        this.AuthorizeUrl = appSettings.Get($"oauth.{Provider}.{nameof(AuthorizeUrl)}", authRealm + "oauth/authorize");
        this.AccessTokenUrl = appSettings.Get($"oauth.{Provider}.{nameof(AccessTokenUrl)}", authRealm + "oauth/access_token");
        this.SaveExtendedUserInfo = appSettings.Get($"oauth.{Provider}.{nameof(SaveExtendedUserInfo)}", true);

        this.UserProfileUrl = appSettings.GetNullableString($"oauth.{Provider}.{nameof(UserProfileUrl)}");
        this.VerifyTokenUrl = appSettings.GetNullableString($"oauth.{Provider}.{nameof(VerifyTokenUrl)}");

        this.OAuthUtils = new OAuthAuthorizer(this);
        this.AuthHttpGateway = new AuthHttpGateway();
    }

    public IAuthHttpGateway AuthHttpGateway { get; set; }

    protected readonly string ConsumerKeyName;
    protected readonly string ConsumerSecretName;
        
    public string ConsumerKey { get; set; }
    public string ConsumerSecret { get; set; }
    public string RequestTokenUrl { get; set; }
    public string AuthorizeUrl { get; set; }
    public string AccessTokenUrl { get; set; }

    public string UserProfileUrl { get; set; }
    public string VerifyTokenUrl { get; set; }
    public string IssuerSigningKeysUrl { get; set; }
    public OAuthAuthorizer OAuthUtils { get; set; }

    public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
    {
        if (request != null)
        {
            if (!LoginMatchesSession(session, request.UserName)) return false;
        }

        return session != null && session.IsAuthenticated && !string.IsNullOrEmpty(tokens?.AccessTokenSecret);
    }
        
    protected virtual void AssertValidState()
    {
        AssertConsumerKey();
        AssertConsumerSecret();
    }

    protected virtual void AssertConsumerSecret()
    {
        if (string.IsNullOrEmpty(ConsumerSecret))
            throw new Exception($"oauth.{Provider}.{ConsumerSecretName} is required");
    }

    protected virtual void AssertConsumerKey()
    {
        if (string.IsNullOrEmpty(ConsumerKey))
            throw new Exception($"oauth.{Provider}.{ConsumerKeyName} is required");
    }

    /// <summary>
    /// The entry point for all AuthProvider providers. Runs inside the AuthService so exceptions are treated normally.
    /// Overridable so you can provide your own Auth implementation.
    /// </summary>
    public abstract override Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token=default);

    /// <summary>
    /// Sets the CallbackUrl and session.ReferrerUrl if not set and initializes the session tokens for this AuthProvider
    /// </summary>
    /// <param name="authService"></param>
    /// <param name="session"></param>
    /// <param name="request"> </param>
    /// <returns></returns>
    protected IAuthTokens Init(IServiceBase authService, ref IAuthSession session, Authenticate request)
    {
        AssertValidState();

        if (this.CallbackUrl.IsNullOrEmpty())
            this.CallbackUrl = authService.Request.AbsoluteUri;

        if (RestoreSessionFromState == true)
        {
            var state = authService.Request.GetQueryStringOrForm(Keywords.State);
            if (!string.IsNullOrEmpty(state))
            {
                (authService.Request.Response as IHttpResponse)?.ClearCookies();
                authService.Request.CreateTemporarySessionId(state);
                session = authService.Request.GetSession(reload:true);
            }
        }

        session.ReferrerUrl = GetReferrerUrl(authService, session, request);

        var tokens = session.GetAuthTokens(Provider);
        if (tokens == null)
            session.AddAuthToken(tokens = new AuthTokens { Provider = Provider });

        return tokens;
    }

    public virtual Task LoadUserOAuthProviderAsync(IAuthSession userSession, IAuthTokens tokens) => Task.CompletedTask;
}