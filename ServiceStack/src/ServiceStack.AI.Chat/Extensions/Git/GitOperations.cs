using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ServiceStack.OrmLite;

namespace ServiceStack.AI;

public sealed partial class GitOperations(ChatFeature feature, GitWorkspace workspace, GitProvisioner provisioner)
{
    GitProcess Git => workspace.Git;
    static HttpError Conflict(string message="The repository changed. Refresh and review its changes before trying again.") => new(409,"GitConflict",message);
    static string Metadata(string repo,string path) => Path.Combine(repo,".git",path);
    public async Task ValidateAsync(GitWorkspace.Location loc,CancellationToken token)
    {
        var repo=loc.Repository ?? throw HttpError.BadRequest("This directory has no Git repository");
        var metadata=Metadata(repo,"");
        if(!Directory.Exists(metadata) || new DirectoryInfo(metadata).LinkTarget!=null || !ProjectsExtension.IsWithin(ProjectsExplorer.PhysicalPath(metadata),repo))throw HttpError.BadRequest("Linked worktrees and external Git directories are not supported for changes.");
        var top=(await Git.CheckedAsync(repo,["rev-parse","--show-toplevel"],token).ConfigureAwait(false)).Trim();
        var common=(await Git.CheckedAsync(repo,["rev-parse","--git-common-dir"],token).ConfigureAwait(false)).Trim();
        if(ProjectsExplorer.PhysicalPath(top)!=ProjectsExplorer.PhysicalPath(repo) || ProjectsExplorer.PhysicalPath(Path.Combine(repo,common))!=ProjectsExplorer.PhysicalPath(metadata))throw HttpError.Forbidden("Git metadata is outside the repository");
        var critical=new List<string> {"config","index","HEAD","FETCH_HEAD","ORIG_HEAD","packed-refs","refs/stash","logs/refs/stash","objects","objects/info","objects/pack","refs","logs","llms-locks","llms-commits"};
        critical.AddRange(Enumerable.Range(0,256).Select(x=>"objects/"+x.ToString("x2")));
        var branch=await Git.RunAsync(repo,["symbolic-ref","--quiet","HEAD"],token).ConfigureAwait(false);
        if(branch.Code==0) {var reference=branch.Output.Trim();if(!reference.StartsWith("refs/"))throw HttpError.BadRequest("Unsupported Git reference");critical.Add(reference);critical.Add("logs/"+reference);}
        foreach(var name in critical)RejectLinks(metadata,name);
        foreach(var name in new[]{"MERGE_HEAD","CHERRY_PICK_HEAD","REVERT_HEAD","rebase-merge","rebase-apply","BISECT_LOG"})if(File.Exists(Metadata(repo,name)) || Directory.Exists(Metadata(repo,name)))throw Conflict("Finish the merge, rebase, or other Git operation first.");
        if((await Git.CheckedAsync(repo,["ls-files","--unmerged","-z"],token).ConfigureAwait(false)).Length>0)throw Conflict("Resolve merge conflicts first.");
    }
    static void RejectLinks(string root,string relative) {
        var target=Path.Combine(root,relative);
        while(target!=root) {FileSystemInfo info=Directory.Exists(target)?new DirectoryInfo(target):new FileInfo(target);if(info.LinkTarget!=null || info.Exists && (info.Attributes&FileAttributes.ReparsePoint)!=0)throw HttpError.Forbidden("Linked Git metadata is unsupported");target=Path.GetDirectoryName(target)!;}
        if(!ProjectsExtension.IsWithin(ProjectsExplorer.PhysicalPath(Path.Combine(root,relative)),root))throw HttpError.Forbidden("Git metadata is outside the repository");
    }
    public string[] Paths(GitWorkspace.Location loc,JsonNode? input)
    {
        if(input is not JsonArray array || array.Count is 0 or >10000 || array.Any(x=>x is not JsonValue value || !value.TryGetValue<string>(out _)))throw HttpError.BadRequest("Select files to change");
        return ValidatePaths(loc,array.Select(x=>x!.GetValue<string>()));
    }
    public string[] ValidatePaths(GitWorkspace.Location loc,IEnumerable<string> paths)
    {
        var result=paths.Distinct().ToArray();if(result.Length>10000)throw HttpError.BadRequest("Too many repository files");
        foreach(var filename in result) {
            if(filename.Length==0 || filename.Contains('\0') || Path.IsPathRooted(filename) || filename.Replace('\\','/').Split('/').Any(x=>x==".."))throw HttpError.BadRequest("Invalid repository path");
            var target=ProjectsExplorer.PhysicalPath(Path.Combine(loc.Repository!,filename));
            if(!ProjectsExtension.IsWithin(target,loc.Root!) || !ProjectsExtension.IsWithin(target,loc.Repository!) || ProjectsExtension.IsWithin(target,Metadata(loc.Repository!,"")))throw HttpError.Forbidden("File is outside the allowed repository files");
        }
        return result;
    }
    /// <summary>
    /// Reject links, submodules, content filters and (unless <paramref name="mergeDrivers"/> is false)
    /// custom merge drivers. Fast-forward pulls never run merge drivers, so they only check filters.
    /// </summary>
    public async Task<string[]> SafePathsAsync(GitWorkspace.Location loc,IEnumerable<string> selection,CancellationToken token,IEnumerable<string>? revisions=null,bool mergeDrivers=true)
    {
        var paths=ValidatePaths(loc,selection);var repo=loc.Repository!;
        foreach(var file in paths) {var target=Path.Combine(repo,file);if(Directory.Exists(target))throw HttpError.BadRequest("Use your Git client for directories or submodules.");RejectLinks(repo,file);}
        if(paths.Length>0) {
            var attributes=(await Git.CheckedAsync(repo,mergeDrivers?["check-attr","-z","filter","merge","--",..paths]:["check-attr","-z","filter","--",..paths],token).ConfigureAwait(false)).Split('\0');
            for(var i=0;i+2<attributes.Length;i+=3)if(attributes[i+1]=="filter"?attributes[i+2] is not ("unspecified" or "unset"):attributes[i+2] is not ("unspecified" or "unset" or "set" or "text" or "binary" or "union"))throw HttpError.BadRequest("These changes use content filters or merge drivers. Use your Git client.");
            var entries=await Git.CheckedAsync(repo,["ls-files","--stage","-z","--",..paths],token).ConfigureAwait(false);
            CheckModes(entries);
        }
        foreach(var rev in revisions??[]) {
            if(paths.Length>0)CheckModes(await Git.CheckedAsync(repo,["ls-tree","-r","-z",rev,"--",..paths],token).ConfigureAwait(false));
            var pattern=mergeDrivers?@"(^|[[:space:]])(filter(=|[[:space:]]|$)|merge=)":@"(^|[[:space:]])filter(=|[[:space:]]|$)";
            var attributes=await Git.RunAsync(repo,["grep","-E","-e",pattern,rev,"--",":(glob)**/.gitattributes"],token,environment:new Dictionary<string,string> { ["GIT_LITERAL_PATHSPECS"]="0" }).ConfigureAwait(false);
            if(attributes.Code!=1)throw HttpError.BadRequest("These changes use Git attributes or merge drivers. Use your Git client.");
        }
        return paths;
    }
    static void CheckModes(string entries) {if(entries.Split('\0',StringSplitOptions.RemoveEmptyEntries).Any(x=>x.Split(' ')[0] is not ("100644" or "100755")))throw HttpError.BadRequest("Use your Git client for links or submodules.");}
    void AssertInactive(string repo,string user,string? projectId)
    {
        if(feature.ChatDb==null)return;
        using var db=feature.ChatDb.OpenDb();
        foreach(var run in db.Select<AgentRun>(x=>x.Status==AgentRunStatus.Queued || x.Status==AgentRunStatus.Running || x.Status==AgentRunStatus.WaitingApproval)) {
            var thread=feature.ChatDb.GetThread(run.ThreadId,run.User);
            if(run.User==user && thread?.ProjectId==projectId)throw Conflict("An agent is active in this workspace. Wait for it to finish.");
            var capture=ChatJson.TryParseObject(run.Workspace);
            foreach(var directory in capture.GetArray("directories")?.OfType<JsonValue>().Select(x=>x.GetValue<string>())??[])if(ProjectsExtension.IsWithin(repo,ProjectsExplorer.PhysicalPath(directory)) || ProjectsExtension.IsWithin(ProjectsExplorer.PhysicalPath(directory),repo))throw Conflict("An agent is active in this workspace. Wait for it to finish.");
        }
    }
    public async Task CheckReviewAsync(string repo,JsonObject data,JsonObject body,CancellationToken token)
    {
        var state=await workspace.StateAsync(repo,token).ConfigureAwait(false);
        if(body.GetString("branch")!=data.GetString("branch") || body.GetString("head")!=data.GetString("head") || body.GetString("indexRevision")!=data.GetString("indexRevision") || state.GetString("indexRevision")!=data.GetString("indexRevision") || body.GetString("workspaceRevision")!=await workspace.WorkspaceRevisionAsync(repo,state.GetString("indexRevision")!,token).ConfigureAwait(false))throw Conflict();
    }
    static IDisposable IndexLock(string repo)
    {
        var path=Metadata(repo,"index.lock");FileStream stream;
        try {stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None);}catch(IOException){throw Conflict("Another Git operation has locked the index. Wait and refresh.");}
        return new Cleanup(()=>{stream.Dispose();File.Delete(path);});
    }
    sealed class Cleanup(Action cleanup):IDisposable {Action? action=cleanup;public void Dispose()=>Interlocked.Exchange(ref action,null)?.Invoke();}
    static string CopyIndex(string repo) {var file=Metadata(repo,"llms-index-"+Guid.NewGuid().ToString("N"));if(File.Exists(Metadata(repo,"index")))File.Copy(Metadata(repo,"index"),file);return file;}
    public async Task<JsonObject> MutateAsync(GitWorkspace.Location loc,string action,JsonObject body,string user,string? projectId,CancellationToken token)
    {
        var repo=loc.Repository??throw HttpError.BadRequest("This directory has no Git repository");
        using var lease=await feature.WorkspaceOperations.AcquireMutationAsync(feature.AppData.BasePath,repo,token).ConfigureAwait(false);
        await ValidateAsync(loc,token).ConfigureAwait(false);AssertInactive(repo,user,projectId);
        var data=await workspace.BrowseAsync(loc,token).ConfigureAwait(false);
        if(action is "push" or "pull" or "sync")return await SyncAsync(loc,data,action,body,token).ConfigureAwait(false);
        if(action is "commit" or "commit-all") {using var indexLock=IndexLock(repo);return await CommitAsync(loc,data,body,user,action=="commit-all",token).ConfigureAwait(false);}
        if(action is "stage" or "unstage" or "stage-all" or "unstage-all") {
            var all=action.EndsWith("-all");var stage=action.StartsWith("stage");
            if(all)await CheckReviewAsync(repo,data,body,token).ConfigureAwait(false);
            var entries=data[stage?"changes":"stagedChanges"]!.AsArray().OfType<JsonObject>().ToArray();
            var paths=all?entries.Select(x=>x.GetString("relativePath")!).ToArray():Paths(loc,body["paths"]);
            if(paths.Any(x=>!entries.Any(e=>e.GetString("relativePath")==x)))throw Conflict("The selected files changed. Refresh and select them again.");
            paths=paths.Concat(entries.Where(x=>paths.Contains(x.GetString("relativePath"))).Select(x=>x.GetString("oldRelativePath")).Where(x=>x!=null).Cast<string>()).Distinct().ToArray();
            if(stage)paths=await SafePathsAsync(loc,paths,token).ConfigureAwait(false);else ValidatePaths(loc,paths);
            using var indexLock=IndexLock(repo);var temporary=CopyIndex(repo);
            try {if(all)await CheckReviewAsync(repo,data,body,token).ConfigureAwait(false);
                if(paths.Length>0)await Git.CheckedAsync(repo,stage?["add","--all","--",..paths]:data.GetString("head")==""?["rm","--cached","-r","-f","--",..paths]:["restore","--staged","--",..paths],token,index:temporary).ConfigureAwait(false);
                if(File.Exists(temporary))File.Move(temporary,Metadata(repo,"index"),true);
            }finally{File.Delete(temporary);File.Delete(temporary+".lock");}
        } else if(action is "discard" or "discard-all") {
            var entries=data["changes"]!.AsArray().OfType<JsonObject>().ToArray();
            var paths=action=="discard-all"?entries.Select(x=>x.GetString("relativePath")!).ToArray():Paths(loc,body["paths"]);
            if(paths.Length==0 || action=="discard" && paths.Length!=1)throw HttpError.BadRequest("Select unstaged files to discard");
            if(paths.Any(x=>!entries.Any(e=>e.GetString("relativePath")==x)))throw Conflict();
            await SafePathsAsync(loc,paths,token).ConfigureAwait(false);
            using var indexLock=IndexLock(repo);
            if(action=="discard-all")await CheckReviewAsync(repo,data,body,token).ConfigureAwait(false);
            else if(body.GetString("indexRevision")!=(await workspace.StateAsync(repo,token).ConfigureAwait(false)).GetString("indexRevision") || body.GetString("worktreeRevision")!=GitWorkspace.WorktreeRevision(repo,paths[0]))throw Conflict();
            var tracked=paths.Where(x=>entries.First(e=>e.GetString("relativePath")==x).GetString("status")!="?").ToArray();
            if(tracked.Length>0)await Git.CheckedAsync(repo,["checkout-index","--force","--",..tracked],token).ConfigureAwait(false);
            foreach(var path in paths.Except(tracked))File.Delete(Path.Combine(repo,path));
        } else if(action=="undo") {
            await CheckReviewAsync(repo,data,body,token).ConfigureAwait(false);
            if(!data.GetBool("canUndo"))throw Conflict(data.GetString("undoReason")!);
            using var indexLock=IndexLock(repo);await CheckReviewAsync(repo,data,body,token).ConfigureAwait(false);
            var head=data.GetString("head")!;var parents=(await Git.CheckedAsync(repo,["rev-list","--parents","-n","1",head],token).ConfigureAwait(false)).Trim().Split(' ').Skip(1).ToArray();
            await Git.CheckedAsync(repo,parents.Length>0?["update-ref","-m","llms: undo last commit","HEAD",parents[0],head]:["update-ref","-d","HEAD",head],token).ConfigureAwait(false);
            return new JsonObject { ["operation"]="undo",["message"]=(await Git.CheckedAsync(repo,["show","-s","--format=%B",head],token).ConfigureAwait(false)).Trim() };
        } else if(action.StartsWith("stash")) {await CheckReviewAsync(repo,data,body,token).ConfigureAwait(false);return await StashAsync(loc,data,action,body,token).ConfigureAwait(false);}
        else throw HttpError.BadRequest("Unknown repository operation");
        return new JsonObject { ["operation"]=action };
    }
    public static JsonObject Identity(JsonObject body,JsonObject state) {
        var identity=body.GetObject("identity")??state.GetObject("identity");
        if(identity==null || new[]{"name","email"}.Any(x=>identity.GetString(x) is not { } value || string.IsNullOrWhiteSpace(value) || value.Length>256 || value.Any(c=>"\r\n\0<>".Contains(c))))throw HttpError.BadRequest("Enter your Git author name and email");
        return new JsonObject { ["name"]=identity.GetString("name")!.Trim(),["email"]=identity.GetString("email")!.Trim() };
    }
    async Task<JsonObject> CommitAsync(GitWorkspace.Location loc,JsonObject data,JsonObject body,string user,bool all,CancellationToken token)
    {
        var repo=loc.Repository!;var message=body.GetString("message")?.Trim();var request=body.GetString("requestId");
        if(string.IsNullOrEmpty(message) || message.Length>10000 || message.Contains('\0'))throw HttpError.BadRequest("Enter a commit message (up to 10,000 characters)");
        if(request==null || !Regex.IsMatch(request,"^[a-zA-Z0-9-]{16,80}$"))throw HttpError.BadRequest("A commit request ID is required");
        var identity=Identity(body,data);
        var fingerprint=GitProcess.Hash(new JsonArray(user,message,identity.DeepClone(),body.GetString("indexRevision"),all?"all":null,all?body.GetString("workspaceRevision"):null).ToJsonString());
        var receipts=Metadata(repo,"llms-commits");Directory.CreateDirectory(receipts);
        var path=Path.Combine(receipts,GitProcess.Hash(user+request)+".json");RejectLinks(receipts,Path.GetFileName(path));
        var previous=data.GetString("head")!;string revision;
        if(File.Exists(path)) {
            var receipt=ChatJson.ParseObject(File.ReadAllText(path));
            if(receipt.GetString("fingerprint")!=fingerprint)throw Conflict("This commit request was already used. Refresh before committing again.");
            revision=receipt.GetString("revision")!;
            if(previous==revision || previous!="" && (await Git.RunAsync(repo,["merge-base","--is-ancestor",revision,"HEAD"],token).ConfigureAwait(false)).Code==0)return new JsonObject { ["operation"]="commit",["revision"]=revision };
            if(previous!=receipt.GetString("previous"))throw Conflict("The branch changed during the commit. Inspect Recent commits before retrying.");
        } else {
            if(body.GetString("indexRevision")!=data.GetString("indexRevision"))throw Conflict("Staged changes changed. Refresh before committing.");
            if(all)await CheckReviewAsync(repo,data,body,token).ConfigureAwait(false);
            var temporary=CopyIndex(repo);
            try {
                if(all) {var paths=await SafePathsAsync(loc,data["changes"]!.AsArray().Concat(data["stagedChanges"]!.AsArray()).OfType<JsonObject>().Select(x=>x.GetString("relativePath")!),token).ConfigureAwait(false);if(paths.Length==0)throw HttpError.BadRequest("There are no changes to commit");await Git.CheckedAsync(repo,["add","--all","--",..paths],token,index:temporary).ConfigureAwait(false);await CheckReviewAsync(repo,data,body,token).ConfigureAwait(false);}
                else if(data["stagedChanges"]!.AsArray().Count==0)throw HttpError.BadRequest("Stage at least one change before committing");
                var staged=(await Git.CheckedAsync(repo,["diff","--cached","--name-only","--no-renames","-z","--"],token,index:temporary).ConfigureAwait(false)).Split('\0',StringSplitOptions.RemoveEmptyEntries);ValidatePaths(loc,staged);
                var tree=(await Git.CheckedAsync(repo,["write-tree"],token,index:temporary).ConfigureAwait(false)).Trim();
                revision=(await Git.CheckedAsync(repo,previous==""?["commit-tree",tree,"-m",message]:["commit-tree",tree,"-p",previous,"-m",message],token,identity:identity).ConfigureAwait(false)).Trim();
                if(body.GetBool("saveIdentity"))foreach(var key in new[]{"name","email"})await Git.CheckedAsync(repo,["config","--local","user."+key,identity.GetString(key)!],token).ConfigureAwait(false);
                var receipt=new JsonObject { ["fingerprint"]=fingerprint,["revision"]=revision,["previous"]=previous };
                var temp=path+"."+Guid.NewGuid().ToString("N");try{using(var file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {var bytes=System.Text.Encoding.UTF8.GetBytes(receipt.ToJsonString());file.Write(bytes);file.Flush(true);}File.Move(temp,path,true);}finally{File.Delete(temp);}
                if(all)File.Move(temporary,Metadata(repo,"index"),true);
            }finally{File.Delete(temporary);File.Delete(temporary+".lock");}
        }
        await Git.CheckedAsync(repo,["update-ref","-m","commit: "+message.Split('\n')[0],"HEAD",revision,previous==""?new string('0',revision.Length):previous],token).ConfigureAwait(false);
        return new JsonObject { ["operation"]="commit",["revision"]=revision };
    }
}
