#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Configuration;
using ServiceStack.Host.AspNet;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public class AspNetWindowsAuthProvider : AuthProvider, IAuthWithRequest
{
    public override string Type => "NTLM";
    public static string Name = AuthenticateService.WindowsAuthProvider;
    public static string Realm = "/auth/" + AuthenticateService.WindowsAuthProvider;

    public List<string> IgnoreRoles { get; set; } = new(new[] { RoleNames.Admin });

    public AspNetWindowsAuthProvider(IAppHost appHost)
    {
        Provider = Name;
        AuthRealm = Realm;

        AllRoles = new List<string>();
        LimitAccessToRoles = new List<string>();

        if (!(appHost is AppHostBase))
        {
            throw new NotSupportedException(
                "AspNetWindowsAuthProvider is only supported on ASP.NET hosts");
        }

        PopulateUserRoles = PopulateUserSessionWithIsInRole;

        //Add all pre-defined Roles used to in App to 'AllRoles'
        appHost.AfterInitCallbacks.Add(host =>
        {
            var allExistingRoles = host.Metadata.GetAllRoles()
                .Where(x => !IgnoreRoles.Contains(x));
            allExistingRoles.Each(x => AllRoles.AddIfNotExists(x));
        });
    }

    /// <summary>
    /// Specify all roles to be used by this application
    /// </summary>
    public List<string> AllRoles { get; set; }

    /// <summary>
    /// Only allow access to users in specified roles
    /// </summary>
    public List<string> LimitAccessToRoles { get; set; }

    public Action<IRequest, IPrincipal, IAuthSession> PopulateUserRoles { get; set; }

    public bool AllowAllWindowsAuthUsers
    {
        get => LimitAccessToRoles == null;
        set => LimitAccessToRoles = null;
    }

    public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
    {
        var user = HttpContext.Current.GetUser();
        return session is { IsAuthenticated: true } 
               && (user == null || LoginMatchesSession(session, user.GetUserName()));
    }

    public virtual bool IsAuthorized(IPrincipal user)
    {
        if (user != null)
        {
            if (!user.Identity.IsAuthenticated)
                return false;
            if (AllowAllWindowsAuthUsers)
                return true;
            if (LimitAccessToRoles.Any(user.IsInRole))
                return true;
        }
        return false;
    }

    public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
    {
        var user = authService.Request.GetUser();
        var userName = user.GetUserName();
        if (!LoginMatchesSession(session, userName))
        {
            await authService.RemoveSessionAsync(token).ConfigAwait();
            session = await authService.GetSessionAsync(token: token).ConfigAwait();
        }

        if (IsAuthorized(user))
        {
            session.IsAuthenticated = true;
            if (session.UserAuthName == null)
            {
                session.UserAuthName = userName;
            }

            var aspReq = (HttpRequestBase)authService.Request.OriginalRequest;

            var loginUser = aspReq.ServerVariables["LOGON_USER"].ToNullIfEmpty();
            var remoteUser = aspReq.ServerVariables["REMOTE_USER"].ToNullIfEmpty();
            var identityName = aspReq.LogonUserIdentity?.Name;
            session.DisplayName = loginUser
                ?? remoteUser
                ?? identityName;

            var tokens = new AuthTokens {
                Provider = Name,
                UserName = userName,
                DisplayName = session.DisplayName,
                Items = new Dictionary<string, string> {
                    {"LOGON_USER", loginUser},
                    {"REMOTE_USER", remoteUser},
                    {"LogonUserIdentityName", identityName},
                }
            };

            session.ReferrerUrl = GetReferrerUrl(authService, session, request);

            var response = await OnAuthenticatedAsync(authService, session, tokens, new Dictionary<string, string>(), token).ConfigAwait();

            if (session.Roles == null)
                session.Roles = new List<string>();

            PopulateUserRoles(authService.Request, user, session);

            await this.SaveSessionAsync(authService, session, SessionExpiry, token).ConfigAwait();
            
            if (response != null)
                return response;

            return new AuthenticateResponse
            {
                UserName = userName,
                SessionId = session.Id,
                DisplayName = session.DisplayName,
                ReferrerUrl = authService.Request.GetReturnUrl()
            };
        }

        throw HttpError.Unauthorized(ErrorMessages.WindowsAuthFailed.Localize(authService.Request));
    }

    protected override IAuthRepository GetAuthRepository(IRequest req)
    {
        return null; //Sources User Info from Windows Auth instead of Auth Repo
    }

    private void PopulateUserSessionWithIsInRole(IRequest req, IPrincipal user, IAuthSession session)
    {
        foreach (var role in AllRoles.Safe())
        {
            if (IgnoreRoles.Contains(role))
                continue;
            if (session.Roles.Contains(role))
                continue;

            try
            {
                if (user.IsInRole(role))
                    session.Roles.AddIfNotExists(role);
            }
            catch (Exception ex)
            {
                var log = LogManager.GetLogger(GetType());
                log.ErrorFormat("Failed to resolve role '{0}' for '{1}'. To ignore checking, add to AspNetWindowsAuthProvider.IgnoreRoles:\n{0}", 
                    role, session.UserAuthName, ex); 
            }
        }
    }

    public static void AuthenticateIfWindowsAuth(IRequest req, IResponse res)
    {
        var winAuthProvider = AuthenticateService.GetAuthProvider(Name) as AspNetWindowsAuthProvider;
        winAuthProvider?.IsAuthorized(req.GetUser());
    }

    public async Task PreAuthenticateAsync(IRequest req, IResponse res)
    {
        var user = req.GetUser();
        if (user != null)
        {
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()
            using var authService = HostContext.ResolveService<AuthenticateService>(req);
            var session = await req.GetSessionAsync().ConfigAwait();
            if (LoginMatchesSession(session, user.Identity.Name)) return;

            var response = await authService.PostAsync(new Authenticate
            {
                provider = Name,
                UserName = user.GetUserName(),
            }).ConfigAwait();
        }
    }
}

public static class HttpContextExtensions
{
    public static IPrincipal GetUser(this HttpContext ctx) => ctx?.User;

    public static IPrincipal GetUser(this HttpContextBase ctx) => ctx?.User;

    public static IPrincipal GetUser(this IRequest req)
    {
        var aspReq = req as AspNetRequest;
        if (aspReq?.OriginalRequest is HttpRequestBase aspReqBase)
        {
            var user = aspReqBase.RequestContext.HttpContext.GetUser();
            return user.GetUserName() == null ? null : user;
        }
        return null;
    }

    public static string GetUserName(this IPrincipal user)
    {
        var userName = user?.Identity.Name;
        return string.IsNullOrEmpty(userName) //can be ""
            ? null
            : userName;
    }
}

#endif