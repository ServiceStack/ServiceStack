using System;
using System.Collections.Specialized;
using System.Linq;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Indicates that the request dto, which is associated with this attribute,
    /// requires authentication.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AuthenticateAttribute : RequestFilterAttribute
    {
        /// <summary>
        /// Restrict authentication to a specific <see cref="IAuthProvider"/>.
        /// For example, if this attribute should only permit access
        /// if the user is authenticated with <see cref="BasicAuthProvider"/>,
        /// you should set this property to <see cref="BasicAuthProvider.Name"/>.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Redirect the client to a specific URL if authentication failed.
        /// If this property is null, simply `401 Unauthorized` is returned.
        /// </summary>
        public string HtmlRedirect { get; set; }

        public AuthenticateAttribute(ApplyTo applyTo)
            : base(applyTo)
        {
            this.Priority = (int)RequestFilterPriority.Authenticate;
        }

        public AuthenticateAttribute()
            : this(ApplyTo.All) { }

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

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (AuthenticateService.AuthProviders == null)
                throw new InvalidOperationException(
                    "The AuthService must be initialized by calling AuthService.Init to use an authenticate attribute");

            var matchingOAuthConfigs = AuthenticateService.AuthProviders.Where(x =>
                this.Provider.IsNullOrEmpty()
                || x.Provider == this.Provider).ToList();

            if (matchingOAuthConfigs.Count == 0)
            {
                res.WriteError(req, requestDto, "No OAuth Configs found matching {0} provider"
                    .Fmt(this.Provider ?? "any"));
                res.EndRequest();
                return;
            }

            if (matchingOAuthConfigs.Any(x => x.Provider == DigestAuthProvider.Name))
                AuthenticateIfDigestAuth(req, res);

            if (matchingOAuthConfigs.Any(x => x.Provider == BasicAuthProvider.Name))
                AuthenticateIfBasicAuth(req, res);

            var session = req.GetSession();
            if (session == null || !matchingOAuthConfigs.Any(x => session.IsAuthorized(x.Provider)))
            {
                if (this.DoHtmlRedirectIfConfigured(req, res, true)) return;

                AuthProvider.HandleFailedAuth(matchingOAuthConfigs[0], session, req, res);
            }
        }

        protected bool DoHtmlRedirectIfConfigured(IRequest req, IResponse res, bool includeRedirectParam = false)
        {
            var htmlRedirect = this.HtmlRedirect ?? AuthenticateService.HtmlRedirect;
            if (htmlRedirect != null && req.ResponseContentType.MatchesContentType(MimeTypes.Html))
            {
                DoHtmlRedirect(htmlRedirect, req, res, includeRedirectParam);
                return true;
            }
            return false;
        }

        public static void DoHtmlRedirect(string redirectUrl, IRequest req, IResponse res, bool includeRedirectParam)
        {
            var url = req.ResolveAbsoluteUrl(redirectUrl);
            if (includeRedirectParam)
            {
                var absoluteRequestPath = req.ResolveAbsoluteUrl("~" + req.PathInfo + ToQueryString(req.QueryString));
                url = url.AddQueryParam(HostContext.ResolveLocalizedString(LocalizedStrings.Redirect), absoluteRequestPath);
            }

            res.RedirectToUrl(url);
        }

        public static void AuthenticateIfBasicAuth(IRequest req, IResponse res)
        {
            //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

            var userPass = req.GetBasicAuthUserAndPassword();
            if (userPass != null)
            {
                var authService = ((IResolver) req).TryResolve<AuthenticateService>();
                authService.Request = req;
                var response = authService.Post(new Authenticate
                {
                    provider = BasicAuthProvider.Name,
                    UserName = userPass.Value.Key,
                    Password = userPass.Value.Value
                });
            }
        }

        public static void AuthenticateIfDigestAuth(IRequest req, IResponse res)
        {
            //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

            var digestAuth = req.GetDigestAuth();
            if (digestAuth != null)
            {
                var authService = ((IResolver) req).TryResolve<AuthenticateService>();
                authService.Request = req;
                var response = authService.Post(new Authenticate
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

        private static string ToQueryString(INameValueCollection queryStringCollection)
        {
            return ToQueryString((NameValueCollection)queryStringCollection.Original);
        }

        private static string ToQueryString(NameValueCollection queryStringCollection)
        {
            if (queryStringCollection == null || queryStringCollection.Count == 0)
                return String.Empty;

            return "?" + queryStringCollection.ToFormUrlEncoded();
        }

    }
}
