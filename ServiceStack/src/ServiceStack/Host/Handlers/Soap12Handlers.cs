#if NETFRAMEWORK

using System.Threading.Tasks;
using ServiceStack.Metadata;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers;

public class Soap12Handler : SoapHandler
{
    public Soap12Handler(RequestAttributes soapType) : base(soapType) { }

    protected override System.ServiceModel.Channels.Message GetRequestMessageFromStream(System.IO.Stream requestStream)
    {
        return GetSoap12RequestMessage(requestStream);
    }
}

public class Soap12MessageReplyHttpHandler : Soap12Handler
{
    public Soap12MessageReplyHttpHandler() : base(RequestAttributes.Soap12) { }

    public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
    {
        if (httpReq.Verb == HttpMethods.Get)
        {
            var wsdl = new Soap12WsdlMetadataHandler();
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
