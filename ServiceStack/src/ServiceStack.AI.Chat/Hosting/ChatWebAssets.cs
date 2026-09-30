using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>Revalidation and negotiated compression for the shared unbundled UI and buffered APIs.</summary>
internal static class ChatWebAssets
{
    static bool IsText(string? contentType)
    {
        var mime = contentType?.Split(';')[0].Trim();
        return mime?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true
            || mime is "application/javascript" or "application/json" or "application/xml" or "image/svg+xml";
    }

    public static ChatResult AssetResult(IRequest request, byte[] body, string contentType)
    {
        var etag = "W/\"" + Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant() + "\"";
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [HttpHeaders.ETag] = etag,
            [HttpHeaders.CacheControl] = "no-cache",
            [HttpHeaders.Vary] = "Accept-Encoding",
        };
        var matches = request.Headers[HttpHeaders.IfNoneMatch]?.Split(',') ?? [];
        var notModified = matches.Any(value => value.Trim() == "*" || value.Trim().Replace("W/", "") == etag[2..]);
        return new ChatResult { Status = notModified ? 304 : 200, Body = notModified ? null : body,
            ContentType = contentType, Headers = headers };
    }

    public static ChatResult PrepareResponse(IRequest? request, ChatResult result)
    {
        if (result.Status is 204 or 304 || result.Body == null && result.Text == null)
            return result;
        var body = result.Body ?? Encoding.UTF8.GetBytes(result.Text!);
        var headers = result.Headers != null
            ? new Dictionary<string, string>(result.Headers, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (body.Length >= 1024 && IsText(result.ContentType) && !headers.ContainsKey(HttpHeaders.ContentEncoding))
        {
            var vary = headers.GetValueOrDefault(HttpHeaders.Vary, "");
            if (!vary.Split(',').Any(x => x.Trim().Equals("Accept-Encoding", StringComparison.OrdinalIgnoreCase)))
                headers[HttpHeaders.Vary] = string.IsNullOrEmpty(vary) ? "Accept-Encoding" : vary + ", Accept-Encoding";
            var encoding = CompressionEncoding(request?.Headers[HttpHeaders.AcceptEncoding]);
            if (encoding != null)
            {
                using var output = new MemoryStream();
                using (Stream compressor = encoding == "gzip"
                    ? new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true)
                    : new ZLibStream(output, CompressionLevel.Fastest, leaveOpen: true))
                    compressor.Write(body);
                body = output.ToArray();
                headers[HttpHeaders.ContentEncoding] = encoding;
            }
        }
        headers[HttpHeaders.ContentLength] = body.Length.ToString(CultureInfo.InvariantCulture);
        return new ChatResult { Status = result.Status, ContentType = result.ContentType, Body = body, Headers = headers };
    }

    static string? CompressionEncoding(string? acceptEncoding)
    {
        var qualities = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in (acceptEncoding ?? "").Split(','))
        {
            var parts = entry.Split(';');
            var name = parts[0].Trim();
            var quality = 1d;
            foreach (var parameter in parts.Skip(1))
            {
                var pair = parameter.Trim().Split('=', 2);
                if (pair.Length == 2 && pair[0].Equals("q", StringComparison.OrdinalIgnoreCase))
                    quality = double.TryParse(pair[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var q)
                        && q >= 0 && q <= 1 ? q : 0;
            }
            qualities[name] = quality;
        }
        var wildcard = qualities.GetValueOrDefault("*", 0);
        var gzip = qualities.GetValueOrDefault("gzip", wildcard);
        var deflate = qualities.GetValueOrDefault("deflate", wildcard);
        return gzip > 0 && gzip >= deflate ? "gzip" : deflate > 0 ? "deflate" : null;
    }
}
