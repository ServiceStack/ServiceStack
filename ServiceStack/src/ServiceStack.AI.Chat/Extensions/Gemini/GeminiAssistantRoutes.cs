using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

public partial class GeminiExtension
{
    readonly GeminiAssistantMinuteLimiter assistantLimiter = new();

    void InstallAssistantRoutes(ExtensionContext ctx)
    {
        ctx.AddGet("filestores/{id}/assistants", ListAssistantsAsync);
        ctx.AddPost("filestores/{id}/assistants", CreateAssistantAsync);
        ctx.AddGet("assistants/{id}", GetAssistantAsync);
        ctx.AddPut("assistants/{id}", UpdateAssistantAsync);
        ctx.AddGet("assistants/{id}/delete-summary", AssistantDeleteSummaryAsync);
        ctx.AddDelete("assistants/{id}", ArchiveAssistantAsync);
        ctx.AddPost("assistants/{id}/restore", RestoreAssistantAsync);
        ctx.AddDelete("assistants/{id}/permanent", DeleteAssistantAsync);
        ctx.AddGet("assistants/{id}/conversations", AssistantConversationsAsync);
        ctx.AddGet("assistants/{id}/conversations/{conversationId}", AssistantConversationAsync);
        ctx.AddGet("public/assistants/widget.js", PublicAssistantScriptAsync, allowAnon: true);
        ctx.AddPost("public/assistants/{publicId}/chat", PublicAssistantChatAsync, allowAnon: true);
    }

    string AssistantBaseUrl(ChatRequestContext req) =>
        req.Request.GetBaseUrl().CombineWith(req.Feature.RoutePrefix).TrimEnd('/');

    JsonObject AssistantDto(ChatAssistant assistant, ChatRequestContext req, long? conversationCount = null)
    {
        var config = GeminiAssistants.NormalizeConfig(ChatJson.TryParseObject(assistant.Config));
        var src = AssistantBaseUrl(req).CombineWith($"ext/gemini/public/assistants/widget.js?g={assistant.PublicId}");
        var dto = assistant.ToDto();
        dto["config"] = config;
        dto["published"] = assistant.PublishedAt != null && assistant.Enabled;
        dto["scriptUrl"] = src;
        dto["embedCode"] = $"<script src=\"{src}\" async></script>";
        if (conversationCount != null) dto["conversationCount"] = conversationCount.Value;
        return dto;
    }

    Task<object?> ListAssistantsAsync(ChatRequestContext req)
    {
        var user = UserOf(req);
        var filestoreId = IdOf(req);
        if (db.GetFilestore(filestoreId, user) == null) return Task.FromResult<object?>(ChatResult.NotFound("File Store does not exist"));
        var rows = db.QueryAssistants(filestoreId, user, includeArchived: true);
        var counts = db.AssistantConversationCounts(rows.Select(x => x.Id));
        return Task.FromResult<object?>(new JsonArray(rows
            .Select(x => (JsonNode)AssistantDto(x, req, counts.GetValueOrDefault(x.Id))).ToArray()));
    }

