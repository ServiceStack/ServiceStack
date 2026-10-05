#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.OrmLite;

namespace ServiceStack.AiTests;

public class AiChatMigrationGitTests
{
    sealed class Repository : IDisposable
    {
        public readonly AiChatMigrationTestHost Host=new();
        public readonly string Root;
        public readonly GitProcess Git;
        public readonly GitWorkspace View;
        public readonly GitOperations Operations;
        public readonly GitWorkspace.Location Location;
        public static JsonObject Author => new() { ["name"]="Test Author",["email"]="author@example.org" };
        public Repository(bool local=false) {
            Root=Path.Combine(Host.DirectoryPath,"repo");Directory.CreateDirectory(Root);
            Git=new GitProcess(localCredentials:local);View=new GitWorkspace(Git);Operations=new GitOperations(Host.Feature,View,new GitProvisioner(Git));Location=View.Resolve([Root],null);
            Git.CheckedAsync(Root,["init","-q","--initial-branch=main"]).GetAwaiter().GetResult();Location=View.Resolve([Root],null);
        }
        public void File(string name,string content)=>System.IO.File.WriteAllText(Path.Combine(Root,name),content);
        public async Task<JsonObject> State()=>await View.BrowseAsync(Location);
        public async Task<JsonObject> Action(string action,JsonObject? body=null) => await Operations.MutateAsync(Location,action,body??await State(),"alice",null,default);
        public async Task Commit(string text="initial",bool all=true) {
            var data=await State();data["message"]=text;data["requestId"]=Guid.NewGuid().ToString();data["identity"]=Author;
            await Action(all?"commit-all":"commit",data);
        }
        public void Dispose()=>Host.Dispose();
    }
    [TestCase(false)] [TestCase(true)]
    public async Task Stage_unstage_commit_discard_preserve_the_index_and_worktree(bool initial)
    {
        using var r=new Repository();r.File("space ü.txt","head\n");if(initial)await r.Commit();
        r.File("space ü.txt","index\n");await r.Action("stage",new JsonObject { ["paths"]=new JsonArray("space ü.txt") });
        r.File("space ü.txt","worktree\n");var state=await r.State();
        var unstaged=await r.View.DiffAsync(r.Location,Path.Combine(r.Root,"space ü.txt"),false);
        Assert.That(unstaged.GetString("patch"),Does.Contain("-index").And.Contain("+worktree"));
        var staged=await r.View.DiffAsync(r.Location,Path.Combine(r.Root,"space ü.txt"),true);
        Assert.That(staged.GetString("patch"),Does.Contain("+index"));
        var discard=new JsonObject { ["paths"]=new JsonArray("space ü.txt"),["indexRevision"]=state.GetString("indexRevision"),["worktreeRevision"]=state["changes"]![0]!["worktreeRevision"]!.DeepClone() };
        await r.Action("discard",discard);Assert.That(System.IO.File.ReadAllText(Path.Combine(r.Root,"space ü.txt")),Is.EqualTo("index\n"));
        Assert.That((await r.State())["stagedChanges"]!.AsArray().Count,Is.EqualTo(1));
        await r.Action("unstage-all");Assert.That((await r.State())["stagedChanges"]!.AsArray(),Is.Empty);
        Assert.That(System.IO.File.ReadAllText(Path.Combine(r.Root,"space ü.txt")),Is.EqualTo("index\n"));
    }
    [TestCase(false)] [TestCase(true)]
    public async Task Commit_receipts_replay_once_and_recover_a_failed_ref_update(bool all)
    {
        using var r=new Repository();r.File("one.txt","first\n");await r.Commit();r.File("one.txt","second\n");r.File("new.txt","new\n");
        if(!all)await r.Action("stage",new JsonObject { ["paths"]=new JsonArray("one.txt") });
        var body=await r.State();body["message"]="Second subject\n\nFull body";body["identity"]=Repository.Author;body["requestId"]=Guid.NewGuid().ToString();
        var previous=body.GetString("head")!;
        // update-ref fails after receipt persistence; repeat must use the same prepared object.
        System.IO.File.WriteAllText(Path.Combine(r.Root,".git","refs","heads","main.lock"),"");
        Assert.ThrowsAsync<HttpError>(async()=>await r.Action(all?"commit-all":"commit",body));
        System.IO.File.Delete(Path.Combine(r.Root,".git","refs","heads","main.lock"));
        var result=await r.Action(all?"commit-all":"commit",body);var replay=await r.Action(all?"commit-all":"commit",body);
        Assert.That(replay.GetString("revision"),Is.EqualTo(result.GetString("revision")));
        Assert.That((await r.Git.CheckedAsync(r.Root,["rev-list","--count","HEAD"])).Trim(),Is.EqualTo("2"));
        Assert.That((await r.State())["commits"]![0]!["message"]!.GetValue<string>(),Does.Contain("Full body"));
        body["message"]="different";Assert.ThrowsAsync<HttpError>(async()=>await r.Action(all?"commit-all":"commit",body));
        var undo=await r.Action("undo");Assert.That(undo.GetString("message"),Does.Contain("Full body"));
        Assert.That((await r.State()).GetString("head"),Is.EqualTo(previous));
    }
    [Test]
    public async Task Historical_diff_rename_delete_binary_and_no_final_newline_do_not_read_todays_file()
    {
        using var r=new Repository();r.File("old.txt","old\n");await r.Commit();System.IO.File.Move(Path.Combine(r.Root,"old.txt"),Path.Combine(r.Root,"renamed ü.txt"));await r.Commit("rename");
        var state=await r.State();var head=state.GetString("head")!;r.File("renamed ü.txt","unrelated today");
        var commit=await r.View.CommitAsync(r.Location,head);Assert.That(commit["changes"]![0]!["oldRelativePath"]!.GetValue<string>(),Is.EqualTo("old.txt"));
        var diff=await r.View.DiffAsync(r.Location,Path.Combine(r.Root,"renamed ü.txt"),false,head);Assert.That(diff.GetString("patch"),Does.Contain("rename from old.txt").And.Not.Contain("unrelated today"));
        r.File("new.txt","no newline");var newFile=await r.View.DiffAsync(r.Location,Path.Combine(r.Root,"new.txt"),false);Assert.That(newFile.GetString("patch"),Does.Contain("No newline at end of file"));
        System.IO.File.WriteAllBytes(Path.Combine(r.Root,"binary"),[1,0,2]);Assert.That((await r.View.DiffAsync(r.Location,Path.Combine(r.Root,"binary"),false)).GetString("message"),Does.Contain("Binary"));
        System.IO.File.WriteAllBytes(Path.Combine(r.Root,"large"),new byte[1024*1024+1]);Assert.That((await r.View.DiffAsync(r.Location,Path.Combine(r.Root,"large"),false)).GetString("message"),Does.Contain("too large"));
        Assert.ThrowsAsync<HttpError>(async()=>await r.View.DiffAsync(r.Location,Path.Combine(r.Host.DirectoryPath,"outside"),false));
        Assert.That(r.View.Resolve([Path.Combine(r.Root,".git")],null).Repository,Is.Null);
    }
    [Test]
    public async Task Stale_reviews_index_locks_filters_hooks_and_linked_metadata_block_side_effects()
    {
        using var r=new Repository();r.File("one.txt","one\n");await r.Commit();r.File("one.txt","two\n");var old=await r.State();r.File("one.txt","third\n");
        Assert.ThrowsAsync<HttpError>(async()=>await r.Action("discard-all",old));
        r.File(".gitattributes","one.txt filter=danger\n");await r.Git.CheckedAsync(r.Root,["config","filter.danger.clean","touch should-not-exist"]);
        Assert.ThrowsAsync<HttpError>(async()=>await r.Action("stage-all"));Assert.That(System.IO.File.Exists(Path.Combine(r.Root,"should-not-exist")),Is.False);
        System.IO.File.Delete(Path.Combine(r.Root,".gitattributes"));
        System.IO.File.WriteAllText(Path.Combine(r.Root,".git","index.lock"),"");Assert.ThrowsAsync<HttpError>(async()=>await r.Action("stage-all"));System.IO.File.Delete(Path.Combine(r.Root,".git","index.lock"));
        if(!OperatingSystem.IsWindows()) {
            Directory.CreateDirectory(Path.Combine(r.Root,".git","hooks"));var hook=Path.Combine(r.Root,".git","hooks","pre-commit");System.IO.File.WriteAllText(hook,"#!/bin/sh\ntouch should-not-exist\n");System.IO.File.SetUnixFileMode(hook,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
            await r.Commit();Assert.That(System.IO.File.Exists(Path.Combine(r.Root,"should-not-exist")),Is.False);
            System.IO.File.Move(Path.Combine(r.Root,".git","config"),Path.Combine(r.Host.DirectoryPath,"config"));System.IO.File.CreateSymbolicLink(Path.Combine(r.Root,".git","config"),Path.Combine(r.Host.DirectoryPath,"config"));
            Assert.ThrowsAsync<HttpError>(async()=>await r.Action("undo"));
        }
    }
    [Test]
    public async Task Submission_gate_and_other_users_captured_runs_exclude_repository_mutation()
    {
        using var r=new Repository();r.File("one.txt","one\n");
        using(r.Host.Feature.WorkspaceOperations.AcquireSubmission(r.Host.Feature.AppData.BasePath))Assert.ThrowsAsync<HttpError>(async()=>await r.Action("stage-all"));
        var thread=r.Host.Db.InsertThread(new ChatThread { User="bob",Messages="[]" });
        r.Host.Db.CreateAgentRun(thread,"bob","model",workspace:new JsonObject { ["directories"]=new JsonArray(r.Root) });
        Assert.ThrowsAsync<HttpError>(async()=>await r.Action("stage-all"));
        Assert.That((await r.State())["stagedChanges"]!.AsArray(),Is.Empty);
    }
    [TestCase("stash")] [TestCase("stash-untracked")] [TestCase("stash-staged")]
    public async Task Stash_apply_pop_preserve_untracked_policy_and_stash_review(string action)
    {
        using var r=new Repository();r.File("one.txt","one\n");await r.Commit();r.File("one.txt","two\n");r.File("new.txt","new\n");
        if(action=="stash-staged")await r.Action("stage",new JsonObject { ["paths"]=new JsonArray("one.txt") });
        var body=await r.State();body["identity"]=Repository.Author;await r.Action(action,body);
        var state=await r.State();Assert.That(state.GetString("stashId"),Is.Not.Null);
        Assert.That(System.IO.File.Exists(Path.Combine(r.Root,"new.txt")),Is.EqualTo(action!="stash-untracked"));
        var bad=state.Clone();bad["stashId"]="changed";Assert.ThrowsAsync<HttpError>(async()=>await r.Action("stash-pop",bad));
        await r.Action("stash-pop",state);Assert.That(System.IO.File.ReadAllText(Path.Combine(r.Root,"one.txt")),Is.EqualTo("two\n"));Assert.That((await r.State()).GetString("stashId"),Is.Null);
    }
    [Test]
    public async Task Init_is_main_without_fake_commit_and_hosted_clone_policy_is_explicit()
    {
        using var host=new AiChatMigrationTestHost();var git=new GitProcess();var provisioner=new GitProvisioner(git);
        var directory=Path.Combine(host.DirectoryPath,"new");await provisioner.ProvisionAsync(directory,new JsonObject { ["kind"]="new" },"alice",default);
        Assert.That((await git.CheckedAsync(directory,["symbolic-ref","--short","HEAD"])).Trim(),Is.EqualTo("main"));Assert.That((await git.RunAsync(directory,["rev-parse","--verify","HEAD"])).Code,Is.Not.Zero);
        Assert.That(Directory.GetFiles(directory),Is.Empty);
        Assert.Throws<HttpError>(()=>provisioner.Validate(new JsonObject { ["kind"]="clone",["url"]="git@github.com:org/repo.git" }));
        Assert.Throws<HttpError>(()=>provisioner.Validate(new JsonObject { ["kind"]="clone",["url"]="https://operator:secret@github.com/org/repo.git" }));
        Assert.Throws<HttpError>(()=>provisioner.Validate(new JsonObject { ["kind"]="clone",["url"]="https://internal.example/org/repo.git" }));
        Assert.Throws<HttpError>(()=>provisioner.Validate(new JsonObject { ["kind"]="clone",["url"]="https://github.com/org/repo.git",["branch"]="../bad" }));
        Assert.That(provisioner.Validate(new JsonObject { ["kind"]="clone",["url"]="https://github.com/org/repo.git" }).GetString("kind"),Is.EqualTo("clone"));
    }
    [Test]
    public async Task Local_sync_push_pull_and_divergence_keep_reviewed_history()
    {
        using var r=new Repository(local:true);r.File("one.txt","one\n");await r.Commit();var remote=Path.Combine(r.Host.DirectoryPath,"remote.git");Directory.CreateDirectory(remote);await r.Git.CheckedAsync(remote,["init","--bare","-q"]);
        await r.Git.CheckedAsync(r.Root,["remote","add","origin",remote]);
        var body=await r.State();body["remoteBranch"]="main";await r.Action("sync",body);Assert.That((await r.State())["remotes"]![0]!["ahead"]!.GetValue<int>(),Is.Zero);
        var other=Path.Combine(r.Host.DirectoryPath,"other");await r.Git.CheckedAsync(r.Host.DirectoryPath,["clone","-q","--branch","main",remote,other]);System.IO.File.WriteAllText(Path.Combine(other,"incoming"),"remote\n");await r.Git.CheckedAsync(other,["add","--all"]);await r.Git.CheckedAsync(other,["commit","-q","-m","incoming"],identity:Repository.Author);await r.Git.CheckedAsync(other,["push","-q"]);
        body=await r.State();body["remoteBranch"]="main";await r.Action("pull",body);Assert.That(System.IO.File.ReadAllText(Path.Combine(r.Root,"incoming")),Is.EqualTo("remote\n"));
        r.File("local","local\n");await r.Commit("local");System.IO.File.WriteAllText(Path.Combine(other,"other"),"other\n");await r.Git.CheckedAsync(other,["add","--all"]);await r.Git.CheckedAsync(other,["commit","-q","-m","diverged"],identity:Repository.Author);await r.Git.CheckedAsync(other,["push","-q"]);
        body=await r.State();var head=body.GetString("head");body["remoteBranch"]="main";Assert.ThrowsAsync<HttpError>(async()=>await r.Action("sync",body));Assert.That((await r.State()).GetString("head"),Is.EqualTo(head));
    }
    [Test]
    public async Task Fast_forward_pull_accepts_merge_driver_attributes_but_not_content_filters()
    {
        // A fast-forward never runs merge drivers (llms-py only rejects filters on pull)
        using var r=new Repository(local:true);r.File("one.txt","one\n");await r.Commit();var remote=Path.Combine(r.Host.DirectoryPath,"remote.git");Directory.CreateDirectory(remote);await r.Git.CheckedAsync(remote,["init","--bare","-q"]);
        await r.Git.CheckedAsync(r.Root,["remote","add","origin",remote]);
        var body=await r.State();body["remoteBranch"]="main";await r.Action("sync",body);
        var other=Path.Combine(r.Host.DirectoryPath,"other");await r.Git.CheckedAsync(r.Host.DirectoryPath,["clone","-q","--branch","main",remote,other]);
        async Task Push(string file,string text){System.IO.File.WriteAllText(Path.Combine(other,file),text);await r.Git.CheckedAsync(other,["add","--all"]);await r.Git.CheckedAsync(other,["commit","-q","-m",file],identity:Repository.Author);await r.Git.CheckedAsync(other,["push","-q"]);}
        await Push(".gitattributes","CHANGELOG.md merge=union\n");await Push("CHANGELOG.md","entry\n");
        body=await r.State();body["remoteBranch"]="main";await r.Action("pull",body);Assert.That(System.IO.File.ReadAllText(Path.Combine(r.Root,"CHANGELOG.md")),Is.EqualTo("entry\n"));
        await Push(".gitattributes","CHANGELOG.md merge=union\n*.bin filter=lfs\n");var head=(await r.State()).GetString("head");
        body=await r.State();body["remoteBranch"]="main";Assert.ThrowsAsync<HttpError>(async()=>await r.Action("pull",body));Assert.That((await r.State()).GetString("head"),Is.EqualTo(head));
    }

    [Test]
    public async Task Extension_routes_are_scoped_and_disabled_dependencies_do_not_advertise_git()
    {
        using var host=new AiChatMigrationTestHost("/chat");var unavailable=host.Install(new GitExtension());Assert.That(unavailable.Disabled,Is.True);
        host.Install(new ProjectsExtension());var root=Path.Combine(host.DirectoryPath,"root");Directory.CreateDirectory(root);host.Feature.SetAllowedDirectories([root],"alice");
        var extension=host.Install(new GitExtension());Assert.That(extension.Disabled,Is.False);
        var options=(JsonObject)(await host.SendAsync("GET","/chat/ext/projects/creation/options","alice"))!;Assert.That(options.GetBool("initializeGit"),Is.True);
        Assert.That(await host.SendAsync("GET","/chat/ext/git/workspace","bob"),Is.TypeOf<JsonObject>());
        var denied=await host.SendAsync("GET","/chat/ext/git/workspace",null);Assert.That(((ChatResult)denied!).Status,Is.EqualTo(401));
    }
    [Test]
    public async Task Message_generation_sends_full_staged_diff_without_tools_history_or_filters()
    {
        using var r=new Repository();r.File("one.txt","one\n");r.File("two.txt","two\n");await r.Action("stage-all");
        var provider=new FakeChatProvider(ChatJson.ParseObject("""{"choices":[{"message":{"role":"assistant","content":"Commit message: \"Add both files\""}}]}""")) {Id="test",Models={ ["model"]=new JsonObject { ["id"]="model",["limit"]=new JsonObject { ["context"]=32768 } } }};
        r.Host.Feature.Providers["test"]=provider;r.Host.Feature.Config["defaults"]=new JsonObject { ["commit"]=new JsonObject { ["model"]="model",["messages"]=new JsonArray(new JsonObject { ["role"]="user",["content"]="{diffs}" }) } };
        r.Host.Feature.Filters.ChatRequestFilters.Add((_,_)=>throw new Exception("helper ran workspace filter"));
        var generator=new GitCommitMessages(r.Host.Feature,r.View,r.Operations);var result=await generator.GenerateAsync(r.Location,await r.State(),"alice",default);
        Assert.That(result.GetString("message"),Is.EqualTo("Add both files"));Assert.That(provider.ReceivedChat!.GetArray("messages")!.ToJsonString(),Does.Contain("one.txt").And.Contain("two.txt"));Assert.That(provider.ReceivedChat.ContainsKey("tools"),Is.False);
        Assert.ThrowsAsync<HttpError>(async()=>await generator.GenerateAsync(r.Location,new JsonObject { ["indexRevision"]="stale" },"alice",default));
    }
    [Test]
    public async Task Hosted_credentials_rewrites_and_shared_commit_undo_are_rejected()
    {
        using var r=new Repository();r.File("one","one\n");await r.Commit();
        await r.Git.CheckedAsync(r.Root,["remote","add","origin","https://github.com/org/repo.git"]);
        var body=await r.State();body["remoteBranch"]="main";Assert.ThrowsAsync<HttpError>(async()=>await r.Action("push",body));
        await r.Git.CheckedAsync(r.Root,["config","http.https://github.com/.extraHeader","Authorization: secret"]);
        Assert.ThrowsAsync<HttpError>(async()=>await r.Action("pull",body));
        await r.Git.CheckedAsync(r.Root,["config","--unset","http.https://github.com/.extraHeader"]);
        await r.Git.CheckedAsync(r.Root,["config","url.ssh://internal/.insteadOf","https://github.com/"]);
        Assert.ThrowsAsync<HttpError>(async()=>await r.Action("pull",body));
        await r.Git.CheckedAsync(r.Root,["update-ref","refs/remotes/origin/main",body.GetString("head")!]);
        Assert.That((await r.State()).GetBool("canUndo"),Is.False);Assert.ThrowsAsync<HttpError>(async()=>await r.Action("undo"));
    }
    [Test]
    public async Task Root_commit_undo_and_same_head_branch_switch_are_reviewed()
    {
        using var r=new Repository();r.File("one","one\n");await r.Commit();var body=await r.State();
        await r.Git.CheckedAsync(r.Root,["branch","other"]);await r.Git.CheckedAsync(r.Root,["symbolic-ref","HEAD","refs/heads/other"]);
        Assert.ThrowsAsync<HttpError>(async()=>await r.Action("undo",body));
        await r.Action("undo");Assert.That((await r.State()).GetString("head"),Is.Empty);Assert.That((await r.State())["stagedChanges"]!.AsArray().Count,Is.EqualTo(1));Assert.That(System.IO.File.ReadAllText(Path.Combine(r.Root,"one")),Is.EqualTo("one\n"));
    }
    [Test]
    public async Task Real_creation_advertises_git_and_persists_main_repository_without_identity()
    {
        using var host=new AiChatMigrationTestHost();var projects=host.Install(new ProjectsExtension());host.Install(new GitExtension());
        var accepted=(ChatResult)(await host.SendAsync("POST","/ext/projects/create","alice",new JsonObject { ["requestId"]=Guid.NewGuid().ToString(),["project"]=new JsonObject { ["name"]="Git project",["folder"]="git-project" },["source"]=new JsonObject { ["kind"]="new",["initializeGit"]=true } }))!;
        var snapshot=ChatJson.ParseObject(accepted.Text!);var deadline=DateTime.UtcNow.AddSeconds(5);
        while(snapshot.GetString("state")!="succeeded" && DateTime.UtcNow<deadline) {await Task.Delay(20);snapshot=await projects.Creation!.GetAsync("alice",snapshot.GetString("id")!,null,default);}
        Assert.That(snapshot.GetString("state"),Is.EqualTo("succeeded"),snapshot.ToJsonString());
        var id=snapshot.GetObject("result").GetObject("project").GetString("id");var state=(JsonObject)(await host.SendAsync("GET","/ext/git/workspace?projectId="+id,"alice"))!;
        Assert.That(state.GetString("branch"),Is.EqualTo("main"));Assert.That(state.GetString("head"),Is.Empty);
    }
    [Test]
    public async Task Unsafe_incoming_attributes_block_pull_without_running_a_filter()
    {
        using var r=new Repository(local:true);r.File("one","one\n");await r.Commit();var remote=Path.Combine(r.Host.DirectoryPath,"bare");Directory.CreateDirectory(remote);await r.Git.CheckedAsync(remote,["init","--bare","-q"]);await r.Git.CheckedAsync(r.Root,["remote","add","origin",remote]);var body=await r.State();body["remoteBranch"]="main";await r.Action("push",body);
        var other=Path.Combine(r.Host.DirectoryPath,"other");await r.Git.CheckedAsync(r.Host.DirectoryPath,["clone","-q","--branch","main",remote,other]);System.IO.File.WriteAllText(Path.Combine(other,".gitattributes"),"* filter=danger\n");System.IO.File.WriteAllText(Path.Combine(other,"new"),"new\n");await r.Git.CheckedAsync(other,["add","--all"]);await r.Git.CheckedAsync(other,["commit","-q","-m","attributes"],identity:Repository.Author);await r.Git.CheckedAsync(other,["push","-q"]);
        await r.Git.CheckedAsync(r.Root,["config","filter.danger.smudge","touch should-not-exist"]);body=await r.State();body["remoteBranch"]="main";var head=body.GetString("head");Assert.ThrowsAsync<HttpError>(async()=>await r.Action("pull",body));Assert.That((await r.State()).GetString("head"),Is.EqualTo(head));Assert.That(System.IO.File.Exists(Path.Combine(r.Root,"should-not-exist")),Is.False);
    }
    [Test]
    public async Task Model_context_and_explicit_disabled_generation_reject_before_provider_call()
    {
        using var r=new Repository();r.File("one",new string('x',10000));await r.Action("stage-all");var provider=new FakeChatProvider(new JsonObject()) { Id="test",Models={ ["model"]=new JsonObject { ["id"]="model",["limit"]=new JsonObject { ["context"]=1000 } } } };r.Host.Feature.Providers["test"]=provider;
        var defaults=r.Host.Feature.Config.GetObject("defaults")!;defaults["commit"]=new JsonObject { ["model"]="model" };var generator=new GitCommitMessages(r.Host.Feature,r.View,r.Operations);
        Assert.ThrowsAsync<HttpError>(async()=>await generator.GenerateAsync(r.Location,await r.State(),"alice",default));Assert.That(provider.ReceivedChat,Is.Null);
        defaults["commit"]=null;Assert.That(generator.Template(),Is.Null);
    }
    [Test]
    public async Task Bounded_process_cancellation_clears_child_and_kills_descendants()
    {
        if(OperatingSystem.IsWindows())Assert.Ignore("POSIX test executable; production process-tree cancellation uses the cross-platform Process API");
        using var host=new AiChatMigrationTestHost();var executable=Path.Combine(host.DirectoryPath,"fake-git");System.IO.File.WriteAllText(executable,"#!/bin/sh\nfor arg in \"$@\"; do if [ \"$arg\" = config ]; then exit 1; fi; done\necho 'Receiving objects: 35%' >&2\nsleep 60 &\nwait\n");System.IO.File.SetUnixFileMode(executable,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
        var provisioner=new GitProvisioner(new GitProcess(executable));var started=new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);int? child=null;string? phase=null;
        using var cancel=new CancellationTokenSource();var task=provisioner.ProvisionAsync(Path.Combine(host.DirectoryPath,"new"),new JsonObject { ["kind"]="clone",["url"]="https://github.com/org/repo" },"alice",(label,percent)=>{phase=label+percent;return Task.CompletedTask;},pid=>{child=pid;if(pid.HasValue)started.TrySetResult(pid.Value);return Task.CompletedTask;},cancel.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));await Task.Delay(100);cancel.Cancel();Assert.That(async()=>await task.WaitAsync(TimeSpan.FromSeconds(2)),Throws.InstanceOf<OperationCanceledException>());Assert.That(child,Is.Null);Assert.That(phase,Is.EqualTo("Receiving objects35"));
    }
    sealed class CloneFixture(string repository) : GitProcess
    {
        public override Task<Result> RunAsync(string repo, System.Collections.Generic.IEnumerable<string> args, CancellationToken token = default,
            string? index = null, JsonObject? identity = null, System.Collections.Generic.IEnumerable<string>? config = null,
            System.Collections.Generic.IReadOnlyDictionary<string,string>? environment = null, int seconds = 30, int maxBytes = 1024*1024,
            Func<int?,Task>? child = null, Func<string,Task>? diagnostic = null, bool tailError = false)
        {
            var arguments = args.Select(x => x == "https://github.com/fixture/repo.git" ? repository : x).ToArray();
            return base.RunAsync(repo, arguments, token, index, identity, (config ?? []).Concat(["protocol.file.allow=always"]), environment, seconds, maxBytes, child, diagnostic, tailError);
        }
    }
    [Test]
    public async Task Clone_provisioning_keeps_selected_branch_history_and_origin_without_fake_files()
    {
        using var r = new Repository(); r.File("one", "one\n"); await r.Commit();
        await r.Git.CheckedAsync(r.Root, ["checkout", "-b", "selected"]); r.File("two", "two\n"); await r.Commit("selected commit");
        var directory = Path.Combine(r.Host.DirectoryPath,"clone");
        await new GitProvisioner(new CloneFixture(r.Root)).ProvisionAsync(directory,
            new JsonObject { ["kind"]="clone",["url"]="https://github.com/fixture/repo.git",["branch"]="selected" },"alice",default);
        Assert.That((await r.Git.CheckedAsync(directory,["symbolic-ref","--short","HEAD"])).Trim(),Is.EqualTo("selected"));
        Assert.That((await r.Git.CheckedAsync(directory,["rev-list","--count","HEAD"])).Trim(),Is.EqualTo("2"));
        Assert.That((await r.Git.CheckedAsync(directory,["remote","get-url","origin"])).Trim(),Is.EqualTo(r.Root));
        Assert.That(System.IO.File.ReadAllText(Path.Combine(directory,"two")),Is.EqualTo("two\n"));
    }
}
