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

        void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase service);
        void OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo);
        void OnLogout(IServiceBase authService);

        /// <summary>
        /// Fired when a new Session is created
        /// </summary>
        /// <param name="httpReq"></param>
        void OnCreated(IRequest httpReq);
    }

    public interface IAuthSessionExtended : IAuthSession
    {
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