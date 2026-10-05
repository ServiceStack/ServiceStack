using ServiceStack.Text;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using ServiceStack.OrmLite;

namespace ServiceStack.AI;

/// <summary>Durable, user-owned bounded folder provisioning. One web process owns App_Data.</summary>
public sealed class ProjectCreation(ProjectsExtension projects, ExtensionContext ctx)
{
    public const string Marker = ".llms-creation";
    readonly string owner = Guid.NewGuid().ToString();
    readonly SemaphoreSlim slots = new(2, 2);
    readonly CancellationTokenSource shutdown = new();
    readonly Dictionary<string, Task> tasks = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, CancellationTokenSource> cancellations = new();
    readonly ConcurrentDictionary<string, TaskCompletionSource> signals = new();
    static readonly ConcurrentDictionary<string, SemaphoreSlim> StoreLocks = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    SemaphoreSlim StoreLock => StoreLocks.GetOrAdd(Path.GetFullPath(ctx.GetHomePath()), _ => new SemaphoreSlim(1, 1));
    ChatDb Db => ctx.Feature.ChatDb ?? throw new InvalidOperationException("Project creation requires ChatDb");
    public TimeProvider Clock { get; set; } = TimeProvider.System;
    DateTime Now => Clock.GetUtcNow().UtcDateTime;
    static bool Terminal(string state) => state is "succeeded" or "failed" or "cancelled" or "interrupted";
    static string Key(string user, string id) => user + "\0" + id;
    static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    void Notify(string user, string id)
    {
        var key = Key(user, id);
        if (signals.TryRemove(key, out var signal)) signal.TrySetResult();
    }

    public void InitSchema()
    {
        using var db = Db.OpenDb();
        db.CreateTableIfNotExists<ChatProjectCreation>();
        db.CreateTableIfNotExists<ChatProjectCreationReservation>();
        ChatDb.AddMissingColumns<ChatProjectCreation>(db);
        ChatDb.AddMissingColumns<ChatProjectCreationReservation>(db);
    }
    public void DropSchema()
    {
        using var db = Db.OpenDb();
        db.DropTable<ChatProjectCreationReservation>();
        db.DropTable<ChatProjectCreation>();
    }

    public ChatProjectCreation Read(string user, string id)
    {
        using var db = Db.OpenDb();
        return db.Single<ChatProjectCreation>(x => x.Id == id && x.User == user) ?? throw HttpError.NotFound("Project creation not found.");
    }
    public JsonObject Snapshot(ChatProjectCreation row)
    {
        var payload = ChatJson.ParseObject(row.Payload);
        return new JsonObject {
            ["id"] = row.Id, ["state"] = row.State, ["message"] = row.Message, ["percent"] = row.Percent,
            ["revision"] = row.Revision, ["created"] = new DateTimeOffset(DateTime.SpecifyKind(row.Created, DateTimeKind.Utc)).ToUnixTimeMilliseconds() / 1000d,
            ["error"] = row.Error, ["project"] = payload["project"]?.DeepClone(), ["source"] = payload["source"]?.DeepClone(),
            ["result"] = row.Result == null ? null : JsonNode.Parse(row.Result),
            ["cancellable"] = (row.State is "queued" or "running") && !row.Cancelled,
        };
    }

