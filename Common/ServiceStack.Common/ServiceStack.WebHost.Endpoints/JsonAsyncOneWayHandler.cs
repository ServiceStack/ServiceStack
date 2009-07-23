using System.Web;
using ServiceStack.Service;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class JsonAsyncOneWayHandler : JsonHandlerBase
	{
		public override void ProcessRequest(HttpContext context)
		{
			var operationName = context.Request.GetOperationName();
			if (string.IsNullOrEmpty(operationName)) return;

			var request = CreateRequest(context.Request, operationName);

			var endpointAttributes = EndpointAttributes.AsyncOneWay | EndpointAttributes.Json 
				| GetEndpointAttributes(context.Request);

			var response = ExecuteService(request, endpointAttributes);
		}

	}
}