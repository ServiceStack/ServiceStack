using System.IO;
using System.Net;
using System.Text;
using System.Web;
using ServiceStack.Service;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.WebHost.Endpoints
{
	public class XmlSyncReplyHttpListener : HttpListenerBase
	{
		protected override void ProcessRequest(HttpListenerContext context)
		{
			if (string.IsNullOrEmpty(context.Request.RawUrl)) return;

			var operationName = context.Request.Url.Segments[context.Request.Url.Segments.Length - 1];
			var request = CreateRequest(context.Request, operationName);

			const EndpointAttributes endpointAttributes = EndpointAttributes.SyncReply | EndpointAttributes.Xml;
			var response = ExecuteService(request, endpointAttributes);
			if (response == null) return;

			var responseXml = DataContractSerializer.Instance.Parse(response);
			context.Response.ContentType = "application/xml";
			WriteToResponse(context.Response, responseXml);
		}
	}
}