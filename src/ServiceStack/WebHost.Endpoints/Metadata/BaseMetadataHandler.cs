using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using ServiceStack.Common.Extensions;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Support.Metadata.Controls;

namespace ServiceStack.WebHost.Endpoints.Metadata
{
    using System.Collections.Generic;
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

        protected virtual void ProcessOperations(HtmlTextWriter writer, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            var operationName = httpReq.QueryString["op"];

            if (!AssertAccess(httpReq, httpRes, operationName)) return;

            var metadata = EndpointHost.Metadata;
            if (operationName != null)
            {
                var allTypes = metadata.GetAllTypes();
                var operationType = allTypes.Single(x => x.Name == operationName);
                var requestMessage = CreateMessage(operationType);
                var restPaths = CreateRestPaths(operationType);
                string responseMessage = null;

                var responseType = metadata.GetResponseTypeByRequest(operationType);
                if (responseType != null)
                {
                    responseMessage = CreateMessage(responseType);
                }

                var description = operationType.GetDescription();
                if (!description.IsNullOrEmpty())
                {
                    description = "<div id='desc'>"
                        + "<p>" + description
                            .Replace("<", "&lt;")
                            .Replace(">", "&gt;")
                            .Replace("\n", "<br />\n")
                        + "</p>"
                        + "</div>";
                }


                RenderOperation(writer, httpReq, operationName, requestMessage, responseMessage, restPaths, description);
                return;
            }

            RenderOperations(writer, httpReq, metadata);
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
            string requestMessage, string responseMessage, string restPaths, string descriptionHtml)
        {
            var operationControl = new OperationControl {
                HttpRequest = httpReq,
                MetadataConfig = EndpointHost.Config.ServiceEndpointsMetadataConfig,
                Title = EndpointHost.Config.ServiceName,
                Format = this.Format,
                OperationName = operationName,
                HostName = httpReq.GetUrlHostName(),
                RequestMessage = requestMessage,
                ResponseMessage = responseMessage,
                RestPaths = restPaths,
                DescriptionHtml = descriptionHtml,
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

        protected virtual string CreateRestPaths(Type operationType)
        {
            var map = EndpointHost.ServiceManager.ServiceController.RestPathMap;
            var paths = new List<RestPath>();
            foreach (var key in map.Keys)
            {
                paths.AddRange(map[key].Where(x => x.RequestType == operationType));
            }
            var restPaths = new StringBuilder();
            foreach (var restPath in paths)
            {
                var verbs = restPath.AllowsAllVerbs ? "All Verbs" : restPath.AllowedVerbs;
                restPaths.AppendLine(verbs + " " + restPath.Path);
            }
            return restPaths.ToString();
        }
    }
}