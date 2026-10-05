#nullable enable
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using NUnit.Framework;

namespace ServiceStack.AiTests;

[NonParallelizable]
public class AiChatSyncTests
{
    sealed class Fixture : IDisposable
    {
        readonly string root = Path.Combine(Path.GetTempPath(), "ai-chat-sync " + Guid.NewGuid().ToString("N"));
        public string Home { get; }
        public string Source { get; }
        public string HostData { get; }
        public Fixture()
        {
            Home = Path.Combine(root, "ServiceStack", "src", "ServiceStack.AI.Chat");
            Source = Path.Combine(root, "llms", "llms");
            HostData = Path.Combine(root, "ServiceStack", "tests", "NorthwindAuto", "App_Data", "chat", "llms.json");
            Directory.CreateDirectory(Home);
            var parent = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            string? scripts = null;
            while (parent != null)
            {
                var candidate = Path.Combine(parent.FullName, "ServiceStack", "src", "ServiceStack.AI.Chat");
                if (File.Exists(Path.Combine(candidate, "sync.py"))) { scripts = candidate; break; }
                parent = parent.Parent;
            }
            Assert.That(scripts, Is.Not.Null, "The sync tests need the repository checkout");
            foreach (var name in new[] { "sync.py", "sync.sh" }) File.Copy(Path.Combine(scripts!, name), Path.Combine(Home, name));
            foreach (var name in new[] { "index.html", "llms.json", "providers.json", "providers-extra.json" }) Write(Source, name, "{}");
            Write(Source, "ui/ai.mjs", "export const source = 'current';");
            Write(Source, "ui/modules/model-selector.mjs", "original main selector");
            Write(Source, "ui/app.css", "compiled shared CSS");
            Write(Source, "ui/tailwind/host.mjs", "development input");
            Write(Source, "extensions/app/themes/dark.json", "{}");
            Write(Source, "extensions/agents/profiles/default/SYSTEM.md", "Shared profile");
            Write(Source, "extensions/jev/ui/index.mjs", "Shared Studio UI");
            Write(Source, "extensions/jev/prompts/create.md", "Shared prompt");
            Write(Source, "extensions/jev/recipes/support.json", "{}");
            Write(Source, "extensions/credentials/ui/index.mjs", "Python-only credentials");
            Write(Home, "chat/ext/credentials/index.mjs", "C# credentials");
            Write(Home, "chat/ext/identity/index.mjs", "C# Identity");
            Write(Home, "chat/ext/private-host/index.mjs", "Custom host extension");
            Write(Home, "chat/custom/HostForm.mjs", "Host custom component");
            Write(Home, "chat/ui/stale.mjs", "Stale shared UI");
            Directory.CreateDirectory(Path.GetDirectoryName(HostData)!);
            File.WriteAllText(HostData, "Private deployed config");
        }
        public static void Write(string root, string path, string contents)
        {
            var file = Path.Combine(root, path); Directory.CreateDirectory(Path.GetDirectoryName(file)!); File.WriteAllText(file, contents);
        }
        public static Dictionary<string, string> Hashes(string root) => Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(p => Path.GetRelativePath(root, p), p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        public Task<(int Exit, string Output)> Run(params string[] args) => RunScript("sync.py", args);
        public async Task<(int Exit, string Output)> RunScript(string script, params string[] args)
        {
            var info = new ProcessStartInfo(OperatingSystem.IsWindows() ? "python" : "python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            info.ArgumentList.Add(Path.Combine(Home, script));
            foreach (var arg in args) info.ArgumentList.Add(arg);
            info.ArgumentList.Add(Source);
            Process? process;
            try { process = Process.Start(info); }
            catch (Win32Exception) { Assert.Ignore("Python 3 is required for shared UI synchronization tests"); throw; }
            using (process)
            {
                var output = process!.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                try { await process.WaitForExitAsync(timeout.Token); }
                catch { process.Kill(true); throw; }
                return (process.ExitCode, await output + await error);
            }
        }
        public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Full_sync_is_verbatim_preserves_source_and_host_files_and_is_repeatable()
    {
        using var f = new Fixture(); var upstream = Fixture.Hashes(f.Source);
        var result = await f.Run(); Assert.That(result.Exit, Is.Zero, result.Output);
        foreach (var p in new[] { "ui/ai.mjs", "ui/app.css", "ui/modules/model-selector.mjs", "index.html", "llms.json" })
            Assert.That(File.ReadAllBytes(Path.Combine(f.Home, "chat", p)), Is.EqualTo(File.ReadAllBytes(Path.Combine(f.Source, p))));
        foreach (var p in new[] { "ui/index.mjs", "prompts/create.md", "recipes/support.json" })
            Assert.That(File.ReadAllBytes(Path.Combine(f.Home, "chat/ext/jev", p.StartsWith("ui/") ? p[3..] : p)), Is.EqualTo(File.ReadAllBytes(Path.Combine(f.Source, "extensions/jev", p))));
        Assert.That(File.Exists(Path.Combine(f.Home, "chat/ui/stale.mjs")), Is.False);
        Assert.That(Directory.Exists(Path.Combine(f.Home, "chat/ui/tailwind")), Is.False);
        Assert.That(File.ReadAllText(f.HostData), Is.EqualTo("Private deployed config"));
        Assert.That(File.ReadAllText(Path.Combine(f.Home, "chat/custom/HostForm.mjs")), Is.EqualTo("Host custom component"));
        Assert.That(File.ReadAllText(Path.Combine(f.Home, "chat/ext/credentials/index.mjs")), Is.EqualTo("C# credentials"));
        Assert.That(File.ReadAllText(Path.Combine(f.Home, "chat/ext/identity/index.mjs")), Is.EqualTo("C# Identity"));
        Assert.That(File.ReadAllText(Path.Combine(f.Home, "chat/ext/private-host/index.mjs")), Is.EqualTo("Custom host extension"));
        Assert.That(Fixture.Hashes(f.Source), Is.EquivalentTo(upstream));
        var outputFile = Path.Combine(f.Home, "chat/ui/ai.mjs"); var stamp = File.GetLastWriteTimeUtc(outputFile);
        Assert.That((await f.Run()).Output, Does.Contain("0 writes, 0 deletions")); Assert.That(File.GetLastWriteTimeUtc(outputFile), Is.EqualTo(stamp));
        Assert.That((await f.Run("--check")).Exit, Is.Zero);
    }

    [Test]
    public async Task Missing_input_fails_before_mutation_and_read_only_modes_leave_all_files_untouched()
    {
        using var f = new Fixture(); var original = Fixture.Hashes(f.Home);
        Assert.That((await f.Run("--check")).Exit, Is.EqualTo(1));
        Assert.That((await f.Run("--dry-run")).Exit, Is.Zero);
        Assert.That(Fixture.Hashes(f.Home), Is.EquivalentTo(original));
        File.Delete(Path.Combine(f.Source, "providers.json"));
        Assert.That((await f.Run()).Exit, Is.EqualTo(1)); Assert.That(Fixture.Hashes(f.Home), Is.EquivalentTo(original));
        Assert.That(Directory.Exists(Path.Combine(f.Home, ".chat-sync.lock")), Is.False);
    }

    [Test]
    public async Task Changed_backdated_source_updates_build_timestamp_and_manifest_without_manual_hash_edits()
    {
        using var f = new Fixture(); Assert.That((await f.Run()).Exit, Is.Zero);
        var target = Path.Combine(f.Home, "chat/ui/ai.mjs"); var old = DateTime.UtcNow.AddDays(-2); File.SetLastWriteTimeUtc(target, old);
        Fixture.Write(f.Source, "ui/ai.mjs", "updated shared bytes"); File.SetLastWriteTimeUtc(Path.Combine(f.Source, "ui/ai.mjs"), old.AddDays(-1));
        Assert.That((await f.Run()).Exit, Is.Zero); Assert.That(File.GetLastWriteTimeUtc(target), Is.GreaterThan(old));
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(f.Home, "chat/shared-assets.json")))!["files"]!;
        Assert.That(manifest["chat/ui/ai.mjs"]!.GetValue<string>(), Is.EqualTo(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(target))).ToLowerInvariant()));
        Assert.That((await f.Run("--check")).Exit, Is.Zero);
    }

    [Test]
    public async Task Focused_modes_preserve_unrelated_assets_and_full_sync_removes_only_previously_owned_extensions()
    {
        using var f = new Fixture(); Assert.That((await f.Run()).Exit, Is.Zero);
        Fixture.Write(f.Source, "ui/ai.mjs", "new core"); Fixture.Write(f.Source, "extensions/jev/ui/index.mjs", "new extension");
        Assert.That((await f.Run("--extension", "jev")).Exit, Is.Zero);
        Assert.That(File.ReadAllText(Path.Combine(f.Home, "chat/ui/ai.mjs")), Does.Not.Contain("new core"));
        Assert.That((await f.Run("--core")).Exit, Is.Zero);
        Directory.Delete(Path.Combine(f.Source, "extensions/jev"), true); Assert.That((await f.Run()).Exit, Is.Zero);
        Assert.That(Directory.Exists(Path.Combine(f.Home, "chat/ext/jev")), Is.False);
        Assert.That(File.Exists(Path.Combine(f.Home, "chat/ext/private-host/index.mjs")), Is.True);
    }

    [Test]
    public async Task Existing_sync_lock_rejects_concurrent_sync_without_removing_the_other_lock()
    {
        using var f = new Fixture(); Directory.CreateDirectory(Path.Combine(f.Home, ".chat-sync.lock")); var before = Fixture.Hashes(f.Home);
        Assert.That((await f.Run()).Exit, Is.EqualTo(1)); Assert.That(Fixture.Hashes(f.Home), Is.EquivalentTo(before));
        Assert.That(Directory.Exists(Path.Combine(f.Home, ".chat-sync.lock")), Is.True);
    }

    [Test]
    public async Task Failed_mid_sync_restores_original_files_and_removes_partial_copies()
    {
        using var f = new Fixture();
        Fixture.Write(f.Home, "rollback.py", """
import sys
from pathlib import Path
from unittest.mock import patch
import sync
home = Path(__file__).resolve().parent
writes, stale, roots, _ = sync.plan(home, Path(sys.argv[1]))
def snapshot():
    return {str(p.relative_to(home)): p.read_bytes() for p in home.rglob('*') if p.is_file()}
before = snapshot()
replace = sync.os.replace
calls = 0
def fail_once(*args):
    global calls
    calls += 1
    if calls == 3:
        raise OSError('Injected write failure')
    return replace(*args)
try:
    with patch.object(sync.os, 'replace', side_effect=fail_once):
        sync.apply(home, writes, stale, roots)
except OSError:
    pass
else:
    raise AssertionError('Expected injected failure')
assert snapshot() == before, 'Failed sync left partial asset changes'
print('Rollback verified')
""");
        var result = await f.RunScript("rollback.py"); Assert.That(result.Exit, Is.Zero, result.Output);
        Assert.That(result.Output, Does.Contain("Rollback verified"));
    }

    [Test]
    public async Task Destination_symlink_is_rejected_before_copying_or_deleting_any_assets()
    {
        if (OperatingSystem.IsWindows()) Assert.Ignore("Symlink creation requires additional Windows privileges");
        using var f = new Fixture(); var outside = Path.Combine(f.Source, "private"); Directory.CreateDirectory(outside); Fixture.Write(outside, "keep.mjs", "private bytes");
        Directory.Delete(Path.Combine(f.Home, "chat/ui"), true); Directory.CreateSymbolicLink(Path.Combine(f.Home, "chat/ui"), outside);
        var upstream = Fixture.Hashes(f.Source); Assert.That((await f.Run()).Exit, Is.EqualTo(1)); Assert.That(Fixture.Hashes(f.Source), Is.EquivalentTo(upstream));
    }
}
