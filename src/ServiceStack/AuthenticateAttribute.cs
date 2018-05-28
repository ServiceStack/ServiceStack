using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Auth;
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
                await res.WriteError(req, requestDto, $"No registered Auth Providers found matching {this.Provider ?? "any"} provider");
                res.EndRequest();
                return;
            }
            
            req.PopulateFromRequestIfHasSessionId(requestDto);

            PreAuthenticate(req, authProviders);

            if (res.IsClosed)
                return;

            var session = req.GetSession();
            if (session == null || !authProviders.Any(x => session.IsAuthorized(x.Provider)))
            {
                if (this.DoHtmlRedirectIfConfigured(req, res, true))
                    return;

                await AuthProvider.HandleFailedAuth(authProviders[0], session, req, res);
            }
        }

        internal static void PreAuthenticate(IRequest req, IEnumerable<IAuthProvider> authProviders)
        {
            //Call before GetSession so Exceptions can bubble
            if (!req.Items.ContainsKey(Keywords.HasPreAuthenticated))
            {
                req.Items[Keywords.HasPreAuthenticated] = true;
                foreach (var authWithRequest in authProviders.OfType<IAuthWithRequest>())
                {
                    authWithRequest.PreAuthenticate(req, req.Response);
                    if (req.Response.IsClosed)
                        return;
                }
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
