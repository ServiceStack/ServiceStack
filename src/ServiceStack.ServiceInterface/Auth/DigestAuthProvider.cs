using System.Globalization;
using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using HttpResponseExtensions = ServiceStack.WebHost.Endpoints.Extensions.HttpResponseExtensions;

namespace ServiceStack.ServiceInterface.Auth
{
    public class DigestAuthProvider : AuthProvider
    {
        class DigestAuthValidator : AbstractValidator<Auth>
        {
            public DigestAuthValidator()
            {
                RuleFor(x => x.UserName).NotEmpty();
                RuleFor(x => x.Password).NotEmpty();
            }
        }

        public static string Name = AuthService.DigestProvider;
        public static string Realm = "/auth/" + AuthService.DigestProvider;
        public static int NonceTimeOut = 600;
        public string PrivateKey;
        public IResourceManager AppSettings { get; set; }
        public DigestAuthProvider()
        {
            this.Provider = Name;
            PrivateKey = Guid.NewGuid().ToString();
            this.AuthRealm = Realm;
        }
        public DigestAuthProvider(IResourceManager appSettings, string authRealm, string oAuthProvider)
            : base(appSettings, authRealm, oAuthProvider) { }

        public DigestAuthProvider(IResourceManager appSettings)
            : base(appSettings, Realm, Name) { }

        public virtual bool TryAuthenticate(IServiceBase authService, string userName, string password)
        {
            var authRepo = authService.TryResolve<IUserAuthRepository>();
            if (authRepo == null)
            {
                Log.WarnFormat("Tried to authenticate without a registered IUserAuthRepository");
                return false;
            }

            var session = authService.GetSession();
            var digestInfo = authService.RequestContext.Get<IHttpRequest>().GetDigestAuth();
            UserAuth userAuth = null;
            if (authRepo.TryAuthenticate(digestInfo,PrivateKey,NonceTimeOut, session.Sequence, out userAuth))
            {
                session.PopulateWith(userAuth);
                session.IsAuthenticated = true;
                session.Sequence = digestInfo["nc"];
                session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                session.ProviderOAuthAccess = authRepo.GetUserOAuthProviders(session.UserAuthId)
                .ConvertAll(x => (IOAuthTokens)x);
                
                return true;
            }
            return false;
        }

        public override bool IsAuthorized(IAuthSession session, IOAuthTokens tokens, Auth request = null)
        {
            if (request != null)
            {
                if (!LoginMatchesSession(session, request.UserName)) return false;
            }

            return !session.UserAuthName.IsNullOrEmpty();
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Auth request)
        {
            //new CredentialsAuthValidator().ValidateAndThrow(request);
            return Authenticate(authService, session, request.UserName, request.Password);
        }

        protected object Authenticate(IServiceBase authService, IAuthSession session, string userName, string password)
        {
            if (!LoginMatchesSession(session, userName))
            {
                authService.RemoveSession();
                session = authService.GetSession();
            }

            if (TryAuthenticate(authService, userName, password))
            {
                if (session.UserAuthName == null)
                    session.UserAuthName = userName;

                OnAuthenticated(authService, session, null, null);

                return new AuthResponse
                {
                    UserName = userName,
                    SessionId = session.Id,
                };
            }

            throw HttpError.Unauthorized("Invalid UserName or Password");
        }

        public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            var userSession = session as AuthUserSession;
            if (userSession != null)
            {
                LoadUserAuthInfo(userSession, tokens, authInfo);
            }

            var authRepo = authService.TryResolve<IUserAuthRepository>();
            if (authRepo != null)
            {
                if (tokens != null)
                {
                    authInfo.ForEach((x, y) => tokens.Items[x] = y);
                    session.UserAuthId = authRepo.CreateOrMergeAuthSession(session, tokens);
                }

                foreach (var oAuthToken in session.ProviderOAuthAccess)
                {
                    var authProvider = AuthService.GetAuthProvider(oAuthToken.Provider);
                    if (authProvider == null) continue;
                    var userAuthProvider = authProvider as OAuthProvider;
                    if (userAuthProvider != null)
                    {
                        userAuthProvider.LoadUserOAuthProvider(session, oAuthToken);
                    }
                }

                //var httpRes = authService.RequestContext.Get<IHttpResponse>();
                //if (httpRes != null)
                //{
                //    httpRes.Cookies.AddPermanentCookie(HttpHeaders.XUserAuthId, session.UserAuthId);
                //}

            }

            authService.SaveSession(session, SessionExpiry);
            session.OnAuthenticated(authService, session, tokens, authInfo);
        }
        public override void OnFailedAuthentication(IAuthSession session, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            var digestHelper = new DigestAuthFunctions();
            httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
            httpRes.AddHeader(HttpHeaders.WwwAuthenticate, "{0} realm=\"{1}\", nonce=\"{2}\", qop=\"auth\"".Fmt(Provider, AuthRealm,digestHelper.GetNonce(httpReq.UserHostAddress,PrivateKey)));
            httpRes.EndServiceStackRequest();
        }
    }
}
