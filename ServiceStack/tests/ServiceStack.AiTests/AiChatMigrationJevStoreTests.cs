#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
namespace ServiceStack.AiTests;

public class AiChatMigrationJevStoreTests
{
    string directory=null!;
    static JsonObject Starter=>JsonNode.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"AiChatFixtures","jev-contract-vectors.json")))!["recipes"]!.AsArray().First(x=>x!["name"]!.GetValue<string>()=="support")!["document"]!.AsObject().Clone();
    JevStore Store()=>new(directory);
    [SetUp] public void Setup(){directory=Path.Combine(Path.GetTempPath(),"jev-store-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);}
    [TearDown] public void Cleanup(){Directory.Delete(directory,true);}
    static (JsonObject,JsonNode,JsonObject) Request(JsonObject? document=null)
    {
        var doc=document??Starter;var input=doc["examples"]![0]!["input"]!.DeepClone();return(doc,input,DecisionRecipeValidator.Compile(doc,input));
    }
    static JsonObject Submit(JevStore store,string submission,string? id=null,long? revision=null)
    {
        var (doc,input,request)=Request();return store.Submit(submission,doc,input,request,"owner",id,revision).Run;
    }
    [Test]
    public void Saved_names_revisions_manual_edits_and_literals_remain_portable_and_isolated()
    {
        var store=Store();var saved=store.Save(Starter,filename:"Café 😀.json.json");Assert.That(saved.GetString("id"),Is.EqualTo("Café 😀.json"));Assert.That(saved.GetString("filename"),Is.EqualTo("Café 😀.json.json"));
        var doc=Starter;doc["description"]="Changed";Assert.Throws<JevConflictException>(()=>store.Save(doc,saved.GetString("id"),2));
        var changed=store.Save(doc,saved.GetString("id"),1);Assert.That(changed["revision"]!.GetValue<long>(),Is.EqualTo(2));Assert.That(changed.GetString("filename"),Is.EqualTo(saved.GetString("filename")));
        doc["name"]="Manual file edit";JevStore.WriteJson(Path.Combine(store.RecipesPath,saved.GetString("filename")!),doc);Assert.That(store.Recipe(saved.GetString("id")!)!["revision"]!.GetValue<long>(),Is.EqualTo(3));
        var other=new JevStore(Path.Combine(directory,"bob"));Assert.That(other.Recipes(),Is.Empty);Assert.Throws<JevRecipeExistsException>(()=>store.Save(Starter,filename:"CAFÉ 😀.JSON.json"));
    }
    [Test]
    public void Unicode_casefold_collision_matches_the_portable_Python_library()
    {
        var store=Store();store.Save(Starter,filename:"Straße.json");Assert.Throws<JevRecipeExistsException>(()=>store.Save(Starter,filename:"STRASSE.json"));
        store.Save(Starter,filename:"İ.json");Assert.Throws<JevRecipeExistsException>(()=>store.Save(Starter,filename:"i̇.json"));
        Assert.That(store.Recipes(),Has.Count.EqualTo(2));
    }
    [Test]
    public void Initialization_seeds_sentiment_once_preserves_receipt_and_never_overwrites_manual_recipes()
    {
        var template=Starter;var store=new JevStore(directory,template);Assert.That(store.Recipes().Single().GetString("templateId"),Is.EqualTo("sentiment"));var doc=template.Clone();doc["description"]="Edited starter";store.Save(doc,"sentiment",1);
        store=new JevStore(directory,template);Assert.That(store.Recipe("sentiment")!["document"]!["description"]!.GetValue<string>(),Is.EqualTo("Edited starter"));Assert.That(File.Exists(Path.Combine(directory,".jev-initialized.json")),Is.True);
    }
    [Test]
    public void Submission_replay_limits_snapshots_monotonic_sequences_and_keyset_history_survive_reload()
    {
        var store=Store();var recipe=store.Save(Starter,filename:"Support ticket.json");var (doc,input,request)=Request();var run=store.Submit("first",doc,input,request,"owner",recipe.GetString("id"),1);Assert.That(run.Created,Is.True);
        doc["name"]="Caller mutated";input["ticket"]="Caller mutated";Assert.That(store.Run(run.Run.GetString("id")!)!["recipe"]!["name"]!.GetValue<string>(),Is.Not.EqualTo("Caller mutated"));
        var (same,sameInput,sameRequest)=Request();Assert.That(store.Submit("first",same,sameInput,sameRequest,"other",recipe.GetString("id"),1).Created,Is.False);
        Assert.Throws<JevConflictException>(()=>store.Submit("first",doc,input,request,"owner"));var second=Submit(store,"second",recipe.GetString("id"),1);Assert.Throws<JevBusyException>(()=>Submit(store,"third"));
        var id=run.Run.GetString("id")!;Assert.That(store.Start(id,"other"),Is.False);Assert.That(store.Start(id,"owner"),Is.True);Assert.That(store.Finish(id,"cancelled",owner:"owner"),Is.True);Assert.That(store.Finish(id,"succeeded",new JsonObject(),new JsonObject(),owner:"owner"),Is.False);Assert.That(store.Run(id)!.GetString("status"),Is.EqualTo("cancelled"));
        Assert.Throws<JevConflictException>(()=>store.DeleteRun(second.GetString("id")));store.Finish(second.GetString("id")!,"failed");var history=store.History(recipe.GetString("id"),limit:1);Assert.That(history["items"]!.AsArray().Count,Is.EqualTo(1));Assert.That(history.GetString("cursor"),Is.Not.Null);Assert.That(store.History(recipe.GetString("id"),history.GetString("cursor"),1)["items"]!.AsArray().Count,Is.EqualTo(1));
        foreach(var field in new[]{"submissionId","requestHash","owner","leaseUntil"})Assert.That(store.Run(id)!.ContainsKey(field),Is.False);
        Assert.That(store.DeleteRun(),Is.EqualTo(2));store=Store();var next=Submit(store,"next",recipe.GetString("id"),1);Assert.That(next.GetString("id"),Does.EndWith("-00003"));
    }
    [Test]
    public void Expired_runs_are_interrupted_without_reexecution_and_readable_history_keeps_private_receipts_local()
    {
        var store=Store();var run=Submit(store,"first");var id=run.GetString("id")!;var path=Path.Combine(store.HistoryPath,"_drafts",id+".md");var text=File.ReadAllText(path);Assert.That(text,Does.Contain("## Input"));Assert.That(text,Does.Contain("<!-- jev-record -->"));Assert.That(JevStore.ReadHistory(path).GetString("submissionId"),Is.EqualTo("first"));
        var clock=new AiChatMigrationClock {Now=DateTimeOffset.UtcNow.AddMinutes(3)};store.Clock=clock;Assert.That(store.Run(id)!.GetString("status"),Is.EqualTo("interrupted"));Assert.That(store.Start(id,"owner"),Is.False);
    }
    [Test]
    public void Replacement_clears_confirmed_history_and_publication_without_reusing_run_numbers()
    {
        var store=Store();var saved=store.Save(Starter,filename:"support.json");store.Favourite("support",true);var run=Submit(store,"first","support",1);
        Assert.Throws<JevConflictException>(()=>store.Save(Starter,replacement:1,filename:"support.json"));store.Finish(run.GetString("id")!,"succeeded");
        store.Transaction(index=>{index["recipes"]!["support"]!["publication"]=new JsonObject {["externalRef"]="old"};return 0;});
        Assert.Throws<JevRecipeExistsException>(()=>store.Save(Starter,filename:"support.json"));Assert.Throws<JevConflictException>(()=>store.Save(Starter,replacement:2,filename:"support.json"));
        var replaced=store.Save(Starter,replacement:1,filename:"support.json");Assert.That(replaced.ContainsKey("publication"),Is.False);Assert.That(store.History("support")["items"]!.AsArray(),Is.Empty);Assert.That(Submit(store,"new","support",2).GetString("id"),Is.EqualTo("support-00002"));
    }
    [Test]
    public void Shared_import_and_legacy_rename_journals_recover_recipe_metadata_history_and_favourites_together()
    {
        var store=Store();var saved=store.Save(Starter,filename:"old.json");var run=Submit(store,"first","old",1);store.Finish(run.GetString("id")!,"failed");store.Favourite("old",true);
        var renamed=Starter;renamed["name"]="Nouveau café";{var index=JevStore.ReadJson(Path.Combine(store.Root,"index.json"))!.AsObject();var metadata=index["recipes"]!["old"]!.AsObject().Clone();metadata["hash"]=JevJson.Hash(renamed);metadata["revision"]=2;JevStore.WriteJson(Path.Combine(store.Root,".renames.json"),new JsonObject {["old"]=new JsonObject {["document"]=renamed,["metadata"]=metadata}});}
        store=Store();Assert.That(store.Recipe("old"),Is.Null);Assert.That(store.Recipe("Nouveau café"),Is.Not.Null);Assert.That(store.Run(run.GetString("id")!)!.GetString("recipeId"),Is.EqualTo("Nouveau café"));Assert.That(store.Favourites(),Is.EqualTo(new[]{"Nouveau café"}));Assert.That(File.Exists(Path.Combine(store.Root,".renames.json")),Is.False);
        var document=Starter;var id="Nouveau café";var meta=new JsonObject {["revision"]=3,["createdAt"]=1,["updatedAt"]=2,["hash"]=JevJson.Hash(document),["importSource"]=new JsonObject {["exampleRef"]=new string('a',32)+".json"}};
        JevStore.WriteJson(Path.Combine(store.Root,".shared-import.json"),new JsonObject {["identity"]=id,["filename"]=id+".json",["document"]=document,["metadata"]=meta,["oldMetadata"]=new JsonObject(),["historyPaths"]=new JsonArray(id+"/"+run.GetString("id")+".md")});
        store=Store();Assert.That(store.History(id)["items"]!.AsArray(),Is.Empty);Assert.That(store.Recipe(id)!["revision"]!.GetValue<long>(),Is.EqualTo(3));Assert.That(File.Exists(Path.Combine(store.Root,".shared-import.json")),Is.False);
    }
    [Test]
    public void Filename_identity_migration_retains_aliases_and_unambiguous_double_json_stems()
    {
        var store=Store();var saved=store.Save(Starter,filename:"foo.json.json");var run=Submit(store,"first","foo.json",1);store.Finish(run.GetString("id")!,"failed");
        var index=JevStore.ReadJson(Path.Combine(store.Root,"index.json"))!.AsObject();index.Remove("filenameStemIdentities");index["filenameIdentities"]=true;var meta=index["recipes"]!["foo.json"]!.DeepClone();index["recipes"]=new JsonObject {["foo.json.json"]=meta};index["favourites"]=new JsonArray("foo.json.json");JevStore.WriteJson(Path.Combine(store.Root,"index.json"),index);
        var path=Path.Combine(store.HistoryPath,"foo.json",run.GetString("id")+".md");var record=JevStore.ReadHistory(path);record["recipeId"]="foo.json.json";JevStore.WriteHistory(path,record);
        store=Store();Assert.That(store.Recipes().Single().GetString("id"),Is.EqualTo("foo.json"));Assert.That(store.Recipes().Single()["previousIds"]!.AsArray().Select(x=>x!.GetValue<string>()),Does.Contain("foo.json.json"));Assert.That(store.Run(run.GetString("id")!)!.GetString("recipeId"),Is.EqualTo("foo.json"));store=Store();Assert.That(store.Run(run.GetString("id")!)!.GetString("recipeId"),Is.EqualTo("foo.json"));
    }
    [Test]
    public async Task Short_transactions_conflict_without_partial_writes_and_release_after_failure()
    {
        var store=Store();using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();var task=Task.Run(()=>store.Transaction(_=>{entered.Set();release.Wait();return 0;}));Assert.That(entered.Wait(TimeSpan.FromSeconds(2)),Is.True);
        try{Assert.Throws<JevConflictException>(()=>store.Save(Starter,filename:"blocked.json"));}finally{release.Set();await task;}Assert.That(store.Recipe("blocked"),Is.Null);
        Assert.Throws<InvalidOperationException>(()=>store.Transaction<int>(_=>throw new InvalidOperationException("fixture")));Assert.DoesNotThrow(()=>store.Save(Starter,filename:"after.json"));
    }
    [Test]
    public void Invalid_history_index_journal_and_linked_paths_fail_without_deleting_uncertain_files()
    {
        var store=Store();JevStore.WriteJson(Path.Combine(store.Root,"index.json"),new JsonArray());Assert.Throws<JevStorageException>(()=>Store());File.Delete(Path.Combine(store.Root,"index.json"));store=Store();
        var row=store.Save(Starter,filename:"safe.json");JevStore.WriteJson(Path.Combine(store.Root,".shared-import.json"),new JsonObject {["identity"]="safe",["filename"]="safe.json",["document"]=Starter,["metadata"]=new JsonObject(),["oldMetadata"]=new JsonObject(),["historyPaths"]=new JsonArray("../escape.md")});
        Assert.Throws<JevStorageException>(()=>store.Recipes());Assert.That(File.Exists(Path.Combine(store.RecipesPath,"safe.json")),Is.True);File.Delete(Path.Combine(store.Root,".shared-import.json"));
        if(!OperatingSystem.IsWindows()){var outside=Path.Combine(directory,"outside.json");JevStore.WriteJson(outside,Starter);File.CreateSymbolicLink(Path.Combine(store.RecipesPath,"linked.json"),outside);Assert.Throws<JevStorageException>(()=>store.Recipes());Assert.That(File.Exists(outside),Is.True);}
    }
}
