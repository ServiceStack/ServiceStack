using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public static class IdentityAuth
{
    public static string TokenCookie = Keywords.TokenCookie;
    public static string RefreshTokenCookie = Keywords.RefreshTokenCookie;
    public static IIdentityAuthContext? Config { get; private set; }
    public static IIdentityAuthContextManager? Manager { get; private set; }
    
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
            () => new AuthUserSession(),
            new IdentityApplicationAuthProvider<TUser, TKey>(),
            new IdentityCredentialsAuthProvider<TUser, TKey>(),
            new IdentityJwtAuthProvider<TUser, TKey>(),
            new IdentityBasicAuthProvider<TUser, TKey>());

        Config = ctx;
        Manager = new IdentityAuthContextManager<TUser,TKey>(ctx);
        ApplicationAuthProvider = ctx.AuthApplication;
        configure(ctx);

        return (services, authFeature) =>
        {
            services.AddSingleton<IIdentityAuthContextManager>(Manager);
            services.AddSingleton<IIdentityAuthContext>(ctx);
            services.AddSingleton<IUserResolver,IdentityAuthUserResolver>();

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
                if (ctx.EnableBasicAuth)
                {
                    authProviders.Add(ctx.AuthBasic);
                    services.AddSingleton<IIdentityBasicAuthProvider>(ctx.AuthBasic);
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
                services.RegisterValidator(c => new IdentityRegistrationValidator<TUser, TKey>());
            }

            if (ctx.EnableJwtAuth)
            {
                if (ctx.AuthJwt.EnableRefreshToken)
                {
                    authFeature.ServiceRoutes[typeof(GetAccessTokenIdentityService)] = ["/" + "access-token".Localize()];
                }
                if (ctx.AuthJwt.IncludeConvertSessionToTokenService)
                {
                    authFeature.ServiceRoutes[typeof(ConvertSessionToTokenService)] = ["/" + "session-to-token".Localize()];
                }
            }

            authFeature.OnAppMetadata.Add(meta =>
            {
                meta.Plugins.Auth.IdentityAuth = new()
                {
                    HasRefreshToken = typeof(TUser).HasInterface(typeof(IRequireRefreshToken))
                };
            });

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

    public static Microsoft.EntityFrameworkCore.DbContext ResolveDbContext<TUser>(IServiceProvider services) where TUser : class
    {
        var userStore = services.GetRequiredService<IUserStore<TUser>>();
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

    public static Microsoft.EntityFrameworkCore.DbSet<TRole> ResolveDbRoles<TRole>(Microsoft.EntityFrameworkCore.DbContext dbContext) where TRole : class
    {
        var dbRoles = (Microsoft.EntityFrameworkCore.DbSet<TRole>) TypeProperties.Get(dbContext.GetType()).GetPublicGetter(
            nameof(Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext.Roles))(dbContext);
        return dbRoles;
    }
}

/// <summary>
/// Configure ServiceStack's Identity Auth Integration
/// </summary>
public class IdentityAuthContext<TUser, TKey>(
    Func<IAuthSession> sessionFactory,
    IdentityApplicationAuthProvider<TUser, TKey> authApplication,
    IdentityCredentialsAuthProvider<TUser, TKey> authCredentials,
    IdentityJwtAuthProvider<TUser, TKey> authJwt,
    IdentityBasicAuthProvider<TUser, TKey> authBasic)
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
    /// Basic Auth Provider
    /// </summary>
    public IdentityBasicAuthProvider<TUser, TKey> AuthBasic { get; set; } = authBasic;

    /// <summary>
    /// Enable Identity Cookie Application Auth (default true) 
    /// </summary>
    internal bool EnableApplicationAuth { get; set; } = true;

    /// <summary>
    /// Enable Username/Password SignIn via ServiceStack's Authenticate API (/auth) 
    /// </summary>
    [Obsolete("Use CredentialsAuth()")]
    public bool EnableCredentialsAuth { get; set; }
    
    /// <summary>
    /// Enable Authentication via Identity Auth JWT
    /// </summary>
    internal bool EnableJwtAuth { get; set; }
    
    /// <summary>
    /// Enable Authentication via Basic Auth
    /// </summary>
    internal bool EnableBasicAuth { get; set; }

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

    /// <summary>
    /// Additional custom logic to convert an Identity User to a ServiceStack Session
    /// </summary>
    public Func<TUser, IAuthSession> UserToSessionConverter { get; set; } = DefaultUserToSessionConverter;
    
    /// <summary>
    /// Additional custom logic to convert a ServiceStack Session to an Identity User
    /// </summary>
    public Func<IAuthSession, TUser> SessionToUserConverter { get; set; } = DefaultSessionToUserConverter;

#if NET8_0_OR_GREATER    
    /// <summary>
    /// Admin Users Feature
    /// </summary>
    internal IdentityAdminUsersFeature<TUser, TKey>? AdminUsers { get; set; }
#endif
    
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

    public void ApplicationAuth(Action<IdentityApplicationAuthProvider<TUser,TKey>>? configure=null)
    {
        EnableApplicationAuth = true;
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

    public void BasicAuth(Action<IdentityBasicAuthProvider<TUser,TKey>>? configure=null)
    {
        EnableBasicAuth = true;
        configure?.Invoke(AuthBasic);
    }

#if NET8_0_OR_GREATER
    public void AdminUsersFeature(Action<IdentityAdminUsersFeature<TUser, TKey>>? configure=null)
    {
        AdminUsers = new IdentityAdminUsersFeature<TUser, TKey>();
        configure?.Invoke(AdminUsers);
        ServiceStackHost.InitOptions.Plugins.AddIfNotExists(AdminUsers);
    }
#endif
}

public class IdentityAuthContextManager<TUser, TKey>(IdentityAuthContext<TUser, TKey> context) : IIdentityAuthContextManager
    where TKey : IEquatable<TKey>
    where TUser : IdentityUser<TKey>, new()
{
    public IdentityAuthContext<TUser, TKey> Context => context;

    public async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(string userId, IRequest? request = null)
    {
        var (user,roles) = await GetUserAndRolesByIdAsync(userId, request).ConfigAwait();

        // Get the claims for the user
        var claims = await GetClaimsAsync(user, request).ConfigAwait();

        // Add default claims if needed
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()!));
        claims.Add(new Claim(ClaimTypes.Name, user.UserName!));
        claims.Add(new Claim(ClaimTypes.Email, user.Email!));

        // Get the user roles and add them as claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);

        return new ClaimsPrincipal(claimsIdentity);
    }
    
