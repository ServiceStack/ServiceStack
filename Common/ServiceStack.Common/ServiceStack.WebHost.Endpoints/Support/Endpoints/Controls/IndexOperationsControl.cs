using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using ServiceStack.WebHost.Endpoints.Support.Templates;

namespace ServiceStack.WebHost.Endpoints.Support.Endpoints.Controls
{
	internal class IndexOperationsControl : System.Web.UI.Control
	{
		public string Title { get; set; }
		public List<string> OperationNames { get; set; }
		public string UsageExamplesBaseUri { get; set; }
		public IDictionary<int, string> Xsds { get; set; }
		public int XsdServiceTypesIndex { get; set; }

		protected override void Render(HtmlTextWriter output)
		{
			var operationsPart = new ListTemplate {
				Title = "Operations:",
				ListItems = this.OperationNames,
				ForEachListItem = operation => string.Format(
				  @"<li><span>{0}</span>
					  <a href=""Soap11/MetaData/?op={0}"">SOAP 1.1</a>
					  <a href=""Soap12/MetaData/?op={0}"">SOAP 1.2</a>
					  <a href=""Xml/MetaData/?op={0}"">XML</a>
					  <a href=""Json/MetaData/?op={0}"">JSON</a>
					</li>", operation)
			}.ToString();

			var xsdsPart = new ListTemplate {
				Title = "XSDS:",
				ListItemsIntMap = this.Xsds,
				ListItemTemplate = @"<li><a href=""?xsd={0}"">{1}</a></li>"
			}.ToString();


			var renderedTemplate = string.Format(PAGE_TEMPLATE,
												 this.Title, this.UsageExamplesBaseUri, this.XsdServiceTypesIndex,
												 operationsPart, xsdsPart);
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
    <p>The following operations are supported. For a formal definition, please review the Service 
	<a href=""?xsd={2}"">Xsd</a> or <a href=""../Html/"">Html</a>.
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

	<h3>WSDLS:</h3>
	<ul>
		<li><a href=""Soap11/Metadata/Wsdl.aspx"">SOAP 1.1</a>, 
			<a href=""Soap11/Metadata/Wsdl.aspx?flash=true&includeAllTypes=true"">Optimized for flash</a></li>
		<li><a href=""Soap12/Metadata/Wsdl.aspx"">SOAP 1.2</a>, 
			<a href=""Soap12/Metadata/Wsdl.aspx?flash=true&includeAllTypes=true"">Optimized for flash</a></li>
	</ul>
 
    </form>
</body>
</html>";
		#endregion

	}
}