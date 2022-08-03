using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Templates;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public abstract class OAuth2Provider : OAuthProvider
    {
        public OAuth2Provider(IAppSettings appSettings, string authRealm, string oAuthProvider)
            : base(appSettings, authRealm, oAuthProvider)
        {
            Scopes = appSettings.Get($"oauth.{Provider}.{nameof(Scopes)}", TypeConstants.EmptyStringArray);
        }

        public OAuth2Provider(IAppSettings appSettings, string authRealm, string oAuthProvider, string consumerKeyName,
            string consumerSecretName)
            : base(appSettings, authRealm, oAuthProvider, consumerKeyName, consumerSecretName)
        {
            Scopes = appSettings.Get($"oauth.{Provider}.{nameof(Scopes)}", TypeConstants.EmptyStringArray);
        }

        public string[] Scopes { get; set; }
        
        public string ResponseMode { get; set; }

        protected override void AssertValidState()
        {
            base.AssertValidState();
            
            AssertAuthorizeUrl();
            AssertAccessTokenUrl();
        }

        protected virtual void AssertAccessTokenUrl()
        {
            if (string.IsNullOrEmpty(AccessTokenUrl))
                throw new Exception($"oauth.{Provider}.{nameof(AccessTokenUrl)} is required");
        }

        protected virtual void AssertAuthorizeUrl()
        {
            if (string.IsNullOrEmpty(AuthorizeUrl))
                throw new Exception($"oauth.{Provider}.{nameof(AuthorizeUrl)} is required");
        }

        public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
        {
            var tokens = Init(authService, ref session, request);
            var ctx = CreateAuthContext(authService, session, tokens);

            //Transferring AccessToken/Secret from Mobile/Desktop App to Server
            if (request?.AccessToken != null && VerifyAccessTokenAsync != null)
            {
                if (!await VerifyAccessTokenAsync(request.AccessToken, ctx).ConfigAwait())
                    return HttpError.Unauthorized(ErrorMessages.InvalidAccessToken.Localize(authService.Request));

                var isHtml = authService.Request.IsHtml();
                var failedResult = await AuthenticateWithAccessTokenAsync(authService, session, tokens, request.AccessToken, ctx.AuthInfo, token: token).ConfigAwait();
                if (failedResult != null)
                    return ConvertToClientError(failedResult, isHtml);

                return isHtml
                    ? await authService.Redirect(SuccessRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("s", "1"))).SuccessAuthResultAsync(authService,session).ConfigAwait()
                    : null; //return default AuthenticateResponse
            }

            var httpRequest = authService.Request;
            var error = httpRequest.GetQueryStringOrForm("error_reason")
                ?? httpRequest.GetQueryStringOrForm("error")
                ?? httpRequest.GetQueryStringOrForm("error_code")
                ?? httpRequest.GetQueryStringOrForm("error_description");

            var hasError = !error.IsNullOrEmpty();
            if (hasError)
            {
                var httpParams = HttpUtils.HasRequestBody(httpRequest.Verb)
                    ? httpRequest.QueryString
                    : httpRequest.FormData;
                Log.Error($"OAuth2 Error callback. {httpParams}");
                return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", error)));
            }
        
            var code = httpRequest.GetQueryStringOrForm(Keywords.Code);
            var isPreAuthCallback = !code.IsNullOrEmpty();
            if (!isPreAuthCallback)
            {
                var oauthstate = session.Id;                
                var preAuthUrl = AuthorizeUrl
                    .AddQueryParam("response_type", "code")
                    .AddQueryParam("client_id", ConsumerKey)
                    .AddQueryParam("redirect_uri", this.CallbackUrl)
                    .AddQueryParam("scope", string.Join(" ", Scopes))
                    .AddQueryParam(Keywords.State, oauthstate);

                if (ResponseMode != null)
                    preAuthUrl = preAuthUrl.AddQueryParam("response_mode", ResponseMode);

                if (session is AuthUserSession authSession)
                    (authSession.Meta ?? (authSession.Meta = new Dictionary<string, string>()))["oauthstate"] = oauthstate;

                await this.SaveSessionAsync(authService, session, SessionExpiry, token).ConfigAwait();
                return authService.Redirect(PreAuthUrlFilter(ctx, preAuthUrl));
            }

            try
            {
                var state = httpRequest.GetQueryStringOrForm(Keywords.State);
                if (state != null && session is AuthUserSession authSession)
                {
                    if (authSession.Meta == null)
                        authSession.Meta = new Dictionary<string, string>();
                    
                    if (authSession.Meta.TryGetValue("oauthstate", out var oauthState) && state != oauthState)
                        return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "InvalidState")));

                    authSession.Meta.Remove("oauthstate");
                }
                
                var contents = await GetAccessTokenJsonAsync(code, ctx, token).ConfigAwait();
                var authInfo = (Dictionary<string,object>)JSON.parse(contents);
                ctx.AuthInfo = authInfo.ToStringDictionary();

                var accessToken = (string)authInfo["access_token"];

                var redirectUrl = SuccessRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("s", "1"));

                var errorResult = await AuthenticateWithAccessTokenAsync(authService, session, tokens, accessToken, ctx.AuthInfo, token).ConfigAwait();
                if (errorResult != null)
                    return errorResult;
                
                //Haz Access!
                if (HostContext.Config?.UseSameSiteCookies == true)
                {
                    // Workaround Set-Cookie HTTP Header not being honoured in 302 Redirects 
                    var redirectHtml = HtmlTemplates.GetHtmlRedirectTemplate(redirectUrl);
                    return await new HttpResult(redirectHtml, MimeTypes.Html).SuccessAuthResultAsync(authService,session).ConfigAwait();
                }
                
                return await authService.Redirect(redirectUrl).SuccessAuthResultAsync(authService,session).ConfigAwait();
            }
            catch (WebException we)
            {
                var errorBody = await we.GetResponseBodyAsync(token).ConfigAwait();
                Log.Error($"Failed to get Access Token for '{Provider}': {errorBody}");
                
                var statusCode = ((HttpWebResponse)we.Response).StatusCode;
                if (statusCode == HttpStatusCode.BadRequest)
                {
                    return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
                }
            }

            //Shouldn't get here
            return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "Unknown")));
        }

        protected virtual async Task<string> GetAccessTokenJsonAsync(string code, AuthContext ctx, CancellationToken token=default)
        {
            var accessTokenUrl = $"{AccessTokenUrl}?code={code}&client_id={ConsumerKey}&client_secret={ConsumerSecret}&redirect_uri={this.CallbackUrl.UrlEncode()}&grant_type=authorization_code";
            var contents = await AccessTokenUrlFilter(ctx, accessTokenUrl).PostToUrlAsync("", token: token).ConfigAwait();
            return contents;
        }

        protected virtual async Task<object> AuthenticateWithAccessTokenAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, 
            string accessToken, Dictionary<string,string> authInfo = null, CancellationToken token=default)
        {
            tokens.AccessToken = accessToken;
            if (authInfo != null)
            {
                tokens.Items ??= new Dictionary<string, string>();
                foreach (var entry in authInfo)
                {
                    tokens.Items[entry.Key] = entry.Value;
                }
            }

            var accessTokenAuthInfo = await this.CreateAuthInfoAsync(accessToken, token).ConfigAwait();

            session.IsAuthenticated = true;

            return await OnAuthenticatedAsync(authService, session, tokens, accessTokenAuthInfo, token).ConfigAwait();
        }

        protected abstract Task<Dictionary<string, string>> CreateAuthInfoAsync(string accessToken, CancellationToken token=default);

        /// <summary>
        /// Override to return User chosen username or Email for this AuthProvider
        /// </summary>
        protected virtual string GetUserAuthName(IAuthTokens tokens, Dictionary<string, string> authInfo) =>
            tokens.Email;

        protected override Task LoadUserAuthInfoAsync(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token=default)
        {
            try
            {
                tokens.UserId = authInfo.Get("user_id");
                tokens.UserName = authInfo.Get("username") ?? tokens.UserId;
                tokens.DisplayName = authInfo.Get("name");
                tokens.FirstName = authInfo.Get("first_name");
                tokens.LastName = authInfo.Get("last_name");
                tokens.Email = authInfo.Get("email");
                userSession.UserAuthName = GetUserAuthName(tokens, authInfo);

                if (authInfo.TryGetValue(AuthMetadataProvider.ProfileUrlKey, out var profileUrl))
                {
                    if (string.IsNullOrEmpty(userSession.ProfileUrl))
                        userSession.ProfileUrl = profileUrl.SanitizeOAuthUrl();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Could not retrieve '{Provider}' profile user info for '{tokens.DisplayName}'", ex);
            }

            return LoadUserOAuthProviderAsync(userSession, tokens);
        }

        /// <summary>
        /// Custom DisplayName resolver function when not provided
        /// </summary>
        public Func<IAuthSession,IAuthTokens, string> ResolveUnknownDisplayName { get; set; }
        
        public override Task LoadUserOAuthProviderAsync(IAuthSession authSession, IAuthTokens tokens)
        {
            if (authSession is AuthUserSession userSession)
            {
                userSession.UserName = tokens.UserName ?? userSession.UserName;
                userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
                userSession.FirstName = tokens.FirstName ?? userSession.FirstName;
                userSession.LastName = tokens.LastName ?? userSession.LastName;
                userSession.Email = userSession.PrimaryEmail =
                    tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;

                if (userSession.DisplayName == null && ResolveUnknownDisplayName != null)
                    userSession.DisplayName = ResolveUnknownDisplayName(authSession, tokens);
            }
            return Task.CompletedTask;
        }
    }
}