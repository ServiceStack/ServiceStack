using System;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class JsvOneWayHandler : GenericHandler
    {
        public JsvOneWayHandler()
            : base(MimeTypes.Jsv, RequestAttributes.OneWay | RequestAttributes.Jsv, Feature.Jsv) { }
    }

    public class JsvReplyHandler : GenericHandler
    {
        public JsvReplyHandler()
            : base(MimeTypes.JsvText, RequestAttributes.Reply | RequestAttributes.Jsv, Feature.Jsv) { }

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
                if (!HostContext.Config.WriteErrorsToResponse) throw;
                HandleException(httpReq, httpRes, operationName, ex);
            }
        }

        public static void WriteDebugResponse(IHttpResponse httpRes, object response)
        {
            httpRes.WriteToResponse(response, WriteDebugRequest,
                new SerializationContext(MimeTypes.PlainText));

            httpRes.EndRequest();
        }
    }
}