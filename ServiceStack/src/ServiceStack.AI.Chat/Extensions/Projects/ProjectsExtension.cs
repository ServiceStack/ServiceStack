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
public partial class ProjectsExtension() : ChatExtension("projects"), IProjectsApi
{
    public override void Install(ExtensionContext ctx)
    {
        ctx.AddGet("projects.json", req =>
            Task.FromResult<object?>(GetUserProjectsJson(req.UserName)));

        ctx.AddPost("projects.json", SaveProjectsAsync);
        ctx.AddPost("save/{name}", SaveProjectAsync);
        ctx.AddPost("active", SetActiveProjectAsync);

        // first-time user setup: apply their active project's directory
        ctx.RegisterSetupUserHandler(request =>
        {
            var user = ctx.GetUserName(request);
            var activeProject = ctx.GetUserPref("project", user)?.GetValue<string>();
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
        Path.GetFullPath(Path.Combine(userPath, "projects", GetProjectFolder(project)));

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
        var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        var pathFull = Path.GetFullPath(path);
        return pathFull == rootFull
            || pathFull.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    // ── Persistence ──

    string ProjectsPath(string? user) =>
        Path.Combine(Ctx.GetUserPath(user), "projects", "projects.json");

    JsonArray GetUserProjectsJson(string? user)
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
                    // migrate v3 projects saved before the folder model
                    foreach (var project in projects.OfType<JsonObject>())
                        NormalizeProject(project, user);
                    return projects;
                }
            }
            catch (Exception e)
            {
                Log.LogError(e, "Failed to parse projects.json");
            }
        }
        return [];
    }

    public List<JsonObject> GetUserProjects(string? user = null) =>
        GetUserProjectsJson(user).OfType<JsonObject>().ToList();

    void WriteProjects(string? user, JsonArray projects)
    {
        var path = ProjectsPath(user);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, projects.ToJsonString(ChatJson.Indented));
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

        foreach (var project in projects.OfType<JsonObject>())
        {
            PrepareProjectForSave(project, user);
        }
        WriteProjects(user, projects);

        // if the active project was deleted, reset the preference
        var activeProject = Ctx.GetUserPref("project", user)?.GetValue<string>();
        if (activeProject != null
            && !projects.OfType<JsonObject>().Any(p => p.GetString("name") == activeProject))
        {
            Ctx.SetUserPref("project", null, user);
            SetProjectDirectories(null, user);
            Log.LogInformation("Active project '{Project}' was deleted, resetting active project", activeProject);
        }
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

        var projects = GetUserProjectsJson(user);
        var existing = projects.OfType<JsonObject>().FirstOrDefault(p => p.GetString("name") == name);
        if (existing != null)
        {
            projects[projects.IndexOf(existing)] = projectData.Clone();
        }
        else
        {
            projects.Add(projectData.Clone());
        }
        WriteProjects(user, projects);

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
        return projects.Clone();
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

        Ctx.SetUserPref("project", name, user);
        var paths = SetProjectDirectories(name, user);
        Log.LogInformation("Switched active project to '{Name}': {Paths}", name, string.Join(", ", paths));
        return project.Clone();
    }

    /// <summary>
    /// Restrict the user's allowed directories to the active project's folder (port of
    /// set_project_directories). llms-py grants nothing when there's no active project; this host
    /// instead falls back to the explicitly configured ToolsConfig.AllowedDirectories, which is
    /// empty by default and is the only thing that enables the filesystem tools here anyway.
    /// </summary>
    List<string> SetProjectDirectories(string? projectName, string? user)
    {
        var paths = Ctx.Tools.AllowedDirectories.ToList();
        if (projectName != null)
        {
            var project = GetUserProjects(user).FirstOrDefault(p => p.GetString("name") == projectName);
            paths = project != null ? [UserProjectDir(user, project)] : [];
        }
        Ctx.SetAllowedDirectories(paths, user);
        return paths;
    }
}
