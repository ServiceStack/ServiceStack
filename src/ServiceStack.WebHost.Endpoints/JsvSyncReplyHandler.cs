using System;
using System.IO;
using System.Text;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints
{
	public class JsvAsyncOneWayHandler : GenericHandler
	{
		public JsvAsyncOneWayHandler()
			: base(ContentType.Jsv, EndpointAttributes.AsyncOneWay | EndpointAttributes.Jsv) {}
	}

	public class JsvSyncReplyHandler : GenericHandler
	{
		public JsvSyncReplyHandler()
			: base(ContentType.JsvText, EndpointAttributes.SyncReply | EndpointAttributes.Jsv) {}

		private static void WriteDebugRequest(object dto, Stream stream)
		{
			var bytes = Encoding.UTF8.GetBytes(dto.SerializeAndFormat());
			stream.Write(bytes, 0, bytes.Length);
		}

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			var isDebugRequest = httpReq.RawUrl.ToLower().Contains("debug");
			if (!isDebugRequest)
			{
				base.ProcessRequest(httpReq, httpRes, operationName);
				return;
			}

			try
			{
				var request = CreateRequest(httpReq, operationName);

				var response = ExecuteService(request,
					HandlerAttributes | GetEndpointAttributes(httpReq), httpReq);

				WriteDebugResponse(httpRes, response);
			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);

				httpRes.WriteErrorToResponse(EndpointAttributes.Jsv, operationName, errorMessage, ex);
			}
		}

		public static void WriteDebugResponse(IHttpResponse httpRes, object response)
		{
			httpRes.WriteToResponse(response, WriteDebugRequest, ContentType.PlainText);
		}
	}
}