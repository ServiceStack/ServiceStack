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
                    ["/" + LocalizedStrings.AssignRoles.Localize()];
                authFeature.ServiceRoutes[typeof(IdentityUnAssignRolesService<TUser, TKey>)] =
                    ["/" + LocalizedStrings.UnassignRoles.Localize()];
            }
            if (ctx.IncludeRegisterService)
            {
                authFeature.ServiceRoutes[typeof(IdentityRegisterService<TUser, TKey>)] =
                    ["/" + "register".Localize()];
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

/// <summary>
/// Configure ServiceStack's Identity Auth Integration
/// </summary>
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

    /// <summary>
    /// Specify which Custom AuthUserSession to use
    /// </summary>
    public Func<IAuthSession> SessionFactory { get; set; }
    
    /// <summary>
    /// Application Cookie Identity Auth Provider
    /// </summary>
    public IdentityApplicationAuthProvider<TUser, TKey>? AuthApplication { get; set; }
    
    /// <summary>
    /// Username/Password SignIn Identity Auth Provider
    /// </summary>
    public IdentityCredentialsAuthProvider<TUser, TKey>? AuthCredentials { get; set; }
    
    /// <summary>
    /// JWT Identity Auth Provider
    /// </summary>
    public IdentityJwtAuthProvider<TUser, TKey>? AuthJwt { get; set; }

    /// <summary>
    /// Enable Username/Password SignIn via ServiceStack's Authenticate API (/auth) 
    /// </summary>
    public bool EnableCredentialsAuth { get; set; }
    
    /// <summary>
    /// Enable Authentication via Identity Auth JWT
    /// </summary>
    public bool EnableJwtAuth { get; set; }

    /// <summary>
    /// Disable Authentication via Application Cookie
    /// </summary>
    public void DisableApplicationCookie() => AuthApplication = null;
    /// <summary>
    /// Disable Authentication via Username/Password
    /// </summary>
    public void DisableCredentialsAuth() => AuthCredentials = null;
    /// <summary>
    /// Disable Authentication via JWT
    /// </summary>
    public void DisableJwt() => AuthJwt = null;

    /// <summary>
    /// Where users should redirect to Sign In
    /// </summary>
    public string? LoginPath { get; set; }
    
    /// <summary>
    /// Where users should redirect to after logging out
    /// </summary>
    public string? LogoutPath { get; set; }
    
    /// <summary>
    /// Which path users should be redirected to if they don't have access to a resource
    /// </summary>
    public string? AccessDeniedPath { get; set; }

    /// <summary>
    /// The URL parameter name used to pass the ReturnUrl
    /// </summary>
    public string? ReturnUrlParameter { get; set; }

    /// <summary>
    /// Register ServiceStack's Register API (/register)
    /// </summary>
    public bool IncludeRegisterService { get; set; }

    /// <summary>
    /// Register ServiceStack's Assign & Unassign Roles Services
    /// </summary>
    public bool IncludeAssignRoleServices { get; set; }

    public List<string> AssignRolesToAdminUsers { get; set; } = new() {
        RoleNames.Admin,
    };

    /// <summary>
    /// Additional custom logic to convert an Identity User to a ServiceStack Session
    /// </summary>
    public Func<TUser, IAuthSession> UserToSessionConverter { get; set; } = DefaultUserToSessionConverter;
    
    /// <summary>
    /// Additional custom logic to convert a ServiceStack Session to an Identity User
    /// </summary>
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