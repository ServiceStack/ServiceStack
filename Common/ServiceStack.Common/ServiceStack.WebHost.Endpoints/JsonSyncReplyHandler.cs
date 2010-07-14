using System;
using System.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class JsonSyncReplyHandler : JsonHandlerBase
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(JsonSyncReplyHandler));

		public override void ProcessRequest(HttpContext context)
		{
			var response = new HttpResponseWrapper(context.Response);
			var operationName = context.Request.GetOperationName();
			if (string.IsNullOrEmpty(operationName)) return;

			if (DefaultHandledRequest(context)) return;

			try
			{
				var request = CreateRequest(context.Request, operationName);

				var endpointAttributes = EndpointAttributes.SyncReply | EndpointAttributes.Json
					| GetEndpointAttributes(context.Request);

				var result = ExecuteService(request, endpointAttributes);

				response.WriteToResponse(result, x => JsonDataContractSerializer.Instance.Parse(x), ContentType.Json);
			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);

				response.WriteJsonErrorToResponse(operationName, errorMessage, ex);
			}
		}

	}
}