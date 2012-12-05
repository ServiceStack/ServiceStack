using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Support.Templates;

namespace ServiceStack.WebHost.Endpoints.Support.Metadata.Controls
{
	internal class IndexOperationsControl : System.Web.UI.Control
	{
		public IHttpRequest HttpRequest { get; set; }
		public string Title { get; set; }
		public List<string> OperationNames { get; set; }
		public string MetadataPageBodyHtml { get; set; }
		public IDictionary<int, string> Xsds { get; set; }
		public int XsdServiceTypesIndex { get; set; }
		public ServiceEndpointsMetadataConfig MetadataConfig { get; set; }

        public string RenderRow(string operation)
        {
            var parentPath = HttpRequest.GetParentAbsolutePath();
            var ignoreFormats = EndpointHost.Config.IgnoreFormatsInMetadata;
            var metadata = EndpointHost.Metadata;

            var show = EndpointHost.Config.DebugMode;
            var opTemplate = new StringBuilder("<tr><th>{0}</th>");
            
            if (MetadataConfig.Xml != null && !ignoreFormats.Contains("xml"))
            {
                if (metadata.IsVisible(HttpRequest, Format.Xml, operation))
                {
                    show = true;
                    opTemplate.AppendFormat(@"<td><a href=""{0}?op={{0}}"">XML</a></td>", parentPath + MetadataConfig.Xml.DefaultMetadataUri);
                }
                else
                    opTemplate.AppendFormat("<td>XML</td>");
            }

            if (MetadataConfig.Json != null && !ignoreFormats.Contains("json"))
            {
                if (metadata.IsVisible(HttpRequest, Format.Json, operation))
                {
                    show = true;
                    opTemplate.AppendFormat(@"<td><a href=""{0}?op={{0}}"">JSON</a></td>", parentPath + MetadataConfig.Json.DefaultMetadataUri);
                }
                else
                    opTemplate.AppendFormat("<td>JSON</td>");
            }

            if (MetadataConfig.Jsv != null && !ignoreFormats.Contains("jsv"))
            {
                if (metadata.IsVisible(HttpRequest, Format.Jsv, operation))
                {
                    show = true;
                    opTemplate.AppendFormat(@"<td><a href=""{0}?op={{0}}"">JSV</a></td>", parentPath + MetadataConfig.Jsv.DefaultMetadataUri);
                }
                else
                    opTemplate.AppendFormat("<td>JSV</td>");
            }
            
            if (MetadataConfig.Custom != null)
            {
                foreach (var format in EndpointHost.ContentTypeFilter.ContentTypeFormats.Keys)
                {
                    if (ignoreFormats.Contains(format)) continue;

                    var uri = parentPath + string.Format(MetadataConfig.Custom.DefaultMetadataUri, format);
                    if (metadata.IsVisible(HttpRequest, format.ToFormat(), operation))
                    {
                        show = true;
                        opTemplate.AppendFormat(@"<td><a href=""{0}?op={{0}}"">{1}</a></td>", uri, format.ToUpper());
                    }
                    else
                        opTemplate.AppendFormat("<td>{0}</td>", format.ToUpper());
                }
            }

            if (MetadataConfig.Soap11 != null && !ignoreFormats.Contains("soap11"))
            {
                if (metadata.IsVisible(HttpRequest, Format.Soap11, operation))
                {
                    show = true;
                    opTemplate.AppendFormat(@"<td><a href=""{0}?op={{0}}"">SOAP 1.1</a></td>", parentPath + MetadataConfig.Soap11.DefaultMetadataUri);
                }
                else
                    opTemplate.AppendFormat("<td>SOAP 1.1</td>");                
            }

            if (MetadataConfig.Soap12 != null && !ignoreFormats.Contains("soap12"))
            {
                if (metadata.IsVisible(HttpRequest, Format.Soap12, operation))
                {
                    show = true;
                    opTemplate.AppendFormat(@"<td><a class=""last"" href=""{0}?op={{0}}"">SOAP 1.2</a></td>", parentPath + MetadataConfig.Soap12.DefaultMetadataUri);
                }
                else
                    opTemplate.AppendFormat("<td>SOAP 1.2</td>");
            }
            
            opTemplate.Append("</tr>");

            return show ? string.Format(opTemplate.ToString(), operation) : "";
        }

		protected override void Render(HtmlTextWriter output)
		{

            var operationsPart = new TableTemplate {
				Title = "Operations:",
				Items = this.OperationNames,
                ForEachItem = RenderRow
			}.ToString();

			var xsdsPart = new ListTemplate {
				Title = "XSDS:",
				ListItemsIntMap = this.Xsds,
				ListItemTemplate = @"<li><a href=""?xsd={0}"">{1}</a></li>"
			}.ToString();

			var wsdlTemplate = new StringBuilder();
			if (MetadataConfig.Soap11 != null || MetadataConfig.Soap12 != null)
			{
				wsdlTemplate.AppendLine("<h3>WSDLS:</h3>");
				wsdlTemplate.AppendLine("<ul>");
				if (MetadataConfig.Soap11 != null)
				{
					wsdlTemplate.AppendFormat(
						@"<li><a href=""{0}"">{0}</a></li>",
						MetadataConfig.Soap11.WsdlMetadataUri);
				}
				if (MetadataConfig.Soap12 != null)
				{
					wsdlTemplate.AppendFormat(
						@"<li><a href=""{0}"">{0}</a></li>",
						MetadataConfig.Soap12.WsdlMetadataUri);
				}
				wsdlTemplate.AppendLine("<ul>");
			}

			var renderedTemplate = string.Format(
				PageTemplate, this.Title, this.MetadataPageBodyHtml, this.XsdServiceTypesIndex,
				operationsPart, xsdsPart, wsdlTemplate);

			output.Write(renderedTemplate);
		}

		#region Page Template
		private const string PageTemplate = @"
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">

<html xmlns=""http://www.w3.org/1999/xhtml"" >
<head>
    <title>{0}</title>
    <style type=""text/css"">
        BODY  {{
            background-color:white;
            color:#000000;
            font-family: Verdana, Helvetica, Arial, ""Lucida Grande"", sans-serif; 
            margin: 0;
            font-size: 13px;
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
            margin-left: 20px;
            padding-bottom: 2em;
        }}
        UL {{
            margin: 10px 0 0 10px;
			padding: 0px 0px 0px 10px;
        }}
        LI {{
			clear: left;
            margin-top: 10px;
        }}
        LI A, TD A {{
            color: #369;
            font-weight: bold;
            text-decoration: underline;
        }}
        LI A:hover, TD A:hover {{
            color: #C30;
        }}
		.operations TABLE {{
			margin-left: 1.5em;
            border-collapse: collapse;
		}}
		TABLE, TR, TH, TD {{
            border: none;
		}}
		.operations TH {{
            font-size: 13px;
            text-align: left;
            font-weight: normal;
			min-width: 27em;
            white-space: nowrap;
		}}
		.operations TD {{
            font-size: 12px;
            font-weight: bold;
            color: #999;
			padding-left: 1em;
		}}
        </style>
</head>
<body>
    <h1>{0}</h1>
    
    <form id=""form1"">
    <p>The following operations are supported. For a formal definition, please review the Service <a href=""?xsd={2}"">XSD</a>.
    </p>

	<div class=""operations"">
	  {3}
	</div>

    {1}    
    
    {4}

	{5}
 
    </form>
</body>
</html>";
		#endregion

	}
}