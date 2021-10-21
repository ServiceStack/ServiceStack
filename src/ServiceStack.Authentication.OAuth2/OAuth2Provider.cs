﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Authentication.OAuth2
{
    [Obsolete("Use built-in OAuth2Provider in ServiceStack.Auth")]
    public abstract class OAuth2Provider : AuthProvider
    {
        protected OAuth2Provider(IAppSettings appSettings, string realm, string provider)
            : base(appSettings, realm, provider)
        {
            this.ConsumerKey = appSettings.GetString("oauth.{0}.ClientId".Fmt(provider))
                ?? appSettings.GetString("oauth.{0}.ConsumerKey".Fmt(provider))
                ?? FallbackConfig(appSettings.GetString("oauth.ConsumerKey"));
            this.ConsumerSecret = appSettings.GetString("oauth.{0}.ClientSecret".Fmt(provider))
                ?? appSettings.GetString("oauth.{0}.ConsumerSecret".Fmt(provider))
                ?? FallbackConfig(appSettings.GetString("oauth.ConsumerSecret"));
            var scopes = appSettings.GetString("oauth.{0}.Scopes".Fmt(provider))
                ?? FallbackConfig(appSettings.GetString("oauth.Scopes")) ?? "";
            this.Scopes = scopes.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            this.RequestTokenUrl = appSettings.GetString("oauth.{0}.RequestTokenUrl".Fmt(provider))
                ?? FallbackConfig(appSettings.GetString("oauth.RequestTokenUrl"));
            this.AuthorizeUrl = appSettings.GetString("oauth.{0}.AuthorizeUrl".Fmt(provider))
                ?? FallbackConfig(appSettings.GetString("oauth.AuthorizeUrl"));
            this.AccessTokenUrl = appSettings.GetString("oauth.{0}.AccessTokenUrl".Fmt(provider))
                ?? FallbackConfig(appSettings.GetString("oauth.AccessTokenUrl"));
            this.UserProfileUrl = appSettings.GetString("oauth.{0}.UserProfileUrl".Fmt(provider))
                ?? FallbackConfig(appSettings.GetString("oauth.UserProfileUrl"));

            this.SaveExtendedUserInfo = appSettings.Get($"oauth.{provider}.SaveExtendedUserInfo", true);
        }

        public string AccessTokenUrl { get; set; }

        public IAuthHttpGateway AuthHttpGateway { get; set; }

        public string AuthorizeUrl { get; set; }

        public string ConsumerKey { get; set; }

        public string ConsumerSecret { get; set; }

        public string RequestTokenUrl { get; set; }

        public string UserProfileUrl { get; set; }

        protected string[] Scopes { get; set; }

        public Action<AuthorizationServerDescription> AuthServerFilter { get; set; }

        public Action<WebServerClient> AuthClientFilter { get; set; }

        public Func<string, bool> VerifyAccessToken { get; set; }

        public virtual IAuthorizationState ProcessUserAuthorization(
            WebServerClient authClient, AuthorizationServerDescription authServer, IServiceBase authService)
        {
            return HostContext.Config.StripApplicationVirtualPath
                ? authClient.ProcessUserAuthorization(authService.Request.ToHttpRequestBase())
                : authClient.ProcessUserAuthorization();
        }

        public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token=default)
        {
            var tokens = this.Init(authService, ref session, request);
            var ctx = CreateAuthContext(authService, session, tokens);

            //Transferring AccessToken/Secret from Mobile/Desktop App to Server
            if (request?.AccessToken != null)
            {
                if (VerifyAccessToken == null)
                    throw new NotImplementedException($"VerifyAccessToken is not implemented by {Provider}");

                if (!VerifyAccessToken(request.AccessToken))
                    return HttpError.Unauthorized($"AccessToken is not for the configured {Provider} App");

                var failedResult = await AuthenticateWithAccessTokenAsync(authService, session, tokens, request.AccessToken, token).ConfigAwait();
                var isHtml = authService.Request.IsHtml();
                if (failedResult != null)
                    return ConvertToClientError(failedResult, isHtml);

                return isHtml
                    ? authService.Redirect(SuccessRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("s", "1")))
                    : null; //return default AuthenticateResponse
            }

            var authServer = new AuthorizationServerDescription { AuthorizationEndpoint = new Uri(this.AuthorizeUrl), TokenEndpoint = new Uri(this.AccessTokenUrl) };

            AuthServerFilter?.Invoke(authServer);

            var authClient = new WebServerClient(authServer, this.ConsumerKey) {
                ClientCredentialApplicator = ClientCredentialApplicator.PostParameter(this.ConsumerSecret),
            };

            AuthClientFilter?.Invoke(authClient);

            var authState = ProcessUserAuthorization(authClient, authServer, authService);
            if (authState == null)
            {
                try
                {
                    var authReq = authClient.PrepareRequestUserAuthorization(this.Scopes, new Uri(this.CallbackUrl));
                    var authContentType = authReq.Headers[HttpHeaders.ContentType];
                    var httpResult = new HttpResult(authReq.ResponseStream, authContentType) { StatusCode = authReq.Status, StatusDescription = "Moved Temporarily" };
                    foreach (string header in authReq.Headers)
                    {
                        httpResult.Headers[header] = authReq.Headers[header];
                    }

                    foreach (string name in authReq.Cookies)
                    {
                        var cookie = authReq.Cookies[name];
                        if (cookie != null)
                        {
                            httpResult.AddCookie(authService.Request, cookie.ToCookie());
                        }
                    }

                    await this.SaveSessionAsync(authService, session, SessionExpiry, token).ConfigAwait();
                    return httpResult;
                }
                catch (ProtocolException ex)
                {
                    Log.Error("Failed to login to {0}".Fmt(this.Provider), ex);
                    return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "Unknown")));
                }
            }

            var accessToken = authState.AccessToken;
            if (accessToken != null)
            {
                tokens.RefreshToken = authState.RefreshToken;
                tokens.RefreshTokenExpiry = authState.AccessTokenExpirationUtc;
            }

            if (accessToken != null)
            {
                try
                {
                    return await AuthenticateWithAccessTokenAsync(authService, session, tokens, accessToken, token).ConfigAwait()
                        ?? authService.Redirect(SuccessRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("s", "1")));
                }
                catch (WebException we)
                {
                    var statusCode = ((HttpWebResponse)we.Response).StatusCode;
                    if (statusCode == HttpStatusCode.BadRequest)
                    {
                        return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "AccessTokenFailed")));
                    }
                }
            }

            return authService.Redirect(FailedRedirectUrlFilter(ctx, session.ReferrerUrl.SetParam("f", "RequestTokenFailed")));
        }

        protected virtual async Task<object> AuthenticateWithAccessTokenAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, string accessToken, CancellationToken token=default)
        {
            tokens.AccessToken = accessToken;

            var authInfo = this.CreateAuthInfo(accessToken);

            session.IsAuthenticated = true;

            return await OnAuthenticatedAsync(authService, session, tokens, authInfo, token).ConfigAwait();
        }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
        {
            if (request != null)
            {
                if (!LoginMatchesSession(session, request.UserName))
                {
                    return false;
                }
            }

            return session != null && session.IsAuthenticated && tokens != null && !string.IsNullOrEmpty(tokens.UserId);
        }

        public void LoadUserOAuthProvider(IAuthSession authSession, IAuthTokens tokens)
        {
            var userSession = authSession as AuthUserSession;
            if (userSession == null)
            {
                return;
            }

            userSession.UserName = tokens.UserName ?? userSession.UserName;
            userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
            userSession.FirstName = tokens.FirstName ?? userSession.FirstName;
            userSession.LastName = tokens.LastName ?? userSession.LastName;
            userSession.PrimaryEmail = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
            userSession.Email = tokens.Email ?? userSession.PrimaryEmail ?? userSession.Email;
        }

        protected abstract Dictionary<string, string> CreateAuthInfo(string accessToken);

        protected override Task LoadUserAuthInfoAsync(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token=default)
        {
            try
            {
                tokens.UserId = authInfo["user_id"];
                tokens.UserName = authInfo["username"];
                tokens.DisplayName = authInfo["name"];
                tokens.FirstName = authInfo["first_name"];
                tokens.LastName = authInfo["last_name"];
                tokens.Email = authInfo["email"];
                userSession.UserAuthName = tokens.Email;

                if (authInfo.TryGetValue("picture", out var profileUrl) 
                    || authInfo.TryGetValue(AuthMetadataProvider.ProfileUrlKey, out profileUrl))
                {
                    tokens.Items[AuthMetadataProvider.ProfileUrlKey] = profileUrl;
                    
                    if (string.IsNullOrEmpty(userSession.ProfileUrl))
                        userSession.ProfileUrl = profileUrl.SanitizeOAuthUrl();
                }

                this.LoadUserOAuthProvider(userSession, tokens);
            }
            catch (Exception ex)
            {
                Log.Error("Could not retrieve Profile info for '{0}'".Fmt(tokens.DisplayName), ex);
            }
            return TypeConstants.EmptyTask;
        }

        protected IAuthTokens Init(IServiceBase authService, ref IAuthSession session, Authenticate request)
        {
            var requestUri = authService.Request.AbsoluteUri;
            if (this.CallbackUrl.IsNullOrEmpty())
            {
                this.CallbackUrl = requestUri;
            }

            if (session.ReferrerUrl.IsNullOrEmpty())
            {
                session.ReferrerUrl = authService.Request.GetReturnUrl() 
                    ?? authService.Request.GetHeader("Referer");
            }

            if (session.ReferrerUrl.IsNullOrEmpty() || session.ReferrerUrl.IndexOf("/auth", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                session.ReferrerUrl = this.RedirectUrl
                    ?? HostContext.Config.WebHostUrl
                    ?? requestUri.Substring(0, requestUri.IndexOf("/", "https://".Length + 1, StringComparison.Ordinal));
            }

            var tokens = session.GetAuthTokens(this.Provider);
            if (tokens == null)
            {
                session.AddAuthToken(tokens = new AuthTokens { Provider = this.Provider });
            }

            return tokens;
        }

        //Workaround to fix "Unexpected OAuth authorization response..." 
        //From http://stackoverflow.com/a/23693111/85785
        class DotNetOpenAuthTokenManager : IClientAuthorizationTracker
        {
            public IAuthorizationState GetAuthorizationState(Uri callbackUrl, string clientState)
            {
                return new AuthorizationState
                {
                    Callback = callbackUrl
                };
            }
        }
    }
}