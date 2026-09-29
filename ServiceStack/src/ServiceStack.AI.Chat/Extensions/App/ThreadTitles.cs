using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Independent one-line title generation (port of llms-py extensions/app/titles.py). There is no
/// job table: the thread row's TitleSource/TitleStatus/TitleVersion are the only state. An
/// idle → pending update claims generation once, a background task calls the configured
/// `defaults.summarize` model directly (no chat filters, tools, persistence or synthetic turns),
/// and the result is applied only while the thread still has the automatic fallback title.
/// Titles left pending by a restart are regenerated at startup from the first user message.
/// </summary>
public partial class ThreadTitles(ChatDb db, ExtensionContext ctx, Action<long> notify, int concurrency = 2)
{
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
    public static TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    const int MaxPromptChars = 12_000;

    // Bounded so title requests can't crowd out primary chat work
    readonly SemaphoreSlim semaphore = new(concurrency, concurrency);
    readonly CancellationTokenSource shutdown = new();
    readonly HashSet<Task> tasks = [];

    /// <summary>Overridable for tests: the provider call returning the model's raw text</summary>
    public Func<JsonObject, string?, CancellationToken, Task<string?>>? RequestTitleOverride { get; set; }

    JsonObject? Template() => ctx.GetConfigDefaults()?["summarize"] as JsonObject;

    /// <summary>Text of the first user message, string or multipart</summary>
    public static string PromptText(IEnumerable<JsonNode?> messages)
    {
        foreach (var message in messages.OfType<JsonObject>())
        {
            if (message.GetString("role") != "user")
                continue;
            return message["content"] switch
            {
                JsonValue v when v.TryGetValue<string>(out var s) => s,
                JsonArray parts => string.Join(" ", parts.OfType<JsonObject>()
                    .Where(p => p.GetString("type") == "text").Select(p => p.GetString("text") ?? "")),
                _ => "",
            };
        }
        return "";
    }

    [GeneratedRegex(@"^(?:title|chat title)\s*:\s*", RegexOptions.IgnoreCase)] private static partial Regex TitlePrefix();

