using ServiceStack.Text;
using System.Text.Json.Nodes;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// Per-request context passed to ChatRouteHandler delegates — the C# equivalent of aiohttp's web.Request
/// as consumed by llms-py extension handlers.
/// </summary>
public class ChatRequestContext(ChatFeature feature, IRequest request, Dictionary<string, string> pathParams)
{
    public ChatFeature Feature => feature;
    public IRequest Request => request;
    public Dictionary<string, string> PathParams => pathParams;

    public string GetPathParam(string name) => pathParams.GetValueOrDefault(name)
        ?? throw new ArgumentException($"Missing path parameter '{name}'");

    public string? QueryString(string name) => request.QueryString[name];
    public bool HasQueryParam(string name) => request.QueryString.AllKeys.Contains(name);

    /// <summary>Authenticated username, or "default" when auth is not required. Null if unauthenticated.</summary>
    public string? UserName => feature.ChatAuth.GetUserName(request);

    public string AssertUserName() => feature.ChatAuth.AssertUserName(request);

    public async Task<JsonObject> GetJsonBodyAsync()
    {
        var json = await request.GetRawBodyAsync().ConfigAwait();
        return ChatJson.TryParseObject(json) ?? new JsonObject();
    }

    public async Task<JsonNode?> GetJsonNodeBodyAsync()
    {
        var json = await request.GetRawBodyAsync().ConfigAwait();
        return string.IsNullOrEmpty(json) ? null : ChatJson.Parse(json);
    }
}

/// <summary>Raw response with explicit status/content-type, mirroring aiohttp web.Response</summary>
public class ChatResult
{
    public int Status { get; set; } = 200;
    public string? ContentType { get; set; }
    public string? Text { get; set; }
    public byte[]? Body { get; set; }
    public Dictionary<string, string>? Headers { get; set; }

    public static ChatResult NotFound(string text = "404: Not Found") => new() { Status = 404, Text = text, ContentType = MimeTypes.PlainText };
    public static ChatResult Forbidden(string text = "403: Forbidden") => new() { Status = 403, Text = text, ContentType = MimeTypes.PlainText };
    public static ChatResult Unauthorized(JsonObject body) => Json(body, 401);
    public static ChatResult Json(JsonNode body, int status = 200) => new() {
        Status = status, Text = body.ToJsonString(ChatJson.Options), ContentType = MimeTypes.Json };
    public static ChatResult Html(string html) => new() { Text = html, ContentType = MimeTypes.HtmlUtf8 };
}

/// <summary>Serve a file from disk, mirroring aiohttp web.FileResponse</summary>
public class ChatFileResult(string filePath)
{
    public string FilePath => filePath;
    public string? ContentType { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}

/// <summary>A route result that owns the response stream (used by server-sent events).</summary>
public class ChatStreamResult(Func<IResponse, Task> write)
{
    public Func<IResponse, Task> Write { get; } = write;
}
