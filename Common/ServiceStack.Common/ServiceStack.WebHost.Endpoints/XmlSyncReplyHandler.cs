using System.Web;
using ServiceStack.Service;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class XmlSyncReplyHandler : XmlHandlerBase
	{
		public override void ProcessRequest(HttpContext context)
		{
			if (string.IsNullOrEmpty(context.Request.PathInfo)) return;

			var operationName = context.Request.PathInfo.Substring("/".Length);
			var request = CreateRequest(context.Request, operationName);

			var endpointAttributes = EndpointAttributes.SyncReply | EndpointAttributes.Xml
				 | GetEndpointAttributes(context.Request);
			var response = ExecuteService(request, endpointAttributes);
			if (response == null) return;

			var responseXml = DataContractSerializer.Instance.Parse(response);
			context.Response.ContentType = "application/xml";
			context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
			context.Response.Write(responseXml);
			context.Response.End();
		}

	}
}