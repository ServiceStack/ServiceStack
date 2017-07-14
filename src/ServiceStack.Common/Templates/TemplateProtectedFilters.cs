using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace ServiceStack.Templates
{
    public class TemplateProtectedFilters : TemplateFilter
    {
        public async Task includeFile(TemplateScopeContext scope, string virtualPath)
        {
            var file = scope.Context.VirtualFiles.GetFile(virtualPath);
            if (file == null)
                throw new FileNotFoundException($"includeFile '{virtualPath}' in page '{scope.Page.File.VirtualPath}' was not found");

            using (var reader = file.OpenRead())
            {
                await reader.CopyToAsync(scope.OutputStream);
            }
        }

        public Task includeUrl(TemplateScopeContext scope, string url) => includeUrl(scope, url, null);
        public async Task includeUrl(TemplateScopeContext scope, string url, object options)
        {
            var scopedParams = scope.AssertOptions(nameof(includeUrl), options);

            var webReq = (HttpWebRequest)WebRequest.Create(url);
            if (scopedParams.TryRemove("method", out object method))
                webReq.Method = (string)method;
            if (scopedParams.TryRemove("contentType", out object contentType))
                webReq.ContentType = (string)contentType;            
            if (scopedParams.TryRemove("accept", out object accept))
                webReq.Accept = (string)accept;            
            if (scopedParams.TryRemove("userAgent", out object userAgent))
                PclExport.Instance.SetUserAgent(webReq, (string)userAgent);

            if (scopedParams.TryRemove("data", out object data))
            {
                if (webReq.Method == null)
                    webReq.Method = HttpMethods.Post;
                    
                if (webReq.ContentType == null)
                    webReq.ContentType = MimeTypes.FormUrlEncoded;

                var body = ConvertDataToString(data, webReq.ContentType);
                using (var stream = await webReq.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(body);
                }
            }

            using (var webRes = await webReq.GetResponseAsync())
            using (var stream = webRes.GetResponseStream())
            {
                await stream.CopyToAsync(scope.OutputStream);
            }
        }

        private static string ConvertDataToString(object data, string contentType)
        {
            if (data is string s)
                return s;
            switch (contentType)
            {
                case MimeTypes.Json:
                    return data.ToJson();
                case MimeTypes.Csv:
                    return data.ToCsv();
                case MimeTypes.Jsv:
                    return data.ToJsv();
                case MimeTypes.Xml:
                    return data.ToXml();
                case MimeTypes.FormUrlEncoded:
                    WriteComplexTypeDelegate holdQsStrategy = QueryStringStrategy.FormUrlEncoded;
                    QueryStringSerializer.ComplexTypeStrategy = QueryStringStrategy.FormUrlEncoded;
                    var urlEncodedBody = QueryStringSerializer.SerializeToString(data);
                    QueryStringSerializer.ComplexTypeStrategy = holdQsStrategy;
                    return urlEncodedBody;
            }

            throw new NotSupportedException($"Can not serialize to unknown Content-Type '{contentType}'");
        }
    }
}