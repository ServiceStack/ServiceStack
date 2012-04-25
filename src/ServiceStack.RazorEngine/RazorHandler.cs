using System.Net;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.RazorEngine
{
	public class RazorHandler : EndpointHandlerBase
	{
		public RazorFormat RazorFormat { get; set; }
		public ViewPage RazorPage { get; set; }

		public string PathInfo { get; set; }
		public string FilePath { get; set; }

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			var contentPage = RazorPage;
			if (contentPage == null)
			{
				var pageFilePath = this.FilePath.WithoutExtension();
				contentPage = RazorFormat.GetContentPage(pageFilePath);
			}
			if (contentPage == null)
			{
				httpRes.StatusCode = (int)HttpStatusCode.NotFound;
				return;
			}

			RazorFormat.ReloadModifiedPageAndTemplates(contentPage);

			if (httpReq.DidReturn304NotModified(contentPage.GetLastModified(), httpRes))
				return;

			RazorFormat.ProcessRazorPage(httpReq, contentPage, null, httpRes);
		}

		public override object CreateRequest(IHttpRequest request, string operationName)
		{
			return null;
		}

		public override object GetResponse(IHttpRequest httpReq, IHttpResponse httpRes, object request)
		{
			return null;
		}
	}
}