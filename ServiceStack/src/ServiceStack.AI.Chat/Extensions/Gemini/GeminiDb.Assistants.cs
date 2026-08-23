using System.Data;
using System.Text.Json.Nodes;
using ServiceStack.OrmLite;

namespace ServiceStack.AI;

public partial class GeminiDb
{
    public ChatAssistant? GetAssistant(long id, string? user)
    {
        using var conn = OpenDb();
        return GetAssistant(conn, id, user);
    }

    static ChatAssistant? GetAssistant(IDbConnection conn, long id, string? user)
    {
        var q = conn.From<ChatAssistant>().Where(x => x.Id == id);
        if (user != null) ChatDb.ApplyUserFilter(q, user);
        return conn.Single(q);
    }

    public ChatAssistant? GetPublicAssistant(string publicId)
    {
        using var conn = OpenDb();
        return conn.Single<ChatAssistant>(x => x.PublicId == publicId && x.Enabled && x.PublishedAt != null);
    }

    public List<ChatAssistant> QueryAssistants(long filestoreId, string? user, bool includeArchived = false)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatAssistant>().Where(x => x.FilestoreId == filestoreId);
        if (user != null) ChatDb.ApplyUserFilter(q, user);
        if (!includeArchived) q.And(x => x.Enabled);
        q.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id);
        return conn.Select(q);
    }

    public Dictionary<long, long> AssistantConversationCounts(IEnumerable<long> assistantIds)
    {
        var ids = assistantIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        using var conn = OpenDb();
        var q = conn.From<ChatAssistantConversation>()
            .Where(x => ids.Contains(x.AssistantId))
            .GroupBy(x => x.AssistantId)
            .Select(x => new { x.AssistantId, Count = Sql.Count("*") });
        return conn.Dictionary<long, long>(q);
    }

    public bool AssistantNameExists(long filestoreId, string name, string? user, long? excludeId = null)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatAssistant>().Where(x =>
            x.FilestoreId == filestoreId && x.Name == name && x.Enabled);
        if (user != null) ChatDb.ApplyUserFilter(q, user);
        if (excludeId != null) q.And(x => x.Id != excludeId.Value);
        return conn.Exists(q);
    }

    public long InsertAssistant(ChatAssistant assistant)
    {
        using var conn = OpenDb();
        return conn.Insert(assistant, selectIdentity: true);
    }

    public void UpdateAssistant(ChatAssistant assistant)
    {
        assistant.UpdatedAt = DateTime.Now;
        using var conn = OpenDb();
        conn.Update(assistant);
    }

    public bool ArchiveAssistant(long id, string? user)
    {
        var assistant = GetAssistant(id, user);
        if (assistant == null) return false;
        assistant.Enabled = false;
        assistant.PublishedAt = null;
        UpdateAssistant(assistant);
        return true;
    }

    /// <summary>Restore an archived Assistant as an unpublished draft.</summary>
    public ChatAssistant? RestoreAssistant(long id, string? user)
    {
        using var conn = OpenDb();
        using var tx = conn.OpenTransaction();
        var assistant = GetAssistant(conn, id, user);
        if (assistant == null) return null;
        var duplicate = conn.From<ChatAssistant>().Where(x => x.FilestoreId == assistant.FilestoreId
            && x.Name == assistant.Name && x.Enabled && x.Id != assistant.Id);
        if (user != null) ChatDb.ApplyUserFilter(duplicate, user);
        if (conn.Exists(duplicate))
            throw new InvalidOperationException($"An active Assistant named '{assistant.Name}' already exists");
        assistant.Enabled = true;
        assistant.PublishedAt = null;
        assistant.UpdatedAt = DateTime.Now;
        conn.Update(assistant);
        tx.Commit();
        return assistant;
    }

    /// <summary>Describe the retained data and referring websites affected by permanent deletion.</summary>
    public JsonObject? AssistantDeleteSummary(long id, string? user)
    {
        using var conn = OpenDb();
        return AssistantDeleteSummary(conn, id, user);
    }

    static JsonObject? AssistantDeleteSummary(IDbConnection conn, long id, string? user)
    {
        var assistant = GetAssistant(conn, id, user);
        if (assistant == null) return null;
        var conversations = conn.Select<ChatAssistantConversation>(x => x.AssistantId == id);
        var conversationIds = conn.Column<long>(conn.From<ChatAssistantConversation>()
            .Where(x => x.AssistantId == id).Select(x => x.Id));
        long messages = 0;
        foreach (var conversationIdsBatch in conversationIds.Chunk(500))
            messages += conn.Count<ChatAssistantMessage>(x => conversationIdsBatch.Contains(x.ConversationId));

        var referrers = new Dictionary<string, (long Count, DateTime LastUsedAt)>();
        var unknown = 0;
        foreach (var conversation in conversations)
        {
            var domain = ReferrerDomain(conversation.Origin, conversation.PageUrl);
            if (domain == null)
            {
                unknown++;
                continue;
            }
            var usedAt = conversation.UpdatedAt == default ? conversation.CreatedAt : conversation.UpdatedAt;
            if (referrers.TryGetValue(domain, out var current))
                referrers[domain] = (current.Count + 1, usedAt > current.LastUsedAt ? usedAt : current.LastUsedAt);
            else
                referrers[domain] = (1, usedAt);
        }

        var sites = new JsonArray(referrers
            .OrderByDescending(x => x.Value.LastUsedAt).ThenByDescending(x => x.Key)
            .Select(x => (JsonNode)new JsonObject
            {
                ["domain"] = x.Key,
                ["conversationCount"] = x.Value.Count,
                ["lastUsedAt"] = ChatDb.ToDateNode(x.Value.LastUsedAt),
            }).ToArray());
        return new JsonObject
        {
            ["id"] = assistant.Id,
            ["name"] = assistant.Name,
            ["publicId"] = assistant.PublicId,
            ["enabled"] = assistant.Enabled,
            ["publishedAt"] = ChatDb.ToDateNode(assistant.PublishedAt),
            ["published"] = assistant.Enabled && assistant.PublishedAt != null,
            ["conversations"] = conversations.Count,
            ["messages"] = messages,
            ["referrers"] = sites,
            ["unknownReferrerConversations"] = unknown,
        };
    }

    static string? ReferrerDomain(string? origin, string? pageUrl)
    {
        foreach (var value in new[] { origin, pageUrl })
        {
            var text = value?.Trim();
            if (string.IsNullOrEmpty(text) || text.Equals("null", StringComparison.OrdinalIgnoreCase))
                continue;
            var candidate = text.Contains("://", StringComparison.Ordinal) ? text : $"https://{text}";
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
                continue;
            var host = uri.HostNameType == UriHostNameType.IPv6
                ? $"[{uri.Host.ToLowerInvariant()}]"
                : uri.IdnHost.ToLowerInvariant();
            var hasExplicitPort = uri.Authority.EndsWith($":{uri.Port}", StringComparison.Ordinal);
            return hasExplicitPort ? $"{host}:{uri.Port}" : host;
        }
        return null;
    }

    /// <summary>Transactionally delete an Assistant, all of its conversations, and every message.</summary>
    public JsonObject? DeleteAssistant(long id, string? user, string? confirmation = null)
    {
        using var conn = OpenDb();
        using var tx = conn.OpenTransaction();
        var impact = AssistantDeleteSummary(conn, id, user);
        if (impact == null) return null;
        if (confirmation != null && confirmation != impact.GetString("name"))
            throw new ArgumentException($"Type \"{impact.GetString("name")}\" to confirm permanent deletion");
        var conversationIds = conn.Column<long>(conn.From<ChatAssistantConversation>()
            .Where(x => x.AssistantId == id).Select(x => x.Id));
        foreach (var conversationIdsBatch in conversationIds.Chunk(500))
            conn.Delete<ChatAssistantMessage>(x => conversationIdsBatch.Contains(x.ConversationId));
        conn.Delete<ChatAssistantConversation>(x => x.AssistantId == id);
        conn.Delete<ChatAssistant>(x => x.Id == id);
        tx.Commit();
        return impact;
    }

    public List<ChatAssistantConversation> QueryAssistantConversations(long assistantId, string? user, int take = 100)
    {
        if (GetAssistant(assistantId, user) == null) return [];
        using var conn = OpenDb();
        return conn.Select(conn.From<ChatAssistantConversation>()
            .Where(x => x.AssistantId == assistantId)
            .OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id)
            .Limit(Math.Clamp(take, 1, 500)));
    }

    public Dictionary<long, long> AssistantUserMessageCounts(IEnumerable<long> conversationIds)
    {
        var ids = conversationIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        using var conn = OpenDb();
        var q = conn.From<ChatAssistantMessage>()
            .Where(x => ids.Contains(x.ConversationId) && x.Role == "user")
            .GroupBy(x => x.ConversationId)
            .Select(x => new { x.ConversationId, Count = Sql.Count("*") });
        return conn.Dictionary<long, long>(q);
    }

    public ChatAssistantConversation? GetAssistantConversation(long id, long? assistantId = null, string? user = null)
    {
        if (assistantId != null && GetAssistant(assistantId.Value, user) == null) return null;
        using var conn = OpenDb();
        var q = conn.From<ChatAssistantConversation>().Where(x => x.Id == id);
        if (assistantId != null) q.And(x => x.AssistantId == assistantId.Value);
        return conn.Single(q);
    }

    public ChatAssistantConversation? FindAssistantConversation(long assistantId, string sessionId)
    {
        using var conn = OpenDb();
        return conn.Single<ChatAssistantConversation>(x => x.AssistantId == assistantId && x.SessionId == sessionId);
    }

    public long CreateAssistantConversation(ChatAssistant assistant, string sessionId, string? origin,
        string? pageUrl, string? userAgent)
    {
        var now = DateTime.Now;
        using var conn = OpenDb();
        return conn.Insert(new ChatAssistantConversation
        {
            AssistantId = assistant.Id, User = assistant.User, CreatedAt = now, UpdatedAt = now,
            SessionId = sessionId, Origin = origin, PageUrl = pageUrl, UserAgent = userAgent,
            Status = "open",
        }, selectIdentity: true);
    }

    public List<ChatAssistantMessage> QueryAssistantMessages(long conversationId)
    {
        using var conn = OpenDb();
        return conn.Select(conn.From<ChatAssistantMessage>()
            .Where(x => x.ConversationId == conversationId).OrderBy(x => x.Id));
    }

    public long AddAssistantMessage(ChatAssistantConversation conversation, string role, string content,
        JsonArray? citations = null, string? error = null)
    {
        using var conn = OpenDb();
        using var tx = conn.OpenTransaction();
        var id = conn.Insert(new ChatAssistantMessage
        {
            ConversationId = conversation.Id, CreatedAt = DateTime.Now, Role = role, Content = content,
            Citations = (citations ?? []).ToJsonString(ChatJson.Options), Error = error,
        }, selectIdentity: true);
        conversation.UpdatedAt = DateTime.Now;
        conversation.MessageCount++;
        if (string.IsNullOrEmpty(conversation.Title) && role == "user")
            conversation.Title = content.SafeSubstring(0, 100);
        conversation.LastMessage = content.SafeSubstring(0, 500);
        conn.Update(conversation);
        tx.Commit();
        return id;
    }

    public List<ChatDocument> AssistantCitationDocuments(long filestoreId, string? user)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatDocument>().Where(x => x.FilestoreId == filestoreId && x.SourceUrl != null)
            .Select(x => new { x.DisplayName, x.SourceKey, x.SourceUrl });
        if (user != null) ChatDb.ApplyUserFilter(q, user);
        return conn.Select(q);
    }
}
