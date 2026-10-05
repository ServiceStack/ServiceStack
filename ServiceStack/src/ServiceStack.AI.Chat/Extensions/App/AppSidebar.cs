using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Project-grouped sidebar API (port of llms-py extensions/app thread-sidebar routes): a grouped
/// bootstrap, independent keyset pages per project or Recents, and a user-scoped revision
/// channel (long-poll and SSE) that tells clients when to refetch.
/// </summary>
public partial class AppExtension
{
    const int ProjectPageSize = 5;
    const int RecentsPageSize = 30;
    /// <summary>Bursts of changes (e.g. a streaming response) are coalesced into one check per interval</summary>
    static readonly TimeSpan SidebarThrottle = TimeSpan.FromSeconds(1);

    /// <summary>Fields a client may never set: ownership, title state and revision counters</summary>
    static readonly string[] ProtectedThreadFields =
    [
        "metadataVersion", "membershipVersion", "titleSource", "titleStatus", "titleVersion",
        "titlePromptSequence", "lastActivityAt", "lastSubmissionId", "user",
    ];

    ThreadTitles Titles = null!;

    void RegisterSidebarRoutes(ExtensionContext ctx)
    {
        Titles = new ThreadTitles(Db, ctx, Updates.NotifyThreadUpdate);
        Titles.Start();
        ctx.RegisterShutdownHandler(_ => Titles.StopAsync());

        // literal routes registered before the parameterized thread routes
        ctx.AddGet("thread-sidebar/updates/stream", SidebarStreamAsync);
        ctx.AddGet("thread-sidebar/updates", SidebarUpdatesAsync);
        ctx.AddGet("thread-sidebar/threads", SidebarThreadsAsync);
        ctx.AddGet("thread-sidebar", SidebarAsync);
    }

    /// <summary>
    /// The workspace a thread's runs execute in. Without a project it's the owner's own workspace
    /// plus host-shared directories, never a stale global project selection or a central folder.
    /// </summary>
    JsonObject ResolveWorkspace(string? projectId, string? user)
    {
        if (projectId == null)
            return new JsonObject
            {
                ["projectId"] = null,
                ["directories"] = new JsonArray(Ctx.Feature.DefaultWorkspaceDirectories(user)
                    .Select(x => Ctx.ResolveDirectory(x)).Where(x => x != null)
                    .Select(x => (JsonNode)x!).ToArray()),
            };
        try
        {
            return Ctx.Projects.ResolveWorkspace(projectId, user);
        }
        catch (ArgumentException e)
        {
            throw HttpError.BadRequest(e.Message);
        }
    }

    List<JsonObject> ProjectHeaders(string? user) => Ctx.Projects.GetUserProjects(user);

    string SidebarRevision(string? user) =>
        Updates.SidebarRevisions.GetOrAdd(user ?? "", _ => ComputeSidebarRevision(user));

