using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;

namespace ServiceStack.AI;

public sealed partial class McpClientExtension
{
    const int MaxConfigurationBytes = 65536;
    static readonly JsonSerializerOptions ConfigurationOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };
    static readonly HashSet<string> PersonalKeys = ["id", "displayName", "endpoint", "auth", "allowedTools",
        "deniedTools", "includeInAll", "oauthClientId", "oauthIssuer", "oauthScopes"];

    string ConfigurationPath(string user)
    {
        if (string.IsNullOrWhiteSpace(user) || user is "." or ".." or "all" or "*"
            || user.IndexOfAny(['/', '\\', '\0']) >= 0)
            throw HttpError.Forbidden("Invalid MCP configuration owner");
        return Path.Combine(Feature.AppData.GetUserPath(user), "mcp_client", "config.json");
    }
    string ReadConfiguration(string user)
    {
        if (Feature.AppData == null) return "{\"servers\":[]}";
        var path = ConfigurationPath(user);
        if (!File.Exists(path)) return "{\"servers\":[]}";
        using var stream = File.OpenRead(path);
        if (stream.Length > MaxConfigurationBytes) throw HttpError.BadRequest("MCP configuration is too large");
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        if (Encoding.UTF8.GetByteCount(text) > MaxConfigurationBytes) throw HttpError.BadRequest("MCP configuration is too large");
        return text;
    }
    List<McpClientServer> ParseConfiguration(string text, string? owner)
    {
        try {
            if (Encoding.UTF8.GetByteCount(text) > MaxConfigurationBytes) throw new JsonException();
            var json = JsonNode.Parse(text)!.AsObject();
            RejectNulls(json);
            if (json.Count != 1 || json["servers"] is not JsonArray array || array.Count > 64) throw new JsonException();
            foreach (var item in array) {
                var obj = item!.AsObject();
                if (obj.ContainsKey("authorize") || owner != null && obj.Any(x => !PersonalKeys.Contains(x.Key)))
                    throw new JsonException();
            }
            var servers = array.Deserialize<List<McpClientServer>>(ConfigurationOptions)!;
            if (servers.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != servers.Count) throw new JsonException();
            foreach (var server in servers) {
                server.Validate(); NetworkPolicy.AssertUri(server.Endpoint);
                if (server.OAuthIssuer != null) NetworkPolicy.AssertUri(server.OAuthIssuer);
                if (owner != null) {
                    if (server.Auth.Mode is not ("anonymous" or "user_oauth" or "bearer") || server.Auth.SecretReference != null)
                        throw new JsonException();
                    server.ConfigurationScope = "personal:" + owner;
                }
                if (server.Auth.Mode == "bearer" && (!Feature.RequireAuth || HostContext.TryResolve<IDataProtectionProvider>() == null))
                    throw new JsonException();
                if (server.Auth.Mode == "user_oauth") {
                    if (OAuthRedirectUri == null)
                        throw HttpError.BadRequest("OAuth is unavailable: configure the MCP client callback URL on the host and restart it.");
                    if (!Feature.RequireAuth)
                        throw HttpError.BadRequest("OAuth is unavailable: the chat host must require sign-in.");
                    if (HostContext.TryResolve<IDataProtectionProvider>() == null)
                        throw HttpError.BadRequest("OAuth is unavailable: the chat host must register Data Protection.");
                }
            }
            return servers;
        } catch (HttpError) {
            throw;
        } catch (Exception e) when (e is not OutOfMemoryException) {
            throw HttpError.BadRequest("Invalid MCP configuration. Check connection settings and host policy.");
        }
    }
    internal IReadOnlyList<McpClientServer> GetServers(string? user)
    {
        var shared = Servers.Concat(ParseConfiguration(ReadConfiguration(ChatDb.DefaultUser), null)).ToList();
        if (shared.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != shared.Count)
            throw HttpError.BadRequest("Duplicate shared MCP connection IDs");
        if (string.IsNullOrEmpty(user) || user == ChatDb.DefaultUser) return shared;
        var personal = ParseConfiguration(ReadConfiguration(user), user);
        if (personal.Any(x => shared.Any(s => s.Id == x.Id)))
            throw HttpError.BadRequest("Personal MCP connection IDs must differ from shared connections");
        return shared.Concat(personal).ToList();
    }
    internal JsonObject GetPersonalConfiguration(string user)
    {
        var text = user == ChatDb.DefaultUser ? "{\"servers\":[]}" : ReadConfiguration(user);
        ParseConfiguration(text, user);
        var result = JsonNode.Parse(text)!.AsObject();
        result["revision"] = McpClientHash.Of(text);
        result["canEdit"] = user != ChatDb.DefaultUser;
        result["oauthRedirectUri"] = OAuthRedirectUri?.AbsoluteUri;
        return result;
    }
    internal JsonObject SavePersonalConfiguration(string user, JsonObject body)
    {
        if (user == ChatDb.DefaultUser) throw HttpError.Forbidden("The default configuration is managed by the host");
        if (body.Any(x => x.Key is not ("servers" or "revision"))) throw HttpError.BadRequest("Invalid MCP configuration fields");
        var json = new JsonObject { ["servers"] = body["servers"]?.DeepClone() };
        var text = json.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        var servers = ParseConfiguration(text, user);
        var shared = Servers.Concat(ParseConfiguration(ReadConfiguration(ChatDb.DefaultUser), null)).ToList();
        if (servers.Any(x => shared.Any(s => s.Id == x.Id))) throw HttpError.BadRequest("A personal connection cannot override a shared connection");
        var path = ConfigurationPath(user);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Exclusive file lock also coordinates saves from other host processes using this directory.
        using var fileLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        var before = ReadConfiguration(user);
        if (body["revision"]?.GetValue<string>() != McpClientHash.Of(before))
            throw HttpError.Conflict("Configuration changed. Reload before saving.");
        var previous = ParseConfiguration(before, user);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, text); File.Move(temporary, path, overwrite: true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        foreach (var old in previous.Where(x => !servers.Any(s => s.Id == x.Id && s.ConfigurationHash == x.ConfigurationHash))) {
            OAuth.Cancel(user, old.Id); Manager.Invalidate(old, user);
            Store.SetDisconnected(old, user, true, delete: true);
        }
        return GetPersonalConfiguration(user);
    }
    void RegisterConfigurationRoutes(ExtensionContext ctx)
    {
        ctx.AddGet("config.json", req => Task.FromResult<object?>(GetPersonalConfiguration(req.AssertUserName()!)));
        ctx.AddPost("config.json", async req => {
            AssertMutation(req);
            var raw = await req.Request.GetRawBodyAsync().ConfigureAwait(false);
            if (raw == null || Encoding.UTF8.GetByteCount(raw) > MaxConfigurationBytes)
                throw HttpError.BadRequest("MCP configuration is too large");
            JsonObject body;
            try { body = JsonNode.Parse(raw)!.AsObject(); }
            catch { throw HttpError.BadRequest("Invalid MCP configuration JSON"); }
            return SavePersonalConfiguration(req.AssertUserName()!, body);
        });
    }
    static void RejectNulls(JsonNode node)
    {
        if (node is JsonObject obj) foreach (var pair in obj) {
            if (pair.Value == null) throw new JsonException();
            RejectNulls(pair.Value);
        }
        else if (node is JsonArray array) foreach (var item in array) {
            if (item == null) throw new JsonException();
            RejectNulls(item);
        }
    }
}
