using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Agent profiles / personas (port of llms-py's "agents" extension). Bundled profiles
/// (chat, coder, planner) are synced to chat/profiles/**; per-user profiles live under
/// App_Data/chat/user/&lt;user&gt;/profiles/ and override bundled ones of the same name.
/// </summary>
public class AgentsExtension : IChatExtension
{
    public string Name => ChatExtension.Agents;

    ExtensionContext ctx = null!;

    public void Install(ExtensionContext ctx)
    {
        this.ctx = ctx;

        ctx.AddGet("", req => Task.FromResult<object?>(GetProfiles(req)));
        ctx.AddGet("{profile}/system", req => Task.FromResult(GetProfileSystemPrompt(req))!);
        ctx.AddGet("{profile}/avatar", req => Task.FromResult(GetProfileAvatar(req))!);
        ctx.AddGet("{profile}/actions", req => Task.FromResult<object?>(GetProfileActions(req)));
    }

    /// <summary>Filesystem profile roots, lowest precedence first (bundled profiles come from the VFS)</summary>
    List<string> ProfileRoots(ChatRequestContext req)
    {
        var roots = new List<string> { Path.Combine(ctx.GetUserPath(), "profiles") };
        if (req.UserName is { } user)
            roots.Add(Path.Combine(ctx.GetUserPath(user), "profiles"));
        return roots;
    }

    /// <summary>Resolve a profile dir on disk, most specific first. Null when only bundled.</summary>
    string? ResolveProfilePath(ChatRequestContext req, string profile)
    {
        if (profile.Contains("..") || profile.Contains('/'))
            return null;
        foreach (var root in ProfileRoots(req).AsEnumerable().Reverse())
        {
            var path = Path.Combine(root, profile);
            if (Directory.Exists(path))
                return path;
        }
        return null;
    }

    JsonObject GetProfiles(ChatRequestContext req)
    {
        var ret = new JsonObject();

        void AddProfile(string name, string? configJson)
        {
            if (ChatJson.TryParseObject(configJson) is not { } config)
                return;
            // profiles are enabled unless explicitly disabled
            if (config.TryGetPropertyValue("enabled", out _) && !config.GetBool("enabled"))
            {
                ret.Remove(name);
                return;
            }
            ret[name] = config;
        }

        // bundled profiles synced from llms-py
        foreach (var dir in HostContext.VirtualFileSources.GetDirectory("chat/profiles")?.Directories ?? [])
        {
            AddProfile(dir.Name,
                HostContext.VirtualFileSources.GetFile($"chat/profiles/{dir.Name}/config.json")?.ReadAllText());
        }

        // user profiles override bundled ones
        foreach (var root in ProfileRoots(req))
        {
            if (!Directory.Exists(root))
                continue;
            foreach (var profileDir in Directory.GetDirectories(root))
            {
                var configPath = Path.Combine(profileDir, "config.json");
                if (File.Exists(configPath))
                    AddProfile(Path.GetFileName(profileDir), File.ReadAllText(configPath));
            }
        }
        return ret;
    }

    /// <summary>
    /// The profile's system prompt: renders SYSTEM.template with {VAR} substitutions from its
    /// sibling .md files plus MEMORY_LATEST (newest memory/*.md), else returns SYSTEM.md.
    /// </summary>
    object GetProfileSystemPrompt(ChatRequestContext req)
    {
        var profile = req.GetPathParam("profile");
        var profilePath = ResolveProfilePath(req, profile);

        if (profilePath != null)
        {
            var templatePath = Path.Combine(profilePath, "SYSTEM.template");
            if (File.Exists(templatePath))
            {
                var template = File.ReadAllText(templatePath);
                var vars = new Dictionary<string, string>();
                foreach (var mdFile in Directory.GetFiles(profilePath, "*.md"))
                {
                    vars[Path.GetFileNameWithoutExtension(mdFile)] = File.ReadAllText(mdFile);
                }

                var memoryPath = Path.Combine(profilePath, "memory");
                if (Directory.Exists(memoryPath))
                {
                    // ISO-dated filenames sort correctly, newest last
                    var latest = Directory.GetFiles(memoryPath, "*.md").OrderBy(x => x, StringComparer.Ordinal).LastOrDefault();
                    if (latest != null)
                        vars["MEMORY_LATEST"] = File.ReadAllText(latest);
                }
                vars.TryAdd("MEMORY_LATEST", "");

                var rendered = template;
                foreach (var entry in vars)
                {
                    rendered = rendered.Replace("{" + entry.Key + "}", entry.Value);
                }
                return new ChatResult { Text = rendered, ContentType = MimeTypes.PlainText };
            }

            var systemMdPath = Path.Combine(profilePath, "SYSTEM.md");
            if (File.Exists(systemMdPath))
                return new ChatResult { Text = File.ReadAllText(systemMdPath), ContentType = MimeTypes.PlainText };
        }

        var bundled = HostContext.VirtualFileSources.GetFile($"chat/profiles/{profile}/SYSTEM.md");
        if (bundled != null)
            return new ChatResult { Text = bundled.ReadAllText(), ContentType = MimeTypes.PlainText };

        return ChatResult.NotFound($"SYSTEM.md or SYSTEM.template not found for profile '{profile}'");
    }

    static readonly Dictionary<string, string> AvatarExtensions = new()
    {
        ["png"] = "image/png",
        ["webp"] = "image/webp",
        ["jpg"] = "image/jpeg",
        ["jpeg"] = "image/jpeg",
        ["svg"] = "image/svg+xml",
    };

    object GetProfileAvatar(ChatRequestContext req)
    {
        var profile = req.GetPathParam("profile");
        var profilePath = ResolveProfilePath(req, profile);
        const string cacheControl = "public, max-age=3600";

        if (profilePath != null)
        {
            foreach (var entry in AvatarExtensions)
            {
                var path = Path.Combine(profilePath, $"avatar.{entry.Key}");
                if (File.Exists(path))
                {
                    return new ChatFileResult(path)
                    {
                        ContentType = entry.Value,
                        Headers = new() { ["Cache-Control"] = cacheControl },
                    };
                }
            }
        }

        foreach (var entry in AvatarExtensions)
        {
            var file = HostContext.VirtualFileSources.GetFile($"chat/profiles/{profile}/avatar.{entry.Key}");
            if (file != null)
            {
                return new ChatResult
                {
                    Body = file.ReadAllBytes(),
                    ContentType = entry.Value,
                    Headers = new() { ["Cache-Control"] = cacheControl },
                };
            }
        }

        // fall back to the extension's default avatar
        var defaultAvatar = HostContext.VirtualFileSources.GetFile("chat/ext/agents/avatar.svg");
        return defaultAvatar != null
            ? new ChatResult
            {
                Body = defaultAvatar.ReadAllBytes(),
                ContentType = "image/svg+xml",
                Headers = new() { ["Cache-Control"] = cacheControl },
            }
            : ChatResult.NotFound();
    }

    /// <summary>
    /// A profile's actions, filtered by their conditions. Only "file" conditions are evaluated
    /// (glob match within the user's allowed directories); unconditional actions always pass.
    /// </summary>
    JsonObject GetProfileActions(ChatRequestContext req)
    {
        var profile = req.GetPathParam("profile");
        var profilePath = ResolveProfilePath(req, profile);

        var configJson = profilePath != null && File.Exists(Path.Combine(profilePath, "config.json"))
            ? File.ReadAllText(Path.Combine(profilePath, "config.json"))
            : HostContext.VirtualFileSources.GetFile($"chat/profiles/{profile}/config.json")?.ReadAllText();

        var actions = ChatJson.TryParseObject(configJson).GetObject("actions");
        if (actions == null)
            return new JsonObject();

        var user = req.UserName;
        var validActions = new JsonObject();
        foreach (var entry in actions)
        {
            if (entry.Value is not JsonObject action)
                continue;

            var condition = action.GetObject("condition");
            var type = condition.GetString("type");
            var match = condition.GetString("glob");
            if (condition == null || string.IsNullOrEmpty(type) || string.IsNullOrEmpty(match))
            {
                validActions[entry.Key] = action.Clone();
                continue;
            }

            if (type != "file")
            {
                ctx.Log.LogInformation("Unknown condition type: {Type}", type);
                continue;
            }

            var shouldExist = condition.GetBool("exists");
            var fileExists = GlobExists(match, user);
            if (shouldExist == fileExists)
                validActions[entry.Key] = action.Clone();
        }
        return validActions;
    }

    bool GlobExists(string match, string? user)
    {
        foreach (var dir in ctx.ResolveAllowedDirectories(user))
        {
            if (!Directory.Exists(dir))
                continue;
            var pattern = match.TrimEnd('/');
            var combined = Path.Combine(dir, pattern);
            if (File.Exists(combined) || Directory.Exists(combined))
                return true;
            try
            {
                var searchDir = Path.GetDirectoryName(combined);
                var searchPattern = Path.GetFileName(combined);
                if (!string.IsNullOrEmpty(searchDir) && !string.IsNullOrEmpty(searchPattern)
                    && Directory.Exists(searchDir)
                    && Directory.EnumerateFileSystemEntries(searchDir, searchPattern).Any())
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                ctx.Log.LogDebug("glob '{Match}' in '{Dir}' failed: {Message}", match, dir, e.Message);
            }
        }
        return false;
    }
}
