using System.Web;
using System.Web.UI;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Support.Templates;

namespace ServiceStack.WebHost.Endpoints.Support.Metadata.Controls
{
	public class OperationControl 
	{
		public ServiceEndpointsMetadataConfig MetadataConfig { get; set; }
		
		public Format Format
		{
			set
			{
				this.ContentType = Common.Web.ContentType.ToContentType(value);
				this.ContentFormat = Common.Web.ContentType.GetContentFormat(value);
			}
		}

		public IHttpRequest HttpRequest { get; set; }
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

		public void Render(HtmlTextWriter output)
		{
            var renderedTemplate = string.Format(HtmlTemplates.OperationControlTemplate, 
				Title, 
				HttpRequest.GetParentAbsolutePath().ToParentPath() + MetadataConfig.DefaultMetadataUri,
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