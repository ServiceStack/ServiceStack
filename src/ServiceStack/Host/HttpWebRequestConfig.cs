#if !NETSTANDARD1_6

using System.Net;
using System.Web;
using System.Web.Security;

namespace ServiceStack.Host
{
    public static class HttpWebRequestConfig
    {
        public static void Configure()
        {
            ServiceClientBase.GlobalRequestFilter = TransferAuthenticationTokens;
        }

        public static void TransferAuthenticationTokens(HttpWebRequest httpWebRequest)
        {
            var cookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null) return;

            var authenticationCookie = new Cookie(
                FormsAuthentication.FormsCookieName,
                cookie.Value,
                cookie.Path,
                HttpContext.Current.Request.Url.Authority);

            httpWebRequest.CookieContainer = new CookieContainer();
            httpWebRequest.CookieContainer.Add(authenticationCookie);
        }
    }
}

#endif
