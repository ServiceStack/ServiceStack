using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using ServiceStack.Host;
using ServiceStack.Support.WebHost;
using ServiceStack.Web;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Metadata
{
    public abstract class BaseMetadataHandler : HttpHandlerBase
    {
        public abstract Format Format { get; }

        public string ContentType { get; set; }
        public string ContentFormat { get; set; }

#if !NETSTANDARD1_6
        public override void Execute(HttpContextBase context)
        {
            var writer = new HtmlTextWriter(context.Response.Output);
            context.Response.ContentType = "text/html; charset=utf-8";

            var request = context.ToRequest();
            ProcessOperations(writer, request, request.Response);
        }
#endif

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return;

            using (var sw = new StreamWriter(httpRes.OutputStream))
            {
                var writer = new HtmlTextWriter(sw);
               httpRes.ContentType = "text/html; charset=utf-8";
               ProcessOperations(writer, httpReq, httpRes);
            }

            httpRes.EndHttpHandlerRequest(skipHeaders:true);
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
            if (type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(Task<>))
                type = type.GetGenericArguments()[0]; 

            return CreateMessage(type);
        }

        protected virtual void ProcessOperations(HtmlTextWriter writer, IRequest httpReq, IResponse httpRes)
        {
            var operationName = httpReq.QueryString["op"];

            if (!AssertAccess(httpReq, httpRes, operationName)) return;

            ContentFormat = ServiceStack.ContentFormat.GetContentFormat(Format);
            var metadata = HostContext.Metadata;
            if (operationName != null)
            {
                var allTypes = metadata.GetAllOperationTypes();
                //var operationType = allTypes.Single(x => x.Name == operationName);
                var operationType = allTypes.Single(x => x.GetOperationName() == operationName);
                var op = metadata.GetOperation(operationType);
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

                var metadataTypes = metadata.GetMetadataTypesForOperation(httpReq, op);
                metadataTypes.Each(x => AppendType(sb, op, x));

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

                RenderOperation(writer, httpReq, operationName, requestMessage, responseMessage,
                    StringBuilderCache.ReturnAndFree(sb));
                return;
            }

            RenderOperations(writer, httpReq, metadata);
        }

        private void AppendType(StringBuilder sb, Operation op, MetadataType metadataType)
        {
            if (metadataType.Properties.IsEmpty()) return;
            
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
                if (!p.AllowableValues.IsEmpty())
                {
                    desc += "<h4>Allowable Values</h4>";
                    desc += "<ul>";
                    p.AllowableValues.Each(x => desc += $"<li>{x}</li>");
                    desc += "</ul>";
                }
                if (p.AllowableMin != null)
                {
                    desc += $"<h4>Valid Range: {p.AllowableMin} - {p.AllowableMax}</h4>";
                }
                sb.Append($"<td>{desc}</td>");
                
                sb.Append("</tr>");
            }
            sb.Append("</tbody>");
            sb.Append("</table>");
        }

        protected void RenderOperations(HtmlTextWriter writer, IRequest httpReq, ServiceMetadata metadata)
        {
            var defaultPage = new IndexOperationsControl
            {
                Request = httpReq,
                MetadataConfig = HostContext.MetadataPagesConfig,
                Title = HostContext.ServiceName,
                Xsds = XsdTypes.Xsds,
                XsdServiceTypesIndex = 1,
                OperationNames = metadata.GetOperationNamesForMetadata(httpReq),
            };

            var metadataFeature = HostContext.GetPlugin<MetadataFeature>();
            metadataFeature?.IndexPageFilter?.Invoke(defaultPage);

            defaultPage.RenderControl(writer);
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

        protected virtual void RenderOperation(HtmlTextWriter writer, IRequest httpReq, string operationName,
            string requestMessage, string responseMessage, string metadataHtml)
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
            operationControl.Render(writer);
        }

    }
}
