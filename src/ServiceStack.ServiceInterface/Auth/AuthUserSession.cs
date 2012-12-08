using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.ServiceInterface.Auth
{
    public class AuthUserSession : IAuthSession
    {
        public AuthUserSession()
        {
            this.ProviderOAuthAccess = new List<IOAuthTokens>();
        }

        public string ReferrerUrl { get; set; }

        public string Id { get; set; }

        public string UserAuthId { get; set; }

        public string UserAuthName { get; set; }

        public string UserName { get; set; }

        public string TwitterUserId { get; set; }

        public string TwitterScreenName { get; set; }

        public string FacebookUserId { get; set; }
        
        public string FacebookUserName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string DisplayName { get; set; }

        public string Email { get; set; }
        
        public string PrimaryEmail { get; set; }

        public DateTime? BirthDate { get; set; }

        public string BirthDateRaw { get; set; }

        public string Country { get; set; }

        public string Culture { get; set; }

        public string FullName { get; set; }

        public string Gender { get; set; }

        public string Language { get; set; }

        public string MailAddress { get; set; }

        public string Nickname { get; set; }

        public string PostalCode { get; set; }

        public string TimeZone { get; set; }

        public string RequestTokenSecret { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastModified { get; set; }

        public List<IOAuthTokens> ProviderOAuthAccess { get; set; }

        public List<string> Roles { get; set; }

        public List<string> Permissions { get; set; }

        public virtual bool IsAuthenticated { get; set; }

        public virtual string Sequence { get; set; }

        public virtual bool IsAuthorized(string provider)
        {
            var tokens = ProviderOAuthAccess.FirstOrDefault(x => x.Provider == provider);
            return AuthService.GetAuthProvider(provider).IsAuthorizedSafe(this, tokens);
        }

        public virtual bool HasPermission(string permission)
        {
            return this.Permissions != null && this.Permissions.Contains(permission);
        }

        public virtual bool HasRole(string role)
        {
            return this.Roles != null && this.Roles.Contains(role);
        }

        public virtual void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo)
        {			
        }

        public virtual void OnLogout(IServiceBase authService) 
        {
        }
    }

    public static class AuthSessionExtensions
    {
        public static IOAuthTokens GetOAuthTokens(this IAuthSession session, string provider)
        {
            foreach (var tokens in session.ProviderOAuthAccess)
            {
                if (string.Compare(tokens.Provider, provider, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return tokens;
            }
            return null;
        }
    }
}