using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

/// <summary>
///   Create VK App at: http://vk.com/editapp?act=create
///   The Callback URL for your app should match the CallbackUrl provided.
/// </summary>
public class VkAuthProvider : OAuthProvider {
    public const string Name = "vkcom";
    public static string Realm = "https://oauth.VK.ru/";
    public static string PreAuthUrl = "https://oauth.vk.com/authorize";
    public static string TokenUrl = "https://oauth.vk.com/access_token";

    static VkAuthProvider() {
    }

    public VkAuthProvider(IAppSettings appSettings)
        : base(appSettings, Realm, Name, "ApplicationId", "SecureKey") {
        ApplicationId = appSettings.GetString("oauth.vkcom.ApplicationId");
        SecureKey = appSettings.GetString("oauth.vkcom.SecureKey");
        Scope = appSettings.GetString("oauth.vkcom.Scope");
        ApiVersion = appSettings.GetString("oauth.vkcom.ApiVersion");
        AccessTokenUrl = TokenUrl;

        NavItem = new NavItem {
            Href = "/auth/" + Name,
            Label = "Sign In with VK",
            Id = "btn-" + Name,
            ClassName = "btn-social btn-vk",
            IconClass = "fab svg-vk",
        };
    }

    public string ApplicationId { get; set; }

    public string SecureKey { get; set; }
    public string Scope { get; set; }
    public string ApiVersion { get; set; }

    private async Task<JsonObject> GetUserInfoAsync(string accessToken, string accessTokenSecret) 
    {
        JsonObject authInfo = null;

        try {
            var sig = WebRequestUtils.CalculateMD5Hash($"/method/users.get?fields=screen_name,bdate,city,country,timezone&access_token={accessToken}{accessTokenSecret}");
            var json = await $"https://api.vk.com/method/users.get?fields=screen_name,bdate,city,country,timezone&access_token={accessToken}&sig={sig}".GetJsonFromUrlAsync().ConfigAwait();

            authInfo = json.ArrayObjects()[0].GetUnescaped("response").ArrayObjects()[0];
        } catch (Exception e) {
            Log.Error($"VK get user info error: {e.Message}");
        }

        return authInfo;
    }

    public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
    {
        IAuthTokens tokens = Init(authService, ref session, request);
        var ctx = CreateAuthContext(authService, session, tokens);
        IRequest httpRequest = authService.Request;

        if (request?.AccessToken != null && request?.AccessTokenSecret != null) {
            var authInfo = await GetUserInfoAsync(request.AccessToken, request.AccessTokenSecret).ConfigAwait();

            if(authInfo == null || !(authInfo.Get("error") ?? authInfo.Get("error_description")).IsNullOrEmpty()){
                Log.Error($"VK access_token error callback. {authInfo}");                    
                return HttpError.Unauthorized("AccessToken is not for App: " + ApplicationId);
            }

            tokens.AccessToken = request.AccessToken;
            tokens.AccessTokenSecret = request.AccessTokenSecret;
                                
            var isHtml = authService.Request.IsHtml();
            var failedResult = await AuthenticateWithAccessTokenAsync(authService, session, tokens, request.AccessToken).ConfigAwait();
            if (failedResult != null)
                return ConvertToClientError(failedResult, isHtml);

            return isHtml
                ? await authService.Redirect(SuccessRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("s", "1"))).SuccessAuthResultAsync(authService,session).ConfigAwait()
                : null; //return default AuthenticateResponse
        }

        string error = httpRequest.QueryString["error_reason"]
                       ?? httpRequest.QueryString["error_description"]
                       ?? httpRequest.QueryString["error"];

        bool hasError = !error.IsNullOrEmpty();
        if (hasError) 
        {
            Log.Error($"VK error callback. {httpRequest.QueryString}");
            return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", error)));
        }

        string code = httpRequest.QueryString["code"];
        bool isPreAuthCallback = !code.IsNullOrEmpty();
        if (!isPreAuthCallback) {
            string preAuthUrl = $"{PreAuthUrl}?client_id={ApplicationId}&scope={Scope}&redirect_uri={CallbackUrl.UrlEncode()}&response_type=code&v={ApiVersion}";

            await this.SaveSessionAsync(authService, session, SessionExpiry, token).ConfigAwait();
            return authService.Redirect(PreAuthUrlFilter(ctx, preAuthUrl));
        }

