using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.Auth;
using ServiceStack.Web;

namespace ServiceStack
{
    [DataContract]
    public class AuthUserSession : IAuthSession
    {
        public AuthUserSession()
        {
            this.ProviderOAuthAccess = new List<IAuthTokens>();
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
        [DataMember(Order = 37)] public virtual bool IsAuthenticated { get; set; }
        [DataMember(Order = 38)] public virtual string Sequence { get; set; }
        [DataMember(Order = 39)] public long Tag { get; set; }
        [DataMember(Order = 40)] public List<IAuthTokens> ProviderOAuthAccess { get; set; }

        public virtual bool IsAuthorized(string provider)
        {
            var tokens = ProviderOAuthAccess.FirstOrDefault(x => x.Provider == provider);
            return AuthenticateService.GetAuthProvider(provider).IsAuthorizedSafe(this, tokens);
        }

        public virtual bool HasPermission(string permission)
        {
            var managesRoles = HostContext.TryResolve<IAuthRepository>() as IManageRoles;
            if (managesRoles != null)
            {
                return managesRoles.HasPermission(this.UserAuthId, permission);
            }

            return this.Permissions != null && this.Permissions.Contains(permission);
        }

        public virtual bool HasRole(string role)
        {
            var managesRoles = HostContext.TryResolve<IAuthRepository>() as IManageRoles;
            if (managesRoles != null)
            {
                return managesRoles.HasRole(this.UserAuthId, role);
            }

            return this.Roles != null && this.Roles.Contains(role);
        }

        [Obsolete("Use OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase service)")]
        public virtual void OnRegistered(IServiceBase service) { }

        public virtual void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase service)
        {
#pragma warning disable 612, 618
            OnRegistered(service);
#pragma warning restore 612, 618
        }

        public virtual void OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo) { }
        public virtual void OnLogout(IServiceBase authService) {}
        public virtual void OnCreated(IRequest httpReq) {}
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
        public static IAuthTokens GetOAuthTokens(this IAuthSession session, string provider)
        {
            foreach (var tokens in session.ProviderOAuthAccess)
            {
                if (string.Compare(tokens.Provider, provider, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return tokens;
            }
            return null;
        }

        public static string GetProfileUrl(this IAuthSession authSession, string defaultUrl = null)
        {
            var profile = HostContext.TryResolve<IAuthMetadataProvider>();
            return profile == null ? defaultUrl : profile.GetProfileUrl(authSession, defaultUrl);
        }

        public static string GetSafeDisplayName(this IAuthSession authSession)
        {
            if (authSession != null)
            {
                return authSession.UserName != null && authSession.UserName.IndexOf('@') < 0
                    ? authSession.UserName
                    : authSession.DisplayName.SafeVarName();
            }
            return null;
        }
    }
}