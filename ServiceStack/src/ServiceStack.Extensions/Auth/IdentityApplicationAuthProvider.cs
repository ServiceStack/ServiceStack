#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

/// <summary>
/// Handles converting from Application Cookie ClaimsPrincipal into a ServiceStack Session
/// </summary>
public class IdentityApplicationAuthProvider : AuthProvider, IAuthWithRequest, IAuthPlugin
{
    public const string Name = AuthenticateService.IdentityProvider;
    public const string Realm = "/auth/" + AuthenticateService.IdentityProvider;
    public override string Type => "Bearer";

    /// <summary>
    /// Which Authentication Scheme configuration to use (default Identity.Application)
    /// </summary>
    public string AuthenticationScheme { get; }

    /// <summary>
    /// Claim Type used for populating permissions (default perms)
    /// </summary>
    public string PermissionClaimType { get; set; } = JwtClaimTypes.Permissions;
        
    public CookieAuthenticationOptions Options { get; set; }

    public Dictionary<string, string> MapClaimsToSession { get; set; } = new() {
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
    public Action<IAuthSession, ClaimsPrincipal, IRequest>? PopulateSessionFilter { get; set; }

    /// <summary>
    /// Run Async custom filter after session is restored from ClaimsPrincipal
    /// </summary>
    public Func<IAuthSession, ClaimsPrincipal, IRequest, Task>? PopulateSessionFilterAsync { get; set; }

    public IdentityApplicationAuthProvider(string? authenticationScheme=null)
    {
        AuthenticationScheme = authenticationScheme ?? IdentityConstants.ApplicationScheme;
        Options = new CookieAuthenticationOptions();
        Provider = Name;
        AuthRealm = Realm;
    }

    public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate? request = null)
    {
        return session.IsAuthenticated && (session as IRequireClaimsPrincipal)?.User.Identity?.IsAuthenticated == true;
    }

    public override Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
    {
        throw new NotImplementedException($"{GetType().Name} Authenticate() should not be called directly");
    }

    public virtual async Task PreAuthenticateAsync(IRequest req, IResponse res)
    {
        var coreReq = (HttpRequest)req.OriginalRequest;
        var claimsPrincipal = coreReq.HttpContext.User;
        if (claimsPrincipal.Identity?.IsAuthenticated != true)
            return;

        var session = await req.GetSessionAsync().ConfigAwait();
        if (session.IsAuthenticated) // if existing Session exists use it instead
            return;

        string source; 
        string sessionId;

        var idClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name);
        if (idClaim != null)
        {
            sessionId = idClaim.Value;
            source = idClaim.Type;
        }
        else
        {
            var clientIdClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.ClientId);
            if (clientIdClaim != null)
            {
                sessionId = clientIdClaim.Value;
                source = JwtClaimTypes.ClientId;
            }
            else throw new NotSupportedException($"Claim '{ClaimTypes.Name}' is required");
        }

        session = SessionFeature.CreateNewSession(req, sessionId);
        if (session is IRequireClaimsPrincipal sessionClaims)
            sessionClaims.User = claimsPrincipal;

        var meta = (session as IMeta)?.Meta;            
        var extended = session as IAuthSessionExtended;
        if (extended != null)
            extended.Type = source;
            
        var authMethodClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.AuthenticationMethod)
                              ?? claimsPrincipal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.AuthMethod);
        session.AuthProvider = authMethodClaim?.Value 
                               ?? claimsPrincipal.Identity?.AuthenticationType
                               ?? Name;

        var sessionValues = new Dictionary<string,string>();
        foreach (var claim in claimsPrincipal.Claims)
        {
            if (claim.Type == ClaimTypes.Role)
            {
                session.Roles ??= new List<string>();
                session.Roles.Add(claim.Value);
            }
            else if (claim.Type == PermissionClaimType)
            {
                session.Permissions ??= new List<string>();
                session.Permissions.Add(claim.Value);
            }
            else if (extended != null && claim.Type == JwtClaimTypes.Audience)
            {
                extended.Audiences ??= new List<string>();
                extended.Audiences.Add(claim.Value);
            }
            else if (extended != null && claim.Type == JwtClaimTypes.Scope)
            {
                extended.Scopes ??= new List<string>();
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

        if (session.UserAuthName?.IndexOf('@') >= 0 && session.Email == null)
            session.Email = session.UserAuthName;
            
        PopulateSessionFilter?.Invoke(session, claimsPrincipal, req);
            
        if (PopulateSessionFilterAsync != null)
            await PopulateSessionFilterAsync(session, claimsPrincipal, req);
            
        session.OnCreated(req);

        req.Items[Keywords.Session] = session;
    }
        
    public override void Register(IAppHost appHost, AuthFeature authFeature)
    {
        base.Register(appHost, authFeature);
            
        var applicationServices = appHost.GetApplicationServices();

        var appOptionsMonitor = applicationServices.TryResolve<IOptionsMonitor<CookieAuthenticationOptions>>();
        Options = appOptionsMonitor.Get(AuthenticationScheme);

        authFeature.HtmlRedirect = Options.LoginPath;
        authFeature.HtmlRedirectAccessDenied = Options.AccessDeniedPath;
        authFeature.HtmlRedirectReturnParam = Options.ReturnUrlParameter;
        authFeature.HtmlRedirectReturnPathOnly = true;
    }
}