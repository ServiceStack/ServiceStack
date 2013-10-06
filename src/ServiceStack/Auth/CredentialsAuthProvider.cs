using System.Collections.Generic;
using System.Globalization;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class CredentialsAuthProvider : AuthProvider
    {
        private class CredentialsAuthValidator : AbstractValidator<Authenticate>
        {
            public CredentialsAuthValidator()
            {
                RuleFor(x => x.UserName).NotEmpty();
                RuleFor(x => x.Password).NotEmpty();
            }
        }

        public static string Name = AuthenticateService.CredentialsProvider;
        public static string Realm = "/auth/" + AuthenticateService.CredentialsProvider;

        public CredentialsAuthProvider()
        {
            Provider = Name;
            AuthRealm = Realm;
        }

        public CredentialsAuthProvider(IAppSettings appSettings, string authRealm, string oAuthProvider)
            : base(appSettings, authRealm, oAuthProvider) {}

        public CredentialsAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name) {}

        public virtual bool TryAuthenticate(IServiceBase authService, string userName, string password)
        {
            var authRepo = authService.TryResolve<IAuthRepository>();
            if (authRepo == null) {
                Log.WarnFormat("Tried to authenticate without a registered IUserAuthRepository");
                return false;
            }

            var session = authService.GetSession();
            IUserAuth userAuth;
            if (authRepo.TryAuthenticate(userName, password, out userAuth)) {
                session.PopulateWith(userAuth);
                session.IsAuthenticated = true;
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
            new CredentialsAuthValidator().ValidateAndThrow(request);
            return Authenticate(authService, session, request.UserName, request.Password, request.Continue);
        }

        protected object Authenticate(IServiceBase authService, IAuthSession session, string userName, string password)
        {
            return Authenticate(authService, session, userName, password, string.Empty);
        }

        protected object Authenticate(IServiceBase authService, IAuthSession session, string userName, string password, string referrerUrl)
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
                    ReferrerUrl = referrerUrl
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

                var httpRes = authService.RequestContext.Get<IHttpResponse>();
                if (httpRes != null) {
                    httpRes.Cookies.AddPermanentCookie(HttpHeaders.XUserAuthId, session.UserAuthId);
                }
            }

            authService.SaveSession(session, SessionExpiry);
            session.OnAuthenticated(authService, session, tokens, authInfo);
        }
    }
}