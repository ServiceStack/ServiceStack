using ServiceStack.Web;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ServiceStack
{
    public class RequestCancelService : Service
    {
        private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> cancelationTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();

        public static CancellationToken TryRegisterCancelableRequest(IHttpRequest httReq)
        {
            Guid requestId;
            var requestIdStr = httReq.Headers.Get(CancelRequest.CancelRequestIdHeader);
            if (!string.IsNullOrWhiteSpace(requestIdStr) && Guid.TryParse(requestIdStr, out requestId))
            {
                var cs = cancelationTokens.GetOrAdd(requestId, new CancellationTokenSource());
                return cs.Token;
            }
            return default(CancellationToken);
        }

        public static void TryRemoveCancelableRequest(IHttpRequest httReq)
        {
            Guid requestId;
            var requestIdStr = httReq.Headers.Get(CancelRequest.CancelRequestIdHeader);
            if (!string.IsNullOrWhiteSpace(requestIdStr) && Guid.TryParse(requestIdStr, out requestId))
            {
                CancellationTokenSource cs;
                if (cancelationTokens.TryRemove(requestId, out cs))
                    cs.Dispose();                
            }           
        }
        
        public void Any(CancelRequest req)
        {
            CancellationTokenSource cs;
            if (cancelationTokens.TryRemove(req.RequestId, out cs))
            {
                cs.Cancel();
                cs.Dispose();
            }
        }
    }
}
