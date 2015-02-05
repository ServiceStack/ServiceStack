using System;
using System.Collections.Generic;
using System.Net;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Authentication.OAuth2
{
    /// <summary>
    /// More info at: http://instagram.com/developer/authentication/
    /// </summary>
    public class InstagramOAuth2Provider : OAuth2Provider
    {
        public const string Name = "Instagram";

        public const string Realm = "https://api.instagram.com/oauth/authorize";

        public InstagramOAuth2Provider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            this.AuthorizeUrl = this.AuthorizeUrl ?? Realm;
            this.AccessTokenUrl = this.AccessTokenUrl ?? "https://api.instagram.com/oauth/access_token";

            this.UserProfileUrl = this.UserProfileUrl
                ?? "https://api.instagram.com/v1/users/self";

            if (this.Scopes.Length == 0)
            {
                this.Scopes = new[] {
                    "basic"
                };
            }
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var tokens = this.Init(authService, ref session, request);

            var authServer = new AuthorizationServerDescription { AuthorizationEndpoint = new Uri(this.AuthorizeUrl), TokenEndpoint = new Uri(this.AccessTokenUrl) };
            var authClient = new WebServerClient(authServer, this.ConsumerKey)
            {
                ClientCredentialApplicator = ClientCredentialApplicator.PostParameter(this.ConsumerSecret),
            };

            /*
             * Because we are exceeding the default max depth (2) we need to increase the quota. 
             * http://stackoverflow.com/questions/14691358/how-do-i-set-jsonreaderquotas-property-on-the-dotnetopenauth-oauth2clientchan
             * */
            authClient.JsonReaderQuotas.MaxDepth = 10;

            var authState = authClient.ProcessUserAuthorization();
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
                    return authService.Redirect(session.ReferrerUrl.AddHashParam("f", "Unknown"));
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
                    session.IsAuthenticated = true;
                    var authInfo = this.CreateAuthInfo(accessToken);
                    this.OnAuthenticated(authService, session, tokens, authInfo);
                    return authService.Redirect(session.ReferrerUrl.AddHashParam("s", "1"));
                }
                catch (WebException we)
                {
                    var statusCode = ((HttpWebResponse)we.Response).StatusCode;
                    if (statusCode == HttpStatusCode.BadRequest)
                    {
                        return authService.Redirect(session.ReferrerUrl.AddHashParam("f", "AccessTokenFailed"));
                    }
                }
            }

            return authService.Redirect(session.ReferrerUrl.AddHashParam("f", "RequestTokenFailed"));
        }

        protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            var url = this.UserProfileUrl.AddQueryParam("access_token", accessToken);
            var json = url.GetJsonFromUrl();

            var obj = JsonObject.Parse(json);
            var data = obj.Object("data");
            var authInfo = new Dictionary<string, string>
            {
                { "user_id", data["id"] }, 
                { "username", data["username"] }, 
                { "name", data["full_name"] }, 
                { "picture", data["profile_picture"] },
            };

            return authInfo;
        }
    }
}
