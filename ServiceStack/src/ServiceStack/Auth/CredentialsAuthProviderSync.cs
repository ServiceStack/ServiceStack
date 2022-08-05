#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class CredentialsAuthProviderSync : AuthProviderSync
    {
        public override string Type => "credentials";

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

        public bool SkipPasswordVerificationForInProcessRequests { get; set; }

        public CredentialsAuthProviderSync()
        {
            Provider = Name;
            AuthRealm = Realm;
        }

        public CredentialsAuthProviderSync(IAppSettings appSettings, string authRealm, string oAuthProvider)
            : base(appSettings, authRealm, oAuthProvider) { }

        public CredentialsAuthProviderSync(IAppSettings appSettings)
            : base(appSettings, Realm, Name) { }

        public IUserAuthRepository GetUserAuthRepository(IRequest request)
        {
            var authRepo = (IUserAuthRepository)HostContext.AppHost.GetAuthRepository(request);
            if (authRepo == null)
                throw new Exception(ErrorMessages.AuthRepositoryNotExists);

            return authRepo;
        }
        
        public virtual bool TryAuthenticate(IServiceBase authService, string userName, string password)
        {
            var authRepo = GetUserAuthRepository(authService.Request);
            using (authRepo as IDisposable)
            {
                var session = authService.GetSession();
                if (authRepo.TryAuthenticate(userName, password, out var userAuth))
                {
                    if (IsAccountLocked(authRepo, userAuth))
                        throw new AuthenticationException(ErrorMessages.UserAccountLocked.Localize(authService.Request));

                    session.PopulateSession(userAuth, authRepo);

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
            if (SkipPasswordVerificationForInProcessRequests && authService.Request.IsInProcessRequest())
            {
                new PrivateAuthValidator().ValidateAndThrow(request);
                return AuthenticatePrivateRequest(authService, session, request.UserName, request.Password, authService.Request.GetReturnUrl());
            }
            
            new CredentialsAuthValidator().ValidateAndThrow(request);
            return Authenticate(authService, session, request.UserName, request.Password, authService.Request.GetReturnUrl());
        }

        protected object Authenticate(IServiceBase authService, IAuthSession session, string userName, string password)
        {
            return Authenticate(authService, session, userName, password, string.Empty);
        }

        protected virtual IAuthSession ResetSessionBeforeLogin(IServiceBase authService, IAuthSession session, string userName)
        {
            if (!LoginMatchesSession(session, userName))
            {
                authService.RemoveSession();
                return authService.GetSession();
            }
            return session;
        }

        protected object Authenticate(IServiceBase authService, IAuthSession session, string userName, string password, string referrerUrl)
        {
            session = ResetSessionBeforeLogin(authService, session, userName);
            
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
                    DisplayName = session.DisplayName
                        ?? session.UserName
                        ?? $"{session.FirstName} {session.LastName}".Trim(),
                    ReferrerUrl = referrerUrl
                };
            }

            throw HttpError.Unauthorized(ErrorMessages.InvalidUsernameOrPassword.Localize(authService.Request));
        }

        protected virtual object AuthenticatePrivateRequest(
            IServiceBase authService, IAuthSession session, string userName, string password, string referrerUrl)
        {
            var authRepo = GetUserAuthRepository(authService.Request);
            using (authRepo as IDisposable)
            {
                var userAuth = authRepo.GetUserAuthByUserName(userName);
                if (userAuth == null)
                    throw HttpError.Unauthorized(ErrorMessages.InvalidUsernameOrPassword.Localize(authService.Request));

                if (IsAccountLocked(authRepo, userAuth))
                    throw new AuthenticationException(ErrorMessages.UserAccountLocked.Localize(authService.Request));

                session.PopulateSession(userAuth, authRepo);

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
        }

        public override IHttpResult OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            session.AuthProvider = Provider;

            if (session is AuthUserSession userSession)
            {
                LoadUserAuthInfo(userSession, tokens, authInfo);
                HostContext.TryResolve<IAuthMetadataProvider>().SafeAddMetadata(tokens, authInfo);
                LoadUserAuthFilter?.Invoke(userSession, tokens, authInfo);
            }

            if (session is IAuthSessionExtended authSession)
            {
                var failed = authSession.Validate(authService, session, tokens, authInfo)
                    ?? AuthEvents.Validate(authService, session, tokens, authInfo);
                if (failed != null)
                {
                    authService.RemoveSession();
                    return failed;
                }
            }

            var authRepo = GetAuthRepository(authService.Request);
            using (authRepo as IDisposable)
            {
                if (CustomValidationFilter != null)
                {
                    var ctx = new AuthContext
                    {
                        Request = authService.Request,
                        Service = authService,
                        AuthProviderSync = this,
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
                    
                    foreach (var oAuthToken in session.GetAuthTokens())
                    {
                        var authProvider = AuthenticateService.GetAuthProvider(oAuthToken.Provider);
                        var userAuthProvider = authProvider as OAuthProvider;
                        userAuthProvider?.LoadUserOAuthProviderAsync(session, oAuthToken);
                    }

                    var httpRes = authService.Request.Response as IHttpResponse;
                    httpRes?.Cookies.AddPermanentCookie(HttpHeaders.XUserAuthId, session.UserAuthId);

                    var failed = ValidateAccount(authService, authRepo, session, tokens);
                    if (failed != null)
                        return failed;
                }
            }

            try
            {
                session.IsAuthenticated = true;
                session.OnAuthenticated(authService, session, tokens, authInfo);
                AuthEvents.OnAuthenticated(authService.Request, session, authService, tokens, authInfo);
            }
            finally
            {
                this.SaveSession(authService, session, SessionExpiry);
                authService.Request.CompletedAuthentication();
            }

            return null;
        }
    }
}