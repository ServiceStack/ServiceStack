using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Text;
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

        this.AuthRealm = appSettings != null ? appSettings.Get("OAuthRealm", authRealm) : authRealm;

        this.Provider = oAuthProvider;
        if (appSettings != null)
        {
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
        }
        else
        {
            this.RequestTokenUrl = authRealm + "oauth/request_token";
            this.AuthorizeUrl = authRealm + "oauth/authorize";
            this.AccessTokenUrl = authRealm + "oauth/access_token";
            this.SaveExtendedUserInfo = true;
        }

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
    /// Authenticating with an AccessToken signs in to, or links the external identity to, the current session.
    /// Only allow it from non-GET, same-origin requests so it can't be triggered from a cross-site link or form,
    /// which would link an attacker's identity to a signed-in user or sign a victim in to the attacker's account
    /// </summary>
    protected virtual bool IsAccessTokenAuthAllowed(IServiceBase authService, IAuthSession session)
    {
        var req = authService.Request;
        if (string.Equals(req.Verb, HttpMethods.Get, StringComparison.OrdinalIgnoreCase))
            return false;
        return IsTrustedOrigin(req);
    }

    /// <summary>
    /// Whether the request was sent from this App's origin (or a CorsFeature whitelisted origin).
    /// Requests without Origin or Referer headers (i.e. non-browser clients) are trusted.
    /// </summary>
    internal static bool IsTrustedOrigin(IRequest req)
    {
        var origin = req.GetHeader(HttpHeaders.Origin);
        if (string.IsNullOrEmpty(origin))
            origin = req.GetHeader(HttpHeaders.Referer);
        if (string.IsNullOrEmpty(origin))
            return true;

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
            || (originUri.Scheme != Uri.UriSchemeHttps && originUri.Scheme != Uri.UriSchemeHttp))
            return false;
        var originAuthority = originUri.GetLeftPart(UriPartial.Authority);

        // Same host as this request. The port and scheme of the request can differ from the browser's behind a
        // TLS-terminating reverse proxy, so only reject an http:// origin for an https:// request
        if (Uri.TryCreate(req.AbsoluteUri, UriKind.Absolute, out var reqUri) 
            && string.Equals(originUri.Host, reqUri.Host, StringComparison.OrdinalIgnoreCase)
            && (originUri.Scheme == Uri.UriSchemeHttps || reqUri.Scheme == Uri.UriSchemeHttp))
            return true;

        var webHostUrl = HostContext.Config?.WebHostUrl;
        if (!string.IsNullOrEmpty(webHostUrl) && Uri.TryCreate(webHostUrl, UriKind.Absolute, out var hostUri)
            && string.Equals(originAuthority, hostUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
            return true;

        var allowedOrigins = HostContext.GetPlugin<CorsFeature>()?.AllowOriginWhitelist;
        if (allowedOrigins != null)
        {
            foreach (var allowedOrigin in allowedOrigins)
            {
                if (string.Equals(allowedOrigin?.TrimEnd('/'), originAuthority, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Query param added to OAuth callbacks after they've been bounced through a same-site page
    /// </summary>
    public const string SessionCookieBounceParam = "ss-bounced";

    /// <summary>
    /// SameSite=Strict session cookies aren't sent on the cross-site redirect back from the OAuth provider.
    /// Returns a page that resends the callback (GET or form_post) from this App's origin so the browser
    /// includes the session cookies, or null if the request isn't an OAuth callback or was already bounced.
    /// </summary>
    public static IHttpResult CreateSessionCookieBounce(IRequest req)
    {
        if (!string.IsNullOrEmpty(req.QueryString[SessionCookieBounceParam]))
            return null;

        var isCallback = !string.IsNullOrEmpty(req.GetQueryStringOrForm(Keywords.State))
            || !string.IsNullOrEmpty(req.GetQueryStringOrForm(Keywords.Code))
            || !string.IsNullOrEmpty(req.QueryString["oauth_token"]);
        if (!isCallback)
            return null;

        // Only ever bounce back to the same path on this App. Collapse leading slashes and backslashes so
        // a path like //evil.com or /\evil.com can't become a protocol-relative URL to another site
        var pathAndQuery = new Uri(req.AbsoluteUri).PathAndQuery;
        pathAndQuery = "/" + pathAndQuery.TrimStart('/', '\\');
        var url = pathAndQuery.AddQueryParam(SessionCookieBounceParam, "1");

        string html;
        if (HttpUtils.HasRequestBody(req.Verb) && req.FormData?.Count > 0)
        {
            var sb = StringBuilderCache.Allocate();
            sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta name=\"referrer\" content=\"no-referrer\" /></head>");
            sb.Append("<body onload=\"HTMLFormElement.prototype.submit.call(document.forms[0])\"><form method=\"post\" action=\"")
              .Append(System.Net.WebUtility.HtmlEncode(url)).Append("\">");
            foreach (string key in req.FormData.AllKeys)
            {
                if (key == null) continue;
                foreach (var value in req.FormData.GetValues(key) ?? TypeConstants.EmptyStringArray)
                {
                    sb.Append("<input type=\"hidden\" name=\"").Append(System.Net.WebUtility.HtmlEncode(key))
                      .Append("\" value=\"").Append(System.Net.WebUtility.HtmlEncode(value)).Append("\" />");
                }
            }
            sb.Append("<button type=\"submit\">Continue</button></form></body></html>");
            html = StringBuilderCache.ReturnAndFree(sb);
        }
        else
        {
            var attrUrl = System.Net.WebUtility.HtmlEncode(url);
            html = "<!DOCTYPE html><html lang=\"en\"><head><meta name=\"referrer\" content=\"no-referrer\" />"
                + "<meta http-equiv=\"refresh\" content=\"0; URL=" + attrUrl + "\" />"
                + "<script>window.location.replace(\"" + Formats.HtmlFormat.EncodeForJavaScriptString(url) + "\");</script>"
                + "</head><body><a href=\"" + attrUrl + "\">Continue</a></body></html>";
        }

        var result = new HttpResult(html, MimeTypes.Html);
        result.Headers[HttpHeaders.CacheControl] = "no-store";
        result.Headers["Referrer-Policy"] = "no-referrer";
        return result;
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

    public virtual Task LoadUserOAuthProviderAsync(IAuthSession userSession, IAuthTokens tokens) => Task.CompletedTask;
}