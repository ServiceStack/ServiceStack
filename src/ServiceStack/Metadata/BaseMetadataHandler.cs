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

namespace ServiceStack.Metadata
{
    public abstract class BaseMetadataHandler : HttpHandlerBase
    {
        public abstract Format Format { get; }

        public string ContentType { get; set; }
        public string ContentFormat { get; set; }

        public override void Execute(HttpContextBase context)
        {
            var writer = new HtmlTextWriter(context.Response.Output);
            context.Response.ContentType = "text/html";

            var request = context.ToRequest();
            ProcessOperations(writer, request, request.Response);
        }

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return;

            using (var sw = new StreamWriter(httpRes.OutputStream))
            {
                var writer = new HtmlTextWriter(sw);
                httpRes.ContentType = "text/html";
                ProcessOperations(writer, httpReq, httpRes);
            }
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
            if (type.IsGenericType)
                type = type.GetGenericArguments()[0]; //e.g. Task<T> => T

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
                var sb = new StringBuilder();
                var description = operationType.GetDescription();
                if (!description.IsNullOrEmpty())
                {
                    sb.AppendFormat("<h3 id='desc'>{0}</div>", ConvertToHtml(description));
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

                            sb.AppendFormat("<th>{0}</th>", verbs);
                            sb.AppendFormat("<th>{0}</th>", path);
                        }
                        sb.AppendFormat("<td>{0}</td>", route.Summary);
                        sb.AppendFormat("<td><i>{0}</i></td>", route.Notes);
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
                   ? " the <b>.{0}</b> suffix or ".Fmt(ContentFormat)
                   : "";
                sb.AppendFormat(@"<p>To override the Content-type in your clients, use the HTTP <b>Accept</b> Header, append {1} <b>?format={0}</b></p>", ContentFormat, overrideExtCopy);
                if (ContentFormat == "json")
                {
                    sb.Append("<p>To embed the response in a <b>jsonp</b> callback, append <b>?callback=myCallback</b></p>");
                }
                sb.Append("</div>");

                RenderOperation(writer, httpReq, operationName, requestMessage, responseMessage, sb.ToString());
                return;
            }

            RenderOperations(writer, httpReq, metadata);
        }

        private void AppendType(StringBuilder sb, Operation op, MetadataType metadataType)
        {
            if (metadataType.Properties.IsEmpty()) return;
            
            sb.Append("<table class='params'>");
            sb.Append("<caption><b>{0}</b> Parameters:</caption>".Fmt(ConvertToHtml(metadataType.DisplayType ?? metadataType.Name)));
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
                sb.AppendFormat("<td>{0}</td>", ConvertToHtml(p.Name));
                sb.AppendFormat("<td>{0}</td>", p.GetParamType(metadataType, op));
                sb.AppendFormat("<td>{0}</td>", ConvertToHtml(p.DisplayType ?? p.Type));
                sb.AppendFormat("<td>{0}</td>", p.IsRequired.GetValueOrDefault() ? "Yes" : "No");

                var desc = p.Description;
                if (!p.AllowableValues.IsEmpty())
                {
                    desc += "<h4>Allowable Values</h4>";
                    desc += "<ul>";
                    p.AllowableValues.Each(x => desc += "<li>{0}</li>".Fmt(x));
                    desc += "</ul>";
                }
                if (p.AllowableMin != null)
                {
                    desc += "<h4>Valid Range: {0} - {1}</h4>".Fmt(p.AllowableMin, p.AllowableMax);
                }
                sb.AppendFormat("<td>{0}</td>", desc);
                
                sb.Append("</tr>");
            }
            sb.Append("</tbody>");
            sb.Append("</table>");
        }

        protected void RenderOperations(HtmlTextWriter writer, IRequest httpReq, ServiceMetadata metadata)
        {
            var defaultPage = new IndexOperationsControl
            {
                HttpRequest = httpReq,
                MetadataConfig = HostContext.MetadataPagesConfig,
                Title = HostContext.ServiceName,
                Xsds = XsdTypes.Xsds,
                XsdServiceTypesIndex = 1,
                OperationNames = metadata.GetOperationNamesForMetadata(httpReq),
            };

            var metadataFeature = HostContext.GetPlugin<MetadataFeature>();
            if (metadataFeature != null && metadataFeature.IndexPageFilter != null)
            {
                metadataFeature.IndexPageFilter(defaultPage);
            }

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
            if (metadataFeature != null && metadataFeature.DetailPageFilter != null)
            {
                metadataFeature.DetailPageFilter(operationControl);
            }

            operationControl.Render(writer);
        }

    }
}