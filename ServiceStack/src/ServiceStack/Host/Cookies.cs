#if NETCORE
using Microsoft.AspNetCore.Http;
#else
using System.Web;
#endif

using System;
using System.Net;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class Cookies : ICookies
    {
        public const string RootPath = "/";

        readonly IHttpResponse httpRes;

        public Cookies(IHttpResponse httpRes)
        {
            this.httpRes = httpRes;
        }

        private bool UseSecureCookie(bool? secureOnly) =>
            (secureOnly ?? HostContext.Config?.UseSecureCookies ?? true) && httpRes.Request.IsSecureConnection;

        /// <summary>
        /// Sets a persistent cookie which never expires
        /// </summary>
        public void AddPermanentCookie(string cookieName, string cookieValue, bool? secureOnly = null)
        {
            var cookie = new Cookie(cookieName, cookieValue, RootPath) {
                Expires = DateTime.UtcNow.AddYears(20),
                Secure = UseSecureCookie(secureOnly)
            };
            httpRes.SetCookie(cookie);
        }

        /// <summary>
        /// Sets a session cookie which expires after the browser session closes
        /// </summary>
        public void AddSessionCookie(string cookieName, string cookieValue, bool? secureOnly = null)
        {
            var cookie = new Cookie(cookieName, cookieValue, RootPath) {
                Secure = UseSecureCookie(secureOnly)
            };
            httpRes.SetCookie(cookie);
        }

        /// <summary>
        /// Deletes a specified cookie by setting its value to empty and expiration to -1 days
        /// </summary>
        public void DeleteCookie(string cookieName)
        {
            var cookie = new Cookie(cookieName, string.Empty, RootPath)
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                Secure = UseSecureCookie(null)
            };
            httpRes.SetCookie(cookie);
        }
    }

    public static class CookiesExtensions
    {
        private static readonly DateTime Session = DateTime.MinValue;

#if !NETCORE

#if !NET472
        private static SetMemberDelegate sameSiteFn;
        private static Enum sameSiteNone;
        private static Enum sameSiteStrict;
        private static Enum sameSiteLax;

        public static void Init()
        {
            // Use reflection to avoid tfm builds and binary dependency on .NET Framework v4.7.2+
            sameSiteFn = TypeProperties<HttpCookie>.GetAccessor("SameSite")?.PublicSetter;
            if (sameSiteFn != null)
            {
                var sameSiteMode = typeof(HttpCookie).Assembly.GetType("System.Web.SameSiteMode");
                if (sameSiteMode != null)
                {
                    sameSiteNone = (Enum) Enum.Parse(sameSiteMode, "None");
                    sameSiteStrict = (Enum) Enum.Parse(sameSiteMode, "Strict");
                    sameSiteLax = (Enum) Enum.Parse(sameSiteMode, "Lax");
                }
            }
        }
#endif
        
        public static HttpCookie ToHttpCookie(this Cookie cookie)
        {
            var config = HostContext.Config;
            var httpCookie = new HttpCookie(cookie.Name, cookie.Value)
            {
                Path = cookie.Path,
                Expires = cookie.Expires,
                HttpOnly = config.UseHttpOnlyCookies || cookie.HttpOnly,
                Secure = cookie.Secure,
            };
            if (!string.IsNullOrEmpty(cookie.Domain))
            {
                httpCookie.Domain = cookie.Domain;
            }
            else if (config.RestrictAllCookiesToDomain != null)
            {
                httpCookie.Domain = config.RestrictAllCookiesToDomain;
            }

#if NET472
            httpCookie.SameSite = config.UseSameSiteCookies == null
                ? SameSiteMode.Lax
                : config.UseSameSiteCookies == true
                    ? SameSiteMode.Strict
                    : SameSiteMode.None;
#else
            var sameSiteCookie = config.UseSameSiteCookies == null
                ? sameSiteLax
                : config.UseSameSiteCookies == true
                    ? sameSiteStrict
                    : sameSiteNone;
            sameSiteFn?.Invoke(httpCookie, sameSiteCookie);
#endif

            HostContext.AppHost?.HttpCookieFilter(httpCookie);

            return httpCookie;
        }
#endif

#if NETCORE
        public static CookieOptions ToCookieOptions(this Cookie cookie)
        {
            var config = HostContext.Config;
            var cookieOptions = new CookieOptions {
                Path = cookie.Path,
                Expires = cookie.Expires == DateTime.MinValue ? (DateTimeOffset?) null : cookie.Expires,
                HttpOnly = config.UseHttpOnlyCookies || cookie.HttpOnly,
                Secure = cookie.Secure,
                SameSite = config.UseSameSiteCookies == null
                    ? SameSiteMode.Lax
                    : config.UseSameSiteCookies == true
                        ? SameSiteMode.Strict
                        : SameSiteMode.None,
            };

            if (!string.IsNullOrEmpty(cookie.Domain))
                cookieOptions.Domain = cookie.Domain;
            else if (config.RestrictAllCookiesToDomain != null)
                cookieOptions.Domain = config.RestrictAllCookiesToDomain;
            
            HostContext.AppHost?.CookieOptionsFilter(cookie, cookieOptions);

            return cookieOptions;
        }
#endif

        public static string AsHeaderValue(this Cookie cookie)
        {
            var config = HostContext.Config;
            var path = cookie.Path ?? "/";
            var sb = StringBuilderCache.Allocate();

            sb.Append($"{cookie.Name}={cookie.Value};path={path}");

            if (cookie.Expires != Session)
            {
                sb.Append($";expires={cookie.Expires:R}");
            }

            if (!string.IsNullOrEmpty(cookie.Domain))
            {
                sb.Append($";domain={cookie.Domain}");
            }
            else if (config.RestrictAllCookiesToDomain != null)
            {
                sb.Append($";domain={config.RestrictAllCookiesToDomain}");
            }

            if (cookie.Secure)
            {
                sb.Append(";Secure");
            }
            
            var sameSiteCookie = config.UseSameSiteCookies == null
                ? "Lax"
                : config.UseSameSiteCookies == true
                    ? "Strict"
                    : "None";
            sb.Append(";SameSite=").Append(sameSiteCookie);
            
            if (config.UseHttpOnlyCookies || cookie.HttpOnly)
            {
                sb.Append(";HttpOnly");
            }

            return StringBuilderCache.ReturnAndFree(sb);
        }
    }
}
