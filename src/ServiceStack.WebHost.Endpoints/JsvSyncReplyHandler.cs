using System;
using System.Web;
using ServiceStack.Common.Web;
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

		public override EndpointAttributes HandlerAttributes
		{
			get { return EndpointAttributes.SyncReply | EndpointAttributes.Jsv; }
		}

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			try
			{
				var request = CreateRequest(httpReq, operationName);

				var response = ExecuteService(request,
					HandlerAttributes | GetEndpointAttributes(httpReq));

				var isDebugRequest = httpReq.RawUrl.ToLower().Contains("debug");
				var writeFn = isDebugRequest
					? (Func<object, string>)JsvFormatter.SerializeAndFormat
					: Serialize;
				var contentType = isDebugRequest ? ContentType.PlainText : ContentType.JsvText;

				httpRes.WriteToResponse(response, writeFn, contentType);
			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);

				httpRes.WriteJsvErrorToResponse(operationName, errorMessage, ex);
			}
		}

	}
}