    string ComputeSidebarRevision(string? user)
    {
        var projects = string.Join('\n', ProjectHeaders(user).Select(p =>
            $"{p.GetString("id")}|{p.GetString("name")}|{p["showInSidebar"]?.ToJsonString() ?? "true"}|{p.GetBool("archived")}"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(Db.SidebarRevision(user) + "\n" + projects));
        return Convert.ToHexString(hash)[..24].ToLowerInvariant();
    }

    static string SidebarUser(ChatRequestContext req) => req.UserName ?? ChatDb.DefaultUser;

    Task<object?> SidebarAsync(ChatRequestContext req)
    {
        var user = SidebarUser(req);
        var headers = ProjectHeaders(user);
        if (Db.ReconcileProjects(headers.Select(p => p.GetString("id")).Where(x => x != null).Select(x => x!).ToList(), user) > 0)
            Updates.NotifySidebar();
        var active = Db.ProjectIdsWithMessages(user);
        var projects = new JsonArray();
        foreach (var project in headers)
        {
            var id = project.GetString("id");
            var hidden = project["showInSidebar"] is JsonValue shown && shown.TryGetValue<bool>(out var visible) && !visible;
            if (id == null || !active.Contains(id) || hidden || project.GetBool("archived"))
                continue;
            var group = Db.SidebarPage(user, id, ProjectPageSize);
            group["id"] = id;
            group["name"] = project.GetString("name");
            projects.Add(group);
        }
        return Task.FromResult<object?>(new JsonObject
        {
            ["revision"] = SidebarRevision(user),
            ["projects"] = projects,
            ["unassigned"] = Db.SidebarPage(user, null, RecentsPageSize),
        });
    }

    Task<object?> SidebarThreadsAsync(ChatRequestContext req)
    {
        var projectId = req.QueryString("projectId");
        var scope = req.QueryString("scope");
        if ((projectId != null && scope != null) || (projectId == null && scope != "unassigned"))
            throw HttpError.BadRequest("Specify projectId or scope=unassigned");
        var user = SidebarUser(req);
        ResolveWorkspace(projectId, user); // ownership: unknown projects are rejected
        if (!int.TryParse(req.QueryString("limit") ?? "10", out var limit))
            throw HttpError.BadRequest("Invalid sidebar limit");
        try
        {
            return Task.FromResult<object?>(Db.SidebarPage(user, projectId, limit, req.QueryString("cursor")));
        }
        catch (ArgumentException e)
        {
            throw HttpError.BadRequest(e.Message);
        }
    }

    async Task<object?> SidebarUpdatesAsync(ChatRequestContext req)
    {
        var user = SidebarUser(req);
        var clientSig = req.QueryString("sig");
        var deadline = DateTime.UtcNow + Updates.LongPollTimeout;
        while (true)
        {
            // capture the signal before reading, so a change can't slip between the two
            var signal = Updates.NextSidebarSignalAsync();
            var revision = SidebarRevision(user);
            var remaining = deadline - DateTime.UtcNow;
            if (revision != clientSig || remaining <= TimeSpan.Zero || !await WaitForSidebarChangeAsync(signal, remaining).ConfigAwait())
                return new JsonObject { ["revision"] = revision };
        }
    }

    /// <summary>True when signalled (after the coalescing delay), false on timeout</summary>
    static async Task<bool> WaitForSidebarChangeAsync(Task signal, TimeSpan timeout)
    {
        if (await Task.WhenAny(signal, Task.Delay(timeout)).ConfigAwait() != signal)
            return false;
        await Task.Delay(SidebarThrottle).ConfigAwait();
        return true;
    }

    Task<object?> SidebarStreamAsync(ChatRequestContext req)
    {
        var config = EventsConfig();
        if (config.GetString("transport") == "long-poll")
            return Task.FromResult<object?>(ChatResult.NotFound("SSE transport is disabled"));
        var user = SidebarUser(req);
        var clientSig = req.QueryString("sig");
        return Task.FromResult<object?>(new ChatStreamResult(async response =>
        {
            try
            {
                response.StatusCode = 200;
                response.ContentType = "text/event-stream";
                response.AddHeader("Cache-Control", "no-cache, no-transform");
                response.AddHeader("X-Accel-Buffering", "no");
                var heartbeatEvery = TimeSpan.FromSeconds(config.GetDouble("sseHeartbeatSeconds") ?? 15);
                var signature = clientSig;
                while (true)
                {
                    var signal = Updates.NextSidebarSignalAsync();
                    var revision = SidebarRevision(user);
                    if (revision != signature)
                    {
                        await WriteSseAsync(response, null,
                            "data: " + new JsonObject { ["revision"] = revision }.ToJsonString() + "\n\n").ConfigAwait();
                        signature = revision;
                    }
                    while (!await WaitForSidebarChangeAsync(signal, heartbeatEvery).ConfigAwait())
                        await WriteSseAsync(response, null, ": heartbeat\n\n").ConfigAwait();
                }
            }
            catch (IOException) { /* browser/proxy disconnected */ }
            catch (ObjectDisposedException) { /* response was closed */ }
            catch (OperationCanceledException) { /* host is stopping */ }
        }));
    }

    static void RemoveProtectedFields(JsonObject thread)
    {
        foreach (var key in ProtectedThreadFields)
            thread.Remove(key);
    }
}
