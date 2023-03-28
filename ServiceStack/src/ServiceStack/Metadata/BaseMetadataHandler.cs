using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ServiceStack.Host;
using ServiceStack.Web;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.NativeTypes;
using ServiceStack.Templates;

namespace ServiceStack.Metadata;

public abstract class BaseMetadataHandler : HttpAsyncTaskHandler
{
    public abstract Format Format { get; }

    public string ContentType { get; set; }
    public string ContentFormat { get; set; }

    public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
    {
        if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
            return;

        httpRes.ContentType = "text/html; charset=utf-8";
        await ProcessOperationsAsync(httpRes.OutputStream, httpReq, httpRes).ConfigAwait();

        await httpRes.EndHttpHandlerRequestAsync(skipHeaders:true);
    }

    public virtual string CreateResponse(Type type)
    {
        if (type == typeof(string))
            return "(string)";
        if (type == typeof(byte[]))
            return "(byte[])";
        if (type == typeof(Stream))
            return "(Stream)";
        if (type == typeof(HttpWebResponse))
            return "(HttpWebResponse)";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            type = type.GetGenericArguments()[0]; 

        return CreateMessage(type);
    }

    protected virtual async Task ProcessOperationsAsync(Stream writer, IRequest httpReq, IResponse httpRes)
    {
        var operationName = httpReq.QueryString["op"];

        if (!AssertAccess(httpReq, httpRes, operationName)) 
            return;

        ContentFormat = ServiceStack.ContentFormat.GetContentFormat(Format);
        var metadata = HostContext.Metadata;
        if (operationName != null)
        {
            var allTypes = metadata.GetAllOperationTypes();
            //var operationType = allTypes.Single(x => x.Name == operationName);
            var operationType = allTypes.Single(x => x.GetOperationName() == operationName);
            var typeValidationRules = await httpReq.GetAllValidateRulesAsync(operationType.Name).ConfigAwait();
            var op = metadata.GetOperation(operationType).ApplyValidationRules(typeValidationRules);
            var requestMessage = CreateResponse(operationType);
            string responseMessage = null;

            var responseType = metadata.GetResponseTypeByRequest(operationType);
            if (responseType != null)
            {
                responseMessage = CreateResponse(responseType);
            }

            var isSoap = Format == Format.Soap11 || Format == Format.Soap12;
            var sb = StringBuilderCache.Allocate();
            var description = operationType.GetDescription();
            if (!description.IsNullOrEmpty())
            {
                sb.Append($"<h3 id='desc'>{ConvertToHtml(description)}</h3>");
            }

            if (op.RequiresAuthentication)
            {
                sb.AppendLine("<table class='authentication'>" +
                              "<caption><b>Requires Authentication</b><i class='auth' style='display:inline-block;margin:0 0 -4px 5px;'></i></caption>");
                sb.Append("<tr>");

                if (!op.RequiredRoles.IsEmpty())
                {
                    var plural = op.RequiredRoles.Count > 1 ? "s" : "";
                    sb.Append("<td>Required role{0}:</td><td>{1}</td>".Fmt(plural, string.Join(", ", op.RequiredRoles)));
                }
                if (!op.RequiresAnyRole.IsEmpty())
                {
                    var plural = op.RequiresAnyRole.Count > 1 ? "Requires any of the roles" : "Requires the role";
                    sb.Append("<td>{0}:</td><td>{1}</td>".Fmt(plural, string.Join(", ", op.RequiresAnyRole)));
                }

                if (!op.RequiredPermissions.IsEmpty())
                {
                    var plural = op.RequiredPermissions.Count > 1 ? "s" : "";
                    sb.Append("<td>Required permission{0}:</td><td>{1}</td>".Fmt(plural, string.Join(", ", op.RequiredPermissions)));
                }
                if (!op.RequiresAnyPermission.IsEmpty())
                {
                    var plural = op.RequiresAnyPermission.Count > 1 ? "Requires any of the permissions" : "Requires the permission";
                    sb.Append("<td>{0}:</td><td>{1}</td>".Fmt(plural, string.Join(", ", op.RequiresAnyPermission)));
                }

                sb.Append("</tr>");
                sb.Append("</table>");
            }

            if (op.Routes.Count > 0)
            {
                sb.Append("<table class='routes'>");
                if (!isSoap)
                {
                    sb.Append("<caption>The following routes are available for this service:</caption>");
                }
                sb.Append("<tbody>");

                foreach (var route in op.Routes)
                {
                    if (isSoap && !(route.AllowsAllVerbs || route.AllowedVerbs.Contains(HttpMethods.Post)))
                        continue;

                    sb.Append("<tr>");
                    var verbs = route.AllowsAllVerbs ? "All Verbs" : route.AllowedVerbs;

                    if (!isSoap)
                    {
                        var path = "/" + PathUtils.CombinePaths(HostContext.Config.HandlerFactoryPath, route.Path);

                        sb.Append($"<th>{verbs}</th>");
                        sb.Append($"<th>{path}</th>");
                    }
                    sb.Append($"<td>{route.Summary}</td>");
                    sb.Append($"<td><i>{route.Notes}</i></td>");
                    sb.Append("</tr>");
                }

                sb.Append("<tbody>");
                sb.Append("</tbody>");
                sb.Append("</table>");
            }

            sb.Append(@"
<style>
.flex { display:flex }
.space-x-4 > * + * { margin-left: 1rem }
.types-nav a { 
  color: rgb(107 114 128);
  padding: .5rem .75rem; 
  font-weight: 500; 
  font-size: 0.875rem; 
  line-height: 1.25rem; 
  border-radius: 0.375rem; 
  text-decoration: none;
}
.types-nav a:hover { color: rgb(55 65 81) }
.types-nav a.active { background-color: rgb(243 244 246); color: rgb(55 65 81) }
</style>");
                
            var entries = HtmlTemplates.ShowLanguages && HostContext.HasPlugin<NativeTypesFeature>() 
                ? new KeyValuePair<string,string>[] {
                    new ("csharp", "C#"),
                    new ("mjs", "JavaScript"),
                    new ("typescript", "TypeScript"),
                    new ("dart", "Dart"),
                    new ("java", "Java"),
                    new ("kotlin", "Kotlin"),
                    new ("python", "Python"),
                    new ("swift", "Swift"),
                    new ("vbnet", "VB.NET"),
                    new ("fsharp", "F#"),
                } : Array.Empty<KeyValuePair<string, string>>();
            sb.AppendLine("<div class=\"types-nav\"><nav class=\"flex space-x-4\">");
            var queryLang = httpReq.QueryString["lang"];
            var queryLangName = "";
            var showMetadata = string.IsNullOrEmpty(queryLang); 
            var cls = showMetadata ? " class=\"active\"" : "";
            var queryPrefix = $"?op={op.Name}";
            sb.AppendLine($"<a {cls} href='{queryPrefix}'>Metadata</a>");
            foreach (var entry in entries)
            {
                cls = queryLang == entry.Key ? " class=\"active\"" : "";
                if (!string.IsNullOrEmpty(cls))
                    queryLangName = entry.Value;
                sb.AppendLine($"<a {cls} href=\"{queryPrefix}&lang={entry.Key}\">{entry.Value}</a>");
            }
            sb.AppendLine("</nav></div>");
                

            var metadataTypes = metadata.GetMetadataTypesForOperation(httpReq, op);
            if (showMetadata)
            {
                metadataTypes.Each(x => AppendType(sb, op, x));
            }
            else
            {
                try
                {
                    var src = metadataTypes.GenerateSourceCode(queryLang, httpReq, c => c.WithoutOptions = true);
                    sb.AppendLine($"<link href=\"{httpReq.ResolveAbsoluteUrl("~/css/highlight.css")}\" rel=\"stylesheet\" />");
                    sb.AppendLine($"<pre style=\"padding-left:1rem;\"><code lang=\"{queryLang}\">{src.HtmlEncodeLite()}</code></pre>");
                    sb.AppendLine($"<p><a href=\"{httpReq.ResolveAbsoluteUrl($"~/types/{queryLang}?IncludeTypes={op.Name}.*")}\">{queryLangName} {op.Name} DTOs</a></p>");
                    sb.AppendLine($"<script type=\"text/javascript\" src=\"{httpReq.ResolveAbsoluteUrl("~/js/highlight.js")}\"></script>");
                    sb.AppendLine("<script>hljs.highlightAll()</script>");
                }
                catch (Exception e)
                {
                    sb.AppendLine($"<pre>{e}</pre>");
                }
            }

            sb.Append(@"<div class=""call-info"">");
            var overrideExtCopy = HostContext.Config.AllowRouteContentTypeExtensions
                ? $" the <b>.{ContentFormat}</b> suffix or "
                : "";
            sb.AppendFormat(@"<p>To override the Content-type in your clients, use the HTTP <b>Accept</b> Header, append {1} <b>?format={0}</b></p>", ContentFormat, overrideExtCopy);
            if (ContentFormat == "json")
            {
                sb.Append("<p>To embed the response in a <b>jsonp</b> callback, append <b>?callback=myCallback</b></p>");
            }
            sb.Append("</div>");

            await  RenderOperationAsync(writer, httpReq, operationName, requestMessage, responseMessage,
                StringBuilderCache.ReturnAndFree(sb), op).ConfigAwait();
        }
        else
        {
            await RenderOperationsAsync(writer, httpReq, metadata).ConfigAwait();
        }
    }

    private void AppendType(StringBuilder sb, Operation op, MetadataType metadataType)
    {
        if (metadataType.IsEnum == true)
        {
            sb.Append("<table class='enum'>");
            sb.Append($"<caption><b>{ConvertToHtml(metadataType.DisplayType ?? metadataType.Name)}</b> Enum:</caption>");

            var hasEnumValues = !metadataType.EnumMemberValues.IsEmpty() ||
                                !metadataType.EnumValues.IsEmpty();
            if (hasEnumValues)
            {
                sb.Append("<thead><tr>");
                sb.Append("<th>Name</th>");
                sb.Append("<th>Value</th>");
                sb.Append("<th></th>");
                sb.Append("</tr></thead>");
            }
                
            sb.Append("<tbody>");

            for (var i = 0; i < metadataType.EnumNames.Count; i++)
            {
                sb.Append("<tr>");
                if (hasEnumValues)
                {
                    sb.Append("<td>")
                        .Append(metadataType.EnumNames[i])
                        .Append("</td><td>")
                        .Append(!metadataType.EnumMemberValues.IsEmpty() 
                            ? metadataType.EnumMemberValues[i]
                            : metadataType.EnumValues[i])
                        .Append("</td>")
                        .Append($"<td>{metadataType.EnumDescriptions?[i]}</td>");
                }
                else
                {
                    sb.Append("<td>")
                        .Append(metadataType.EnumNames[i])
                        .Append("</td>")
                        .Append($"<td>{metadataType.EnumDescriptions?[i]}</td>");
                }
                sb.Append("</tr>");
            }
                
            sb.Append("</tbody>");
            sb.Append("</table>");
            return;
        }
        if (metadataType.Properties.IsEmpty()) 
            return;
            
        sb.Append("<table class='params'>");
        sb.Append($"<caption><b>{ConvertToHtml(metadataType.DisplayType ?? metadataType.Name)}</b> Parameters:</caption>");
        sb.Append("<thead><tr>");
        sb.Append("<th>Name</th>");
        sb.Append("<th>Parameter</th>");
        sb.Append("<th>Data Type</th>");
        sb.Append("<th>Required</th>");
        sb.Append("<th>Description</th>");
        sb.Append("</tr></thead>");

        sb.Append("<tbody>");
        foreach (var p in metadataType.Properties)
        {
            sb.Append("<tr>");
            sb.Append($"<td>{ConvertToHtml(p.Name)}</td>");
            sb.Append($"<td>{p.GetParamType(metadataType, op)}</td>");
            sb.Append($"<td>{ConvertToHtml(p.DisplayType ?? p.Type)}</td>");
            sb.Append($"<td>{(p.IsRequired.GetValueOrDefault() ? "Yes" : "No")}</td>");

            var desc = p.Description;
            var allowableValues = p.AllowableValues ?? p.Input?.AllowableValues;
            if (!allowableValues.IsEmpty())
            {
                desc += "<h4>Allowable Values</h4>";
                desc += "<ul>";
                allowableValues.Each(x => desc += $"<li>{x}</li>");
                desc += "</ul>";
            }

            var allowableMin = p.AllowableMin ?? (p.Input?.Min != null ? int.TryParse(p.Input?.Min, out var min) ? min : null : null);
            if (allowableMin != null)
            {
                var allowableMax = p.AllowableMax ?? (p.Input?.Max != null ? int.TryParse(p.Input?.Max, out var max) ? max : null : null);
                desc += $"<h4>Valid Range: {allowableMin} - {allowableMax}</h4>";
            }
            sb.Append($"<td>{desc}</td>");
                
            sb.Append("</tr>");
        }
        sb.Append("</tbody>");
        sb.Append("</table>");
    }

    protected virtual async Task RenderOperationsAsync(Stream output, IRequest httpReq, ServiceMetadata metadata)
    {
        var allValidationRules = await httpReq.GetAllValidateRulesAsync().ConfigAwait();
            
        var defaultPage = new IndexOperationsControl
        {
            Request = httpReq,
            MetadataConfig = HostContext.MetadataPagesConfig,
            Title = HostContext.ServiceName,
            Xsds = XsdTypes.Xsds,
            XsdServiceTypesIndex = 1,
            OperationNames = metadata.GetOperationNamesForMetadata(httpReq),
            GetOperation = operationName => {
                var opType = HostContext.Metadata.GetOperationType(operationName);
                var op = HostContext.Metadata.GetOperation(opType).ApplyValidationRules(allValidationRules);
                return op;
            }
        };

        var metadataFeature = HostContext.GetPlugin<MetadataFeature>();
        metadataFeature?.IndexPageFilter?.Invoke(defaultPage);

        await defaultPage.RenderAsync(output).ConfigAwait();
    }

    private string ConvertToHtml(string text)
    {
        return text.Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\n", "<br />\n");
    }

    protected bool AssertAccess(IRequest httpReq, IResponse httpRes, string operationName)
    {
        var appHost = HostContext.AppHost;
        if (!appHost.HasAccessToMetadata(httpReq, httpRes)) return false;

        if (operationName == null) return true; //For non-operation pages we don't need to check further permissions
        if (!appHost.Config.EnableAccessRestrictions) return true;
        if (!appHost.MetadataPagesConfig.IsVisible(httpReq, Format, operationName))
        {
            appHost.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Service Not Available");
            return false;
        }

        return true;
    }

    protected abstract string CreateMessage(Type dtoType);

    protected virtual Task RenderOperationAsync(Stream output, IRequest httpReq, string operationName,
        string requestMessage, string responseMessage, string metadataHtml, Operation operation)
    {
        var operationControl = new OperationControl
        {
            HttpRequest = httpReq,
            MetadataConfig = HostContext.Config.ServiceEndpointsMetadataConfig,
            Title = HostContext.ServiceName,
            Format = this.Format,
            OperationName = operationName,
            HostName = httpReq.GetUrlHostName(),
            RequestMessage = requestMessage,
            ResponseMessage = responseMessage,
            MetadataHtml = metadataHtml,
            Operation = operation,
        };
        if (!this.ContentType.IsNullOrEmpty())
        {
            operationControl.ContentType = this.ContentType;
        }
        if (!this.ContentFormat.IsNullOrEmpty())
        {
            operationControl.ContentFormat = this.ContentFormat;
        }

        var metadataFeature = HostContext.GetPlugin<MetadataFeature>();
        metadataFeature?.DetailPageFilter?.Invoke(operationControl);
        return operationControl.RenderAsync(output);
    }

}