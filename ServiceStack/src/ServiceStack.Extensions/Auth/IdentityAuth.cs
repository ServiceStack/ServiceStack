using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
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
        where TUser : IdentityUser<TKey>, new()
    => Config as IdentityAuthContext<TUser, TKey>;

    public static Action<IServiceCollection,AuthFeature> For<TUser>(Action<IdentityAuthContext<TUser, string>> configure)
        where TUser : IdentityUser<string>, new() => For<TUser, string>(configure);

    public static Action<IServiceCollection,AuthFeature> For<TUser,TKey>(Action<IdentityAuthContext<TUser, TKey>> configure)
        where TKey : IEquatable<TKey>
        where TUser : IdentityUser<TKey>, new()
    {
        var ctx = new IdentityAuthContext<TUser, TKey>(
            () => new IdentityAuthSession(new ClaimsPrincipal()),
            new IdentityApplicationAuthProvider<TUser, TKey>(),
            new IdentityCredentialsAuthProvider<TUser, TKey>(),
            new IdentityJwtAuthProvider<TUser, TKey>());

        Config = ctx;
        ApplicationAuthProvider = ctx.AuthApplication;
        configure(ctx);

        return (services, authFeature) =>
        {
            services.AddSingleton<IIdentityAuthContext>(ctx);
            
            var authProviders = new List<IAuthProvider>();
            if (ctx.EnableApplicationAuth)
            {
                authProviders.Add(ctx.AuthApplication);
                services.AddSingleton<IIdentityApplicationAuthProvider>(ctx.AuthApplication);
                if (ctx.EnableCredentialsAuth)
                {
                    authProviders.Add(ctx.AuthCredentials);
                    services.AddSingleton<IIdentityCredentialsAuthProvider>(ctx.AuthCredentials);
                }
            }
            if (ctx.EnableJwtAuth)
            {
                authProviders.Add(ctx.AuthJwt);
                services.AddSingleton<IIdentityJwtAuthProvider>(ctx.AuthJwt);
            }
            
            authFeature.RegisterAuthProviders(authProviders.ToArray());
            authFeature.SessionFactory = ctx.SessionFactory;
            authFeature.RegisterPlugins.RemoveAll(x => x is SessionFeature);

            // Always remove IAuthRepo Services
            authFeature.IncludeAssignRoleServices = false;

            // GET /oauth-provider not needed when using ASP.NET Identity Auth
            authFeature.ServiceRoutesVerbs["/" + LocalizedStrings.Auth.Localize() + "/{provider}"] = "POST";

            if (ctx.IncludeAssignRoleServices)
            {
                authFeature.ServiceRoutes[typeof(IdentityAssignRolesService<TUser, TKey>)] =
                    ["/" + LocalizedStrings.AssignRoles.Localize()];
                authFeature.ServiceRoutes[typeof(IdentityUnAssignRolesService<TUser, TKey>)] =
                    ["/" + LocalizedStrings.UnassignRoles.Localize()];
            }
            if (ctx.IncludeRegisterService)
            {
                authFeature.ServiceRoutes[typeof(IdentityRegisterService<TUser, TKey>)] = ["/" + "register".Localize()];
                services.AddSingleton<IValidator<Register>, IdentityRegistrationValidator<TUser, TKey>>();
            }
            if (ctx.AuthJwt.EnableRefreshToken)
            {
                authFeature.ServiceRoutes[typeof(GetAccessTokenIdentityService)] = ["/" + "access-token".Localize()];
            }
            if (ctx.AuthJwt.IncludeConvertSessionToTokenService)
            {
                authFeature.ServiceRoutes[typeof(ConvertSessionToTokenService)] = ["/" + "session-to-token".Localize()];
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

    public static Microsoft.EntityFrameworkCore.DbContext ResolveDbContext<TUser>(IResolver req) where TUser : class
    {
        var userStore = req.TryResolve<IUserStore<TUser>>() ?? throw new NotSupportedException("IUserStore<TUser> is not registered");
        var dbContextGetter = TypeProperties.Get(userStore.GetType()).GetPublicGetter(
            nameof(Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore.Context));
        if (dbContextGetter is null)
            throw new NotSupportedException("Failed to resolve DbContext from " + userStore.GetType().Name);

        var dbContext = (Microsoft.EntityFrameworkCore.DbContext)dbContextGetter(userStore);
        return dbContext;
    }

    public static Microsoft.EntityFrameworkCore.DbSet<TUser> ResolveDbUsers<TUser>(Microsoft.EntityFrameworkCore.DbContext dbContext) where TUser : class
    {
        var dbUsers = (Microsoft.EntityFrameworkCore.DbSet<TUser>) TypeProperties.Get(dbContext.GetType()).GetPublicGetter(
            nameof(Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserContext<IdentityUser>.Users))(dbContext);
        return dbUsers;
    }
}

public interface IIdentityAuthContext
{
    Func<IAuthSession> SessionFactory { get; }
}

/// <summary>
/// Configure ServiceStack's Identity Auth Integration
/// </summary>
public class IdentityAuthContext<TUser, TKey>(
    Func<IAuthSession> sessionFactory,
    IdentityApplicationAuthProvider<TUser, TKey> authApplication,
    IdentityCredentialsAuthProvider<TUser, TKey> authCredentials,
    IdentityJwtAuthProvider<TUser, TKey> authJwt)
    : IIdentityAuthContext
    where TKey : IEquatable<TKey>
    where TUser : IdentityUser<TKey>, new()
{
    /// <summary>
    /// Specify which Custom AuthUserSession to use
    /// </summary>
    public Func<IAuthSession> SessionFactory { get; set; } = sessionFactory;

    /// <summary>
    /// Application Cookie Identity Auth Provider
    /// </summary>
    public IdentityApplicationAuthProvider<TUser, TKey> AuthApplication { get; set; } = authApplication;

    /// <summary>
    /// Username/Password SignIn Identity Auth Provider
    /// </summary>
    public IdentityCredentialsAuthProvider<TUser, TKey> AuthCredentials { get; set; } = authCredentials;

    /// <summary>
    /// JWT Identity Auth Provider
    /// </summary>
    public IdentityJwtAuthProvider<TUser, TKey> AuthJwt { get; set; } = authJwt;

    /// <summary>
    /// Enable Identity Cookie Application Auth (default true) 
    /// </summary>
    public bool EnableApplicationAuth { get; set; } = true;

    /// <summary>
    /// Enable Username/Password SignIn via ServiceStack's Authenticate API (/auth) 
    /// </summary>
    public bool EnableCredentialsAuth { get; set; }
    
    /// <summary>
    /// Enable Authentication via Identity Auth JWT
    /// </summary>
    public bool EnableJwtAuth { get; set; }

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
    /// Register ServiceStack's Assign & UnAssign Roles Services
    /// </summary>
    public bool IncludeAssignRoleServices { get; set; }

    public List<string> AssignRolesToAdminUsers { get; set; } = [
        RoleNames.Admin
    ];

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

    public void ApplicationAuth(Action<IdentityApplicationAuthProvider<TUser,TKey>>? configure=null) {
        configure?.Invoke(AuthApplication);
    }

    public void CredentialsAuth(Action<IdentityCredentialsAuthProvider<TUser,TKey>>? configure=null)
    {
        EnableCredentialsAuth = true;
        configure?.Invoke(AuthCredentials);
    }

    public void JwtAuth(Action<IdentityJwtAuthProvider<TUser,TKey>>? configure=null)
    {
        EnableJwtAuth = true;
        configure?.Invoke(AuthJwt);
    }
}

public interface IRequireClaimsPrincipal
{
    ClaimsPrincipal User { get; set; }
}

public class IdentityAuthSession(ClaimsPrincipal user) : AuthUserSession, IRequireClaimsPrincipal
{
    [IgnoreDataMember] public ClaimsPrincipal User { get; set; } = user;
}

