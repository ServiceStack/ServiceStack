using System.Net;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.Razor
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


		    var modelType = RazorPage.GetRazorTemplate().ModelType;
            var model = modelType == typeof(DynamicRequestObject) 
                ? null
                : DeserializeHttpRequest(modelType, httpReq, httpReq.ContentType);

            RazorFormat.ProcessRazorPage(httpReq, contentPage, model, httpRes);
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