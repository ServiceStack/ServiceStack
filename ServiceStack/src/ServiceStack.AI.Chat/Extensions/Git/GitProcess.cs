using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>Shell-free, bounded, hook-free Git. Hosted processes never borrow machine credentials.</summary>
public class GitProcess(string executable = "git", bool localCredentials = false)
{
    public string Executable { get; } = executable;
    public bool LocalCredentials { get; } = localCredentials;
    public static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));
    public static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
    public static string NullDevice => OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
    public sealed record Result(int Code, string Output, string Error);

    public virtual async Task<Result> RunAsync(string repo, IEnumerable<string> args, CancellationToken token = default,
        string? index = null, JsonObject? identity = null, IEnumerable<string>? config = null,
        IReadOnlyDictionary<string,string>? environment = null, int seconds = 30, int maxBytes = 1024 * 1024, Func<int?,Task>? child = null, Func<string,Task>? diagnostic = null, bool tailError = false)
    {
        var start = new ProcessStartInfo(Executable) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var key in start.Environment.Keys.Where(x => x.StartsWith("GIT_", StringComparison.OrdinalIgnoreCase)).ToArray()) start.Environment.Remove(key);
        foreach (var pair in new Dictionary<string,string> { ["GIT_TERMINAL_PROMPT"]="0", ["GIT_LITERAL_PATHSPECS"]="1", ["GIT_NO_LAZY_FETCH"]="1", ["GIT_OPTIONAL_LOCKS"]="0", ["GIT_LFS_SKIP_SMUDGE"]="1", ["GIT_ASKPASS"]="", ["SSH_ASKPASS"]="", ["GIT_SSH_COMMAND"]="ssh -oBatchMode=yes -oStrictHostKeyChecking=yes" }) start.Environment[pair.Key] = pair.Value;
        if (!LocalCredentials) { start.Environment["GIT_CONFIG_NOSYSTEM"]="1"; start.Environment["GIT_CONFIG_GLOBAL"]=NullDevice; }
        if (index != null) start.Environment["GIT_INDEX_FILE"] = index;
        if (identity != null) foreach (var prefix in new[] { "GIT_AUTHOR_", "GIT_COMMITTER_" }) { start.Environment[prefix+"NAME"] = identity.GetString("name"); start.Environment[prefix+"EMAIL"] = identity.GetString("email"); }
        if (environment != null) foreach (var pair in environment) start.Environment[pair.Key] = pair.Value;
        start.ArgumentList.Add("--no-pager");
        foreach (var setting in new[] { "core.fsmonitor=false", "core.hooksPath="+NullDevice, "commit.gpgSign=false", "init.templateDir=", "submodule.recurse=false" }.Concat(config ?? [])) { start.ArgumentList.Add("-c"); start.ArgumentList.Add(setting); }
        start.ArgumentList.Add("-C"); start.ArgumentList.Add(repo);
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(seconds));
        using var process = Process.Start(start) ?? throw HttpError.ServiceUnavailable("Git is unavailable");
        using var kill = timeout.Token.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch (InvalidOperationException) { } });
        async Task<string> Read(Stream stream, bool stderr = false)
        {
            using var bytes = new MemoryStream(); var buffer = new byte[8192];
            while (true) {
                var count = await stream.ReadAsync(buffer, timeout.Token).ConfigureAwait(false); if (count == 0) break;
                if (stderr && diagnostic != null) await diagnostic(Encoding.UTF8.GetString(buffer, 0, count)).ConfigureAwait(false);
                if (stderr && tailError && bytes.Length + count > maxBytes) {
                    var previous = bytes.ToArray(); bytes.SetLength(0);
                    var retained = Math.Min(previous.Length, Math.Max(0, maxBytes - count));
                    bytes.Write(previous, previous.Length - retained, retained);
                }
                if (bytes.Length + count > maxBytes) { try { process.Kill(true); } catch (InvalidOperationException) { } throw HttpError.BadRequest("Git output is too large. Use your Git client for this operation."); }
                bytes.Write(buffer, 0, count);
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
        try {
            if (child != null) await child(process.Id).ConfigureAwait(false);
            async Task<string> GuardedRead(Stream stream, bool stderr = false) {
                try { return await Read(stream, stderr).ConfigureAwait(false); }
                catch { try { if (!process.HasExited) process.Kill(true); } catch (InvalidOperationException) { } throw; }
            }
            var output = GuardedRead(process.StandardOutput.BaseStream); var error = GuardedRead(process.StandardError.BaseStream, true);
            // Observe both readers even when one exceeds the bound, so pipes cannot deadlock.
            await Task.WhenAll(output, error, process.WaitForExitAsync(timeout.Token)).ConfigureAwait(false);
            return new Result(process.ExitCode, await output, await error);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw new HttpError(408, "GitTimeout", "Git took too long. Refresh before retrying."); }
        finally { if (!process.HasExited) { try { process.Kill(true); } catch (InvalidOperationException) { } await process.WaitForExitAsync().ConfigureAwait(false); } if (child != null) await child(null).ConfigureAwait(false); }
    }
    public async Task<string> CheckedAsync(string repo, IEnumerable<string> args, CancellationToken token = default, string? index = null, JsonObject? identity = null,
        IEnumerable<string>? config = null, IReadOnlyDictionary<string,string>? environment = null, int seconds = 30, int maxBytes = 1024 * 1024)
    {
        var result = await RunAsync(repo,args,token,index,identity,config,environment,seconds,maxBytes).ConfigureAwait(false);
        if (result.Code != 0) throw new HttpError(409,"GitConflict","Git could not complete this operation. Refresh and check for conflicts or a locked index.");
        return result.Output;
    }
}