#if NET8_0_OR_GREATER
    public async Task<List<TUser>> SearchUsersAsync(string query, string? orderBy = null, int? skip = null, int? take = null, IRequest? request = null)
    {
        List<TUser> QueryUsers(DbContext dbContext)
        {
            var feature = Context.AdminUsers!;
            var dbUsers = IdentityAuth.ResolveDbUsers<TUser>(dbContext);
            var q = dbUsers.AsQueryable();
            if (!string.IsNullOrEmpty(query))
            {
                q = feature.SearchUsersFilter(q, query);
            }
            if (skip != null)
                q = q.Skip(skip.Value);
            if (take != null)
                q = q.Take(take.Value);
            q = orderBy != null 
                ? q.OrderBy(orderBy) 
                : feature.DefaultOrderBy != null
                    ? q.OrderBy(feature.DefaultOrderBy)
                    : q.OrderBy(x => x.Id);
            return q.ToList();
        }
        
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            await using var dbContext = IdentityAuth.ResolveDbContext<TUser>(scope.ServiceProvider);
            return QueryUsers(dbContext);
        }
        else
        {
            var services = request.GetServiceProvider();
            var dbContext = IdentityAuth.ResolveDbContext<TUser>(services);
            return QueryUsers(dbContext);
        }
    }
