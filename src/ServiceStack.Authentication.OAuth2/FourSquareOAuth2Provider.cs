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
    /// More info at: https://developer.foursquare.com/overview/auth.html
    /// Create Foursquare App at: https://foursquare.com/developers/register
    /// </summary>
    public class FourSquareOAuth2Provider : OAuth2Provider
    {
        public const string Name = "FourSquare";

        public const string Realm = "https://foursquare.com/oauth2/authenticate";

        // https://developer.foursquare.com/docs/changelog  as of 7/1/2014, 
        //   the v (i.e. Version) parameter is required for all API requests. 
        public DateTime Version { get; set; }

        public int ProfileImageWidth { get; set; }

        public int ProfileImageHeight { get; set; }

        public FourSquareOAuth2Provider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            this.AuthorizeUrl = this.AuthorizeUrl ?? Realm;
            this.AccessTokenUrl = this.AccessTokenUrl ?? "https://foursquare.com/oauth2/access_token";
            this.UserProfileUrl = this.UserProfileUrl ?? "https://api.foursquare.com/v2/users/self";

            // https://developer.foursquare.com/overview/versioning
            DateTime versionDate;
            if (!DateTime.TryParse(appSettings.GetString("oauth.{0}.Version".Fmt(Name)), out versionDate)) 
                versionDate = DateTime.UtcNow;

            // version dates before June 9, 2012 will automatically be rejected
            if (versionDate < new DateTime(2012, 6, 9))
                versionDate = DateTime.UtcNow;
            
            this.Version = versionDate;

            // Profile Image URL requires dimensions (Width x height) in the URL (default = 64x64 and minimum = 16x16)
            int profileImageWidth;
            if (!int.TryParse(appSettings.GetString("oauth.{0}.ProfileImageWidth".Fmt(Name)), out profileImageWidth))
                profileImageWidth = 64;

            this.ProfileImageWidth = Math.Max(profileImageWidth, 16);

            int profileImageHeight;
            if (!int.TryParse(appSettings.GetString("oauth.{0}.ProfileImageHeight".Fmt(Name)), out profileImageHeight))
                profileImageHeight = 64;

            this.ProfileImageHeight = Math.Max(profileImageHeight, 16);

            Scopes = appSettings.Get("oauth.{0}.Scopes".Fmt(Name), new[] { "basic" });
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
            var url = this.UserProfileUrl.AddQueryParam("oauth_token", accessToken);
            url = url.AddQueryParam("v", Version.ToString("yyyyMMdd"));
            var json = url.GetJsonFromUrl();

            var obj = JsonObject.Parse(json);
            var response = obj.Object("response");
            var user = response.Object("user");
            var userContact = user.Object("contact");
            var userPhoto = user.Object("photo");

            var fullName = "{0} {1}".Fmt(user["firstName"], user["lastName"]);

            var photoDimensions = "{0}x{1}".Fmt(ProfileImageWidth, ProfileImageHeight);
            var photoUrl = userPhoto["prefix"].CombineWith(photoDimensions, userPhoto["suffix"]).SanitizeOAuthUrl();

            var authInfo = new Dictionary<string, string>
            {
                { "user_id", user["id"] }, 
                { "username", user["id"] }, // FourSquare uses Phone & Email for user name
                { "name", fullName }, 
                { "first_name", user["firstName"] }, 
                { "last_name", user["lastName"] }, 
                { "gender", user["gender"] }, // male, female, or none
                { "picture", photoUrl },
                { AuthMetadataProvider.ProfileUrlKey, photoUrl },
            };

            if (user.ContainsKey("birthday"))
                authInfo["birthday"] = user["birthday"];

            var contactItems = new[] { "facebook", "twitter", "email", "phone" };

            foreach (var item in contactItems)
            {
                if (userContact.ContainsKey(item))
                    authInfo[item] = userContact[item];
            }

            return authInfo;
        }

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            tokens.Gender = authInfo["gender"];
            if (tokens.Gender != "none")
                userSession.Gender = tokens.Gender;

            if (authInfo.ContainsKey("phone"))
                tokens.PhoneNumber = authInfo["phone"];
            userSession.PhoneNumber = tokens.PhoneNumber ?? userSession.PhoneNumber;

            if (authInfo.ContainsKey("birthday"))
            {
                tokens.BirthDateRaw = authInfo["birthday"];                

                long unixDateTime;
                if (long.TryParse(tokens.BirthDateRaw, out unixDateTime))
                {
                    tokens.BirthDate = unixDateTime.FromUnixTime();
                }
            }
            userSession.BirthDateRaw = tokens.BirthDateRaw ?? userSession.BirthDateRaw;
            userSession.BirthDate = tokens.BirthDate ?? userSession.BirthDate;

            if (authInfo.ContainsKey("facebook"))
                userSession.FacebookUserId = authInfo["facebook"];

            if (authInfo.ContainsKey("twitter"))
                userSession.TwitterUserId = authInfo["twitter"];

            base.LoadUserAuthInfo(userSession, tokens, authInfo);
        }
    }
}
