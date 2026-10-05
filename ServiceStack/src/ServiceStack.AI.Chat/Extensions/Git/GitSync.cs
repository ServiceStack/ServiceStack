using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public sealed partial class GitOperations
{
    async Task<(string Url,string Protocol)> RemoteUrlAsync(string repo,string name,bool push,CancellationToken token)
    {
        var urls=push?await workspace.ValuesAsync(repo,"remote."+name+".pushurl",token).ConfigureAwait(false):[];
        if(urls.Length==0)urls=await workspace.ValuesAsync(repo,"remote."+name+".url",token).ConfigureAwait(false);
        if(urls.Length!=1)throw HttpError.BadRequest("Configure one repository URL for this remote with your Git client.");
        if((await Git.RunAsync(repo,["config","--get-regexp",@"^url\..*\.(insteadof|pushinsteadof)$"],token).ConfigureAwait(false)).Code==0)throw HttpError.BadRequest("Git URL rewriting is unsupported. Sync with your Git client.");
        if(Git.LocalCredentials && Path.IsPathRooted(urls[0]) && Directory.Exists(urls[0]))return (urls[0],"file");
        var source=provisioner.Validate(new JsonObject { ["kind"]="clone",["url"]=urls[0] });
        return (source.GetString("url")!,source.GetString("url")!.StartsWith("https://")?"https":"ssh");
    }
    async Task<bool> TransportAsync(string repo,string action,(string Url,string Protocol) remote,string target,bool missing,CancellationToken token)
    {
        var config=new List<string> { "protocol.allow=never","protocol."+remote.Protocol+".allow=always","credential.interactive=false","fetch.recurseSubmodules=false","submodule.recurse=false","http.followRedirects=false" };
        if(!Git.LocalCredentials) {
            config.AddRange(["credential.helper=","http.extraHeader=","http.cookieFile="]);
            if((await Git.RunAsync(repo,["config","--get-regexp",@"^(http\..*\.(extraheader|cookiefile)|credential\..*\.helper)$"],token).ConfigureAwait(false)).Code==0)throw HttpError.Forbidden("Repository-specific credentials are unavailable in hosted mode.");
        }
        string[] args=action=="push"?["push","--porcelain","--no-verify","--no-follow-tags","--recurse-submodules=no","--receive-pack=git-receive-pack","--",remote.Url,target]:["fetch","--no-tags","--no-recurse-submodules","--upload-pack=git-upload-pack","--",remote.Url,target];
        var result=await Git.RunAsync(repo,args,token,config:config,seconds:120).ConfigureAwait(false);
        if(result.Code==0)return true;
        if(missing && action=="pull" && result.Error.Contains("couldn't find remote ref",StringComparison.OrdinalIgnoreCase))return false;
        throw Conflict("Unable to "+action+". Check the remote, access and connection. Diverged branches require your Git client.");
    }
    async Task<JsonObject> SyncAsync(GitWorkspace.Location loc,JsonObject data,string action,JsonObject body,CancellationToken token)
    {
        var repo=loc.Repository!;
        if(action is "push" or "sync" && !Git.LocalCredentials)throw HttpError.Forbidden("Hosted push requires per-user Git authentication. Local credentials require explicit host configuration.");
        if(!data.GetBool("canSync"))throw HttpError.BadRequest("Create a commit and check out a branch before syncing.");
        if(body.GetString("head")!=data.GetString("head") || body.GetString("branch")!=data.GetString("branch"))throw Conflict();
        var remote=data["remotes"]!.AsArray().OfType<JsonObject>().FirstOrDefault(x=>x.GetString("name")==body.GetString("remote"))??throw HttpError.BadRequest("Select a configured Git remote.");
        var name=remote.GetString("name")!;var branch=remote.GetString("branch");
        if(branch==null || (await Git.RunAsync(repo,["check-ref-format","refs/heads/"+branch],token).ConfigureAwait(false)).Code!=0)throw HttpError.BadRequest("Select a configured Git remote.");
        if(branch!=body.GetString("remoteBranch"))throw Conflict("The remote branch changed. Refresh before syncing.");
        var url=await RemoteUrlAsync(repo,name,action=="push",token).ConfigureAwait(false);
        var push=action=="sync"?await RemoteUrlAsync(repo,name,true,token).ConfigureAwait(false):url;
        var tracking="refs/remotes/"+name+"/"+branch;RejectLinks(Metadata(repo,""),tracking);RejectLinks(Metadata(repo,""),"logs/"+tracking);
        var head=data.GetString("head")!;
        async Task Recheck() {
            if((await Git.CheckedAsync(repo,["rev-parse","HEAD"],token).ConfigureAwait(false)).Trim()!=head || (await Git.CheckedAsync(repo,["symbolic-ref","--short","HEAD"],token).ConfigureAwait(false)).Trim()!=body.GetString("branch") || (await Git.CheckedAsync(repo,["status","--porcelain=v1","--untracked-files=all"],token).ConfigureAwait(false)).Length!=0)throw Conflict("The repository changed during network I/O. Refresh before syncing.");
        }
        if(action=="push") {
            await TransportAsync(repo,"push",url,head+":refs/heads/"+branch,false,token).ConfigureAwait(false);
            await Git.CheckedAsync(repo,["update-ref",tracking,head],token).ConfigureAwait(false);
        } else {
            await Recheck().ConfigureAwait(false);
            var fetched=await TransportAsync(repo,"pull",url,"+refs/heads/"+branch+":"+tracking,action=="sync" && remote["ahead"]==null,token).ConfigureAwait(false);
            var revision=fetched?(await Git.CheckedAsync(repo,["rev-parse","--verify",tracking+"^{commit}"],token).ConfigureAwait(false)).Trim():head;
            var forward=(await Git.RunAsync(repo,["merge-base","--is-ancestor",head,revision],token).ConfigureAwait(false)).Code==0;
            var backward=(await Git.RunAsync(repo,["merge-base","--is-ancestor",revision,head],token).ConfigureAwait(false)).Code==0;
            if(!forward && !backward)throw Conflict("The branches diverged. Your local commits were kept. Resolve with your Git client.");
            await Recheck().ConfigureAwait(false);
            var paths=(await Git.CheckedAsync(repo,["diff","--name-only","-z",head,revision,"--"],token).ConfigureAwait(false)).Split('\0',StringSplitOptions.RemoveEmptyEntries);
            await SafePathsAsync(loc,paths,token,[revision],mergeDrivers:false).ConfigureAwait(false);
            await Git.CheckedAsync(repo,["merge","--ff-only","--no-edit",revision],token).ConfigureAwait(false);
            if(forward)head=revision;
            if(action=="sync") {await Recheck().ConfigureAwait(false);await TransportAsync(repo,"push",push,head+":refs/heads/"+branch,false,token).ConfigureAwait(false);await Git.CheckedAsync(repo,["update-ref",tracking,head],token).ConfigureAwait(false);}
        }
        if((await workspace.ValuesAsync(repo,"branch."+body.GetString("branch")+".remote",token).ConfigureAwait(false)).Length==0) {
            await Git.CheckedAsync(repo,["config","--local","branch."+body.GetString("branch")+".remote",name],token).ConfigureAwait(false);
            await Git.CheckedAsync(repo,["config","--local","branch."+body.GetString("branch")+".merge","refs/heads/"+branch],token).ConfigureAwait(false);
        }
        return new JsonObject { ["operation"]=action,["remote"]=name,["branch"]=branch };
    }
}
