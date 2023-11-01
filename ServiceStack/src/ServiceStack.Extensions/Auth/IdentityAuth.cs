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
    public static IIdentityApplicationAuthProvider? ApplicationAuthProvider { get; private set; }
    public static IIdentityApplicationAuthProvider AuthApplication => ApplicationAuthProvider
        ?? throw new Exception("IdentityAuth.AuthApplication is not configured");

    public static IdentityAuthContext<TUser, TKey>? Instance<TUser, TKey>()
        where TKey : IEquatable<TKey>
        where TUser : IdentityUser<TKey>
    => Config as IdentityAuthContext<TUser, TKey>;

    public static Action<AuthFeature> For<TUser>(Action<IdentityAuthContext<TUser, string>> configure)
        where TUser : IdentityUser<string> => For<TUser, string>(configure);

    public static Action<AuthFeature> For<TUser,TKey>(Action<IdentityAuthContext<TUser, TKey>> configure)
        where TKey : IEquatable<TKey>
        where TUser : IdentityUser<TKey>
    {
        var ctx = new IdentityAuthContext<TUser, TKey>(
            () => new IdentityAuthSession(new ClaimsPrincipal()),
            new IdentityApplicationAuthProvider<TUser, TKey>(),
            new IdentityCredentialsAuthProvider<TUser, TKey>(),
            new IdentityJwtAuthProvider<TUser, TKey>());

        Config = ctx;
        ApplicationAuthProvider = ctx.AuthApplication;
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
                authFeature.ServiceRoutes[typeof(IdentityAssignRolesService<TUser, TKey>)] =
                    new[] { "/" + LocalizedStrings.AssignRoles.Localize() };
                authFeature.ServiceRoutes[typeof(IdentityUnAssignRolesService<TUser, TKey>)] =
                    new[] { "/" + LocalizedStrings.UnassignRoles.Localize() };
            }
            if (ctx.IncludeRegisterService)
            {
                authFeature.ServiceRoutes[typeof(IdentityRegisterService<TUser, TKey>)] =
                    new[] { "/" + "register".Localize() };
                HostContext.Container.RegisterAs<IdentityRegistrationValidator<TUser, TKey>, IValidator<Register>>();
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

public class IdentityAuthContext<TUser, TKey> : IIdentityAuthContext
        where TKey : IEquatable<TKey>
        where TUser : IdentityUser<TKey>
{
    public IdentityAuthContext(Func<IAuthSession> sessionFactory,
        IdentityApplicationAuthProvider<TUser, TKey> authApplication,
        IdentityCredentialsAuthProvider<TUser, TKey> authCredentials,
        IdentityJwtAuthProvider<TUser, TKey> authJwt)
    {
        AuthApplication = authApplication;
        AuthCredentials = authCredentials;
        AuthJwt = authJwt;
        SessionFactory = sessionFactory;
    }

    public Func<IAuthSession> SessionFactory { get; set; }
    public IdentityApplicationAuthProvider<TUser, TKey>? AuthApplication { get; set; }
    public IdentityCredentialsAuthProvider<TUser, TKey>? AuthCredentials { get; set; }
    public IdentityJwtAuthProvider<TUser, TKey>? AuthJwt { get; set; }

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