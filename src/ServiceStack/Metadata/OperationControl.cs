using System.Web;
using System.Web.UI;
using ServiceStack.Templates;
using ServiceStack.Text;
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
                this.ContentType = ServiceStack.ContentFormat.ToContentType(value);
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
                return string.Format("{0}/{1}", endpontPath, OperationName);
            }
        }

        public virtual void Render(HtmlTextWriter output)
        {
            string baseUrl = HttpRequest.ResolveAbsoluteUrl("~/");
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

        public virtual string HttpRequestTemplate
        {
            get
            {
                return string.Format(
@"POST {0} HTTP/1.1 
Host: {1} 
Content-Type: {2}
Content-Length: <span class=""value"">length</span>

{3}", RequestUri, HostName, ContentType, HttpUtility.HtmlEncode(RequestMessage));
            }
        }

        public virtual string ResponseTemplate
        {
            get
            {
                var httpResponse = this.HttpResponseTemplate;
                return string.IsNullOrEmpty(httpResponse) ? null : string.Format(@"
<div class=""response"">
<pre>
{0}
</pre>
</div>
", httpResponse);
            }
        }

        public virtual string HttpResponseTemplate
        {
            get
            {
                if (string.IsNullOrEmpty(ResponseMessage)) return null;
                return string.Format(
@"HTTP/1.1 200 OK
Content-Type: {0}
Content-Length: length

{1}", ContentType, HttpUtility.HtmlEncode(ResponseMessage));
            }
        }
    }
}