    static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    static string Canonical(JsonNode? node) => node switch {
        JsonObject obj => "{" + string.Join(",", obj.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => JsonValue.Create(x.Key)!.ToJsonString() + ":" + Canonical(x.Value))) + "}",
        JsonArray arr => "[" + string.Join(",", arr.Select(Canonical)) + "]",
        _ => node?.ToJsonString() ?? "null",
    };
    static string NormalizePath(string path) => OperatingSystem.IsWindows() ? path.ToUpperInvariant() : path;
    static IEnumerable<string> ReservationKeys(ChatProjectCreation row)
    {
        yield return Hash(row.User + "\0name\0" + row.Name);
        yield return Hash(row.User + "\0destination\0" + NormalizePath(row.Destination));
    }
    void Reserve(System.Data.IDbConnection db, ChatProjectCreation row)
    {
        foreach (var key in ReservationKeys(row))
        {
            var existing = db.SingleById<ChatProjectCreationReservation>(key);
            if (existing != null && existing.OperationId != row.Id) throw HttpError.Conflict("A project with that name or folder is already being created.");
            if (existing == null) db.Insert(new ChatProjectCreationReservation { Id = key, User = row.User, OperationId = row.Id });
        }
    }

    (string RequestId, string Fingerprint, JsonObject Payload, string Destination) Validate(string user, JsonObject data)
    {
        var input = data.GetObject("project") ?? throw HttpError.BadRequest("Enter project details.");
        var name = input.GetString("name");
        var folder = input.GetString("folder");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200) throw HttpError.BadRequest("Enter a project name (up to 200 characters).");
        if (string.IsNullOrWhiteSpace(folder) || folder.Length > 150 || folder.IndexOfAny("/\\:*?\"<>|".ToCharArray()) >= 0
            || folder.Any(c => c < 32) || folder.StartsWith('.') || folder.EndsWith('.') || folder.EndsWith(' '))
            throw HttpError.BadRequest("Use a folder name without slashes or special path characters.");
        folder = folder.Trim();
        var reserved = folder.Split('.')[0].ToUpperInvariant();
        if (reserved is "CON" or "PRN" or "AUX" or "NUL" || Enumerable.Range(1, 9).Any(i => reserved == "COM" + i || reserved == "LPT" + i))
            throw HttpError.BadRequest("Choose a different folder name.");
        var show = input.ContainsKey("showInSidebar") ? input["showInSidebar"] : JsonValue.Create(true);
        if (show is not JsonValue sv || !sv.TryGetValue<bool>(out var visible)) throw HttpError.BadRequest("Invalid project details.");
        var project = new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["name"] = name.Trim(), ["folder"] = folder, ["showInSidebar"] = visible };
        foreach (var field in new[] { "description", "publish" })
        {
            if (input.ContainsKey(field) && (input[field] is not JsonValue v || !v.TryGetValue<string>(out _))) throw HttpError.BadRequest("Invalid project details.");
            var value = input.GetString(field) ?? "";
            if (value.Length > 4000) throw HttpError.BadRequest("Invalid project details.");
            project[field] = value;
        }
        if (data.ContainsKey("source") && data["source"] is not JsonObject) throw HttpError.BadRequest("Choose New project or Clone repository.");
        var source = data.GetObject("source")?.Clone() ?? new JsonObject { ["kind"] = "new" };
        var kind = source.GetString("kind");
        if (kind is not ("new" or "clone")) throw HttpError.BadRequest("Choose New project or Clone repository.");
        if (kind == "clone")
        {
            if (!ctx.Feature.GitProvisioner.Clone) throw new HttpError(503, "GitUnavailable", "Git cloning is not available on this server.");
            source = ctx.Feature.GitProvisioner.Validate(source);
            project["gitSource"] = source.Clone();
        }
        else
        {
            var initialize = false;
            if (source.ContainsKey("initializeGit") && (source["initializeGit"] is not JsonValue v || !v.TryGetValue<bool>(out initialize)))
                throw HttpError.BadRequest("Invalid Git initialization option.");
            if (initialize && !ctx.Feature.GitProvisioner.InitializeGit) throw new HttpError(503, "GitUnavailable", "Git is not available. Uncheck Initialize Git repository to continue.");
            source = new JsonObject { ["kind"] = "new", ["initializeGit"] = initialize };
        }
        if (!Guid.TryParse(data.GetString("requestId"), out var requestId)) throw HttpError.BadRequest("Invalid creation request. Refresh and try again.");
        var payload = new JsonObject { ["project"] = project, ["source"] = source };
        var fingerprint = payload.Clone();
        fingerprint.GetObject("project")!.Remove("id");
        return (requestId.ToString(), Hash(Canonical(fingerprint)), payload, projects.CreationDestination(project, user));
    }

    public async Task<JsonObject> CreateAsync(string user, JsonObject data)
    {
        var (requestId, fingerprint, payload, destination) = Validate(user, data);
        ChatProjectCreation row;
        await StoreLock.WaitAsync(shutdown.Token).ConfigAwait();
        try
        {
            using var db = Db.OpenDb();
            using var tx = db.OpenTransaction();
            var existing = db.Single<ChatProjectCreation>(x => x.User == user && x.RequestId == requestId);
            if (existing != null)
            {
                if (existing.Fingerprint != fingerprint) throw HttpError.Conflict("This request was already submitted with different project details.");
                row = existing;
            }
            else
            {
                projects.CheckCreation(payload.GetObject("project")!, user);
                if (Path.Exists(destination) || new DirectoryInfo(destination).LinkTarget != null) throw HttpError.Conflict("That folder already exists. Choose another folder name.");
                if (db.Count<ChatProjectCreation>(x => x.User == user && (x.State == "queued" || x.State == "running" || x.State == "finalizing")) >= 2)
                    throw HttpError.Conflict("Two projects are already being created. Wait for one to finish.");
                var id = Guid.NewGuid().ToString();
                row = new ChatProjectCreation { Id = id, User = user, RequestId = requestId, Fingerprint = fingerprint,
                    Payload = payload.ToJsonString(), Name = payload.GetObject("project").GetString("name")!, Destination = destination,
                    Created = Now, Updated = Now, Temporary = ".create-" + id };
                Reserve(db, row);
                db.Insert(row);
            }
            tx.Commit();
        }
        finally { StoreLock.Release(); }
        Schedule(row);
        return Snapshot(row);
    }

    async Task<ChatProjectCreation> UpdateAsync(string user, string id, Action<ChatProjectCreation> change, bool requireOwner = false, bool reserve = false)
    {
        await StoreLock.WaitAsync().ConfigAwait();
        ChatProjectCreation row;
        try
        {
            using var db = Db.OpenDb();
            using var tx = db.OpenTransaction();
            row = db.Single<ChatProjectCreation>(x => x.Id == id && x.User == user) ?? throw HttpError.NotFound("Project creation not found.");
            if (requireOwner && row.Owner != owner) throw new LostLeaseException();
            change(row);
            if (reserve) Reserve(db, row);
            row.Updated = Now;
            row.Revision++;
            db.Update(row);
            if (Terminal(row.State)) db.Delete<ChatProjectCreationReservation>(x => x.OperationId == row.Id && x.User == user);
            tx.Commit();
        }
        finally { StoreLock.Release(); }
        Notify(user, id);
        return row;
    }

    void Schedule(ChatProjectCreation row)
    {
        var key = Key(row.User, row.Id);
        lock (tasks)
        {
            if (shutdown.IsCancellationRequested || Terminal(row.State) || tasks.ContainsKey(key)) return;
            tasks[key] = Task.Run(async () =>
            {
                try { await RunAsync(row.User, row.Id).ConfigAwait(); }
                finally
                {
                    lock (tasks) tasks.Remove(key);
                    // A retry can reset the row just before the prior task leaves its finally.
                    if (!shutdown.IsCancellationRequested)
                    {
                        var current = Read(row.User, row.Id);
                        if (current.State == "queued") Schedule(current);
                    }
                }
            });
        }
    }

    public Task StartAsync()
    {
        using var db = Db.OpenDb();
        foreach (var row in db.Select<ChatProjectCreation>(x => x.State == "queued" || x.State == "running" || x.State == "finalizing")) Schedule(row);
        return Task.CompletedTask;
    }

    public JsonObject Options(string user)
    {
        using var db = Db.OpenDb();
        var live = db.Select<ChatProjectCreation>(x => x.User == user && (x.State == "queued" || x.State == "running" || x.State == "finalizing"));
        foreach (var row in live) Schedule(row);
        var rows = db.Select(db.From<ChatProjectCreation>().Where(x => x.User == user).OrderByDescending(x => x.Created).Limit(5));
        return new JsonObject { ["create"] = true, ["initializeGit"] = ctx.Feature.GitProvisioner.InitializeGit,
            ["clone"] = ctx.Feature.GitProvisioner.Clone, ["root"] = projects.ManagedRoot(user),
            ["operations"] = new JsonArray(rows.Select(x => (JsonNode)Snapshot(x)).ToArray()) };
    }

    public async Task<JsonObject> GetAsync(string user, string id, string? revision, CancellationToken token)
    {
        var key = Key(user, id);
        var signal = signals.GetOrAdd(key, _ => NewSignal());
        var row = Read(user, id);
        Schedule(row);
        if (!Terminal(row.State) && revision == row.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture))
        {
            try { await signal.Task.WaitAsync(TimeSpan.FromSeconds(25), token).ConfigAwait(); }
            catch (TimeoutException) { }
            row = Read(user, id);
        }
        return Snapshot(row);
    }

    public async Task<JsonObject> CancelAsync(string user, string id)
    {
        var row = await UpdateAsync(user, id, row => {
            if (row.State is "queued" or "running") row.Cancelled = true;
            else if (!Terminal(row.State)) throw HttpError.Conflict("Your project is almost ready. Please wait for creation to finish.");
        }).ConfigAwait();
        if (cancellations.TryGetValue(Key(user, id), out var cts)) cts.Cancel();
        Schedule(row);
        return Snapshot(row);
    }

    public async Task<JsonObject> RetryAsync(string user, string id)
    {
        var existing = Read(user, id);
        if (existing.State is not ("failed" or "interrupted" or "cancelled")) return Snapshot(existing);
        var row = await UpdateAsync(user, id, row => {
            if (row.State is not ("failed" or "interrupted" or "cancelled")) return;
            if (!row.Prepared)
            {
                projects.CheckCreation(ChatJson.ParseObject(row.Payload).GetObject("project")!, user);
                if (Path.Exists(row.Destination)) throw HttpError.Conflict("That folder already exists. Choose another folder name.");
                // Orphaned child processes cannot write into the fresh retry workspace.
                row.Temporary = ".create-" + Guid.NewGuid();
            }
            row.State = "queued"; row.Cancelled = false; row.Owner = null; row.Lease = null;
            row.Child = null; row.Error = null; row.Percent = null; row.Message = "Preparing your project…";
        }, reserve: true).ConfigAwait();
        Schedule(row);
        return Snapshot(row);
    }

    async Task RunAsync(string user, string id)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
        var key = Key(user, id);
        cancellations[key] = cts;
        var acquired = false;
        var claimed = false;
        string? temporary = null;
        try
        {
            await slots.WaitAsync(cts.Token).ConfigAwait(); acquired = true;
            while (true)
            {
                cts.Token.ThrowIfCancellationRequested();
                try
                {
                    await UpdateAsync(user, id, row => {
                        if (Terminal(row.State)) throw new LostLeaseException();
                        if (row.Owner != null && row.Owner != owner && row.Lease > Now) throw new LeaseBusyException();
                        row.Owner = owner; row.Lease = Now.AddSeconds(20);
                    }).ConfigAwait();
                    claimed = true; break;
                }
                catch (LeaseBusyException) { await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigAwait(); }
            }
            var row = Read(user, id);
            temporary = Path.Combine(projects.ManagedRoot(user), row.Temporary);
            if (row.State == "running" && !row.Prepared)
            {
                await FinishAsync(user, id, "interrupted", "Creation interrupted", "Creation was interrupted. Retry to start again safely.").ConfigAwait();
                return;
            }
            if (row.Cancelled) throw new OperationCanceledException();
            var payload = ChatJson.ParseObject(row.Payload);
            var project = payload.GetObject("project")!;
            var source = payload.GetObject("source")!;
            if (!row.Prepared)
            {
                await UpdateAsync(user, id, r => { r.State = "running"; r.Message = source.GetString("kind") == "clone" ? "Cloning repository…" : "Creating your project…"; }, true).ConfigAwait();
                if (Path.Exists(temporary) || new DirectoryInfo(temporary).LinkTarget != null) throw HttpError.Conflict("The temporary project folder has changed.");
                Directory.CreateDirectory(temporary);
                File.WriteAllText(Path.Combine(temporary, Marker), id);
                var work = Path.Combine(temporary, "workspace");
                using var heartbeat = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                var pulse = HeartbeatAsync(user, id, heartbeat.Token);
                try
                {
                    if (source.GetString("kind") == "clone" || source.GetBool("initializeGit"))
                        await ctx.Feature.GitProvisioner.ProvisionAsync(work, source, user,
                            (phase, percent) => UpdateAsync(user, id, r => { r.Message = phase; r.Percent = percent; }, true),
                            pid => UpdateAsync(user, id, r => r.Child = pid), cts.Token).ConfigAwait();
                    else Directory.CreateDirectory(work);
                }
                finally
                {
                    heartbeat.Cancel();
                    try { await pulse.ConfigAwait(); } catch (OperationCanceledException) { }
                }
                cts.Token.ThrowIfCancellationRequested();
                if (Path.Exists(Path.Combine(work, Marker))) throw HttpError.Conflict("The repository contains a reserved project metadata file.");
                File.WriteAllText(Path.Combine(work, Marker), id);
                await UpdateAsync(user, id, r => {
                    if (r.Cancelled) throw new OperationCanceledException();
                    r.State = "finalizing"; r.Prepared = true; r.Message = "Opening your project…"; r.Percent = null;
                }, true).ConfigAwait();
            }
            var result = await projects.RegisterCreatedAsync(project, user, Path.Combine(temporary, "workspace"), id).ConfigAwait();
            await UpdateAsync(user, id, r => { r.State = "succeeded"; r.Result = result.ToJsonString(); r.Error = null; r.Message = "Your project is ready"; r.Owner = null; r.Lease = null; }, true).ConfigAwait();
            Cleanup(temporary, id);
        }
        catch (LostLeaseException) { }
        catch (OperationCanceledException)
        {
            if (claimed)
                await FinishAsync(user, id, shutdown.IsCancellationRequested ? "interrupted" : "cancelled",
                    shutdown.IsCancellationRequested ? "Creation interrupted" : "Creation cancelled", shutdown.IsCancellationRequested ? "The server restarted. Retry to continue." : null).ConfigAwait();
            // Never started: a shutdown leaves it queued for StartAsync to resume after restart
            // (llms-py recover parity); only the user's cancellation ends it.
            else if (!shutdown.IsCancellationRequested && !Terminal(Read(user, id).State))
                await UpdateAsync(user, id, r => { r.State = "cancelled"; r.Message = "Creation cancelled"; r.Owner = null; r.Lease = null; }).ConfigAwait();
            if (temporary != null && !Read(user, id).Prepared) Cleanup(temporary, id);
        }
        catch (Exception e)
        {
            if (claimed)
            {
                var message = e is HttpError h ? h.Message : "Unable to create the project folder. Check available space and folder permissions.";
                await FinishAsync(user, id, "failed", "Project could not be created", message.Length > 500 ? message[..500] : message).ConfigAwait();
                if (temporary != null && !Read(user, id).Prepared) Cleanup(temporary, id);
            }
        }
        finally
        {
            cancellations.TryRemove(key, out _);
            if (acquired) slots.Release();
        }
    }

    Task<ChatProjectCreation> FinishAsync(string user, string id, string state, string message, string? error) => UpdateAsync(user, id, r => {
        r.State = state; r.Message = message; r.Error = error; r.Owner = null; r.Lease = null;
    }, true);

    async Task HeartbeatAsync(string user, string id, CancellationToken token)
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigAwait();
            await UpdateAsync(user, id, r => r.Lease = Now.AddSeconds(20), true).ConfigAwait();
        }
    }

    public static bool VerifiedMarker(string directory, string id)
    {
        var marker = Path.Combine(directory, Marker);
        return Directory.Exists(directory) && new DirectoryInfo(directory).LinkTarget == null
            && File.Exists(marker) && new FileInfo(marker).LinkTarget == null && File.ReadAllText(marker) == id;
    }
    static void Cleanup(string directory, string id)
    {
        if (VerifiedMarker(directory, id)) Directory.Delete(directory, true);
    }
    public async Task StopAsync()
    {
        await shutdown.CancelAsync().ConfigAwait();
        Task[] running; lock (tasks) running = tasks.Values.ToArray();
        await Task.WhenAll(running).ConfigAwait();
    }
    sealed class LostLeaseException : Exception;
    sealed class LeaseBusyException : Exception;
}

