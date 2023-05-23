using System;
using System.Collections.Generic;
using ServiceStack.Configuration;
using ServiceStack.Host.Handlers;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Plugin adds support for Cross-origin resource sharing (CORS, see http://www.w3.org/TR/access-control/). 
    /// CORS allows to access resources from different domain which usually forbidden by origin policy. 
    /// </summary>
    public class CorsFeature : IPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.Cors;
        public const string DefaultOrigin = "*";
        public const string DefaultMethods = "GET, POST, PUT, DELETE, PATCH, OPTIONS, HEAD";
        public const string DefaultHeaders = "Content-Type";
        public const int DefaultMaxAge = 600; // num of secs to cache pre-flight responses, Chrome Max 600s

        private readonly string allowedOrigins;
        private readonly string allowedMethods;
        private readonly string allowedHeaders;
        private readonly string exposeHeaders;
        private readonly int? maxAge;

        private readonly bool allowCredentials;

        private readonly ICollection<string> allowOriginWhitelist;

        public ICollection<string> AllowOriginWhitelist => allowOriginWhitelist;

        public bool AutoHandleOptionsRequests { get; set; }
        
        public CorsFeature(IAppSettings appSettings)
        {
            this.allowedHeaders = appSettings.Get("CorsFeature:allowedHeaders", DefaultHeaders);
            this.allowedMethods = appSettings.Get("CorsFeature:allowedMethods", DefaultMethods);
            this.allowOriginWhitelist = appSettings.GetList("CorsFeature:allowOriginWhitelist");
            this.allowCredentials = appSettings.Get("CorsFeature:allowCredentials", false);
            this.maxAge = appSettings.Get("CorsFeature:maxAge", DefaultMaxAge);
        }

        /// <summary>
        /// Represents a default constructor with Allow Origin equals to "*", Allowed GET, POST, PUT, DELETE, OPTIONS request and allowed "Content-Type" header.
        /// </summary>
        public CorsFeature(string allowedOrigins = DefaultOrigin, string allowedMethods = DefaultMethods, string allowedHeaders = DefaultHeaders, bool allowCredentials = false,
            string exposeHeaders = null, int? maxAge = DefaultMaxAge)
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
            string exposeHeaders = null, int? maxAge = DefaultMaxAge)
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

            if (allowOriginWhitelist != null)
            {
                void allowOriginFilter(IRequest httpReq, IResponse httpRes)
                {
                    var origin = httpReq.Headers.Get(HttpHeaders.Origin);
                    if (allowOriginWhitelist.Contains(origin))
                    {
                        httpRes.AddHeader(HttpHeaders.AllowOrigin, origin);
                    }
                }

                appHost.PreRequestFilters.Add(allowOriginFilter);
            }

            if (AutoHandleOptionsRequests)
            {
                //Handles Request and closes Response after emitting global HTTP Headers
                var emitGlobalHeadersHandler = new CustomActionHandler(
                    (httpReq, httpRes) =>
                    {
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