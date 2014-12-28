using System.Reflection;
using System.Web;
using ServiceStack.Host.AspNet;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Inherit from this class if you want to host your web services inside an
    /// ASP.NET application.
    /// </summary>
    public abstract class AppHostBase : ServiceStackHost
    {
        protected AppHostBase(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices) { }

        public override string ResolveAbsoluteUrl(string virtualPath, IRequest httpReq)
        {
            virtualPath = virtualPath.SanitizedVirtualPath();
            return httpReq.GetAbsoluteUrl(virtualPath);
        }

        public override string ResolvePhysicalPath(string virtualPath, IRequest httpReq)
        {
            var path = ((AspNetRequest)httpReq).HttpRequest.PhysicalPath;
            return path;
        }

        public override IRequest GetCurrentRequest()
        {
            return HttpContext.Current.ToRequest();
        }
    }
}
