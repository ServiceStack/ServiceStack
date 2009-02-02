using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using ServiceStack.WebHost.Endpoints.Support.Templates;

namespace ServiceStack.WebHost.Endpoints.Support.Metadata.Controls
{
	internal class IndexOperationsControl : System.Web.UI.Control
	{
		public string Title { get; set; }
		public List<string> OperationNames { get; set; }
		public string UsageExamplesBaseUri { get; set; }
		public IDictionary<int, string> Xsds { get; set; }
		public int XsdServiceTypesIndex { get; set; }
		public ServiceEndpointsMetadataConfig MetadataConfig { get; set; }

		protected override void Render(HtmlTextWriter output)
		{
			var opTemplate = new StringBuilder("<li><span>{0}</span>");
			if (MetadataConfig.Soap11 != null)
				opTemplate.AppendFormat(@"<a href=""../{0}?op={{0}}"">SOAP 1.1</a>", MetadataConfig.Soap11.DefaultMetadataUri);
			if (MetadataConfig.Soap12 != null)
				opTemplate.AppendFormat(@"<a href=""../{0}?op={{0}}"">SOAP 1.2</a>", MetadataConfig.Soap12.DefaultMetadataUri);
			if (MetadataConfig.Soap11 != null)
				opTemplate.AppendFormat(@"<a href=""../{0}?op={{0}}"">XML</a>", MetadataConfig.Xml.DefaultMetadataUri);
			if (MetadataConfig.Soap11 != null)
				opTemplate.AppendFormat(@"<a href=""../{0}?op={{0}}"">JSON</a>", MetadataConfig.Json.DefaultMetadataUri);
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
						@"<li><a href=""{0}"">SOAP 1.1</a>, <a href=""{0}?flash=true&includeAllTypes=true"">Optimized for flash</a></li>",
						MetadataConfig.Soap11.WsdlMetadataUri);
				}
				if (MetadataConfig.Soap12 != null)
				{
					wsdlTemplate.AppendFormat(
						@"<li><a href=""{0}"">SOAP 1.2</a>, <a href=""{0}?flash=true&includeAllTypes=true"">Optimized for flash</a></li>",
						MetadataConfig.Soap12.WsdlMetadataUri);
				}
				wsdlTemplate.AppendLine("<ul>");
			}

			var renderedTemplate = string.Format(PAGE_TEMPLATE,
												 this.Title, this.UsageExamplesBaseUri, this.XsdServiceTypesIndex,
												 operationsPart, xsdsPart, wsdlTemplate);
			output.Write(renderedTemplate);
		}

		#region Page Template
		private const string PAGE_TEMPLATE = @"
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
            margin: 10px 0 0 20px;
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
		.operations SPAN {{
			float: left;
			display: block;
			width: 15em;
		}}
		.operations A {{
			border-left: solid 1px #ccc;
			margin-left: 1em;
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
    
    <br />
    <h3>Client Usage Examples:</h3>
    <ul>
        <li><a href=""{1}/UsingServiceClients.cs"">Using Service Clients</a></li>
        <li><a href=""{1}/UsingDtoFromAssembly.cs"">Using Dto From Assembly</a></li>
        <li><a href=""{1}/UsingDtoFromXsd.cs"">Using Dto From Xsd</a></li>
        <li><a href=""{1}/UsingServiceReferenceClient.cs"">Using Service Reference Client</a></li>
        <li><a href=""{1}/UsingSvcutilGeneratedClient.cs"">Using SvcUtil Generated Client</a></li>
        <li><a href=""{1}/UsingRawHttpClient.cs"">Using Raw Http Client</a></li>
        <li><a href=""{1}/UsingRestAndJson.cs"">Using Rest and Json</a></li>
        <li><a href=""{1}/UsingRestAndXml.cs"">Using Rest and Xml</a></li>
    </ul>
    
    {4}

	{5}
 
    </form>
</body>
</html>";
		#endregion

	}
}