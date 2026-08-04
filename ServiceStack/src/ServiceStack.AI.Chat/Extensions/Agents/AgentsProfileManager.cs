using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// The Profile Manager: create, edit and delete agent profiles in the user's own profiles folder.
/// <para>
/// Bundled profiles synced from llms-py are read-only — saving over one writes a user copy of the
/// same name into App_Data/chat/user/&lt;user&gt;/profiles/, which then takes precedence everywhere.
/// (Per-profile model/theme overrides are a UI preference and never reach the server.)
/// </para>
/// </summary>
public partial class AgentsExtension
{
    /// <summary>Prompt files a profile is made of, the only ones the manager will read or write</summary>
    public const string SystemTemplate = "SYSTEM.template";

    /// <summary>Non-alphanumeric runs collapse to '-', matching the Python profile id slug</summary>
    [GeneratedRegex("[^a-zA-Z0-9]+")]
    private static partial Regex SlugRegex();

    // ── Paths ──

    /// <summary>Where this user's own profiles live, whether or not they're signed in</summary>
    string UserProfilesRoot(ChatRequestContext req) => Path.Combine(Ctx.GetUserPath(req.UserName), "profiles");

    string UserProfileDir(ChatRequestContext req, string profile) =>
        Path.Combine(UserProfilesRoot(req), AssertProfileName(profile));

    /// <summary>A profile shipped with the App, which can only be edited by copying it</summary>
    static bool IsBuiltInProfile(string profile) =>
        HostContext.VirtualFileSources.GetDirectory($"chat/profiles/{profile}") != null;

    static string AssertProfileName(string profile)
    {
        if (string.IsNullOrEmpty(profile) || profile.Contains("..")
            || profile.Contains('/') || profile.Contains('\\'))
            throw new ArgumentException($"Invalid profile '{profile}'");
        return profile;
    }

    /// <summary>Only .md prompt files and SYSTEM.template are editable</summary>
    static string AssertFileName(string filename, bool markdownOnly = false)
    {
        var name = Path.GetFileName(filename ?? "");
        var isValid = markdownOnly
            ? name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            : name.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || name == SystemTemplate;
        if (!isValid)
            throw new ArgumentException(markdownOnly ? "Can only delete .md files" : "Invalid file type");
        return name;
    }

    /// <summary>
    /// Read-only until the user has their own copy: the bundled profiles are part of the App, and
    /// on a published site they aren't writable at all.
    /// </summary>
    ChatResult? AssertWritable(ChatRequestContext req, string profile)
    {
        if (IsBuiltInProfile(profile) && !Directory.Exists(UserProfileDir(req, profile)))
            return Forbidden("Built-in profiles are read-only");
        return null;
    }

    static ChatResult Forbidden(string message) =>
        ChatResult.Json(ChatJson.CreateErrorResponse(message, "Forbidden"), 403);

    static ChatResult NotFound(string message) =>
        ChatResult.Json(ChatJson.CreateErrorResponse(message, "NotFound"), 404);

    static ChatResult BadRequest(string message) =>
        ChatResult.Json(ChatJson.CreateErrorResponse(message, "BadRequest"), 400);

    // ── Profile files ──

    /// <summary>
    /// A profile's prompt files, in the order the editor lists them: the template first, then the
    /// system prompt, then everything else alphabetically.
    /// </summary>
    static List<string> SortProfileFiles(IEnumerable<string> fileNames) => fileNames
        .OrderBy(x => x == SystemTemplate ? 0 : x == "SYSTEM.md" ? 1 : 2)
        .ThenBy(x => x == SystemTemplate || x == "SYSTEM.md" ? x : x.ToLower(), StringComparer.Ordinal)
        .ToList();

    static bool IsProfileFile(string name) =>
        name.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || name == SystemTemplate;

    static List<string> DiskProfileFiles(string profileDir) => Directory.Exists(profileDir)
        ? SortProfileFiles(Directory.EnumerateFiles(profileDir).Select(Path.GetFileName)
            .Where(name => name != null && IsProfileFile(name))!)
        : [];

