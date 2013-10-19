using System.Reflection;
using System.Web;
using System.Web.Hosting;
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
            if (HostingEnvironment.ApplicationVirtualPath != null
                && virtualPath.StartsWith("~" + HostingEnvironment.ApplicationVirtualPath))
            {
                virtualPath = virtualPath.Remove(1, HostingEnvironment.ApplicationVirtualPath.Length);
            }

            return Config.WebHostUrl == null
                ? VirtualPathUtility.ToAbsolute(virtualPath)
                : httpReq.GetAbsoluteUrl(virtualPath);
        }

        public override string ResolvePhysicalPath(string virtualPath, IRequest httpReq)
        {
            var path = ((AspNetRequest)httpReq).HttpRequest.PhysicalPath;
            return path;
        }
    }
}
