using System.IO;
using ServiceStack.Web;

namespace ServiceStack
{
    public class CompressResponseAttribute : ResponseFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object response)
        {
            var httpResult = response as IHttpResult;
            var dto = httpResult != null ? httpResult.Response : response;
            if (dto == null || dto is IPartialWriter || dto is IStreamWriter)
                return;

            var encoding = req.GetCompressionType();
            if (encoding == null) //Client doesn't support compression
                return;

            var responseBytes = dto as byte[];
            if (responseBytes == null)
            {
                var rawStr = dto as string;
                if (rawStr != null)
                    responseBytes = rawStr.ToUtf8Bytes();
                else
                {
                    var stream = dto as Stream;
                    if (stream != null)
                        responseBytes = stream.ReadFully();
                }
            }

            if (responseBytes != null || req.ResponseContentType.IsBinary())
            {
                if (responseBytes == null)
                    responseBytes = HostContext.ContentTypes.SerializeToBytes(req, dto);

                res.AddHeader(HttpHeaders.ContentEncoding, encoding);
                responseBytes = responseBytes.CompressBytes(encoding);
            }
            else
            {
                var serializedDto = req.SerializeToString(dto);
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

            res.WriteBytesToResponse(responseBytes, req.ResponseContentType);
        }
    }
}