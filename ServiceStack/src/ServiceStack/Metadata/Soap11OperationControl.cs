namespace ServiceStack.Metadata;

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
            var endpointPath = ResponseMessage != null ? endpointConfig.SyncReplyUri : endpointConfig.AsyncOneWayUri;
            return $"{endpointPath}";
        }
    }

    public override string HttpRequestTemplateWithBody(string httpMethod) => 
        $@"POST {RequestUri} HTTP/1.1 
Host: {HostName} 
Content-Type: text/xml; charset=utf-8
Content-Length: <span class=""value"">length</span>
SOAPAction: {OperationName}

{PclExportClient.Instance.HtmlEncode(RequestMessage)}";
}