using System;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.Web;

namespace ServiceStack.ServiceInterface.Cors
{
    /// <summary>
    /// Attribute marks that specific response class has support for Cross-origin resource sharing (CORS, see http://www.w3.org/TR/access-control/). CORS allows to access resources from different domain which usually forbidden by origin policy. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class EnableCorsAttribute : Attribute, IHasResponseFilter
    {
        public int Priority { get { return 0; } }

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
        }

        public void ResponseFilter(IHttpRequest req, IHttpResponse res, object response)
        {
            if (!string.IsNullOrEmpty(allowedOrigins))
                res.AddHeader(HttpHeaders.AllowOrigin, allowedOrigins);
            if (!string.IsNullOrEmpty(allowedMethods))
                res.AddHeader(HttpHeaders.AllowMethods, allowedMethods);
            if (!string.IsNullOrEmpty(allowedHeaders))
                res.AddHeader(HttpHeaders.AllowHeaders, allowedHeaders);
            if (allowCredentials)
                res.AddHeader(HttpHeaders.AllowCredentials, "true");
        }

        public IHasResponseFilter Copy()
        {
            return (IHasResponseFilter)MemberwiseClone();
        }
    }
}
