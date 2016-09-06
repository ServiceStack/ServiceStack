using System;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Attribute marks that specific response class has support for Cross-origin resource sharing (CORS, see http://www.w3.org/TR/access-control/). CORS allows to access resources from different domain which usually forbidden by origin policy. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class EnableCorsAttribute : AttributeBase, IHasRequestFilter
    {
        public int Priority => 0;

        public bool AutoHandleOptionRequests { get; set; }

        private readonly string allowedOrigins;
        private readonly string allowedMethods;
        private readonly string allowedHeaders;

        private readonly bool allowCredentials;

        /// <summary>
        /// Represents a default constructor with Allow Origin equals to "*", Allowed GET, POST, PUT, DELETE, OPTIONS request and allowed "Content-Type" header.
        /// </summary>
        public EnableCorsAttribute(string allowedOrigins = "*", string allowedMethods = CorsFeature.DefaultMethods, string allowedHeaders = CorsFeature.DefaultHeaders, bool allowCredentials = false)
        {
            this.allowedOrigins = allowedOrigins;
            this.allowedMethods = allowedMethods;
            this.allowedHeaders = allowedHeaders;
            this.allowCredentials = allowCredentials;
            this.AutoHandleOptionRequests = true;
        }

        public void RequestFilter(IRequest req, IResponse res, object requestDto)
        {
            if (!string.IsNullOrEmpty(allowedOrigins))
                res.AddHeader(HttpHeaders.AllowOrigin, allowedOrigins);
            if (!string.IsNullOrEmpty(allowedMethods))
                res.AddHeader(HttpHeaders.AllowMethods, allowedMethods);
            if (!string.IsNullOrEmpty(allowedHeaders))
                res.AddHeader(HttpHeaders.AllowHeaders, allowedHeaders);
            if (allowCredentials)
                res.AddHeader(HttpHeaders.AllowCredentials, "true");

            if (AutoHandleOptionRequests && req.Verb == HttpMethods.Options)
                res.EndRequest();
        }

        public IHasRequestFilter Copy()
        {
            return (IHasRequestFilter)MemberwiseClone();
        }
    }
}
