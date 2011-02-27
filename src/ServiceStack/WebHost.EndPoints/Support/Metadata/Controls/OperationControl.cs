using System.Web;
using System.Web.UI;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Support.Metadata.Controls
{
	public class OperationControl 
	{
		public ServiceEndpointsMetadataConfig MetadataConfig { get; set; }
		
		public EndpointType EndpointType
		{
			set
			{
				this.ContentType = Common.Web.ContentType.GetContentType(value);
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
		public string RestPaths { get; set; }

		public string DescriptionHtml { get; set; }

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
			var renderedTemplate = string.Format(PageTemplate, 
				Title, 
				HttpRequest.GetParentAbsolutePath().ToParentPath() + MetadataConfig.DefaultMetadataUri,
				ContentFormat.ToUpper(), 
				OperationName,
				HttpRequestTemplate, 
				ResponseTemplate,
				RestPathsTemplate,
				DescriptionHtml);

			output.Write(renderedTemplate);
		}

		protected const string PageTemplate =
@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"" >
<head>
    <title>{0}</title>
    <style type=""text/css"">
        BODY  {{
            background-color:white;
            color:#000000;
            font-family:Verdana;
            margin: 0;
        }}
		#desc{{
			margin: 0;
			padding: 0 0 15px 20px;
			font-size: 16px;
		}}
		#desc P {{
			margin: 5px 0;
			padding: 0;
			line-height: 20px;
			color: #444;
		}}
        H1 {{
            background-color: #036;
            color: #FFF;
            font-family: Tahoma;
            font-size: 26px;
            font-weight: normal;
            margin: 0;
            padding: 10px 0 3px 15px;
        }}
        FORM {{
            font-size: 0.7em;
            margin-left: 20px;
            padding-bottom: 2em;
        }}
        UL {{
            margin: 10px 0 0 20px;
        }}
        LI {{
            margin-top: 10px;
        }}
        LI A {{
            color: #369;
            font-weight: bold;
            text-decoration: underline;
        }}
        LI A:hover {{
            color: #C30;
        }}

        H2 {{
	        border-top: 1px solid #003366;
	        color: #036;
	        font-size: 1.5em;
	        font-weight: bold;
	        margin: 25px 0 20px 0;
        }}
        .example {{
	        padding-left: 15px;
        }}
        .example h3 {{ 
	        color:#000000;
	        font-size:1.1em;
	        margin: 10px 0 0 -15px;
        }}
        .example pre {{
	        background-color: #E5E5CC;
	        border: 1px solid #F0F0E0;
	        font-family: Courier New;
	        font-size: small;
	        padding: 5px;
	        margin-right: 15px;
	        white-space: pre-wrap;
        }}
        .example .value {{
	        color: blue;
        }}    
        </style>
</head>
<body>
    <h1>{0}</h1>
    
    <form>
    <div>
        <p><a href=""{1}"">&lt;back to all web services</a></p>
        <h2>{3}</h2>

		{7}

		<div class=""example"">
<!-- REST Examples -->
{6}

            <h3>HTTP + {2}</h3>
            <p> The following are sample HTTP requests and responses. 
                The placeholders shown need to be replaced with actual values.</p>

<div class=""request"">
<pre>
{4}
</pre>
</div>

{5}

        </div>
    </div>
    </form>
</body>
</html>";

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

		public virtual string RestPathsTemplate
		{
			get
			{
				var jsonp = ContentFormat == "json"
		            ? "<p>To embed the response in a <b>jsonp</b> callback, append <b>?callback=myCallback</b></p>"
		            : "";

				return string.IsNullOrEmpty(RestPaths) ? null : string.Format(
@"
<h3>REST Paths</h3>
<p>The following REST paths are available for this service.</p>
<div class=""restpaths"">
<pre>{0}</pre>
<p>To override the Content-type in your clients HTTP <b>Accept</b> Header, append <b>?format={1}</b></p>
{2}
</div>
", RestPaths, ContentFormat, jsonp);
			}
		}
	}
}