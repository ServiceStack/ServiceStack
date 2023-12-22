using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth;

/// <summary>
///   Create an App at: https://github.com/settings/applications/new
///   The Callback URL for your app should match the CallbackUrl provided.
/// </summary>
public class GithubAuthProvider : OAuthProvider
{
    public const string Name = "github";
    public static string Realm = "https://github.com/login/";
        
    public const string DefaultPreAuthUrl = "https://github.com/login/oauth/authorize";
    public const string DefaultVerifyAccessTokenUrl = "https://api.github.com/applications/{0}/tokens/{1}";

    public string PreAuthUrl { get; set; }

    public string VerifyAccessTokenUrl { get; set; }

    public override Dictionary<string, string> Meta { get; } = new Dictionary<string, string> {
        [Keywords.Allows] = Keywords.Embed + "," + Keywords.AccessTokenAuth,
    };
        
    static GithubAuthProvider() {}

    public GithubAuthProvider(IAppSettings appSettings)
        : base(appSettings, Realm, Name, "ClientId", "ClientSecret")
    {
        ClientId = appSettings.GetString("oauth.github.ClientId");
        ClientSecret = appSettings.GetString("oauth.github.ClientSecret");
        Scopes = appSettings.Get("oauth.github.Scopes", new[] { "user" });
        PreAuthUrl = DefaultPreAuthUrl;
        VerifyAccessTokenUrl = DefaultVerifyAccessTokenUrl;            
        ClientConfig.ConfigureTls12();

        Icon = Svg.ImageSvg("<svg xmlns='http://www.w3.org/2000/svg' fill='currentColor' viewBox='0 0 20 20'><path fill-rule='evenodd' d='M10 0C4.477 0 0 4.484 0 10.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0110 4.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.203 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.942.359.31.678.921.678 1.856 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0020 10.017C20 4.484 15.522 0 10 0z' clip-rule='evenodd' /></svg>");
        NavItem = new NavItem {
            Href = "/auth/" + Name,
            Label = "Sign In with GitHub",
            Id = "btn-" + Name,
            ClassName = "btn-social btn-github",
            IconClass = "fab svg-github",
        };
    }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string[] Scopes { get; set; }

    public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
    {
        var tokens = Init(authService, ref session, request);
        var ctx = CreateAuthContext(authService, session, tokens);

        //Transferring AccessToken/Secret from Mobile/Desktop App to Server
        if (request?.AccessToken != null)
        {
            //https://developer.github.com/v3/oauth_authorizations/#check-an-authorization

            var url = VerifyAccessTokenUrl.Fmt(ClientId, request.AccessToken);
            var json = await url.GetJsonFromUrlAsync(requestFilter: req => 
                req.With(c => {
                    c.UserAgent = ServiceClientBase.DefaultUserAgent;
                    c.SetAuthBasic(ClientId, ClientSecret);
                }), token: token).ConfigAwait();

            var isHtml = authService.Request.IsHtml();
            var failedResult = await AuthenticateWithAccessTokenAsync(authService, session, tokens, request.AccessToken, token).ConfigAwait();
            if (failedResult != null)
                return ConvertToClientError(failedResult, isHtml);

            return isHtml
                ? await authService.Redirect(SuccessRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("s", "1"))).SuccessAuthResultAsync(authService,session).ConfigAwait()
                : null; //return default AuthenticateResponse
        }

        var httpRequest = authService.Request;

        //https://developer.github.com/v3/oauth/#common-errors-for-the-authorization-request
        var error = httpRequest.QueryString["error"]
                    ?? httpRequest.QueryString["error_uri"]
                    ?? httpRequest.QueryString["error_description"];

        var hasError = !error.IsNullOrEmpty();
        if (hasError)
        {
            Log.Error($"GitHub error callback. {httpRequest.QueryString}");
            return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", error)));
        }

        var code = httpRequest.QueryString["code"];
        var isPreAuthCallback = !code.IsNullOrEmpty();
        if (!isPreAuthCallback)
        {
            var scopes = Scopes.Join("%20");
            string preAuthUrl = $"{PreAuthUrl}?client_id={ClientId}&redirect_uri={CallbackUrl.UrlEncode()}&scope={scopes}&{Keywords.State}={session.Id}";

            await this.SaveSessionAsync(authService, session, SessionExpiry, token).ConfigAwait();
            return authService.Redirect(PreAuthUrlFilter(ctx, preAuthUrl));
        }

