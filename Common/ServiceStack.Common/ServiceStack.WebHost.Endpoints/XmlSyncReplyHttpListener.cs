using System.Net;
using ServiceStack.Service;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public abstract class XmlSyncReplyHttpListener : HttpListenerBase
	{
		protected override void ProcessRequest(HttpListenerContext context)
		{
			if (string.IsNullOrEmpty(context.Request.RawUrl)) return;

			var operationName = context.Request.GetOperationName();
			var request = CreateRequest(context.Request, operationName);

			const EndpointAttributes endpointAttributes = EndpointAttributes.SyncReply | EndpointAttributes.Xml;
			
			var result = ExecuteService(request, endpointAttributes);

			context.Response.WriteToResponse(result, x => DataContractSerializer.Instance.Parse(result), ContentType.Xml);
		}
	}

}