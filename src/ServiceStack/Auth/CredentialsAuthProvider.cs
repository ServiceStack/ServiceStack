using System;
using System.Collections.Generic;
using System.Globalization;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
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

        private class PrivateAuthValidator : AbstractValidator<Authenticate>
        {
            public PrivateAuthValidator()
            {
                RuleFor(x => x.UserName).NotEmpty();
            }
        }

        public static string Name = AuthenticateService.CredentialsProvider;
        public static string Realm = "/auth/" + AuthenticateService.CredentialsProvider;

        public bool SkipPasswordVerificationForPrivateRequests { get; set; }

        public CredentialsAuthProvider()
        {
            Provider = Name;
            AuthRealm = Realm;
        }

        public CredentialsAuthProvider(IAppSettings appSettings, string authRealm, string oAuthProvider)
            : base(appSettings, authRealm, oAuthProvider) { }

        public CredentialsAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name) { }

        public virtual bool TryAuthenticate(IServiceBase authService, string userName, string password)
        {
            var authRepo = authService.TryResolve<IAuthRepository>().AsUserAuthRepository(authService.GetResolver());

            var session = authService.GetSession();
            IUserAuth userAuth;
            if (authRepo.TryAuthenticate(userName, password, out userAuth))
            {
                if (IsAccountLocked(authRepo, userAuth))
                    throw new AuthenticationException("This account has been locked");

                PopulateSession(authRepo, userAuth, session);

                return true;
            }

            return false;
        }

        private static void PopulateSession(IUserAuthRepository authRepo, IUserAuth userAuth, IAuthSession session)
        {
            var holdSessionId = session.Id;
            session.PopulateWith(userAuth); //overwrites session.Id
            session.Id = holdSessionId;
            session.IsAuthenticated = true;
            session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
            session.ProviderOAuthAccess = authRepo.GetUserAuthDetails(session.UserAuthId)
                .ConvertAll(x => (IAuthTokens) x);
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
            if (SkipPasswordVerificationForPrivateRequests && authService.Request.IsPrivateRequest())
            {
                new PrivateAuthValidator().ValidateAndThrow(request);
                return AuthenticatePrivateRequest(authService, session, request.UserName, request.Password, request.Continue);
            }
            
            new CredentialsAuthValidator().ValidateAndThrow(request);
            return Authenticate(authService, session, request.UserName, request.Password, request.Continue);
        }

        protected object Authenticate(IServiceBase authService, IAuthSession session, string userName, string password)
        {
            return Authenticate(authService, session, userName, password, string.Empty);
        }

        protected object Authenticate(IServiceBase authService, IAuthSession session, string userName, string password, string referrerUrl)
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
                {
                    session.UserAuthName = userName;
                }

                var response = OnAuthenticated(authService, session, null, null);
                if (response != null)
                    return response;

                return new AuthenticateResponse
                {
                    UserId = session.UserAuthId,
                    UserName = userName,
                    SessionId = session.Id,
                    ReferrerUrl = referrerUrl
                };
            }

            throw HttpError.Unauthorized(ErrorMessages.InvalidUsernameOrPassword);
        }

        protected object AuthenticatePrivateRequest(
            IServiceBase authService, IAuthSession session, string userName, string password, string referrerUrl)
        {
            var authRepo = authService.TryResolve<IAuthRepository>().AsUserAuthRepository(authService.GetResolver());

            var userAuth = authRepo.GetUserAuthByUserName(userName);
            if (userAuth == null)
                throw HttpError.Unauthorized(ErrorMessages.InvalidUsernameOrPassword);

            if (IsAccountLocked(authRepo, userAuth))
                throw new AuthenticationException("This account has been locked");

            PopulateSession(authRepo, userAuth, session);

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
                ReferrerUrl = referrerUrl
            };
        }

        public override IHttpResult OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            var userSession = session as AuthUserSession;
            if (userSession != null)
            {
                LoadUserAuthInfo(userSession, tokens, authInfo);
                HostContext.TryResolve<IAuthMetadataProvider>().SafeAddMetadata(tokens, authInfo);

                if (LoadUserAuthFilter != null)
                {
                    LoadUserAuthFilter(userSession, tokens, authInfo);
                }
            }

            var authRepo = authService.TryResolve<IAuthRepository>();

            if (CustomValidationFilter != null)
            {
                var ctx = new AuthContext
                {
                    Service = authService,
                    AuthProvider = this,
                    Session = session,
                    AuthTokens = tokens,
                    AuthInfo = authInfo,
                    AuthRepository = authRepo,
                };
                var response = CustomValidationFilter(ctx);
                if (response != null)
                {
                    authService.RemoveSession();
                    return response;
                }
            }

            if (authRepo != null)
            {
                if (tokens != null)
                {
                    authInfo.ForEach((x, y) => tokens.Items[x] = y);
                    session.UserAuthId = authRepo.CreateOrMergeAuthSession(session, tokens).UserAuthId.ToString();
                }

                foreach (var oAuthToken in session.ProviderOAuthAccess)
                {
                    var authProvider = AuthenticateService.GetAuthProvider(oAuthToken.Provider);
                    if (authProvider == null)
                    {
                        continue;
                    }
                    var userAuthProvider = authProvider as OAuthProvider;
                    if (userAuthProvider != null)
                    {
                        userAuthProvider.LoadUserOAuthProvider(session, oAuthToken);
                    }
                }

                var httpRes = authService.Request.Response as IHttpResponse;
                if (httpRes != null)
                {
                    httpRes.Cookies.AddPermanentCookie(HttpHeaders.XUserAuthId, session.UserAuthId);
                }

                var failed = ValidateAccount(authService, authRepo, session, tokens);
                if (failed != null)
                    return failed;
            }

            try
            {
                session.IsAuthenticated = true;
                session.OnAuthenticated(authService, session, tokens, authInfo);
                AuthEvents.OnAuthenticated(authService.Request, session, authService, tokens, authInfo);
            }
            finally
            {
                authService.SaveSession(session, SessionExpiry);
            }

            return null;
        }
    }
}