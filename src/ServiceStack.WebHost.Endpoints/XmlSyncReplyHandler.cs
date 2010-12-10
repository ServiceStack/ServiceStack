using System;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class XmlSyncReplyHandler : XmlHandlerBase
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (XmlSyncReplyHandler));

		public override EndpointAttributes HandlerAttributes
		{
			get { return EndpointAttributes.SyncReply | EndpointAttributes.Xml; }
		}

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			try
			{
				if (string.IsNullOrEmpty(operationName)) return;

				var request = CreateRequest(httpReq, operationName);

				var response = ExecuteService(request,
					HandlerAttributes | GetEndpointAttributes(httpReq));

				httpRes.WriteToResponse(response, Serialize, ContentType.Xml);
			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);

				httpRes.WriteXmlErrorToResponse(operationName, errorMessage, ex);
			}
		}
	}

}