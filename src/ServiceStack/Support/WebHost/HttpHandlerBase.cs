using System.Web;
using ServiceStack.Host.Handlers;

namespace ServiceStack.Support.WebHost
{
    public abstract class HttpHandlerBase : HttpAsyncTaskHandler, IHttpHandler
    {
        public override bool IsReusable => false;
    }
}