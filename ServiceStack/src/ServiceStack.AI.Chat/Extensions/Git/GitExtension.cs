using System.Diagnostics;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>Optional authenticated Git workspace. Hosted defaults never reuse operator credentials.</summary>
public sealed class GitExtension() : ChatExtension("git")
{
    public string Executable { get; set; } = "git";
    public bool UseLocalCredentials { get; set; }
    public List<string>? CloneHosts { get; set; }
    public GitWorkspace Workspace { get; private set; } = null!;
    public GitOperations Operations { get; private set; } = null!;
    public override void Install(ExtensionContext ctx)
    {
        if(ctx.Feature.ProjectsApi is NullProjectsApi || !Available()) {ctx.Disabled=true;Disabled=true;return;}
        var process=new GitProcess(Executable,UseLocalCredentials);
        var hosts=CloneHosts ?? ctx.Feature.Config.GetArray("git_clone_hosts")?.OfType<JsonValue>().Select(x=>x.GetValue<string>()).ToList();
        var provisioner=new GitProvisioner(process,hosts);ctx.Feature.GitProvisioner=provisioner;
        Workspace=new GitWorkspace(process);Operations=new GitOperations(ctx.Feature,Workspace,provisioner);var messages=new GitCommitMessages(ctx.Feature,Workspace,Operations);
        ctx.AddGet("capabilities",_=>Task.FromResult<object?>(new JsonObject { ["initializeGit"]=true,["clone"]=true,["commit"]=true,["sync"]=true,["githubPublish"]=false }));
        ctx.AddGet("workspace",async req=>{var result=await Workspace.BrowseAsync(Resolve(req,req.QueryString("projectId"),req.QueryString("path")),req.Request.RequestAborted).ConfigureAwait(false);result["canGenerateCommit"]=messages.Template()!=null;return result;});
        ctx.AddGet("diff",async req=>await Workspace.DiffAsync(Resolve(req,req.QueryString("projectId"),req.QueryString("path")),req.QueryString("file"),req.QueryString("staged")=="1",req.QueryString("commit"),req.Request.RequestAborted).ConfigureAwait(false));
        ctx.AddGet("commit",async req=>await Workspace.CommitAsync(Resolve(req,req.QueryString("projectId"),req.QueryString("path")),req.QueryString("commit"),req.Request.RequestAborted).ConfigureAwait(false));
        // Literal routes avoid assumptions about regex parameter matching, and reserve message first.
        ctx.AddPost("repositories/message",async req=>{var body=await req.GetJsonBodyAsync().ConfigureAwait(false);return await messages.GenerateAsync(Resolve(req,body.GetString("projectId"),body.GetString("path")),body,req.AssertUserName(),req.Request.RequestAborted).ConfigureAwait(false);});
        foreach(var action in new[]{"stage","unstage","commit","commit-all","discard","stage-all","unstage-all","discard-all","undo","stash","stash-untracked","stash-staged","stash-apply","stash-pop","push","pull","sync"}) {
            var selected=action;
            ctx.AddPost("repositories/"+selected,async req=>{var body=await req.GetJsonBodyAsync().ConfigureAwait(false);return await Operations.MutateAsync(Resolve(req,body.GetString("projectId"),body.GetString("path")),selected,body,req.AssertUserName(),body.GetString("projectId"),req.Request.RequestAborted).ConfigureAwait(false);});
        }
    }
    GitWorkspace.Location Resolve(ChatRequestContext req,string? projectId,string? path)
    {
        req.AssertUserName();
        JsonObject scope;try{scope=Feature.ProjectsApi.ResolveExplorerWorkspace(string.IsNullOrEmpty(projectId)?null:projectId,req.UserName,Ctx.IsAdmin(req.Request));}catch(ArgumentException){throw HttpError.BadRequest("Project is unavailable");}
        return Workspace.Resolve(scope.GetArray("directories")?.OfType<JsonValue>().Select(x=>x.GetValue<string>())??[],path);
    }
    bool Available() {try {using var process=Process.Start(new ProcessStartInfo(Executable,"--version") { UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true });if(process==null)return false;if(!process.WaitForExit(3000)){process.Kill(true);return false;}return process.ExitCode==0;}catch(System.ComponentModel.Win32Exception){return false;}}
}
