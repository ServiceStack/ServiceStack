using System;
using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class JsvAsyncOneWayHandler : JsvHandlerBase
	{
		public override EndpointAttributes HandlerAttributes
		{
			get { return EndpointAttributes.AsyncOneWay | EndpointAttributes.Jsv; }
		}

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			var request = CreateRequest(httpReq, operationName);

			var response = ExecuteService(request, 
				HandlerAttributes | GetEndpointAttributes(httpReq));
		}

	}
}