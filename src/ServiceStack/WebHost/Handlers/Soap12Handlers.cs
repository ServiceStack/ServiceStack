using System.Web;
using System.Xml;
using ServiceStack.Metadata;
using ServiceStack.Server;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Handlers
{
    public class Soap12Handler : SoapHandler
    {
        public Soap12Handler(EndpointAttributes soapType) : base(soapType) { }

        protected override System.ServiceModel.Channels.Message GetRequestMessageFromStream(System.IO.Stream requestStream)
        {
            return GetSoap12RequestMessage(requestStream);
        }
    }

    public class Soap12Handlers : Soap12Handler
    {
        public Soap12Handlers() : base(EndpointAttributes.Soap12) { }
    }

    public class Soap12OneWayHandler : Soap12Handler
    {
        public Soap12OneWayHandler() : base(EndpointAttributes.Soap12) { }
    }

    public class Soap12MessageOneWayHttpHandler
        : Soap12Handler, IHttpHandler
    {
        public Soap12MessageOneWayHttpHandler() : base(EndpointAttributes.Soap12) { }

        public new void ProcessRequest(HttpContext context)
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

    public class Soap12MessageReplyHttpHandler : Soap12Handler, IHttpHandler
    {
        public Soap12MessageReplyHttpHandler() : base(EndpointAttributes.Soap12) { }

        public new void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod == HttpMethods.Get)
            {
                var wsdl = new Soap12WsdlMetadataHandler();
                wsdl.Execute(context);
                return;
            }

            var responseMessage = Send(null);

            context.Response.ContentType = GetSoapContentType(context.Request.ContentType);
            using (var writer = XmlWriter.Create(context.Response.OutputStream))
            {
                responseMessage.WriteMessage(writer);
            }
        }

        public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            if (httpReq.HttpMethod == HttpMethods.Get)
            {
                var wsdl = new Soap12WsdlMetadataHandler();
                wsdl.Execute(httpReq, httpRes);
                return;
            }

            var responseMessage = Send(null, httpReq, httpRes);

            httpRes.ContentType = GetSoapContentType(httpReq.ContentType);
            using (var writer = XmlWriter.Create(httpRes.OutputStream))
            {
                responseMessage.WriteMessage(writer);
            }
        }
    }

}