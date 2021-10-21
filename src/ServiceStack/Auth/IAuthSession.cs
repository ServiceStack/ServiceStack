using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public interface IAuthSession
    {
        string ReferrerUrl { get; set; }
        string Id { get; set; }
        string UserAuthId { get; set; }
        string UserAuthName { get; set; }
        string UserName { get; set; }
        string DisplayName { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string Email { get; set; }
        List<IAuthTokens> ProviderOAuthAccess { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime LastModified { get; set; }
        List<string> Roles { get; set; }
        List<string> Permissions { get; set; }
        bool IsAuthenticated { get; set; }
        bool FromToken { get; set; } //Partially restored from JWT
        string AuthProvider { get; set; }
        string ProfileUrl { get; set; }

        //Used for digest authentication replay protection
        string Sequence { get; set; }

        bool HasRole(string role, IAuthRepository authRepo);
        Task<bool> HasRoleAsync(string role, IAuthRepositoryAsync authRepo, CancellationToken token=default);
        bool HasPermission(string permission, IAuthRepository authRepo);
        Task<bool> HasPermissionAsync(string permission, IAuthRepositoryAsync authRepo, CancellationToken token=default);

        ICollection<string> GetRoles(IAuthRepository authRepo);
        Task<ICollection<string>> GetRolesAsync(IAuthRepositoryAsync authRepo, CancellationToken token = default);
        ICollection<string> GetPermissions(IAuthRepository authRepo);
        Task<ICollection<string>> GetPermissionsAsync(IAuthRepositoryAsync authRepo, CancellationToken token = default);

        bool IsAuthorized(string provider);

        /// <summary>
        /// Fired when a new Session is created
        /// </summary>
        /// <param name="httpReq"></param>
        void OnCreated(IRequest httpReq);

        /// <summary>
        /// Called when the user is registered or on the first OAuth login 
        /// </summary>
        void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase service);

        /// <summary>
        /// Called after the user has successfully authenticated 
        /// </summary>
        void OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo);
        
        /// <summary>
        /// Fired before the session is removed after the /auth/logout Service is called
        /// </summary>
        void OnLogout(IServiceBase authService);
    }

    public interface IAuthSessionExtended : IAuthSession
    {
        string Company { get; set; }
        string PrimaryEmail { get; set; }
        DateTime? BirthDate { get; set; }
        string Address { get; set; }
        string Address2 { get; set; }
        string City { get; set; }
        string State { get; set; }
        string PostalCode { get; set; }
        string Country { get; set; }
        string PhoneNumber { get; set; }
        string BirthDateRaw { get; set; }
        string Gender { get; set; }
        
        //Claims https://docs.microsoft.com/en-us/previous-versions/windows-identity-foundation/ee727097(v=msdn.10)
        List<string> Audiences { get; set; }
        List<string> Scopes { get; set; }
        string Dns { get; set; }
        string Rsa { get; set; }
        string Sid { get; set; }
        string Hash { get; set; }
        string HomePhone { get; set; }
        string MobilePhone { get; set; }
        string Webpage { get; set; }

        //IdentityUser<TKey>
        bool? EmailConfirmed { get; set; }
        bool? PhoneNumberConfirmed { get; set; }
        bool? TwoFactorEnabled { get; set; }
        string SecurityStamp { get; set; }
        string Type { get; set; }

        /// <summary>
        /// High-level overridable API that ServiceStack uses to check whether a user has all requiredRoles.
        /// </summary>
        Task<bool> HasAllRolesAsync(ICollection<string> requiredRoles,
            IAuthRepositoryAsync authRepo, IRequest req, CancellationToken token = default);

        /// <summary>
        /// High-level overridable API that ServiceStack uses to check whether a user has any of the specified roles.
        /// </summary>
        Task<bool> HasAnyRolesAsync(ICollection<string> roles, IAuthRepositoryAsync authRepo,
            IRequest req, CancellationToken token = default);

        /// <summary>
        /// High-level overridable API that ServiceStack uses to check whether a user has all requiredPermissions.
        /// </summary>
        Task<bool> HasAllPermissionsAsync(ICollection<string> requiredPermissions,
            IAuthRepositoryAsync authRepo, IRequest req, CancellationToken token = default);

        /// <summary>
        /// High-level overridable API that ServiceStack uses to check whether a user has any of the specified roles.
        /// </summary>
        Task<bool> HasAnyPermissionsAsync(ICollection<string> permissions, IAuthRepositoryAsync authRepo,
            IRequest req, CancellationToken token = default);
        
        /// <summary>
        /// Fired before Session is resolved
        /// </summary>
        void OnLoad(IRequest httpReq);

        /// <summary>
        /// Called when the user is registered or on the first OAuth login 
        /// </summary>
        Task OnRegisteredAsync(IRequest httpReq, IAuthSession session, IServiceBase service, CancellationToken token=default);

        /// <summary>
        /// Called after the user has successfully authenticated 
        /// </summary>
        Task OnAuthenticatedAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token=default);
        
        /// <summary>
        /// Fired before the session is removed after the /auth/logout Service is called
        /// </summary>
        Task OnLogoutAsync(IServiceBase authService, CancellationToken token=default);

        /// <summary>
        /// Override with Custom Validation logic to Assert if User is allowed to Authenticate. 
        /// Returning a non-null response invalidates Authentication with IHttpResult response returned to client.
        /// </summary>
        IHttpResult Validate(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo);

        /// <summary>
        /// Override with Custom Validation logic to Assert if User is allowed to Authenticate. 
        /// Returning a non-null response invalidates Authentication with IHttpResult response returned to client.
        /// </summary>
        Task<IHttpResult> ValidateAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token=default);
    }

    public interface IWebSudoAuthSession : IAuthSession
    {
        DateTime AuthenticatedAt { get; set; }

        int AuthenticatedCount { get; set; }

        DateTime? AuthenticatedWebSudoUntil { get; set; }
    }
}