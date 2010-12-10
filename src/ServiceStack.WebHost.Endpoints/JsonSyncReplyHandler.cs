using System;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class JsonSyncReplyHandler : JsonHandlerBase
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(JsonSyncReplyHandler));

		public override EndpointAttributes HandlerAttributes
		{
			get { return EndpointAttributes.SyncReply | EndpointAttributes.Json; }
		}

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			try
			{
				var request = CreateRequest(httpReq, operationName);

				var response = ExecuteService(request,
					HandlerAttributes | GetEndpointAttributes(httpReq));

				httpRes.WriteToResponse(response, Serialize, ContentType.Json);
			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);

				httpRes.WriteJsonErrorToResponse(operationName, errorMessage, ex);
			}
		}
	}

}