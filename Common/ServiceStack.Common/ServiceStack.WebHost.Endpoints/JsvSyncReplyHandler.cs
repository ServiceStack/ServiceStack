using System;
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

			var endpointAttributes = EndpointAttributes.SyncReply | EndpointAttributes.Jsv
				| GetEndpointAttributes(context.Request);

			var result = ExecuteService(request, endpointAttributes);

			var response = new HttpResponseWrapper(context.Response);

			var isDebugRequest = context.Request.RawUrl.ToLower().Contains("debug");
			var writeFn = isDebugRequest
				? (Func<object, string>)JsvFormatter.SerializeAndFormat
				: TypeSerializer.SerializeToString;
			var contentType = isDebugRequest ? ContentType.PlainText : ContentType.JsvText;

			response.WriteToResponse(result, writeFn, contentType);
		}

	}
}