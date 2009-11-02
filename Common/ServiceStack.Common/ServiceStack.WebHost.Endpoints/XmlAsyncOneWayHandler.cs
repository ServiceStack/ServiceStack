using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class XmlAsyncOneWayHandler : XmlHandlerBase
	{
		public override void ProcessRequest(HttpContext context)
		{
			var operationName = context.Request.GetOperationName();
			if (string.IsNullOrEmpty(operationName)) return;

			var request = CreateRequest(context.Request, operationName);
			
			var endpointAttributes = EndpointAttributes.AsyncOneWay | EndpointAttributes.Xml 
				| GetEndpointAttributes(context.Request);
			
			var result = ExecuteService(request, endpointAttributes);
		}
	}
}