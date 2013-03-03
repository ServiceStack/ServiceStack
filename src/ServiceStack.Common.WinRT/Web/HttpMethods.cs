using System.Collections.Generic;
using System.Linq;
using ServiceStack.ServiceHost;
#if WINDOWS_PHONE && !WP
using ServiceStack.Text.WP;
#endif

namespace ServiceStack.Common.Web
{
    public static class HttpMethods
    {
        static readonly string[] allVerbs = new[] {
            "OPTIONS", "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "CONNECT", // RFC 2616
            "PROPFIND", "PROPPATCH", "MKCOL", "COPY", "MOVE", "LOCK", "UNLOCK",    // RFC 2518
            "VERSION-CONTROL", "REPORT", "CHECKOUT", "CHECKIN", "UNCHECKOUT",
            "MKWORKSPACE", "UPDATE", "LABEL", "MERGE", "BASELINE-CONTROL", "MKACTIVITY",  // RFC 3253
            "ORDERPATCH", // RFC 3648
            "ACL",        // RFC 3744
            "PATCH",      // https://datatracker.ietf.org/doc/draft-dusseault-http-patch/
            "SEARCH",     // https://datatracker.ietf.org/doc/draft-reschke-webdav-search/
            "BCOPY", "BDELETE", "BMOVE", "BPROPFIND", "BPROPPATCH", "NOTIFY",  
            "POLL",  "SUBSCRIBE", "UNSUBSCRIBE" //MS Exchange WebDav: http://msdn.microsoft.com/en-us/library/aa142917.aspx
        };

        public static HashSet<string> AllVerbs = new HashSet<string>(allVerbs);

        public static bool HasVerb(string httpVerb)
        {
#if NETFX_CORE
            return allVerbs.Any(p => p.Equals(httpVerb.ToUpper()));
#else
            return AllVerbs.Contains(httpVerb.ToUpper());
#endif
        }

        public const string Get = "GET";
        public const string Put = "PUT";
        public const string Post = "POST";
        public const string Delete = "DELETE";
        public const string Options = "OPTIONS";
        public const string Head = "HEAD";
        public const string Patch = "PATCH";

        public static EndpointAttributes GetEndpointAttribute(string httpMethod)
        {
            switch (httpMethod.ToUpper())
            {
                case Get:
                    return EndpointAttributes.HttpGet;
                case Put:
                    return EndpointAttributes.HttpPut;
                case Post:
                    return EndpointAttributes.HttpPost;
                case Delete:
                    return EndpointAttributes.HttpDelete;
                case Patch:
                    return EndpointAttributes.HttpPatch;
                case Head:
                    return EndpointAttributes.HttpHead;
                case Options:
                    return EndpointAttributes.HttpOptions;
            }

            return EndpointAttributes.HttpOther;
        }
    }
}
