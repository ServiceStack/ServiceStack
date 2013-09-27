using System.Web;

namespace ServiceStack.Metadata
{
    internal class Soap11OperationControl : OperationControl
    {
        public Soap11OperationControl()
        {
            Format = Format.Soap11;
        }

        public override string RequestUri
        {
            get
            {
                var endpointConfig = MetadataConfig.Soap11;
                var endpontPath = ResponseMessage != null ? endpointConfig.SyncReplyUri : endpointConfig.AsyncOneWayUri;
                return string.Format("{0}", endpontPath);
            }
        }

        public override string HttpRequestTemplate
        {
            get
            {
                return string.Format(
@"POST {0} HTTP/1.1 
Host: {1} 
Content-Type: text/xml; charset=utf-8
Content-Length: <span class=""value"">length</span>
SOAPAction: {2}

{3}", RequestUri, HostName, base.OperationName, HttpUtility.HtmlEncode(RequestMessage));
            }
        }

    }
}