        try
        {
            string accessTokenUrl = $"{AccessTokenUrl}?client_id={ClientId}&redirect_uri={CallbackUrl.UrlEncode()}&client_secret={ClientSecret}&code={code}";
            var contents = await AccessTokenUrlFilter(ctx, accessTokenUrl).GetStringFromUrlAsync().ConfigAwait();
            var authInfo = PclExportClient.Instance.ParseQueryString(contents);

            //GitHub does not throw exception, but just return error with descriptions
            //https://developer.github.com/v3/oauth/#common-errors-for-the-access-token-request
            var accessTokenError = authInfo["error"]
                                   ?? authInfo["error_uri"]
                                   ?? authInfo["error_description"];

            if (!accessTokenError.IsNullOrEmpty())
            {
                Log.Error($"GitHub access_token error callback. {authInfo}");
                return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
            }

            var accessToken = authInfo["access_token"];

            //Haz Access!
            return await AuthenticateWithAccessTokenAsync(authService, session, tokens, accessToken, token).ConfigAwait()
                   ?? await authService.Redirect(SuccessRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("s", "1"))).SuccessAuthResultAsync(authService,session).ConfigAwait();
        }
        catch (WebException webException)
        {
            var errorBody = webException.GetResponseBodyAsync(token);
            Log.Error("GitHub AccessToken Failed:\n" + errorBody);
                
            //just in case GitHub will start throwing exceptions 
            var statusCode = ((HttpWebResponse)webException.Response).StatusCode;
            if (statusCode == HttpStatusCode.BadRequest)
            {
                return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
            }
        }
        return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "Unknown")));
    }

    protected virtual async Task<object> AuthenticateWithAccessTokenAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, string accessToken, CancellationToken token=default)
    {
        tokens.AccessTokenSecret = accessToken;

        var json = await AuthHttpGateway.DownloadGithubUserInfoAsync(accessToken, token).ConfigAwait();
        var authInfo = JsonObject.Parse(json);

        session.IsAuthenticated = true;

        return await OnAuthenticatedAsync(authService, session, tokens, authInfo, token).ConfigAwait();
    }

    protected override async Task LoadUserAuthInfoAsync(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token=default)
    {
        try
        {
            tokens.UserId = authInfo.Get("id");
            tokens.UserName = authInfo.Get("login");
            tokens.DisplayName = authInfo.Get("name");
            tokens.Email = authInfo.Get("email");
            tokens.Company = authInfo.Get("company");
            tokens.Country = authInfo.Get("country");

            if (authInfo.TryGetValue("avatar_url", out var profileUrl))
            {
                tokens.Items[AuthMetadataProvider.ProfileUrlKey] = profileUrl;
                        
                if (string.IsNullOrEmpty(userSession.ProfileUrl))
                    userSession.ProfileUrl = profileUrl.SanitizeOAuthUrl();
            }

            if (string.IsNullOrEmpty(tokens.Email))
            {
                var json = await AuthHttpGateway.DownloadGithubUserEmailsInfoAsync(tokens.AccessTokenSecret, token).ConfigAwait();
                var objs = JsonArrayObjects.Parse(json);
                foreach (var obj in objs)
                {
                    if (obj.Get<bool>("primary"))
                    {
                        tokens.Email = obj.Get("email");
                        if (obj.Get<bool>("verified"))
                        {
                            tokens.Items["email_verified"] = "true";
                        }
                        break;
                    }
                }
            }
            userSession.UserAuthName = tokens.UserName ?? tokens.Email;
        }
        catch (Exception ex)
        {
            Log.Error($"Could not retrieve github user info for '{tokens.DisplayName}'", ex);
        }

        await LoadUserOAuthProviderAsync(userSession, tokens).ConfigAwait();
    }

    public override Task LoadUserOAuthProviderAsync(IAuthSession authSession, IAuthTokens tokens)
    {
        if (authSession is AuthUserSession userSession)
        {
            userSession.UserName = tokens.UserName ?? userSession.UserName;
            userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
            userSession.Company = tokens.Company ?? userSession.Company;
            userSession.Country = tokens.Country ?? userSession.Country;
            userSession.PrimaryEmail = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
            userSession.Email = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
        }
        return Task.CompletedTask;
    }
}