using System;
using System.Web;
using ServiceStack.Logging;
using ServiceStack.Service;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class XmlSyncReplyHandler : XmlHandlerBase
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (XmlSyncReplyHandler));

		public override void ProcessRequest(HttpContext context)
		{
			try
			{
				if (string.IsNullOrEmpty(context.Request.PathInfo)) return;

				var operationName = context.Request.PathInfo.Substring("/".Length);
				var request = CreateRequest(context.Request, operationName);

				var endpointAttributes = EndpointAttributes.SyncReply | EndpointAttributes.Xml
					 | GetEndpointAttributes(context.Request);

				var result = ExecuteService(request, endpointAttributes);

				//If its needed re-enable it via configuration
				//context.Response.Cache.SetCacheability(HttpCacheability.NoCache);

				context.Response.WriteToResponse(result, x => DataContractSerializer.Instance.Parse(x), ContentType.Xml);
			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);

				context.Response.WriteErrorToResponse(errorMessage, ex);
			}
		}

	}
}