    static List<string> BundledProfileFiles(string profile) => SortProfileFiles(
        (HostContext.VirtualFileSources.GetDirectory($"chat/profiles/{profile}")?.Files ?? [])
        .Select(x => x.Name).Where(IsProfileFile));

    /// <summary>The files of a profile as it's actually resolved: the user's copy, else the bundled one</summary>
    List<string> ProfileFiles(ChatRequestContext req, string profile) =>
        ResolveProfilePath(req, profile) is { } dir ? DiskProfileFiles(dir) : BundledProfileFiles(profile);

    // ── Routes ──

    async Task<object?> CreateProfileAsync(ChatRequestContext req)
    {
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var name = (body.GetString("name") ?? "").Trim();
        if (name.Length == 0)
            return BadRequest("Profile name is required");

        var profileId = SlugRegex().Replace(name.ToLower(), "-").Trim('-');
        if (profileId.Length == 0)
            profileId = $"agent-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        var profileDir = UserProfileDir(req, profileId);
        if (Directory.Exists(profileDir) || IsBuiltInProfile(profileId))
            return BadRequest($"Profile '{profileId}' already exists");

        Directory.CreateDirectory(profileDir);
        var config = new JsonObject
        {
            ["name"] = name,
            ["model"] = null,
            ["theme"] = null,
            ["onlySkills"] = null,
            ["onlyTools"] = null,
        };
        WriteConfig(profileDir, config);
        await File.WriteAllTextAsync(Path.Combine(profileDir, "SYSTEM.md"), "").ConfigAwait();

        Log.LogInformation("Created profile {Profile} for {User}", profileId, req.UserName);
        return new JsonObject
        {
            ["status"] = "ok",
            ["id"] = profileId,
            ["name"] = name,
            ["config"] = config.Clone(),
        };
    }

    object DeleteProfile(ChatRequestContext req)
    {
        var profile = AssertProfileName(req.GetPathParam("profile"));
        var profileDir = UserProfileDir(req, profile);

        if (profile == "default" || (IsBuiltInProfile(profile) && !Directory.Exists(profileDir)))
            return Forbidden("Built-in profiles cannot be deleted");
        if (!Directory.Exists(profileDir))
            return NotFound("Profile not found");

        Directory.Delete(profileDir, recursive: true);
        Log.LogInformation("Deleted profile {Profile} for {User}", profile, req.UserName);
        return new JsonObject { ["status"] = "ok", ["id"] = profile };
    }

    /// <summary>The tools and skills a profile can be restricted to</summary>
    JsonObject GetToolsAndSkills(ChatRequestContext req)
    {
        var tools = Feature.Tools.Tools.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();

        var skills = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in Feature.Skills.ResolveAllSkills(req.UserName))
        {
            skills.Add(entry.Key);
        }
        // skills registered as tools count too, however they were installed
        foreach (var name in Feature.Tools.Groups.GetValueOrDefault("skills") ?? [])
        {
            skills.Add(name);
        }

