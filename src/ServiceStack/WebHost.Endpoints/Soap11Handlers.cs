using System.Web;
using System.Xml;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
    public class Soap11SyncReplyHandler : SoapHandler
    {
        public Soap11SyncReplyHandler() : base(EndpointAttributes.Soap11) { }
    }

    public class Soap11AsyncOneWayHandler : SoapHandler
    {
        public Soap11AsyncOneWayHandler() : base(EndpointAttributes.Soap11) { }

        public override void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod == HttpMethods.Get)
            {
                var wsdl = new Soap11WsdlMetadataHandler();
                wsdl.Execute(context);
                return;
            }

            var requestMessage = GetSoap11RequestMessage(context.Request.InputStream);
            SendOneWay(requestMessage);
        }
    }

    public class Soap11MessageSyncReplyHttpHandler : SoapHandler, IHttpHandler
    {
        public Soap11MessageSyncReplyHttpHandler() : base(EndpointAttributes.Soap11) { }

        public new void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod == HttpMethods.Get)
            {
                var wsdl = new Soap11WsdlMetadataHandler();
                wsdl.Execute(context);
                return;
            }

            var requestMessage = GetSoap11RequestMessage(context.Request.InputStream);
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
                var wsdl = new Soap11WsdlMetadataHandler();
                wsdl.Execute(httpReq, httpRes);
                return;
            }

            var requestMessage = GetSoap11RequestMessage(httpReq.InputStream);
            var responseMessage = Send(requestMessage, httpReq, httpRes);

            httpRes.ContentType = GetSoapContentType(httpReq.ContentType);
            using (var writer = XmlWriter.Create(httpRes.OutputStream))
            {
                responseMessage.WriteMessage(writer);
            }
        }
    }

}