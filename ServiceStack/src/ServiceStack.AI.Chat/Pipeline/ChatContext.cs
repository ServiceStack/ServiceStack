using ServiceStack.Text;
using System.Text.Json.Nodes;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// Per-completion context flowing through the chat pipeline — the C# equivalent of llms-py's
/// context dict ({"chat", "request", "user", "threadId", "tools", "nostore", "nohistory", ...}).
/// </summary>
public class ChatContext
{
    public JsonObject? Chat { get; set; }
    public string? User { get; set; }
    public long? ThreadId { get; set; }

    /// <summary>
    /// The HTTP request this completion is running under, when there is one. Tools that act on the
    /// user's behalf (e.g. calling the App's own APIs) need it to execute as that user — without it
    /// they can only refuse, since a username alone can't authorize anything.
    /// </summary>
    public IRequest? Request { get; set; }
    /// <summary>Tool selector: "all" | "none" | csv of tool/group names</summary>
    public string Tools { get; set; } = "all";
    public bool NoStore { get; set; }
    public bool NoHistory { get; set; }

    public ChatProvider? Provider { get; set; }
    public JsonObject? ModelInfo { get; set; }
    public JsonObject? ModelCost { get; set; }
    public JsonObject? ProviderResponse { get; set; }

    /// <summary>Extension state bag (Python stores arbitrary keys in the context dict)</summary>
    public Dictionary<string, object?> Items { get; } = [];

    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

    /// <summary>
    /// Build the context for a completion from its request JSON, reading the same `metadata` keys
    /// llms-py does. Shared by the /v1/chat/completions service and IChatClient so both dispatch
    /// identically. `user` is always supplied by the caller — it is never read from the request,
    /// so a client can't select which user's data a completion is attributed to.
    /// </summary>
    public static ChatContext FromChat(JsonObject chat, string? user, CancellationToken token = default,
        IRequest? request = null)
    {
        var metadata = chat.GetObject("metadata");
        var noStore = MetaBool(metadata, "nostore");
        return new ChatContext
        {
            Chat = chat,
            User = user,
            Request = request,
            ThreadId = metadata.GetLong("threadId"),
            Tools = metadata.GetString("tools") ?? "all",
            NoStore = noStore,
            NoHistory = MetaBool(metadata, "nohistory") || noStore,
            CancellationToken = token,
        };
    }

    /// <summary>
    /// A detached copy of a request for background completions. A queued completion outlives the
    /// HTTP request that queued it, so the live IRequest is gone by the time its tools run — this
    /// carries over the resolved session, which is all a tool acting as the user needs.
    /// </summary>
    public static IRequest DetachRequest(IRequest request)
    {
        var to = new Host.BasicRequest();
        if (request.GetSession() is { } session)
            to.SetItem(Keywords.Session, session);
        return to;
    }

    /// <summary>
    /// Raw OpenAI JSON carries real booleans here, but a typed ChatCompletion's
    /// Dictionary&lt;string,string&gt; Metadata can only produce "true"/"false" — accept both.
    /// </summary>
    static bool MetaBool(JsonObject? metadata, string key) =>
        metadata.GetBool(key) || (bool.TryParse(metadata.GetString(key), out var b) && b);
}

/// <summary>Chat pipeline filter hooks that extensions can register (mirrors AppExtensions filters)</summary>
public class ChatFilters
{
    public List<Func<JsonObject, ChatContext, Task>> ChatRequestFilters { get; } = [];
    public List<Func<JsonObject, ChatContext, Task>> ChatToolFilters { get; } = [];
    public List<Func<string, ChatContext, Task>> ChatStatusFilters { get; } = [];
    public List<Func<JsonObject, ChatContext, Task>> ChatResponseFilters { get; } = [];
    public List<Func<Exception, ChatContext, Task>> ChatErrorFilters { get; } = [];
    public List<Action<CacheSavedContext>> CacheSavedFilters { get; } = [];
    public List<Func<IRequest, Task>> SetupUserHandlers { get; } = [];
    public List<Action> ShutdownHandlers { get; } = [];

    public async Task OnChatRequestAsync(JsonObject chat, ChatContext context)
    {
        foreach (var filter in ChatRequestFilters)
            await filter(chat, context).ConfigAwait();
    }

    public async Task OnChatToolAsync(JsonObject chat, ChatContext context)
    {
        foreach (var filter in ChatToolFilters)
            await filter(chat, context).ConfigAwait();
    }

    public async Task OnChatStatusAsync(string status, ChatContext context)
    {
        foreach (var filter in ChatStatusFilters)
            await filter(status, context).ConfigAwait();
    }

    public async Task OnChatResponseAsync(JsonObject response, ChatContext context)
    {
        foreach (var filter in ChatResponseFilters)
            await filter(response, context).ConfigAwait();
    }

    public async Task OnChatErrorAsync(Exception e, ChatContext context)
    {
        foreach (var filter in ChatErrorFilters)
            await filter(e, context).ConfigAwait();
    }

    public void OnCacheSaved(CacheSavedContext context)
    {
        foreach (var filter in CacheSavedFilters)
            filter(context);
    }
}

/// <summary>Fired after a file is written to the content-addressed cache (uploads, generated media)</summary>
public class CacheSavedContext
{
    public required string Url { get; set; }
    public required JsonObject Info { get; set; }
    public string? User { get; set; }
}
