using System.Web;
using System.Xml;
using ServiceStack.Metadata;
using ServiceStack.Server;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Handlers
{
    public class Soap11Handler : SoapHandler
    {
        public Soap11Handler(EndpointAttributes soapType) : base(soapType) { }

        protected override System.ServiceModel.Channels.Message GetRequestMessageFromStream(System.IO.Stream requestStream)
        {
            return GetSoap11RequestMessage(requestStream);
        }
    }

    public class Soap11ReplyHandler : Soap11Handler
    {
        public Soap11ReplyHandler() : base(EndpointAttributes.Soap11) { }
    }

    public class Soap11OneWayHandler : Soap11Handler
    {
        public Soap11OneWayHandler() : base(EndpointAttributes.Soap11) { }

        public override void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod == HttpMethods.Get)
            {
                var wsdl = new Soap11WsdlMetadataHandler();
                wsdl.Execute(context);
                return;
            }

            SendOneWay(null);
        }
    }

    public class Soap11MessageReplyHttpHandler : Soap11Handler, IHttpHandler
    {
        public Soap11MessageReplyHttpHandler() : base(EndpointAttributes.Soap11) { }

        public new void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod == HttpMethods.Get)
            {
                var wsdl = new Soap11WsdlMetadataHandler();
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
                var wsdl = new Soap11WsdlMetadataHandler();
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