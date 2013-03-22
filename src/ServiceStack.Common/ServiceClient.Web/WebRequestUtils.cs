using System;
using System.Net;
using System.Text;
using System.Reflection;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using System.Security.Cryptography;


#if NETFX_CORE
using System.Net.Http.Headers;
#endif

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
	
	// by adamfowleruk
	public class AuthenticationInfo 
	{
		public string method {get;set;}
		public string realm {get;set;}
		public string qop {get;set;}
		public string nonce { get; set; }
		public string opaque { get; set; }

		// these values used between requests, and not taken from WWW-Authenticate header of response
		public string cnonce { get; set; }
		public int nc { get; set; }

		public AuthenticationInfo(String authHeader) {
			cnonce = "0a4f113b";
			nc = 1;


			// get method from first word
			int pos = authHeader.IndexOf (" ");
			method = authHeader.Substring (0, pos).ToLower ();
			string remainder = method.Substring (pos + 1);

			// split the rest by comma, then =
			string[] pars = remainder.Split (",");
			string[] newpars = new string[pars.Length];
			int maxnewpars = 0;
			// test possibility that a comma is mid value for a split
			for (int i = 0; i < pars.Length; i++) {
				if (pars[i].EndsWith("\"")) {
					newpars[maxnewpars] = pars[i];
					maxnewpars++;
				} else {
					// merge with next one
					newpars[maxnewpars] = pars[i] + "," + pars[i+1];
					maxnewpars++;
					i++; // skips next value
				}
			}

			// now go through each part, splitting on first = character, and removing leading and trailing spaces and " quotes
			for (int i = 0;i < maxnewpars;i++) {
				int pos2 = newpars[i].IndexOf("=");
				string name = newpars[i].Substring(0,pos2).Trim();
				string value = newpars[i].Substring(pos2 + 1).Trim ();
				if (value.StartsWith("\"")) {
					value = value.Substring(1);
				}
				if (value.EndsWith("\"")) {
					value = value.Substring(0,value.Length - 1);
				}

				if ("qop".Equals (name)) {
					qop = value;
				} else if ("realm".Equals (name)) {
					realm = value;
				} else if ("nonce".Equals (name)) {
					nonce = value;
				} else if ("opaque".Equals (name)) {
					opaque = value;
				}
			}
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
            client.Headers[ServiceStack.Common.Web.HttpHeaders.Authorization]
                = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password));
        }

		internal static string CalculateMD5Hash(string input)
		{
			// copied/pasted by adamfowleruk
			// step 1, calculate MD5 hash from input
			MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
			byte[] hash = md5.ComputeHash(inputBytes);
			
			// step 2, convert byte array to hex string
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("X2"));
			}
			return sb.ToString();
		}

		internal static string padNC(int num) 
		{
			// by adamfowleruk
			var pad = "";
			for (var i = 0;i < (8 - ("" + num).Length);i++) {
				pad += "0";
			}
			var ret = pad + num;
			return ret;
		}

		internal static void AddAuthInfo(this WebRequest client,string userName,string password,AuthenticationInfo authInfo) {
			
			if ("basic".Equals (authInfo.method)) {
				client.AddBasicAuth (userName, password); // FIXME AddBasicAuth ignores the server provided Realm property. Potential Bug.
			} else if ("digest".Equals (authInfo.method)) {
				// do digest auth header using auth info
				// TODO save auth info somewhere for re-use on subsequent requests
				client.AddDigestAuth (userName, password, authInfo);
			}
		}

		internal static void AddDigestAuth(this WebRequest client,string userName,string password,AuthenticationInfo authInfo)
		{
			// by adamfowleruk
			// See Client Request at http://en.wikipedia.org/wiki/Digest_access_authentication

			string ncUse = padNC(authInfo.nc);
			authInfo.nc++; // incrememnt for subsequent requests

			string ha1raw = userName + ":" + authInfo.realm + ":" + password;
			string ha1 = CalculateMD5Hash(ha1raw);


			string ha2raw = client.Method + ":" + client.RequestUri;
			string ha2 = CalculateMD5Hash(ha2raw);

			string md5rraw = ha1 + ":" + authInfo.nonce + ":" + ncUse + ":" + authInfo.cnonce + ":" + authInfo.qop + ":" + ha2;
			string response = CalculateMD5Hash(md5rraw);



			client.Headers[ServiceStack.Common.Web.HttpHeaders.Authorization] = 
				"Digest username=\"" + userName + "\", realm=\"" + authInfo.realm + "\", nonce=\"" + authInfo.nonce + "\", uri=\"" + 
				client.RequestUri + "\", cnonce=\"" + authInfo.cnonce + "\", nc=" + ncUse + ", qop=\"" + authInfo.qop + "\", response=\"" + response + 
				"\", opaque=\"" + authInfo.opaque + "\"";
			// TODO ensure client.RequestUri is the /path/?param=value type URL (MUST include query string)
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

                    var returnDtoType = genericDef.GenericTypeArguments()[0];
                    var hasResponseStatus = returnDtoType is IHasResponseStatus
                        || returnDtoType.GetPropertyInfo("ResponseStatus") != null;
                   
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