    async Task<object?> CreateAssistantAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var user = UserOf(req);
        var filestoreId = IdOf(req);
        if (db.GetFilestore(filestoreId, user) == null) return ChatResult.NotFound("File Store does not exist");
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var name = (body.GetString("name") ?? "").Trim().SafeSubstring(0, 200);
        if (name.Length == 0) return Error("Name is required", "ValidationError", 400);
        if (db.AssistantNameExists(filestoreId, name, user))
            return Error($"An Assistant named '{name}' already exists", "AlreadyExists", 409);
        JsonObject config;
        try { config = GeminiAssistants.ValidateConfig(body.GetObject("config")); }
        catch (ArgumentException e) { return Error(e.Message, "ValidationError", 400); }
        var now = DateTime.Now;
        var publish = body.GetBool("published");
        var assistant = new ChatAssistant
        {
            FilestoreId = filestoreId, User = user, CreatedAt = now, UpdatedAt = now, Name = name,
            PublicId = GeminiAssistants.NewPublicId(), Enabled = true,
            PublishedAt = publish ? now : null, Config = config.ToJsonString(ChatJson.Options),
        };
        assistant.Id = db.InsertAssistant(assistant);
        if (publish) PublishFilestore(filestoreId, user);
        return AssistantDto(assistant, req);
    }

    Task<object?> GetAssistantAsync(ChatRequestContext req)
    {
        var assistant = db.GetAssistant(IdOf(req), UserOf(req));
        return Task.FromResult<object?>(assistant == null ? ChatResult.NotFound("Assistant does not exist") : AssistantDto(assistant, req));
    }

    async Task<object?> UpdateAssistantAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var user = UserOf(req);
        var assistant = db.GetAssistant(IdOf(req), user);
        if (assistant == null) return ChatResult.NotFound("Assistant does not exist");
        if (!assistant.Enabled)
            return Error("Restore this Assistant before editing or publishing it", "AssistantArchived", 409);
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var name = (body.GetString("name") ?? assistant.Name ?? "").Trim().SafeSubstring(0, 200);
        if (name.Length == 0) return Error("Name is required", "ValidationError", 400);
        if (db.AssistantNameExists(assistant.FilestoreId, name, user, assistant.Id))
            return Error($"An Assistant named '{name}' already exists", "AlreadyExists", 409);
        JsonObject config;
        try
        {
            config = GeminiAssistants.ValidateConfig(body.GetObject("config") ?? ChatJson.TryParseObject(assistant.Config));
        }
        catch (ArgumentException e) { return Error(e.Message, "ValidationError", 400); }
        var published = body.TryGetPropertyValue("published", out _) ? body.GetBool("published") : assistant.PublishedAt != null;
        assistant.Name = name;
        assistant.Config = config.ToJsonString(ChatJson.Options);
        assistant.PublishedAt = published ? assistant.PublishedAt ?? DateTime.Now : null;
        if (body.GetBool("regeneratePublicId")) assistant.PublicId = GeminiAssistants.NewPublicId();
        db.UpdateAssistant(assistant);
        if (published) PublishFilestore(assistant.FilestoreId, user);
        return AssistantDto(assistant, req);
    }

    async Task<object?> AssistantDeleteSummaryAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var summary = db.AssistantDeleteSummary(IdOf(req), UserOf(req));
        return summary != null ? summary : ChatResult.NotFound("Assistant does not exist");
    }

    async Task<object?> ArchiveAssistantAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        return db.ArchiveAssistant(IdOf(req), UserOf(req))
            ? new JsonObject { ["archived"] = true, ["conversationsRetained"] = true }
            : ChatResult.NotFound("Assistant does not exist");
    }

    async Task<object?> RestoreAssistantAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        try
        {
            var assistant = db.RestoreAssistant(IdOf(req), UserOf(req));
            return assistant == null
                ? ChatResult.NotFound("Assistant does not exist")
                : AssistantDto(assistant, req);
        }
        catch (InvalidOperationException e)
        {
            return Error(e.Message, "AlreadyExists", 409);
        }
    }

    async Task<object?> DeleteAssistantAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var id = IdOf(req);
        var user = UserOf(req);
        var summary = db.AssistantDeleteSummary(id, user);
        if (summary == null) return ChatResult.NotFound("Assistant does not exist");
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var confirmation = body.GetString("confirm");
        if (confirmation != summary.GetString("name"))
            return Error($"Type \"{summary.GetString("name")}\" to confirm permanent deletion",
                "ConfirmationRequired", 400);
        try
        {
            var deleted = db.DeleteAssistant(id, user, confirmation);
            return deleted == null
                ? ChatResult.NotFound("Assistant does not exist")
                : new JsonObject { ["deleted"] = deleted };
        }
        catch (ArgumentException e)
        {
            return Error(e.Message, "ConfirmationRequired", 400);
        }
    }

    Task<object?> AssistantConversationsAsync(ChatRequestContext req)
    {
        var rows = db.QueryAssistantConversations(IdOf(req), UserOf(req), req.QueryString("take").ToInt(100));
        var counts = db.AssistantUserMessageCounts(rows.Select(x => x.Id));
        return Task.FromResult<object?>(new JsonArray(rows.Select(x =>
        {
            var dto = x.ToDto();
            dto["userMessageCount"] = counts.GetValueOrDefault(x.Id);
            return (JsonNode)dto;
        }).ToArray()));
    }

    Task<object?> AssistantConversationAsync(ChatRequestContext req)
    {
        var assistantId = IdOf(req);
        var conversationId = long.TryParse(req.GetPathParam("conversationId"), out var id) ? id : 0;
        var row = db.GetAssistantConversation(conversationId, assistantId, UserOf(req));
        if (row == null) return Task.FromResult<object?>(ChatResult.NotFound("Conversation does not exist"));
        var dto = row.ToDto();
        dto["messages"] = new JsonArray(db.QueryAssistantMessages(row.Id).Select(x => (JsonNode)x.ToDto()).ToArray());
        return Task.FromResult<object?>(dto);
    }

    void PublishFilestore(long filestoreId, string? user)
    {
        var store = db.GetFilestore(filestoreId, user);
        if (store == null) return;
        store.Visibility = "public";
        db.UpdateFilestore(store);
    }

    static ChatResult Error(string message, string code = "Error", int status = 500,
        Dictionary<string, string>? headers = null) => new()
    {
        Status = status, ContentType = MimeTypes.Json,
        Text = ChatJson.CreateErrorResponse(message, code).ToJsonString(ChatJson.Options), Headers = headers,
    };

    (ChatAssistant? Assistant, ChatFilestore? Store) PublicAssistant(string publicId)
    {
        var assistant = db.GetPublicAssistant(publicId);
        var store = assistant == null ? null : db.GetFilestore(assistant.FilestoreId, assistant.User);
        return store?.Visibility == "public" ? (assistant, store) : (null, null);
    }

    static string BundledMarkdownSource()
    {
        var source = HostContext.VirtualFileSources.GetFile("chat/ui/lib/marked.min.mjs")?.ReadAllText()
            ?? throw new FileNotFoundException("chat/ui/lib/marked.min.mjs is not embedded");
        source = Regex.Replace(source, @"(?m)^//# sourceMappingURL=.*$", "");
        var export = Regex.Match(source, @"\bexport\{([^{}]*)\};");
        if (!export.Success) throw new InvalidDataException("Bundled Marked module has no export declaration");
        var binding = Regex.Match(export.Groups[1].Value,
            @"(?:^|,)\s*([A-Za-z_$][\w$]*)\s+as\s+marked(?=\s*(?:,|$))");
        if (!binding.Success) throw new InvalidDataException("Bundled Marked module does not export marked");
        return source[..export.Index] + $"return {binding.Groups[1].Value};"
            + source[(export.Index + export.Length)..];
    }

    Task<object?> PublicAssistantScriptAsync(ChatRequestContext req)
    {
        var headers = new Dictionary<string, string>
        {
            [HttpHeaders.CacheControl] = "no-cache", ["X-Content-Type-Options"] = "nosniff",
        };
        var (assistant, _) = PublicAssistant(req.QueryString("g") ?? "");
        if (assistant == null)
            return Task.FromResult<object?>(new ChatResult
            {
                ContentType = "application/javascript", Headers = headers,
                Text = "console.error(\"Gemini Assistant widget failed to load: Assistant is unavailable (404)\");",
            });
        try
        {
            var widget = Ctx.GetBundledText("assistant-widget.js")
                ?? throw new FileNotFoundException("assistant-widget.js is not embedded");
            string markdown;
            try
            {
                markdown = BundledMarkdownSource();
            }
            catch (Exception e)
            {
                Log.LogWarning(e, "Failed embedding the bundled Marked renderer");
                markdown = "console.warn(\"Gemini Assistant Markdown renderer is unavailable; using plain text.\");return null;";
            }
            var config = GeminiAssistants.NormalizeConfig(ChatJson.TryParseObject(assistant.Config));
            var behavior = config.GetObject("behavior")!;
            var publicConfig = new JsonObject
            {
                ["assistantId"] = assistant.PublicId,
                ["title"] = config.GetObject("identity").GetString("title"),
                ["description"] = config.GetObject("identity").GetString("description"),
                ["welcome"] = config.GetObject("identity").GetString("welcome"),
                ["suggestions"] = config.GetObject("identity")?["suggestions"]?.DeepClone(),
                ["notice"] = behavior.GetString("notice"),
                ["launch"] = new JsonObject
                {
                    ["openMode"] = behavior.GetString("openMode"),
                    ["keyboardShortcut"] = behavior.GetBool("keyboardShortcut"),
                },
                ["appearance"] = config["appearance"]?.DeepClone(),
                ["chatUrl"] = AssistantBaseUrl(req).CombineWith($"ext/gemini/public/assistants/{assistant.PublicId}/chat"),
            };
            var source = $"(()=>{{const CONFIG={publicConfig.ToJsonString(ChatJson.Options)};"
                + $"const SCRIPT=document.currentScript;const MARKDOWN=(()=>{{\n{markdown}\n}})();"
                + $"const mount=()=>{{\n{widget}\n}};"
                + "if(document.body)mount();else addEventListener('DOMContentLoaded',mount,{once:true});})();";
            return Task.FromResult<object?>(new ChatResult
            {
                ContentType = "application/javascript", Headers = headers, Text = source,
            });
        }
        catch (Exception e)
        {
            Log.LogError(e, "Failed generating Gemini Assistant widget script");
            return Task.FromResult<object?>(new ChatResult
            {
                ContentType = "application/javascript", Headers = headers,
                Text = "console.error(\"Gemini Assistant widget failed to load. Check the server logs for details.\");",
            });
        }
    }

    async Task<object?> PublicAssistantChatAsync(ChatRequestContext req)
    {
        var (assistant, store) = PublicAssistant(req.GetPathParam("publicId"));
        if (assistant == null || store == null) return ChatResult.NotFound("Assistant is unavailable");
        var config = GeminiAssistants.NormalizeConfig(ChatJson.TryParseObject(assistant.Config));
        var allowedOrigins = GeminiMetadata.AsList(config.GetObject("hosting")?["allowedOrigins"]);
        var origin = req.Request.Headers[HttpHeaders.Origin];
        var allowed = GeminiAssistants.OriginAllowed(origin, allowedOrigins);
        var headers = CorsHeaders(origin, allowed);
        if (!allowed) return Error("This website is not allowed to use this Assistant", "OriginNotAllowed", 403, headers);
        var limit = config.GetObject("hosting").GetInt("requestsPerMinute") ?? 30;
        if (!assistantLimiter.Allow($"{assistant.Id}:{req.Request.RemoteIp ?? "unknown"}", limit))
        {
            headers["Retry-After"] = "60";
            return Error("Too many requests. Please wait a moment and try again.", "RateLimited", 429, headers);
        }
        JsonObject body;
        try { body = await req.GetJsonBodyAsync().ConfigAwait(); }
        catch { return Error("Invalid request", "ValidationError", 400, headers); }
        var message = (body.GetString("message") ?? "").Trim().SafeSubstring(0, 8000);
        var sessionId = (body.GetString("sessionId") ?? "").Trim().SafeSubstring(0, 100);
        if (message.Length == 0 || !Regex.IsMatch(sessionId, "^[A-Za-z0-9._~-]{12,100}$"))
            return Error("A message and valid sessionId are required", "ValidationError", 400, headers);
        if (string.IsNullOrEmpty(store.Name)) return Error("Assistant knowledge base is unavailable", status: 503, headers: headers);

        var conversation = db.FindAssistantConversation(assistant.Id, sessionId);
        if (conversation == null)
        {
            var conversationId = db.CreateAssistantConversation(assistant, sessionId, origin,
                (body.GetString("pageUrl") ?? "").SafeSubstring(0, 2000), req.Request.UserAgent.SafeSubstring(0, 1000));
            conversation = db.GetAssistantConversation(conversationId)!;
        }
        db.AddAssistantMessage(conversation, "user", message);
        var history = db.QueryAssistantMessages(conversation.Id);
        if (body.GetBool("stream"))
            return StreamAssistantAnswer(req, assistant, store, conversation, history, config, headers);
        try
        {
            var (answer, citations) = await GenerateAssistantAnswerAsync(assistant, store, history, config).ConfigAwait();
            db.AddAssistantMessage(conversation, "assistant", answer, citations);
            return new ChatResult
            {
                ContentType = MimeTypes.Json, Headers = headers,
                Text = new JsonObject { ["conversationId"] = conversation.Id, ["message"] = answer,
                    ["citations"] = citations }.ToJsonString(ChatJson.Options),
            };
        }
        catch (Exception e)
        {
            Log.LogError(e, "Assistant {AssistantId} chat failed", assistant.Id);
            var fallback = config.GetObject("behavior").GetString("fallback")!;
            db.AddAssistantMessage(conversation, "assistant", fallback, error: e.Message.SafeSubstring(0, 2000));
            return Error("The Assistant could not answer right now. Please try again.", "AssistantError", 500, headers);
        }
    }

    ChatStreamResult StreamAssistantAnswer(ChatRequestContext req, ChatAssistant assistant, ChatFilestore store,
        ChatAssistantConversation conversation, List<ChatAssistantMessage> history, JsonObject config,
        Dictionary<string, string> headers) => new(async response =>
    {
        response.ContentType = "application/x-ndjson";
        foreach (var (key, value) in headers) response.AddHeader(key, value);
        var connected = true;
        async Task WriteAsync(JsonObject value)
        {
            if (!connected) return;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(value.ToJsonString(ChatJson.Options) + "\n");
                await response.OutputStream.WriteAsync(bytes).ConfigAwait();
                await response.OutputStream.FlushAsync().ConfigAwait();
            }
            catch (IOException) { connected = false; }
            catch (ObjectDisposedException) { connected = false; }
        }

        var chunks = new StringBuilder();
        var citations = new JsonArray();
        var citationKeys = new HashSet<string>();
        try
        {
            var (behavior, model, generation) = AssistantGeneration(store, history, config);
            await foreach (var chunk in client.GenerateContentStreamAsync(model, generation))
            {
                var delta = ResultText(chunk);
                if (delta.Length > 0)
                {
                    chunks.Append(delta);
                    await WriteAsync(new JsonObject { ["delta"] = delta }).ConfigAwait();
                }
                foreach (var citation in ResultCitations(chunk, behavior.GetBool("citations", true)).OfType<JsonObject>())
                {
                    var key = $"{citation.GetString("title")}\u001f{citation.GetString("url")}";
                    // ResultCitations owns each node through its temporary JsonArray. Clone it
                    // before moving it into the cross-chunk accumulator.
                    if (citationKeys.Add(key)) citations.Add(citation.DeepClone());
                }
            }
            var answer = chunks.ToString().Trim();
            if (answer.Length == 0) answer = behavior.GetString("fallback")!;
            citations = ResolveCitationUrls(citations, store, assistant.User);
            db.AddAssistantMessage(conversation, "assistant", answer, citations);
            if (chunks.Length == 0) await WriteAsync(new JsonObject { ["delta"] = answer }).ConfigAwait();
            await WriteAsync(new JsonObject
            {
                ["done"] = true, ["citations"] = citations, ["conversationId"] = conversation.Id,
            }).ConfigAwait();
        }
        catch (Exception e)
        {
            Log.LogError(e, "Assistant {AssistantId} streaming chat failed", assistant.Id);
            var fallback = config.GetObject("behavior").GetString("fallback")!;
            db.AddAssistantMessage(conversation, "assistant", fallback, error: e.Message.SafeSubstring(0, 2000));
            await WriteAsync(new JsonObject { ["error"] = "The Assistant could not answer right now." }).ConfigAwait();
        }
    });

    (JsonObject Behavior, string Model, JsonObject Request) AssistantGeneration(ChatFilestore store,
        List<ChatAssistantMessage> messages, JsonObject config)
    {
        var behavior = config.GetObject("behavior")!;
        var system = GeminiAssistants.SystemInstruction(behavior);
        var search = new JsonObject
        {
            ["fileSearchStoreNames"] = new JsonArray(store.Name), ["topK"] = 10,
        };
        var expression = GeminiAssistants.MetadataFilter(config.GetObject("scope"));
        if (expression.Length > 0) search["metadataFilter"] = expression;
        var contents = new JsonArray(messages.Where(x => x.Role is "user" or "assistant").TakeLast(20).Select(x =>
            (JsonNode)new JsonObject
            {
                ["role"] = x.Role == "assistant" ? "model" : "user",
                ["parts"] = new JsonArray(new JsonObject { ["text"] = x.Content ?? "" }),
            }).ToArray());
        var request = new JsonObject
        {
            ["contents"] = contents,
            ["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = system }) },
            ["tools"] = new JsonArray(new JsonObject { ["fileSearch"] = search }),
        };
        var model = GeminiAssistants.ResolveModel(config,
            Ctx.Feature.ResolveVariable("$GEMINI_ASSISTANT_MODEL") ?? "gemini-flash-latest");
        return (behavior, model, request);
    }

    async Task<(string Answer, JsonArray Citations)> GenerateAssistantAnswerAsync(ChatAssistant assistant,
        ChatFilestore store, List<ChatAssistantMessage> messages, JsonObject config)
    {
        var (behavior, model, generation) = AssistantGeneration(store, messages, config);
        var result = await client.GenerateContentAsync(model, generation).ConfigAwait();
        var answer = ResultText(result).Trim();
        if (answer.Length == 0) answer = behavior.GetString("fallback")!;
        var citations = ResolveCitationUrls(ResultCitations(result, behavior.GetBool("citations", true)), store, assistant.User);
        return (answer, citations);
    }

    static string ResultText(JsonObject result)
    {
        var text = new StringBuilder();
        foreach (var candidate in result.GetArray("candidates")?.OfType<JsonObject>() ?? [])
        foreach (var part in candidate.GetObject("content")?.GetArray("parts")?.OfType<JsonObject>() ?? [])
            if (part.GetString("text") is { } value) text.Append(value);
        return text.ToString();
    }

    static JsonArray ResultCitations(JsonObject result, bool enabled)
    {
        var citations = new JsonArray();
        if (!enabled) return citations;
        var seen = new HashSet<string>();
        foreach (var candidate in result.GetArray("candidates")?.OfType<JsonObject>() ?? [])
        foreach (var chunk in candidate.GetObject("groundingMetadata")?.GetArray("groundingChunks")?.OfType<JsonObject>() ?? [])
        {
            var context = chunk.GetObject("retrievedContext");
            if (context == null) continue;
            var title = context.GetString("title") ?? "Source";
            var url = context.GetString("uri");
            if (seen.Add($"{title}\u001f{url}")) citations.Add(new JsonObject { ["title"] = title, ["url"] = url });
        }
        return citations;
    }

    JsonArray ResolveCitationUrls(JsonArray citations, ChatFilestore store, string? user)
    {
        var sourceUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var doc in db.AssistantCitationDocuments(store.Id, user))
        {
            if (string.IsNullOrEmpty(doc.SourceUrl)) continue;
            foreach (var value in new[] { doc.DisplayName, doc.SourceKey })
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                sourceUrls.TryAdd(value.Trim(), doc.SourceUrl);
                sourceUrls.TryAdd(Path.GetFileName(value.Trim()), doc.SourceUrl);
            }
        }
        foreach (var citation in citations.OfType<JsonObject>())
        {
            var title = (citation.GetString("title") ?? "").Trim();
            if (sourceUrls.TryGetValue(title, out var sourceUrl)
                || sourceUrls.TryGetValue(Path.GetFileName(title), out sourceUrl)) citation["url"] = sourceUrl;
            else if (citation.GetString("url") is not { } url
                     || !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                     && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) citation["url"] = null;
        }
        return citations;
    }

    static Dictionary<string, string> CorsHeaders(string? origin, bool allowed)
    {
        var headers = new Dictionary<string, string> { [HttpHeaders.Vary] = HttpHeaders.Origin };
        if (allowed) headers[HttpHeaders.AllowOrigin] = origin ?? "*";
        return headers;
    }
}
