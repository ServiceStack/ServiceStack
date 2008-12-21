using System.Collections.Generic;
using System.Web.UI;
using ServiceStack.Common.Host.Support.Templates;

namespace ServiceStack.Common.Host.Support.Endpoints.Controls
{
    internal class OperationsControl : System.Web.UI.Control
    {
        public string Title { get; set; }
        public List<string> OperationNames { get; set; }
        public string UsageExamplesBaseUri { get; set; }

        protected override void Render(HtmlTextWriter output)
        {
            var operationsPart = new ListTemplate
            {
                ListItems = this.OperationNames,
                ListItemTemplate = @"<li><a href=""?op={0}"">{0}</a></li>"
            }.ToString();
            var renderedTemplate = string.Format(PAGE_TEMPLATE, 
                this.Title, this.UsageExamplesBaseUri, operationsPart);
            output.Write(renderedTemplate);
        }

        #region Page Template
        private const string PAGE_TEMPLATE = 
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
        </style>
</head>
<body>
    <h1>{0}</h1>
    
    <form>
    <p> The following operations are supported.
        For a more information please view the <a href=""../../Default.ashx"">Service Documentation</a>.
    </p>

    {2}
    
    <br />
    <h3>Usage Examples:</h3>
    <ul>
        <li><a href=""{1}/UsingRestAndJson.cs"">Using Rest and JSON</a></li>
        <li><a href=""{1}/UsingRestAndXml.cs"">Using Rest and XML</a></li>
    </ul>
    
    </form>
</body>
</html>";
        #endregion

    }
}