#if !NETSTANDARD1_6

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using ServiceStack.Host.AspNet;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class AspNetWindowsAuthProvider : AuthProvider, IAuthWithRequest
    {
        public static string Name = AuthenticateService.WindowsAuthProvider;
        public static string Realm = "/auth/" + AuthenticateService.WindowsAuthProvider;

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

            //Add all pre-defined Roles used to in App to 'AllRoles'
            appHost.AfterInitCallbacks.Add(host =>
            {
                var requiredRoles = host.Metadata.OperationsMap
                    .SelectMany(x => x.Key.AllAttributes<RequiredRoleAttribute>()
                        .Concat(x.Value.ServiceType.AllAttributes<RequiredRoleAttribute>()))
                    .SelectMany(x => x.RequiredRoles);

                requiredRoles.Each(x => AllRoles.AddIfNotExists(x));
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

        public bool AllowAllWindowsAuthUsers
        {
            get { return LimitAccessToRoles == null; }
            set { LimitAccessToRoles = null; }
        }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
        {
            return session != null && session.IsAuthenticated
                && LoginMatchesSession(session, HttpContext.Current.GetUser().GetUserName());
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

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var user = authService.Request.GetUser();
            var userName = user.GetUserName();
            if (!LoginMatchesSession(session, userName))
            {
                authService.RemoveSession();
                session = authService.GetSession();
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

                var response = OnAuthenticated(authService, session, tokens, new Dictionary<string, string>());

                if (session.Roles == null)
                    session.Roles = new List<string>();

                foreach (var role in AllRoles.Safe())
                {
                    if (user.IsInRole(role))
                        session.Roles.AddIfNotExists(role);
                }

                this.SaveSession(authService, session, SessionExpiry);
                
                if (response != null)
                    return response;

                return new AuthenticateResponse
                {
                    UserName = userName,
                    SessionId = session.Id,
                    DisplayName = session.DisplayName,
                    ReferrerUrl = request.Continue
                };
            }

            throw HttpError.Unauthorized(ErrorMessages.WindowsAuthFailed);
        }

        public static void AuthenticateIfWindowsAuth(IRequest req, IResponse res)
        {
            var winAuthProvider = AuthenticateService.GetAuthProvider(Name) as AspNetWindowsAuthProvider;
            winAuthProvider?.IsAuthorized(req.GetUser());
        }

        public void PreAuthenticate(IRequest req, IResponse res)
        {
            var user = req.GetUser();
            if (user != null)
            {
                SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()
                using (var authService = HostContext.ResolveService<AuthenticateService>(req))
                {
                    var session = req.GetSession();
                    if (LoginMatchesSession(session, user.Identity.Name)) return;

                    var response = authService.Post(new Authenticate
                    {
                        provider = Name,
                        UserName = user.GetUserName(),
                    });
                }
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
            var aspReqBase = aspReq?.OriginalRequest as HttpRequestBase;
            if (aspReqBase != null)
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
}

#endif