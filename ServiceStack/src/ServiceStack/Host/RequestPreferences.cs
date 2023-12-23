using ServiceStack.Web;

namespace ServiceStack.Host;

public class RequestPreferences : IRequestPreferences
{
    private string acceptEncoding;

#if !NETCORE
    private readonly System.Web.HttpContextBase httpContext;

    public RequestPreferences(System.Web.HttpContextBase httpContext)
    {
        this.httpContext = httpContext;
        this.acceptEncoding = httpContext.Request.Headers[HttpHeaders.AcceptEncoding];
        if (this.acceptEncoding.IsNullOrEmpty())
        {
            this.acceptEncoding = "none";
            return;
        }
        this.acceptEncoding = this.acceptEncoding.ToLower();
    }

    public static System.Web.HttpWorkerRequest GetWorker(System.Web.HttpContextBase context)
    {
        var provider = (System.IServiceProvider)context;
        var worker = (System.Web.HttpWorkerRequest)provider.GetService(typeof(System.Web.HttpWorkerRequest));
        return worker;
    }

    private System.Web.HttpWorkerRequest httpWorkerRequest;
    private System.Web.HttpWorkerRequest HttpWorkerRequest => this.httpWorkerRequest ??= GetWorker(this.httpContext);

    public string AcceptEncoding
    {
        get
        {
            if (acceptEncoding != null)
                return acceptEncoding;
            if (Text.Env.IsMono)
                return acceptEncoding;

            acceptEncoding = HttpWorkerRequest.GetKnownRequestHeader(
                System.Web.HttpWorkerRequest.HeaderAcceptEncoding)?.ToLower();
            return acceptEncoding;
        }
    }
#else 
    public string AcceptEncoding => acceptEncoding;
#endif

    public RequestPreferences(IRequest httpRequest)
    {
        this.acceptEncoding = httpRequest.Headers[HttpHeaders.AcceptEncoding];
        if (string.IsNullOrEmpty(this.acceptEncoding))
        {
            this.acceptEncoding = "none";
            return;
        }
        this.acceptEncoding = this.acceptEncoding.ToLower();
    }

    public bool AcceptsBrotli => AcceptEncoding != null && AcceptEncoding.Contains("br");
    public bool AcceptsDeflate => AcceptEncoding != null && AcceptEncoding.Contains("deflate");
    public bool AcceptsGzip => AcceptEncoding != null && AcceptEncoding.Contains("gzip");
}
