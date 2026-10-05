using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>One host owns App_Data. The gate and refresh lease span extension instances at the same user path.</summary>
public sealed class OpenAiSubscriptionStore(ExtensionContext ctx)
{
    public sealed class UserState
    {
        public object Gate { get; } = new();
        public SemaphoreSlim Refresh { get; } = new(1, 1);
        public long Generation;
    }
    static readonly ConcurrentDictionary<string, UserState> States = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    public string PathFor(string user) => Path.Combine(ctx.GetUserPath(user), "credentials", "openai_subscription.json");
    public UserState State(string user) => States.GetOrAdd(Path.GetFullPath(PathFor(user)), _ => new());
    static void CheckPath(string path)
    {
        for (var current = path; current != null; current = Path.GetDirectoryName(current))
        {
            if (File.Exists(current) || Directory.Exists(current))
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Subscription credential paths must not contain links.");
        }
    }
    public JsonObject? Load(string user)
    {
        var state = State(user); lock (state.Gate)
        {
            var path = PathFor(user); CheckPath(path); if (!File.Exists(path)) return null;
            if (new FileInfo(path).Length > 1024 * 1024) throw new InvalidOperationException("Subscription credentials exceed the size limit.");
            return ChatJson.ParseObject(File.ReadAllText(path));
        }
    }
    public void Save(string user, JsonObject credentials)
    { var state = State(user); lock (state.Gate) { Write(PathFor(user), credentials); state.Generation++; } }
    public bool SaveIfCurrent(string user, JsonObject credentials, long generation, string? expected)
    {
        var state = State(user); lock (state.Gate)
        {
            if (state.Generation != generation || Fingerprint(Load(user)) != expected) return false;
            Write(PathFor(user), credentials); state.Generation++; return true;
        }
    }
    public static string? Fingerprint(JsonObject? credentials) => credentials == null ? null : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(credentials.ToJsonString(ChatJson.Options))));
    public void Disconnect(string user)
    {
        var state = State(user); lock (state.Gate)
        {
            state.Generation++; var path = PathFor(user); CheckPath(path); if (File.Exists(path)) File.Delete(path);
        }
    }
    public string HostId()
    {
        var root = Path.Combine(ctx.Feature.AppData.BasePath, "openai-agent-host.json"); var state = States.GetOrAdd(Path.GetFullPath(root), _ => new());
        lock (state.Gate)
        {
            CheckPath(root); if (File.Exists(root)) return ChatJson.ParseObject(File.ReadAllText(root)).GetString("id") ?? throw new InvalidOperationException("Invalid subscription host identity.");
            var id = "urn:uuid:" + Guid.NewGuid(); Write(root, new JsonObject { ["id"] = id }); return id;
        }
    }
    static void Write(string path, JsonObject value)
    {
        CheckPath(path); var directory = Path.GetDirectoryName(path)!; Directory.CreateDirectory(directory); CheckPath(directory);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temp, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                var bytes = System.Text.Encoding.UTF8.GetBytes(value.ToJsonString(ChatJson.Indented)); stream.Write(bytes); stream.Flush(true);
            }
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
