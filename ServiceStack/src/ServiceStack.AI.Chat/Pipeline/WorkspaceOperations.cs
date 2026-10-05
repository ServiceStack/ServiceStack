using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// Single-host App_Data coordination. All instances sharing a normalized data root share state.
/// This is not a cross-process lease: multiple web processes must not own the same App_Data.
/// Lock order: submission gate, repository lease, then the Git index. Leases may cross await.
/// </summary>
public sealed class WorkspaceOperations
{
    static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    static readonly ConcurrentDictionary<string, RootState> Roots = new(PathComparer);
    sealed class RootState
    {
        internal readonly object Sync = new();
        internal int Readers;
        internal bool Writer;
        internal readonly ConcurrentDictionary<string, SemaphoreSlim> Repositories = new(PathComparer);
    }
    static string Key(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    static HttpError Conflict() => new(409, "WorkspaceBusy", "A repository operation is in progress. Try again shortly.");

    /// <summary>Shared, brief lease around accepting or resuming a run, before touching messages.</summary>
    public IDisposable AcquireSubmission(string dataRoot, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var state = Roots.GetOrAdd(Key(dataRoot), _ => new RootState());
        lock (state.Sync)
        {
            if (state.Writer) throw Conflict();
            state.Readers++;
        }
        return new Lease(() => { lock (state.Sync) state.Readers--; });
    }

    /// <summary>Exclusive, nonblocking lease; callers check all captured active workspaces inside it.</summary>
    public async Task<IDisposable> AcquireMutationAsync(string dataRoot, string repository, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var state = Roots.GetOrAdd(Key(dataRoot), _ => new RootState());
        lock (state.Sync)
        {
            if (state.Writer || state.Readers != 0) throw Conflict();
            state.Writer = true;
        }
        var repo = state.Repositories.GetOrAdd(Key(repository), _ => new SemaphoreSlim(1, 1));
        try
        {
            if (!await repo.WaitAsync(0, token).ConfigureAwait(false)) throw Conflict();
        }
        catch
        {
            lock (state.Sync) state.Writer = false;
            throw;
        }
        return new Lease(() =>
        {
            repo.Release();
            lock (state.Sync) state.Writer = false;
        });
    }

    sealed class Lease(Action release) : IDisposable
    {
        Action? action = release;
        public void Dispose() => Interlocked.Exchange(ref action, null)?.Invoke();
    }
}

public partial class ChatFeature
{
    public WorkspaceOperations WorkspaceOperations { get; } = new();
    /// <summary>App supplies its in-memory signal; disabled App is a no-op.</summary>
    public Action? SidebarNotification { get; set; }
    public void NotifySidebar() => SidebarNotification?.Invoke();
    public IGitProvisioner GitProvisioner { get; set; } = new NullGitProvisioner();
}

/// <summary>Projects owns durable creation; Git only prepares the operation-owned directory.</summary>
public interface IGitProvisioner
{
    bool InitializeGit { get; }
    bool Clone { get; }
    JsonObject Validate(JsonObject source) => source.Clone();
    Task ProvisionAsync(string directory, JsonObject source, string user, CancellationToken token);
    Task ProvisionAsync(string directory, JsonObject source, string user, Func<string,int?,Task> progress,
        Func<int?,Task> child, CancellationToken token) => ProvisionAsync(directory, source, user, token);
}

public sealed class NullGitProvisioner : IGitProvisioner
{
    public bool InitializeGit => false;
    public bool Clone => false;
    public Task ProvisionAsync(string directory, JsonObject source, string user, CancellationToken token) =>
        throw new InvalidOperationException("Git provisioning is unavailable");
}
