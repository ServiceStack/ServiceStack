using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface.Auth
{
    [DataContract]
    public class AuthUserSession : IAuthSession
    {
        public AuthUserSession()
        {
            this.ProviderOAuthAccess = new List<IOAuthTokens>();
        }

        [DataMember(Order = 01)] public string ReferrerUrl { get; set; }
        [DataMember(Order = 02)] public string Id { get; set; }
        [DataMember(Order = 03)] public string UserAuthId { get; set; }
        [DataMember(Order = 04)] public string UserAuthName { get; set; }
        [DataMember(Order = 05)] public string UserName { get; set; }
        [DataMember(Order = 06)] public string TwitterUserId { get; set; }
        [DataMember(Order = 07)] public string TwitterScreenName { get; set; }
        [DataMember(Order = 08)] public string FacebookUserId { get; set; }
        [DataMember(Order = 09)] public string FacebookUserName { get; set; }
        [DataMember(Order = 10)] public string FirstName { get; set; }
        [DataMember(Order = 11)] public string LastName { get; set; }
        [DataMember(Order = 12)] public string DisplayName { get; set; }
        [DataMember(Order = 13)] public string Email { get; set; }
        [DataMember(Order = 14)] public string PrimaryEmail { get; set; }
        [DataMember(Order = 15)] public DateTime? BirthDate { get; set; }
        [DataMember(Order = 16)] public string BirthDateRaw { get; set; }
        [DataMember(Order = 17)] public string Country { get; set; }
        [DataMember(Order = 18)] public string Culture { get; set; }
        [DataMember(Order = 19)] public string FullName { get; set; }
        [DataMember(Order = 20)] public string Gender { get; set; }
        [DataMember(Order = 21)] public string Language { get; set; }
        [DataMember(Order = 22)] public string MailAddress { get; set; }
        [DataMember(Order = 23)] public string Nickname { get; set; }
        [DataMember(Order = 24)] public string PostalCode { get; set; }
        [DataMember(Order = 25)] public string TimeZone { get; set; }
        [DataMember(Order = 26)] public string RequestTokenSecret { get; set; }
        [DataMember(Order = 27)] public DateTime CreatedAt { get; set; }
        [DataMember(Order = 28)] public DateTime LastModified { get; set; }
        [DataMember(Order = 29)] public List<IOAuthTokens> ProviderOAuthAccess { get; set; }
        [DataMember(Order = 30)] public List<string> Roles { get; set; }
        [DataMember(Order = 31)] public List<string> Permissions { get; set; }
        [DataMember(Order = 32)] public virtual bool IsAuthenticated { get; set; }
        [DataMember(Order = 33)] public virtual string Sequence { get; set; }
        [DataMember(Order = 34)] public virtual long Tag { get; set; }

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

        public virtual void OnRegistered(IServiceBase registrationService) {}
        public virtual void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo) {}
        public virtual void OnLogout(IServiceBase authService) {}
        public virtual void OnCreated(IHttpRequest httpReq) {}
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