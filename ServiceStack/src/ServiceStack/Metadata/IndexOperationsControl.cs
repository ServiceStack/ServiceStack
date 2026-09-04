using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Metadata;

public class IndexOperationsControl 
{
    public IRequest Request { get; set; }
    public string Title { get; set; }
    public List<string> OperationNames { get; set; }
    public IDictionary<int, string> Xsds { get; set; }
    public int XsdServiceTypesIndex { get; set; }
    public MetadataPagesConfig MetadataConfig { get; set; }
        
    public Func<string, Operation> GetOperation { get; set; }

    public string RenderRow(string operationName)
    {
        if (operationName == null) return "";
        var show = HostContext.DebugMode //Show in DebugMode
                   && (MetadataConfig?.AlwaysHideInMetadata(operationName) != true); //Hide When [Restrict(VisibilityTo = None)]

        // use a fully qualified path if WebHostUrl is set
        string baseUrl = Request?.ResolveAbsoluteUrl("~/") ?? "/";

        var op = GetOperation?.Invoke(operationName);
        if (op == null) return "";

        var icons = CreateIcons(op);

        var opTemplate = StringBuilderCache.Allocate();

        var tagsStr = op.Tags != null ? op.Tags.Map(x => x).Join(",") : "";
        var isApiSchemaEnabled = HostContext.AppHost?.GetPlugin<MetadataFeature>()?.IsApiSchemaEnabled == true;
        var hasUi = HostContext.AppHost?.HasUi() == true;

        opTemplate.Append($"<tr><th data-tags=\"" + 
                          tagsStr + 
                          "\">" + icons + 
                          (isApiSchemaEnabled ? 
                            """
                            <a href="schema/{0}" style="margin-right:0.3em;" title="{0} Schema"><svg xmlns="http://www.w3.org/2000/svg" width="1.3em" height="1.3em" style="vertical-align:bottom;" viewBox="0 0 24 24">
                                <path d="M0 0h24v24H0z" fill="none" />
                                <path fill="currentColor" d="m12 14l-2-2l2-2l2 2zM9.875 8.471L8.183 6.78L12 2.962l3.817 3.817l-1.692 1.692L12 6.346zm-3.096 7.346L2.962 12l3.817-3.817L8.47 9.875L6.346 12l2.125 2.125zm10.442 0l-1.692-1.692L17.654 12l-2.125-2.125l1.692-1.692L21.038 12zM12 21.038l-3.817-3.817l1.692-1.692L12 17.654l2.125-2.125l1.692 1.692z" />
                            </svg></a>
                            """ 
                            : "") +
                          (hasUi ? "<a href='ui/{0}'>{0}</a>" : "{0}") +
                          "</th>");
        if (MetadataConfig?.AvailableFormatConfigs != null)
        {
            foreach (var config in MetadataConfig.AvailableFormatConfigs)
            {
                var uri = baseUrl.CombineWith(config.DefaultMetadataUri);
                if (MetadataConfig.IsVisible(Request, config.Format.ToFormat(), operationName))
                {
                    show = true;
                    opTemplate.Append($@"<td><a href=""{uri}?op={{0}}"">{config.Name}</a></td>");
                }
                else
                {
                    opTemplate.Append($"<td>{config.Name}</td>");
                }
            }
        }

        opTemplate.Append("</tr>");

        return show ? string.Format(StringBuilderCache.ReturnAndFree(opTemplate), operationName) : "";
    }

