using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// File storage under {ContentRoot}/App_Data/chat — the C# equivalent of llms-py's ~/.llms home dir:
///   llms.json, providers.json, providers-extra.json   config (seeded from embedded resources on first run)
///   cache/&lt;2ch&gt;/&lt;sha256&gt;.&lt;ext&gt; (+ .info.json sidecars)  content-addressed asset cache
///   user/&lt;username&gt;/...                                per-user files (prefs.json, projects/, profiles/, ...)
/// </summary>
public class ChatAppData(string basePath)
{
    public string BasePath { get; } = Path.GetFullPath(basePath);

    public string GetHomePath(string relativePath = "")
    {
        if (string.IsNullOrEmpty(relativePath))
            return BasePath;
        var fullPath = Path.GetFullPath(Path.Combine(BasePath, relativePath));
        if (!fullPath.StartsWith(BasePath, StringComparison.Ordinal))
            throw new UnauthorizedAccessException($"Invalid path: {relativePath}");
        return fullPath;
    }

    public string GetCachePath(string relativePath = "") =>
        GetHomePath(string.IsNullOrEmpty(relativePath) ? "cache" : $"cache/{relativePath}");

    /// <summary>user/&lt;username&gt;, or user/default when user is null (Python parity)</summary>
    public string GetUserPath(string? user = null) =>
        GetHomePath(Path.Combine("user", string.IsNullOrEmpty(user) ? "default" : user));

    public string GetUserFilePath(string? user, string relativePath) =>
        Path.Combine(GetUserPath(user), relativePath);

    public string? TextFromFile(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

    public JsonNode? JsonFromFile(string path)
    {
        var text = TextFromFile(path);
        return text == null ? null : ChatJson.Parse(text);
    }

    public void WriteTextFile(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    public JsonObject GetUserPrefs(string? user)
    {
        var prefsPath = GetUserFilePath(user, "prefs.json");
        return ChatJson.TryParseObject(TextFromFile(prefsPath)) ?? new JsonObject();
    }

    public void SetUserPref(string key, JsonNode? value, string? user)
    {
        var prefs = GetUserPrefs(user);
        prefs[key] = value?.DeepClone();
        WriteTextFile(GetUserFilePath(user, "prefs.json"), prefs.ToJsonString(ChatJson.Indented));
    }

    /// <summary>Seed a home file from an embedded resource if it doesn't exist yet. Returns its content.</summary>
    public string SeedFile(string fileName, Func<string> resolveDefaultContent)
    {
        var path = GetHomePath(fileName);
        var existing = TextFromFile(path);
        if (existing != null)
            return existing;
        var content = resolveDefaultContent();
        WriteTextFile(path, content);
        return content;
    }

    /// <summary>Seed a missing file, or replace it with the bundled version when auto-update is enabled.</summary>
    public string SeedOrUpdateFile(string fileName, string bundledContent, bool autoUpdate)
    {
        var path = GetHomePath(fileName);
        if (autoUpdate || !File.Exists(path))
            WriteTextFile(path, bundledContent);
        return TextFromFile(path)!;
    }
}
