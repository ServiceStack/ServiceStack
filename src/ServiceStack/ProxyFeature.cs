using ServiceStack.Web;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;

namespace ServiceStack
{
    public class ProxyFeature : IPlugin
    {
        private readonly Func<IHttpRequest, bool> matchingRequests;
        public readonly Func<IHttpRequest, string> ResolveUrl;
        public Action<IHttpRequest, HttpWebRequest> ProxyRequestFilter { get; set; }
        public Action<IHttpResponse, HttpWebResponse> ProxyResponseFilter { get; set; }
        public Func<IHttpResponse, Stream, Stream> TransformBody { get; set; }

        public ProxyFeature(
            Func<IHttpRequest, bool> matchingRequests,
            Func<IHttpRequest, string> resolveUrl)
        {
            this.matchingRequests = matchingRequests ?? throw new ArgumentNullException(nameof(matchingRequests));
            this.ResolveUrl = resolveUrl ?? throw new ArgumentNullException(nameof(resolveUrl));
        }

        public void Register(IAppHost appHost)
        {
            appHost.Config.SkipFormDataInCreatingRequest = true;

            appHost.RawHttpHandlers.Add(req => matchingRequests(req)
                ? new ProxyFeatureHandler
                {
                    ResolveUrl = ResolveUrl,
                    ProxyRequestFilter = ProxyRequestFilter,
                    ProxyResponseFilter = ProxyResponseFilter,
                    TransformBody = TransformBody,
                }
                : null);
        }
    }

    public class ProxyFeatureHandler : HttpAsyncTaskHandler
    {
        public override bool RunAsAsync() => true;

        public Func<IHttpRequest, string> ResolveUrl { get; set; }
        public Action<IHttpRequest, HttpWebRequest> ProxyRequestFilter { get; set; }
        public Action<IHttpResponse, HttpWebResponse> ProxyResponseFilter { get; set; }
        public Func<IHttpResponse, Stream, Stream> TransformBody { get; set; }

        public override Task ProcessRequestAsync(IRequest req, IResponse response, string operationName)
        {
            var httpReq = (IHttpRequest)req;
            var proxyUrl = ResolveUrl(httpReq);
            try
            {
                return ProxyRequestAsync(httpReq, proxyUrl);
            }
            catch (Exception ex)
            {
                return req.Response.WriteErrorBody(ex);
            }
        }

        public virtual async Task ProxyRequestAsync(IHttpRequest httpReq, string url)
        {
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            webReq.Method = httpReq.Verb;
            webReq.ContentType = httpReq.ContentType;
            webReq.Accept = httpReq.Accept;

            PclExport.Instance.SetUserAgent(webReq, httpReq.UserAgent);

#if NET45
            webReq.Referer = httpReq.UrlReferrer?.ToString();
            webReq.ServicePoint.Expect100Continue = false;

            var date = httpReq.GetDate();
            if (date != null)
                webReq.Date = date.Value;

            var ifModifiedSince = httpReq.GetIfModifiedSince();
            if (ifModifiedSince != null)
                webReq.IfModifiedSince = ifModifiedSince.Value;
#endif

            foreach (var header in httpReq.Headers.AllKeys)
            {
                if (HttpHeaders.RestrictedHeaders.Contains(header))
                    continue;

                webReq.Headers[header] = httpReq.Headers[header];
            }

            ProxyRequestFilter?.Invoke(httpReq, webReq);

            if (httpReq.ContentLength > 0)
            {
                using (var reqStream = await webReq.GetRequestStreamAsync())
                {
                    await httpReq.InputStream.CopyToAsync(reqStream);
                }
            }
            var res = (IHttpResponse)httpReq.Response;
            try
            {
                using (var webRes = (HttpWebResponse)await webReq.GetResponseAsync())
                {
                    await CopyToResponse(res, webRes);
                }
            }
            catch (WebException webEx)
            {
                var status = webEx.GetStatus();
                using (var errorResponse = (HttpWebResponse)webEx.Response)
                {
                    await CopyToResponse(res, errorResponse);
                }
            }
        }

        public virtual async Task CopyToResponse(IHttpResponse res, HttpWebResponse webRes)
        {
            res.StatusCode = (int) webRes.StatusCode;
            res.StatusDescription = webRes.StatusDescription;
            res.ContentType = webRes.ContentType;
            res.SetContentLength(webRes.ContentLength);

            foreach (var header in webRes.Headers.AllKeys)
            {
                var value = webRes.Headers[header];
                res.AddHeader(header, value);
            }

            ProxyResponseFilter?.Invoke(res, webRes);

            var responseStream = webRes.GetResponseStream();
            if (responseStream != null)
            {
                if (TransformBody != null)
                    responseStream = TransformBody(res, responseStream);

                using (responseStream)
                {
                    await responseStream.CopyToAsync(res.OutputStream);
                }
            }

            res.EndHttpHandlerRequest(skipHeaders: true);
        }
    }
}