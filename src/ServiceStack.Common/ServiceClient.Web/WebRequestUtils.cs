using System;
using System.Net;
using System.Text;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;

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

    public static class WebRequestUtils
    {
        internal static AuthenticationException CreateCustomException(string uri, AuthenticationException ex)
        {
            if (uri.StartsWith("https"))
            {
                return new AuthenticationException(
                    String.Format("Invalid remote SSL certificate, overide with: \nServicePointManager.ServerCertificateValidationCallback += ((sender, certificate, chain, sslPolicyErrors) => isValidPolicy);"),
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
                    && !String.IsNullOrEmpty(userName)
                    && !String.IsNullOrEmpty(password));
        }

        internal static void AddBasicAuth(this WebRequest client, string userName, string password)
        {
            client.Headers[HttpHeaders.Authorization]
                = "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password));
        }

        /// <summary>
        /// Naming convention for the request's Response DTO
        /// </summary>
        public const string ResponseDtoSuffix = "Response";

        public static string GetResponseDtoName(object request)
        {
            var requestType = request.GetType();
            return requestType != typeof(object)
                ? requestType.FullName + ResponseDtoSuffix
                : request.GetType().FullName + ResponseDtoSuffix;
        }

        public static Type GetErrorResponseDtoType(object request)
        {
            if (request == null) 
                return typeof(ErrorResponse);

            //If a conventionally-named Response type exists use that regardless if it has ResponseStatus or not
            var responseDtoType = AssemblyUtils.FindType(GetResponseDtoName(request));
            if (responseDtoType == null)
            {
                var genericDef = request.GetType().GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
                if (genericDef != null)
                {
                    
                    var returnDtoType = genericDef.GetGenericArguments()[0];
                    var hasResponseStatus = returnDtoType is IHasResponseStatus 
                        || returnDtoType.GetProperty("ResponseStatus") != null;
                    
                    //Only use the specified Return type if it has a ResponseStatus property
                    if (hasResponseStatus)
                    {
                        responseDtoType = returnDtoType;
                    }
                }
            }

            return responseDtoType ?? typeof(ErrorResponse);
        }

    }

}