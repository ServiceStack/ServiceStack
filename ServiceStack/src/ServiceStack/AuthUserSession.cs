using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    [DataContract]
    public class AuthUserSession : IAuthSessionExtended, IMeta
    {
        public AuthUserSession()
        {
            this.ProviderOAuthAccess = new List<IAuthTokens>();
            this.Meta = new Dictionary<string, string>();
        }

        [DataMember(Order = 01)] public virtual string ReferrerUrl { get; set; }
        [DataMember(Order = 02)] public virtual string Id { get; set; }
        [DataMember(Order = 03)] public virtual string UserAuthId { get; set; }
        /// <summary>
        /// User chosen Username when available or Email
        /// </summary>
        [DataMember(Order = 04)] public virtual string UserAuthName { get; set; }
        [DataMember(Order = 05)] public virtual string UserName { get; set; }
        [DataMember(Order = 06)] public string TwitterUserId { get; set; }
        [DataMember(Order = 07)] public string TwitterScreenName { get; set; }
        [DataMember(Order = 08)] public string FacebookUserId { get; set; }
        [DataMember(Order = 09)] public string FacebookUserName { get; set; }
        [DataMember(Order = 10)] public string FirstName { get; set; }
        [DataMember(Order = 11)] public string LastName { get; set; }
        [DataMember(Order = 12)] public virtual string DisplayName { get; set; }
        [DataMember(Order = 13)] public string Company { get; set; }
        [DataMember(Order = 14)] public string Email { get; set; }
        [DataMember(Order = 15)] public string PrimaryEmail { get; set; }
        [DataMember(Order = 16)] public string PhoneNumber { get; set; }
        [DataMember(Order = 17)] public DateTime? BirthDate { get; set; }
        [DataMember(Order = 18)] public string BirthDateRaw { get; set; }
        [DataMember(Order = 19)] public string Address { get; set; }
        [DataMember(Order = 20)] public string Address2 { get; set; }
        [DataMember(Order = 21)] public string City { get; set; }
        [DataMember(Order = 22)] public string State { get; set; }
        [DataMember(Order = 23)] public string Country { get; set; }
        [DataMember(Order = 24)] public string Culture { get; set; }
        [DataMember(Order = 25)] public string FullName { get; set; }
        [DataMember(Order = 26)] public string Gender { get; set; }
        [DataMember(Order = 27)] public string Language { get; set; }
        [DataMember(Order = 28)] public string MailAddress { get; set; }
        [DataMember(Order = 29)] public string Nickname { get; set; }
        [DataMember(Order = 30)] public string PostalCode { get; set; }
        [DataMember(Order = 31)] public string TimeZone { get; set; }
        [DataMember(Order = 32)] public string RequestTokenSecret { get; set; }
        [DataMember(Order = 33)] public DateTime CreatedAt { get; set; }
        [DataMember(Order = 34)] public DateTime LastModified { get; set; }
        [DataMember(Order = 35)] public virtual List<string> Roles { get; set; }
        [DataMember(Order = 36)] public virtual List<string> Permissions { get; set; }
        [DataMember(Order = 37)] public virtual bool IsAuthenticated { get; set; }
        [DataMember(Order = 38)] public virtual bool FromToken { get; set; }
        [DataMember(Order = 39)] public virtual string ProfileUrl { get; set; } //Avatar
        [DataMember(Order = 40)] public string Sequence { get; set; }
        [DataMember(Order = 41)] public long Tag { get; set; }
        [DataMember(Order = 42)] public virtual string AuthProvider { get; set; }
        [DataMember(Order = 43)] public virtual List<IAuthTokens> ProviderOAuthAccess { get; set; }
        [DataMember(Order = 44)] public virtual Dictionary<string, string> Meta { get; set; }
        
        //Claims https://docs.microsoft.com/en-us/previous-versions/windows-identity-foundation/ee727097(v=msdn.10)
        [DataMember(Order = 45)] public List<string> Audiences { get; set; }
        [DataMember(Order = 46)] public List<string> Scopes { get; set; }
        [DataMember(Order = 47)] public string Dns { get; set; }
        [DataMember(Order = 48)] public string Rsa { get; set; }
        [DataMember(Order = 49)] public string Sid { get; set; }
        [DataMember(Order = 50)] public string Hash { get; set; }
        [DataMember(Order = 51)] public string HomePhone { get; set; }
        [DataMember(Order = 52)] public string MobilePhone { get; set; }
        [DataMember(Order = 53)] public string Webpage { get; set; }

        //IdentityUser<TKey>
        [DataMember(Order = 54)] public bool? EmailConfirmed { get; set; }
        [DataMember(Order = 55)] public bool? PhoneNumberConfirmed { get; set; }
        [DataMember(Order = 56)] public bool? TwoFactorEnabled { get; set; }
        [DataMember(Order = 57)] public string SecurityStamp { get; set; }
        [DataMember(Order = 58)] public string Type { get; set; }
        
        [DataMember(Order = 59)] public virtual string RecoveryToken { get; set; }
        [DataMember(Order = 60)] public virtual int? RefId { get; set; }
        [DataMember(Order = 61)] public virtual string RefIdStr { get; set; }

        public virtual bool IsAuthorized(string provider)
        {
            var tokens = this.GetAuthTokens(provider);
            return AuthenticateService.GetAuthProvider(provider).IsAuthorizedSafe(this, tokens);
        }

        public virtual bool HasPermission(string permission, IAuthRepository authRepo)
        {
            if (permission == null)
                throw new ArgumentNullException(nameof(permission));
            
            var permissions = GetPermissions(authRepo);
            return permissions.Contains(permission);
        }

        public virtual async Task<bool> HasPermissionAsync(string permission, IAuthRepositoryAsync authRepo, CancellationToken token=default)
        {
            if (permission == null)
                throw new ArgumentNullException(nameof(permission));
            
            var permissions = await GetPermissionsAsync(authRepo, token).ConfigAwait();
            return permissions.Contains(permission);
        }

        /// <summary>
        /// High-level overridable API that ServiceStack uses to check whether a user has all requiredRoles.
        /// </summary>
        public virtual async Task<bool> HasAllRolesAsync(ICollection<string> requiredRoles,
            IAuthRepositoryAsync authRepo, IRequest req, CancellationToken token = default)
        {
            if (requiredRoles == null)
                throw new ArgumentNullException(nameof(requiredRoles));
            
            var allRoles = await GetRolesAsync(authRepo, token).ConfigAwait();
            if (allRoles.Contains(RoleNames.Admin) || requiredRoles.All(allRoles.Contains))
                return true;

            await this.UpdateFromUserAuthRepoAsync(req, authRepo).ConfigAwait();

            allRoles = await GetRolesAsync(authRepo, token).ConfigAwait();
            if (allRoles.Contains(RoleNames.Admin) || requiredRoles.All(allRoles.Contains))
            {
                await req.SaveSessionAsync(this, token: token).ConfigAwait();
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// High-level overridable API that ServiceStack uses to check whether a user has any of the specified roles.
        /// </summary>
        public virtual async Task<bool> HasAnyRolesAsync(ICollection<string> roles, IAuthRepositoryAsync authRepo,
            IRequest req, CancellationToken token = default)
        {
            if (roles == null)
                throw new ArgumentNullException(nameof(roles));
            
            var userRoles = await GetRolesAsync(authRepo, token).ConfigAwait();
            if (userRoles.Contains(RoleNames.Admin) || roles.Any(userRoles.Contains)) 
                return true;

            await this.UpdateFromUserAuthRepoAsync(req, authRepo).ConfigAwait();

            userRoles = await GetRolesAsync(authRepo, token).ConfigAwait();
            if (userRoles.Contains(RoleNames.Admin) || roles.Any(userRoles.Contains)) 
            {
                await req.SaveSessionAsync(this, token: token).ConfigAwait();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// High-level overridable API that ServiceStack uses to check whether a user has all requiredPermissions.
        /// </summary>
        public virtual async Task<bool> HasAllPermissionsAsync(ICollection<string> requiredPermissions, 
            IAuthRepositoryAsync authRepo, IRequest req, CancellationToken token = default)
        {
            if (requiredPermissions == null)
                throw new ArgumentNullException(nameof(requiredPermissions));
            
            var allPerms = await GetPermissionsAsync(authRepo, token).ConfigAwait();
            if (requiredPermissions.All(allPerms.Contains))
                return true;

            if (await HasRoleAsync(RoleNames.Admin, authRepo, token).ConfigAwait())
                return true;

            await this.UpdateFromUserAuthRepoAsync(req, authRepo).ConfigAwait();

            allPerms = await GetPermissionsAsync(authRepo, token).ConfigAwait();
            if (requiredPermissions.All(allPerms.Contains))
            {
                await req.SaveSessionAsync(this, token: token).ConfigAwait();
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// High-level overridable API that ServiceStack uses to check whether a user has any of the specified permissions.
        /// </summary>
        public virtual async Task<bool> HasAnyPermissionsAsync(ICollection<string> permissions, IAuthRepositoryAsync authRepo,
            IRequest req, CancellationToken token=default)
        {
            if (permissions == null)
                throw new ArgumentNullException(nameof(permissions));
            
            var allPerms = await GetPermissionsAsync(authRepo, token).ConfigAwait();
            if (permissions.Any(allPerms.Contains)) 
                return true;

            if (await HasRoleAsync(RoleNames.Admin, authRepo, token).ConfigAwait())
                return true;

            await this.UpdateFromUserAuthRepoAsync(req, authRepo).ConfigAwait();

            allPerms = await GetPermissionsAsync(authRepo, token).ConfigAwait();
            if (permissions.Any(allPerms.Contains)) 
            {
                await req.SaveSessionAsync(this, token: token).ConfigAwait();
                return true;
            }
            return false;
        }

        public virtual bool HasRole(string role, IAuthRepository authRepo)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));
            
            var roles = GetRoles(authRepo);
            return roles.Contains(role);
        }

        public virtual async Task<bool> HasRoleAsync(string role, IAuthRepositoryAsync authRepo, CancellationToken token=default)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));
            
            var roles = await GetRolesAsync(authRepo, token).ConfigAwait();
            return roles.Contains(role);
        }

        public virtual ICollection<string> GetRoles(IAuthRepository authRepo)
        {
            if (UserAuthId != null)
            {
                if (!FromToken) //If populated from a token it should have the complete list of roles
                {
                    if (authRepo is IManageRoles managesRoles)
                    {
                        return managesRoles.GetRoles(this.UserAuthId);
                    }
                }
            }

            return this.Roles != null 
                ? this.Roles
                : TypeConstants.EmptyStringArray;
        }

        public virtual async Task<ICollection<string>> GetRolesAsync(IAuthRepositoryAsync authRepo, CancellationToken token=default)
        {
            if (UserAuthId != null)
            {
                if (!FromToken) //If populated from a token it should have the complete list of roles
                {
                    if (authRepo is IManageRolesAsync managesRoles)
                    {
                        return await managesRoles.GetRolesAsync(this.UserAuthId, token);
                    }
                }
            }

            return this.Roles != null 
                ? this.Roles
                : TypeConstants.EmptyStringArray;
        }

        public virtual ICollection<string> GetPermissions(IAuthRepository authRepo)
        {
            if (UserAuthId != null)
            {
                if (!FromToken) //If populated from a token it should have the complete list of roles
                {
                    if (authRepo is IManageRoles managesRoles)
                    {
                        return managesRoles.GetPermissions(this.UserAuthId);
                    }
                }
            }

            return this.Permissions != null 
                ? this.Permissions
                : TypeConstants.EmptyStringArray;
        }

        public virtual async Task<ICollection<string>> GetPermissionsAsync(IAuthRepositoryAsync authRepo, CancellationToken token=default)
        {
            if (UserAuthId != null)
            {
                if (!FromToken) //If populated from a token it should have the complete list of roles
                {
                    if (authRepo is IManageRolesAsync managesRoles)
                    {
                        return await managesRoles.GetPermissionsAsync(this.UserAuthId, token);
                    }
                }
            }

            return this.Permissions != null 
                ? this.Permissions
                : TypeConstants.EmptyStringArray;
        }

        public virtual async Task<List<Claim>> AsClaimsAsync(IAuthRepositoryAsync authRepo)
        {
            List<Claim> claims = new() {
                new Claim(ClaimTypes.NameIdentifier, UserAuthId),
                new Claim(ClaimTypes.Name, DisplayName),
                new Claim(ClaimTypes.Email, Email == null || UserAuthName.Contains('@') ? UserAuthName : Email),
                new Claim(JwtClaimTypes.Picture, ProfileUrl ?? JwtClaimTypes.DefaultProfileUrl),
            };

            var roles = (FromToken
                ? Roles
                : await GetRolesAsync(authRepo)).OrEmpty().ToList();
            // Add all App Roles to Admin Users
            if (roles.Contains(RoleNames.Admin))
            {
                roles.AddDistinctRange(HostContext.Metadata.GetAllRoles());
            }
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var perms = FromToken
                ? Permissions
                : await GetRolesAsync(authRepo);
            foreach (var permission in perms.OrEmpty())
            {
                claims.Add(new Claim(JwtClaimTypes.Permissions, permission));
            }
            return claims;
        }

        public virtual void OnLoad(IRequest httpReq) {}
        public virtual void OnCreated(IRequest httpReq) {}

        public virtual void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase service) {}

        public virtual Task OnRegisteredAsync(IRequest httpReq, IAuthSession session, IServiceBase service, CancellationToken token = default) =>
            TypeConstants.EmptyTask;

        public virtual void OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo) { }

        public virtual Task OnAuthenticatedAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo,
            CancellationToken token = default) => TypeConstants.EmptyTask;

        public virtual void OnLogout(IServiceBase authService) {}
        public virtual Task OnLogoutAsync(IServiceBase authService, CancellationToken token = default) => TypeConstants.EmptyTask;

        
        public virtual IHttpResult Validate(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo) => null;
        public virtual Task<IHttpResult> ValidateAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo,
            CancellationToken token = default) => ((IHttpResult)null).InTask();

        public virtual HashSet<string> GetUserAttributes(IRequest request)
        {
            var attrs = new HashSet<string> {
                HostContext.DebugMode ? When.Development : When.Production
            };

            if (IsAuthenticated)
            {
                attrs.Add(When.IsAuthenticated);
                
                if (HostContext.HasValidAuthSecret(request))
                    attrs.Add(RoleNames.Admin);

                var roles = Roles;
                var permissions = Permissions;
                
                if (roles.IsEmpty() && permissions.IsEmpty())
                {
                    var authRepo = HostContext.AppHost.GetAuthRepository(request);
                    using (authRepo as IDisposable)
                    {
                        if (authRepo is IManageRoles manageRoles)
                        {
                            manageRoles.GetRolesAndPermissions(UserAuthId, out var managedRoles, out var managedPerms);
                            roles = managedRoles.ToList();
                            permissions = managedPerms.ToList();
                        }
                    }
                }
                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        attrs.Add(When.HasRole(role));
                    }
                }
                if (permissions != null)
                {
                    foreach (var perm in permissions)
                    {
                        attrs.Add(When.HasPermission(perm));
                    }
                }
                if (this is IAuthSessionExtended extended)
                {
                    if (extended.Scopes != null)
                    {
                        foreach (var item in extended.Scopes)
                        {
                            attrs.Add(When.HasScope(item));
                        }
                    }
                }
                var claims = request.GetClaims();
                if (claims != null)
                {
                    foreach (var claim in claims)
                    {
                        attrs.Add(When.HasClaim(claim.ToString()));
                    }
                }
            }
            return attrs;
        }
    }

    public class WebSudoAuthUserSession : AuthUserSession, IWebSudoAuthSession
    {
        [DataMember(Order = 41)]
        public DateTime AuthenticatedAt { get; set; }

        [DataMember(Order = 42)]
        public int AuthenticatedCount { get; set; }

        [DataMember(Order = 43)]
        public DateTime? AuthenticatedWebSudoUntil { get; set; }
    }

    public static class AuthSessionExtensions
    {
        public static void AddAuthToken(this IAuthSession session, IAuthTokens tokens)
        {
            if (session.ProviderOAuthAccess == null)
                session.ProviderOAuthAccess = new List<IAuthTokens>();

            session.ProviderOAuthAccess.Add(tokens);
        }

        public static List<IAuthTokens> GetAuthTokens(this IAuthSession session)
        {
            return session.ProviderOAuthAccess ?? TypeConstants<IAuthTokens>.EmptyList;
        }

        public static IAuthTokens GetAuthTokens(this IAuthSession session, string provider)
        {
            if (session.ProviderOAuthAccess != null)
            {
                foreach (var tokens in session.ProviderOAuthAccess)
                {
                    if (string.Compare(tokens.Provider, provider, StringComparison.OrdinalIgnoreCase) == 0)
                        return tokens;
                }
            }
            return null;
        }

        public static string GetProfileUrl(this IAuthSession authSession, string defaultUrl = null)
        {
            if (authSession.ProfileUrl != null)
                return authSession.ProfileUrl;

            var profile = HostContext.TryResolve<IAuthMetadataProvider>();
            return profile == null ? defaultUrl : profile.GetProfileUrl(authSession, defaultUrl);
        }

        public static string GetSafeDisplayName(this IAuthSession authSession)
        {
            if (authSession != null)
            {
                var displayName = authSession.UserName != null 
                    && authSession.UserName.IndexOf('@') == -1      // don't use email
                    && !long.TryParse(authSession.UserName, out _) // don't use id number
                        ? authSession.UserName
                        : authSession.DisplayName.SafeVarName();

                return displayName;
            }
            return null;
        }

        public static async Task UpdateFromUserAuthRepoAsync(this IAuthSession session, IRequest req, IAuthRepositoryAsync authRepo = null)
        {
            if (session == null)
                return;

            var newAuthRepo = authRepo == null
                ? HostContext.AppHost.GetAuthRepositoryAsync(req)
                : null;
            
            if (authRepo == null)
                authRepo = newAuthRepo;

            if (authRepo == null)
                return;

            using (newAuthRepo as IDisposable)
            {
                var userAuth = await authRepo.GetUserAuthAsync(session, null).ConfigAwait();
                session.UpdateSession(userAuth);
            }
        }

        public static void UpdateFromUserAuthRepo(this IAuthSession session, IRequest req, IAuthRepository authRepo = null)
        {
            if (session == null)
                return;

            var newAuthRepo = authRepo == null
                ? HostContext.AppHost.GetAuthRepository(req)
                : null;
            
            if (authRepo == null)
                authRepo = newAuthRepo;

            if (authRepo == null)
                return;

            using (newAuthRepo as IDisposable)
            {
                var userAuth = authRepo.GetUserAuth(session, null);
                session.UpdateSession(userAuth);
            }
        }

        public static Task<bool> HasAllRolesAsync(this IAuthSession session, ICollection<string> requiredRoles, 
            IAuthRepositoryAsync authRepo, IRequest req, CancellationToken token = default)
        {
            if (session is IAuthSessionExtended extended) // always true for sessions inheriting AuthUserSession
                return extended.HasAllRolesAsync(requiredRoles, authRepo, req, token);

#pragma warning disable 618
            return session.HasAllRoles(requiredRoles, (IAuthRepository) authRepo, req).InTask();
#pragma warning restore 618
        }
        
        [Obsolete("Use HasAllRolesAsync")]
        internal static bool HasAllRoles(this IAuthSession session, ICollection<string> requiredRoles, IAuthRepository authRepo, IRequest req)
        {
            var allRoles = session.GetRoles(authRepo);
            if (allRoles.Contains(RoleNames.Admin) || requiredRoles.All(allRoles.Contains))
                return true;

            session.UpdateFromUserAuthRepo(req, authRepo);

            allRoles = session.GetRoles(authRepo);
            if (allRoles.Contains(RoleNames.Admin) || requiredRoles.All(allRoles.Contains))
            {
                req.SaveSession(session);
                return true;
            }

            return false;
        }

        public static Task<bool> HasAnyRolesAsync(this IAuthSession session, ICollection<string> roles,
            IAuthRepositoryAsync authRepo, IRequest req, CancellationToken token = default)
        {
            if (session is IAuthSessionExtended extended) // always true for sessions inheriting AuthUserSession
                return extended.HasAnyRolesAsync(roles, authRepo, req, token);

#pragma warning disable 618
            return session.HasAnyRoles(roles, (IAuthRepository) authRepo, req).InTask();
#pragma warning restore 618
        }


        [Obsolete("Use HasAnyRolesAsync")]
        internal static bool HasAnyRoles(this IAuthSession session, ICollection<string> roles,
            IAuthRepository authRepo, IRequest req)
        {
            var userRoles = session.GetRoles(authRepo);
            if (userRoles.Contains(RoleNames.Admin) || roles.Any(userRoles.Contains)) 
                return true;

            session.UpdateFromUserAuthRepo(req, authRepo);

            userRoles = session.GetRoles(authRepo);
            if (userRoles.Contains(RoleNames.Admin) || roles.Any(userRoles.Contains)) 
            {
                req.SaveSession(session);
                return true;
            }
            return false;
        }

        public static Task<bool> HasAllPermissionsAsync(this IAuthSession session, ICollection<string> requiredPermissions, 
            IAuthRepositoryAsync authRepo, IRequest req, CancellationToken token = default)
        {
            if (session is IAuthSessionExtended extended) // always true for sessions inheriting AuthUserSession
                return extended.HasAllPermissionsAsync(requiredPermissions, authRepo, req, token);

#pragma warning disable 618
            return session.HasAllPermissions(requiredPermissions, (IAuthRepository) authRepo, req).InTask();
#pragma warning restore 618
        }
        
        [Obsolete("Use HasAllPermissionsAsync")]
        internal static bool HasAllPermissions(this IAuthSession session, ICollection<string> requiredPermissions, 
            IAuthRepository authRepo, IRequest req)
        {
            var allPerms = session.GetPermissions(authRepo);
            if (requiredPermissions.All(allPerms.Contains))
                return true;

            if (session.HasRole(RoleNames.Admin, authRepo))
                return true;

            session.UpdateFromUserAuthRepo(req, authRepo);

            allPerms = session.GetPermissions(authRepo);
            if (requiredPermissions.All(allPerms.Contains))
            {
                req.SaveSession(session);
                return true;
            }
            return false;
        }
        
        public static Task<bool> HasAnyPermissionsAsync(this IAuthSession session, ICollection<string> permissions,
            IAuthRepositoryAsync authRepo, IRequest req, CancellationToken token = default)
        {
            if (session is IAuthSessionExtended extended) // always true for sessions inheriting AuthUserSession
                return extended.HasAnyPermissionsAsync(permissions, authRepo, req, token);

#pragma warning disable 618
            return session.HasAnyPermissions(permissions, (IAuthRepository) authRepo, req).InTask();
#pragma warning restore 618
        }

        [Obsolete("Use HasAnyRolesAsync")]
        internal static bool HasAnyPermissions(this IAuthSession session, ICollection<string> permissions,
            IAuthRepository authRepo, IRequest req)
        {
            var allPerms = session.GetPermissions(authRepo);
            if (permissions.Any(allPerms.Contains)) 
                return true;

            if (session.HasRole(RoleNames.Admin, authRepo))
                return true;

            allPerms = session.GetPermissions(authRepo);
            if (permissions.Any(allPerms.Contains)) 
            {
                req.SaveSession(session);
                return true;
            }
            return false;
        }
    }
}