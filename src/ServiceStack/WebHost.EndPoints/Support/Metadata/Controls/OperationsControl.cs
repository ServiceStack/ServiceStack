using System.Collections.Generic;
using System.Web.UI;
using ServiceStack.WebHost.Endpoints.Support.Templates;

namespace ServiceStack.WebHost.Endpoints.Support.Metadata.Controls
{
    internal class OperationsControl : System.Web.UI.Control
    {
        public string Title { get; set; }
        public List<string> OperationNames { get; set; }
        public string MetadataOperationPageBodyHtml { get; set; }

        protected override void Render(HtmlTextWriter output)
        {
            var operationsPart = new ListTemplate
            {
                ListItems = this.OperationNames,
                ListItemTemplate = @"<li><a href=""?op={0}"">{0}</a></li>"
            }.ToString();
            var renderedTemplate = string.Format(PageTemplate, 
                this.Title, this.MetadataOperationPageBodyHtml, operationsPart);
            output.Write(renderedTemplate);
        }

        #region Page Template
        private const string PageTemplate = 
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
        For a more information please view the <a href=""../../Metadata"">Service Documentation</a>.
    </p>

    {2}

    {1}    
    
    </form>
</body>
</html>";
        #endregion

    }
}