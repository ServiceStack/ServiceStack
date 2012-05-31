using System;
using System.Net;
using System.Text;
using ServiceStack.Common.Web;

namespace ServiceStack.ServiceClient.Web
{
    public class AuthenticationException : Exception
    {
        public AuthenticationException()
        {
        }

        public AuthenticationException(string message) : base(message)
        {
        }

        public AuthenticationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    internal static class WebRequestUtils
    {
        internal static AuthenticationException CreateCustomException(string uri, AuthenticationException ex)
        {
            if (uri.StartsWith("https"))
            {
                return new AuthenticationException(
                    string.Format("Invalid remote SSL certificate, overide with: \nServicePointManager.ServerCertificateValidationCallback += ((sender, certificate, chain, sslPolicyErrors) => isValidPolicy);"),
                    ex);
            }
            return null;
        }

        internal static bool ShouldAuthenticate(Exception ex, string userName, string password)
        {
            var webEx = ex as WebException;
            return (webEx != null
                    && webEx.Response != null
                    && ((HttpWebResponse) webEx.Response).StatusCode == HttpStatusCode.Unauthorized
                    && !string.IsNullOrEmpty(userName)
                    && !string.IsNullOrEmpty(password));
        }

        internal static void AddBasicAuth(this WebRequest client, string userName, string password)
        {
            client.Headers[HttpHeaders.Authorization]
                = "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password));
        }

    }

}