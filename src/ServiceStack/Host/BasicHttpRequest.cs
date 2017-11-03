using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class BasicHttpRequest : BasicRequest, IHttpRequest
    {
        public IHttpResponse HttpResponse { get; }
        public string HttpMethod { get; }
        public string XForwardedFor { get; }
        public int? XForwardedPort { get; }
        public string XForwardedProtocol { get; }
        public string XRealIp { get; }
        public string Accept { get; }
    }
}