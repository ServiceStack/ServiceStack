using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>Project output discovery shared by independently enabled sharing destinations.</summary>
public sealed class ProjectOutputRoutes(ExtensionContext ctx)
{
    public void Register()
    {
        ctx.AddGet("detect-dist", req => Task.FromResult<object?>(DetectDist(req.UserName, req.QueryString("threadId"))));
        ctx.AddGet("list-subdirs", req => Task.FromResult<object?>(ListSubdirs(req)));
    }

    // ── Project directory discovery ──

    JsonObject? ActiveProject(string? user)
    {
        var activeProject = ctx.GetUserPref("project", user)?.GetValue<string>();
        if (activeProject == null)
            return null;
        return ctx.Projects.GetUserProjects(user)
            .FirstOrDefault(p => p.GetString("name") == activeProject);
    }

    /// <summary>
    /// The project's publish directory, relative to its project folder ("" = project root). With a
    /// threadId it's the thread's own project, otherwise the user's standalone active project.
    /// </summary>
    JsonObject DetectDist(string? user, string? threadId = null)
    {
        JsonObject? project;
        if (!string.IsNullOrEmpty(threadId))
        {
            var thread = long.TryParse(threadId, out var id)
                ? ctx.Feature.ChatDb?.GetThread(id, user, includeMessages: false) : null;
            if (thread == null)
                throw HttpError.NotFound("Thread not found");
            var projectId = thread.ProjectId;
            project = projectId == null ? null
                : ctx.Projects.GetUserProjects(user).FirstOrDefault(p => p.GetString("id") == projectId);
        }
        else
        {
            project = ActiveProject(user);
        }
        if (project == null)
            return new JsonObject { ["dist"] = "" };

        var projectDir = ProjectsExtension.GetProjectDir(ctx.GetUserPath(user), project);
        var publish = ProjectsExtension.SanitizePublishPath(project.GetString("publish"), projectDir);
        if (publish.Length > 0)
            return new JsonObject { ["dist"] = publish };

        return new JsonObject { ["dist"] = Directory.Exists(Path.Combine(projectDir, "dist")) ? "dist" : "" };
    }

    /// <summary>
    /// Folder browser for the publish dialog, confined to the project folder. Every path in and out
    /// is relative to it, so the UI never sees a server path; `displayPath` is the ~/ label to show.
    /// </summary>
    object ListSubdirs(ChatRequestContext req)
    {
        var user = req.UserName;
        var pathParam = req.QueryString("path") ?? "";
        var projectParam = req.QueryString("project");

        var activeProject = !string.IsNullOrEmpty(projectParam)
            ? projectParam
            : ctx.GetUserPref("project", user)?.GetValue<string>();

        var userPath = Path.GetFullPath(ctx.GetUserPath(user));
        JsonObject? project = null;
        if (!string.IsNullOrEmpty(activeProject))
        {
            project = ctx.Projects.GetUserProjects(user).FirstOrDefault(p =>
                p.GetString("name") == activeProject || p.GetString("folder") == activeProject);
        }
        var projectDir = project != null
            ? ProjectsExtension.GetProjectDir(userPath, project)
            : userPath;

        var cleanRel = ProjectsExtension.SanitizePublishPath(pathParam, projectDir);
        var resolvedPath = Path.GetFullPath(Path.Combine(projectDir, cleanRel));

        if (!ProjectsExtension.IsWithin(resolvedPath, projectDir) || !Directory.Exists(resolvedPath))
        {
            return ChatResult.Json(new JsonObject
            {
                ["error"] = "Invalid or non-existent path",
                ["path"] = pathParam,
            }, 400);
        }

        var subdirs = new JsonArray();
        foreach (var dir in Directory.EnumerateDirectories(resolvedPath)
                     .Where(d => !Path.GetFileName(d).StartsWith('.'))
                     .OrderBy(d => Path.GetFileName(d).ToLowerInvariant()))
        {
            subdirs.Add(new JsonObject
            {
                ["name"] = Path.GetFileName(dir),
                ["path"] = ToRelative(dir, projectDir),
            });
        }

        var currentPath = ToRelative(resolvedPath, projectDir);

        // "" (the project root) is a valid parent, null means there's nowhere left to go up to
        string? parentPath = null;
        if (resolvedPath != projectDir)
        {
            var parentAbs = Path.GetDirectoryName(resolvedPath);
            if (parentAbs != null && ProjectsExtension.IsWithin(parentAbs, projectDir))
                parentPath = ToRelative(parentAbs, projectDir);
        }

        var userProjectsDir = Path.GetFullPath(Path.Combine(userPath, "projects"));
        var displayPath = ProjectsExtension.IsWithin(resolvedPath, userProjectsDir)
            ? "~/" + ToRelative(resolvedPath, userProjectsDir)
            : project != null
                ? $"~/{ProjectsExtension.GetProjectFolder(project)}"
                    + (currentPath.Length > 0 ? $"/{currentPath}" : "")
                : "~/" + Path.GetFileName(resolvedPath);

        return new JsonObject
        {
            ["currentPath"] = currentPath,
            ["displayPath"] = displayPath,
            ["parentPath"] = parentPath,
            ["subdirs"] = subdirs,
        };
    }

    /// <summary>Path relative to root, using '/' separators; the root itself is ""</summary>
    static string ToRelative(string path, string root)
    {
        var rel = Path.GetRelativePath(root, path);
        return rel == "." ? "" : rel.Replace(Path.DirectorySeparatorChar, '/');
    }

}
