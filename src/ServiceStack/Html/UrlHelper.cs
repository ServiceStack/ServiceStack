using System.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Html
{
	public class UrlHelper
	{
		public string Content(string url)
		{
		    return VirtualPathUtility.ToAbsolute(url);

		    //if (url != null && url.StartsWith("~/"))
		    //{
		    //    return EndpointHost.AppHost.Config.ServiceStackHandlerFactoryPath == null
		    //        ? url.Substring(1)
		    //        : "/{0}{1}".Fmt(EndpointHost.AppHost.Config.ServiceStackHandlerFactoryPath, url.Substring(1));                
		    //}

		    //return url;
		}
	}
}