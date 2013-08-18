using System;
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
            : base(ContentType.Jsv, EndpointAttributes.OneWay | EndpointAttributes.Jsv, Feature.Jsv) { }
    }

    public class JsvSyncReplyHandler : GenericHandler
    {
        public JsvSyncReplyHandler()
            : base(ContentType.JsvText, EndpointAttributes.Reply | EndpointAttributes.Jsv, Feature.Jsv) { }

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
                    HandlerAttributes | httpReq.GetAttributes(), httpReq, httpRes);

                WriteDebugResponse(httpRes, response);
            }
            catch (Exception ex)
            {
                if (!EndpointHost.Config.WriteErrorsToResponse) throw;
                HandleException(httpReq, httpRes, operationName, ex);
            }
        }

        public static void WriteDebugResponse(IHttpResponse httpRes, object response)
        {
            httpRes.WriteToResponse(response, WriteDebugRequest,
                new SerializationContext(ContentType.PlainText));

            httpRes.EndRequest();
        }
    }
}