using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Projects (port of llms-py's "projects" extension): each project is a dedicated folder under
/// App_Data/chat/user/&lt;user&gt;/projects/&lt;folder&gt; that the filesystem/code tools are restricted to.
/// The project list is file-backed per user at user/&lt;user&gt;/projects/projects.json and the active
/// project is a user pref.
/// </summary>
public partial class ProjectsExtension() : ChatExtension("projects"), IProjectsApi, IHasSchema
{
    public override void Install(ExtensionContext ctx)
    {
        RegisterOrganizationRoutes(ctx);
        RegisterExplorerRoutes(ctx);
        InstallCreation(ctx);
        ctx.AddGet("projects.json", req =>
            Task.FromResult<object?>(GetUserProjectsJson(req.UserName)));

        ctx.AddPost("projects.json", SaveProjectsAsync);
        ctx.AddPost("save/{name}", SaveProjectAsync);
        ctx.AddPost("active", SetActiveProjectAsync);
        ctx.AddPatch("sidebar/{id}", SetSidebarVisibilityAsync);

        // first-time user setup: apply their active project's directory
        ctx.RegisterSetupUserHandler(request =>
        {
            var user = ctx.GetUserName(request);
            var activeProject = ctx.GetUserPref("project", user)?.GetValue<string>();
            if (GetUserProjects(user).Any(p => p.GetBool("archived") && p.GetString("name") == activeProject))
            {
                ctx.SetUserPref("project", null, user);
                activeProject = null;
            }
            var paths = SetProjectDirectories(activeProject, user);
            Log.LogInformation("Projects [{User}] {Project}: {Paths}",
                user ?? "default", activeProject ?? "(none)", string.Join(", ", paths));
            return Task.CompletedTask;
        });

        ctx.Projects = this;
    }

    // ── Folder model (shared with PublishExtension) ──

    [GeneratedRegex(@"[^\w\s-]")] private static partial Regex NonSlugChars();
    [GeneratedRegex(@"[\s_]+")] private static partial Regex SlugSeparators();
    [GeneratedRegex(@"-+")] private static partial Regex RepeatedDashes();

    /// <summary>"My App (v2)" -> "my-app-v2" (port of kebab_case)</summary>
    public static string KebabCase(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        s = NonSlugChars().Replace(s, "");
        s = SlugSeparators().Replace(s, "-");
        s = RepeatedDashes().Replace(s, "-");
        return s.Trim('-').ToLowerInvariant();
    }

    /// <summary>A project's folder name, defaulting to a kebab-case slug of its name</summary>
    public static string GetProjectFolder(JsonObject? project) =>
        project.GetString("folder") is { } folder && !string.IsNullOrWhiteSpace(folder)
            ? folder.Trim()
            : KebabCase(project.GetString("name"));

    /// <summary>&lt;userPath&gt;/projects/&lt;folder&gt; — the only directory a project can access</summary>
    public static string GetProjectDir(string userPath, JsonObject project) =>
        ProjectsExplorer.PhysicalPath(Path.Combine(userPath, "projects", GetProjectFolder(project)));

    string UserProjectDir(string? user, JsonObject project) => GetProjectDir(Ctx.GetUserPath(user), project);

    /// <summary>
    /// Coerce a publish directory to a relative path inside the project folder (port of
    /// sanitize_publish_path). Absolute paths, a leading '/', a redundant '&lt;folder&gt;/' or
    /// 'projects/&lt;folder&gt;/' prefix and any '..' segments are all reduced away; the project root is "".
    /// </summary>
    public static string SanitizePublishPath(string? publish, string? projectDir = null)
    {
        if (string.IsNullOrWhiteSpace(publish))
            return "";
        publish = publish.Trim();

        if (!string.IsNullOrEmpty(projectDir))
        {
            var absProject = Path.GetFullPath(projectDir);
            var folderName = Path.GetFileName(absProject);

            if (Path.IsPathRooted(publish))
            {
                var absPublish = Path.GetFullPath(publish);
                if (absPublish == absProject)
                    return "";
                if (IsWithin(absPublish, absProject))
                    return JoinSegments(Path.GetRelativePath(absProject, absPublish));
            }

            var clean = publish.TrimStart('/', '\\');
            if (clean == folderName || clean == $"projects/{folderName}")
                return "";
            if (clean.StartsWith($"projects/{folderName}/", StringComparison.Ordinal))
                clean = clean[$"projects/{folderName}/".Length..];
            else if (clean.StartsWith($"{folderName}/", StringComparison.Ordinal))
                clean = clean[$"{folderName}/".Length..];
            return JoinSegments(clean);
        }

        // no project folder to resolve against: keep only what follows 'projects/<folder>/'
        var path = publish.TrimStart('/', '\\');
        var idx = path.LastIndexOf("projects/", StringComparison.Ordinal);
        if (idx >= 0)
        {
            var tail = path[(idx + "projects/".Length)..];
            var slash = tail.IndexOf('/');
            path = slash >= 0 ? tail[(slash + 1)..] : "";
        }
        return JoinSegments(path);
    }

    static readonly char[] PathSeparators = ['/', '\\'];

    static string JoinSegments(string path) =>
        string.Join('/', path.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p != "." && p != ".."));

    /// <summary>True when path is root or below it (both are compared as full paths)</summary>
    public static bool IsWithin(string path, string root)
    {
        var rootFull = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var pathFull = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(pathFull, rootFull, comparison)
            || pathFull.StartsWith(Path.EndsInDirectorySeparator(rootFull) ? rootFull : rootFull + Path.DirectorySeparatorChar, comparison);
    }

    // ── Persistence ──

    /// <summary>
    /// Serializes projects.json read-modify-write so concurrent requests can't assign competing IDs
    /// or lose projects. Writes are atomic (temp file + replace). A single web host owns App_Data, so
    /// this is an in-process lock rather than llms-py's cross-process file lock.
    /// </summary>
    readonly SemaphoreSlim projectsLock = new(1, 1);

    async Task<IDisposable> LockProjectsAsync()
    {
        await projectsLock.WaitAsync().ConfigAwait();
        return new Releaser(projectsLock);
    }

    sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }

    string ProjectsPath(string? user) =>
        Path.Combine(Ctx.GetUserPath(user), "projects", "projects.json");

    JsonArray GetUserProjectsJson(string? user)
    {
        projectsLock.Wait();
        try { return ReadUserProjectsJson(user); }
        finally { projectsLock.Release(); }
    }

    /// <summary>Read (caller holds <see cref="projectsLock"/>), persisting any newly assigned IDs</summary>
    JsonArray ReadUserProjectsJson(string? user)
    {
        var candidatePaths = new List<string>();
        if (user != null)
            candidatePaths.Add(ProjectsPath(user));
        candidatePaths.Add(ProjectsPath(null));

        foreach (var path in candidatePaths)
        {
            if (!File.Exists(path))
                continue;
            try
            {
                if (JsonNode.Parse(File.ReadAllText(path)) is JsonArray projects)
                {
                    // Backfill stable identities once, persisted to the file they were read from so
                    // repeated reads never invent different IDs for the same project.
                    var assigned = false;
                    foreach (var project in projects.OfType<JsonObject>())
                    {
                        if (project.GetString("id").IsNullOrEmpty())
                        {
                            project["id"] = Guid.NewGuid().ToString();
                            assigned = true;
                        }
                    }
                    if (assigned)
                        WriteProjectsFile(path, projects);
                    // migrate v3 projects saved before the folder model
                    foreach (var project in projects.OfType<JsonObject>())
                        NormalizeProject(project, user);
                    return projects;
                }
            }
            catch (Exception e)
            {
                Log.LogError(e, "Failed to parse projects.json");
                throw new HttpError(500, "ProjectStorageError", "Unable to read project configuration");
            }
        }
        return [];
    }

    public List<JsonObject> GetUserProjects(string? user = null) =>
        GetUserProjectsJson(user).OfType<JsonObject>().ToList();

    void WriteProjects(string? user, JsonArray projects) => WriteProjectsFile(ProjectsPath(user), projects);

    static void WriteProjectsFile(string path, JsonArray projects)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, projects.ToJsonString(ChatJson.Indented));
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    /// <summary>
    /// Keep identities across saves: match by ID, then by name for compatibility payloads that omit
    /// it, and carry the sidebar preference over when the payload doesn't mention it. Returns the
    /// IDs of projects that were removed. Rejects removing a project whose threads have active runs.
    /// </summary>
    HashSet<string> PreserveIds(JsonArray projects, List<JsonObject> previous, string? user)
    {
        var byId = previous.Where(p => p.GetString("id") != null).ToDictionary(p => p.GetString("id")!);
        var byName = previous.GroupBy(p => p.GetString("name") ?? "").ToDictionary(g => g.Key, g => g.First());
        var seen = new HashSet<string>();
        foreach (var project in projects.OfType<JsonObject>())
        {
            var existing = (project.GetString("id") is { } id ? byId.GetValueOrDefault(id) : null)
                ?? byName.GetValueOrDefault(project.GetString("name") ?? "");
            var projectId = existing?.GetString("id") ?? Guid.NewGuid().ToString();
            project["id"] = projectId;
            if (existing != null && !project.ContainsKey("showInSidebar") && existing.ContainsKey("showInSidebar"))
                project["showInSidebar"] = existing["showInSidebar"]?.DeepClone();
            PreserveServerFields(project, existing);
            if (!seen.Add(projectId))
                throw HttpError.BadRequest("Duplicate project");
        }
        var removed = byId.Keys.Where(x => !seen.Contains(x)).ToSet();
        if (removed.Count > 0 && Ctx.Feature.ChatDb is { } db && db.HasActiveRunsInProjects(removed, user))
            throw HttpError.Conflict("A project has an active run");
        return removed;
    }

    /// <summary>Project names, folders and visibility appear in the chat sidebar</summary>
    void NotifySidebar() => Ctx.NotifySidebar();

    /// <summary>Move chats of deleted projects to Recents (idempotent; also run by the sidebar)</summary>
    void ReconcileThreads(JsonArray projects, string? user)
    {
        Ctx.Feature.ChatDb?.ReconcileProjects(projects.OfType<JsonObject>()
            .Select(p => p.GetString("id")).Where(x => x != null).Select(x => x!).ToList(), user ?? ChatDb.DefaultUser);
    }

    /// <summary>Back-fill `folder` and keep `publish` relative to the project folder</summary>
    void NormalizeProject(JsonObject project, string? user)
    {
        project["folder"] = GetProjectFolder(project);
        if (project.ContainsKey("publish"))
            project["publish"] = SanitizePublishPath(project.GetString("publish"), UserProjectDir(user, project));
    }

    /// <summary>Normalize a project being saved and create its folder on disk</summary>
    void PrepareProjectForSave(JsonObject project, string? user)
    {
        NormalizeProject(project, user);
        project.Remove("paths"); // dropped in v4: a project is a single folder
        var projectDir = UserProjectDir(user, project);
        var projectsRoot = ProjectsExplorer.PhysicalPath(Path.Combine(Ctx.GetUserPath(user), "projects"));
        if (!IsWithin(projectDir, projectsRoot) || Path.GetFullPath(projectDir) == projectsRoot)
            throw HttpError.BadRequest("Project folder must be inside the projects directory");
        try
        {
            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
                Log.LogInformation("Created directory: {Dir}", projectDir);
            }
        }
        catch (Exception e)
        {
            Log.LogError(e, "Failed to create directory {Dir}", projectDir);
        }
    }

    async Task<object?> SaveProjectsAsync(ChatRequestContext req)
    {
        var user = req.UserName;
        var body = await req.GetJsonNodeBodyAsync().ConfigAwait();
        if (body is not JsonArray projects)
            throw new ArgumentException("Expected a JSON array of projects");

        using (await LockProjectsAsync().ConfigAwait())
        {
            var previous = ReadUserProjectsJson(user).OfType<JsonObject>().ToList();
            if (projects.Any(p => p is not JsonObject)) throw HttpError.BadRequest("Expected project objects");
            var submitted = projects.OfType<JsonObject>().SelectMany(p => new[] { p.GetString("id"), p.GetString("name") }).ToHashSet();
            foreach (var archived in previous.Where(p => p.GetBool("archived") && !submitted.Contains(p.GetString("id")) && !submitted.Contains(p.GetString("name"))))
                projects.Add(archived.Clone());
            PreserveIds(projects, previous, user);
            foreach (var project in projects.OfType<JsonObject>())
            {
                PrepareProjectForSave(project, user);
            }
            WriteProjects(user, projects);
        }
        ReconcileThreads(projects, user);

        // if the active project was deleted, reset the preference
        var activeProject = Ctx.GetUserPref("project", user)?.GetValue<string>();
        if (activeProject != null
            && !projects.OfType<JsonObject>().Any(p => p.GetString("name") == activeProject))
        {
            Ctx.SetUserPref("project", null, user);
            SetProjectDirectories(null, user);
            Log.LogInformation("Active project '{Project}' was deleted, resetting active project", activeProject);
        }
        NotifySidebar();
        return projects.Clone();
    }

    async Task<object?> SaveProjectAsync(ChatRequestContext req)
    {
        var user = req.UserName;
        var name = req.GetPathParam("name");
        var projectData = await req.GetJsonBodyAsync().ConfigAwait();

        if (projectData.GetString("name").IsNullOrEmpty())
            return ChatResult.Json(ChatJson.CreateErrorResponse("Project name is required"), 400);

        PrepareProjectForSave(projectData, user);

        JsonArray projects;
        using (await LockProjectsAsync().ConfigAwait())
        {
            projects = ReadUserProjectsJson(user);
            // matched by the original name route: an edit (including a rename) keeps its identity
            var existing = projects.OfType<JsonObject>().FirstOrDefault(p => p.GetString("name") == name);
            if (existing != null)
            {
                projectData["id"] = existing.GetString("id");
                PreserveServerFields(projectData, existing);
                if (!projectData.ContainsKey("showInSidebar") && existing.ContainsKey("showInSidebar"))
                    projectData["showInSidebar"] = existing["showInSidebar"]?.DeepClone();
                projects[projects.IndexOf(existing)] = projectData.Clone();
            }
            else
            {
                projectData["id"] = Guid.NewGuid().ToString();
                PreserveServerFields(projectData, null);
                projects.Add(projectData.Clone());
            }
            WriteProjects(user, projects);
        }

        // follow a rename of the active project
        var activeProject = Ctx.GetUserPref("project", user)?.GetValue<string>();
        if (activeProject == name)
        {
            var newName = projectData.GetString("name");
            if (newName != null && newName != activeProject)
            {
                Ctx.SetUserPref("project", newName, user);
                Log.LogInformation("Renamed active project from '{Old}' to '{New}'", activeProject, newName);
            }
            // the folder may have changed even when the name didn't
            SetProjectDirectories(newName ?? activeProject, user);
        }
        NotifySidebar();
        return projects.Clone();
    }

    /// <summary>PATCH sidebar/{id} {"showInSidebar": bool}: the user's folder visibility preference</summary>
    async Task<object?> SetSidebarVisibilityAsync(ChatRequestContext req)
    {
        var user = req.UserName;
        var data = await req.GetJsonNodeBodyAsync().ConfigAwait();
        if (data is not JsonObject obj || obj["showInSidebar"] is not JsonValue value
            || !value.TryGetValue<bool>(out var visible))
            throw HttpError.BadRequest("showInSidebar must be a boolean");
        var projectId = req.GetPathParam("id");
        using var _ = await LockProjectsAsync().ConfigAwait();
        var projects = ReadUserProjectsJson(user);
        var project = projects.OfType<JsonObject>().FirstOrDefault(p => p.GetString("id") == projectId)
            ?? throw HttpError.NotFound("Project not found");
        if (visible && project.GetBool("archived")) throw HttpError.Conflict("Unarchive this project before showing it in the sidebar.");
        project["showInSidebar"] = visible;
        WriteProjects(user, projects);
        NotifySidebar();
        return projects.Clone();
    }

    public JsonObject ResolveWorkspace(string projectId, string? user = null)
    {
        var project = GetUserProjects(user).FirstOrDefault(p => p.GetString("id") == projectId)
            ?? throw new ArgumentException("Project not found");
        var directory = UserProjectDir(user, project);
        var root = ProjectsExplorer.PhysicalPath(Path.Combine(Ctx.GetUserPath(user), "projects"));
        if (!IsWithin(directory, root) || directory == root)
            throw new ArgumentException("Project folder must be inside the projects directory");
        return new JsonObject { ["projectId"] = projectId, ["directories"] = new JsonArray(directory) };
    }

    async Task<object?> SetActiveProjectAsync(ChatRequestContext req)
    {
        var user = req.UserName;
        var data = await req.GetJsonBodyAsync().ConfigAwait();
        var name = data.GetString("name");

        if (name == null)
        {
            Ctx.SetUserPref("project", null, user);
            SetProjectDirectories(null, user);
            Log.LogInformation("Unselected active project");
            return JsonValue.Create((string?)null);
        }

        var project = GetUserProjects(user).FirstOrDefault(p => p.GetString("name") == name)
            ?? throw new Exception($"Project '{name}' not found");

        if (project.GetBool("archived")) throw HttpError.Conflict("Unarchive this project before selecting it.");
        Ctx.SetUserPref("project", name, user);
        var paths = SetProjectDirectories(name, user);
        Log.LogInformation("Switched active project to '{Name}': {Paths}", name, string.Join(", ", paths));
        return project.Clone();
    }

    /// <summary>
    /// Restrict the user's allowed directories to the active project's folder (port of
    /// set_project_directories). Without an active project a user gets their own workspace plus
    /// any host-shared ToolsConfig.AllowedDirectories, never another user's or a central folder.
    /// </summary>
    List<string> SetProjectDirectories(string? projectName, string? user)
    {
        var paths = Ctx.Feature.DefaultWorkspaceDirectories(user);
        if (projectName != null)
        {
            var project = GetUserProjects(user).FirstOrDefault(p => p.GetString("name") == projectName && !p.GetBool("archived"));
            paths = project != null ? [UserProjectDir(user, project)] : [];
        }
        Ctx.SetAllowedDirectories(paths, user);
        return paths;
    }
}
