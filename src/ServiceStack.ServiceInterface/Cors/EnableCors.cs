using System;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface.Cors
{
    public class EnableCors : IHasRequestFilter
    {
        public int Priority { get { return 0; } }

        private readonly string allowedOrigins;
        private readonly string allowedMethods;
        private readonly string allowedHeaders;

        private readonly bool allowCredentials;

        private readonly Func<IHttpRequest, object, bool> applyWhere;

        /// <summary>
        /// Represents a default constructor with Allow Origin equals to "*", Allowed GET, POST, PUT, DELETE, OPTIONS request and allowed "Content-Type" header.
        /// </summary>
        public EnableCors(
            string allowedOrigins = "*", 
            string allowedMethods = CorsFeature.DefaultMethods, 
            string allowedHeaders = CorsFeature.DefaultHeaders, 
            bool allowCredentials = false,
            Func<IHttpRequest, object, bool> applyWhere = null)
        {
            this.allowedOrigins = allowedOrigins;
            this.allowedMethods = allowedMethods;
            this.allowedHeaders = allowedHeaders;
            this.allowCredentials = allowCredentials;
            this.applyWhere = applyWhere;
        }

        public void RequestFilter(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            if (applyWhere != null && !applyWhere(req, requestDto))
                return;

            if (!string.IsNullOrEmpty(allowedOrigins))
                res.AddHeader(HttpHeaders.AllowOrigin, allowedOrigins);
            if (!string.IsNullOrEmpty(allowedMethods))
                res.AddHeader(HttpHeaders.AllowMethods, allowedMethods);
            if (!string.IsNullOrEmpty(allowedHeaders))
                res.AddHeader(HttpHeaders.AllowHeaders, allowedHeaders);
            if (allowCredentials)
                res.AddHeader(HttpHeaders.AllowCredentials, "true");
        }

        public IHasRequestFilter Copy()
        {
            return (IHasRequestFilter)MemberwiseClone();
        }
    }
}