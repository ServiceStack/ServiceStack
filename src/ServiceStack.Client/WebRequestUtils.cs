using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Logging;

#if NETFX_CORE
using System.Net.Http.Headers;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
#endif

namespace ServiceStack
{
    public class TokenException : AuthenticationException
    {
        public TokenException(string message) : base(message) {}
    }

    public class AuthenticationException : Exception, IHasStatusCode
    {
        public AuthenticationException() {}

        public AuthenticationException(string message)
            : base(message) {}

        public AuthenticationException(string message, Exception innerException)
            : base(message, innerException) {}

        public int StatusCode => (int) HttpStatusCode.Unauthorized;
    }

    // by adamfowleruk
    public class AuthenticationInfo
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AuthenticationInfo));

        public string method { get; set; }
        public string realm { get; set; }
        public string qop { get; set; }
        public string nonce { get; set; }
        public string opaque { get; set; }

        // these values used between requests, and not taken from WWW-Authenticate header of response
        public string cnonce { get; set; }
        public int nc { get; set; }

        public AuthenticationInfo(string authHeader)
        {
            cnonce = "0a4f113b";
            nc = 1;

            // Example Digest header: WWW-Authenticate: Digest realm="testrealm@host.com", qop="auth,auth-int", nonce="dcd98b7102dd2f0e8b11d0f600bfb0c093", opaque="5ccc069c403ebaf9f0171e9517f40e41"

            // get method from first word
            int pos = authHeader.IndexOf(" ", StringComparison.Ordinal);
            if (pos < 0)
                throw new AuthenticationException($"Authentication header not supported: {authHeader}");

            method = authHeader.Substring(0, pos).ToLowerInvariant();
            string remainder = authHeader.Substring(pos + 1);

            // split the rest by comma, then =
            string[] pars = remainder.Split(',');
            string[] newpars = new string[pars.Length];
            int maxnewpars = 0;
            // test possibility that a comma is mid value for a split (as in above example)
            for (int i = 0; i < pars.Length; i++)
            {
                if (pars[i].EndsWith("\""))
                {
                    newpars[maxnewpars] = pars[i];
                    maxnewpars++;
                }
                else
                {
                    // merge with next one
                    newpars[maxnewpars] = pars[i] + "," + pars[i + 1];
                    maxnewpars++;
                    i++; // skips next value
                }
            }

            // now go through each part, splitting on first = character, and removing leading and trailing spaces and " quotes
            for (int i = 0; i < maxnewpars; i++)
            {
                var pos2 = newpars[i].IndexOf("=", StringComparison.Ordinal);
                if (pos2 == -1)
                    return;
                var name = newpars[i].Substring(0, pos2).Trim();
                var value = newpars[i].Substring(pos2 + 1).Trim();
                if (value.StartsWith("\""))
                    value = value.Substring(1);
                if (value.EndsWith("\""))
                    value = value.Substring(0, value.Length - 1);
                switch (name)
                {
                    case "qop":
                        qop = value;
                        break;
                    case "realm":
                        realm = value;
                        break;
                    case "nonce":
                        nonce = value;
                        break;
                    case "opaque":
                        opaque = value;
                        break;
                }
            }
        }

        public override string ToString()
        {
            return $"[AuthenticationInfo: method={method}, realm={realm}, qop={qop}, nonce={nonce}, opaque={opaque}, cnonce={cnonce}, nc={nc}]";
        }
    }

    public static class WebRequestUtils
    {
        internal static AuthenticationException CreateCustomException(string uri, AuthenticationException ex)
        {
            if (uri.StartsWith("https"))
            {
                return new AuthenticationException(
                    "Invalid remote SSL certificate, override with: \nServicePointManager.ServerCertificateValidationCallback += ((sender, certificate, chain, sslPolicyErrors) => isValidPolicy);", ex);
            }
            return null;
        }

        internal static bool ShouldAuthenticate(WebException webEx, bool hasAuthInfo)
        {
            return webEx?.Response != null 
                && ((HttpWebResponse)webEx.Response).StatusCode == HttpStatusCode.Unauthorized 
                && hasAuthInfo;
       }

        public static void AddBasicAuth(this WebRequest client, string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                return;

            client.Headers[HttpHeaders.Authorization]
                = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password));
        }

        public static void AddApiKeyAuth(this WebRequest client, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return;

            client.Headers[HttpHeaders.Authorization]
                = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey + ":"));
        }

        public static void AddBearerToken(this WebRequest client, string bearerToken)
        {
            if (string.IsNullOrEmpty(bearerToken))
                return;

            client.Headers[HttpHeaders.Authorization] = "Bearer " + bearerToken;
        }

        public static string CalculateMD5Hash(string input)
        {
            // copied/pasted by adamfowleruk
            // step 1, calculate MD5 hash from input
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            var sb = StringBuilderCache.Allocate();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("X2"));
            }
            return StringBuilderCache.ReturnAndFree(sb).ToLower(); // The RFC requires the hex values are lowercase
        }

        internal static string padNC(int num)
        {
            // by adamfowleruk
            var pad = "";
            for (var i = 0; i < (8 - ("" + num).Length); i++)
            {
                pad += "0";
            }
            var ret = pad + num;
            return ret;
        }

        internal static void AddAuthInfo(this WebRequest client, string userName, string password, AuthenticationInfo authInfo)
        {
            if ("basic".Equals(authInfo.method))
            {
                client.AddBasicAuth(userName, password); // FIXME AddBasicAuth ignores the server provided Realm property. Potential Bug.
            }
            else if ("digest".Equals(authInfo.method))
            {
                // do digest auth header using auth info
                // auth info saved in ServiceClientBase for subsequent requests
                client.AddDigestAuth(userName, password, authInfo);
            }
        }

        internal static void AddDigestAuth(this WebRequest client, string userName, string password, AuthenticationInfo authInfo)
        {
            // by adamfowleruk
            // See Client Request at http://en.wikipedia.org/wiki/Digest_access_authentication

            string ncUse = padNC(authInfo.nc);
            authInfo.nc++; // increment for subsequent requests

            string ha1raw = userName + ":" + authInfo.realm + ":" + password;
            string ha1 = CalculateMD5Hash(ha1raw);


            string ha2raw = client.Method + ":" + client.RequestUri.PathAndQuery;
            string ha2 = CalculateMD5Hash(ha2raw);

            string md5rraw = ha1 + ":" + authInfo.nonce + ":" + ncUse + ":" + authInfo.cnonce + ":" + authInfo.qop + ":" + ha2;
            string response = CalculateMD5Hash(md5rraw);

            string header =
                "Digest username=\"" + userName + "\", realm=\"" + authInfo.realm + "\", nonce=\"" + authInfo.nonce + "\", uri=\"" +
                client.RequestUri.PathAndQuery + "\", cnonce=\"" + authInfo.cnonce + "\", nc=" + ncUse + ", qop=\"" + authInfo.qop + "\", response=\"" + response +
                "\", opaque=\"" + authInfo.opaque + "\"";

            client.Headers[HttpHeaders.Authorization] = header;
        }

        /// <summary>
        /// Naming convention for the request's Response DTO
        /// </summary>
        public const string ResponseDtoSuffix = "Response";

        public static string GetResponseDtoName(Type requestType)
        {
#if NETCORE
            return requestType.FullName + ResponseDtoSuffix + "," + requestType.Assembly.GetName().Name;
#else        
            return requestType.FullName + ResponseDtoSuffix;
#endif
        }

        public static Type GetErrorResponseDtoType<TResponse>(object request)
        {
            if (request is object[] batchRequest && batchRequest.Length > 0)
                request = batchRequest[0]; 

            var hasResponseStatus = typeof(TResponse).HasInterface(typeof(IHasResponseStatus))
                || typeof(TResponse).GetProperty("ResponseStatus") != null;

            return hasResponseStatus ? typeof(TResponse) : GetErrorResponseDtoType(request);
        }

        public static Type GetErrorResponseDtoType(object request)
        {
            return request == null 
                ? typeof(ErrorResponse) 
                : GetErrorResponseDtoType(request.GetType());
        }

        public static Type GetErrorResponseDtoType(Type requestType)
        {
            if (requestType == null)
                return typeof (ErrorResponse);

            //If a conventionally-named Response type exists use that regardless if it has ResponseStatus or not
#if NETCORE
            var responseDtoType = Type.GetType(GetResponseDtoName(requestType));
#else                        
            var responseDtoType = AssemblyUtils.FindType(GetResponseDtoName(requestType));
#endif
            if (responseDtoType == null)
            {
                var genericDef = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
                if (genericDef != null)
                {
                    var returnDtoType = genericDef.GetGenericArguments()[0];
                    var hasResponseStatus = returnDtoType.HasInterface(typeof(IHasResponseStatus))
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

        /// <summary>
        /// Shortcut to get the ResponseStatus whether it's bare or inside a IHttpResult
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static ResponseStatus GetResponseStatus(this object response)
        {
            if (response == null)
                return null;

            if (response is ResponseStatus status)
                return status;

            if (response is IHasResponseStatus hasResponseStatus)
                return hasResponseStatus.ResponseStatus;

            var statusGetter = TypeProperties.Get(response.GetType()).GetPublicGetter(nameof(ResponseStatus));
            return statusGetter?.Invoke(response) as ResponseStatus;
        }
        
        public static HttpWebRequest InitWebRequest(string url, string method="GET", Dictionary<string,string> headers=null)
        {
            var webReq = (HttpWebRequest) WebRequest.Create(url);
            if (method != null)
                webReq.Method = method;
            if (headers != null)
            {
                foreach (var key in headers.Keys)
                {
                    var keyLower = key.ToLower();
                    var value = headers[key];
                    switch (keyLower)
                    {
                        case "content-type":
                            webReq.ContentType = value;
                            break;
                        case "user-agent":
                            webReq.UserAgent = value;
                            break;
                        case "accept":
                            webReq.Accept = value;
                            break;
                        default:
                            webReq.Headers[key] = value;
                            break;
                    }
                }
            }
            return webReq;
        }

        public static void AppendHttpRequestHeaders(this HttpWebRequest webReq, StringBuilder sb, Uri baseUri = null)
        {
            if (webReq == null)
                throw new ArgumentNullException(nameof(webReq));
            if (sb == null)
                throw new ArgumentNullException(nameof(sb));

            var uri = webReq.RequestUri;
            if (baseUri == null)
                baseUri = new Uri($"{uri.Scheme}://{uri.Authority}");

            sb.AppendLine($"{webReq.Method} {uri.PathAndQuery} HTTP/{webReq.ProtocolVersion}");
            var port = uri.Port != 80 && uri.Port != 443 ? $":{uri.Port}" : "";
            sb.AppendLine($"Host: {uri.Host}{port}");
            for(var i = 0; i < webReq.Headers.Count; ++i)
            {
                var header = webReq.Headers.GetKey(i);
                var values = webReq.Headers.GetValues(i);
                if (values == null) continue;
                foreach(var value in values)
                {
                    sb.AppendLine($"{header}: {value}");
                }
            }

            var cookies = webReq.CookieContainer.GetCookies(baseUri);
            if (cookies.Count > 0)
            {
                sb.Append("Cookie: ");
                foreach (Cookie cookie in cookies)
                {
                    if (sb.Length > 0)
                        sb.Append("; ");
                    sb.Append(cookie.Name).Append('=').Append(cookie.Value);
                }
                sb.AppendLine();
            }
        }

        public static void AppendHttpResponseHeaders(this HttpWebResponse webRes, StringBuilder sb)
        {
            sb.AppendLine($"HTTP/{webRes.ProtocolVersion} {(int)webRes.StatusCode} {webRes.StatusDescription}");
            for (var i = 0; i < webRes.Headers.Count; ++i)
            {
                var header = webRes.Headers.GetKey(i);
                var values = webRes.Headers.GetValues(i);
                if (values == null) continue;
                foreach(var value in values)
                {
                    sb.AppendLine($"{header}: {value}");
                }
            }
            sb.AppendLine();
        }

    }
}
