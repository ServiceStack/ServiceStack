using System.Text;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>An error response from the Gemini API, retaining the HTTP status (404s are expected + ignorable)</summary>
public class GeminiApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode => statusCode;
}

/// <summary>
/// REST client for Gemini's File Search Store API (https://ai.google.dev/api/file-search) —
/// the C# equivalent of the google-genai SDK calls llms-py's gemini extension makes
/// (client.file_search_stores.*). Authenticates with ?key= like <see cref="GoogleProvider"/>.
/// </summary>
public class GeminiClient(IHttpClientFactory httpClientFactory, string apiKey)
{
    public string Api { get; set; } = "https://generativelanguage.googleapis.com";

    /// <summary>Uploads of large documents can take a while, so this isn't the chat client timeout</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>How long to wait between polls of a running upload operation</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = Timeout;
        return client;
    }

    string Url(string path, string? query = null, bool upload = false) =>
        $"{Api}/{(upload ? "upload/" : "")}v1beta/{path}?key={apiKey}" + (query != null ? $"&{query}" : "");

    // ── File search stores ──

    public Task<JsonObject> CreateFileSearchStoreAsync(string displayName, CancellationToken token = default) =>
        SendAsync(HttpMethod.Post, Url("fileSearchStores"),
            new JsonObject { ["displayName"] = displayName }, token);

    public Task<JsonObject> GetFileSearchStoreAsync(string name, CancellationToken token = default) =>
        SendAsync(HttpMethod.Get, Url(name), null, token);

    /// <summary>force=true also deletes the store's documents</summary>
    public Task<JsonObject> DeleteFileSearchStoreAsync(string name, CancellationToken token = default) =>
        SendAsync(HttpMethod.Delete, Url(name, "force=true"), null, token);

    // ── Documents ──

    /// <summary>Gemini rejects a documents.list pageSize outside 1-20 (default 10)</summary>
    public const int MaxDocumentsPageSize = 20;

    /// <summary>All documents in a store, following nextPageToken (the SDK's documents.list() pager)</summary>
    public async Task<List<JsonObject>> ListDocumentsAsync(string parent, CancellationToken token = default)
    {
        var ret = new List<JsonObject>();
        string? pageToken = null;
        do
        {
            var query = $"pageSize={MaxDocumentsPageSize}" + (pageToken != null ? $"&pageToken={Uri.EscapeDataString(pageToken)}" : "");
            var page = await SendAsync(HttpMethod.Get, Url($"{parent}/documents", query), null, token).ConfigAwait();
            foreach (var node in page.GetArray("documents") ?? [])
            {
                if (node is JsonObject doc)
                    ret.Add(doc);
            }
            pageToken = page.GetString("nextPageToken");
        } while (!string.IsNullOrEmpty(pageToken));
        return ret;
    }

    public Task<JsonObject> GetDocumentAsync(string name, CancellationToken token = default) =>
        SendAsync(HttpMethod.Get, Url(name), null, token);

    public Task<JsonObject> DeleteDocumentAsync(string name, CancellationToken token = default) =>
        SendAsync(HttpMethod.Delete, Url(name, "force=true"), null, token);

    // ── Uploads ──

    /// <summary>
    /// Upload a file into a file search store using Gemini's 2-request resumable protocol, returning
    /// the long-running Operation (poll it with <see cref="WaitForOperationAsync"/>).
    /// </summary>
    public async Task<JsonObject> UploadToFileSearchStoreAsync(string storeName, string filePath,
        JsonObject config, string contentType, CancellationToken token = default)
    {
        var content = await File.ReadAllBytesAsync(filePath, token).ConfigAwait();
        using var client = CreateClient();

        // 1. start the resumable session, which carries the document metadata
        var startReq = new HttpRequestMessage(HttpMethod.Post,
            Url($"{storeName}:uploadToFileSearchStore", upload: true));
        startReq.Headers.TryAddWithoutValidation("X-Goog-Upload-Protocol", "resumable");
        startReq.Headers.TryAddWithoutValidation("X-Goog-Upload-Command", "start");
        startReq.Headers.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Length", content.Length.ToString());
        startReq.Headers.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Type", contentType);
        startReq.Content = new StringContent(config.ToJsonString(ChatJson.Options), Encoding.UTF8, MimeTypes.Json);

        using var startRes = await client.SendAsync(startReq, token).ConfigAwait();
        await AssertSuccessAsync(startRes).ConfigAwait();

        var uploadUrl = startRes.Headers.TryGetValues("x-goog-upload-url", out var values)
            ? values.FirstOrDefault()
            : null;
        if (string.IsNullOrEmpty(uploadUrl))
            throw new Exception("Gemini did not return an upload url (missing x-goog-upload-url header)");

        // 2. send the bytes + finalize, which returns the Operation
        var uploadReq = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        uploadReq.Headers.TryAddWithoutValidation("X-Goog-Upload-Offset", "0");
        uploadReq.Headers.TryAddWithoutValidation("X-Goog-Upload-Command", "upload, finalize");
        uploadReq.Content = new ByteArrayContent(content);

        using var uploadRes = await client.SendAsync(uploadReq, token).ConfigAwait();
        return await ReadJsonAsync(uploadRes).ConfigAwait();
    }

    public Task<JsonObject> GetOperationAsync(string name, CancellationToken token = default) =>
        SendAsync(HttpMethod.Get, Url(name), null, token);

    /// <summary>Poll an Operation until it reports done, then return it (errors are left for the caller)</summary>
    public async Task<JsonObject> WaitForOperationAsync(JsonObject operation, CancellationToken token = default)
    {
        while (!operation.GetBool("done"))
        {
            var name = operation.GetString("name")
                ?? throw new Exception("Gemini upload operation has no name");
            await Task.Delay(PollInterval, token).ConfigAwait();
            operation = await GetOperationAsync(name, token).ConfigAwait();
        }
        return operation;
    }

    // ── HTTP ──

    async Task<JsonObject> SendAsync(HttpMethod method, string url, JsonObject? body, CancellationToken token)
    {
        using var client = CreateClient();
        var httpReq = new HttpRequestMessage(method, url);
        if (body != null)
            httpReq.Content = new StringContent(body.ToJsonString(ChatJson.Options), Encoding.UTF8, MimeTypes.Json);
        using var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();
        return await ReadJsonAsync(httpRes).ConfigAwait();
    }

    static async Task<JsonObject> ReadJsonAsync(HttpResponseMessage httpRes)
    {
        var text = await httpRes.Content.ReadAsStringAsync().ConfigAwait();
        AssertSuccess(httpRes, text);
        // DELETE returns an empty body
        return ChatJson.TryParseObject(text) ?? new JsonObject();
    }

    static async Task AssertSuccessAsync(HttpResponseMessage httpRes)
    {
        if ((int)httpRes.StatusCode < 400)
            return;
        AssertSuccess(httpRes, await httpRes.Content.ReadAsStringAsync().ConfigAwait());
    }

    static void AssertSuccess(HttpResponseMessage httpRes, string text)
    {
        if ((int)httpRes.StatusCode < 400)
            return;
        var error = ChatJson.TryParseObject(text).GetObject("error");
        var message = error.GetString("message")
            ?? $"Gemini API failed with {(int)httpRes.StatusCode}: {text.SafeSubstring(0, 500)}";
        throw new GeminiApiException((int)httpRes.StatusCode, message);
    }
}
