﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class DigestAuthProvider : AuthProvider
    {
        //private class DigestAuthValidator : AbstractValidator<Authenticate>
        //{
        //    public DigestAuthValidator()
        //    {
        //        RuleFor(x => x.UserName).NotEmpty();
        //        RuleFor(x => x.Password).NotEmpty();
        //    }
        //}

        public static string Name = AuthenticateService.DigestProvider;
        public static string Realm = "/auth/" + AuthenticateService.DigestProvider;
        public static int NonceTimeOut = 600;
        public string PrivateKey;
        public IAppSettings AppSettings { get; set; }

        public DigestAuthProvider()
        {
            Provider = Name;
            PrivateKey = Guid.NewGuid().ToString();
            AuthRealm = Realm;
        }

        public DigestAuthProvider(IAppSettings appSettings, string authRealm, string oAuthProvider)
            : base(appSettings, authRealm, oAuthProvider) { }

        public DigestAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name) { }

        public virtual bool TryAuthenticate(IServiceBase authService, string userName, string password)
        {
            var authRepo = authService.TryResolve<IAuthRepository>();
            if (authRepo == null) {
                Log.WarnFormat("Tried to authenticate without a registered IUserAuthRepository");
                return false;
            }

            var session = authService.GetSession();
            var digestInfo = authService.RequestContext.Get<IHttpRequest>().GetDigestAuth();
            IUserAuth userAuth;
            if (authRepo.TryAuthenticate(digestInfo, PrivateKey, NonceTimeOut, session.Sequence, out userAuth)) {
                session.PopulateWith(userAuth);
                session.IsAuthenticated = true;
                session.Sequence = digestInfo["nc"];
                session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                session.ProviderOAuthAccess = authRepo.GetUserOAuthProviders(session.UserAuthId)
                    .ConvertAll(x => (IAuthTokens) x);

                return true;
            }
            return false;
        }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
        {
            if (request != null) {
                if (!LoginMatchesSession(session, request.UserName)) {
                    return false;
                }
            }

            return !session.UserAuthName.IsNullOrEmpty();
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            //new CredentialsAuthValidator().ValidateAndThrow(request);
            return Authenticate(authService, session, request.UserName, request.Password);
        }

        protected object Authenticate(IServiceBase authService, IAuthSession session, string userName, string password)
        {
            if (!LoginMatchesSession(session, userName)) {
                authService.RemoveSession();
                session = authService.GetSession();
            }

            if (TryAuthenticate(authService, userName, password)) {
                if (session.UserAuthName == null) {
                    session.UserAuthName = userName;
                }

                OnAuthenticated(authService, session, null, null);

                return new AuthenticateResponse {
                    UserName = userName,
                    SessionId = session.Id,
                };
            }

            throw HttpError.Unauthorized("Invalid UserName or Password");
        }

        public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            var userSession = session as AuthUserSession;
            if (userSession != null) {
                LoadUserAuthInfo(userSession, tokens, authInfo);
            }

            var authRepo = authService.TryResolve<IAuthRepository>();
            if (authRepo != null) {
                if (tokens != null) {
                    authInfo.ForEach((x, y) => tokens.Items[x] = y);
                    session.UserAuthId = authRepo.CreateOrMergeAuthSession(session, tokens);
                }

                foreach (var oAuthToken in session.ProviderOAuthAccess) {
                    var authProvider = AuthenticateService.GetAuthProvider(oAuthToken.Provider);
                    if (authProvider == null) {
                        continue;
                    }
                    var userAuthProvider = authProvider as OAuthProvider;
                    if (userAuthProvider != null) {
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
            httpRes.StatusCode = (int) HttpStatusCode.Unauthorized;
            httpRes.AddHeader(
                HttpHeaders.WwwAuthenticate,
                "{0} realm=\"{1}\", nonce=\"{2}\", qop=\"auth\"".Fmt(Provider, AuthRealm, digestHelper.GetNonce(httpReq.UserHostAddress, PrivateKey)));
            httpRes.EndRequest();
        }
    }
}