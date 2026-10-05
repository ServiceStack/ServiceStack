using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;

public sealed class GitWorkspace(GitProcess git)
{
    public GitProcess Git { get; } = git;
    public sealed record Location(string[] Roots, string? Path, string? Root, string? Repository)
    {
        public JsonObject Json() => new() { ["roots"]=new JsonArray(Roots.Select(x=>(JsonNode)JsonValue.Create(x)!).ToArray()), ["path"]=Path, ["repository"]=Repository };
    }
    public Location Resolve(IEnumerable<string> roots, string? path)
    {
        var allowed = roots.Select(x=>ProjectsExplorer.PhysicalPath(x)).Distinct().ToArray();
        if (allowed.Length == 0) return new(allowed,null,null,null);
        var selected = ProjectsExplorer.PhysicalPath(string.IsNullOrEmpty(path)?allowed[0]:path);
        var root = allowed.FirstOrDefault(x=>ProjectsExtension.IsWithin(selected,x)) ?? throw HttpError.Forbidden("Path is outside the workspace");
        if (!Directory.Exists(selected)) throw HttpError.NotFound("Directory is unavailable");
        var repo = selected;
        while (!File.Exists(System.IO.Path.Combine(repo,".git")) && !Directory.Exists(System.IO.Path.Combine(repo,".git")) && repo != root) repo = System.IO.Path.GetDirectoryName(repo)!;
        return new(allowed,selected,root,File.Exists(System.IO.Path.Combine(repo,".git")) || Directory.Exists(System.IO.Path.Combine(repo,".git")) ? repo:null);
    }
    public async Task<JsonObject> StateAsync(string repo, CancellationToken token = default)
    {
        var head = await git.RunAsync(repo,["rev-parse","--verify","HEAD"],token).ConfigureAwait(false);
        var index = System.IO.Path.Combine(repo,".git","index");
        var name = await git.RunAsync(repo,["config","--get","user.name"],token).ConfigureAwait(false);
        var email = await git.RunAsync(repo,["config","--get","user.email"],token).ConfigureAwait(false);
        return new JsonObject { ["head"]=head.Code==0?head.Output.Trim():"", ["indexRevision"]=(head.Code==0?head.Output.Trim():"")+":"+GitProcess.Hash(File.Exists(index)?File.ReadAllBytes(index):[]), ["identity"]=new JsonObject { ["name"]=name.Output.Trim(), ["email"]=email.Output.Trim() } };
    }
    public static string WorktreeRevision(string repo, string filename)
    {
        var file = new FileInfo(System.IO.Path.Combine(repo,filename));
        // Opaque review tokens are host-local. Include file bytes as well as metadata, detecting
        // replacements on filesystems with coarse timestamps without platform-specific stat calls.
        if (!file.Exists && file.LinkTarget==null) return GitProcess.Hash("missing");
        var content = file.LinkTarget!=null ? GitProcess.Hash(file.LinkTarget): file.Exists && file.Length <= 10*1024*1024 ? GitProcess.Hash(File.ReadAllBytes(file.FullName)) : "";
        return GitProcess.Hash($"{file.Length}:{file.LastWriteTimeUtc.Ticks}:{file.CreationTimeUtc.Ticks}:{file.Attributes}:{content}");
    }
    public async Task<string> WorkspaceRevisionAsync(string repo,string indexRevision,CancellationToken token = default)
    {
        var status = await git.CheckedAsync(repo,["status","--porcelain=v1","-z","--untracked-files=all"],token).ConfigureAwait(false);
        var text = new StringBuilder(indexRevision+status);
        var records = status.Split('\0');
        for(var i=0;i<records.Length;i++) { var r=records[i]; if(r.Length<4)continue; text.Append(WorktreeRevision(repo,r[3..])); if("RC".Contains(r[0]) || "RC".Contains(r[1])) { if(++i<records.Length) text.Append(WorktreeRevision(repo,records[i])); } }
        return GitProcess.Hash(text.ToString());
    }
    public async Task<JsonObject> BrowseAsync(Location location,CancellationToken token = default)
    {
        var result = location.Json(); result["changes"]=new JsonArray(); result["stagedChanges"]=new JsonArray(); result["commits"]=new JsonArray();
        if(location.Repository is not { } repo)return result;
        var records=(await git.CheckedAsync(repo,["status","--porcelain=v1","-z","--untracked-files=all"],token).ConfigureAwait(false)).Split('\0');
        for(var i=0;i<records.Length;i++) {
            var row=records[i]; if(row.Length<4)continue;
            var filename=row[3..]; string? original=null;
            if("RC".Contains(row[0]) || "RC".Contains(row[1])) original=++i<records.Length?records[i]:null;
            var path=System.IO.Path.Combine(repo,filename);
            if(!ProjectsExplorer.IsContained(path,location.Root!))continue;
            JsonObject Entry(char status,bool worktree) => new() { ["name"]=System.IO.Path.GetFileName(filename),["relativePath"]=filename,["path"]=path,["oldRelativePath"]=worktree && !"RC".Contains(row[1])?null:original,["status"]=status.ToString(),["deleted"]=status=='D' };
            if(row[1]!=' ' || row[0]=='U') { var entry=Entry(row[..2]=="??"?'?':row[1],true); entry["worktreeRevision"]=WorktreeRevision(repo,filename); result["changes"]!.AsArray().Add(entry); }
            if(row[0]!=' ' && row[0]!='?') result["stagedChanges"]!.AsArray().Add(Entry(row[0],false));
        }
        var state=await StateAsync(repo,token).ConfigureAwait(false);
        foreach(var pair in state) result[pair.Key]=pair.Value?.DeepClone();
        result["workspaceRevision"]=await WorkspaceRevisionAsync(repo,state.GetString("indexRevision")!,token).ConfigureAwait(false);
        var branch=await git.RunAsync(repo,["symbolic-ref","--quiet","--short","HEAD"],token).ConfigureAwait(false);
        result["branch"]=branch.Code==0?branch.Output.Trim():"Detached HEAD";
        var normal=Directory.Exists(System.IO.Path.Combine(repo,".git")) && new DirectoryInfo(System.IO.Path.Combine(repo,".git")).LinkTarget==null;
        result["canCommit"]=normal; result["canSync"]=normal && branch.Code==0 && state.GetString("head")!=""; result["canPush"]=git.LocalCredentials;
        var remotes = await RemotesAsync(repo,branch.Code==0?branch.Output.Trim():null,state.GetString("head")!,token).ConfigureAwait(false);
        result["remotes"]=remotes;
        var upstream=branch.Code==0?await ValuesAsync(repo,"branch."+branch.Output.Trim()+".remote",token).ConfigureAwait(false):[];
        result["remote"]=remotes.OfType<JsonObject>().FirstOrDefault(r=>upstream.SequenceEqual([r.GetString("name")!]))?.GetString("name") ?? remotes.OfType<JsonObject>().FirstOrDefault(r=>r.GetString("name")=="origin")?.GetString("name") ?? remotes.OfType<JsonObject>().FirstOrDefault()?.GetString("name");
        var stash=await git.RunAsync(repo,["rev-parse","--verify","refs/stash^{commit}"],token).ConfigureAwait(false); result["stashId"]=stash.Code==0?stash.Output.Trim():null;
        var published=state.GetString("head")!="" ? await git.CheckedAsync(repo,["for-each-ref","--contains",state.GetString("head")!,"--format=%(refname)","refs/remotes"],token).ConfigureAwait(false):"";
        var reason=state.GetString("head")==""?"Create a commit first":branch.Code!=0?"Check out a branch before undoing a commit":published.Length>0?"This commit is on a remote-tracking branch. Use your Git client to revert a shared commit.":"";
        result["canUndo"]=reason.Length==0; result["undoReason"]=reason;
        if(state.GetString("head")!="") {
            var fields=(await git.CheckedAsync(repo,["log","-50","-z","--format=%H%x00%h%x00%aI%x00%an%x00%ae%x00%s%x00%b%x00%D%x00%B"],token).ConfigureAwait(false)).Split('\0');
            string[] names=["id","hash","date","author","email","subject","body","refs","message"];
            for(var i=0;i+8<fields.Length;i+=9) {var commit=new JsonObject(); for(var j=0;j<9;j++)commit[names[j]]=fields[i+j]; result["commits"]!.AsArray().Add(commit);}
        }
        return result;
    }
    public async Task<string[]> ValuesAsync(string repo,string key,CancellationToken token=default) {
        var result=await git.RunAsync(repo,["config","--get-all",key],token).ConfigureAwait(false);
        return result.Code==0?result.Output.TrimEnd('\n','\r').Split('\n').Select(x=>x.TrimEnd('\r')).ToArray():[];
    }
    async Task<JsonArray> RemotesAsync(string repo,string? branch,string head,CancellationToken token)
    {
        var remote=branch!=null?await ValuesAsync(repo,"branch."+branch+".remote",token).ConfigureAwait(false):[];
        var merge=branch!=null?await ValuesAsync(repo,"branch."+branch+".merge",token).ConfigureAwait(false):[];
        var result=new JsonArray();
        foreach(var name in (await git.CheckedAsync(repo,["remote"],token).ConfigureAwait(false)).Split('\n',StringSplitOptions.RemoveEmptyEntries)) {
            if(!Regex.IsMatch(name,@"^[\w.-]+$") || name.StartsWith('-'))continue;
            var target=remote.SequenceEqual([name]) && merge.Length==1 && merge[0].StartsWith("refs/heads/")?merge[0][11..]:branch;
            int? ahead=null,behind=null;
            if(head!="" && target!=null && (await git.RunAsync(repo,["check-ref-format","refs/heads/"+target],token).ConfigureAwait(false)).Code==0) {
                var counts=await git.RunAsync(repo,["rev-list","--left-right","--count","HEAD...refs/remotes/"+name+"/"+target,"--"],token).ConfigureAwait(false);
                var parts=counts.Output.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries); if(counts.Code==0 && parts.Length==2) {ahead=int.Parse(parts[0]);behind=int.Parse(parts[1]);}
            }
            var urls=await ValuesAsync(repo,"remote."+name+".url",token).ConfigureAwait(false);
            result.Add(new JsonObject { ["name"]=name,["branch"]=target,["ahead"]=ahead,["behind"]=behind,["githubUrl"]=urls.Length==1?GithubUrl(urls[0]):null });
        }
        return result;
    }
    static string? GithubUrl(string url) {
        var scp=Regex.Match(url,@"^git@github\.com:([^\s]+)$"); string path;
        if(scp.Success)path=scp.Groups[1].Value;
        else {if(!Uri.TryCreate(url,UriKind.Absolute,out var uri) || uri.Host!="github.com" || uri.Scheme is not ("https" or "ssh") || uri.UserInfo is not ("" or "git") || uri.Query!="" || uri.Fragment!="" || uri.Port is not (-1 or 22 or 443))return null;path=uri.AbsolutePath.Trim('/');}
        if(path.EndsWith(".git"))path=path[..^4];
        return Regex.IsMatch(path,@"^[\w.-]+/[\w.-]+$") && !path.Split('/').Any(x=>x is "." or "..")?"https://github.com/"+path:null;
    }
    public async Task<JsonObject> CommitAsync(Location loc,string? commit,CancellationToken token=default)
    {
        var repo=loc.Repository ?? throw HttpError.BadRequest("This directory has no Git repository");
        if(commit==null || !Regex.IsMatch(commit,"^[0-9a-fA-F]{7,64}$"))throw HttpError.BadRequest("Select a valid commit hash");
        var resolved=await git.RunAsync(repo,["rev-parse","--verify",commit+"^{commit}"],token).ConfigureAwait(false); if(resolved.Code!=0)throw HttpError.NotFound("Commit is unavailable");
        var id=resolved.Output.Trim();
        var parents=(await git.CheckedAsync(repo,["rev-list","--parents","-n","1",id],token).ConfigureAwait(false)).Trim().Split(' ').Skip(1).ToArray();
        var comparison=parents.Length>0?new[]{"diff",parents[0],id}:new[]{"diff-tree","--root","--no-commit-id","-r",id};
        var records=(await git.CheckedAsync(repo,comparison.Concat(["--name-status","-z","-M","--"]),token).ConfigureAwait(false)).Split('\0');
        var changes=new JsonArray();
        for(var i=0;i+1<records.Length;i++) {var status=records[i];if(status.Length==0)continue;var old=records[++i];var name="RC".Contains(status[0]) && i+1<records.Length?records[++i]:old;var path=System.IO.Path.Combine(repo,name);if(!ProjectsExplorer.IsContained(path,loc.Root!) || !ProjectsExplorer.IsContained(System.IO.Path.Combine(repo,old),loc.Root!))continue; changes.Add(new JsonObject { ["name"]=System.IO.Path.GetFileName(name),["relativePath"]=name,["path"]=path,["oldRelativePath"]="RC".Contains(status[0])?old:null,["status"]=status[..1],["deleted"]=status[0]=='D' });}
        var result=loc.Json();result["commit"]=id;result["parent"]=parents.FirstOrDefault();result["changes"]=changes;return result;
    }
    public async Task<JsonObject> DiffAsync(Location loc,string? file,bool staged,string? commit=null,CancellationToken token=default)
    {
        if(string.IsNullOrEmpty(file))throw HttpError.BadRequest("Select a file to compare");
        var repo=loc.Repository ?? throw HttpError.BadRequest("This directory has no Git repository");
        var path=System.IO.Path.GetFullPath(file);
        if(!ProjectsExtension.IsWithin(ProjectsExplorer.PhysicalPath(path),loc.Root!) || !ProjectsExtension.IsWithin(path,repo))throw HttpError.Forbidden("File is outside the repository workspace");
        var relative=System.IO.Path.GetRelativePath(repo,path).Replace('\\','/');
        var result=loc.Json(); result["patch"]="";result["file"]=new JsonObject { ["path"]=path,["name"]=System.IO.Path.GetFileName(path),["relativePath"]=relative };
        JsonObject Message(string message) {result["message"]=message;return result;}
        List<string> args;
        if(commit!=null) {
            result=await CommitAsync(loc,commit,token).ConfigureAwait(false);
            var change=result["changes"]!.AsArray().OfType<JsonObject>().FirstOrDefault(x=>x.GetString("path")==path) ?? throw HttpError.NotFound("File is not part of this commit or is outside the workspace");
            var paths=new[]{relative,change.GetString("oldRelativePath")??relative}.Distinct().ToArray();
            result["file"]=change.DeepClone();result.Remove("changes");result["patch"]="";
            foreach(var rev in new[]{result.GetString("parent"),result.GetString("commit")}.Where(x=>x!=null))if(await LargeObjectsAsync(repo,["ls-tree","-l","-z",rev!,"--",..paths],token).ConfigureAwait(false))return Message("File is too large to compare (1 MiB limit).");
            args=result.GetString("parent") is { } parent?["diff",parent,result.GetString("commit")!]:["diff-tree","--root","--no-commit-id","-r",result.GetString("commit")!]; args.AddRange(["-p","-M","--no-ext-diff","--no-textconv","--no-color","--unified=3","--",..paths]);
        } else {
            var index=await git.CheckedAsync(repo,["ls-files","--stage","-z","--",relative],token).ConfigureAwait(false);
            foreach(var row in index.Split('\0').Where(x=>x.Contains('\t'))) {var parts=row.Split('\t')[0].Split(' ');if(parts[0]=="160000")continue;var size=await git.CheckedAsync(repo,["cat-file","-s",parts[1]],token).ConfigureAwait(false);if(long.Parse(size.Trim())>1024*1024)return Message("File is too large to compare (1 MiB limit).");}
            if(staged && (await StateAsync(repo,token).ConfigureAwait(false)).GetString("head")!="" && await LargeObjectsAsync(repo,["ls-tree","-l","-z","HEAD","--",relative],token).ConfigureAwait(false))return Message("File is too large to compare (1 MiB limit).");
            if(!staged && File.Exists(path)) {var info=new FileInfo(path);if(info.Length>1024*1024)return Message("File is too large to compare (1 MiB limit)."); if(info.LinkTarget==null && File.ReadAllBytes(path).Contains((byte)0))return Message("Binary file differences cannot be displayed.");}
            if(!staged && index.Length==0) {
                if(!File.Exists(path))throw HttpError.NotFound("File is unavailable");
                var content=File.ReadAllText(path); if(content.Length==0)return Message("New empty file. There are no lines to display.");
                var lines=content.Split('\n');var count=lines.Length-(content.EndsWith('\n')?1:0);
                var patch="--- /dev/null\n+++ b/"+relative+"\n@@ -0,0 +1"+(count==1?"":","+count)+" @@\n"+string.Join("",lines.Take(count).Select(x=>"+"+x+"\n"))+(content.EndsWith('\n')?"":"\\ No newline at end of file\n");
                if(Encoding.UTF8.GetByteCount(patch)>1024*1024 || count>10000)return Message("Diff is too large to preview. Open the file to inspect its contents.");result["patch"]=patch;return result;
            }
            args=["diff"];if(staged)args.Add("--cached");args.AddRange(["--no-ext-diff","--no-textconv","--no-color","--unified=3","--submodule=short","--",relative]);
        }
        var output=await git.CheckedAsync(repo,args,token,maxBytes:2*1024*1024).ConfigureAwait(false);
        if(output.Contains("\nBinary files ") || output.StartsWith("Binary files "))return Message("Binary file differences cannot be displayed.");
        if(output.Split('\n').Any(x=>x.StartsWith("@@@")))return Message("This file has merge conflicts. Resolve them before comparing unstaged changes.");
        if(Encoding.UTF8.GetByteCount(output)>1024*1024 || output.Count(x=>x=='\n')>10000)return Message("Diff is too large to preview.");
        result["patch"]=output;if(output.Length==0)return Message(staged?"No staged differences.":"No unstaged differences. The file may have changed since the list was refreshed.");return result;
    }
    async Task<bool> LargeObjectsAsync(string repo,string[] args,CancellationToken token) => (await git.CheckedAsync(repo,args,token).ConfigureAwait(false)).Split('\0').Where(x=>x.Contains('\t')).Any(x=> {var fields=x.Split('\t')[0].Split(' ',StringSplitOptions.RemoveEmptyEntries);return fields.Length==4 && fields[1]=="blob" && long.TryParse(fields[3],out var size) && size>1024*1024;});
}
