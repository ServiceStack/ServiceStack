using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public static class HttpRequestAuthentication
    {
        public static string GetAuthorization(this IRequest req) => HostContext.AppHost.GetAuthorization(req);
        public static string GetBearerToken(this IRequest req) => HostContext.AppHost.GetBearerToken(req);
        public static string GetJwtToken(this IRequest req) => HostContext.AppHost.GetJwtToken(req);
        public static string GetJwtRefreshToken(this IRequest req) => HostContext.AppHost.GetJwtRefreshToken(req);

        public static string GetBasicAuth(this IRequest req)
        {
            var auth = req.GetAuthorization();
            if (auth == null) 
                return null;

            var pos = auth.IndexOf(' ');
            return pos >= 0 && string.Equals("Basic", auth.Substring(0, pos), StringComparison.OrdinalIgnoreCase) 
                ? auth.Substring(pos + 1)
                : null;
        }

        public static KeyValuePair<string, string>? GetBasicAuthUserAndPassword(this IRequest httpReq)
        {
            var userPassBase64 = httpReq.GetBasicAuth();
            if (userPassBase64 == null) 
                return null;
            var userPass = Encoding.UTF8.GetString(Convert.FromBase64String(userPassBase64));
            var pos = userPass.IndexOf(':');
            if (pos < 0)
                return null;
            return new KeyValuePair<string, string>(userPass.Substring(0, pos), userPass.Substring(pos + 1));
        }

        public static Dictionary<string,string> GetDigestAuth(this IRequest httpReq)
        {
            var auth = httpReq.GetAuthorization();
            if (auth == null) return null;
            var parts = auth.Split(' ');
            // There should be at least to parts
            if (parts.Length < 2) return null;
            // It has to be a digest request
            if (parts[0].ToLowerInvariant() != "digest") return null;
            // Remove up til the first space
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
            httpReq.Cookies.TryGetValue(cookieName, out var cookie);
            return cookie?.Value;
        }

        public static string GetItemStringValue(this IRequest httpReq, string itemName)
        {
            if (!httpReq.Items.TryGetValue(itemName, out var val)) return null;
            return val as string;
        }

    }
}