using System.Web;
using System.Web.UI;
using ServiceStack.Common;
using ServiceStack.ServiceHost;

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
			var renderedTemplate = string.Format(PageTemplate, 
				Title, 
				HttpRequest.GetParentAbsolutePath().ToParentPath() + MetadataConfig.DefaultMetadataUri,
				ContentFormat.ToUpper(), 
				OperationName,
				HttpRequestTemplate, 
				ResponseTemplate,
				MetadataHtml);

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
        table {{
            border-collapse: collapse;
            border-spacing: 0;
            margin: 0 0 20px 0;
            font-size: 12px;
        }}
        caption {{
            text-align: left;
            font-size: 14px;
            padding: 10px 0;
            white-space: nowrap;
        }}
        tbody th {{
            text-align: left;
        }}
        thead th {{
            background-color: #E5E5CC;
            text-align: left;
        }}
        th, td {{
            padding: 5px 15px;
        }}
        .call-info {{
            margin: 0 0 20px 0;
        }}
        a#logo {{
            position: absolute;
            top: 8px;
            right: 5px;
            width: 46px;
            height: 30px;
            background-repeat: no-repeat;
            background-image: url(data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAC4AAAAeCAYAAABTwyyaAAADCklEQVRYw+3YXUhTYRgH8FUXQR8gEV10G5RXdqMIkQV9YWEXEmaQ9jHCXAMrFcNN7YSWkm1Nps2p2Uxzdcyshh9LdLRkqDG3Y3PuM5DuBMkSIlJ5et61E8uW2077UOriD4fDBr/znv97nrPxAIC3GsP5i263O9Fms+1adXCTyRSH+CmMyeVyFWC2rQo4idPpPI5wIEH4N8wTPN674uHeyuhZvE9G8SLSeTzemhUL9111PzE7HI60mMP5JcojFEWt9T1H0/Q6XN3pZfCkRjrcyAkxg2eL7gm3HLiykJZ3151ZVNtWKnucRM4jrGU5uDcLGCnDMBtjUpVzpQ2Spo5XMGxkoKqxCzIK5TNnxEq7zmCEIPAk77E++2LS8UO51S7rpP0nhhzXPuqGsyVKEMnUMMZMBMIv4l0qJzWLKrxUpk5G4KI/FEFfr6MhW1wPHT16CND9AbvdvjWqT5XDudVTgWrR+mIQsooVIG/rBtef8W7Ex0cNfrpYoTK8NQfV6x7dsOcOkDoh1N9nZrD3yVGBX5O2nySbNMgN6YlmwACniuqgs++Nv5WfC2XqcoZT8qeJlY3PQoKzUah7IVtUD+Z31qX4z1ibpIjCxTXqPVLVS05wkolJGwgqmqGB1i7FT+M03hExeF6VStiu0XGGs3n4fAAuUE1gdzh98VactJsjAsfp2T8+MfnXcJKRsXEyyICx/FIdOiLw9Ks1M+FAs3E4nZAlUoDRbPE9fz6scEHFg/zmzn4IJ5xN3P7LoH09wlbmE1Zme1jglFKz4ahQ8jESaDYp/FtgZCwsXh0W+MGLty0hvFBxChlSmfi8t1htP2rkZziFhE4VSIY6evURRbMhG5/gvRfSxwlO1dGbUviVjmBHfLjSpR0CWYvGc4yDaXdIcGF5swA7PTs0aooqmk3OjfueyuCqKwPCM/A9+dJNVUGq8M4H8mYXCzAbsknzq1vJ8SxO1PW/wXMoZXyWuL7wmFA6uDNdPJeQSX3FHwZf+GUN8/yyxoVY5kShfB6f82bfTcr75/6C+w/nmO/AJ8aemGSrCwAAAABJRU5ErkJggg==);
        }}
		#desc{{
			margin: 0;
			padding: 0 0 15px 15px;
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
    <a id=""logo"" href=""http://www.servicestack.net"" title=""servicestack""></a>
    <h1>{0}</h1>
    
    <form>
    <div>
        <p><a href=""{1}"">&lt;back to all web services</a></p>
        <h2>{3}</h2>

		{6}

		<div class=""example"">
<!-- REST Examples -->

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
	}
}