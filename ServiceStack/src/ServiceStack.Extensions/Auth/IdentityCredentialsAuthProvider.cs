#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ServiceStack.Html;
using ServiceStack.Text;

namespace ServiceStack.Auth;

/// <summary>
/// Implements /auth/credentials authenticating against ASP.NET Identity IdentityUser
/// </summary>
public class IdentityCredentialsAuthProvider<TUser,TKey> : IdentityAuthProvider<TUser,TKey>, IIdentityCredentialsAuthProvider
    where TKey : IEquatable<TKey>
    where TUser : IdentityUser<TKey>, new()
{
    public override string Type => "credentials";
    public static string Name = AuthenticateService.CredentialsProvider;
    public static string Realm = "/auth/" + AuthenticateService.CredentialsProvider;

    public IdentityCredentialsAuthProvider()
    {
        Provider = Name;
        AuthRealm = Realm;
        Init();
    }

    protected virtual void Init()
    {
        Sort = -1;
        Label = Provider.ToPascalCase();
        FormLayout = new() {
            Input.For<Authenticate>(x => x.UserName, c =>
            {
                c.Label = "Email address";
                c.Required = true;
            }),
            Input.For<Authenticate>(x => x.Password, c =>
            {
                c.Type = "Password";
                c.Required = true;
            }),
            Input.For<Authenticate>(x => x.RememberMe),
        };
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
        string userName, string password, bool? rememberMe = false, CancellationToken token = default)
    {
        var signInManager = authService.Resolve<SignInManager<TUser>>();

        // This doesn't count login failures towards account lockout
        // To enable password failures to trigger account lockout, set lockoutOnFailure: true
        var result = await signInManager.PasswordSignInAsync(
            userName, password, rememberMe.GetValueOrDefault(), lockoutOnFailure: LockoutOnFailure);

        return result;
    }

    protected virtual async Task<IAuthSession> ResetSessionBeforeLoginAsync(IServiceBase authService, IAuthSession session, string userName, CancellationToken token = default)
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
            await IdentityAuth.AuthApplication.PopulateSessionAsync(
                authService.Request,
                session,
                authService.Request.GetClaimsPrincipal());

            session.IsAuthenticated = true;
            session.UserAuthName ??= request.UserName;

            var response = await OnAuthenticatedAsync(authService, session, null, null, token).ConfigAwait();
            if (response != null)
                return response;

            var ret = new AuthenticateResponse
            {
                UserId = session.UserAuthId,
                UserName = request.UserName,
                SessionId = session.Id,
                DisplayName = session.DisplayName
                              ?? session.UserName
                              ?? (session.FirstName != null ? $"{session.FirstName} {session.LastName}".Trim() : null)
                              ?? session.Email,
                Roles = authFeature.IncludeRolesInAuthenticateResponse
                    ? session.Roles
                    : null,
                ReferrerUrl = authService.Request.GetReturnUrl()
            };

            return ret;
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