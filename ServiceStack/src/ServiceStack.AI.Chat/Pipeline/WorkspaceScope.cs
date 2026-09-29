namespace ServiceStack.AI;

/// <summary>
/// The workspace a durable run resolved when it was queued (port of llms-py's execution_context
/// ContextVar). It flows through every await of the run's slice, so tool execution, filesystem
/// validation and subprocess working directories use the run's project instead of the user's
/// global selection — concurrent runs of the same user in different projects stay isolated, and
/// no user-wide directory list is swapped around awaited calls.
/// </summary>
public sealed record WorkspaceScope(string? ProjectId, List<string> Directories, string? User, long? RunId)
{
    static readonly AsyncLocal<WorkspaceScope?> current = new();

    public static WorkspaceScope? Current => current.Value;

    /// <summary>Apply for the lifetime of the returned handle, restoring the previous scope on dispose</summary>
    public static IDisposable Enter(WorkspaceScope scope)
    {
        var previous = current.Value;
        current.Value = scope;
        return new Restore(previous);
    }

    /// <summary>True when this scope governs directory access for <paramref name="user"/></summary>
    public bool AppliesTo(string? user) =>
        user == null || (User ?? ChatDb.DefaultUser) == user;

    sealed class Restore(WorkspaceScope? previous) : IDisposable
    {
        public void Dispose() => current.Value = previous;
    }
}
