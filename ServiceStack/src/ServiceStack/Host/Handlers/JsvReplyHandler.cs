using System;
using System.Threading.Tasks;
using ServiceStack.Web;
using ServiceStack.Text;

namespace ServiceStack.Host.Handlers;

public class JsvOneWayHandler : GenericHandler
{
    public JsvOneWayHandler()
        : base(MimeTypes.Jsv, RequestAttributes.OneWay | RequestAttributes.Jsv, Feature.Jsv) { }
}

public class JsvReplyHandler : GenericHandler
{
    public JsvReplyHandler()
        : base(MimeTypes.JsvText, RequestAttributes.Reply | RequestAttributes.Jsv, Feature.Jsv) { }

    public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
    {
        var isDebugRequest = httpReq.RawUrl.ToLower().Contains(Keywords.Debug);
        if (!isDebugRequest)
        {
            await base.ProcessRequestAsync(httpReq, httpRes, operationName).ConfigAwait();
            return;
        }

        try
        {
            var request = httpReq.Dto = await CreateRequestAsync(httpReq, operationName).ConfigAwaitNetCore();

            await appHost.ApplyRequestFiltersAsync(httpReq, httpRes, request).ConfigAwaitNetCore();
            if (httpRes.IsClosed)
                return;

            httpReq.RequestAttributes |= HandlerAttributes;

            var rawResponse = await GetResponseAsync(httpReq, request).ConfigAwait();

            await WriteDebugResponse(httpRes, rawResponse).ConfigAwait();
        }
        catch (Exception ex)
        {
            if (!HostContext.Config.WriteErrorsToResponse)
                throw;

            await HandleException(httpReq, httpRes, operationName, ex).ConfigAwait();
        }
    }
}
