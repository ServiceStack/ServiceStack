using System;
using System.Collections.Generic;
using ServiceStack.Host.Handlers;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Plugin adds support for Cross-origin resource sharing (CORS, see http://www.w3.org/TR/access-control/). 
    /// CORS allows to access resources from different domain which usually forbidden by origin policy. 
    /// </summary>
    public class CorsFeature : IPlugin
    {
        internal const string DefaultMethods = "GET, POST, PUT, DELETE, OPTIONS";
        internal const string DefaultHeaders = "Content-Type";

        private readonly string allowedOrigins;
        private readonly string allowedMethods;
        private readonly string allowedHeaders;
        private readonly string exposeHeaders;
        private readonly int? maxAge;

        private readonly bool allowCredentials;

        private readonly ICollection<string> allowOriginWhitelist;

        public ICollection<string> AllowOriginWhitelist
        {
            get { return allowOriginWhitelist; }
        }

        public bool AutoHandleOptionsRequests { get; set; }

        /// <summary>
        /// Represents a default constructor with Allow Origin equals to "*", Allowed GET, POST, PUT, DELETE, OPTIONS request and allowed "Content-Type" header.
        /// </summary>
        public CorsFeature(string allowedOrigins = "*", string allowedMethods = DefaultMethods, string allowedHeaders = DefaultHeaders, bool allowCredentials = false, 
            string exposeHeaders = null, int? maxAge = null)
        {
            this.allowedOrigins = allowedOrigins;
            this.allowedMethods = allowedMethods;
            this.allowedHeaders = allowedHeaders;
            this.allowCredentials = allowCredentials;
            this.AutoHandleOptionsRequests = true;
            this.exposeHeaders = exposeHeaders;
            this.maxAge = maxAge;
        }

        public CorsFeature(ICollection<string> allowOriginWhitelist, string allowedMethods = DefaultMethods, string allowedHeaders = DefaultHeaders, bool allowCredentials = false,
            string exposeHeaders = null, int? maxAge = null)
        {
            this.allowedMethods = allowedMethods;
            this.allowedHeaders = allowedHeaders;
            this.allowCredentials = allowCredentials;
            this.allowOriginWhitelist = allowOriginWhitelist;
            this.AutoHandleOptionsRequests = true;
            this.exposeHeaders = exposeHeaders;
            this.maxAge = maxAge;
        }

        public void Register(IAppHost appHost)
        {
            if (appHost.HasMultiplePlugins<CorsFeature>())
                throw new NotSupportedException("CorsFeature has already been registered");

            if (!string.IsNullOrEmpty(allowedOrigins) && allowOriginWhitelist == null)
                appHost.Config.GlobalResponseHeaders.Add(HttpHeaders.AllowOrigin, allowedOrigins);
            if (!string.IsNullOrEmpty(allowedMethods))
                appHost.Config.GlobalResponseHeaders.Add(HttpHeaders.AllowMethods, allowedMethods);
            if (!string.IsNullOrEmpty(allowedHeaders))
                appHost.Config.GlobalResponseHeaders.Add(HttpHeaders.AllowHeaders, allowedHeaders);
            if (allowCredentials)
                appHost.Config.GlobalResponseHeaders.Add(HttpHeaders.AllowCredentials, "true");
            if (exposeHeaders != null)
                appHost.Config.GlobalResponseHeaders.Add(HttpHeaders.ExposeHeaders, exposeHeaders);
            if (maxAge != null)
                appHost.Config.GlobalResponseHeaders.Add(HttpHeaders.AccessControlMaxAge, maxAge.Value.ToString());

            Action<IRequest, IResponse> allowOriginFilter = null;

            if (allowOriginWhitelist != null)
            {
                allowOriginFilter = (httpReq, httpRes) => {
                    var origin = httpReq.Headers.Get(HttpHeaders.Origin);
                    if (allowOriginWhitelist.Contains(origin))
                    {
                        httpRes.AddHeader(HttpHeaders.AllowOrigin, origin);
                    }
                };

                appHost.PreRequestFilters.Add(allowOriginFilter);
            }

            if (AutoHandleOptionsRequests)
            {
                //Handles Request and closes Response after emitting global HTTP Headers
                var emitGlobalHeadersHandler = new CustomActionHandler(
                    (httpReq, httpRes) => {
                        httpRes.EndRequest(); //PreRequestFilters already written in CustomActionHandler
                    });

                appHost.RawHttpHandlers.Add(httpReq =>
                    httpReq.HttpMethod == HttpMethods.Options
                        ? emitGlobalHeadersHandler
                        : null);                
            }
        }
    }
}