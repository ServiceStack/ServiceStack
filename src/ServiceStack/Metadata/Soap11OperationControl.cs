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
                return $"{endpontPath}";
            }
        }

        public override string HttpRequestTemplate => 
$@"POST {RequestUri} HTTP/1.1 
Host: {HostName} 
Content-Type: text/xml; charset=utf-8
Content-Length: <span class=""value"">length</span>
SOAPAction: {OperationName}

{PclExportClient.Instance.HtmlEncode(RequestMessage)}";
    }
}