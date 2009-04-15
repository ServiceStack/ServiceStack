using System.IO;
using System.Web;
using ServiceStack.Service;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class JsonAsyncOneWayHandler : JsonHandlerBase
	{
		public override void ProcessRequest(HttpContext context)
		{
			if (string.IsNullOrEmpty(context.Request.PathInfo)) return;

			var operationName = context.Request.PathInfo.Substring("/".Length);
			var request = CreateRequest(context.Request, operationName);
			var endpointAttributes = EndpointAttributes.AsyncOneWay | EndpointAttributes.Json 
				| GetEndpointAttributes(context.Request);
			var response = ExecuteService(request, endpointAttributes);
		}

	}
}