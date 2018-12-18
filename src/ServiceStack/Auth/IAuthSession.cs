using System;
using System.Collections.Generic;
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
        bool HasPermission(string permission, IAuthRepository authRepo);

        bool IsAuthorized(string provider);

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

        /// <summary>
        /// Fired when a new Session is created
        /// </summary>
        /// <param name="httpReq"></param>
        void OnCreated(IRequest httpReq);
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
        
        /// <summary>
        /// Fired before Session is resolved
        /// </summary>
        void OnLoad(IRequest httpReq);

        /// <summary>
        /// Override with Custom Validation logic to Assert if User is allowed to Authenticate. 
        /// Returning a non-null response invalidates Authentication with IHttpResult response returned to client.
        /// </summary>
        IHttpResult Validate(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo);
    }

    public interface IWebSudoAuthSession : IAuthSession
    {
        DateTime AuthenticatedAt { get; set; }

        int AuthenticatedCount { get; set; }

        DateTime? AuthenticatedWebSudoUntil { get; set; }
    }
}