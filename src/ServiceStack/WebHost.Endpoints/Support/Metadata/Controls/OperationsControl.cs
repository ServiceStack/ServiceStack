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
            var renderedTemplate = HtmlTemplates.Format(HtmlTemplates.OperationsControlTemplate,
                this.Title, this.MetadataOperationPageBodyHtml, operationsPart);
            output.Write(renderedTemplate);
        }

    }
}