using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Cors
{
    /// <summary>
    /// Plugin adds support for Cross-origin resource sharing (CORS, see http://www.w3.org/TR/access-control/). CORS allows to access resources from different domain which usually forbidden by origin policy. 
    /// </summary>
    public class CorsFeature : IPlugin
    {
        internal const string AllowOriginHeader = "Access-Control-Allow-Origin";
        internal const string AllowMethodsHeader = "Access-Control-Allow-Methods";
        internal const string AllowHeadersHeader = "Access-Control-Allow-Headers";
        internal const string AllowCredentialsHeader = "Access-Control-Allow-Credentials";
        
        private static readonly string [] DefaultMethods = { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
        private static readonly string [] DefaultContentTypes = new[] { "Content-Type" };
        
        private readonly string _allowedOrigins;
        private readonly string _allowedMethods;
        private readonly string _allowedHeaders;

        private readonly bool _allowCredentials;

        private static bool _isInstalled = false;

        /// <summary>
        /// Represents a default constructor with Allow Origin equals to "*", Allowed GET, POST, PUT, DELETE, OPTIONS request and allowed "Content-Type" header.
        /// </summary>
        public CorsFeature()
            : this(null, null, null, false)
        {
        }

        public CorsFeature(string[] allowedOrigins, string[] allowedMethods, string[] allowedHeaders, bool allowCredentials)
        {
            _allowedOrigins = PrepareAllowedOriginsValue(allowedOrigins);
            _allowedMethods = PrepareAllowedMethodsHeaderValue(allowedMethods);
            _allowedHeaders = PrepareAllowedHeadersHeaderValue(allowedHeaders);
            _allowCredentials = allowCredentials;
        }

        protected internal static string PrepareAllowedOriginsValue(string[] allowedOrigins)
        {
            if (allowedOrigins == null) return "*";
            return string.Join(" ", allowedOrigins);
        }

        protected internal static string PrepareAllowedMethodsHeaderValue(string[] allowedMethods)
        {
            if (allowedMethods == null) allowedMethods = DefaultMethods;
            return string.Join(", ", allowedMethods);
        }

        protected internal static string PrepareAllowedHeadersHeaderValue(string[] allowedHeaders)
        {
            if (allowedHeaders == null) allowedHeaders = DefaultContentTypes;
            if (allowedHeaders.Length == 0) return null;
            return string.Join(", ", allowedHeaders);
        }

        public void Register(IAppHost appHost)
        {
            if (_isInstalled) return;
            _isInstalled = true;

            appHost.Config.GlobalResponseHeaders.Add(AllowOriginHeader, _allowedOrigins);
            appHost.Config.GlobalResponseHeaders.Add(AllowMethodsHeader, _allowedMethods);
            if (!string.IsNullOrEmpty(_allowedHeaders)) 
                appHost.Config.GlobalResponseHeaders.Add(AllowHeadersHeader, _allowedHeaders);
            if (_allowCredentials)
                appHost.Config.GlobalResponseHeaders.Add(AllowCredentialsHeader, "true");
        }
    }
}