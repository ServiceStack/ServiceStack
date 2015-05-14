using ServiceStack.Web;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace ServiceStack
{
    public class RequestCancelService : Service
    {
        private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> cancelationTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();

        private static Guid? TryGetRequestId(IHttpRequest httReq)
        {
            Guid requestId;
            var requestIdStr = httReq.Headers.Get(CancelRequest.CancelRequestIdHeader);
            if (!string.IsNullOrWhiteSpace(requestIdStr) && Guid.TryParse(requestIdStr, out requestId))
                return requestId;
            return null;
        }

        public static CancellationToken TryRegisterCancelableRequest(IHttpRequest httReq)
        {
            Guid? requestId = TryGetRequestId(httReq);
            if(requestId.HasValue)
            {
                var cs = cancelationTokens.GetOrAdd(requestId.Value, new CancellationTokenSource());
                return cs.Token;
            }
            return default(CancellationToken);
        }

        public static void TryRemoveCancelableRequest(IHttpRequest httReq)
        {
            Guid? requestId = TryGetRequestId(httReq);
            if (requestId.HasValue)
            {
                CancellationTokenSource cs;
                if (cancelationTokens.TryRemove(requestId.Value, out cs))
                    cs.Dispose();                
            }           
        }

        public static CancellationToken GetCancellationToken(IRequest req)
        {
            var httpReq = req as IHttpRequest;
            if(httpReq != null)
                return TryRegisterCancelableRequest(httpReq);
            return default(CancellationToken);
        }
        
        public void Any(CancelRequest req)
        {
            CancellationTokenSource cs;
            if (cancelationTokens.TryRemove(req.RequestId, out cs))
            {
                cs.Cancel();
                cs.Dispose();
            }
            else
            {
                throw new HttpError(HttpStatusCode.ExpectationFailed, 
                    new InvalidOperationException("Failed to cancel request id:" + req.RequestId));
            }
        }
    }

    /*  
     *  Another way to support cancelation without changing IHttpRequest
     *  and its implementations. In fact we dont even need request filter, 
     *  CancellationTokenSource will be created when and if GetCancellationToken 
     *  is called from service
     *  
     *  Response filter will dispose CancellationTokenSource if it was created.
     *  Maybe there is more efficient way to do this?
     */
    public class AsyncRequestCancelationFeature : IPlugin
    {

        public void Register(IAppHost appHost)
        {
            appHost.GlobalRequestFilters.Add(RegisterCancelationSource);
            appHost.GlobalResponseFilters.Add(UnRegisterCancelationSource);
        }
       
        private void RegisterCancelationSource(IRequest req, IResponse resp, object dto)
        {
            var httpReq = req as IHttpRequest;
            if(httpReq != null)
            {
                RequestCancelService.TryRegisterCancelableRequest(httpReq);
            }
        }

        private void UnRegisterCancelationSource(IRequest req, IResponse resp, object dto)
        {
            var httpReq = req as IHttpRequest;
            if (httpReq != null)
            {
                RequestCancelService.TryRemoveCancelableRequest(httpReq);
            }
        }
        
    }
}
