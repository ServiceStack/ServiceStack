using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public static class HttpRequestAuthentication
    {
        public static string GetBearerToken(this IRequest httpReq)
        {
            var auth = httpReq.Headers[HttpHeaders.Authorization];
            if (auth == null) return null;

            var pos = auth.IndexOf(' ');
            return auth.Substring(0, pos).EqualsIgnoreCase("Bearer") ? auth.Substring(pos + 1) : null;
        }

        public static string GetBasicAuth(this IRequest httpReq)
        {
            var auth = httpReq.Authorization;
            if (auth == null) return null;

            var pos = auth.IndexOf(' ');
            return auth.Substring(0, pos).EqualsIgnoreCase("Basic") ? auth.Substring(pos + 1) : null;
        }

        public static KeyValuePair<string, string>? GetBasicAuthUserAndPassword(this IRequest httpReq)
        {
            var userPassBase64 = httpReq.GetBasicAuth();
            if (userPassBase64 == null) return null;
            var userPass = Encoding.UTF8.GetString(Convert.FromBase64String(userPassBase64));
            var pos = userPass.IndexOf(':');
            return new KeyValuePair<string, string>(userPass.Substring(0, pos), userPass.Substring(pos + 1));
        }

        public static Dictionary<string,string> GetDigestAuth(this IRequest httpReq)
        {
            var auth = httpReq.Headers[HttpHeaders.Authorization];
            if (auth == null) return null;
            var parts = auth.Split(' ');
            // There should be at least to parts
            if (parts.Length < 2) return null;
            // It has to be a digest request
            if (parts[0].ToLower() != "digest") return null;
            // Remove uptil the first space
            auth = auth.Substring(auth.IndexOf(' '));
            
            int i = 0;
            int line = 0;
            bool inQuotes = false;
            bool escape = false;

            var prts = new List<string> { "" };
            auth = auth.Trim(' ', ',');
            while (i < auth.Length)
            {

                if (auth[i]=='"' && !escape)
                    inQuotes = !inQuotes;

                if (auth[i] == ',' && !inQuotes && !escape)
                {
                    i++;
                    prts.Add("");
                    line++;
                }
                else
                {
                    escape = auth[i]=='\\';
                    prts[line] += auth[i];
                    i++;
                }
            }
            
            parts = prts.ToArray();

            try 
            {
                var result = new Dictionary<string, string>();
                foreach (var item in parts)
                {
                    var param = item.Trim().Split(new[] { '=' },2);
                    result.Add(param[0],param[1].Trim('"'));
                }
                result.Add("method", httpReq.Verb);
                result.Add("userhostaddress", httpReq.UserHostAddress);
                return result;
            }
            catch (Exception) {}

            return null;
        }

        public static string GetCookieValue(this IRequest httpReq, string cookieName)
        {
            Cookie cookie;
            httpReq.Cookies.TryGetValue(cookieName, out cookie);
            return cookie?.Value;
        }

        public static string GetItemStringValue(this IRequest httpReq, string itemName)
        {
            object val;
            if (!httpReq.Items.TryGetValue(itemName, out val)) return null;
            return val as string;
        }

    }
}