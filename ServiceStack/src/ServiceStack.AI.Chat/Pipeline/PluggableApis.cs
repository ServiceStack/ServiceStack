using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// Pluggable cross-extension seams with no-op defaults (port of main.py ThreadApi/MediaApi/ProjectsApi).
/// The app, gallery and projects extensions replace these with real implementations.
/// </summary>
public interface IThreadApi
{
    JsonObject? GetThread(long threadId, string? user);
    Task UpdateThreadAsync(long threadId, JsonObject thread, string? user = null);

    /// <summary>
    /// Persist the in-flight assistant message of a streaming response. Implementations write it
    /// outside the thread's durable `messages`, so a stream that dies can't damage the conversation.
    /// </summary>
    Task CheckpointStreamAsync(long threadId, JsonObject message, string? user = null) => Task.CompletedTask;

    JsonObject? GetRequest(string requestId, string? user);
}

public interface IMediaApi
{
    List<JsonObject> QueryMedia(JsonObject query, string? user = null);
    Task UpdateMediaAsync(long id, JsonObject media, string? user = null) => Task.CompletedTask;
}

public interface IProjectsApi
{
    List<JsonObject> GetUserProjects(string? user = null);
}

public class NullThreadApi : IThreadApi
{
    public JsonObject? GetThread(long threadId, string? user) => null;
    public Task UpdateThreadAsync(long threadId, JsonObject thread, string? user = null) => Task.CompletedTask;
    public JsonObject? GetRequest(string requestId, string? user) => null;
}

public class NullMediaApi : IMediaApi
{
    public List<JsonObject> QueryMedia(JsonObject query, string? user = null) => [];
}

public class NullProjectsApi : IProjectsApi
{
    public List<JsonObject> GetUserProjects(string? user = null) => [];
}
