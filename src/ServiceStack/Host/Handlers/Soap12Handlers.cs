using System.Web;
using System.Xml;
using ServiceStack.Metadata;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class Soap12Handler : SoapHandler
    {
        public Soap12Handler(RequestAttributes soapType) : base(soapType) { }

        protected override System.ServiceModel.Channels.Message GetRequestMessageFromStream(System.IO.Stream requestStream)
        {
            return GetSoap12RequestMessage(requestStream);
        }
    }

    public class Soap12Handlers : Soap12Handler
    {
        public Soap12Handlers() : base(RequestAttributes.Soap12) { }
    }

    public class Soap12OneWayHandler : Soap12Handler
    {
        public Soap12OneWayHandler() : base(RequestAttributes.Soap12) { }
    }

    public class Soap12MessageOneWayHttpHandler
        : Soap12Handler
    {
        public Soap12MessageOneWayHttpHandler() : base(RequestAttributes.Soap12) { }

        public override void ProcessRequest(HttpContextBase context)
        {
            if (context.Request.HttpMethod == HttpMethods.Get)
            {
                var wsdl = new Soap12WsdlMetadataHandler();
                wsdl.Execute(context);
                return;
            }

            SendOneWay(null);
        }
    }

    public class Soap12MessageReplyHttpHandler : Soap12Handler
    {
        public Soap12MessageReplyHttpHandler() : base(RequestAttributes.Soap12) { }

        public override void ProcessRequest(HttpContextBase context)
        {
            if (context.Request.HttpMethod == HttpMethods.Get)
            {
                var wsdl = new Soap12WsdlMetadataHandler();
                wsdl.Execute(context);
                return;
            }

            var responseMessage = Send(null);

            context.Response.ContentType = GetSoapContentType(context.Request.ContentType);

            HostContext.AppHost.WriteSoapMessage(responseMessage, context.Response.OutputStream);
        }

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (httpReq.Verb == HttpMethods.Get)
            {
                var wsdl = new Soap12WsdlMetadataHandler();
                wsdl.Execute(httpReq, httpRes);
                return;
            }

            var responseMessage = Send(null, httpReq, httpRes);

            if (httpRes.IsClosed)
                return;

            HostContext.AppHost.WriteSoapMessage(responseMessage, httpRes.OutputStream);
        }
    }

}