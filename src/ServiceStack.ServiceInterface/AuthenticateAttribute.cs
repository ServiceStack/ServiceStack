using System;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// Indicates that the request dto, which is associated with this attribute,
    /// requires authentication.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method /*MVC Actions*/, Inherited = false, AllowMultiple = false)]
    public class AuthenticateAttribute : RequestFilterAttribute
    {
        public string Provider { get; set; }

        public AuthenticateAttribute(ApplyTo applyTo)
            : base(applyTo)
        {
            this.Priority = (int) RequestFilterPriority.Authenticate;
        }

        public AuthenticateAttribute()
            : this(ApplyTo.All) {}

        public AuthenticateAttribute(string provider)
            : this(ApplyTo.All)
        {
            this.Provider = provider;
        }

        public AuthenticateAttribute(ApplyTo applyTo, string provider)
            : this(applyTo)
        {
            this.Provider = provider;
        }

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            if (AuthService.AuthProviders == null) throw new InvalidOperationException("The AuthService must be initialized by calling "
                 + "AuthService.Init to use an authenticate attribute");

            var matchingOAuthConfigs = AuthService.AuthProviders.Where(x =>
                this.Provider.IsNullOrEmpty()
                || x.Provider == this.Provider).ToList();

            if (matchingOAuthConfigs.Count == 0)
            {
                res.WriteError(req, requestDto, "No OAuth Configs found matching {0} provider"
                    .Fmt(this.Provider ?? "any"));
                res.EndServiceStackRequest();
                return;
            }

            AuthenticateIfDigestAuth(req, res);
            AuthenticateIfBasicAuth(req, res);

            using (var cache = req.GetCacheClient())
            {
                var sessionId = req.GetSessionId();
                var session = sessionId != null ? cache.GetSession(sessionId) : null;

                if (session == null || !matchingOAuthConfigs.Any(x => session.IsAuthorized(x.Provider)))
                {
                    AuthProvider.HandleFailedAuth(matchingOAuthConfigs[0], session, req, res);
                }
            }
        }

        //Also shared by RequiredRoleAttribute and RequiredPermissionAttribute
        public static void AuthenticateIfBasicAuth(IHttpRequest req, IHttpResponse res)
        {
            //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

            var userPass = req.GetBasicAuthUserAndPassword();
            if (userPass != null)
            {
                var authService = req.TryResolve<AuthService>();
                authService.RequestContext = new HttpRequestContext(req, res, null);
                var response = authService.Post(new Auth.Auth {
                    provider = BasicAuthProvider.Name,
                    UserName = userPass.Value.Key,
                    Password = userPass.Value.Value
                });
            }
        }
        public static void AuthenticateIfDigestAuth(IHttpRequest req, IHttpResponse res)
        {
            //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

            var digestAuth = req.GetDigestAuth();
            if (digestAuth != null)
            {
                var authService = req.TryResolve<AuthService>();
                authService.RequestContext = new HttpRequestContext(req, res, null);
                var response = authService.Post(new Auth.Auth
                {
                    provider = DigestAuthProvider.Name,
                    nonce = digestAuth["nonce"],
                    uri = digestAuth["uri"],
                    response = digestAuth["response"],
                    qop = digestAuth["qop"],
                    nc = digestAuth["nc"],
                    cnonce = digestAuth["cnonce"],
                    UserName = digestAuth["username"]
                });
            }
        }
    }
}