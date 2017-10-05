using ServiceStack.Web;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ServiceStack
{
    public class CompressResponseAttribute : ResponseFilterAsyncAttribute
    {
        public override async Task ExecuteAsync(IRequest req, IResponse res, object response)
        {
            if (response is Exception)
                return;

            var httpResult = response as IHttpResult;
            var src = httpResult != null ? httpResult.Response : response;
            if (src == null)
            {
                var concreteResult = response as HttpResult;
                src = concreteResult?.ResponseStream 
                    ?? (object) concreteResult?.ResponseText;
            }

            if (src == null || src is IPartialWriter || src is IPartialWriterAsync || src is IStreamWriter || src is IStreamWriterAsync)
                return;

            var encoding = req.GetCompressionType();
            if (encoding == null) //Client doesn't support compression
                return;

            var responseBytes = src as byte[];
            if (responseBytes == null)
            {
                var rawStr = src as string;
                if (rawStr != null)
                    responseBytes = rawStr.ToUtf8Bytes();
                else
                {
                    var stream = src as Stream;
                    if (stream != null)
                        responseBytes = stream.ReadFully();
                }
            }

            if (responseBytes != null || req.ResponseContentType.IsBinary())
            {
                if (responseBytes == null)
                    responseBytes = HostContext.ContentTypes.SerializeToBytes(req, src);

                res.AddHeader(HttpHeaders.ContentEncoding, encoding);
                responseBytes = responseBytes.CompressBytes(encoding);
            }
            else
            {
                var serializedDto = req.SerializeToString(src);
                if (req.ResponseContentType.MatchesContentType(MimeTypes.Json))
                {
                    var jsonp = req.GetJsonpCallback();
                    if (jsonp != null)
                        serializedDto = jsonp + "(" + serializedDto + ")";
                }

                responseBytes = serializedDto.ToUtf8Bytes();

                res.AddHeader(HttpHeaders.ContentEncoding, encoding);
                responseBytes = responseBytes.CompressBytes(encoding);
            }

            if (httpResult != null)
            {
                foreach (var header in httpResult.Headers)
                {
                    res.AddHeader(header.Key, header.Value);
                }
            }

            await res.WriteBytesToResponse(responseBytes, req.ResponseContentType);
            using (response as IDisposable) {}
        }
    }
}