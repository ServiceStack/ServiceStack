using System.Text.RegularExpressions;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public sealed class GitProvisioner(GitProcess git, IEnumerable<string>? hosts = null) : IGitProvisioner
{
    public bool InitializeGit => true;
    public bool Clone => true;
    readonly HashSet<string> approvedHosts = new(hosts ?? ["github.com", "gitlab.com", "bitbucket.org"], StringComparer.OrdinalIgnoreCase);
    public JsonObject Validate(JsonObject source)
    {
        if (source.GetString("kind") != "clone") return source.Clone();
        var url = source.GetString("url")?.Trim() ?? "";
        if (url.Length is 0 or > 2048 || url.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)) || url.StartsWith('-')) throw HttpError.BadRequest("Enter a valid repository URL.");
        Uri? uri = null;
        var scp = Regex.Match(url, @"^([\w.-]+)@([\w.-]+):([\w./~-]+)$");
        string hostname;
        if (scp.Success) hostname = scp.Groups[2].Value;
        else {
            if (!Uri.TryCreate(url,UriKind.Absolute,out uri) || uri.Scheme is not ("https" or "ssh") || string.IsNullOrEmpty(uri.Host) || uri.Host.StartsWith('-') || uri.AbsolutePath.Trim('/').Length == 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.UserInfo.Contains(':') || uri.Scheme == "https" && uri.UserInfo.Length > 0) throw HttpError.BadRequest("Use an HTTPS or SSH clone URL without embedded credentials.");
            hostname = uri.Host;
            if (hostname == "github.com" && uri.AbsolutePath.Trim('/').Split('/').Length != 2) throw HttpError.BadRequest("Use the GitHub repository URL, rather than a file or issue URL.");
        }
        if (!git.LocalCredentials && (uri?.Scheme != "https" || !approvedHosts.Contains(hostname))) throw HttpError.BadRequest("This server supports public HTTPS repositories on approved Git hosts.");
        var branch = source.GetString("branch");
        if (!string.IsNullOrEmpty(branch) && (branch.Length > 200 || branch.StartsWith('-') || branch.StartsWith('/') || Regex.IsMatch(branch,@"[\s~^:?*\[\\\x00-\x1f]") || branch.Contains("..") || branch.Contains("@{") || branch.Contains("//") || branch.EndsWith('/') || branch.EndsWith('.') || branch.EndsWith(".lock"))) throw HttpError.BadRequest("Enter a valid branch name.");
        return new JsonObject { ["kind"]="clone", ["url"]=url, ["branch"]=string.IsNullOrEmpty(branch)? null:branch };
    }
    public Task ProvisionAsync(string directory, JsonObject source, string user, CancellationToken token) =>
        ProvisionAsync(directory, source, user, (_,_)=>Task.CompletedTask, _=>Task.CompletedTask, token);
    public async Task ProvisionAsync(string directory, JsonObject source, string user, Func<string,int?,Task> progress,
        Func<int?,Task> child, CancellationToken token)
    {
        source = Validate(source);
        var config = new List<string> { "protocol.allow=never", "protocol.https.allow=always", "protocol.ssh.allow="+(git.LocalCredentials?"always":"never"), "credential.interactive=false", "http.followRedirects=false" };
        if (!git.LocalCredentials) config.AddRange(["credential.helper=", "http.extraHeader=", "http.cookieFile="]);
        // Local opt-in may reuse credentials, but checkout must never run an operator's
        // content filter. Disable every inherited filter command, including LFS processing.
        var inherited = await git.RunAsync(Path.GetDirectoryName(directory)!,
            ["config", "--get-regexp", @"^(filter\..*\.(clean|smudge|process|required)|url\..*\.(insteadof|pushinsteadof)|http\..*\.(extraheader|cookiefile)|credential\..*\.helper)$"], token).ConfigureAwait(false);
        foreach (var line in inherited.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)) {
            var key = line.Split(' ', 2)[0];
            if (key.StartsWith("filter.", StringComparison.OrdinalIgnoreCase))
                config.Add(key + (key.EndsWith(".required", StringComparison.OrdinalIgnoreCase) ? "=false" : "="));
            else if (key.StartsWith("url.", StringComparison.OrdinalIgnoreCase))
                throw HttpError.BadRequest("Git URL rewriting is unsupported for project cloning.");
            else if (!git.LocalCredentials)
                throw HttpError.Forbidden("Repository-specific credentials are unavailable in hosted mode.");
        }
        var args = new List<string>();
        var clone = source.GetString("kind") == "clone";
        if (clone) {
            args.AddRange(["clone", "--template=", "--no-recurse-submodules"]);
            if (source.GetString("branch") is { } branch) args.AddRange(["--branch", branch]);
            args.AddRange(["--", source.GetString("url")!,directory]);
        } else args.AddRange(["init","--quiet","--template=",directory]);
        async Task Diagnostic(string output) {
            foreach (var label in new[] { "Receiving objects", "Resolving deltas", "Updating files" }) {
                var matches = Regex.Matches(output, Regex.Escape(label) + @":\s*(\d{1,3})%");
                if (matches.Count > 0) await progress(label, Math.Min(100, int.Parse(matches[^1].Groups[1].Value))).ConfigureAwait(false);
            }
        }
        var result = await git.RunAsync(Path.GetDirectoryName(directory)!, args, token, config:config,seconds:600,maxBytes:16000,
            child:child,diagnostic:Diagnostic,tailError:true).ConfigureAwait(false);
        if (result.Code != 0) throw HttpError.BadRequest(clone ? "Unable to clone this repository. Check the URL, branch, connection and access permissions. Hosted mode supports public repositories." : "Unable to initialize Git. Check folder permissions.");
        if (!clone) await git.CheckedAsync(directory,["symbolic-ref","HEAD","refs/heads/main"],token).ConfigureAwait(false);
    }
}
