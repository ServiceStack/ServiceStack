using System.Web;
using System.Web.UI;

namespace ServiceStack.Common.Host.Support.Endpoints.Controls
{
	public class OperationControl : System.Web.UI.Control
	{
		public string EndpointType { get; set; }
		public string Title { get; set; }
		public string OperationName { get; set; }
		public string HostName { get; set; }
		public string RequestMessage { get; set; }
		public string ResponseMessage { get; set; }

		public virtual string RequestUri
		{
			get
			{
				var endpontFileName = ResponseMessage != null ? "SyncReply.ashx" : "AsyncOneWay.ashx";
				return string.Format("/Endpoints/{0}/{1}/{2}", EndpointType.ToLower(), endpontFileName, OperationName);
			}
		}

		protected override void Render(HtmlTextWriter output)
		{
			var renderedTemplate = string.Format(PAGE_TEMPLATE, Title, EndpointType, OperationName,
				HttpRequestTemplate, HttpResponseTemplate);
			output.Write(renderedTemplate);
		}

		protected const string PAGE_TEMPLATE =
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
        <p>Click <a href=""../../Default.ashx"">here</a> for a complete list of operations.</p>
        <h2>{2}</h2>
        <div class=""example"">
            <h3>HTTP + {1}</h3>
            <p> The following is a sample HTTP requests and responses. 
                The placeholders shown need to be replaced with actual values.</p>

<div class=""request"">
<pre>
{3}
</pre>       
</div>

<div class=""response"">
<pre>
{4}
</pre>            
</div>

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
Content-Type: application/{2}; charset=utf-8
Content-Length: <span class=""value"">length</span>

{3}", RequestUri, HostName, EndpointType.ToLower(), HttpUtility.HtmlEncode(RequestMessage));
			}
		}

		public virtual string HttpResponseTemplate
		{
			get
			{
				return string.Format(
@"HTTP/1.1 200 OK
Content-Type: application/{0}; charset=utf-8
Content-Length: length

{1}", EndpointType.ToLower(), HttpUtility.HtmlEncode(ResponseMessage));
			}
		}

	}
}