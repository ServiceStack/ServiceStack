using ServiceStack.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public partial class ProjectsExtension
{
    public async Task UpdatePublicationAsync(JsonObject captured,string publishedUrl,string? user=null)
    {
        using var lease=await LockProjectsAsync().ConfigAwait();
        var projects=ReadUserProjectsJson(user);
        var current=projects.OfType<JsonObject>().FirstOrDefault(p=>p.GetString("id")==captured.GetString("id"));
        if(current==null || GetProjectDir(Ctx.GetUserPath(user),current)!=GetProjectDir(Ctx.GetUserPath(user),captured))return;
        current["publishedUrl"]=publishedUrl;
        WriteProjects(user,projects);
    }

    public async Task UpdateStaticPublicationAsync(JsonObject captured, JsonObject publication, string? user=null)
    {
        using var lease=await LockProjectsAsync().ConfigAwait();
        var projects=ReadUserProjectsJson(user);
        var current=projects.OfType<JsonObject>().FirstOrDefault(p=>p.GetString("id")==captured.GetString("id"))
            ?? throw HttpError.NotFound("Project not found");
        if(current.GetString("folder")!=captured.GetString("folder") || current.GetString("publish")!=captured.GetString("publish"))
            throw HttpError.Conflict("Project output settings changed during publishing. Retry publishing.");
        current["staticPublication"]=publication.DeepClone();
        WriteProjects(user,projects);
    }

    static void PreserveServerFields(JsonObject project, JsonObject? existing)
    {
        foreach (var key in new[] { "archived", "archivedSidebarVisibility", "gitSource", "staticPublication" })
        {
            if (existing?.ContainsKey(key) == true) project[key] = existing[key]?.DeepClone();
            else project.Remove(key);
        }
        if (project.GetBool("archived")) project["showInSidebar"] = false;
    }

    void RegisterOrganizationRoutes(ExtensionContext ctx)
    {
        ctx.AddPost("order", OrderProjectsAsync);
        ctx.AddPatch("archive/{id}", ArchiveProjectAsync);
    }

    async Task<object?> OrderProjectsAsync(ChatRequestContext req)
    {
        var user = req.AssertUserName();
        var body = await req.GetJsonNodeBodyAsync().ConfigAwait();
        if (body is not JsonObject obj || obj["ids"] is not JsonArray ids
            || ids.Any(id => id is not JsonValue v || !v.TryGetValue<string>(out _)))
            throw HttpError.BadRequest("Provide each active project ID once.");
        var order = ids.Select(x => x!.GetValue<string>()).ToList();
        if (order.Count != order.Distinct(StringComparer.Ordinal).Count())
            throw HttpError.BadRequest("Provide each active project ID once.");
        using var lease = await LockProjectsAsync().ConfigAwait();
        var projects = ReadUserProjectsJson(user);
        var active = projects.OfType<JsonObject>().Where(p => !p.GetBool("archived")).ToDictionary(p => p.GetString("id")!);
        if (!order.ToHashSet(StringComparer.Ordinal).SetEquals(active.Keys))
            throw HttpError.Conflict("The project list changed. Refresh it and try again.");
        var sorted = new JsonArray(order.Select(id => (JsonNode)active[id].Clone())
            .Concat(projects.OfType<JsonObject>().Where(p => p.GetBool("archived")).Select(p => (JsonNode)p.Clone())).ToArray());
        WriteProjects(user, sorted);
        NotifySidebar();
        return sorted;
    }

    async Task<object?> ArchiveProjectAsync(ChatRequestContext req)
    {
        var user = req.AssertUserName();
        var body = await req.GetJsonNodeBodyAsync().ConfigAwait();
        if (body is not JsonObject obj || obj["archived"] is not JsonValue v || !v.TryGetValue<bool>(out var archived))
            throw HttpError.BadRequest("archived must be a boolean");
        JsonArray projects;
        var clearActive = false;
        using (await LockProjectsAsync().ConfigAwait())
        {
            projects = ReadUserProjectsJson(user);
            var project = projects.OfType<JsonObject>().FirstOrDefault(p => p.GetString("id") == req.GetPathParam("id"))
                ?? throw HttpError.NotFound("Project not found");
            if (project.GetBool("archived") != archived)
            {
                if (archived)
                {
                    project["archivedSidebarVisibility"] = project["showInSidebar"]?.DeepClone() ?? JsonValue.Create(true);
                    project["showInSidebar"] = false;
                }
                else
                {
                    project["showInSidebar"] = project["archivedSidebarVisibility"]?.DeepClone() ?? JsonValue.Create(true);
                    project.Remove("archivedSidebarVisibility");
                }
                project["archived"] = archived;
                projects.Remove(project);
                var index = archived ? projects.Count : projects.TakeWhile(p => !(p as JsonObject).GetBool("archived")).Count();
                projects.Insert(index, project);
                WriteProjects(user, projects);
            }
            clearActive = archived && Ctx.GetUserPref("project", user)?.GetValue<string>() == project.GetString("name");
        }
        if (clearActive)
        {
            Ctx.SetUserPref("project", null, user);
            SetProjectDirectories(null, user);
        }
        NotifySidebar();
        return projects.Clone();
    }
}
