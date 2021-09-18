using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
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

        [DataMember(Order = 01)] public string ReferrerUrl { get; set; }
        [DataMember(Order = 02)] public string Id { get; set; }
        [DataMember(Order = 03)] public string UserAuthId { get; set; }
        /// <summary>
        /// User chosen Username when available or Email
        /// </summary>
        [DataMember(Order = 04)] public string UserAuthName { get; set; }
        [DataMember(Order = 05)] public string UserName { get; set; }
        [DataMember(Order = 06)] public string TwitterUserId { get; set; }
        [DataMember(Order = 07)] public string TwitterScreenName { get; set; }
        [DataMember(Order = 08)] public string FacebookUserId { get; set; }
        [DataMember(Order = 09)] public string FacebookUserName { get; set; }
        [DataMember(Order = 10)] public string FirstName { get; set; }
        [DataMember(Order = 11)] public string LastName { get; set; }
        [DataMember(Order = 12)] public string DisplayName { get; set; }
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
        [DataMember(Order = 35)] public List<string> Roles { get; set; }
        [DataMember(Order = 36)] public List<string> Permissions { get; set; }
        [DataMember(Order = 37)] public bool IsAuthenticated { get; set; }
        [DataMember(Order = 38)] public bool FromToken { get; set; }
        [DataMember(Order = 39)] public string ProfileUrl { get; set; } //Avatar
        [DataMember(Order = 40)] public string Sequence { get; set; }
        [DataMember(Order = 41)] public long Tag { get; set; }
        [DataMember(Order = 42)] public string AuthProvider { get; set; }
        [DataMember(Order = 43)] public List<IAuthTokens> ProviderOAuthAccess { get; set; }
        [DataMember(Order = 44)] public Dictionary<string, string> Meta { get; set; }
        
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

        public virtual bool IsAuthorized(string provider)
        {
            var tokens = this.GetAuthTokens(provider);
            return AuthenticateService.GetAuthProvider(provider).IsAuthorizedSafe(this, tokens);
        }

        public virtual bool HasPermission(string permission, IAuthRepository authRepo)
        {
            if (!FromToken) //If populated from a token it should have the complete list of permissions
            {
                if (authRepo is IManageRoles managesRoles)
                {
                    if (UserAuthId == null)
                        return false;

                    return managesRoles.HasPermission(this.UserAuthId, permission);
                }
            }

            return this.Permissions != null && this.Permissions.Contains(permission);
        }

        public virtual async Task<bool> HasPermissionAsync(string permission, IAuthRepositoryAsync authRepo, CancellationToken token=default)
        {
            if (!FromToken) //If populated from a token it should have the complete list of permissions
            {
                if (authRepo is IManageRolesAsync managesRoles)
                {
                    if (UserAuthId == null)
                        return false;

                    return await managesRoles.HasPermissionAsync(this.UserAuthId, permission, token);
                }
            }

            return this.Permissions != null && this.Permissions.Contains(permission);
        }

        public virtual bool HasRole(string role, IAuthRepository authRepo)
        {
            if (!FromToken) //If populated from a token it should have the complete list of roles
            {
                if (authRepo is IManageRoles managesRoles)
                {
                    if (UserAuthId == null)
                        return false;

                    return managesRoles.HasRole(this.UserAuthId, role);
                }
            }

            return this.Roles != null && this.Roles.Contains(role);
        }

        public virtual async Task<bool> HasRoleAsync(string role, IAuthRepositoryAsync authRepo, CancellationToken token=default)
        {
            if (!FromToken) //If populated from a token it should have the complete list of roles
            {
                if (authRepo is IManageRolesAsync managesRoles)
                {
                    if (UserAuthId == null)
                        return false;

                    return await managesRoles.HasRoleAsync(this.UserAuthId, role, token);
                }
            }

            return this.Roles != null && this.Roles.Contains(role);
        }

        public virtual ICollection<string> GetRoles(IAuthRepository authRepo)
        {
            if (UserAuthId == null)
                return TypeConstants.EmptyStringArray;

            if (!FromToken) //If populated from a token it should have the complete list of roles
            {
                if (authRepo is IManageRoles managesRoles)
                {
                    return managesRoles.GetRoles(this.UserAuthId);
                }
            }

            return this.Roles != null 
                ? this.Roles
                : TypeConstants.EmptyStringArray;
        }

        public virtual async Task<ICollection<string>> GetRolesAsync(IAuthRepositoryAsync authRepo, CancellationToken token=default)
        {
            if (UserAuthId == null)
                return TypeConstants.EmptyStringArray;

            if (!FromToken) //If populated from a token it should have the complete list of roles
            {
                if (authRepo is IManageRolesAsync managesRoles)
                {
                    return await managesRoles.GetRolesAsync(this.UserAuthId, token);
                }
            }

            return this.Roles != null 
                ? this.Roles
                : TypeConstants.EmptyStringArray;
        }

        public virtual ICollection<string> GetPermissions(IAuthRepository authRepo)
        {
            if (UserAuthId == null)
                return TypeConstants.EmptyStringArray;

            if (!FromToken) //If populated from a token it should have the complete list of roles
            {
                if (authRepo is IManageRoles managesRoles)
                {
                    return managesRoles.GetPermissions(this.UserAuthId);
                }
            }

            return this.Permissions != null 
                ? this.Permissions
                : TypeConstants.EmptyStringArray;
        }

        public virtual async Task<ICollection<string>> GetPermissionsAsync(IAuthRepositoryAsync authRepo, CancellationToken token=default)
        {
            if (UserAuthId == null)
                return TypeConstants.EmptyStringArray;

            if (!FromToken) //If populated from a token it should have the complete list of roles
            {
                if (authRepo is IManageRolesAsync managesRoles)
                {
                    return await managesRoles.GetPermissionsAsync(this.UserAuthId, token);
                }
            }

            return this.Permissions != null 
                ? this.Permissions
                : TypeConstants.EmptyStringArray;
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
    }
}