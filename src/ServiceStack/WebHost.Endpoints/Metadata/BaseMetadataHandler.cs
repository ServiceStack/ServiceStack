using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Support.Metadata.Controls;
using HttpRequestWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpRequestWrapper;
using HttpResponseWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpResponseWrapper;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
    using System.Text;
    using ServiceHost;

    public abstract class BaseMetadataHandler : HttpHandlerBase, IServiceStackHttpHandler
    {
        public abstract Format Format { get; }

        public string ContentType { get; set; }
        public string ContentFormat { get; set; }

        public override void Execute(HttpContext context)
        {
            var writer = new HtmlTextWriter(context.Response.Output);
            context.Response.ContentType = "text/html";

            ProcessOperations(writer, new HttpRequestWrapper(GetType().Name, context.Request), new HttpResponseWrapper(context.Response));
        }

        public virtual void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
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

            return CreateMessage(type);
        }

        protected virtual void ProcessOperations(HtmlTextWriter writer, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            var operationName = httpReq.QueryString["op"];

            if (!AssertAccess(httpReq, httpRes, operationName)) return;

            ContentFormat = Common.Web.ContentType.GetContentFormat(Format);
            var metadata = EndpointHost.Metadata;
            if (operationName != null)
            {
                var allTypes = metadata.GetAllTypes();
                var operationType = allTypes.Single(x => x.Name == operationName);
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
                    sb.Append("<table>");
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
                            var path = "/" + PathUtils.CombinePaths(EndpointHost.Config.ServiceStackHandlerFactoryPath, route.Path);

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

                var apiMembers = operationType.GetApiMembers();
                if (apiMembers.Count > 0)
                {
                    sb.Append("<table><caption>Parameters:</caption>");
                    sb.Append("<thead><tr>");
                    sb.Append("<th>Name</th>");
                    sb.Append("<th>Parameter</th>");
                    sb.Append("<th>Data Type</th>");
                    sb.Append("<th>Required</th>");
                    sb.Append("<th>Description</th>");
                    sb.Append("</tr></thead>");

                    sb.Append("<tbody>");
                    foreach (var apiMember in apiMembers)
                    {
                        sb.Append("<tr>");
                        sb.AppendFormat("<td>{0}</td>", ConvertToHtml(apiMember.Name));
                        sb.AppendFormat("<td>{0}</td>", apiMember.ParameterType);
                        sb.AppendFormat("<td>{0}</td>", apiMember.DataType);
                        sb.AppendFormat("<td>{0}</td>", apiMember.IsRequired ? "Yes" : "No");
                        sb.AppendFormat("<td>{0}</td>", apiMember.Description);
                        sb.Append("</tr>");
                    }
                    sb.Append("</tbody>");
                    sb.Append("</table>");
                }

                sb.Append(@"<div class=""call-info"">");
                sb.AppendFormat(@"<p>To override the Content-type in your clients HTTP <b>Accept</b> Header, append <b>?format={0}</b></p>", ContentFormat);
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

        private string ConvertToHtml(string text)
        {
            return text.Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\n", "<br />\n");
        }

        protected bool AssertAccess(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            if (!EndpointHost.Config.HasAccessToMetadata(httpReq, httpRes)) return false;

            if (operationName == null) return true; //For non-operation pages we don't need to check further permissions
            if (!EndpointHost.Config.EnableAccessRestrictions) return true;
            if (!EndpointHost.Config.MetadataPagesConfig.IsVisible(httpReq, Format, operationName))
            {
                EndpointHost.Config.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Service Not Available");
                return false;
            }

            return true;
        }

        protected abstract string CreateMessage(Type dtoType);

        protected virtual void RenderOperation(HtmlTextWriter writer, IHttpRequest httpReq, string operationName,
            string requestMessage, string responseMessage, string metadataHtml)
        {
            var operationControl = new OperationControl
            {
                HttpRequest = httpReq,
                MetadataConfig = EndpointHost.Config.ServiceEndpointsMetadataConfig,
                Title = EndpointHost.Config.ServiceName,
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

            operationControl.Render(writer);
        }

        protected abstract void RenderOperations(HtmlTextWriter writer, IHttpRequest httpReq, ServiceMetadata metadata);

        //protected virtual string CreateRestPaths(Type operationType)
        //{
        //    var paths = GetRestPaths(operationType);
        //    var restPaths = new StringBuilder();
        //    foreach (var restPath in paths)
        //    {
        //        var verbs = restPath.AllowsAllVerbs ? "All Verbs" : restPath.AllowedVerbs;
        //        restPaths.AppendLine(verbs + " " + restPath.Path);
        //    }
        //    return restPaths.ToString();
        //}

        //private static List<RestPath> GetRestPaths(Type operationType)
        //{
        //    var map = EndpointHost.ServiceManager.ServiceController.RestPathMap;
        //    var paths = new List<RestPath>();
        //    foreach (var key in map.Keys)
        //    {
        //        paths.AddRange(map[key].Where(x => x.RequestType == operationType));
        //    }
        //    return paths;
        //}
    }
}