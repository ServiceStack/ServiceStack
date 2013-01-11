using System.Web;
using System.Xml;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
    public class Soap12Handlers : SoapHandler
    {
        public Soap12Handlers() : base(EndpointAttributes.Soap12) { }
    }

    public class Soap12AsyncOneWayHandler : SoapHandler
    {
        public Soap12AsyncOneWayHandler() : base(EndpointAttributes.Soap12) { }
    }

    public class Soap12MessageAsyncOneWayHttpHandler
        : SoapHandler, IHttpHandler
    {
        public Soap12MessageAsyncOneWayHttpHandler() : base(EndpointAttributes.Soap12) { }

        public new void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod == HttpMethods.Get)
            {
                var wsdl = new Soap12WsdlMetadataHandler();
                wsdl.Execute(context);
                return;
            }

            var requestMessage = GetSoap12RequestMessage(context.Request.InputStream);
            SendOneWay(requestMessage);
        }
    }

    public class Soap12MessageSyncReplyHttpHandler : SoapHandler, IHttpHandler
    {
        public Soap12MessageSyncReplyHttpHandler() : base(EndpointAttributes.Soap12) { }

        public new void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod == HttpMethods.Get)
            {
                var wsdl = new Soap12WsdlMetadataHandler();
                wsdl.Execute(context);
                return;
            }

            var requestMessage = GetSoap12RequestMessage(context.Request.InputStream);
            var responseMessage = Send(requestMessage);

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

            var requestMessage = GetSoap12RequestMessage(httpReq.InputStream);
            var responseMessage = Send(requestMessage, httpReq, httpRes);

            httpRes.ContentType = GetSoapContentType(httpReq.ContentType);
            using (var writer = XmlWriter.Create(httpRes.OutputStream))
            {
                responseMessage.WriteMessage(writer);
            }
        }
    }

}