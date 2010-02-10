using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class JsvAsyncOneWayHandler : JsvHandlerBase
	{
		public override void ProcessRequest(HttpContext context)
		{
			var operationName = context.Request.GetOperationName();
			if (string.IsNullOrEmpty(operationName)) return;

			if (!AllowRequest(context)) return;

			var request = CreateRequest(context.Request, operationName);

			var endpointAttributes = EndpointAttributes.AsyncOneWay | EndpointAttributes.Text 
				| GetEndpointAttributes(context.Request);

			var response = ExecuteService(request, endpointAttributes);
		}

	}
}