#endif

    public async Task<IdentityResult> UpdateUserAsync(TUser user, IRequest? request = null)
    {
        async Task<IdentityResult> UpdateUser(UserManager<TUser> userManager)
        {
            return await userManager.UpdateAsync(user).ConfigAwait();
        }
        
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await UpdateUser(userManager).ConfigAwait();
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            return await UpdateUser(userManager).ConfigAwait();
        }
    }

    public async Task<IdentityResult> DeleteUserByIdAsync(string userId, IRequest? request = null)
    {
        async Task<IdentityResult> DeleteUser(UserManager<TUser> userManager)
        {
            var user = await userManager.FindByIdAsync(userId).ConfigAwait();
            if (user == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(request));
            return await userManager.DeleteAsync(user).ConfigAwait();
        }
        
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await DeleteUser(userManager).ConfigAwait();
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            return await DeleteUser(userManager).ConfigAwait();
        }
    }

    public async Task<IdentityResult> LockUserAsync(TUser user, DateTimeOffset lockoutEnd, IRequest? request = null)
    {
        async Task<IdentityResult> LockUser(UserManager<TUser> userManager)
        {
            if (!user.LockoutEnabled)
                await userManager.SetLockoutEnabledAsync(user, true).ConfigAwait();
            return await userManager.SetLockoutEndDateAsync(user, lockoutEnd).ConfigAwait();
        }
        
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await LockUser(userManager).ConfigAwait();
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            return await LockUser(userManager).ConfigAwait();
        }
    }

    public async Task<IdentityResult> UnlockUserAsync(TUser user, IRequest? request = null)
    {
        async Task<IdentityResult> UnlockUser(UserManager<TUser> userManager)
        {
            if (!user.LockoutEnabled)
                await userManager.SetLockoutEnabledAsync(user, false).ConfigAwait();
            return await userManager.SetLockoutEndDateAsync(user, null).ConfigAwait();
        }
        
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await UnlockUser(userManager).ConfigAwait();
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            return await UnlockUser(userManager).ConfigAwait();
        }
    }

    public async Task<IdentityResult> ChangePasswordAsync(TUser user, string password, IRequest? request = null)
    {
        async Task<IdentityResult> ChangePassword(UserManager<TUser> userManager)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            return await userManager.ResetPasswordAsync(user, token, password);
        }
        
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await ChangePassword(userManager).ConfigAwait();
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            return await ChangePassword(userManager).ConfigAwait();
        }
    }

    public async Task<IdentityResult> AddRolesAsync(TUser user, IEnumerable<string> roles, IRequest? request = null)
    {
        async Task<IdentityResult> AddRoles(UserManager<TUser> userManager)
        {
            return await userManager.AddToRolesAsync(user, roles).ConfigAwait();
        }
        
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await AddRoles(userManager).ConfigAwait();
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            return await AddRoles(userManager).ConfigAwait();
        }
    }

    public async Task<IdentityResult> RemoveRolesAsync(TUser user, IEnumerable<string> roles, IRequest? request = null)
    {
        async Task<IdentityResult> RemoveRoles(UserManager<TUser> userManager)
        {
            return await userManager.RemoveFromRolesAsync(user, roles).ConfigAwait();
        }
        
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await RemoveRoles(userManager).ConfigAwait();
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            return await RemoveRoles(userManager).ConfigAwait();
        }
    }
    
    public async Task<IdentityResult> AddClaimsAsync(TUser user, IEnumerable<Claim> claims, IRequest? request = null)
    {
        async Task<IdentityResult> AddClaims(UserManager<TUser> userManager)
        {
            return await userManager.AddClaimsAsync(user, claims).ConfigAwait();
        }
        
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await AddClaims(userManager).ConfigAwait();
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            return await AddClaims(userManager).ConfigAwait();
        }
    }

    public async Task<IdentityResult> RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, IRequest? request = null)
    {
        async Task<IdentityResult> RemoveClaims(UserManager<TUser> userManager)
        {
            return await userManager.RemoveClaimsAsync(user, claims).ConfigAwait();
        }
        
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await RemoveClaims(userManager).ConfigAwait();
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            return await RemoveClaims(userManager).ConfigAwait();
        }
    }
    
    public Task<TUser?> FindUserByIdAsync(string userId, IRequest? request = null) =>
        FindUserAsync(userManager => userManager.FindByIdAsync(userId), request);

    public Task<TUser?> FindUserByNameAsync(string userName, IRequest? request = null) =>
        FindUserAsync(userManager => userManager.FindByNameAsync(userName), request);

    public async Task<TUser?> FindUserAsync(Func<UserManager<TUser>, Task<TUser?>> findUser, IRequest? request = null)
    {
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await findUser(userManager).ConfigAwait();
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            return await findUser(userManager).ConfigAwait();
        }
    }

    public Task<IList<Claim>> GetClaimsByIdAsync(string userId, IRequest? request = null) =>
        GetClaimsAsync(async userManager => await GetClaimsAsync(await userManager.FindByIdAsync(userId)).ConfigAwait(), request);

    public Task<IList<Claim>> GetClaimsByNameAsync(string userName, IRequest? request = null) =>
        GetClaimsAsync(async userManager => await GetClaimsAsync(await userManager.FindByNameAsync(userName)).ConfigAwait(), request);

    public Task<IList<Claim>> GetClaimsAsync(TUser? user, IRequest? request = null) =>
        GetClaimsAsync(async userManager => {
            if (user == null) return [];
            return await userManager.GetClaimsAsync(user).ConfigAwait();
        }, request);

    public async Task<IList<Claim>> GetClaimsAsync(Func<UserManager<TUser>, Task<IList<Claim>>> getClaims, IRequest? request = null)
    {
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await getClaims(userManager).ConfigAwait();
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            return await getClaims(userManager).ConfigAwait();
        }
    }

    public Task<(TUser, List<string>)> GetUserAndRolesByIdAsync(string userId, IRequest? request = null) =>
        GetUserAndRolesAsync(userManager => userManager.FindByIdAsync(userId), request);
    
    public Task<(TUser, List<string>)> GetUserAndRolesByNameAsync(string userName, IRequest? request = null) =>
        GetUserAndRolesAsync(userManager => userManager.FindByNameAsync(userName), request);
    
    public async Task<(TUser, List<string>)> GetUserAndRolesAsync(Func<UserManager<TUser>, Task<TUser?>> findUser, IRequest? request = null)
    {
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            var user = await findUser(userManager).ConfigAwait();

            if (user == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(request));
            
            var roles = (await userManager.GetRolesAsync(user).ConfigAwait()).ToList();
            return (user, roles);
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            var user = await findUser(userManager).ConfigAwait();
            
            if (user == null)
            {
                var session = await request.GetSessionAsync().ConfigAwait();
                if (HostContext.AssertPlugin<AuthFeature>().AuthSecretSession == session)
                    user = context.SessionToUserConverter(session);
            }

            if (user == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(request));

            var roles = (await userManager.GetRolesAsync(user).ConfigAwait()).ToList();
            return (user, roles);
        }
    }
    
    public Task<(TUser, List<string>, List<Claim>)> GetUserInfoByIdAsync(string userId, IRequest? request = null) =>
        GetUserInfoAsync(userManager => userManager.FindByIdAsync(userId), request);
    public Task<(TUser, List<string>, List<Claim>)> GetUserInfoByNameAsync(string userName, IRequest? request = null) =>
        GetUserInfoAsync(userManager => userManager.FindByNameAsync(userName), request);
    
    public async Task<(TUser, List<string>, List<Claim>)> GetUserInfoAsync(Func<UserManager<TUser>, Task<TUser?>> findUser, IRequest? request = null)
    {
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            var user = await findUser(userManager).ConfigAwait();

            if (user == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(request));
            
            var roles = (await userManager.GetRolesAsync(user).ConfigAwait()).ToList();
            var claims = (await userManager.GetClaimsAsync(user).ConfigAwait()).ToList();
            return (user, roles, claims);
        }
        else
        {
            var userManager = request.GetServiceProvider().GetRequiredService<UserManager<TUser>>();
            var user = await findUser(userManager).ConfigAwait();
            
            if (user == null)
            {
                var session = await request.GetSessionAsync().ConfigAwait();
                if (HostContext.AssertPlugin<AuthFeature>().AuthSecretSession == session)
                    user = context.SessionToUserConverter(session);
            }

            if (user == null)
                throw HttpError.NotFound(ErrorMessages.UserNotExists.Localize(request));

            var roles = (await userManager.GetRolesAsync(user).ConfigAwait()).ToList();
            var claims = (await userManager.GetClaimsAsync(user).ConfigAwait()).ToList();
            return (user, roles, claims);
        }
    }

    public Task<List<IdentityRole>> GetAllRolesAsync(IRequest? request = null)
    {
        async Task<List<IdentityRole>> GetAllRoles(RoleManager<IdentityRole> roleManager)
        {
            return await roleManager.Roles.ToListAsync().ConfigAwait();
        }
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            return GetAllRoles(roleManager);
        }
        else
        {
            var roleManager = request.GetServiceProvider().GetRequiredService<RoleManager<IdentityRole>>();
            return GetAllRoles(roleManager);
        }
    }

    public Task<(IdentityRole, IList<Claim>)> GetRoleAndClaimsAsync(string roleId, IRequest? request = null)
    {
        async Task<(IdentityRole, IList<Claim>)> GetAllRoleClaims(RoleManager<IdentityRole> roleManager)
        {
            var role = await roleManager.FindByIdAsync(roleId).ConfigAwait();
            if (role == null)
                throw HttpError.NotFound(ErrorMessages.RoleNotExists.Localize(request));
            return (role, await roleManager.GetClaimsAsync(role).ConfigAwait());
        }
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            return GetAllRoleClaims(roleManager);
        }
        else
        {
            var roleManager = request.GetServiceProvider().GetRequiredService<RoleManager<IdentityRole>>();
            return GetAllRoleClaims(roleManager);
        }
    }

    public async Task<IdentityRole> UpdateRoleAsync(string id, string role, 
        IEnumerable<Claim>? addClaims = null, IEnumerable<Claim>? removeClaims = null, 
        IRequest? request = null)
    {
        async Task<IdentityRole> UpdateRole(RoleManager<IdentityRole> roleManager)
        {
            var existingRole = await roleManager.FindByIdAsync(id).ConfigAwait();
            if (existingRole == null)
                throw HttpError.NotFound(ErrorMessages.RoleNotExists.Localize(request));
            existingRole.Name = role;
            var result = await roleManager.UpdateAsync(existingRole).ConfigAwait();
            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);

            List<Task<IdentityResult>> tasks = new();
            if (removeClaims?.Count() > 0)
            {
                var allClaims = await roleManager.GetClaimsAsync(existingRole).ConfigAwait();
                var removeExistingClaims = removeClaims
                    .Select(x => allClaims.FirstOrDefault(c => x.Type == c.Type && x.Value == c.Value))
                    .Where(c => c != null)
                    .Select(c => c!)
                    .ToList();
                if (removeExistingClaims.Count > 0)
                {
                    tasks.AddRange(removeExistingClaims
                        .Select(claim => roleManager.RemoveClaimAsync(existingRole, claim)));
                }
            }

            if (addClaims?.Count() > 0)
            {
                tasks.AddRange(addClaims.Select(c => roleManager.AddClaimAsync(existingRole, c)));
            }
            
            var results = await Task.WhenAll(tasks.ToArray()).ConfigAwait();
            if (results.Any(x => !x.Succeeded))
            {
                throw new Exception(results.First(x => !x.Succeeded).Errors.First().Description);
            }
            
            return existingRole;
        }
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            return await UpdateRole(roleManager).ConfigAwait();
        }
        else
        {
            var roleManager = request.GetServiceProvider().GetRequiredService<RoleManager<IdentityRole>>();
            return await UpdateRole(roleManager).ConfigAwait();
        }
    }

    public async Task<IdentityRole> CreateRoleAsync(string role, IRequest? request = null)
    {
        async Task<IdentityRole> CreateRole(RoleManager<IdentityRole> roleManager)
        {
            var newRole = new IdentityRole(role);
            var result = await roleManager.CreateAsync(newRole).ConfigAwait();
            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);
            return newRole;
        }
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            return await CreateRole(roleManager).ConfigAwait();
        }
        else
        {
            var roleManager = request.GetServiceProvider().GetRequiredService<RoleManager<IdentityRole>>();
            return await CreateRole(roleManager).ConfigAwait();
        }
    }

    public Task DeleteRoleByIdAsync(string id, IRequest? request = null)
    {
        async Task DeleteRole(RoleManager<IdentityRole> roleManager)
        {
            var role = await roleManager.FindByIdAsync(id).ConfigAwait();
            if (role == null)
                throw HttpError.NotFound(ErrorMessages.RoleNotExists.Localize(request));
            var result = await roleManager.DeleteAsync(role).ConfigAwait();
            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);
        }
        if (request == null)
        {
            var scopeFactory = ServiceStackHost.Instance.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            return DeleteRole(roleManager);
        }
        else
        {
            var roleManager = request.GetServiceProvider().GetRequiredService<RoleManager<IdentityRole>>();
            return DeleteRole(roleManager);
        }
    }
}
