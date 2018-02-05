using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    //DigestAuth Info: http://www.ntu.edu.sg/home/ehchua/programming/webprogramming/HTTP_Authentication.html
    public class DigestAuthProvider : AuthProvider, IAuthWithRequest
    {
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
            var authRepo = HostContext.AppHost.GetAuthRepository(authService.Request);
            using (authRepo as IDisposable)
            {
                var session = authService.GetSession();
                var digestInfo = authService.Request.GetDigestAuth();
                if (authRepo.TryAuthenticate(digestInfo, PrivateKey, NonceTimeOut, session.Sequence, out var userAuth))
                {

                    var holdSessionId = session.Id;
                    session.PopulateWith(userAuth); //overwrites session.Id
                    session.Id = holdSessionId;
                    session.IsAuthenticated = true;
                    session.Sequence = digestInfo["nc"];
                    session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                    session.ProviderOAuthAccess = authRepo.GetUserAuthDetails(session.UserAuthId)
                        .ConvertAll(x => (IAuthTokens)x);

                    return true;
                }
                return false;
            }
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

            return session != null && session.IsAuthenticated && !session.UserAuthName.IsNullOrEmpty();
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
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
                session.IsAuthenticated = true;

                if (session.UserAuthName == null)
                    session.UserAuthName = userName;

                var response = OnAuthenticated(authService, session, null, null);
                if (response != null)
                    return response;

                return new AuthenticateResponse
                {
                    UserId = session.UserAuthId,
                    UserName = userName,
                    SessionId = session.Id,
                };
            }

            throw HttpError.Unauthorized(ErrorMessages.InvalidUsernameOrPassword.Localize(authService.Request));
        }

        public override IHttpResult OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            session.AuthProvider = Name;
            if (session is AuthUserSession userSession)
            {
                LoadUserAuthInfo(userSession, tokens, authInfo);
                HostContext.TryResolve<IAuthMetadataProvider>().SafeAddMetadata(tokens, authInfo);
            }

            var authRepo = HostContext.AppHost.GetAuthRepository(authService.Request);
            using (authRepo as IDisposable)
            {
                if (authRepo != null)
                {
                    if (tokens != null)
                    {
                        authInfo.ForEach((x, y) => tokens.Items[x] = y);
                        session.UserAuthId = authRepo.CreateOrMergeAuthSession(session, tokens).UserAuthId.ToString();
                    }

                    foreach (var oAuthToken in session.GetAuthTokens())
                    {
                        var authProvider = AuthenticateService.GetAuthProvider(oAuthToken.Provider);

                        var userAuthProvider = authProvider as OAuthProvider;
                        userAuthProvider?.LoadUserOAuthProvider(session, oAuthToken);
                    }

                    var failed = ValidateAccount(authService, authRepo, session, tokens);
                    if (failed != null)
                        return failed;
                }
            }

            try
            {
                session.OnAuthenticated(authService, session, tokens, authInfo);
                AuthEvents.OnAuthenticated(authService.Request, session, authService, tokens, authInfo);
            }
            finally
            {
                this.SaveSession(authService, session, SessionExpiry);
                authService.Request.Items[Keywords.DidAuthenticate] = true;
            }

            return null;
        }

        public override void OnFailedAuthentication(IAuthSession session, IRequest httpReq, IResponse httpRes)
        {
            var digestHelper = new DigestAuthFunctions();
            httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
            httpRes.AddHeader(
                HttpHeaders.WwwAuthenticate,
                $"{Provider} realm=\"{AuthRealm}\", nonce=\"{digestHelper.GetNonce(httpReq.UserHostAddress, PrivateKey)}\", qop=\"auth\"");
            httpRes.EndRequest();
        }

        public void PreAuthenticate(IRequest req, IResponse res)
        {
            var digestAuth = req.GetDigestAuth();
            if (digestAuth != null)
            {
                //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
                SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

                using (var authService = HostContext.ResolveService<AuthenticateService>(req))
                {
                    var response = authService.Post(new Authenticate
                    {
                        provider = Name,
                        nonce = digestAuth["nonce"],
                        uri = digestAuth["uri"],
                        response = digestAuth["response"],
                        qop = digestAuth["qop"],
                        nc = digestAuth["nc"],
                        cnonce = digestAuth["cnonce"],
                        UserName = digestAuth["username"]
                    });
                }
            }
        }
    }
}