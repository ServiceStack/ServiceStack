using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class CredentialsAuthProvider : AuthProvider
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

        public CredentialsAuthProvider()
        {
            Provider = Name;
            AuthRealm = Realm;
        }

        public CredentialsAuthProvider(IAppSettings appSettings, string authRealm, string oAuthProvider)
            : base(appSettings, authRealm, oAuthProvider) { }

        public CredentialsAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name) { }
        
        public virtual async Task<bool> TryAuthenticateAsync(IServiceBase authService, string userName, string password, CancellationToken token=default)
        {
            var authRepo = GetUserAuthRepositoryAsync(authService.Request);
#if NET472 || NETSTANDARD2_0
            await using (authRepo as IAsyncDisposable)
#else
            using (authRepo as IDisposable)
#endif
            {
                var session = await authService.GetSessionAsync(token: token).ConfigAwait();
                var userAuth = await authRepo.TryAuthenticateAsync(userName, password, token).ConfigAwait();
                if (userAuth != null)
                {
                    if (await IsAccountLockedAsync(authRepo, userAuth, token: token))
                        throw new AuthenticationException(ErrorMessages.UserAccountLocked.Localize(authService.Request));

                    await session.PopulateSessionAsync(userAuth, authRepo, token).ConfigAwait();

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

        public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token=default)
        {
            if (SkipPasswordVerificationForInProcessRequests && authService.Request.IsInProcessRequest())
            {
                await new PrivateAuthValidator().ValidateAndThrowAsync(request, cancellationToken: token);
                return await AuthenticatePrivateRequestAsync(authService, session, request.UserName, request.Password, authService.Request.GetReturnUrl(), token).ConfigAwait();
            }
            
            await new CredentialsAuthValidator().ValidateAndThrowAsync(request, cancellationToken: token);
            return await AuthenticateAsync(authService, session, request.UserName, request.Password, authService.Request.GetReturnUrl(), token).ConfigAwait();
        }

        protected virtual async Task<IAuthSession> ResetSessionBeforeLoginAsync(IServiceBase authService, IAuthSession session, string userName, CancellationToken token=default)
        {
            if (!LoginMatchesSession(session, userName))
            {
                await authService.RemoveSessionAsync(token).ConfigAwait();
                return await authService.GetSessionAsync(token: token).ConfigAwait();
            }
            return session;
        }
        
        protected async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, string userName, string password, string referrerUrl, CancellationToken token=default)
        {
            session = await ResetSessionBeforeLoginAsync(authService, session, userName, token).ConfigAwait();
            
            if (await TryAuthenticateAsync(authService, userName, password, token).ConfigAwait())
            {
                session.IsAuthenticated = true;

                if (session.UserAuthName == null)
                    session.UserAuthName = userName;

                var response = await OnAuthenticatedAsync(authService, session, null, null, token).ConfigAwait();
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

        protected virtual async Task<object> AuthenticatePrivateRequestAsync(
            IServiceBase authService, IAuthSession session, string userName, string password, string referrerUrl, CancellationToken token=default)
        {
            var authRepo = GetUserAuthRepositoryAsync(authService.Request);
#if NET472 || NETSTANDARD2_0
            await using (authRepo as IAsyncDisposable)
#else
            using (authRepo as IDisposable)
#endif
            {
                var userAuth = await authRepo.GetUserAuthByUserNameAsync(userName, token).ConfigAwait();
                if (userAuth == null)
                    throw HttpError.Unauthorized(ErrorMessages.InvalidUsernameOrPassword.Localize(authService.Request));

                if (await IsAccountLockedAsync(authRepo, userAuth, token: token).ConfigAwait())
                    throw new AuthenticationException(ErrorMessages.UserAccountLocked.Localize(authService.Request));

                await session.PopulateSessionAsync(userAuth, authRepo, token).ConfigAwait();

                session.IsAuthenticated = true;

                if (session.UserAuthName == null)
                    session.UserAuthName = userName;

                var response = await OnAuthenticatedAsync(authService, session, null, null, token).ConfigAwait();
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
    }
}