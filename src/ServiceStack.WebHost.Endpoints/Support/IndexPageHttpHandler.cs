using System;
using System.Web;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class IndexPageHttpHandler : IHttpHandler
	{
		public void ProcessRequest(HttpContext context)
		{
			var defaultUrl = EndpointHost.Config.ServiceEndpointsMetadataConfig.DefaultMetadataUri;

			if (context.Request.PathInfo == "/"
				|| context.Request.FilePath.EndsWith("/"))
			{
				//new NotFoundHttpHandler().ProcessRequest(context); return;
				
				var relativeUrl = defaultUrl.Substring(defaultUrl.IndexOf('/'));
				var absoluteUrl = context.Request.Url.AbsoluteUri.TrimEnd('/') + relativeUrl;
				context.Response.Redirect(absoluteUrl);
			}
			else
			{
				context.Response.Redirect(defaultUrl);
			}

		}

		public bool IsReusable
		{
			get { return true; }
		}
	}
}