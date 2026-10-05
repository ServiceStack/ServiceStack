#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.AiTests;

public class AiChatMigrationJevSharingTests
{
    static JsonObject Fixture=>JsonNode.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"AiChatFixtures","jev-sharing-contract.json")))!.AsArray().First(x=>x!["valid"]!.GetValue<bool>())!.AsObject().Clone();
    static JsonObject Unwrap(object? value)=>value is ChatResult result?JsonNode.Parse(result.Text!)!.AsObject():(JsonObject)value!;
    static string Hash(JsonObject payload)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToJsonString(ChatJson.Options)))).ToLowerInvariant();
    sealed class Remote
    {
        public readonly Dictionary<string,JsonObject> Rows=[];
        public readonly List<(string Method,string Path,string? Key,JsonNode? Body)> Calls=[];
        readonly Dictionary<string,HashSet<string>> stars=[];
        readonly Dictionary<string,string> owners=[];
        public bool LoseCreate,LoseUpdate;
        /// <summary>The publisher owns shares by account; keys of the same account map to it (default: key itself).</summary>
        public readonly Dictionary<string,string> Accounts=[];
        string Account(string key)=>Accounts.GetValueOrDefault(key,key);
        public Action? BeforeRequest;
        static HttpResponseMessage Reply(JsonNode node,int status=200)=>new((HttpStatusCode)status){Content=new StringContent(node.ToJsonString(),Encoding.UTF8,"application/json")};
        public async Task<HttpResponseMessage> Send(HttpRequestMessage request,CancellationToken token)
        {
            var body=request.Content!=null?JsonNode.Parse(await request.Content.ReadAsStringAsync(token)):null;var method=request.Method.Method;var path=request.RequestUri!.PathAndQuery;var key=request.Headers.Authorization?.Parameter;Calls.Add((method,path,key,body?.DeepClone()));var callback=BeforeRequest;BeforeRequest=null;callback?.Invoke();
            var endpoint=request.RequestUri.AbsolutePath;var reference=endpoint.Split('/').Last().Replace(".json","");var authenticated=key!=null;
            if(endpoint=="/publish/decisions/tags")return Reply(new JsonObject {["version"]=1,["tags"]=new JsonArray(new JsonObject {["label"]="Verification",["group"]="tag"})});
            if(endpoint=="/publish/decisions")return Reply(new JsonObject {["items"]=new JsonArray(Rows.Values.Select(r=>{var result=r.Clone();result.Remove("document");result.Remove("execution");result.Remove("publisherRunCount");result["starred"]=key!=null&&stars.GetValueOrDefault(r.GetString("externalRef")!)?.Contains(key)==true;return (JsonNode)result;}).ToArray()),["take"]=50,["skip"]=0,["hasMore"]=false});
            if(endpoint.StartsWith("/d/"))return Rows.TryGetValue(reference,out var download)?Reply(download["document"]!.DeepClone()):Reply(new JsonObject(),404);
            if(endpoint.EndsWith("/star")){reference=endpoint.Split('/')[3];if(!authenticated)return Reply(new JsonObject(),401);var group=stars.GetValueOrDefault(reference)??new HashSet<string>();stars[reference]=group;if(body!.AsObject().GetBool("starred"))group.Add(key!);else group.Remove(key!);return Reply(new JsonObject {["externalRef"]=reference,["starCount"]=group.Count,["starred"]=group.Contains(key!)});}
            if(method=="POST"){
                if(!authenticated)return Reply(new JsonObject(),401);var payload=body!.AsObject().Clone();var idempotency=payload.GetString("idempotencyKey")!;payload.Remove("idempotencyKey");var existing=Rows.Values.FirstOrDefault(r=>r.GetString("key")==key+":"+idempotency);
                if(existing==null){reference="ref"+(Rows.Count+1);existing=payload.Clone();existing["externalRef"]=reference;existing["revision"]=1;existing["contentHash"]=Hash(payload);existing["publishedUrl"]="https://publisher.example/d/"+reference;existing["author"]=new JsonObject {["userName"]="Author"};existing["key"]=key+":"+idempotency;Rows[reference]=existing;owners[reference]=Account(key!);}
                if(LoseCreate){LoseCreate=false;throw new HttpRequestException("Controlled response loss");}var result=existing.Clone();result.Remove("key");result.Remove("publisherRunCount");return Reply(result);
            }
            if(!Rows.TryGetValue(reference,out var row))return Reply(new JsonObject(),404);
            if(method=="GET"){var result=row.Clone();result.Remove("publisherRunCount");result.Remove("key");return Reply(result);}
            if(!authenticated)return Reply(new JsonObject(),401);if(owners[reference]!=Account(key))return Reply(new JsonObject(),403);
            var query=System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);var revision=method=="DELETE"?int.Parse(query["revision"]!):body!["revision"]!.GetValue<int>();if(row["revision"]!.GetValue<int>()!=revision)return Reply(new JsonObject(),409);
            if(method=="DELETE"){Rows.Remove(reference);return Reply(new JsonObject());}
            var update=body!.AsObject().Clone();update.Remove("revision");if(row.GetString("contentHash")!=Hash(update)){foreach(var pair in update)row[pair.Key]=pair.Value?.DeepClone();row["revision"]=revision+1;row["contentHash"]=Hash(update);}
            if(LoseUpdate){LoseUpdate=false;throw new HttpRequestException("Controlled update response loss");}return Reply(row.Clone());
        }
    }
    static PublishExtension InstallPublisher(AiChatMigrationTestHost host,Remote remote)
    {
        var publisher=host.Install(new PublishExtension {Enabled=true,HttpHandlerFactory=()=>new AiChatMigrationHttpHandler(remote.Send)});
        var config=new PublisherConfiguration(publisher.Ctx);foreach(var user in new[]{"alice","bob"})config.Save(user,new JsonObject {["baseUrl"]="https://publisher.example",["apiKey"]=user+"-key",["userId"]=user+"-publisher",["userName"]=user});return publisher;
    }
    static (JevStore Store,JsonObject Recipe,string Run) Local(AiChatMigrationTestHost host,string user="alice")
    {
        var store=new JevStore(host.Feature.AppData.GetUserPath(user));var fixture=Fixture;var doc=DecisionRecipeValidator.Validate(fixture["document"]);var saved=store.Save(doc,filename:"claim.json");var execution=fixture["execution"]!.AsObject();var input=execution["input"]!;var request=DecisionRecipeValidator.Compile(doc,input);var run=store.Submit("fixture",doc,input,request,"owner","claim",1).Run;var id=run.GetString("id")!;store.Start(id,"owner");store.Finish(id,"succeeded",new JsonObject {["model"]=execution["model"]!.DeepClone()},execution["answers"]!.AsObject(),owner:"owner");return(store,saved,id);
    }
    static JsonObject PublishBody(JsonObject recipe,string run,long? published=null)=>new(){["revision"]=recipe["revision"]!.DeepClone(),["runId"]=run,["publishedRevision"]=published};
    [TestCase(""),TestCase("/chat")]
    public async Task Complete_route_surface_keeps_recipe_revision_errors_owned_history_and_literal_preview_contract(string prefix)
    {
        using var host=new AiChatMigrationTestHost(prefix);var remote=new Remote();InstallPublisher(host,remote);var extension=host.Install(new JevExtension());var fixture=Fixture;var create=await host.SendAsync("POST",prefix+"/ext/jev/recipes","alice",new JsonObject {["document"]=fixture["document"]!.DeepClone(),["filename"]="Café 😀.json"});Assert.That(((ChatResult)create!).Status,Is.EqualTo(201));var row=Unwrap(create);
        Assert.That(row.GetString("id"),Is.EqualTo("Café 😀"));var bob=Unwrap(await host.SendAsync("GET",prefix+"/ext/jev/recipes","bob"));Assert.That(bob["items"]!.AsArray().Any(x=>x!["id"]!.GetValue<string>()=="Café 😀"),Is.False);
        var conflict=(ChatResult)(await host.SendAsync("PUT",prefix+"/ext/jev/recipes/Caf%C3%A9%20%F0%9F%98%80","alice",new JsonObject {["document"]=fixture["document"]!.DeepClone(),["revision"]=999}))!;Assert.That(conflict.Status,Is.EqualTo(409));
        var invalid=(ChatResult)(await host.SendAsync("POST",prefix+"/ext/jev/validate","alice",new JsonObject {["recipe"]=new JsonObject()}))!;Assert.That(invalid.Status,Is.EqualTo(400));Assert.That(Unwrap(invalid)["responseStatus"]!["errors"]![0]!["fieldName"]!.GetValue<string>(),Is.EqualTo("schemaVersion"));
        foreach(var (method,path) in new[]{("GET","status"),("GET","runs"),("DELETE","history"),("GET","tags"),("GET","shared-recipes")})Assert.DoesNotThrowAsync(async()=>await host.SendAsync(method,prefix+"/ext/jev/"+path,"alice"));
        Assert.That(Unwrap(await host.SendAsync("GET",prefix+"/ext/jev/status","alice")).GetBool("available"),Is.False);
        var preview=(ChatResult)(await host.SendAsync("POST",prefix+"/ext/jev/recipes/preview-url","alice",new JsonObject {["url"]="file:///etc/passwd"}))!;Assert.That(preview.Status,Is.EqualTo(400));Assert.That(Unwrap(preview)["responseStatus"]!["errors"]![0]!["fieldName"]!.GetValue<string>(),Is.EqualTo("url"));
        Assert.That(JevAssets.Read("ext/jev/prompts/create-recipe.md"),Does.Contain("recipe"));Assert.That(JevAssets.Read("ext/jev/recipes/sentiment.json"),Is.Not.Null);
    }
    [Test]
    public async Task Lost_creation_response_keeps_exact_pending_snapshot_after_edits_and_binds_only_original_account()
    {
        using var host=new AiChatMigrationTestHost();var remote=new Remote {LoseCreate=true};var publisher=InstallPublisher(host,remote);var sharing=new DecisionSharing(host.Feature,new DecisionImporter(host.Feature));var (store,saved,run)=Local(host);var before=store.Run(run)!.ToJsonString();
        Assert.ThrowsAsync<HttpError>(async()=>await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run),CancellationToken.None));var index=JevStore.ReadJson(Path.Combine(store.Root,"index.json"))!.AsObject();var pending=index["recipes"]!["claim"]!["pendingPublication"]!.AsObject().Clone();var path=Path.Combine(store.Root,pending.GetString("pendingPayloadRef")!);var original=File.ReadAllText(path);
        var changed=saved["document"]!.AsObject().Clone();changed["description"]="Later local edit";store.Save(changed,"claim",1);
        var config=new PublisherConfiguration(publisher.Ctx);config.Save("alice",new JsonObject {["apiKey"]="other-key",["userId"]="other-account"});Assert.ThrowsAsync<JevValidationException>(async()=>await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run),CancellationToken.None));Assert.That(File.ReadAllText(path),Is.EqualTo(original));
        config.Save("alice",new JsonObject {["apiKey"]="alice-key",["userId"]="alice-publisher"});var receipt=await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run),CancellationToken.None);Assert.That(remote.Rows,Has.Count.EqualTo(1));Assert.That(store.Recipe("claim")!["document"]!["description"]!.GetValue<string>(),Is.EqualTo("Later local edit"));Assert.That(remote.Rows.Values.Single()["document"]!["description"]!.GetValue<string>(),Is.Not.EqualTo("Later local edit"));Assert.That(store.Run(run)!.ToJsonString(),Is.EqualTo(before));Assert.That(File.Exists(path),Is.False);Assert.That(receipt.GetString("externalRef"),Is.EqualTo("ref1"));
        var status=await sharing.StatusAsync("alice",store,"claim",CancellationToken.None);Assert.That(status.GetBool("savedChangesNotShared"),Is.True);Assert.That(status["pendingPublication"],Is.Null);Assert.That(status["publication"]!["publisherRunCount"]!.GetValue<int>(),Is.EqualTo(1));Assert.That(status["account"]!.AsObject().GetString("apiKey"),Is.EqualTo(PublisherConfiguration.Mask("alice-key")));
    }
    [Test]
    public async Task Rotating_the_publisher_api_key_keeps_updating_and_revoking_the_same_share()
    {
        // llms-py parity: a share belongs to the publisher origin and account, not to one API key
        using var host=new AiChatMigrationTestHost();var remote=new Remote();var publisher=InstallPublisher(host,remote);var sharing=new DecisionSharing(host.Feature,new DecisionImporter(host.Feature));var(store,saved,run)=Local(host);
        await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run),CancellationToken.None);
        remote.Accounts["alice-rotated-key"]="alice-key";
        new PublisherConfiguration(publisher.Ctx).Save("alice",new JsonObject {["apiKey"]="alice-rotated-key",["userId"]="alice-publisher"});
        store.Favourite("claim",true); // changes shared usage, so the next publish is a real update
        var status=await sharing.StatusAsync("alice",store,"claim",CancellationToken.None);Assert.That(status["publication"]!["externalRef"]!.GetValue<string>(),Is.EqualTo("ref1"));
        var updated=await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run,1),CancellationToken.None);
        Assert.That(updated.GetString("externalRef"),Is.EqualTo("ref1"));Assert.That(remote.Rows,Has.Count.EqualTo(1));Assert.That(remote.Calls.Count(c=>c.Method=="PUT"),Is.EqualTo(1));
        Assert.That((await sharing.UnpublishAsync("alice",store,"claim",new JsonObject {["publishedRevision"]=2},CancellationToken.None)).GetBool("unpublished"),Is.True);
    }

    [Test]
    public async Task Lost_update_response_reconciles_hidden_usage_hash_and_conflicts_keep_review_evidence()
    {
        using var host=new AiChatMigrationTestHost();var remote=new Remote();InstallPublisher(host,remote);var sharing=new DecisionSharing(host.Feature,new DecisionImporter(host.Feature));var(store,saved,run)=Local(host);await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run),CancellationToken.None);
        store.Favourite("claim",true);remote.LoseUpdate=true;Assert.ThrowsAsync<HttpError>(async()=>await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run,1),CancellationToken.None));var putCount=remote.Calls.Count(c=>c.Method=="PUT");var recovered=await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run,1),CancellationToken.None);Assert.That(recovered["revision"]!.GetValue<int>(),Is.EqualTo(2));Assert.That(remote.Calls.Count(c=>c.Method=="PUT"),Is.EqualTo(putCount));
        store.Favourite("claim",false);remote.Rows["ref1"]["revision"]=3;remote.Rows["ref1"]["contentHash"]="externally-changed";Assert.ThrowsAsync<JevConflictException>(async()=>await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run,2),CancellationToken.None));Assert.That(Directory.GetFiles(Path.Combine(store.Root,"publication-conflicts")),Has.Length.EqualTo(1));Assert.That(store.Recipe("claim")!["publication"]!.AsObject().GetBool("remoteChanged"),Is.True);
    }
    [Test]
    public async Task Publication_includes_all_examples_and_imports_pin_revision_keep_attribution_and_clear_replaced_history()
    {
        using var host=new AiChatMigrationTestHost();var remote=new Remote();InstallPublisher(host,remote);var sharing=new DecisionSharing(host.Feature,new DecisionImporter(host.Feature));var(store,saved,run)=Local(host);var receipt=await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run),CancellationToken.None);Assert.That(JsonNode.DeepEquals(remote.Rows["ref1"]["document"]!["examples"],saved["document"]!["examples"]),Is.True);
        var body=new JsonObject {["externalRef"]=receipt["externalRef"]!.DeepClone(),["publishedRevision"]=1,["contentHash"]=receipt["contentHash"]!.DeepClone(),["filename"]="claim.json"};
        Assert.ThrowsAsync<JevRecipeExistsException>(async()=>await sharing.ImportAsync("alice",store,body,CancellationToken.None));body["contentHash"]="stale";Assert.ThrowsAsync<JevConflictException>(async()=>await sharing.ImportAsync("alice",store,body,CancellationToken.None));body["contentHash"]=receipt["contentHash"]!.DeepClone();body["replaceRevision"]=1;
        var imported=await sharing.ImportAsync("alice",store,body,CancellationToken.None);Assert.That(imported["publication"],Is.Null);Assert.That(imported["sourceExample"],Is.Not.Null);Assert.That(store.Run(run),Is.Null);Assert.That(store.Recipe("claim")!["sourceDocument"],Is.Not.Null);Assert.That(store.History("claim")["items"]!.AsArray(),Is.Empty);
        var status=await sharing.StatusAsync("alice",store,"claim",CancellationToken.None);Assert.That(status["usage"]!["publisherRunCount"]!.GetValue<int>(),Is.Zero);Assert.That(status["runs"]!.AsArray(),Is.Empty);
    }
    [Test]
    public async Task Stars_are_remote_personalized_catalog_is_bounded_and_anonymous_tags_have_no_credentials()
    {
        using var host=new AiChatMigrationTestHost();var remote=new Remote();var publisher=InstallPublisher(host,remote);var sharing=new DecisionSharing(host.Feature,new DecisionImporter(host.Feature));var(store,saved,run)=Local(host);await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run),CancellationToken.None);
        await sharing.StarAsync("bob","ref1",new JsonObject {["starred"]=true},CancellationToken.None);Assert.That(store.Favourites(),Is.Empty);
        var catalog=(JsonObject)(await sharing.CatalogAsync("bob",new[]{new KeyValuePair<string,string?>("take","999"),new("skip","99999"),new("secret","ignored")},CancellationToken.None))!;
        Assert.That(catalog["items"]![0]!.AsObject().GetBool("starred"),Is.True);var call=remote.Calls.Last();Assert.That(call.Path,Does.Contain("take=50"));Assert.That(call.Path,Does.Contain("skip=10000"));Assert.That(call.Path,Does.Not.Contain("secret"));Assert.That(call.Key,Is.EqualTo("bob-key"));Assert.That(catalog["items"]![0]!.AsObject().ContainsKey("publisherRunCount"),Is.False);
        await sharing.TagsAsync("alice",CancellationToken.None);Assert.That(remote.Calls.Last().Key,Is.Null);await sharing.StarAsync("bob","ref1",new JsonObject {["starred"]=false},CancellationToken.None);Assert.ThrowsAsync<JevValidationException>(async()=>await sharing.StarAsync("bob","ref1",new JsonObject {["starred"]="yes"},CancellationToken.None));
        new PublisherConfiguration(publisher.Ctx).Disconnect("bob");await sharing.CatalogAsync("bob",[],CancellationToken.None);Assert.That(remote.Calls.Last().Key,Is.Null);publisher.Disabled=true;Assert.ThrowsAsync<HttpError>(async()=>await sharing.TagsAsync("alice",CancellationToken.None));
    }
    [Test]
    public async Task Concurrent_local_deletion_or_account_switch_does_not_restore_recipe_or_attribute_other_grants()
    {
        using var host=new AiChatMigrationTestHost();var remote=new Remote();var publisher=InstallPublisher(host,remote);var sharing=new DecisionSharing(host.Feature,new DecisionImporter(host.Feature));var(store,saved,run)=Local(host);
        remote.BeforeRequest=()=>store.DeleteRecipe("claim");await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run),CancellationToken.None);Assert.That(store.Recipe("claim"),Is.Null);
        (store,saved,run)=Local(host,"bob");remote.BeforeRequest=()=>new PublisherConfiguration(publisher.Ctx).Save("bob",new JsonObject {["apiKey"]="new-account-key",["userId"]="new-account"});await sharing.PublishAsync("bob",store,"claim",PublishBody(saved,run),CancellationToken.None);
        Assert.That((await sharing.StatusAsync("bob",store,"claim",CancellationToken.None))["publication"],Is.Null);Assert.That(remote.Calls.Any(c=>c.Method=="POST"&&c.Key=="bob-key"),Is.True);
    }
    [Test,Explicit("Requires the isolated migration publisher fixture at 127.0.0.1:5129")]
    public async Task Real_CSharp_to_isolated_publisher_creation_response_loss_update_import_stars_and_revocation()
    {
        const string origin="http://127.0.0.1:5129";using var network=new HttpClient(new SocketsHttpHandler {AllowAutoRedirect=false,UseCookies=false}) {Timeout=TimeSpan.FromSeconds(15)};
        Assert.That(await network.GetStringAsync(origin+"/fixture/health"),Is.EqualTo("isolated-ai-chat-migration-publisher"));
        using var host=new AiChatMigrationTestHost();var loseCreate=true;var loseUpdate=false;var posts=0;var puts=0;
        var publisher=host.Install(new PublishExtension {Enabled=true,HttpHandlerFactory=()=>new AiChatMigrationHttpHandler(async(request,token)=>{
            if(request.Method==HttpMethod.Post)posts++;if(request.Method==HttpMethod.Put&&!request.RequestUri!.AbsolutePath.EndsWith("/star"))puts++;
            using var forwarded=new HttpRequestMessage(request.Method,request.RequestUri);
            foreach(var header in request.Headers)forwarded.Headers.TryAddWithoutValidation(header.Key,header.Value);
            if(request.Content!=null){forwarded.Content=new ByteArrayContent(await request.Content.ReadAsByteArrayAsync(token));foreach(var header in request.Content.Headers)forwarded.Content.Headers.TryAddWithoutValidation(header.Key,header.Value);}
            var response=await network.SendAsync(forwarded,token);response.RequestMessage=request;
            if(request.Method==HttpMethod.Post&&loseCreate){loseCreate=false;response.Dispose();throw new HttpRequestException("Controlled committed create response loss");}
            if(request.Method==HttpMethod.Put&&loseUpdate&&!request.RequestUri!.AbsolutePath.EndsWith("/star")){loseUpdate=false;response.Dispose();throw new HttpRequestException("Controlled committed update response loss");}
            return response;
        })});
        var config=new PublisherConfiguration(publisher.Ctx);foreach(var user in new[]{"alice","bob"})config.Save(user,new JsonObject {["baseUrl"]=origin,["allowHttp"]=true,["apiKey"]="migration-publisher-"+user,["userId"]=user+"-publisher",["userName"]=user});
        var sharing=new DecisionSharing(host.Feature,new DecisionImporter(host.Feature));var (store,saved,run)=Local(host);
        Assert.ThrowsAsync<HttpError>(async()=>await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run),CancellationToken.None));
        var receipt=await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run),CancellationToken.None);var reference=receipt.GetString("externalRef")!;Assert.That(posts,Is.EqualTo(2));Assert.That(receipt.ContainsKey("publisherRunCount"),Is.False);
        store.Favourite("claim",true);loseUpdate=true;Assert.ThrowsAsync<HttpError>(async()=>await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run,1),CancellationToken.None));receipt=await sharing.PublishAsync("alice",store,"claim",PublishBody(saved,run,1),CancellationToken.None);Assert.That(puts,Is.EqualTo(1));Assert.That(receipt.GetLong("revision"),Is.EqualTo(2));
        var star=(JsonObject)(await sharing.StarAsync("bob",reference,new JsonObject {["starred"]=true},CancellationToken.None))!;Assert.That(star.GetLong("starCount"),Is.EqualTo(2));
        var catalog=(JsonObject)(await sharing.CatalogAsync("bob",new[]{new KeyValuePair<string,string?>("q",saved["document"]!.AsObject().GetString("name"))},CancellationToken.None))!;Assert.That(catalog["items"]!.AsArray().First(r=>r!.AsObject().GetString("externalRef")==reference)!.AsObject().GetBool("starred"),Is.True);
        var preview=await sharing.PreviewAsync("bob",JsonValue.Create(origin+"/d/"+reference),CancellationToken.None);var bobStore=new JevStore(host.Feature.AppData.GetUserPath("bob"));
        var imported=await sharing.ImportAsync("bob",bobStore,new JsonObject {["externalRef"]=reference,["publishedRevision"]=preview["revision"]!.DeepClone(),["contentHash"]=preview["contentHash"]!.DeepClone(),["filename"]="Claim copy.json"},CancellationToken.None);
        Assert.That(imported.GetString("id"),Is.EqualTo("Claim copy"));Assert.That(imported["sourceExample"],Is.Not.Null);Assert.That(imported["publication"],Is.Null);Assert.That(bobStore.History()["items"]!.AsArray(),Is.Empty);
        await sharing.StarAsync("bob",reference,new JsonObject {["starred"]=false},CancellationToken.None);await sharing.UnpublishAsync("alice",store,"claim",new JsonObject {["publishedRevision"]=2},CancellationToken.None);Assert.That((await sharing.StatusAsync("alice",store,"claim",CancellationToken.None))["publication"],Is.Null);
    }
}
