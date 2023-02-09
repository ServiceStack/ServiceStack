using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Metadata
{
    public class OperationControl
    {
        public ServiceEndpointsMetadataConfig MetadataConfig { get; set; }

        public Format Format
        {
            set
            {
                this.ContentType = value.ToContentType();
                this.ContentFormat = ServiceStack.ContentFormat.GetContentFormat(value);
            }
        }

        public IRequest HttpRequest { get; set; }
        public string ContentType { get; set; }
        public string ContentFormat { get; set; }

        public string Title { get; set; }
        public string OperationName { get; set; }
        public string HostName { get; set; }
        public string RequestMessage { get; set; }
        public string ResponseMessage { get; set; }

        public string MetadataHtml { get; set; }

        public Operation Operation { get; set; }

        public virtual string RequestUri
        {
            get
            {
                if (Operation != null && Operation.Routes.Count > 0)
                {
                    var postRoute = Operation.Routes.FirstOrDefault(x => x.AllowsAllVerbs || x.Verbs.Contains(HttpMethods.Post));
                    var path = postRoute != null 
                        ? postRoute.Path 
                        : Operation.Routes[0].Path;
                    return HostContext.Config.HandlerFactoryPath != null
                        ? "/" + HostContext.Config.HandlerFactoryPath.CombineWith(path)
                        : path;
                }

                var endpointConfig = MetadataConfig.GetEndpointConfig(ServiceStack.ContentFormat.GetContentFormat(ContentType));
                var endpointPath = ResponseMessage != null
                    ? endpointConfig.SyncReplyUri : endpointConfig.AsyncOneWayUri;
                return $"{endpointPath}/{OperationName}";
            }
        }

        public virtual Task RenderAsync(Stream output)
        {
            var baseUrl = HttpRequest.ResolveAbsoluteUrl("~/");
            var sbTags = StringBuilderCache.Allocate();
            Operation?.Tags.Each(x => sbTags.Append($"<span><b>{x}</b></span>"));
            var tagsHtml = sbTags.Length > 0
                ? "<div class=\"tags\">" + StringBuilderCache.ReturnAndFree(sbTags) + "</div>"
                : "";
            
            var renderedTemplate = Templates.HtmlTemplates.Format(Templates.HtmlTemplates.GetOperationControlTemplate(),
                Title,
                baseUrl.CombineWith(MetadataConfig.DefaultMetadataUri),
                ContentFormat.ToUpper(),
                OperationName,
                GetHttpRequestTemplate(),
                ResponseTemplate,
                MetadataHtml,
                tagsHtml);

            return output.WriteAsync(renderedTemplate);
        }

        public virtual string GetHttpRequestTemplate()
        {
            if (Operation == null || Operation.Routes.Count == 0)
                return HttpRequestTemplateWithBody(HttpMethods.Post);

            var allowedVerbs = new HashSet<string>();
            foreach (var route in Operation.Routes)
            {
                if (route.AllowsAllVerbs)
                {
                    allowedVerbs.Add(HttpMethods.Post);
                    break;
                }
                foreach (var routeVerb in route.Verbs)
                {
                    allowedVerbs.Add(routeVerb);
                }
            }

            if (allowedVerbs.Contains(HttpMethods.Post))
                return HttpRequestTemplateWithBody(HttpMethods.Post);
            if (allowedVerbs.Contains(HttpMethods.Put))
                return HttpRequestTemplateWithBody(HttpMethods.Put);
            if (allowedVerbs.Contains(HttpMethods.Patch))
                return HttpRequestTemplateWithBody(HttpMethods.Patch);

            if (allowedVerbs.Contains(HttpMethods.Get))
                return HttpRequestTemplateWithoutBody(HttpMethods.Get);
            if (allowedVerbs.Contains(HttpMethods.Delete))
                return HttpRequestTemplateWithoutBody(HttpMethods.Delete);

            return HttpRequestTemplateWithBody(HttpMethods.Post);
        }

        public virtual string HttpRequestTemplateWithBody(string httpMethod) =>
            httpMethod + $@" {RequestUri} HTTP/1.1 
Host: {HostName} 
Accept: {ContentType}
Content-Type: {ContentType}
Content-Length: <span class=""value"">length</span>

{PclExportClient.Instance.HtmlEncode(RequestMessage)}";

        public virtual string HttpRequestTemplateWithoutBody(string httpMethod) =>
httpMethod + $@" {RequestUri} HTTP/1.1 
Host: {HostName} 
Accept: {ContentType}";

        public virtual string ResponseTemplate
        {
            get
            {
                var httpResponse = this.HttpResponseTemplate;
                return string.IsNullOrEmpty(httpResponse) ? null :
$@"
<div class=""response"">
<pre>
{httpResponse}
</pre>
</div>
";
            }
        }

        public virtual string HttpResponseTemplate
        {
            get
            {
                if (string.IsNullOrEmpty(ResponseMessage)) return null;
                return
$@"HTTP/1.1 200 OK
Content-Type: {ContentType}
Content-Length: length

{PclExportClient.Instance.HtmlEncode(ResponseMessage)}";
            }
        }
    }
}