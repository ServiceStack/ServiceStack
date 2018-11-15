#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ServiceStack.Configuration;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class NetCoreIdentityAuthProvider : AuthProvider, IAuthWithRequest, IAuthPlugin
    {
        public const string Name = AuthenticateService.IdentityProvider;
        public const string Realm = "/auth/" + AuthenticateService.IdentityProvider;

        /// <summary>
        /// Whether to override AuthFeature HtmlRedirect with .NET Core Identity defaults
        /// </summary>
        public bool OverrideHtmlRedirect { get; set; } = true;

        public string IdClaimType { get; set; } = ClaimTypes.NameIdentifier;
        public string[] RoleClaimTypes { get; set; } = { ClaimTypes.Role, "role" };
        public string PermissionClaimType { get; set; } = "perm";
        
        public Dictionary<string, string> MapClaimsToSession { get; set; } = new Dictionary<string, string> {
            [ClaimTypes.NameIdentifier] = nameof(AuthUserSession.Id),
            [ClaimTypes.Email] = nameof(AuthUserSession.Email),
            [ClaimTypes.Name] = nameof(AuthUserSession.UserAuthName),
            [ClaimTypes.GivenName] = nameof(AuthUserSession.FirstName),
            [ClaimTypes.Surname] = nameof(AuthUserSession.LastName),
            [ClaimTypes.StreetAddress] = nameof(AuthUserSession.Address),
            [ClaimTypes.Locality] = nameof(AuthUserSession.City),
            [ClaimTypes.StateOrProvince] = nameof(AuthUserSession.State),
            [ClaimTypes.PostalCode] = nameof(AuthUserSession.PostalCode),
            [ClaimTypes.Country] = nameof(AuthUserSession.Country),
            [ClaimTypes.HomePhone] = nameof(AuthUserSession.PhoneNumber),
            [ClaimTypes.MobilePhone] = nameof(AuthUserSession.PhoneNumber),
            [ClaimTypes.DateOfBirth] = nameof(AuthUserSession.BirthDateRaw),
            [ClaimTypes.Gender] = nameof(AuthUserSession.Gender),
        };

        /// <summary>
        /// Run custom filter after session is restored from ClaimsPrincipal
        /// </summary>
        public Action<IAuthSession, ClaimsPrincipal, IRequest> PopulateSessionFilter { get; set; }

        public NetCoreIdentityAuthProvider(IAppSettings appSettings) 
            : base(appSettings, Realm, Name) { }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
        {
            return session.IsAuthenticated;
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            throw new NotImplementedException("NetCoreIdentityAuthProvider Authenticate() should not be called directly");
        }

        public void PreAuthenticate(IRequest req, IResponse res)
        {
            var coreReq = (HttpRequest)req.OriginalRequest;
            var claimsPrincipal = coreReq.HttpContext.User;
            if (claimsPrincipal.Identity?.IsAuthenticated != true)
                return;

            var sessionId = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == IdClaimType);
            if (sessionId == null)
                throw new NotSupportedException($"Claim '{IdClaimType}' is required");

            var session = SessionFeature.CreateNewSession(req, sessionId.Value);
            var meta = (session as IMeta)?.Meta;
            session.AuthProvider = Name;

            var sessionValues = new Dictionary<string,string>();
            
            foreach (var claim in claimsPrincipal.Claims)
            {
                if (RoleClaimTypes.Contains(claim.Type))
                {
                    if (session.Roles == null)
                        session.Roles = new List<string>();
                    session.Roles.Add(claim.Value);
                }
                if (PermissionClaimType == claim.Type)
                {
                    if (session.Permissions == null)
                        session.Permissions = new List<string>();
                    session.Permissions.Add(claim.Value);
                }
                else if (MapClaimsToSession.TryGetValue(claim.Type, out var sessionProp))
                {
                    sessionValues[sessionProp] = claim.Value;
                }
                else if (meta != null)
                {
                    meta[claim.Type] = claim.Value;
                }
            }
            
            session.PopulateFromMap(sessionValues);

            if (session.UserAuthName.IndexOf('@') >= 0)
            {
                session.Email = session.UserAuthName;
            }
            
            PopulateSessionFilter?.Invoke(session, claimsPrincipal, req);

            req.Items[Keywords.Session] = session;
        }

        public void Register(IAppHost appHost, AuthFeature authFeature)
        {
            if (!OverrideHtmlRedirect)
                return;
            
            // defaults: https://github.com/aspnet/Security/blob/master/src/Microsoft.AspNetCore.Authentication.Cookies/CookieAuthenticationDefaults.cs
            authFeature.HtmlRedirect = "~/Account/Login";
            authFeature.HtmlRedirectReturnParam = "ReturnUrl";
            authFeature.HtmlRedirectReturnPathOnly = true;
        }
    }
}
#endif