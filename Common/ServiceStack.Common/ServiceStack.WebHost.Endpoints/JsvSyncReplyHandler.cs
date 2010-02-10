using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class JsvSyncReplyHandler : JsvHandlerBase
	{
		public override void ProcessRequest(HttpContext context)
		{
			var operationName = context.Request.GetOperationName();
			if (string.IsNullOrEmpty(operationName)) return;

			if (!AllowRequest(context)) return;

			var request = CreateRequest(context.Request, operationName);

			var endpointAttributes = EndpointAttributes.SyncReply | EndpointAttributes.Text 
				| GetEndpointAttributes(context.Request);
			
			var result = ExecuteService(request, endpointAttributes);

			var response = new HttpResponseWrapper(context.Response);
			response.WriteToResponse(result, TypeSerializer.SerializeToString, ContentType.JsvText);
		}

	}
}