    private static string CreateIcons(Operation op)
    {
        if (op == null) return "";
        var sbIcons = StringBuilderCache.Allocate();
        if (op.RequiresAuthentication)
        {
            sbIcons.Append("<i class=\"auth\" title=\"");

            var requiredRolesCount = op.RequiredRoles?.Count ?? 0;
            var requiresAnyRoleCount = op.RequiresAnyRole?.Count ?? 0;
            var hasRoles = requiredRolesCount + requiresAnyRoleCount > 0;
            if (hasRoles)
            {
                sbIcons.Append("Requires Roles:");
                var sbRoles = StringBuilderCacheAlt.Allocate();
                if (op.RequiredRoles != null)
                {
                    foreach (var role in op.RequiredRoles)
                    {
                        if (sbRoles.Length > 0)
                            sbRoles.Append(",");

                        sbRoles.Append(" " + role);
                    }
                }

                if (op.RequiresAnyRole != null)
                {
                    foreach (var role in op.RequiresAnyRole)
                    {
                        if (sbRoles.Length > 0)
                            sbRoles.Append(", ");

                        sbRoles.Append(" " + role + "?");
                    }
                }
                sbIcons.Append(StringBuilderCacheAlt.ReturnAndFree(sbRoles));
            }

            var requiredPermissionsCount = op.RequiredPermissions?.Count ?? 0;
            var requiresAnyPermissionCount = op.RequiresAnyPermission?.Count ?? 0;
            var hasPermissions = requiredPermissionsCount + requiresAnyPermissionCount > 0;
            if (hasPermissions)
            {
                if (hasRoles)
                    sbIcons.Append(". ");

                sbIcons.Append("Requires Permissions:");
                var sbPermission = StringBuilderCacheAlt.Allocate();
                if (op.RequiredPermissions != null)
                {
                    foreach (var permission in op.RequiredPermissions)
                    {
                        if (sbPermission.Length > 0)
                            sbPermission.Append(",");

                        sbPermission.Append(" " + permission);
                    }
                }

                if (op.RequiresAnyPermission != null)
                {
                    foreach (var permission in op.RequiresAnyPermission)
                    {
                        if (sbPermission.Length > 0)
                            sbPermission.Append(",");

                        sbPermission.Append(" " + permission + "?");
                    }
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

    public Task RenderAsync(Stream output)
    {
        var operationsPart = new TableTemplate
        {
            Title = "Operations",
            Items = this.OperationNames,
            ForEachItem = RenderRow
        }.ToString();
        operationsPart = $"<div style=\"margin-bottom:3em\">{operationsPart}</div>\n"
                         + (HostContext.AppHost?.HasUi() == true
                             ? $"<div><h3><a id=\"ui\" href=\"ui\">API Explorer</a></h3></div>\n"
                             : "");
            

#if !NETCORE
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
        var soap11Config = MetadataConfig?.GetMetadataConfig("soap11") as SoapMetadataConfig;
        var soap12Config = MetadataConfig?.GetMetadataConfig("soap12") as SoapMetadataConfig;
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
                ListItemsMap = ToAbsoluteUrls(metadata.PluginLinks),
                ListItemTemplate = "<li><a href=\"{0}\" style=\"" + metadata.PluginLinksStyle + "\">{1}</a></li>"
            }.ToString()
            : "";

        var debugOnlyInfo = HostContext.DebugMode && metadata != null && metadata.DebugLinks.Count > 0
            ? new ListTemplate
            {
                Title = metadata.DebugLinksTitle,
                ListItemsMap = ToAbsoluteUrls(metadata.DebugLinks),
                ListItemTemplate = "<li><a href=\"{0}\" style=\"" + metadata.DebugLinksStyle + "\">{1}</a></li>"
            }.ToString()
            : "";

        var errorCount = HostContext.AppHost?.StartUpErrors?.Count ?? 0;
        var plural = errorCount > 1 ? "s" : "";
        var startupErrors = "";
        if (HostContext.DebugMode)
        {
            startupErrors = errorCount > 0
                ? $"<div class='error-popup'><a href='?debug=requestinfo'>Review {errorCount} Error{plural} on Startup</a></div>"
                : LicenseUtils.LicenseWarningMessage != null 
                    ? $"<div class='error-popup'>{LicenseUtils.LicenseWarningMessage}</div>"                
                    : "";
        }

        var renderedTemplate = Templates.HtmlTemplates.Format(
            Templates.HtmlTemplates.GetIndexOperationsTemplate(),
            this.Title,
            this.XsdServiceTypesIndex,
            operationsPart,
            xsdsPart,
            StringBuilderCache.ReturnAndFree(wsdlTemplate),
            pluginLinks,
            debugOnlyInfo,
            Env.VersionString,
            startupErrors);

        return output.WriteAsync(renderedTemplate);
    }

    public Dictionary<string, string> ToAbsoluteUrls(Dictionary<string, string> linksMap)
    {
        var to = new Dictionary<string,string>();
        if (linksMap == null) return to;
        var baseUrl = Request?.GetBaseUrl() ?? "/";

        foreach (var entry in linksMap)
        {
            if (entry.Key == null) continue;
            var url = entry.Key.IndexOf("://", StringComparison.Ordinal) >= 0 
                ? entry.Key
                : baseUrl.CombineWith(entry.Key);
            to[url] = entry.Value;
        }

        return to;
    }
}