        return new JsonObject
        {
            ["tools"] = new JsonArray(tools.Select(x => (JsonNode)x).ToArray()),
            ["skills"] = new JsonArray(skills.OrderBy(x => x, StringComparer.Ordinal)
                .Select(x => (JsonNode)x).ToArray()),
        };
    }

    async Task<object?> UpdateProfileConfigAsync(ChatRequestContext req)
    {
        var profile = AssertProfileName(req.GetPathParam("profile"));
        if (AssertWritable(req, profile) is { } forbidden)
            return forbidden;

        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var profileDir = UserProfileDir(req, profile);
        Directory.CreateDirectory(profileDir);

        var configPath = Path.Combine(profileDir, "config.json");
        var config = File.Exists(configPath)
            ? ChatJson.TryParseObject(await File.ReadAllTextAsync(configPath).ConfigAwait()) ?? new JsonObject()
            : new JsonObject();

        // only the fields the manager edits, so nothing else in the config is disturbed
        foreach (var key in new[] { "name", "model", "theme", "onlyTools", "onlySkills" })
        {
            if (body.TryGetPropertyValue(key, out var value))
                config[key] = value?.DeepClone();
        }

        WriteConfig(profileDir, config);
        return config;
    }

    static void WriteConfig(string profileDir, JsonObject config) =>
        File.WriteAllText(Path.Combine(profileDir, "config.json"), config.ToJsonString(ChatJson.Indented));

    object ListProfileFiles(ChatRequestContext req)
    {
        var profile = AssertProfileName(req.GetPathParam("profile"));
        if (ResolveProfilePath(req, profile) == null && !IsBuiltInProfile(profile))
            return NotFound("Profile not found");
        var files = ProfileFiles(req, profile);
        return new JsonArray(files.Select(x => (JsonNode)x).ToArray());
    }

    object GetProfileFile(ChatRequestContext req)
    {
        var profile = AssertProfileName(req.GetPathParam("profile"));
        var filename = AssertFileName(req.GetPathParam("filename"));

        if (ResolveProfilePath(req, profile) is { } profileDir)
        {
            var path = Path.Combine(profileDir, filename);
            if (File.Exists(path))
                return new ChatResult { Text = File.ReadAllText(path), ContentType = MimeTypes.PlainText };
        }

        var bundled = HostContext.VirtualFileSources.GetFile($"chat/profiles/{profile}/{filename}");
        return bundled != null
            ? new ChatResult { Text = bundled.ReadAllText(), ContentType = MimeTypes.PlainText }
            : NotFound("File not found");
    }

    async Task<object?> SaveProfileFileAsync(ChatRequestContext req)
    {
        var profile = AssertProfileName(req.GetPathParam("profile"));
        if (AssertWritable(req, profile) is { } forbidden)
            return forbidden;
        var filename = AssertFileName(req.GetPathParam("filename"));

        var profileDir = UserProfileDir(req, profile);
        Directory.CreateDirectory(profileDir);
        var content = await req.Request.GetRawBodyAsync().ConfigAwait() ?? "";
        await File.WriteAllTextAsync(Path.Combine(profileDir, filename), content).ConfigAwait();

        return new JsonObject { ["status"] = "ok", ["filename"] = filename };
    }

    async Task<object?> CreateProfileFileAsync(ChatRequestContext req)
    {
        var profile = AssertProfileName(req.GetPathParam("profile"));
        if (AssertWritable(req, profile) is { } forbidden)
            return forbidden;

        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var filename = Path.GetFileName((body.GetString("filename") ?? "").Trim());
        if (filename.Length == 0)
            return BadRequest("Filename is required");

        var profileDir = UserProfileDir(req, profile);
        Directory.CreateDirectory(profileDir);
        var templatePath = Path.Combine(profileDir, SystemTemplate);
        var systemMdPath = Path.Combine(profileDir, "SYSTEM.md");
        var content = body.GetString("content") ?? "";

        // A profile has one system prompt in one of two forms, so asking for the other form
        // converts what's there rather than leaving the profile with both.
        if (filename is SystemTemplate or SystemTemplate + ".md")
        {
            filename = SystemTemplate;
            if (File.Exists(systemMdPath))
                File.Move(systemMdPath, templatePath, overwrite: true);
            else if (!File.Exists(templatePath))
                await File.WriteAllTextAsync(templatePath, content).ConfigAwait();
        }
        else if (filename == "SYSTEM.md")
        {
            if (File.Exists(templatePath))
                File.Move(templatePath, systemMdPath, overwrite: true);
            else if (!File.Exists(systemMdPath))
                await File.WriteAllTextAsync(systemMdPath, content).ConfigAwait();
        }
        else
        {
            if (!filename.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                filename += ".md";
            await File.WriteAllTextAsync(Path.Combine(profileDir, filename), content).ConfigAwait();
        }

        return new JsonObject { ["status"] = "ok", ["filename"] = filename };
    }

    object DeleteProfileFile(ChatRequestContext req)
    {
        var profile = AssertProfileName(req.GetPathParam("profile"));
        if (AssertWritable(req, profile) is { } forbidden)
            return forbidden;
        var filename = AssertFileName(req.GetPathParam("filename"), markdownOnly: true);

        var path = Path.Combine(UserProfileDir(req, profile), filename);
        if (File.Exists(path))
            File.Delete(path);

        return new JsonObject { ["status"] = "ok", ["filename"] = filename };
    }

    // ── Avatars ──

    async Task<object?> UploadProfileAvatarAsync(ChatRequestContext req)
    {
        var profile = AssertProfileName(req.GetPathParam("profile"));
        if (AssertWritable(req, profile) is { } forbidden)
            return forbidden;

        var profileDir = UserProfileDir(req, profile);
        Directory.CreateDirectory(profileDir);

        // the UI posts a multipart file; a raw image body is accepted too, typed by Content-Type
        var file = req.Request.Files.FirstOrDefault(x => x.Name == "file") ?? req.Request.Files.FirstOrDefault();
        byte[] bytes;
        string ext;
        if (file != null)
        {
            ext = NormalizeAvatarExt((file.FileName ?? "").LastRightPart('.'));
            using var ms = new MemoryStream();
            await file.InputStream.CopyToAsync(ms).ConfigAwait();
            bytes = ms.ToArray();
        }
        else
        {
            var contentType = req.Request.ContentType ?? "";
            ext = contentType.Contains("webp") ? "webp"
                : contentType.Contains("jpeg") || contentType.Contains("jpg") ? "jpg"
                : contentType.Contains("svg") ? "svg"
                : "png";
            bytes = await req.Request.InputStream.ReadFullyAsync().ConfigAwait();
        }
        if (bytes.Length == 0)
            return BadRequest("No image data");

        // one avatar per profile, whatever it was saved as before
        foreach (var oldExt in AvatarExtensions.Keys)
        {
            var oldPath = Path.Combine(profileDir, $"avatar.{oldExt}");
            if (File.Exists(oldPath))
                File.Delete(oldPath);
        }

        await File.WriteAllBytesAsync(Path.Combine(profileDir, $"avatar.{ext}"), bytes).ConfigAwait();
        return new JsonObject { ["status"] = "ok", ["avatar"] = $"avatar.{ext}" };
    }

    static string NormalizeAvatarExt(string? ext)
    {
        var to = (ext ?? "").ToLower();
        return AvatarExtensions.ContainsKey(to) ? to : "png";
    }

    /// <summary>Stable per-profile colour, so a generated avatar keeps the same one</summary>
    public static string GetProfileColor(string name)
    {
        string[] colors =
        [
            "#3b82f6", "#10b981", "#8b5cf6", "#f59e0b", "#ef4444",
            "#ec4899", "#06b6d4", "#6366f1", "#14b8a6", "#f97316",
        ];
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(name));
        // the Python takes the whole md5 as one big-endian integer; its last byte decides the
        // colour whenever the palette length divides 256 evenly, but keep the full value to match
        var value = System.Numerics.BigInteger.Abs(
            new System.Numerics.BigInteger(hash.Reverse().Append((byte)0).ToArray()));
        return colors[(int)(value % colors.Length)];
    }

    /// <summary>An initial on a coloured circle, for a profile with no avatar of its own</summary>
    static ChatResult GeneratedAvatar(string profileName)
    {
        var initial = profileName.Trim().Length > 0
            ? char.ToUpper(profileName.Trim()[0]).ToString()
            : "A";
        var svg = $"""
                   <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">
                       <circle cx="32" cy="32" r="32" fill="{GetProfileColor(profileName)}"/>
                       <text x="32" y="42" font-size="30" font-family="system-ui, -apple-system, sans-serif" font-weight="bold" fill="#ffffff" text-anchor="middle">{initial.HtmlEncode()}</text>
                   </svg>
                   """;
        return new ChatResult
        {
            Text = svg,
            ContentType = "image/svg+xml",
            Headers = new Dictionary<string, string> { ["Cache-Control"] = "no-cache" },
        };
    }
}
