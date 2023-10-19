#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;

namespace ServiceStack.Auth;

public static class IdentityAuth
{
    public static string TokenCookie = Keywords.TokenCookie;
    public static string RefreshTokenCookie = Keywords.RefreshTokenCookie;
    public static IIdentityAuthContext? Config { get; private set; }

    public static IdentityAuthContext<TUser>? Instance<TUser>()
        where TUser : IdentityUser
        => Config as IdentityAuthContext<TUser>;

    public static Action<AuthFeature> For<TUser>(Action<IdentityAuthContext<TUser>> configure)
        where TUser : IdentityUser
    {
        var ctx = new IdentityAuthContext<TUser>(
            () => new IdentityAuthSession(new ClaimsPrincipal()),
            new IdentityApplicationAuthProvider<TUser>(),
            new IdentityCredentialsAuthProvider<TUser>(),
            new IdentityJwtAuthProvider<TUser>());

        Config = ctx;
        configure(ctx);

        return authFeature =>
        {
            var authProviders = new List<IAuthProvider>();
            if (ctx.AuthApplication != null) authProviders.Add(ctx.AuthApplication);
            if (ctx is { EnableCredentialsAuth: true, AuthCredentials: not null }) authProviders.Add(ctx.AuthCredentials);
            if (ctx is { EnableJwtAuth: true, AuthJwt: not null }) authProviders.Add(ctx.AuthJwt);
            authFeature.RegisterAuthProviders(authProviders.ToArray());
            authFeature.SessionFactory = ctx.SessionFactory;
            authFeature.RegisterPlugins.RemoveAll(x => x is SessionFeature);

            // Always remove IAuthRepo Services
            authFeature.IncludeAssignRoleServices = false;

            if (ctx.IncludeAssignRoleServices)
            {
                authFeature.ServiceRoutes[typeof(IdentityAssignRolesService<TUser>)] =
                    new[] { "/" + LocalizedStrings.AssignRoles.Localize() };
                authFeature.ServiceRoutes[typeof(IdentityUnAssignRolesService<TUser>)] =
                    new[] { "/" + LocalizedStrings.UnassignRoles.Localize() };
            }
            if (ctx.IncludeRegisterService)
            {
                authFeature.ServiceRoutes[typeof(IdentityRegisterService<TUser>)] =
                    new[] { "/" + "register".Localize() };
                HostContext.Container.RegisterAs<IdentityRegistrationValidator<TUser>, IValidator<Register>>();
            }

            authFeature.OnAfterInit.Add(feature =>
            {
                if (ctx.AccessDeniedPath != null)
                    feature.HtmlRedirectAccessDenied = ctx.AccessDeniedPath;
                if (ctx.LoginPath != null)
                    feature.HtmlRedirect = ctx.LoginPath;
                if (ctx.LogoutPath != null)
                    feature.HtmlLogoutRedirect = ctx.LogoutPath;
                if (ctx.ReturnUrlParameter != null)
                    feature.HtmlRedirectReturnParam = ctx.ReturnUrlParameter;
            });
        };
    }

    public static IApplicationBuilder UseJwtCookie(this IApplicationBuilder app, string? cookieName = null)
    {
        return app.Use(async (context, next) =>
        {
            var cookie = context.Request.Cookies[cookieName ?? TokenCookie];
            if (cookie != null && !context.Request.Headers.ContainsKey(HttpHeaders.Authorization))
                context.Request.Headers.Append(HttpHeaders.Authorization, "Bearer " + cookie);
            await next.Invoke();
        });
    }
}

public interface IIdentityAuthContext
{
    Func<IAuthSession> SessionFactory { get; }
}

public class IdentityAuthContext<TUser> : IIdentityAuthContext
    where TUser : IdentityUser
{
    public IdentityAuthContext(Func<IAuthSession> sessionFactory,
        IdentityApplicationAuthProvider<TUser> authApplication,
        IdentityCredentialsAuthProvider<TUser> authCredentials,
        IdentityJwtAuthProvider<TUser> authJwt)
    {
        AuthApplication = authApplication;
        AuthCredentials = authCredentials;
        AuthJwt = authJwt;
        SessionFactory = sessionFactory;
    }

    public Func<IAuthSession> SessionFactory { get; set; }
    public IdentityApplicationAuthProvider<TUser>? AuthApplication { get; set; }
    public IdentityCredentialsAuthProvider<TUser>? AuthCredentials { get; set; }
    public IdentityJwtAuthProvider<TUser>? AuthJwt { get; set; }

    public bool EnableCredentialsAuth { get; set; }
    public bool EnableJwtAuth { get; set; }

    public void DisableApplicationCookie() => AuthApplication = null;
    public void DisableCredentialsAuth() => AuthCredentials = null;
    public void DisableJwt() => AuthJwt = null;

    public string? LoginPath { get; set; }
    public string? LogoutPath { get; set; }
    public string? AccessDeniedPath { get; set; }
    public string? ReturnUrlParameter { get; set; }

    /// <summary>
    /// Register ServiceStack Identity Register Service
    /// </summary>
    public bool IncludeRegisterService { get; set; }

    /// <summary>
    /// Register ServiceStack Identity Un/Assign Roles Services
    /// </summary>
    public bool IncludeAssignRoleServices { get; set; }

    public List<string> AssignRolesToAdminUsers { get; set; } = new() {
        RoleNames.Admin,
    };

    public Func<TUser, IAuthSession> UserToSessionConverter { get; set; } = DefaultUserToSessionConverter;
    public Func<IAuthSession, TUser> SessionToUserConverter { get; set; } = DefaultSessionToUserConverter;

    public static TUser DefaultSessionToUserConverter(IAuthSession session)
    {
        var to = session.ConvertTo<TUser>();
        return to;
    }

    public static IAuthSession DefaultUserToSessionConverter(TUser user)
    {
        var to = user.ConvertTo<AuthUserSession>();
        return to;
    }
}

public interface IRequireClaimsPrincipal
{
    ClaimsPrincipal User { get; set; }
}

public class IdentityAuthSession : AuthUserSession, IRequireClaimsPrincipal
{
    public IdentityAuthSession(ClaimsPrincipal user) => User = user;
    [IgnoreDataMember] public ClaimsPrincipal User { get; set; }
}