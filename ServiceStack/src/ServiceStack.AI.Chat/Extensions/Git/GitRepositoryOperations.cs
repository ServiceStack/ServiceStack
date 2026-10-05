using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public sealed partial class GitOperations
{
    async Task<JsonObject> StashAsync(GitWorkspace.Location loc,JsonObject data,string action,JsonObject body,CancellationToken token)
    {
        var repo=loc.Repository!;var head=data.GetString("head")!;if(head=="")throw HttpError.BadRequest("Create a commit before stashing");
        var environment=new Dictionary<string,string> { ["GIT_LITERAL_PATHSPECS"]="0" };
        if(action is "stash-apply" or "stash-pop") {
            var latest=data.GetString("stashId")??throw HttpError.BadRequest("There is no stash to apply");
            if(body.GetString("stashId")!=latest)throw Conflict("The latest stash changed. Refresh before applying.");
            if(data["stagedChanges"]!.AsArray().Count>0 || data["changes"]!.AsArray().OfType<JsonObject>().Any(x=>x.GetString("status")!="?"))throw Conflict("Commit or stash your current changes before applying a stash.");
            var basis=(await Git.CheckedAsync(repo,["rev-parse",latest+"^1"],token).ConfigureAwait(false)).Trim();
            var paths=new List<string>();
            foreach(var rev in new[]{latest,latest+"^2"})paths.AddRange((await Git.CheckedAsync(repo,["diff","--name-only","--no-renames","-z",basis,rev,"--"],token).ConfigureAwait(false)).Split('\0',StringSplitOptions.RemoveEmptyEntries));
            var revisions=new List<string>{head,latest,latest+"^2"};
            var extra=await Git.RunAsync(repo,["rev-parse","--verify",latest+"^3"],token).ConfigureAwait(false);
            if(extra.Code==0) {revisions.Add(extra.Output.Trim());paths.AddRange((await Git.CheckedAsync(repo,["ls-tree","-r","--name-only","-z",extra.Output.Trim(),"--"],token).ConfigureAwait(false)).Split('\0',StringSplitOptions.RemoveEmptyEntries));}
            await SafePathsAsync(loc,paths,token,revisions).ConfigureAwait(false);
            var applied=await Git.RunAsync(repo,["stash","apply",latest],token,environment:environment).ConfigureAwait(false);
            if(applied.Code!=0)throw Conflict("The stash could not be fully applied. It was kept. Refresh and resolve conflicts with your Git client.");
            if(action=="stash-pop") {
                var current=await Git.RunAsync(repo,["rev-parse","--verify","refs/stash^{commit}"],token).ConfigureAwait(false);
                if(current.Output.Trim()!=latest)throw Conflict("The stash was applied but the stash list changed. The stash was kept.");
                await Git.CheckedAsync(repo,["stash","drop","stash@{0}"],token).ConfigureAwait(false);
            }
        } else {
            if(action is not ("stash" or "stash-untracked" or "stash-staged"))throw HttpError.BadRequest("Unknown stash operation");
            var changes=data["changes"]!.AsArray().OfType<JsonObject>().ToArray();var staged=data["stagedChanges"]!.AsArray().OfType<JsonObject>().ToArray();
            if(action=="stash-staged" && staged.Length==0 || staged.Length==0 && !changes.Any(x=>x.GetString("status")!="?" || action=="stash-untracked"))throw HttpError.BadRequest("There are no changes for this stash operation");
            await SafePathsAsync(loc,changes.Concat(staged).Select(x=>x.GetString("relativePath")!),token,[head]).ConfigureAwait(false);
            var args=new List<string>{"stash","push"};if(action=="stash-untracked")args.Add("--include-untracked");if(action=="stash-staged")args.Add("--staged");
            var result=await Git.RunAsync(repo,args,token,identity:Identity(body,data),environment:environment).ConfigureAwait(false);
            if(result.Code!=0)throw Conflict("Git could not finish stashing. Review saved stashes with your Git client. Stash Staged requires Git 2.35 or newer.");
        }
        return new JsonObject { ["operation"]=action };
    }
}
