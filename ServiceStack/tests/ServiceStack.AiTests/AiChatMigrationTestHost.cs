#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.AI;
using ServiceStack.Host;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack.AiTests;

/// <summary>Isolated extension route harness. No production OAuth, publisher, or model calls.</summary>
public sealed class AiChatMigrationTestHost : IDisposable
{
    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "ai-chat-migration-" + Guid.NewGuid().ToString("N"));
    public ChatFeature Feature { get; }
    public ChatDb Db { get; }
    public AiChatMigrationClock Clock { get; } = new();

    public AiChatMigrationTestHost(string prefix = "")
    {
        Directory.CreateDirectory(DirectoryPath);
        var factory = new OrmLiteConnectionFactory(Path.Combine(DirectoryPath, "chat.sqlite"), SqliteDialect.Provider);
        Db = new ChatDb(factory);
        Db.InitSchema();
        Feature = new ChatFeature {
            RoutePrefix = prefix, AppData = new ChatAppData(DirectoryPath), ChatDb = Db,
            ChatAuth = new MigrationAuth(),
            Config = ChatJson.ParseObject("{\"defaults\":{\"summarize\":null}}"),
        };
    }

    public T Install<T>(T extension) where T : ChatExtension
    {
        extension.Ctx = new ExtensionContext(Feature, extension.Name);
        extension.Install(extension.Ctx);
        return extension;
    }

    /// <summary>Dispatch the real C# handler with request-local identity and prefix-free route matching.</summary>
    public async Task<object?> SendAsync(string method, string path, string? user, JsonNode? body = null, bool admin = false)
    {
        var request = new BasicRequest { Verb = method, PathInfo = path };
        if (user != null)
        {
            request.Items["migration.user"] = user;
            request.Items[Keywords.Session] = new AuthUserSession { UserAuthName = user, UserAuthId = user, IsAuthenticated = true };
        }
        if (admin) request.Items["migration.admin"] = true;
        typeof(BasicRequest).GetField("body", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(request, body?.ToJsonString() ?? "");
        var query = path.IndexOf('?');
        if (query >= 0)
        {
            foreach (var pair in path[(query + 1)..].Split('&'))
            {
                var parts = pair.Split('=', 2);
                request.QueryString[Uri.UnescapeDataString(parts[0])] = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
            }
            path = path[..query];
        }
        var prefix = Feature.RoutePrefix;
        if (prefix.Length > 0)
        {
            if (!path.StartsWith(prefix + "/", StringComparison.Ordinal)) throw new ArgumentException("Wrong route prefix");
            path = path[prefix.Length..];
        }
        var match = Feature.Routes.Match(method, path) ?? throw new ArgumentException("Route not registered: " + path);
        if (!match.Route.AllowAnon && !Feature.ChatAuth.CheckAuth(request).IsAuthenticated)
            return ChatResult.Unauthorized(Feature.ErrorAuthRequired());
        return await match.Route.Handler(new ChatRequestContext(Feature, request, match.Params));
    }

    public void Dispose()
    {
        Feature.RunShutdownHandlers();
        if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true);
    }

    sealed class MigrationAuth : IChatAuth
    {
        public bool IsEnabled => true;
        public string? GetUserName(IRequest request) => request.Items.TryGetValue("migration.user", out var value) ? value as string : null;
        public string? AssertUserName(IRequest request) => GetUserName(request) ?? throw new UnauthorizedAccessException();
        public (bool, JsonObject?) CheckAuth(IRequest request) => (GetUserName(request) != null, null);
        public Task<JsonObject?> GetAuthInfoAsync(IRequest request) => Task.FromResult<JsonObject?>(GetUserName(request) is { } user ? new JsonObject { ["userName"] = user } : null);
        public Task SignOutAsync(IRequest request) { request.Items.Remove("migration.user"); return Task.CompletedTask; }
        public bool IsAdmin(IRequest request) => request.Items.GetValueOrDefault("migration.admin") is true;
    }
}

public sealed class AiChatMigrationClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.Parse("2026-10-05T00:00:00Z");
    public override DateTimeOffset GetUtcNow() => Now;
}

public sealed class AiChatMigrationHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
}
