using System;
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

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            var isDebugRequest = httpReq.RawUrl.ToLower().Contains(Keywords.Debug);
            if (!isDebugRequest)
            {
                base.ProcessRequest(httpReq, httpRes, operationName);
                return;
            }

            try
            {
                var request = httpReq.Dto = CreateRequest(httpReq, operationName);

                httpReq.RequestAttributes |= HandlerAttributes;
                var response = ExecuteService(request, httpReq);

                WriteDebugResponse(httpRes, response);
            }
            catch (Exception ex)
            {
                if (!HostContext.Config.WriteErrorsToResponse) throw;
                HandleException(httpReq, httpRes, operationName, ex);
            }
        }
    }
}