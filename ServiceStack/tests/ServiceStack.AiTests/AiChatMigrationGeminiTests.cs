#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.OrmLite;

namespace ServiceStack.AiTests;

public class AiChatMigrationGeminiTests
{
    [Test]
    public async Task Portable_manifest_keeps_settings_metadata_and_relative_paths_without_server_fields()
    {
        using var host=new AiChatMigrationTestHost();var root=Path.Combine(host.DirectoryPath,"source");Directory.CreateDirectory(root);var input=Path.Combine(root,"pages");Directory.CreateDirectory(input);
        var manifest=Path.Combine(root,"import.json");File.WriteAllText(manifest,"""{"version":1,"metadata":{"defaults":{"category":"Docs","product":"Parent"},"rules":[]},"crawl":{"url":"https://example.org/","name":"site"},"transforms":[],"source":{"name":"Portable","type":"folder","config":{"path":"pages"}}}""");
        var source=GeminiImportManifest.Load(manifest,new JsonObject { ["id"]=123,["filestoreId"]=45,["user"]="alice" });Assert.That(source.GetObject("config").GetString("path"),Is.EqualTo(input));Assert.That(source.GetObject("category").GetString("prefix"),Is.EqualTo("Docs"));
        source.GetObject("config")!["saved"]=true;source.GetObject("config")!["metadataSpecified"]=false;source["rules"]=new JsonObject { ["defaults"]=new JsonObject { ["product"]="Ignored" },["rules"]=new JsonArray() };
        await GeminiImportManifest.SaveAsync(source);var saved=GeminiImportManifest.Read(manifest);Assert.That(saved.GetObject("metadata").GetObject("defaults").GetString("product"),Is.EqualTo("Parent"));Assert.That(saved.GetObject("source").GetObject("config").GetString("path"),Is.EqualTo("pages"));Assert.That(saved.GetObject("source").ContainsKey("filestoreId"),Is.False);Assert.That(saved.GetObject("source").GetObject("config").ContainsKey("saved"),Is.False);
        source.GetObject("config")!["metadataSpecified"]=true;await GeminiImportManifest.SaveAsync(source);Assert.That(GeminiImportManifest.Read(manifest).GetObject("metadata").GetObject("defaults").GetString("product"),Is.EqualTo("Ignored"));
    }
    [TestCase("[]")] [TestCase("{broken")] [TestCase("{\"include\":[3]}")] [TestCase("{\"source\":[]} ")] [TestCase("{\"source\":{\"config\":[]}}")] [TestCase("{\"metadata\":{\"rules\":{}}}")]
    public void Malformed_manifests_fail_before_discovery_could_prune_documents(string contents)
    {
        using var host=new AiChatMigrationTestHost();File.WriteAllText(Path.Combine(host.DirectoryPath,"import.json"),contents);
        Assert.Throws<ArgumentException>(()=>GeminiImportManifest.Read(Path.Combine(host.DirectoryPath,"import.json")));
        Assert.Throws<ArgumentException>(()=>GeminiIngest.Discover(new JsonObject { ["path"]=host.DirectoryPath },"folder"));
    }
    [Test]
    public void Nested_ignore_patterns_child_defaults_and_explicit_metadata_are_applied()
    {
        using var host=new AiChatMigrationTestHost();var root=host.DirectoryPath;Directory.CreateDirectory(Path.Combine(root,"child"));Directory.CreateDirectory(Path.Combine(root,"skip"));
        File.WriteAllText(Path.Combine(root,"import.json"),"""{"include":"**/*.txt,*.txt","ignore":"skip","metadata":{"defaults":{"product":"Parent"}}}""");File.WriteAllText(Path.Combine(root,"child","import.json"),"""{"ignore":["hidden.txt"],"metadata":{"defaults":{"product":"Child"}}}""");
        foreach(var relative in new[]{"one.txt","child/two.txt","child/hidden.txt","skip/hidden.txt"})File.WriteAllText(Path.Combine(root,relative),"Some text for extraction");
        var config=new JsonObject { ["path"]=root };var source=new ChatSource { Type="folder",Config=config.ToJsonString(),Rules="{\"defaults\":{\"product\":\"Fallback\"}}",Extract="{\"minWords\":0}" };
        var plan=GeminiIngest.BuildPlan(source,[]);Assert.That(plan.Added.Select(x=>x.SourceKey),Is.EquivalentTo(new[]{"one.txt","child/two.txt"}));Assert.That(plan.Added.Single(x=>x.SourceKey=="child/two.txt").Metadata.GetString("product"),Is.EqualTo("Child"));
        config["metadataSpecified"]=true;source.Config=config.ToJsonString();Assert.That(GeminiIngest.BuildPlan(source,[]).Added.Single(x=>x.SourceKey=="child/two.txt").Metadata.GetString("product"),Is.EqualTo("Fallback"));
    }
    [Test]
    public async Task Private_crawl_copy_has_stable_name_relative_input_attribution_and_no_links()
    {
        using var host=new AiChatMigrationTestHost();var original=Path.Combine(host.DirectoryPath,"outside");var imports=Path.Combine(host.DirectoryPath,"private");Directory.CreateDirectory(Path.Combine(original,"pages"));var manifest=Path.Combine(original,"import.json");File.WriteAllText(manifest,"""{"crawl":{"name":"site","url":"https://example.org/"},"source":{"type":"folder","config":{"path":"pages"}}}""");File.WriteAllText(Path.Combine(original,"pages","page.md"),"first");
        if(!OperatingSystem.IsWindows())File.CreateSymbolicLink(Path.Combine(original,"linked"),manifest);
        var owned=await GeminiImportWorkspaces.ImportAsync(imports,manifest);Assert.That(owned,Is.EqualTo(Path.Combine(imports,"site","import.json")));Assert.That(GeminiImportManifest.Load(owned).GetObject("config").GetString("path"),Is.EqualTo(Path.Combine(imports,"site","pages")));Assert.That(GeminiImportManifest.Read(owned).GetString("importedFrom"),Is.EqualTo(manifest));Assert.That(File.Exists(Path.Combine(imports,"site","linked")),Is.False);
        File.WriteAllText(Path.Combine(original,"pages","page.md"),"second");Assert.That(await GeminiImportWorkspaces.ImportAsync(imports,manifest),Is.EqualTo(owned));Assert.That(File.ReadAllText(Path.Combine(imports,"site","pages","page.md")),Is.EqualTo("second"));Assert.That(GeminiImportWorkspaces.Target(imports,owned),Is.EqualTo(Path.Combine(imports,"site")));
    }
    [Test]
    public void Detached_manifests_keep_distinct_identity_and_reload_attaches_only_the_owned_store()
    {
        using var host=new AiChatMigrationTestHost();var db=new GeminiDb(host.Db);db.InitSchema();
        var store=db.InsertFilestore(new ChatFilestore { User="alice",DisplayName="Docs" });
        var other=db.InsertFilestore(new ChatFilestore { User="bob",DisplayName="Other" });
        ChatSource Source(string name,string path)=>new() { User="alice",FilestoreId=store,Name=name,Type="folder",Config=new JsonObject { ["path"]=path,["manifestPath"]=Path.Combine(path,"import.json") }.ToJsonString() };
        var one=Source("One",Path.Combine(host.DirectoryPath,"one"));one.Id=db.InsertSource(one);var two=Source("Two",Path.Combine(host.DirectoryPath,"two"));two.Id=db.InsertSource(two);
        ChatDocument Document(ChatSource source)=>new() { User="alice",FilestoreId=store,SourceId=source.Id,SourceKey="same.md",DisplayName="same.md",SourceManifestPath=ChatJson.ParseObject(source.Config!).GetString("manifestPath"),Name="remote/"+source.Name,Hash="same" };
        var a=Document(one);a.Id=db.InsertDocument(a);var b=Document(two);b.Id=db.InsertDocument(b);
        db.DeleteSource(one.Id,"alice");db.DeleteSource(two.Id,"alice");Assert.That(db.GetDocument(a.Id,"alice")!.SourceId,Is.Null);Assert.That(db.GetDocument(b.Id,"alice")!.SourceId,Is.Null);
        Assert.That(db.FindDocumentBySourceKey(store,null,"same.md","alice"),Is.Null,"Manual upload lookup must not adopt a detached import");
        var manual=new ChatDocument { User="alice",FilestoreId=store,SourceKey="same.md",DisplayName="same.md",Name="remote/manual" };manual.Id=db.InsertDocument(manual);
        Assert.That(db.FindDocumentBySourceKey(store,null,"same.md","alice")!.Id,Is.EqualTo(manual.Id));
        var newOne=Source("Reload",Path.Combine(host.DirectoryPath,"one"));newOne.Id=db.InsertSource(newOne);db.AttachSourceDocuments(newOne.Id,store,a.SourceManifestPath!,"alice");
        Assert.That(db.GetDocument(a.Id,"alice")!.SourceId,Is.EqualTo(newOne.Id));Assert.That(db.GetDocument(a.Id,"alice")!.Name,Is.EqualTo("remote/One"));Assert.That(db.GetDocument(b.Id,"alice")!.SourceId,Is.Null);
        db.AttachSourceDocuments(newOne.Id,other,b.SourceManifestPath!,"bob");Assert.That(db.GetDocument(b.Id,"alice")!.SourceId,Is.Null);
        Assert.That(db.GetDocument(a.Id,"alice")!.ToDto().ContainsKey("sourceScopeId"),Is.False);
    }
    [Test]
    public void Identity_reservations_preserve_unicode_case_null_keys_and_owner_partitions()
    {
        using var host=new AiChatMigrationTestHost();var db=new GeminiDb(host.Db);db.InitSchema();
        var store=db.InsertFilestore(new ChatFilestore { User="alice",DisplayName="Docs" });
        foreach(var key in new[]{"A.md","a.md","é.md","é.md"})db.InsertDocument(new ChatDocument { User="alice",FilestoreId=store,SourceId=123,SourceKey=key,DisplayName=key });
        Assert.That(db.QueryAllDocuments(store,"alice").Count(),Is.EqualTo(4));
        Assert.That(()=>db.InsertDocument(new ChatDocument { User="alice",FilestoreId=store,SourceId=123,SourceKey="A.md" }),Throws.Exception);
        db.InsertDocument(new ChatDocument { User="bob",FilestoreId=store,SourceId=123,SourceKey="A.md" });
        db.InsertDocument(new ChatDocument { User="alice",FilestoreId=store });db.InsertDocument(new ChatDocument { User="alice",FilestoreId=store });
        var manual=new ChatDocument { User="alice",FilestoreId=store,SourceKey="manual" };db.InsertDocument(manual);
        Assert.That(()=>db.InsertDocument(new ChatDocument { User="alice",FilestoreId=store,SourceKey="manual",SourceManifestPath="" }),Throws.Exception);
        var ids=db.QueryAllDocuments(store,null).Select(x=>x.Id).ToArray();db.InitSchema();Assert.That(db.QueryAllDocuments(store,null).Select(x=>x.Id),Is.EquivalentTo(ids));
    }
    [Test]
    public void Backfill_detects_duplicate_active_identity_before_rewriting_any_document()
    {
        using var host=new AiChatMigrationTestHost();var db=new GeminiDb(host.Db);db.InitSchema();
        using(var connection=db.OpenDb()) {
            connection.Insert(new ChatDocument { User="alice",FilestoreId=1,SourceId=99,SourceScopeId=100,SourceKey="same" });
            connection.Insert(new ChatDocument { User="alice",FilestoreId=1,SourceId=99,SourceScopeId=101,SourceKey="same" });
        }
        Assert.Throws<InvalidOperationException>(()=>db.InitSchema());using var verify=db.OpenDb();Assert.That(verify.Select<ChatDocument>().Select(x=>x.SourceScopeId),Is.EquivalentTo(new long[]{100,101}));
    }
}
