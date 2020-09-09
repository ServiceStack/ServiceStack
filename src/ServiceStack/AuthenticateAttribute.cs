using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Indicates that the request dto, which is associated with this attribute,
    /// requires authentication.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AuthenticateAttribute : RequestFilterAsyncAttribute
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

        public override async Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
        {
            if (AuthenticateService.AuthProviders == null)
                throw new InvalidOperationException(
                    "The AuthService must be initialized by calling AuthService.Init to use an authenticate attribute");

            if (HostContext.HasValidAuthSecret(req))
                return;

            var authProviders = AuthenticateService.GetAuthProviders(this.Provider);
            if (authProviders.Length == 0)
            {
                await res.WriteError(req, requestDto, $"No registered Auth Providers found matching {this.Provider ?? "any"} provider").ConfigAwait();
                res.EndRequest();
                return;
            }
            
            req.PopulateFromRequestIfHasSessionId(requestDto);

            await PreAuthenticateAsync(req, authProviders).ConfigAwait();

            if (res.IsClosed)
                return;

            var session = await req.GetSessionAsync();
            if (session == null || !authProviders.Any(x => session.IsAuthorized(x.Provider)))
            {
                if (this.DoHtmlRedirectIfConfigured(req, res, true))
                    return;

                await AuthProvider.HandleFailedAuth(authProviders[0], session, req, res).ConfigAwait();
            }
        }

        public static bool Authenticate(IRequest req, object requestDto=null, IAuthSession session=null, IAuthProvider[] authProviders=null)
        {
            if (HostContext.HasValidAuthSecret(req))
                return true;

            session ??= (req ?? throw new ArgumentNullException(nameof(req))).GetSession();
            authProviders ??= AuthenticateService.GetAuthProviders();
            var authValidate = HostContext.GetPlugin<AuthFeature>()?.OnAuthenticateValidate;
            var ret = authValidate?.Invoke(req);
            if (ret != null)
                return false;

            req.PopulateFromRequestIfHasSessionId(requestDto);

            if (!req.Items.ContainsKey(Keywords.HasPreAuthenticated))
            {
                var mockResponse = new BasicRequest().Response;
                req.Items[Keywords.HasPreAuthenticated] = true;
                foreach (var authWithRequest in authProviders.OfType<IAuthWithRequest>())
                {
                    authWithRequest.PreAuthenticate(req, mockResponse);
                    if (mockResponse.IsClosed)
                        return false;
                }
            }
            
            return session != null && (authProviders.Length > 0
                ? authProviders.Any(x => session.IsAuthorized(x.Provider))
                : session.IsAuthenticated);
        }

        public static void AssertAuthenticated(IRequest req, object requestDto=null, IAuthSession session=null, IAuthProvider[] authProviders=null)
        {
            if (Authenticate(req, requestDto:requestDto, session:session))
                return;

            ThrowNotAuthenticated(req);
        }

        public static void ThrowNotAuthenticated(IRequest req=null) => 
            throw new HttpError(401, nameof(HttpStatusCode.Unauthorized), ErrorMessages.NotAuthenticated.Localize(req));

        public static void ThrowInvalidRole(IRequest req=null) => 
            throw new HttpError(403, nameof(HttpStatusCode.Forbidden), ErrorMessages.InvalidRole.Localize(req));

        public static void ThrowInvalidPermission(IRequest req=null) => 
            throw new HttpError(403, nameof(HttpStatusCode.Forbidden), ErrorMessages.InvalidPermission.Localize(req));

        internal static Task PreAuthenticateAsync(IRequest req, IEnumerable<IAuthProvider> authProviders)
        {
            var authValidate = HostContext.GetPlugin<AuthFeature>()?.OnAuthenticateValidate;
            var ret = authValidate?.Invoke(req);
            if (ret != null)
            {
                return req.Response.WriteToResponse(req, ret);
            }

            //Call before GetSession so Exceptions can bubble
            if (!req.Items.ContainsKey(Keywords.HasPreAuthenticated))
            {
                req.Items[Keywords.HasPreAuthenticated] = true;
                foreach (var authWithRequest in authProviders.OfType<IAuthWithRequest>())
                {
                    authWithRequest.PreAuthenticate(req, req.Response);
                    if (req.Response.IsClosed)
                        return TypeConstants.EmptyTask;
                }
            }
            return TypeConstants.EmptyTask;
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

        protected bool DoHtmlRedirectAccessDeniedIfConfigured(IRequest req, IResponse res, bool includeRedirectParam = false)
        {
            var htmlRedirect = this.HtmlRedirect ?? AuthenticateService.HtmlRedirectAccessDenied ?? AuthenticateService.HtmlRedirect;
            if (htmlRedirect != null && req.ResponseContentType.MatchesContentType(MimeTypes.Html))
            {
                DoHtmlRedirect(htmlRedirect, req, res, includeRedirectParam);
                return true;
            }
            return false;
        }

        public static void DoHtmlRedirect(string redirectUrl, IRequest req, IResponse res, bool includeRedirectParam)
        {
            var url = GetHtmlRedirectUrl(req, redirectUrl, includeRedirectParam);
            res.RedirectToUrl(url);
        }

        public static string GetHtmlRedirectUrl(IRequest req) => GetHtmlRedirectUrl(req,
            AuthenticateService.HtmlRedirectAccessDenied ?? AuthenticateService.HtmlRedirect,
            includeRedirectParam: true);
        
        public static string GetHtmlRedirectUrl(IRequest req, string redirectUrl, bool includeRedirectParam)
        {
            var url = req.ResolveAbsoluteUrl(redirectUrl);
            if (includeRedirectParam)
            {
                var redirectPath = !AuthenticateService.HtmlRedirectReturnPathOnly
                    ? req.ResolveAbsoluteUrl("~" + req.PathInfo + ToQueryString(req.QueryString))
                    : req.PathInfo + ToQueryString(req.QueryString);

                var returnParam = HostContext.ResolveLocalizedString(AuthenticateService.HtmlRedirectReturnParam) ??
                                  HostContext.ResolveLocalizedString(LocalizedStrings.Redirect);

                if (url.IndexOf("?" + returnParam, StringComparison.OrdinalIgnoreCase) == -1 &&
                    url.IndexOf("&" + returnParam, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    return url.AddQueryParam(returnParam, redirectPath);
                }
            }
            return url;
        }

        private static string ToQueryString(NameValueCollection queryStringCollection)
        {
            if (queryStringCollection == null || queryStringCollection.Count == 0)
                return string.Empty;

            return "?" + queryStringCollection.ToFormUrlEncoded();
        }

        protected bool Equals(AuthenticateAttribute other)
        {
            return base.Equals(other) && string.Equals(Provider, other.Provider) && string.Equals(HtmlRedirect, other.HtmlRedirect);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AuthenticateAttribute)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (Provider?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (HtmlRedirect?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
}
