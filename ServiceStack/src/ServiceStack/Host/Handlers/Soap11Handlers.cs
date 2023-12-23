#if !NETCORE

using System.Threading.Tasks;
using ServiceStack.Metadata;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers;

public class Soap11Handler : SoapHandler
{
    public Soap11Handler(RequestAttributes soapType) : base(soapType) { }

    protected override System.ServiceModel.Channels.Message GetRequestMessageFromStream(System.IO.Stream requestStream)
    {
        return GetSoap11RequestMessage(requestStream);
    }
}

public class Soap11MessageReplyHttpHandler : Soap11Handler
{
    public Soap11MessageReplyHttpHandler() : base(RequestAttributes.Soap11) { }

    public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
    {
        if (httpReq.Verb == HttpMethods.Get)
        {
            var wsdl = new Soap11WsdlMetadataHandler();
            await wsdl.Execute(httpReq, httpRes);
            return;
        }

        var responseMessage = await Send(null, httpReq, httpRes);

        if (httpRes.IsClosed)
            return;

        HostContext.AppHost.WriteSoapMessage(httpReq, responseMessage, httpRes.OutputStream);
    }
}

#endif
