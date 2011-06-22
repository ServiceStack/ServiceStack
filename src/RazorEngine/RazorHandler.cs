using System.Net;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support;

namespace RazorEngine
{
	public class RazorHandler : EndpointHandlerBase
	{
		public MvcRazorFormat MvcRazorFormat { get; set; }
		public RazorPage RazorPage { get; set; }

		public string PathInfo { get; set; }
		public string FilePath { get; set; }

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			var contentPage = RazorPage;
			if (contentPage == null)
			{
				var pageFilePath = this.FilePath.WithoutExtension();
				contentPage = MvcRazorFormat.GetContentPage(pageFilePath);
			}
			if (contentPage == null)
			{
				httpRes.StatusCode = (int)HttpStatusCode.NotFound;
				return;
			}

			MvcRazorFormat.ReloadModifiedPageAndTemplates(contentPage);

			if (httpReq.DidReturn304NotModified(contentPage.GetLastModified(), httpRes))
				return;

			MvcRazorFormat.ProcessRazorPage(httpReq, contentPage, null, httpRes);
		}

		public override object CreateRequest(IHttpRequest request, string operationName)
		{
			return null;
		}

		public override object GetResponse(IHttpRequest httpReq, object request)
		{
			return null;
		}
	}
}