using System.Web;

namespace ServiceStack.Metadata
{
    internal class Soap12OperationControl : OperationControl
    {
        public Soap12OperationControl()
        {
            Format = Format.Soap12;
        }

        public override string RequestUri
        {
            get
            {
                var endpointConfig = MetadataConfig.Soap12;
                var endpontPath = ResponseMessage != null ? endpointConfig.SyncReplyUri : endpointConfig.AsyncOneWayUri;
                return $"{endpontPath}";
            }
        }

        public override string HttpRequestTemplate => 
$@"POST {RequestUri} HTTP/1.1 
Host: {HostName} 
Content-Type: text/xml; charset=utf-8
Content-Length: <span class=""value"">length</span>

{PclExportClient.Instance.HtmlEncode(RequestMessage)}";
    }
}