    /// <summary>One plain line: strip prefixes, wrapping quotes/markdown, collapse whitespace, cap length</summary>
    public static string? NormalizeTitle(string? value)
    {
        if (value == null)
            return null;
        value = TitlePrefix().Replace(value.Trim(), "");
        value = string.Join(' ', value.Trim('"', '\'', '`', '#', '*', ' ')
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return value.Length > 0 && value.Length <= 300 ? (value.Length > 80 ? value[..80] : value) : null;
    }

    /// <summary>Start title generation for a thread's first accepted turn. Returns the task, if any.</summary>
    public Task? Enqueue(ChatThread thread, JsonArray messages, string? user)
    {
        if (thread.TitleSource != ChatDb.TitleSources.Fallback)
            return null;
        var prompt = Truncate(PromptText(messages).Trim());
        if (prompt.Length == 0 || Template() == null)
        {
            db.SetTitleStatus(thread.Id, ChatDb.TitleStatuses.Skipped, ChatDb.TitleStatuses.Idle);
            return null;
        }
        // idle -> pending is the claim: retries and follow-up turns never start a second request
        if (!db.SetTitleStatus(thread.Id, ChatDb.TitleStatuses.Pending, ChatDb.TitleStatuses.Idle))
            return null;
        return Spawn(thread.Id, thread.TitleVersion ?? 0, prompt, user ?? thread.User);
    }

    /// <summary>Resume titles interrupted by a restart, using each thread's first user message</summary>
    public void Start()
    {
        foreach (var row in db.GetPendingTitleThreads())
        {
            var first = db.GetFirstUserMessage(row.Id);
            var prompt = first == null ? "" : Truncate(PromptText([first]).Trim());
            if (prompt.Length > 0 && Template() != null)
            {
                db.SetTitleStatus(row.Id, ChatDb.TitleStatuses.Pending, ChatDb.TitleStatuses.Idle, ChatDb.TitleStatuses.Pending);
                Spawn(row.Id, row.TitleVersion ?? 0, prompt, row.User);
            }
            else
            {
                db.SetTitleStatus(row.Id, ChatDb.TitleStatuses.Skipped, ChatDb.TitleStatuses.Idle, ChatDb.TitleStatuses.Pending);
            }
        }
    }

    /// <summary>Cancel and join owned tasks</summary>
    public async Task StopAsync()
    {
        await shutdown.CancelAsync().ConfigAwait();
        Task[] running;
        lock (tasks) running = tasks.ToArray();
        try { await Task.WhenAll(running).ConfigAwait(); }
        catch (Exception) { /* cancelled */ }
    }

    static string Truncate(string s) => s.Length > MaxPromptChars ? s[..MaxPromptChars] : s;

    Task Spawn(long threadId, long version, string prompt, string? user)
    {
        var task = Task.Run(() => GenerateAsync(threadId, version, prompt, user));
        lock (tasks) tasks.Add(task);
        task.ContinueWith(t => { lock (tasks) tasks.Remove(t); }, TaskScheduler.Default);
        return task;
    }

    public async Task<string?> GenerateAsync(long threadId, long version, string prompt, string? user)
    {
        string? title = null;
        try
        {
            await semaphore.WaitAsync(shutdown.Token).ConfigAwait();
            try
            {
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        title = await RequestTitleAsync(prompt, user).ConfigAwait();
                        break;
                    }
                    catch (Exception e) when (attempt == 0 && IsTransient(e))
                    {
                        // one retry for transient failures; config/output errors are terminal
                        await Task.Delay(RetryDelay, shutdown.Token).ConfigAwait();
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
            return null; // host stopping: left pending, regenerated on next startup
        }
        catch (Exception e)
        {
            ctx.Log.LogDebug("Title generation failed for thread {ThreadId}: {Message}", threadId, e.Message);
        }
        // conditional on version/source: manual renames and deleted threads discard late results
        db.CompleteGeneratedTitle(threadId, version, title);
        notify(threadId);
        return title;
    }

    bool IsTransient(Exception e) => !shutdown.IsCancellationRequested
        && e is TimeoutException or TaskCanceledException or HttpRequestException or IOException;

    async Task<string> RequestTitleAsync(string prompt, string? user)
    {
        var template = Template()?.Clone() ?? throw new InvalidOperationException("Title generation disabled");
        var model = template.GetString("model") ?? throw new InvalidOperationException("Title model not configured");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
        cts.CancelAfter(Timeout);

        string? content;
        if (RequestTitleOverride != null)
        {
            content = await RequestTitleOverride(BuildRequest(template, prompt, null), user, cts.Token)
                .WaitAsync(Timeout, shutdown.Token).ConfigAwait();
        }
        else
        {
            var provider = ctx.GetProviders().Values.FirstOrDefault(p => p.ProviderModel(model) != null)
                ?? throw new InvalidOperationException("Title model unavailable");
            var info = provider.ModelInfo(model) ?? new JsonObject();
            var chat = BuildRequest(template, prompt, info);
            // No ThreadId/RunId: nothing is persisted, streamed to a thread or recorded as a turn
            var context = new ChatContext
            {
                Chat = chat, User = user, Tools = "none", NoHistory = true, NoStore = true,
                Provider = provider, ModelInfo = info, CancellationToken = cts.Token,
                Request = new Host.BasicRequest(),
            };
            context.Items["purpose"] = "thread_title";
            var response = await provider.ChatAsync(chat, context).WaitAsync(Timeout, shutdown.Token).ConfigAwait();
            content = (response.GetArray("choices")?.FirstOrDefault() as JsonObject)
                .GetObject("message").GetString("content");
        }
        return NormalizeTitle(content) ?? throw new InvalidOperationException("Invalid title response");
    }

    /// <summary>
    /// The template's system messages plus only the bounded first prompt: no history, attachments,
    /// project files or tool definitions, and no app-only keys forwarded to the provider.
    /// </summary>
    static JsonObject BuildRequest(JsonObject template, string prompt, JsonObject? modelInfo)
    {
        var context = modelInfo.GetObject("limit").GetLong("context") ?? 4096;
        var budget = (int)Math.Max(256, Math.Min(MaxPromptChars, context - 512));
        var messages = new JsonArray();
        foreach (var message in template.GetArray("messages")?.OfType<JsonObject>() ?? [])
            if (message.GetString("role") == "system") messages.Add(message.Clone());
        messages.Add(new JsonObject { ["role"] = "user", ["content"] = prompt.Length > budget ? prompt[..budget] : prompt });
        template["messages"] = messages;
        template["stream"] = false;
        foreach (var key in new[] { "tools", "metadata", "title", "threadId", "submissionId", "projectId" })
            template.Remove(key);
        return template;
    }
}
