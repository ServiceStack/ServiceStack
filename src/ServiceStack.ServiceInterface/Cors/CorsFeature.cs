using System.Collections.Generic;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Cors
{
    /// <summary>
    /// Plugin adds support for Cross-origin resource sharing (CORS, see http://www.w3.org/TR/access-control/). CORS allows to access resources from different domain which usually forbidden by origin policy. 
    /// </summary>
    public class CorsFeature : IPlugin
    {
        internal const string DefaultMethods = "GET, POST, PUT, DELETE, OPTIONS";
        internal const string DefaultHeaders = "Content-Type";
        
        private readonly string allowedOrigins;
        private readonly string allowedMethods;
        private readonly string allowedHeaders;

        private readonly bool allowCredentials;

        private static bool isInstalled = false;
        private readonly ICollection<string> allowOriginWhitelist;

        /// <summary>
        /// Represents a default constructor with Allow Origin equals to "*", Allowed GET, POST, PUT, DELETE, OPTIONS request and allowed "Content-Type" header.
        /// </summary>
        public CorsFeature(string allowedOrigins = "*", string allowedMethods = DefaultMethods, string allowedHeaders = DefaultHeaders, bool allowCredentials = false)
        {
            this.allowedOrigins = allowedOrigins;
            this.allowedMethods = allowedMethods;
            this.allowedHeaders = allowedHeaders;
            this.allowCredentials = allowCredentials;
        }
        
        public CorsFeature(ICollection<string> allowOriginWhitelist, string allowedMethods = DefaultMethods, string allowedHeaders = DefaultHeaders, bool allowCredentials = false)
        {
            this.allowedMethods = allowedMethods;
            this.allowedHeaders = allowedHeaders;
            this.allowCredentials = allowCredentials;
            this.allowOriginWhitelist = allowOriginWhitelist;
        }

        public void Register(IAppHost appHost)
        {
            if (isInstalled) return;
            isInstalled = true;

            if (!string.IsNullOrEmpty(allowedOrigins) && allowOriginWhitelist == null)
                appHost.Config.GlobalResponseHeaders.Add(HttpHeaders.AllowOrigin, allowedOrigins);
            if (!string.IsNullOrEmpty(allowedMethods))
                appHost.Config.GlobalResponseHeaders.Add(HttpHeaders.AllowMethods, allowedMethods);
            if (!string.IsNullOrEmpty(allowedHeaders))
                appHost.Config.GlobalResponseHeaders.Add(HttpHeaders.AllowHeaders, allowedHeaders);
            if (allowCredentials)
                appHost.Config.GlobalResponseHeaders.Add(HttpHeaders.AllowCredentials, "true");

            if (allowOriginWhitelist != null)
            {
                appHost.GlobalRequestFilters.Add((httpReq, httpRes, requestDto) =>
                    {
                        var origin = httpReq.Headers.Get("Origin");
                        if (allowOriginWhitelist.Contains(origin))
                        {
                            httpRes.AddHeader(HttpHeaders.AllowOrigin, origin);
                        }
                    });
            }
        }
    }
}