        try {
            code = EnsureLatestCode(code);

            string accessTokeUrl = $"{AccessTokenUrl}?client_id={ApplicationId}&client_secret={SecureKey}&code={code}&redirect_uri={CallbackUrl.UrlEncode()}";

            string contents = await AccessTokenUrlFilter(ctx, accessTokeUrl)
                .GetStringFromUrlAsync(requestFilter:req => req.With(c => c.UserAgent = ServiceClientBase.DefaultUserAgent), token:token).ConfigAwait();

            var authInfo = JsonObject.Parse(contents);

            //VK does not throw exception, but returns error property in JSON response
            string accessTokenError = authInfo.Get("error") ?? authInfo.Get("error_description");

            if (!accessTokenError.IsNullOrEmpty()) {
                Log.Error($"VK access_token error callback. {authInfo}");
                return authService.Redirect(session.ReferrerUrl.SetParam("f", "AccessTokenFailed"));
            }
            tokens.AccessTokenSecret = authInfo.Get("access_token");
            tokens.UserId = authInfo.Get("user_id");

            session.IsAuthenticated = true;

            //Haz Access
            return await OnAuthenticatedAsync(authService, session, tokens, authInfo.ToDictionary(), token).ConfigAwait()
                   ?? await authService.Redirect(SuccessRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("s", "1"))).SuccessAuthResultAsync(authService,session).ConfigAwait();
        } 
        catch (Exception ex) 
        {
            //just in case VK will start throwing exceptions 
            var statusCode = ex.GetStatus();
            if (statusCode == HttpStatusCode.BadRequest)
                return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
        }
        return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "Unknown")));
    }

    /// <summary>
    /// If previous attempts fails, the sequential calls 
    /// build up code value like "code1,code2,code3"
    /// so we need the last one only
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    private string EnsureLatestCode(string code) {
        int idx = code.LastIndexOf(",", StringComparison.Ordinal);
        if (idx > 0) {
            code = code.Substring(idx);
        }
        return code;
    }

    protected override async Task LoadUserAuthInfoAsync(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token = default)
    {
        try {
            if (!tokens.AccessToken.IsNullOrEmpty() && !tokens.AccessTokenSecret.IsNullOrEmpty()) {
                tokens.UserName = authInfo.Get("screen_name");
                tokens.DisplayName = authInfo.Get("screen_name");
                tokens.FirstName = authInfo.Get("first_name");
                tokens.LastName = authInfo.Get("last_name");
                tokens.BirthDateRaw = authInfo.Get("bdate");
                tokens.TimeZone = authInfo.Get("timezone");
            } else {
                string json = await "https://api.vk.com/method/users.get?user_ids={0}&fields=screen_name,bdate,city,country,timezone&oauth_token={0}"
                    .Fmt(tokens.UserId, tokens.AccessTokenSecret).GetJsonFromUrlAsync().ConfigAwait();

                var obj = json.ArrayObjects()[0].GetUnescaped("response").ArrayObjects()[0];

                tokens.UserName = obj.Get("screen_name");
                tokens.DisplayName = obj.Get("screen_name");
                tokens.FirstName = obj.Get("first_name");
                tokens.LastName = obj.Get("last_name");
                tokens.BirthDateRaw = obj.Get("bdate");
                tokens.TimeZone = obj.Get("timezone");

                if (SaveExtendedUserInfo) {
                    obj.Each(x => authInfo[x.Key] = x.Value);
                }
            }
        } catch (Exception ex) {
            Log.Error($"Could not retrieve VK user info for '{tokens.DisplayName}'", ex);
        }

        await LoadUserOAuthProviderAsync(userSession, tokens).ConfigAwait();
    }

    public override Task LoadUserOAuthProviderAsync(IAuthSession authSession, IAuthTokens tokens) 
    {
        if (authSession is AuthUserSession userSession)
        {
            userSession.UserName = tokens.UserName ?? userSession.UserName;
            userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
            userSession.FirstName = tokens.FirstName ?? userSession.DisplayName;
            userSession.LastName = tokens.LastName ?? userSession.LastName;
            userSession.BirthDateRaw = tokens.BirthDateRaw ?? userSession.BirthDateRaw;
        }
        return Task.CompletedTask;
    }

    protected virtual async Task<object> AuthenticateWithAccessTokenAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, string accessToken) {
        var authInfo = await GetUserInfoAsync(tokens.AccessToken, tokens.AccessTokenSecret).ConfigAwait();
        session.IsAuthenticated = true;

        return await OnAuthenticatedAsync(authService, session, tokens, authInfo).ConfigAwait();
    }
}