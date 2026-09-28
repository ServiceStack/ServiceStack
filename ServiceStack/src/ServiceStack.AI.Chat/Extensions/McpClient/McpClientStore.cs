using Microsoft.AspNetCore.DataProtection;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace ServiceStack.AI;

public sealed class ChatMcpBinding
{
    [PrimaryKey] public string Id { get; set; } = null!;
    [Index] public string Owner { get; set; } = null!;
    public string ServerId { get; set; } = null!;
    public string ConfigurationHash { get; set; } = null!;
    public long Revision { get; set; }
    public bool Disconnected { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? ProtectedTokens { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? ProtectedClientSecret { get; set; }
    public long TokenVersion { get; set; }
    public string? LeaseId { get; set; }
    public DateTime? LeaseUntil { get; set; }
}

public sealed class ChatMcpInvocation
{
    [PrimaryKey] public string Id { get; set; } = null!;
    [Index] public string Owner { get; set; } = null!;
    public string ServerId { get; set; } = null!;
    public string ToolName { get; set; } = null!;
    public string ConfigurationHash { get; set; } = null!;
    public string SchemaHash { get; set; } = null!;
    public long? ThreadId { get; set; }
    public long? RunId { get; set; }
    public string State { get; set; } = "prepared";
    public DateTime UpdatedAt { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Result { get; set; }
}

public sealed class ChatMcpAuthorization
{
    [PrimaryKey] public string Id { get; set; } = null!;
    [Index] public string Owner { get; set; } = null!;
    public string ServerId { get; set; } = null!;
    public string ConfigurationHash { get; set; } = null!;
    public string RedirectUri { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = "pending";
    [StringLength(StringLengthAttribute.MaxText)] public string? ProtectedResponse { get; set; }
}

public sealed class ChatMcpApprovalGrant
{
    [PrimaryKey] public string Id { get; set; } = null!;
    [Index] public string Owner { get; set; } = null!;
    public string ServerId { get; set; } = null!;
    public string ToolName { get; set; } = null!;
    public string ConfigurationHash { get; set; } = null!;
    public string SchemaHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

internal sealed class McpClientStore(ChatDb db, IDataProtectionProvider? protection) : IHasSchema
{
    static string GrantKey(string owner, string server, string tool, string config, string schema) =>
        McpClientHash.Of(owner + "\0" + server + "\0" + tool + "\0" + config + "\0" + schema);
    internal bool HasApprovalGrant(string owner, McpClientServer server, McpClientTool tool)
    {
        using var conn = db.OpenDb();
        return conn.Exists<ChatMcpApprovalGrant>(x => x.Id == GrantKey(owner, server.Id, tool.Name, server.ConfigurationHash, tool.SchemaHash));
    }
    internal void SaveApprovalGrant(string owner, McpClientServer server, McpClientTool tool)
    {
        using var conn = db.OpenDb();
        var row = new ChatMcpApprovalGrant { Id = GrantKey(owner, server.Id, tool.Name, server.ConfigurationHash, tool.SchemaHash),
            Owner = owner, ServerId = server.Id, ToolName = tool.Name, ConfigurationHash = server.ConfigurationHash,
            SchemaHash = tool.SchemaHash, CreatedAt = DateTime.UtcNow };
        conn.Save(row);
    }
    internal List<ChatMcpApprovalGrant> ApprovalGrants(string owner, McpClientServer server)
    {
        using var conn = db.OpenDb();
        return conn.Select<ChatMcpApprovalGrant>(x => x.Owner == owner && x.ServerId == server.Id && x.ConfigurationHash == server.ConfigurationHash);
    }
    internal void RevokeApprovalGrant(string owner, McpClientServer server, string tool)
    {
        using var conn = db.OpenDb();
        conn.Delete<ChatMcpApprovalGrant>(x => x.Owner == owner && x.ServerId == server.Id && x.ToolName == tool);
    }
    internal void CreateAuthorization(ChatMcpAuthorization row)
    {
        using var conn = db.OpenDb();
        var now = DateTime.UtcNow;
        conn.Delete<ChatMcpAuthorization>(x => x.ExpiresAt < now);
        conn.Insert(row);
    }
    internal ChatMcpAuthorization? GetAuthorization(string id, string user)
    {
        using var conn = db.OpenDb();
        return conn.Single<ChatMcpAuthorization>(x => x.Id == id && x.Owner == user);
    }
    internal bool SubmitAuthorization(ChatMcpAuthorization row, string response)
    {
        using var conn = db.OpenDb();
        var protectedResponse = Protector(row.Id).Protect(response);
        var now = DateTime.UtcNow;
        return conn.UpdateOnly(() => new ChatMcpAuthorization { ProtectedResponse = protectedResponse, Status = "submitted" },
            x => x.Id == row.Id && x.Owner == row.Owner && x.Status == "pending" && x.ExpiresAt > now) == 1;
    }
    internal string? AuthorizationResponse(ChatMcpAuthorization row) => row.ProtectedResponse == null ? null : Protector(row.Id).Unprotect(row.ProtectedResponse);
    internal void AuthorizationOutcome(string id, string status)
    {
        using var conn = db.OpenDb();
        conn.UpdateOnly(() => new ChatMcpAuthorization { Status = status, ProtectedResponse = null },
            x => x.Id == id && (x.Status == "pending" || x.Status == "submitted"));
    }
    internal void CancelAuthorizations(string user, string server)
    {
        using var conn = db.OpenDb();
        conn.UpdateOnly(() => new ChatMcpAuthorization { Status = "canceled", ProtectedResponse = null },
            x => x.Owner == user && x.ServerId == server && (x.Status == "pending" || x.Status == "submitted"));
    }
    internal ChatMcpBinding? GetBinding(McpClientServer server, string user)
    {
        using var conn = db.OpenDb();
        return conn.SingleById<ChatMcpBinding>(Key(server, user));
    }
    internal ChatMcpBinding EnsureBinding(McpClientServer server, string user)
    {
        var binding = GetBinding(server, user);
        if (binding != null) return binding;
        using var conn = db.OpenDb();
        binding = new ChatMcpBinding { Id = Key(server, user), Owner = user, ServerId = server.Id,
            ConfigurationHash = server.ConfigurationHash, Revision = 1 };
        try { conn.Insert(binding); }
        catch { return GetBinding(server, user) ?? throw new McpClientException("storage_error", "Connection could not be created"); }
        return binding;
    }
    internal void SetDisconnected(McpClientServer server, string user, bool disconnected, bool delete = false, bool preserveClientSecret = false)
    {
        var row = EnsureBinding(server, user);
        using var conn = db.OpenDb();
        var changed = conn.UpdateOnly(() => new ChatMcpBinding {
            Disconnected = disconnected, Revision = row.Revision + 1,
            ProtectedTokens = delete ? null : row.ProtectedTokens,
            ProtectedClientSecret = delete && !preserveClientSecret ? null : row.ProtectedClientSecret,
            LeaseId = null, LeaseUntil = null,
        }, x => x.Id == row.Id && x.Revision == row.Revision);
        if (changed != 1) throw new McpClientException("connection_changed", "Connection changed; refresh and try again");
    }
    internal string? ReadTokens(ChatMcpBinding row) => row.ProtectedTokens == null ? null : Protector(row.Id).Unprotect(row.ProtectedTokens);
    internal string? ReadClientSecret(ChatMcpBinding row) => row.ProtectedClientSecret == null ? null : Protector(row.Id).Unprotect(row.ProtectedClientSecret);
    internal void SaveClientSecret(McpClientServer server, string user, string secret)
    {
        var row = EnsureBinding(server, user);
        var encrypted = Protector(row.Id).Protect(secret);
        using var conn = db.OpenDb();
        if (conn.UpdateOnly(() => new ChatMcpBinding { ProtectedClientSecret = encrypted },
            x => x.Id == row.Id && x.Revision == row.Revision) != 1)
            throw new McpClientException("connection_changed", "Connection changed; refresh and try again");
    }
    internal void WriteTokens(ChatMcpBinding row, string json)
    {
        using var conn = db.OpenDb();
        var encrypted = Protector(row.Id).Protect(json);
        if (conn.UpdateOnly(() => new ChatMcpBinding { ProtectedTokens = encrypted, TokenVersion = row.TokenVersion + 1 },
            x => x.Id == row.Id && x.TokenVersion == row.TokenVersion && x.Revision == row.Revision && !x.Disconnected) != 1)
            throw new McpClientException("connection_changed", "Credentials changed during authorization");
        row.TokenVersion++;
        row.ProtectedTokens = encrypted;
    }
    IDataProtector Protector(string id) => (protection ?? throw new McpClientException("auth_required", "Host data protection is required for stored credentials"))
        .CreateProtector("ServiceStack.AI.McpClient.Tokens.v1", id);
    internal string ClaimCredential(ChatMcpBinding row)
    {
        using var conn = db.OpenDb();
        var now = DateTime.UtcNow;
        var lease = Guid.NewGuid().ToString("N");
        if (conn.UpdateOnly(() => new ChatMcpBinding { LeaseId = lease, LeaseUntil = now.AddMinutes(6) },
            x => x.Id == row.Id && x.Revision == row.Revision && (x.LeaseUntil == null || x.LeaseUntil < now) && !x.Disconnected) != 1)
            throw new McpClientException("busy", "Another authorization or call is using this account");
        return lease;
    }
    internal void ReleaseCredential(ChatMcpBinding row, string lease)
    {
        using var conn = db.OpenDb();
        conn.UpdateOnly(() => new ChatMcpBinding { LeaseId = null, LeaseUntil = null }, x => x.Id == row.Id && x.LeaseId == lease);
    }
    internal void Prepare(ChatMcpInvocation invocation)
    {
        using var conn = db.OpenDb();
        try { conn.Insert(invocation); } // Unique invocation id is the replay boundary.
        catch {
            if (conn.SingleById<ChatMcpInvocation>(invocation.Id) != null)
                throw new McpClientException("outcome_unknown", "This invocation was already prepared or dispatched. Reconcile its stored outcome; do not replay it.");
            throw new McpClientException("storage_error", "Could not prepare remote invocation");
        }
    }
    internal void Outcome(string id, string state, string? result = null)
    {
        using var conn = db.OpenDb();
        conn.UpdateOnly(() => new ChatMcpInvocation { State = state, Result = result, UpdatedAt = DateTime.UtcNow }, x => x.Id == id);
    }
    /// <summary>Host user-deletion hook. Removes only MCP-owned credentials and invocation data.</summary>
    internal void DeleteUser(string user)
    {
        using var conn = db.OpenDb();
        conn.Delete<ChatMcpBinding>(x => x.Owner == user);
        conn.Delete<ChatMcpAuthorization>(x => x.Owner == user);
        conn.Delete<ChatMcpInvocation>(x => x.Owner == user);
        conn.Delete<ChatMcpApprovalGrant>(x => x.Owner == user);
    }
    static string Key(McpClientServer server, string user) => McpClientHash.Of(user + "\0" + server.Id + "\0" + server.ConfigurationHash);
    public void InitSchema()
    {
        using var conn = db.OpenDb();
        conn.CreateTableIfNotExists<ChatMcpAuthorization>();
        ChatDb.AddMissingColumns<ChatMcpAuthorization>(conn);
        conn.CreateTableIfNotExists<ChatMcpBinding>();
        conn.CreateTableIfNotExists<ChatMcpInvocation>();
        conn.CreateTableIfNotExists<ChatMcpApprovalGrant>();
        ChatDb.AddMissingColumns<ChatMcpBinding>(conn);
        ChatDb.AddMissingColumns<ChatMcpInvocation>(conn);
    }
    public void DropSchema() { using var conn = db.OpenDb(); conn.DropTables(typeof(ChatMcpApprovalGrant), typeof(ChatMcpAuthorization), typeof(ChatMcpInvocation), typeof(ChatMcpBinding)); }
}
