using ServiceStack.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Host.Handlers;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// Dispatches all Chat UI requests under ChatFeature.RoutePrefix through the RouteRegistry —
/// the C# equivalent of llms-py's aiohttp server loop, including managed_handler error wrapping,
/// on_request user setup, and the SPA index.html fallback.
/// </summary>
public class ChatHttpHandler(ChatFeature feature, string pathInfo) : HttpAsyncTaskHandler
{
    public override bool RunAsAsync() => true;

    public override async Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
    {
        var path = pathInfo;
        if (feature.RoutePrefix.Length > 0)
        {
            path = path.Length > feature.RoutePrefix.Length
                ? path[feature.RoutePrefix.Length..]
                : "/";
        }
        if (!path.StartsWith('/'))
            path = "/" + path;

        try
        {
            await feature.OnRequestAsync(req).ConfigAwait();

            var match = feature.Routes.Match(req.Verb, path);
            if (match != null)
            {
                var ctx = new ChatRequestContext(feature, req, match.Value.Params);
                object? result;
                try
                {
                    result = await match.Value.Route.Handler(ctx).ConfigAwait();
                }
                catch (UnauthorizedAccessException)
                {
                    await WriteResultAsync(res, ChatResult.Unauthorized(feature.ErrorAuthRequired())).ConfigAwait();
                    return;
                }
                catch (Exception e)
                {
                    feature.Log.LogError(e, "Error handling {Method} {Path}: {Message}", req.Verb, path, e.Message);
                    await WriteResultAsync(res, ChatResult.Json(ChatJson.ToErrorResponse(e), 500)).ConfigAwait();
                    return;
                }
                await WriteResultAsync(res, result).ConfigAwait();
                return;
            }

            // Unmatched extension APIs get a JSON 404 rather than the SPA fallback: serving
            // index.html for a missing API route surfaces in the UI as "Unexpected token '<'"
            // instead of a real error (e.g. when an extension isn't installed).
            if (path.StartsWith("/ext/", StringComparison.OrdinalIgnoreCase))
            {
                await WriteResultAsync(res, ChatResult.Json(
                    ChatJson.CreateErrorResponse($"{req.Verb} {path} not found", "NotFound"), 404)).ConfigAwait();
                return;
            }

            // SPA fallback: any unmatched route serves index.html (Python: add_route("*", "/{tail:.*}", index_handler))
            var indexResult = await feature.IndexHandlerAsync(req).ConfigAwait();
            await WriteResultAsync(res, indexResult).ConfigAwait();
        }
        finally
        {
            await res.EndRequestAsync(skipHeaders: true).ConfigAwait();
        }
    }

    public static async Task WriteResultAsync(IResponse res, object? result)
    {
        switch (result)
        {
            case null:
                res.StatusCode = 204;
                break;

            case ChatResult raw:
                res.StatusCode = raw.Status;
                res.ContentType = raw.ContentType ?? MimeTypes.Json;
                if (raw.Headers != null)
                {
                    foreach (var entry in raw.Headers)
                        res.AddHeader(entry.Key, entry.Value);
                }
                if (raw.Body != null)
                {
                    await res.OutputStream.WriteAsync(raw.Body).ConfigAwait();
                }
                else if (raw.Text != null)
                {
                    await res.WriteAsync(raw.Text).ConfigAwait();
                }
                break;

            case ChatFileResult file:
                res.ContentType = file.ContentType ?? MimeTypes.GetMimeType(file.FilePath);
                if (file.Headers != null)
                {
                    foreach (var entry in file.Headers)
                        res.AddHeader(entry.Key, entry.Value);
                }
                await using (var fs = File.OpenRead(file.FilePath))
                {
                    await fs.CopyToAsync(res.OutputStream).ConfigAwait();
                }
                break;

            case JsonNode node:
                res.ContentType = MimeTypes.Json;
                await res.WriteAsync(node.ToJsonString(ChatJson.Options)).ConfigAwait();
                break;

            case string text:
                res.ContentType = MimeTypes.PlainText;
                await res.WriteAsync(text).ConfigAwait();
                break;

            default:
                res.ContentType = MimeTypes.Json;
                await res.WriteAsync(ChatJson.Serialize(result)).ConfigAwait();
                break;
        }
    }
}
