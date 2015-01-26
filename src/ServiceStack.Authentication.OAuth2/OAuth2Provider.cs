using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using ServiceStack.Auth;
using ServiceStack.Configuration;

namespace ServiceStack.Authentication.OAuth2
{
    public abstract class OAuth2Provider : AuthProvider
    {
        protected OAuth2Provider(IAppSettings appSettings, string realm, string provider)
            : base(appSettings, realm, provider)
        {
            this.ConsumerKey = appSettings.GetString("oauth.{0}.ConsumerKey".Fmt(provider))
                ?? FallbackConfig(appSettings.GetString("oauth.ConsumerKey"));
            this.ConsumerSecret = appSettings.GetString("oauth.{0}.ConsumerSecret".Fmt(provider))
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
        }

        public string AccessTokenUrl { get; set; }

        public IAuthHttpGateway AuthHttpGateway { get; set; }

        public string AuthorizeUrl { get; set; }

        public string ConsumerKey { get; set; }

        public string ConsumerSecret { get; set; }

        public string RequestTokenUrl { get; set; }

        public string UserProfileUrl { get; set; }

        protected string[] Scopes { get; set; }

        public virtual IAuthorizationState ProcessUserAuthorization(
            WebServerClient authClient, AuthorizationServerDescription authServer, IServiceBase authService)
        {
            return HostContext.Config.StripApplicationVirtualPath
                ? authClient.ProcessUserAuthorization(authService.Request.ToHttpRequestBase())
                : authClient.ProcessUserAuthorization();
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var tokens = this.Init(authService, ref session, request);

            var authServer = new AuthorizationServerDescription { AuthorizationEndpoint = new Uri(this.AuthorizeUrl), TokenEndpoint = new Uri(this.AccessTokenUrl) };
            var authClient = new WebServerClient(authServer, this.ConsumerKey) {
                ClientCredentialApplicator = ClientCredentialApplicator.PostParameter(this.ConsumerSecret),
            };

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
                            httpResult.SetSessionCookie(name, cookie.Value, cookie.Path);
                        }
                    }

                    authService.SaveSession(session, this.SessionExpiry);
                    return httpResult;
                }
                catch (ProtocolException ex)
                {
                    Log.Error("Failed to login to {0}".Fmt(this.Provider), ex);
                    return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.AddParam("f", "Unknown")));
                }
            }

            var accessToken = authState.AccessToken;
            if (accessToken != null)
            {
                try
                {
                    tokens.AccessToken = accessToken;
                    tokens.RefreshToken = authState.RefreshToken;
                    tokens.RefreshTokenExpiry = authState.AccessTokenExpirationUtc;

                    var authInfo = this.CreateAuthInfo(accessToken);

                    session.IsAuthenticated = true;
                    
                    return OnAuthenticated(authService, session, tokens, authInfo)
                        ?? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.AddParam("s", "1")));
                }
                catch (WebException we)
                {
                    var statusCode = ((HttpWebResponse)we.Response).StatusCode;
                    if (statusCode == HttpStatusCode.BadRequest)
                    {
                        return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.AddParam("f", "AccessTokenFailed")));
                    }
                }
            }

            return authService.Redirect(FailedRedirectUrlFilter(this, session.ReferrerUrl.AddParam("f", "RequestTokenFailed")));
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

            return tokens != null && !string.IsNullOrEmpty(tokens.UserId);
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

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo)
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

                string profileUrl;
                if (authInfo.TryGetValue("picture", out profileUrl))
                    tokens.Items[AuthMetadataProvider.ProfileUrlKey] = profileUrl;

                this.LoadUserOAuthProvider(userSession, tokens);
            }
            catch (Exception ex)
            {
                Log.Error("Could not retrieve Profile info for '{0}'".Fmt(tokens.DisplayName), ex);
            }
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
                session.ReferrerUrl = (request != null ? request.Continue : null) ?? authService.Request.GetHeader("Referer");
            }

            if (session.ReferrerUrl.IsNullOrEmpty() || session.ReferrerUrl.IndexOf("/auth", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                session.ReferrerUrl = this.RedirectUrl
                                      ?? HttpHandlerFactory.GetBaseUrl()
                                      ?? requestUri.Substring(0, requestUri.IndexOf("/", "https://".Length + 1, StringComparison.Ordinal));
            }

            var tokens = session.ProviderOAuthAccess.FirstOrDefault(x => x.Provider == this.Provider);
            if (tokens == null)
            {
                session.ProviderOAuthAccess.Add(tokens = new AuthTokens { Provider = this.Provider });
            }

            return tokens;
        }
    }
}