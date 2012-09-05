using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface.Cors
{
    /// <summary>
    /// Attribute marks that specific response class has support for Cross-origin resource sharing (CORS, see http://www.w3.org/TR/access-control/). CORS allows to access resources from different domain which usually forbidden by origin policy. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CorsSupportAttribute : Attribute, IHasResponseFilter
    {
        public int Priority { get { return 0; } }

        private readonly string _allowedOrigins;
        private readonly string _allowedMethods;
        private readonly string _allowedHeaders;

        private readonly bool _allowCredentials;

        /// <summary>
        /// Represents a default constructor with Allow Origin equals to "*", Allowed GET, POST, PUT, DELETE, OPTIONS request and allowed "Content-Type" header.
        /// </summary>
        public CorsSupportAttribute() : this(null, null, null, false)
        {
        }

        public CorsSupportAttribute(string[] allowedOrigins, string[] allowedMethods, string[] allowedHeaders, bool allowCredentials)
        {
            _allowedOrigins = CorsFeature.PrepareAllowedOriginsValue(allowedOrigins);
            _allowedMethods = CorsFeature.PrepareAllowedMethodsHeaderValue(allowedMethods);
            _allowedHeaders = CorsFeature.PrepareAllowedMethodsHeaderValue(allowedHeaders);
            _allowCredentials = allowCredentials;
        }

        public void ResponseFilter(IHttpRequest req, IHttpResponse res, object response)
        {
            res.AddHeader(CorsFeature.AllowOriginHeader, _allowedOrigins);
            res.AddHeader(CorsFeature.AllowMethodsHeader, _allowedMethods);

            if (!string.IsNullOrEmpty(_allowedHeaders)) 
                res.AddHeader(CorsFeature.AllowHeadersHeader, _allowedHeaders);
            if (_allowCredentials)
                res.AddHeader(CorsFeature.AllowCredentialsHeader, "true");

        }

        public IHasResponseFilter Copy()
        {
            return (IHasResponseFilter)MemberwiseClone();
        }
    }
}
