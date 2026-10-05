#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.OrmLite;

namespace ServiceStack.AiTests;

public class AiChatMigrationGeminiRouteTests
{
    sealed class HttpFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler:false);
    }
    static HttpResponseMessage Response(string json="{}", HttpStatusCode status=HttpStatusCode.OK) => new(status) { Content=new StringContent(json) };
    static GeminiDb Install(AiChatMigrationTestHost host,Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>>? send=null)
    {
        host.Feature.Services=new ServiceCollection().BuildServiceProvider();
        host.Feature.Variables["GEMINI_API_KEY"]="fixture";
        host.Feature.Variables["GEMINI_UPLOAD_MAX_RETRIES"]="1";
        host.Feature.Log=NullLogger.Instance;
        host.Feature.HttpClientFactory=new HttpFactory(new AiChatMigrationHttpHandler(async(req,token)=>{var response=await (send??((_,_)=>Task.FromResult(Response())))(req,token);response.RequestMessage??=req;return response;}));
        // Import roots come from the requesting user's own allowed directories, never 'default''s
        host.Feature.SetAllowedDirectories([host.DirectoryPath],"alice");
        host.Install(new GeminiExtension());return new GeminiDb(host.Db);
    }
    static long Store(GeminiDb db,string user="alice") => db.InsertFilestore(new ChatFilestore { User=user,Name="fileSearchStores/"+user,DisplayName="Docs" });
    static ChatDocument Doc(GeminiDb db,long store,string name,string category="docs",string user="alice")
    {
        var doc=new ChatDocument { User=user,FilestoreId=store,Name="remote/"+name,DisplayName=name,SourceKey=name,Category=category,Hash="hash",UploadedAt=DateTime.UtcNow };
        doc.Id=db.InsertDocument(doc);return doc;
    }
    [TestCase("")] [TestCase("/chat")]
    public async Task Saved_import_registration_edits_detach_and_reload_keep_remote_identity_without_upload(string prefix)
    {
        using var host=new AiChatMigrationTestHost(prefix);var calls=0;
        var db=Install(host,(_,_)=>{Interlocked.Increment(ref calls);return Task.FromResult(Response());});var store=Store(db);var bob=Store(db,"bob");
        host.Feature.SetAllowedDirectories([host.DirectoryPath],"bob"); // each user's import roots are their own grants
        var root=Path.Combine(host.DirectoryPath,"source");Directory.CreateDirectory(root);var manifest=Path.Combine(root,"import.json");
        File.WriteAllText(manifest,"""{"version":1,"source":{"name":"Docs","type":"folder","config":{"path":"."}},"metadata":{"defaults":{"product":"Parent"}},"include":["*.md"]}""");
        async Task<JsonObject> Register(string user="alice",long? target=null) => (JsonObject)(await host.SendAsync("POST",prefix+"/ext/gemini/sources/load",user,new JsonObject { ["filestoreId"]=target??store,["path"]=manifest }))!;
        var one=await Register();var id=one.GetLong("id")!.Value;
        Assert.That((await Register()).GetLong("id"),Is.EqualTo(id));Assert.That(db.QuerySources(store,"alice").Count,Is.EqualTo(1));
        Assert.ThrowsAsync<HttpError>(async()=>await Register("bob"));
        var other=await Register("bob",bob);Assert.That(other.GetLong("id"),Is.Not.EqualTo(id));
        var doc=Doc(db,store,"one.md");doc.SourceId=id;doc.SourceManifestPath=manifest;db.UpdateDocument(doc);
        await host.SendAsync("PATCH",prefix+"/ext/gemini/sources/"+id,"alice",new JsonObject { ["name"]="Edited",["saveConfig"]=true,["rules"]=new JsonObject { ["defaults"]=new JsonObject { ["product"]="Edited" } } });
        Assert.That(GeminiImportManifest.Read(manifest).GetObject("metadata").GetObject("defaults").GetString("product"),Is.EqualTo("Edited"));
        Assert.That(db.GetSource(id,"alice")!.LastRunId,Is.Null);
        await host.SendAsync("DELETE",prefix+"/ext/gemini/sources/"+id,"alice");Assert.That(db.GetDocument(doc.Id,"alice")!.SourceId,Is.Null);
        var loaded=await Register();Assert.That(db.GetDocument(doc.Id,"alice")!.SourceId,Is.EqualTo(loaded.GetLong("id")));Assert.That(db.GetDocument(doc.Id,"alice")!.Name,Is.EqualTo(doc.Name));Assert.That(calls,Is.Zero);
    }
    [Test]
    public async Task Deletion_preserves_failed_rows_names_errors_and_last_visible_document()
    {
        using var host=new AiChatMigrationTestHost();var deletes=0;
        var db=Install(host,(request,_)=>{
            if(request.Method!=HttpMethod.Delete)return Task.FromResult(Response());
            Interlocked.Increment(ref deletes);var path=request.RequestUri!.AbsolutePath;
            return Task.FromResult(path.EndsWith("/gone")?Response("",HttpStatusCode.Gone):path.EndsWith("/bad")?Response("{\"error\":{\"message\":\"Rejected\"}}",HttpStatusCode.InternalServerError):Response());
        });var store=Store(db);var one=Doc(db,store,"one");var gone=Doc(db,store,"gone");var bad=Doc(db,store,"bad");var keep=Doc(db,store,"keep","other");
        var result=(JsonObject)(await host.SendAsync("POST","/ext/gemini/documents/delete","alice",new JsonObject { ["ids"]=new JsonArray(one.Id,gone.Id,bad.Id) }))!;
        Assert.That(result.GetInt("deleted"),Is.EqualTo(2));Assert.That(result.GetArray("errors")![0]!.AsObject().GetString("displayName"),Is.EqualTo("bad"));Assert.That(db.GetDocument(bad.Id,"alice"),Is.Not.Null);
        Assert.ThrowsAsync<HttpError>(async()=>await host.SendAsync("POST","/ext/gemini/documents/delete","alice",new JsonObject { ["filter"]=new JsonObject { ["filestoreId"]=store,["categoryUnder"]="/" } }));Assert.That(deletes,Is.EqualTo(3));
        var summary=(JsonObject)(await host.SendAsync("POST","/ext/gemini/documents/summary","alice",new JsonObject { ["ids"]=new JsonArray(bad.Id,keep.Id) }))!;Assert.That(summary.GetBool("deleteAllowed"),Is.False);
        var empty=(JsonObject)(await host.SendAsync("POST","/ext/gemini/documents/delete","alice",new JsonObject { ["ids"]=new JsonArray(),["filter"]=new JsonObject { ["filestoreId"]=store } }))!;Assert.That(empty.GetInt("selected"),Is.Zero);Assert.That(deletes,Is.EqualTo(3));
    }
    [Test]
    public async Task Concurrent_manual_deletions_cannot_jointly_empty_store()
    {
        using var host=new AiChatMigrationTestHost();var db=Install(host);var store=Store(db);var a=Doc(db,store,"a");var b=Doc(db,store,"b");
        async Task<bool> Delete(long id) {try{await host.SendAsync("DELETE","/ext/gemini/documents/"+id,"alice");return true;}catch(HttpError){return false;}}
        Assert.That((await Task.WhenAll(Delete(a.Id),Delete(b.Id))).Count(x=>x),Is.EqualTo(1));Assert.That(db.QueryAllDocuments(store,"alice").Count(),Is.EqualTo(1));
    }
    [Test]
    public async Task Deletion_bounds_parallelism_and_leaves_progress_reads_responsive()
    {
        using var host=new AiChatMigrationTestHost();var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var active=0;var peak=0;
        var db=Install(host,async(request,token)=>{if(request.Method!=HttpMethod.Delete)return Response();var n=Interlocked.Increment(ref active);peak=Math.Max(peak,n);if(n==4)entered.TrySetResult();await release.Task.WaitAsync(token);Interlocked.Decrement(ref active);return Response();});
        var store=Store(db);var docs=Enumerable.Range(1,8).Select(x=>Doc(db,store,x.ToString())).ToArray();Doc(db,store,"keep","other");
        var task=host.SendAsync("POST","/ext/gemini/documents/delete","alice",new JsonObject { ["filter"]=new JsonObject { ["filestoreId"]=store,["categoryUnder"]="docs" } });
        try{await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));Assert.That(task.IsCompleted,Is.False);await host.SendAsync("GET","/ext/gemini/worker","alice");Assert.That(peak,Is.EqualTo(4));}finally{release.TrySetResult();}
        Assert.That(((JsonObject)(await task)!).GetInt("deleted"),Is.EqualTo(8));
    }
    [Test]
    public async Task Empty_queue_resume_is_a_noop_and_progress_counts_are_source_scoped()
    {
        using var host=new AiChatMigrationTestHost();var calls=0;var db=Install(host,(_,_)=>{Interlocked.Increment(ref calls);return Task.FromResult(Response());});var store=Store(db);var a=Doc(db,store,"a");a.SourceId=123;a.UploadedAt=null;a.StartedAt=DateTime.UtcNow;db.UpdateDocument(a);var b=Doc(db,store,"b");b.SourceId=456;b.UploadedAt=null;b.Error="failed";db.UpdateDocument(b);
        var result=(JsonObject)(await host.SendAsync("POST","/ext/gemini/filestores/"+store+"/resume-uploads","alice",new JsonObject { ["ids"]=new JsonArray() }))!;Assert.That(result.GetInt("queued"),Is.Zero);Assert.That(calls,Is.Zero);Assert.That(db.GetDocument(b.Id,"alice")!.Error,Is.EqualTo("failed"));
        var counts=(JsonObject)(await host.SendAsync("GET","/ext/gemini/documents/pending?filestoreId="+store,"alice"))!;Assert.That(counts.GetLong("uploading"),Is.EqualTo(1));Assert.That(counts.GetObject("sources").GetObject("123").GetInt("pending"),Is.EqualTo(1));Assert.That(counts.GetObject("sources").GetObject("456").GetInt("failed"),Is.EqualTo(1));
    }
    [Test]
    public void Pending_exclusion_happens_before_limit_and_legacy_backfill_preserves_manifest()
    {
        using var host=new AiChatMigrationTestHost();var db=Install(host);var store=Store(db);var docs=Enumerable.Range(1,5).Select(x=>Doc(db,store,x.ToString())).ToArray();foreach(var d in docs){d.UploadedAt=null;db.UpdateDocument(d);}
        Assert.That(db.GetPendingDocuments(2,docs.Take(2).Select(x=>x.Id).ToArray()).Select(x=>x.Id),Is.EqualTo(docs.Skip(2).Take(2).Select(x=>x.Id)));
        var source=new ChatSource { User="alice",FilestoreId=store,Config=new JsonObject { ["manifestPath"]=Path.Combine(host.DirectoryPath,"import.json") }.ToJsonString() };source.Id=db.InsertSource(source);using(var connection=db.OpenDb())connection.UpdateOnly(()=>new ChatDocument { SourceId=source.Id,SourceScopeId=source.Id,SourceManifestPath=null },x=>x.Id==docs[0].Id);
        db.InitSchema();Assert.That(db.GetDocument(docs[0].Id,"alice")!.SourceManifestPath,Is.EqualTo(Path.Combine(host.DirectoryPath,"import.json")));
    }
    [Test]
    public async Task Worker_retains_ownership_through_stats_refresh_and_drains_requested_restart()
    {
        using var host=new AiChatMigrationTestHost();var refreshEntered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);GeminiDb? db=null;var gets=0;var refreshes=0;
        db=Install(host,async(request,token)=>{
            var path=request.RequestUri!.AbsolutePath;
            if(path.Contains("remote/")){Interlocked.Increment(ref gets);var name=path.Split('/').Last();var doc=db!.QueryAllDocuments(1,"alice").Single(x=>x.DisplayName==name);return Response(new JsonObject { ["name"]=doc.Name,["displayName"]=doc.DisplayName,["state"]="STATE_ACTIVE",["customMetadata"]=GeminiMetadata.ToCustomMetadata(doc) }.ToJsonString());}
            if(Interlocked.Increment(ref refreshes)==1){refreshEntered.TrySetResult();await release.Task.WaitAsync(token);}return Response();
        });var store=Store(db);var a=Doc(db,store,"a");a.UploadedAt=null;db.UpdateDocument(a);
        var context=new ExtensionContext(host.Feature,"gemini");var client=new GeminiClient(host.Feature.HttpClientFactory,"fixture");var worker=new GeminiUploadWorker(context,db,client,new GeminiStores(db,client,NullLogger.Instance));
        try {
            worker.Start();await refreshEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));Assert.That(worker.Running,Is.True);var b=Doc(db,store,"b");b.UploadedAt=null;db.UpdateDocument(b);worker.Start();Assert.That(refreshes,Is.EqualTo(1));release.TrySetResult();
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));while(worker.Running)await Task.Delay(10,deadline.Token);
            Assert.That(db.GetDocument(b.Id,"alice")!.UploadedAt,Is.Not.Null);Assert.That(gets,Is.EqualTo(2));Assert.That(refreshes,Is.EqualTo(2));
        }finally {release.TrySetResult();await worker.StopAsync();}
    }
    [Test]
    public async Task Queued_metadata_change_does_not_adopt_prior_hash_and_superseded_delete_receipt_survives_retry()
    {
        using var host=new AiChatMigrationTestHost();GeminiDb? db=null;var uploads=0;var deletes=0;JsonObject? sent=null;
        db=Install(host,async(request,token)=>{
            var path=request.RequestUri!.AbsolutePath;
            if(request.Method==HttpMethod.Delete){Interlocked.Increment(ref deletes);return deletes==1?Response("{\"error\":{\"message\":\"Retry cleanup\"}}",HttpStatusCode.InternalServerError):Response();}
            if(path.Contains(":uploadToFileSearchStore")){uploads++;sent=ChatJson.ParseObject(await request.Content!.ReadAsStringAsync(token));var response=Response();response.Headers.Add("x-goog-upload-url","https://fixture.example/finalize");return response;}
            if(path=="/finalize")return Response("""{"done":true,"response":{"documentName":"remote/new"}}""");
            if(path.Contains("remote/")){var doc=db!.QueryAllDocuments(1,"alice").Single();return Response(new JsonObject { ["name"]=path.EndsWith("/new")?"remote/new":doc.Name,["displayName"]=doc.DisplayName,["state"]="STATE_ACTIVE",["customMetadata"]=path.EndsWith("/new")?GeminiMetadata.ToCustomMetadata(doc):new JsonArray(new JsonObject { ["key"]="hash",["stringValue"]=doc.Hash }) }.ToJsonString());}return Response();
        });var store=Store(db);var doc=Doc(db,store,"old");doc.UploadedAt=null;doc.Category="changed";doc.Url="/~cache/aa/test.md";db.UpdateDocument(doc);Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(new ExtensionContext(host.Feature,"gemini").GetCachePath("test"))!,"aa"));File.WriteAllText(Path.Combine(Path.GetDirectoryName(new ExtensionContext(host.Feature,"gemini").GetCachePath("test"))!,"aa","test.md"),"test");
        var worker=new GeminiUploadWorker(new ExtensionContext(host.Feature,"gemini"),db,new GeminiClient(host.Feature.HttpClientFactory,"fixture"),new GeminiStores(db,new GeminiClient(host.Feature.HttpClientFactory,"fixture"),NullLogger.Instance));
        try {worker.Start();using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));while(worker.Running)await Task.Delay(10,deadline.Token);Assert.That(uploads,Is.EqualTo(1));Assert.That(sent.GetArray("customMetadata")!.OfType<JsonObject>().Single(x=>x.GetString("key")=="category").GetString("stringValue"),Is.EqualTo("changed"));Assert.That(deletes,Is.EqualTo(2));var saved=db.GetDocument(doc.Id,"alice")!;Assert.That(saved.Name,Is.EqualTo("remote/new"));Assert.That(saved.PendingDeleteNames,Is.Null);Assert.That(saved.Error,Is.Null);}finally {await worker.StopAsync();}
    }
    [Test]
    public async Task Sync_reports_malformed_saved_source_and_continues_without_erasing_queued_metadata()
    {
        using var host=new AiChatMigrationTestHost();GeminiDb? db=null;var remote=new JsonArray();
        db=Install(host,(_,_)=>Task.FromResult(Response(new JsonObject { ["documents"]=remote.DeepClone() }.ToJsonString())));var store=Store(db);
        var root=Path.Combine(host.DirectoryPath,"broken");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"import.json"),"broken json");
        var source=new ChatSource { User="alice",FilestoreId=store,Name="Broken",Type="folder",Enabled=true,Config=new JsonObject { ["path"]=root,["saved"]=true,["manifestPath"]=Path.Combine(root,"import.json") }.ToJsonString() };source.Id=db.InsertSource(source);
        var a=Doc(db,store,"a");var b=Doc(db,store,"b");a.UploadedAt=null;a.Category="Edited";db.UpdateDocument(a);
        remote.Add(new JsonObject { ["name"]=a.Name,["displayName"]="Prior name",["state"]="STATE_ACTIVE",["customMetadata"]=new JsonArray(new JsonObject { ["key"]="id",["numericValue"]=a.Id },new JsonObject { ["key"]="hash",["stringValue"]=a.Hash }) });
        remote.Add(new JsonObject { ["name"]=b.Name,["displayName"]=b.DisplayName,["state"]="STATE_ACTIVE",["customMetadata"]=GeminiMetadata.ToCustomMetadata(b) });
        // Prevent background upload consumption; reconciliation itself still executes normally.
        await host.Feature.RunAsyncShutdownHandlers();
        var response=(JsonObject)(await host.SendAsync("POST","/ext/gemini/filestores/"+store+"/sync","alice",new JsonObject()))!;
        // The shared UI renders syncResult['Source Errors'] as an IssueCard {count, docs}
        var errors=response.GetObject("Source Errors")!;Assert.That(errors.GetInt("count"),Is.EqualTo(1));Assert.That(errors.GetArray("docs")![0]!.GetValue<string>(),Does.StartWith("Broken: "));
        var saved=db.GetDocument(a.Id,"alice")!;Assert.That(saved.DisplayName,Is.EqualTo("a"));Assert.That(saved.Category,Is.EqualTo("Edited"));Assert.That(saved.UploadedAt,Is.Null);
        Assert.That(db.GetDocument(b.Id,"alice")!.Name,Is.EqualTo(b.Name));
    }

    [TestCase(HttpStatusCode.NotFound)] [TestCase(HttpStatusCode.Gone)]
    public async Task Crawl_skips_missing_pages_instead_of_failing_the_whole_crawl(HttpStatusCode missing)
    {
        // llms-py parity: a dead link (404/410) is skipped; other HTTP errors still fail the crawl
        using var host=new AiChatMigrationTestHost();
        var db=Install(host,(request,_)=>{
            if(request.RequestUri!.AbsolutePath=="/missing")return Task.FromResult(Response("gone",missing));
            var response=Response("<html><head><title>Guide</title></head><body>Guide content with a <a href=\"/missing\">dead link</a>.</body></html>");response.Content.Headers.ContentType=new("text/html");return Task.FromResult(response);
        });var store=Store(db);
        var created=(JsonObject)(await host.SendAsync("POST","/ext/gemini/imports/crawl","alice",new JsonObject { ["filestoreId"]=store,["url"]="https://fixture.example/guide",["name"]="fixture",["respectRobots"]=false,["maxPages"]=5 }))!;
        var id=created.GetObject("source")!.GetLong("id")!.Value;
        var pages=(JsonObject)(await host.SendAsync("GET","/ext/gemini/imports/fixture/pages?sourceId="+id,"alice"))!;Assert.That(pages.GetArray("pages")!.Count,Is.EqualTo(1));
    }

    [Test]
    public async Task Sync_store_reports_source_errors_and_requeues_repairable_documents_from_cache()
    {
        using var host=new AiChatMigrationTestHost();var db=Install(host,(_,_)=>Task.FromResult(Response(new JsonObject { ["documents"]=new JsonArray() }.ToJsonString())));var store=Store(db);
        var cached=Doc(db,store,"cached.md");cached.Url="/~cache/ab/cached.md";db.UpdateDocument(cached);
        var path=host.Feature.AppData.GetCachePath("ab/cached.md");Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,"cached");
        var lost=Doc(db,store,"lost.md");lost.Url="/~cache/cd/lost.md";db.UpdateDocument(lost);
        var documents=(JsonArray)(await host.SendAsync("GET","/ext/gemini/documents?filestoreId="+store,"alice"))!;
        Assert.That(documents.OfType<JsonObject>().Single(x=>x.GetLong("id")==cached.Id).GetBool("localFileExists"),Is.True);
        Assert.That(documents.OfType<JsonObject>().Single(x=>x.GetLong("id")==lost.Id).GetBool("localFileExists"),Is.False);
        await host.Feature.RunAsyncShutdownHandlers();
        var response=(JsonObject)(await host.SendAsync("POST","/ext/gemini/filestores/"+store+"/sync","alice",new JsonObject()))!;
        Assert.That(response.GetObject("Source Errors")!.GetInt("count"),Is.Zero);
        // Missing from Gemini: a cached copy is re-queued for upload; one without a cache stays flagged
        var repaired=db.GetDocument(cached.Id,"alice")!;Assert.That(repaired.UploadedAt,Is.Null);Assert.That(repaired.Name,Is.Null);
        var flagged=db.GetDocument(lost.Id,"alice")!;Assert.That(flagged.UploadedAt,Is.Not.Null);Assert.That(flagged.State,Is.EqualTo("MISSING_FROM_REMOTE"));
        Assert.That(response.GetObject("Pending Uploads")!.GetInt("count"),Is.EqualTo(1));
    }

    [Test]
    public async Task Crawl_registration_selected_pages_transforms_and_failed_recrawl_preserve_owned_source()
    {
        using var host=new AiChatMigrationTestHost();var fail=false;
        var db=Install(host,(request,_)=>{
            if(fail)return Task.FromResult(Response("unavailable",HttpStatusCode.ServiceUnavailable));
            var response=Response("<html><head><title>Fixture</title></head><body>Original guide content.</body></html>");response.Content.Headers.ContentType=new("text/html");return Task.FromResult(response);
        });var store=Store(db);
        var created=(JsonObject)(await host.SendAsync("POST","/ext/gemini/imports/crawl","alice",new JsonObject { ["filestoreId"]=store,["url"]="https://fixture.example/guide",["name"]="fixture",["respectRobots"]=false,["maxPages"]=1,["sourceSettings"]=new JsonObject { ["category"]=new JsonObject { ["prefix"]="Docs" },["rules"]=new JsonObject { ["defaults"]=new JsonObject { ["product"]="Fixture" } } } }))!;
        var source=created.GetObject("source")!;var id=source.GetLong("id")!.Value;var root=created.GetString("path")!;
        Assert.That(source.GetObject("category").GetString("prefix"),Is.EqualTo("Docs"));Assert.That(db.GetSource(id,"alice"),Is.Not.Null);
        var pages=(JsonObject)(await host.SendAsync("GET","/ext/gemini/imports/wrong-name/pages?sourceId="+id,"alice"))!;Assert.That(pages.GetArray("pages")!.Count,Is.EqualTo(1));var relative=pages.GetArray("pages")![0]!.GetValue<string>();
        var transformed=(JsonObject)(await host.SendAsync("POST","/ext/gemini/imports/wrong-name/transform?sourceId="+id,"alice",new JsonObject { ["transforms"]=new JsonArray(new JsonObject { ["pattern"]="Original",["replacement"]="Edited",["flags"]="g" }) }))!;Assert.That(transformed.GetInt("changed"),Is.EqualTo(1));
        var page=(JsonObject)(await host.SendAsync("GET","/ext/gemini/imports/wrong-name/page?sourceId="+id+"&path="+Uri.EscapeDataString(relative),"alice"))!;Assert.That(page.GetString("content"),Does.Contain("Edited"));
        Assert.ThrowsAsync<HttpError>(async()=>await host.SendAsync("GET","/ext/gemini/imports/wrong-name/pages?sourceId="+id,"bob"));
        fail=true;Assert.ThrowsAsync<HttpRequestException>(async()=>await host.SendAsync("POST","/ext/gemini/imports/crawl","alice",new JsonObject { ["filestoreId"]=store,["sourceId"]=id }));Assert.That(File.ReadAllText(Path.Combine(root,relative)),Does.Contain("Edited"));Assert.That(db.QuerySources(store,"alice").Count,Is.EqualTo(1));
    }

    [Test]
    public async Task Current_roots_and_roles_are_checked_at_execution_and_repeated_previews_do_not_queue_uploads()
    {
        using var host=new AiChatMigrationTestHost();var calls=0;var db=Install(host,(_,_)=>{Interlocked.Increment(ref calls);return Task.FromResult(Response());});var store=Store(db);var root=Path.Combine(host.DirectoryPath,"source");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"page.md"),"A guide for an isolated import.");var manifest=Path.Combine(root,"import.json");File.WriteAllText(manifest,"""{"source":{"name":"Docs","type":"folder","config":{"path":".","include":["*.md"]},"extract":{"minWords":0}}}""");
        var source=(JsonObject)(await host.SendAsync("POST","/ext/gemini/sources/load","alice",new JsonObject { ["path"]=manifest,["filestoreId"]=store }))!;var id=source.GetLong("id");
        var previews=await Task.WhenAll(Enumerable.Range(0,3).Select(_=>host.SendAsync("POST","/ext/gemini/sources/"+id+"/run","alice",new JsonObject { ["dryRun"]=true })));
        Assert.That(previews.Cast<JsonObject>().All(x=>x.GetInt("added")==1),Is.True);Assert.That(db.QueryAllDocuments(store,"alice"),Is.Empty);Assert.That(calls,Is.Zero);
        var allowed=Path.Combine(host.DirectoryPath,"other");Directory.CreateDirectory(allowed);host.Feature.SetAllowedDirectories([allowed],"alice");
        Assert.ThrowsAsync<UnauthorizedAccessException>(async()=>await host.SendAsync("POST","/ext/gemini/sources/"+id+"/run","alice",new JsonObject { ["dryRun"]=false }));Assert.That(calls,Is.Zero);
        using var roleHost=new AiChatMigrationTestHost();roleHost.Feature.Variables["GEMINI_WRITE_ROLE"]="Importer";var roleDb=Install(roleHost);var roleStore=Store(roleDb);
        Assert.ThrowsAsync<HttpError>(async()=>await roleHost.SendAsync("POST","/ext/gemini/sources/load","alice",new JsonObject { ["path"]=manifest,["filestoreId"]=roleStore }));
    }
    [Test]
    public async Task Failed_upstream_deletion_retains_remote_name_and_is_retried_on_the_next_source_run()
    {
        using var host=new AiChatMigrationTestHost();var fail=true;var deletes=0;
        var db=Install(host,(request,_)=>{if(request.Method==HttpMethod.Delete){deletes++;return Task.FromResult(fail?Response("{\"error\":{\"message\":\"Provider timed out\"}}",HttpStatusCode.InternalServerError):Response("",HttpStatusCode.Gone));}return Task.FromResult(Response());});var store=Store(db);var root=Path.Combine(host.DirectoryPath,"source");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"keep.md"),"A guide with content that remains in the import.");var manifest=Path.Combine(root,"import.json");File.WriteAllText(manifest,"""{"source":{"name":"Docs","type":"folder","config":{"path":".","include":["*.md"]},"extract":{"minWords":0}}}""");
        var source=(JsonObject)(await host.SendAsync("POST","/ext/gemini/sources/load","alice",new JsonObject { ["path"]=manifest,["filestoreId"]=store }))!;var id=source.GetLong("id")!.Value;
        var removed=Doc(db,store,"gone.md");removed.SourceId=id;removed.SourceManifestPath=manifest;db.UpdateDocument(removed);await host.Feature.RunAsyncShutdownHandlers();
        var first=(JsonObject)(await host.SendAsync("POST","/ext/gemini/sources/"+id+"/run","alice",new JsonObject { ["dryRun"]=false }))!;Assert.That(first.GetArray("deleteErrors")!.Count,Is.EqualTo(1));Assert.That(db.GetDocument(removed.Id,"alice")!.Name,Is.EqualTo(removed.Name));Assert.That(db.GetDocument(removed.Id,"alice")!.TombstonedAt,Is.Null);
        fail=false;var second=(JsonObject)(await host.SendAsync("POST","/ext/gemini/sources/"+id+"/run","alice",new JsonObject { ["dryRun"]=false }))!;Assert.That(second.GetInt("removedApplied"),Is.EqualTo(1));Assert.That(db.GetDocument(removed.Id,"alice")!.TombstonedAt,Is.Not.Null);Assert.That(deletes,Is.EqualTo(2));
    }

}
