using System;
using System.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class JsvSyncReplyHandler : JsvHandlerBase
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(JsvSyncReplyHandler));

		public override void ProcessRequest(HttpContext context)
		{
			var response = new HttpResponseWrapper(context.Response);
			var operationName = context.Request.GetOperationName();
			if (string.IsNullOrEmpty(operationName)) return;

			if (DefaultHandledRequest(context)) return;

			try
			{
				var request = CreateRequest(context.Request, operationName);

				var endpointAttributes = EndpointAttributes.SyncReply | EndpointAttributes.Jsv
					| GetEndpointAttributes(context.Request);

				var result = ExecuteService(request, endpointAttributes);

				var isDebugRequest = context.Request.RawUrl.ToLower().Contains("debug");
				var writeFn = isDebugRequest
					? (Func<object, string>)JsvFormatter.SerializeAndFormat
					: TypeSerializer.SerializeToString;
				var contentType = isDebugRequest ? ContentType.PlainText : ContentType.JsvText;

				response.WriteToResponse(result, writeFn, contentType);
			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);

				response.WriteJsvErrorToResponse(operationName, errorMessage, ex);
			}
		}

	}
}