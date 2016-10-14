using System.Web;
using System.Web.UI;
using ServiceStack.Templates;
using ServiceStack.Web;

namespace ServiceStack.Metadata
{
    public class OperationControl
    {
        public ServiceEndpointsMetadataConfig MetadataConfig { get; set; }

        public Format Format
        {
            set
            {
                this.ContentType = value.ToContentType();
                this.ContentFormat = ServiceStack.ContentFormat.GetContentFormat(value);
            }
        }

        public IRequest HttpRequest { get; set; }
        public string ContentType { get; set; }
        public string ContentFormat { get; set; }

        public string Title { get; set; }
        public string OperationName { get; set; }
        public string HostName { get; set; }
        public string RequestMessage { get; set; }
        public string ResponseMessage { get; set; }

        public string MetadataHtml { get; set; }

        public virtual string RequestUri
        {
            get
            {
                var endpointConfig = MetadataConfig.GetEndpointConfig(ContentType);
                var endpontPath = ResponseMessage != null
                    ? endpointConfig.SyncReplyUri : endpointConfig.AsyncOneWayUri;
                return $"{endpontPath}/{OperationName}";
            }
        }

        public virtual void Render(HtmlTextWriter output)
        {
            var baseUrl = HttpRequest.ResolveAbsoluteUrl("~/");
            var renderedTemplate = HtmlTemplates.Format(HtmlTemplates.GetOperationControlTemplate(),
                Title,
                baseUrl.CombineWith(MetadataConfig.DefaultMetadataUri),
                ContentFormat.ToUpper(),
                OperationName,
                HttpRequestTemplate,
                ResponseTemplate,
                MetadataHtml);

            output.Write(renderedTemplate);
        }

        public virtual string HttpRequestTemplate => 
$@"POST {RequestUri} HTTP/1.1 
Host: {HostName} 
Content-Type: {ContentType}
Content-Length: <span class=""value"">length</span>

{PclExportClient.Instance.HtmlEncode(RequestMessage)}";

        public virtual string ResponseTemplate
        {
            get
            {
                var httpResponse = this.HttpResponseTemplate;
                return string.IsNullOrEmpty(httpResponse) ? null :
$@"
<div class=""response"">
<pre>
{httpResponse}
</pre>
</div>
";
            }
        }

        public virtual string HttpResponseTemplate
        {
            get
            {
                if (string.IsNullOrEmpty(ResponseMessage)) return null;
                return
$@"HTTP/1.1 200 OK
Content-Type: {ContentType}
Content-Length: length

{PclExportClient.Instance.HtmlEncode(ResponseMessage)}";
            }
        }
    }
}