public partial class ProjectsExtension
{
    public ProjectCreation? Creation { get; private set; }
    public bool CreationAvailable => Creation != null;
    public string ManagedRoot(string? user) => ProjectsExplorer.PhysicalPath(Path.Combine(Ctx.GetUserPath(user), "projects"));
    public string CreationDestination(JsonObject project, string? user)
    {
        var destination = UserProjectDir(user, project);
        if (!IsWithin(destination, ManagedRoot(user)) || destination == ManagedRoot(user)) throw HttpError.BadRequest("Project folder must be inside the projects directory");
        return destination;
    }
    public void CheckCreation(JsonObject project, string? user)
    {
        var destination = CreationDestination(project, user);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (GetUserProjects(user).Any(p => p.GetString("id") != project.GetString("id") &&
            (p.GetString("name") == project.GetString("name") || string.Equals(CreationDestination(p, user), destination, comparison))))
            throw HttpError.Conflict("A project with that name or folder already exists.");
    }
    public async Task<JsonObject> RegisterCreatedAsync(JsonObject project, string user, string temporary, string operationId)
    {
        using var lease = await LockProjectsAsync().ConfigAwait();
        var projects = ReadUserProjectsJson(user);
        var existing = projects.OfType<JsonObject>().FirstOrDefault(p => p.GetString("id") == project.GetString("id"));
        if (existing != null)
        {
            var directory = CreationDestination(existing, user);
            if (ProjectCreation.VerifiedMarker(directory, operationId)) File.Delete(Path.Combine(directory, ProjectCreation.Marker));
            return new JsonObject { ["project"] = existing.Clone(), ["projects"] = projects.Clone() };
        }
        var destination = CreationDestination(project, user);
        if (projects.OfType<JsonObject>().Any(p => p.GetString("name") == project.GetString("name") || CreationDestination(p, user) == destination))
            throw HttpError.Conflict("A project with that name or folder already exists.");
        if (Path.Exists(destination))
        {
            if (!ProjectCreation.VerifiedMarker(destination, operationId)) throw HttpError.Conflict("That folder belongs to another creation request.");
        }
        else
        {
            if (!ProjectCreation.VerifiedMarker(temporary, operationId) || !IsWithin(ProjectsExplorer.PhysicalPath(temporary), ManagedRoot(user)))
                throw HttpError.Conflict("Unable to verify the new project folder.");
            Directory.Move(temporary, destination);
        }
        var metadata = Path.Combine(destination, ".git");
        if (new DirectoryInfo(metadata).LinkTarget != null || File.Exists(metadata)
            || !IsWithin(ProjectsExplorer.PhysicalPath(metadata), destination))
            throw HttpError.Conflict("Linked or external Git metadata is unsupported.");
        project = project.Clone();
        project["publish"] = SanitizePublishPath(project.GetString("publish"), destination);
        projects.Add(project.Clone());
        WriteProjects(user, projects);
        File.Delete(Path.Combine(destination, ProjectCreation.Marker));
        NotifySidebar();
        return new JsonObject { ["project"] = project, ["projects"] = projects };
    }
    void InstallCreation(ExtensionContext ctx)
    {
        if (ctx.Feature.ChatDb == null) return;
        Creation = new ProjectCreation(this, ctx);
        if (ctx.Feature.AutoInitSchema) Creation.InitSchema();
        ctx.RegisterShutdownHandler(_ => Creation.StopAsync());
        ctx.AddGet("creation/options", req => Task.FromResult<object?>(Creation.Options(req.AssertUserName())));
        ctx.AddPost("create", async req => ChatResult.Json(await Creation.CreateAsync(req.AssertUserName(), await req.GetJsonBodyAsync().ConfigAwait()).ConfigAwait(), 202));
        ctx.AddGet("creation/operations/{id}", async req => await Creation.GetAsync(req.AssertUserName(), req.GetPathParam("id"), req.QueryString("revision"), req.Request.RequestAborted).ConfigAwait());
        ctx.AddPost("creation/operations/{id}/cancel", async req => await Creation.CancelAsync(req.AssertUserName(), req.GetPathParam("id")).ConfigAwait());
        ctx.AddPost("creation/operations/{id}/retry", async req => ChatResult.Json(await Creation.RetryAsync(req.AssertUserName(), req.GetPathParam("id")).ConfigAwait(), 202));
    }
    public override Task LoadAsync(ExtensionContext ctx, CancellationToken token = default) => Creation?.StartAsync() ?? Task.CompletedTask;
    public void InitSchema() => Creation?.InitSchema();
    public void DropSchema() => Creation?.DropSchema();
}
