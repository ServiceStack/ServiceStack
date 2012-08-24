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

		protected override void Render(HtmlTextWriter output)
		{
			var parentPath = HttpRequest.GetParentAbsolutePath();
			var ignoreFormats = EndpointHost.Config.IgnoreFormatsInMetadata;
			var opTemplate = new StringBuilder("<li><span>{0}</span>");
			if (MetadataConfig.Xml != null && !ignoreFormats.Contains("xml"))
				opTemplate.AppendFormat(@"<a href=""{0}?op={{0}}"">XML</a>", parentPath + MetadataConfig.Xml.DefaultMetadataUri);
			if (MetadataConfig.Json != null && !ignoreFormats.Contains("json"))
				opTemplate.AppendFormat(@"<a href=""{0}?op={{0}}"">JSON</a>", parentPath + MetadataConfig.Json.DefaultMetadataUri);
			if (MetadataConfig.Jsv != null && !ignoreFormats.Contains("jsv"))
				opTemplate.AppendFormat(@"<a href=""{0}?op={{0}}"">JSV</a>", parentPath + MetadataConfig.Jsv.DefaultMetadataUri);

			if (MetadataConfig.Custom != null)
			{
				foreach (var format in EndpointHost.ContentTypeFilter.ContentTypeFormats.Keys)
				{
					if (ignoreFormats.Contains(format)) continue;

					var uri = parentPath + string.Format(MetadataConfig.Custom.DefaultMetadataUri, format);
					opTemplate.AppendFormat(@"<a href=""{0}?op={{0}}"">{1}</a>", uri, format.ToUpper());
				}
			}

			if (MetadataConfig.Soap11 != null && !ignoreFormats.Contains("soap11"))
				opTemplate.AppendFormat(@"<a href=""{0}?op={{0}}"">SOAP 1.1</a>", parentPath + MetadataConfig.Soap11.DefaultMetadataUri);
			if (MetadataConfig.Soap12 != null && !ignoreFormats.Contains("soap12"))
				opTemplate.AppendFormat(@"<a class=""last"" href=""{0}?op={{0}}"">SOAP 1.2</a>", parentPath + MetadataConfig.Soap12.DefaultMetadataUri);

			opTemplate.Append("</li>");

			var operationsPart = new ListTemplate {
				Title = "Operations:",
				ListItems = this.OperationNames,
				ForEachListItem = operation => string.Format(opTemplate.ToString(), operation)
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
            margin: 10px 0 0 10px;
			padding: 0px 0px 0px 10px;
        }}
        LI {{
			clear: left;
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
		.operations UL {{
			list-style: none;
		}}
		.operations SPAN {{
			float: left;
			display: block;
			width: 27em;
		}}
		.operations A {{
			border-right: 1px solid #CCC;
			margin-right: 1em;
			padding-right: 1em;
		}}
		.operations A.last {{
			border:none;
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