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
			: base(ContentType.Jsv, EndpointAttributes.AsyncOneWay | EndpointAttributes.Jsv, Feature.Jsv) {}
	}

	public class JsvSyncReplyHandler : GenericHandler
	{
		public JsvSyncReplyHandler()
			: base(ContentType.JsvText, EndpointAttributes.SyncReply | EndpointAttributes.Jsv, Feature.Jsv) { }

		private static void WriteDebugRequest(IRequestContext requestContext, object dto, IHttpResponse httpRes)
		{
			var bytes = Encoding.UTF8.GetBytes(dto.SerializeAndFormat());
			httpRes.OutputStream.Write(bytes, 0, bytes.Length);
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
                bool writeErrorToResponse = ServiceStack.Configuration.ConfigUtils.GetAppSetting<bool>(ServiceStack.Configuration.Keys.WriteErrorsToResponse, true);
                if(!writeErrorToResponse) {
                    throw;
                }
                try {
                    if(!httpRes.IsClosed) {
                        var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
                        httpRes.WriteErrorToResponse(EndpointAttributes.Jsv, operationName, errorMessage, ex);
                    }
                }
                catch(Exception /*WriteErrorEx*/) {
                    //Exception in writing to response should not hide the original exception
                    //Log.Info("Failed to write error to response: {0}", WriteErrorEx);
                    //rethrow the original exception
                    throw ex;
                }
			}
		}

		public static void WriteDebugResponse(IHttpResponse httpRes, object response)
		{
			httpRes.WriteToResponse(response, WriteDebugRequest,
				new SerializationContext(ContentType.PlainText));

			httpRes.Close();
		}
	}
}