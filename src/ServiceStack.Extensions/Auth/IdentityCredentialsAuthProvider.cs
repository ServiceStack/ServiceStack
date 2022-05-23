#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public interface IIdentityCredentialsAuthProvider
    {
        bool LockoutOnFailure { get; set; }
    }
    
    /// <summary>
    /// Implements /auth/credentials authenticating against ASP.NET Identity IdentityUser
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class IdentityCredentialsAuthProvider<TUser> : AuthProvider, IIdentityCredentialsAuthProvider
        where TUser : IdentityUser
    {
        public override string Type => "credentials";
        public static string Name = AuthenticateService.CredentialsProvider;
        public static string Realm = "/auth/" + AuthenticateService.CredentialsProvider;

        public IdentityCredentialsAuthProvider()
        {
            Provider = Name;
            AuthRealm = Realm;
        }

        /// <summary>
        /// Should Lock User of failed attempts
        /// </summary>
        public bool LockoutOnFailure { get; set; }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate? request = null)
        {
            if (request != null)
            {
                if (!LoginMatchesSession(session, request.UserName))
                    return false;
            }
            return session.IsAuthenticated && !session.UserAuthName.IsNullOrEmpty();
        }

        public virtual async Task<SignInResult> TryAuthenticateAsync(IServiceBase authService, 
            string userName, string password, bool? rememberMe=false, CancellationToken token = default)
        {
            var signInManager = authService.Resolve<SignInManager<TUser>>();

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            var result = await signInManager.PasswordSignInAsync(
                userName, password, rememberMe.GetValueOrDefault(), lockoutOnFailure: LockoutOnFailure);

            return result;
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
        
        public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request,
            CancellationToken token = new())
        {
            session = await ResetSessionBeforeLoginAsync(authService, session, request.UserName, token).ConfigAwait();

            SignInResult result = SignInResult.Failed;
            var authFeature = HostContext.AppHost.AssertPlugin<AuthFeature>();
            if (HostContext.HasValidAuthSecret(authService.Request))
            {
                if (request.UserName == authFeature.AuthSecretSession.UserName)
                {
                    session = authFeature.AuthSecretSession;
                    result = SignInResult.Success;
                }
            }

            if (!result.Succeeded)
                result = await TryAuthenticateAsync(authService, request.UserName, request.Password, request.RememberMe, token);

            if (result.Succeeded)
            {
                // _logger.LogInformation("User logged in");
                // return authService.LocalRedirect(authService.Request.GetReturnUrl());
                
                session.IsAuthenticated = true;

                if (session.UserAuthName == null)
                    session.UserAuthName = request.UserName;

                var response = await OnAuthenticatedAsync(authService, session, null, null, token).ConfigAwait();
                if (response != null)
                    return response;

                return new AuthenticateResponse
                {
                    UserId = session.UserAuthId,
                    UserName = request.UserName,
                    SessionId = session.Id,
                    DisplayName = session.DisplayName
                        ?? session.UserName
                        ?? (session.FirstName != null ? $"{session.FirstName} {session.LastName}".Trim() : null)
                        ?? session.Email,
                    ReferrerUrl = authService.Request.GetReturnUrl()
                };                
            }

            var isHtml = authService.Request.ResponseContentType.MatchesContentType(MimeTypes.Html);

            if (result.RequiresTwoFactor)
            {
                if (authFeature.HtmlRedirectLoginWith2Fa != null && isHtml)
                {
                    var redirectUrl = authFeature.HtmlRedirectLoginWith2Fa
                        .AddQueryParam(authFeature.HtmlRedirectReturnParam, authFeature.HtmlRedirect)
                        .AddQueryParam(nameof(Authenticate.RememberMe), request.RememberMe);
                    return authService.Redirect(redirectUrl);
                }
                throw new AuthenticationException(ErrorMessages.Requires2FA.Localize(authService.Request));
            }
            
            if (result.IsLockedOut)
            {
                // _logger.LogWarning("User account locked out");
                if (authFeature.HtmlRedirectLockout != null && isHtml)
                    return authService.Redirect(authFeature.HtmlRedirectLockout);

                throw new AuthenticationException(ErrorMessages.UserAccountLocked.Localize(authService.Request));
            }

            throw HttpError.Unauthorized(ErrorMessages.InvalidUsernameOrPassword.Localize(authService.Request));
        }
    }
}