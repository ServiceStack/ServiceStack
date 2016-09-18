using System.Collections.Generic;
using System.Web.UI;
using ServiceStack.Host;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Metadata
{
    public class IndexOperationsControl : System.Web.UI.Control
    {
        public IRequest Request { get; set; }
        public string Title { get; set; }
        public List<string> OperationNames { get; set; }
        public IDictionary<int, string> Xsds { get; set; }
        public int XsdServiceTypesIndex { get; set; }
        public MetadataPagesConfig MetadataConfig { get; set; }

        public string RenderRow(string operationName)
        {
            var show = HostContext.DebugMode //Show in DebugMode
                && !MetadataConfig.AlwaysHideInMetadata(operationName); //Hide When [Restrict(VisibilityTo = None)]

            // use a fully qualified path if WebHostUrl is set
            string baseUrl = Request.ResolveAbsoluteUrl("~/");

            var opType = HostContext.Metadata.GetOperationType(operationName);
            var op = HostContext.Metadata.GetOperation(opType);

            var icons = CreateIcons(op);

            var opTemplate = StringBuilderCache.Allocate();
            opTemplate.Append("<tr><th>" + icons + "{0}</th>");
            foreach (var config in MetadataConfig.AvailableFormatConfigs)
            {
                var uri = baseUrl.CombineWith(config.DefaultMetadataUri);
                if (MetadataConfig.IsVisible(Request, config.Format.ToFormat(), operationName))
                {
                    show = true;
                    opTemplate.Append($@"<td><a href=""{uri}?op={{0}}"">{config.Name.UrlEncode()}</a></td>");
                }
                else
                {
                    opTemplate.Append($"<td>{config.Name}</td>");
                }
            }

            opTemplate.Append("</tr>");

            return show ? string.Format(StringBuilderCache.ReturnAndFree(opTemplate), operationName) : "";
        }

        private static string CreateIcons(Operation op)
        {
            var sbIcons = StringBuilderCache.Allocate();
            if (op.RequiresAuthentication)
            {
                sbIcons.Append("<i class=\"auth\" title=\"");

                var hasRoles = op.RequiredRoles.Count + op.RequiresAnyRole.Count > 0;
                if (hasRoles)
                {
                    sbIcons.Append("Requires Roles:");
                    var sbRoles = StringBuilderCacheAlt.Allocate();
                    foreach (var role in op.RequiredRoles)
                    {
                        if (sbRoles.Length > 0)
                            sbRoles.Append(",");

                        sbRoles.Append(" " + role);
                    }

                    foreach (var role in op.RequiresAnyRole)
                    {
                        if (sbRoles.Length > 0)
                            sbRoles.Append(", ");

                        sbRoles.Append(" " + role + "?");
                    }
                    sbIcons.Append(StringBuilderCacheAlt.ReturnAndFree(sbRoles));
                }

                var hasPermissions = op.RequiredPermissions.Count + op.RequiresAnyPermission.Count > 0;
                if (hasPermissions)
                {
                    if (hasRoles)
                        sbIcons.Append(". ");

                    sbIcons.Append("Requires Permissions:");
                    var sbPermission = StringBuilderCacheAlt.Allocate();
                    foreach (var permission in op.RequiredPermissions)
                    {
                        if (sbPermission.Length > 0)
                            sbPermission.Append(",");

                        sbPermission.Append(" " + permission);
                    }

                    foreach (var permission in op.RequiresAnyPermission)
                    {
                        if (sbPermission.Length > 0)
                            sbPermission.Append(",");

                        sbPermission.Append(" " + permission + "?");
                    }
                    sbIcons.Append(StringBuilderCacheAlt.ReturnAndFree(sbPermission));
                }

                if (!hasRoles && !hasPermissions)
                    sbIcons.Append("Requires Authentication");

                sbIcons.Append("\"></i>");
            }

            var icons = sbIcons.Length > 0
                ? "<span class=\"icons\">" + StringBuilderCache.ReturnAndFree(sbIcons) + "</span>"
                : "";
            return icons;
        }

        protected override void Render(HtmlTextWriter output)
        {
            var operationsPart = new TableTemplate
            {
                Title = "Operations",
                Items = this.OperationNames,
                ForEachItem = RenderRow
            }.ToString();

#if !NETSTANDARD1_6
            var xsdsPart = new ListTemplate
            {
                Title = "XSDS:",
                ListItemsIntMap = this.Xsds,
                ListItemTemplate = @"<li><a href=""?xsd={0}"">{1}</a></li>"
            }.ToString();
#else
            var xsdsPart = "";
#endif

            var wsdlTemplate = StringBuilderCache.Allocate();
            var soap11Config = MetadataConfig.GetMetadataConfig("soap11") as SoapMetadataConfig;
            var soap12Config = MetadataConfig.GetMetadataConfig("soap12") as SoapMetadataConfig;
            if (soap11Config != null || soap12Config != null)
            {
                wsdlTemplate.AppendLine("<h3>WSDLS:</h3>");
                wsdlTemplate.AppendLine("<ul>");
                if (soap11Config != null)
                {
                    wsdlTemplate.AppendFormat(
                        @"<li><a href=""{0}"">{0}</a></li>",
                        soap11Config.WsdlMetadataUri);
                }
                if (soap12Config != null)
                {
                    wsdlTemplate.AppendFormat(
                        @"<li><a href=""{0}"">{0}</a></li>",
                        soap12Config.WsdlMetadataUri);
                }
                wsdlTemplate.AppendLine("</ul>");
            }

            var metadata = HostContext.GetPlugin<MetadataFeature>();
            var pluginLinks = metadata != null && metadata.PluginLinks.Count > 0
                ? new ListTemplate
                {
                    Title = metadata.PluginLinksTitle,
                    ListItemsMap = metadata.PluginLinks,
                    ListItemTemplate = @"<li><a href=""{0}"">{1}</a></li>"
                }.ToString()
                : "";

            var debugOnlyInfo = HostContext.DebugMode && metadata != null && metadata.DebugLinks.Count > 0
                ? new ListTemplate
                {
                    Title = metadata.DebugLinksTitle,
                    ListItemsMap = metadata.DebugLinks,
                    ListItemTemplate = @"<li><a href=""{0}"">{1}</a></li>"
                }.ToString()
                : "";

            var renderedTemplate = HtmlTemplates.Format(
                HtmlTemplates.GetIndexOperationsTemplate(),
                this.Title,
                this.XsdServiceTypesIndex,
                operationsPart,
                xsdsPart,
                StringBuilderCache.ReturnAndFree(wsdlTemplate),
                pluginLinks,
                debugOnlyInfo,
                Env.VersionString);

            output.Write(renderedTemplate);
        }

    }
}