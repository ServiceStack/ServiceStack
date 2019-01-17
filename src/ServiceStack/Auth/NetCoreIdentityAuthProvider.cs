#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServiceStack.Configuration;
using ServiceStack.Host.NetCore;
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

        /// <summary>
        /// Whether to auto sign-in ServiceStack Sessions for pass-through requests not handled by ServiceStack
        /// </summary>
        public bool AutoSignInSessions { get; set; } = true;

        public Func<IRequest, bool> AutoSignInSessionsMatching { get; set; }

        public string AuthenticationType { get; set; } = "Identity.Application"; //Used by SignInManager<T>.IsSignedIn()
        public string Issuer { get; set; } = HostContext.ServiceName;

        public string IdClaimType
        {
            get => IdClaimTypes.FirstOrDefault();
            set => IdClaimTypes = new List<string> { value };
        }
        
        public List<string> IdClaimTypes { get; set; } = new List<string> {
            ClaimTypes.NameIdentifier, //ASP.NET Identity default
            "sub",                     //JWT User
        };
        
        /// <summary>
        /// Allow access to JWT Client Apps containing the client_id or 'null' to allow all Authenticated client_id's (default). 
        /// </summary>
        public List<string> RestrictToClientIds { get; set; }
        
        public string RoleClaimType { get; set; } = ClaimTypes.Role;
        public string PermissionClaimType { get; set; } = "perm";
        
        /// <summary>
        /// Automatically Assign these roles to Admin Users. 
        /// </summary>
        public List<string> AdminRoles { get; set; } = new List<string> {
            RoleNames.Admin,
        };
        
        public Dictionary<string, string> MapClaimsToSession { get; set; } = new Dictionary<string, string> {
            [ClaimTypes.Email] = nameof(AuthUserSession.Email),
            [ClaimTypes.Name] = nameof(AuthUserSession.UserAuthName),
            [ClaimTypes.GivenName] = nameof(AuthUserSession.FirstName),
            [ClaimTypes.Surname] = nameof(AuthUserSession.LastName),
            [ClaimTypes.StreetAddress] = nameof(AuthUserSession.Address),
            [ClaimTypes.Locality] = nameof(AuthUserSession.City),
            [ClaimTypes.StateOrProvince] = nameof(AuthUserSession.State),
            [ClaimTypes.PostalCode] = nameof(AuthUserSession.PostalCode),
            [ClaimTypes.Country] = nameof(AuthUserSession.Country),
            [ClaimTypes.OtherPhone] = nameof(AuthUserSession.PhoneNumber),
            [ClaimTypes.DateOfBirth] = nameof(AuthUserSession.BirthDateRaw),
            [ClaimTypes.Gender] = nameof(AuthUserSession.Gender),
            [ClaimTypes.Dns] = nameof(AuthUserSession.Dns),
            [ClaimTypes.Rsa] = nameof(AuthUserSession.Rsa),
            [ClaimTypes.Sid] = nameof(AuthUserSession.Sid),
            [ClaimTypes.Hash] = nameof(AuthUserSession.Hash),
            [ClaimTypes.HomePhone] = nameof(AuthUserSession.HomePhone),
            [ClaimTypes.MobilePhone] = nameof(AuthUserSession.MobilePhone),
            [ClaimTypes.Webpage] = nameof(AuthUserSession.Webpage),
        };

        /// <summary>
        /// Run custom filter after session is restored from ClaimsPrincipal
        /// </summary>
        public Action<IAuthSession, ClaimsPrincipal, IRequest> PopulateSessionFilter { get; set; }

        /// <summary>
        /// Run custom filter after ClaimsPrincipal is created from Session
        /// </summary>
        public Func<List<Claim>, IAuthSession, IRequest, ClaimsPrincipal> CreateClaimsPrincipal { get; set; }

        public NetCoreIdentityAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            AutoSignInSessionsMatching = DefaultAutoSignInSessionsMatching;
        }

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

            var session = req.GetSession();
            if (session.IsAuthenticated) // if existing Session exists use it instead
                return;

            string source; 
            string sessionId;
            Claim idClaim = null;
            foreach (var idClaimType in IdClaimTypes)
            {
                idClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == idClaimType);
                if (idClaim != null)
                    break;
            }

            if (idClaim != null)
            {
                sessionId = idClaim.Value;
                source = idClaim.Type;
            }
            else
            {
                var clientIdClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == "client_id");
                if (clientIdClaim != null)
                {
                    if (RestrictToClientIds == null || RestrictToClientIds.Contains(clientIdClaim.Value))
                    {
                        sessionId = clientIdClaim.Value;
                        source = "client_id";
                    }
                    else throw new NotSupportedException($"Unknown client_id '{clientIdClaim.Value}' not found in NetCoreIdentityAuthProvider.RestrictToClientIds");
                }
                else throw new NotSupportedException($"Claim '{IdClaimType}' is required");
            }

            session = SessionFeature.CreateNewSession(req, sessionId);
            session.IsAuthenticated = true;
            var meta = (session as IMeta)?.Meta;            
            var extended = session as IAuthSessionExtended;
            if (extended != null)
                extended.Type = source;
            session.AuthProvider = Name;

            var sessionValues = new Dictionary<string,string>();
            
            foreach (var claim in claimsPrincipal.Claims)
            {
                if (claim.Type == RoleClaimType)
                {
                    if (session.Roles == null)
                        session.Roles = new List<string>();
                    session.Roles.Add(claim.Value);
                }
                if (claim.Type == PermissionClaimType)
                {
                    if (session.Permissions == null)
                        session.Permissions = new List<string>();
                    session.Permissions.Add(claim.Value);
                }
                else if (claim.Type == "aud" && extended != null)
                {
                    if (extended.Audiences == null)
                        extended.Audiences = new List<string>();
                    extended.Audiences.Add(claim.Value);
                }
                else if (claim.Type == "scope" && extended != null)
                {
                    if (extended.Scopes == null)
                        extended.Scopes = new List<string>();
                    extended.Scopes.Add(claim.Value);
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

            if (session.UserAuthName?.IndexOf('@') >= 0)
            {
                session.Email = session.UserAuthName;
            }
            
            PopulateSessionFilter?.Invoke(session, claimsPrincipal, req);

            req.Items[Keywords.Session] = session;
        }
        
        public HashSet<string> IgnoreAutoSignInForExtensions { get; set; } = new HashSet<string> {
            "js", "css", "png", "jpg", "jpeg", "gif", "svg", "ico"
        };

        public bool DefaultAutoSignInSessionsMatching(IRequest req)
        {
            var netReq = (NetCoreRequest) req;
            var ext = netReq.HttpRequest.Path.HasValue
                ? netReq.HttpRequest.Path.Value.LastRightPart('.')
                : null;

            if (!string.IsNullOrEmpty(ext) && ext.IndexOf('/') == -1)
                return !IgnoreAutoSignInForExtensions.Contains(ext);
            
            return true;
        }

        //For pass-through requests not handled by ServiceStack
        public async Task SignInAuthenticatedSessions(NetCoreRequest req)
        {
            if (!AutoSignInSessionsMatching(req))
                return;
            
            var session = req.GetSession();
            if (session.IsAuthenticated)
            {
                var claims = session.ConvertSessionToClaims(
                    issuer:Issuer,
                    roleClaimType:RoleClaimType,
                    permissionClaimType:PermissionClaimType);
                
                if (session.Roles.IsEmpty() && HostContext.AppHost.GetAuthRepository(req) is IManageRoles authRepo)
                {
                    using (authRepo as IDisposable)
                    {
                        var roles = authRepo.GetRoles(session.UserAuthId.ToInt());
                        foreach (var role in roles)
                        {
                            claims.Add(new Claim(RoleClaimType, role, Issuer));
                        }
                    }
                }
                
                if (HostContext.HasValidAuthSecret(req) || claims.Any(x => x.Type == RoleClaimType && x.Value == RoleNames.Admin))
                {
                    foreach (var adminRole in AdminRoles)
                    {
                        claims.Add(new Claim(RoleClaimType, adminRole, ClaimValueTypes.String, Issuer));
                    }
                }
                
                var principal = CreateClaimsPrincipal != null
                    ? CreateClaimsPrincipal(claims, session, req)
                    : new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType));

                req.HttpContext.User = principal;
            }
            else if (HostContext.HasValidAuthSecret(req))
            {
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.NameIdentifier, nameof(HostConfig.AdminAuthSecret), ClaimValueTypes.String, Issuer),
                    new Claim(ClaimTypes.Name, RoleNames.Admin, ClaimValueTypes.String, Issuer),
                    new Claim(ClaimTypes.GivenName, RoleNames.Admin, ClaimValueTypes.String, Issuer),
                    new Claim(ClaimTypes.Surname, "User", ClaimValueTypes.String, Issuer),
                };

                foreach (var adminRole in AdminRoles)
                {
                    claims.Add(new Claim(RoleClaimType, adminRole, ClaimValueTypes.String, Issuer));
                }

                var principal = CreateClaimsPrincipal != null
                    ? CreateClaimsPrincipal(claims, session, req)
                    : new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType));

                req.HttpContext.User = principal;
            }
        }

        public void Register(IAppHost appHost, AuthFeature authFeature)
        {
            if (AutoSignInSessions)
            {
                ((AppHostBase)appHost).BeforeNextMiddleware = SignInAuthenticatedSessions;
            }

            if (OverrideHtmlRedirect)
            {
                // defaults: https://github.com/aspnet/Security/blob/master/src/Microsoft.AspNetCore.Authentication.Cookies/CookieAuthenticationDefaults.cs
                authFeature.HtmlRedirect = "~/Account/Login";
                authFeature.HtmlRedirectAccessDenied = "~/Account/AccessDenied";
                authFeature.HtmlRedirectReturnParam = "ReturnUrl";
                authFeature.HtmlRedirectReturnPathOnly = true;
            }
        }
    }
}
#endif