using System;
using System.Collections.Generic;
using ServiceStack.Configuration;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public abstract class OAuthProviderSync : AuthProviderSync, IOAuthProvider
{
    public override string Type => "oauth";

    public Func<string, bool> VerifyAccessToken { get; set; }

    public override Dictionary<string, string> Meta => VerifyAccessToken != null
        ? new Dictionary<string, string> {
            [Keywords.Allows] = Keywords.AccessTokenAuth,
        }
        : null;

        
    public OAuthProviderSync() { }

    public OAuthProviderSync(IAppSettings appSettings, string authRealm, string oAuthProvider)
        : this(appSettings, authRealm, oAuthProvider, nameof(ConsumerKey), nameof(ConsumerSecret)) { }

    public OAuthProviderSync(IAppSettings appSettings, string authRealm, string oAuthProvider,
        string consumerKeyName, string consumerSecretName)
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
    /// <param name="authService"></param>
    /// <param name="session"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public abstract override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request);

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

        // Sessions are never restored from the OAuth state param, which would let anyone with a callback URL
        // for their own session sign a victim into it. When RestoreSessionFromState is enabled,
        // AuthenticateService bounces callbacks through a same-site page so SameSite=Strict session cookies are sent.

        session.ReferrerUrl = GetReferrerUrl(authService, session, request);

        var tokens = session.GetAuthTokens(Provider);
        if (tokens == null)
            session.AddAuthToken(tokens = new AuthTokens { Provider = Provider });

        return tokens;
    }

    protected const string OAuthStateKey = "oauthstate";

    /// <summary>
    /// Session key used to store the pending OAuth state, scoped per provider so concurrent
    /// sign-in flows with different providers don't overwrite each other's state
    /// </summary>
    protected virtual string OAuthStateSessionKey => OAuthStateKey + ":" + Provider;

    /// <summary>
    /// Authenticating with an AccessToken links the external identity to the current session,
    /// only allow it from non-GET requests so it can't be triggered from a cross-site link and
    /// don't allow cross-site requests to link an identity to an already authenticated session
    /// </summary>
    protected virtual bool IsAccessTokenAuthAllowed(IServiceBase authService, IAuthSession session)
    {
        var req = authService.Request;
        if (string.Equals(req.Verb, HttpMethods.Get, StringComparison.OrdinalIgnoreCase))
            return false;
        return OAuthProvider.IsTrustedOrigin(req);
    }

    protected virtual string CreateOAuthState(IAuthSession session, IAuthTokens tokens = null)
    {
        var oauthState = SessionExtensions.CreateRandomSessionId();

        if (session is AuthUserSession authSession)
            (authSession.Meta ??= new Dictionary<string, string>())[OAuthStateSessionKey] = oauthState;
        else if (tokens != null)
            (tokens.Items ??= new Dictionary<string, string>())[OAuthStateSessionKey] = oauthState;

        return oauthState;
    }

    protected virtual bool ValidateOAuthState(IRequest httpRequest, IAuthSession session, IAuthTokens tokens = null)
    {
        var state = httpRequest.GetQueryStringOrForm(Keywords.State);
        if (string.IsNullOrEmpty(state))
            return false;

        string expectedState = null;
        if (session is AuthUserSession authSession)
        {
            if (authSession.Meta == null || !authSession.Meta.TryGetValue(OAuthStateSessionKey, out expectedState))
                return false;

            if (string.IsNullOrEmpty(expectedState) || !CryptUtils.FixedTimeEquals(state, expectedState))
                return false;

            authSession.Meta.Remove(OAuthStateSessionKey);
            return true;
        }

        if (tokens != null)
        {
            if (tokens.Items == null || !tokens.Items.TryGetValue(OAuthStateSessionKey, out expectedState))
                return false;

            if (string.IsNullOrEmpty(expectedState) || !CryptUtils.FixedTimeEquals(state, expectedState))
                return false;

            tokens.Items.Remove(OAuthStateSessionKey);
            return true;
        }

        return false;
    }

    public virtual void LoadUserOAuthProvider(IAuthSession userSession, IAuthTokens tokens) { }
}