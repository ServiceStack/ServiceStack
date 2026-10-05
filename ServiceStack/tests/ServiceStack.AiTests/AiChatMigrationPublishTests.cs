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
using Microsoft.AspNetCore.Hosting;
using ServiceStack.AI;

namespace ServiceStack.AiTests;

public class AiChatMigrationPublishTests
{
    static JsonObject Config(string? key="fixture")=>new() { ["baseUrl"]="https://publisher.example",["apiKey"]=key,["userName"]="Publisher",["userId"]="publisher" };
    static HttpResponseMessage Response(string json="{}",int status=200,string type="application/json")
    {
        var result=new HttpResponseMessage((HttpStatusCode)status){Content=new StringContent(json)};result.Content.Headers.ContentType=new(type);return result;
    }
    static Func<HttpMessageHandler> Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send)=>()=>new AiChatMigrationHttpHandler(async(request,token)=>{var response=await send(request,token);response.RequestMessage??=request;return response;});
    [TestCase("")] [TestCase("/chat")]
    public async Task Named_accounts_inherit_only_public_defaults_and_connect_disconnect_preserve_others(string prefix)
    {
        using var host=new AiChatMigrationTestHost(prefix);var extension=host.Install(new ShareLlmspyExtension {Enabled=true});var configuration=new PublisherConfiguration(extension.Ctx);
        configuration.Save(null,new JsonObject { ["baseUrl"]="http://127.0.0.1:5000",["allowHttp"]=true,["apiKey"]="default-key",["userId"]="default-owner",["userName"]="Default" });
        Assert.That(host.Feature.PublisherApi.Available,Is.True);var alice=extension.GetConfiguration("alice");Assert.That(alice.GetString("apiKey"),Is.Null);Assert.That(alice.GetString("userName"),Is.Null);Assert.That(alice.GetString("userId"),Is.Null);Assert.That(alice.GetString("baseUrl"),Is.EqualTo("http://127.0.0.1:5000"));
        foreach(var user in new[]{"alice","bob"})await host.SendAsync("POST",prefix+"/ext/share_llmspy/config.json",user,new JsonObject { ["apiKey"]=user+"-key",["userName"]=user,["userId"]=user+"-owner" });
        var shown=(JsonObject)(await host.SendAsync("GET",prefix+"/ext/share_llmspy/config.json","alice"))!;Assert.That(shown.GetString("apiKey"),Is.EqualTo(PublisherConfiguration.Mask("alice-key")));
        await host.SendAsync("POST",prefix+"/ext/share_llmspy/config.json","alice",new JsonObject { ["apiKey"]=shown.GetString("apiKey"),["userName"]="Alice renamed" });Assert.That(extension.GetConfiguration("alice").GetString("apiKey"),Is.EqualTo("alice-key"));
        await host.SendAsync("POST",prefix+"/ext/share_llmspy/disconnect","alice");Assert.That(extension.GetConfiguration("alice").GetString("apiKey"),Is.Null);Assert.That(extension.GetConfiguration("bob").GetString("apiKey"),Is.EqualTo("bob-key"));Assert.That(configuration.Get(null,false).GetString("apiKey"),Is.EqualTo("default-key"));
        extension.Disabled=true;Assert.That(host.Feature.PublisherApi.Available,Is.False);
    }
    [Test]
    public void Legacy_publisher_grants_migrate_on_save_and_disconnect_cannot_restore_them()
    {
        using var host=new AiChatMigrationTestHost();
        var extension=host.Install(new ShareLlmspyExtension());
        var legacy=Path.Combine(extension.Ctx.GetUserPath("alice"),"publish","config.json");
        Directory.CreateDirectory(Path.GetDirectoryName(legacy)!);
        File.WriteAllText(legacy,"{\"apiKey\":\"legacy-key\",\"userName\":\"Alice\"}");
        var store=new PublisherConfiguration(extension.Ctx);
        Assert.That(store.Get("alice",false).GetString("apiKey"),Is.EqualTo("legacy-key"));
        store.Save("alice",new JsonObject { ["userName"]="Alice renamed" });
        Assert.That(File.Exists(legacy),Is.False);
        Assert.That(File.Exists(Path.Combine(extension.Ctx.GetUserPath("alice"),"share_llmspy","config.json")),Is.True);
        Assert.That(store.Get("alice",false).GetString("apiKey"),Is.EqualTo("legacy-key"));
        Directory.CreateDirectory(Path.GetDirectoryName(legacy)!);
        File.WriteAllText(legacy,"{\"apiKey\":\"old-key\"}");
        store.Disconnect("alice");
        Assert.That(File.Exists(legacy),Is.False);
        Assert.That(store.Get("alice",false).GetString("apiKey"),Is.Null);
    }

    [TestCase("x"),TestCase("abc"),TestCase("abcdef"),TestCase("long-secret-key")]
    public void Short_keys_are_always_obscured_and_late_avatar_receipts_do_not_restore_disconnected_grants(string key)
    {
        using var host=new AiChatMigrationTestHost();var extension=host.Install(new ShareLlmspyExtension {Enabled=true});var store=new PublisherConfiguration(extension.Ctx);store.Save("alice",new JsonObject { ["apiKey"]=key });Assert.That(store.Get("alice").GetString("apiKey"),Is.Not.EqualTo(key));var captured=store.Get("alice",false);store.Disconnect("alice");Assert.That(store.SaveAvatar("alice",captured,"default","https://publisher.example/avatar"),Is.False);Assert.That(store.Get("alice",false).GetString("apiKey"),Is.Null);
    }
    [TestCase("http://publisher.example"),TestCase("https://user:secret@publisher.example"),TestCase("https://publisher.example/path"),TestCase("https://publisher.example/path/.."),TestCase("https://publisher.example?q=x"),TestCase("https://publisher.example#fragment"),TestCase("file:///tmp/publish")]
    public void Invalid_origin_or_registration_is_rejected_before_credentials_are_saved(string value)
    {
        using var host=new AiChatMigrationTestHost();var extension=host.Install(new ShareLlmspyExtension {Enabled=true});var store=new PublisherConfiguration(extension.Ctx);
        Assert.Throws<HttpError>(()=>store.Save("alice",new JsonObject { ["baseUrl"]=value,["apiKey"]="new-key" }));Assert.That(store.Get("alice",false).GetString("apiKey"),Is.Null);
        Assert.Throws<HttpError>(()=>store.Save("alice",new JsonObject { ["registerUrl"]="https://another.example/register",["apiKey"]="new-key" }));Assert.That(store.Get("alice",false).GetString("apiKey"),Is.Null);
    }
    [TestCase(401,401),TestCase(403,401),TestCase(404,404),TestCase(409,409),TestCase(413,413),TestCase(429,429),TestCase(500,502),TestCase(302,502)]
    public void Publisher_errors_are_bounded_actionable_and_redirects_do_not_trigger_another_request(int status,int expected)
    {
        var calls=0;var client=new PublisherClient(Config(),Handler((_,_)=>{calls++;return Task.FromResult(Response("{}",status));}));
        var error=Assert.ThrowsAsync<HttpError>(async()=>await client.SendAsync(HttpMethod.Post,"/publish/decision",new JsonObject(),true));Assert.That((int)error!.StatusCode,Is.EqualTo(expected));Assert.That(calls,Is.EqualTo(1));Assert.That(error.Message,Does.Not.Contain("fixture"));
    }
    [Test]
    public async Task Client_attaches_credentials_only_for_authenticated_same_origin_calls_and_validates_references()
    {
        var headers=new List<string?>();var client=new PublisherClient(Config(),Handler(async(request,token)=>{headers.Add(request.Headers.Authorization?.Parameter);if(request.Content!=null)Assert.That(await request.Content.ReadAsStringAsync(token),Does.Contain("example"));return Response();}));
        await client.SendAsync(HttpMethod.Get,"/publish/decisions?q=guide",null,false);await client.SendAsync(HttpMethod.Post,"/publish/decision",new JsonObject { ["content"]="example" },true);Assert.That(headers,Is.EqualTo(new string?[]{null,"fixture"}));
        foreach(var path in new[]{"https://another.example/upload","//another.example/upload","/../upload","/%2e%2e/upload","/path\\upload"})Assert.ThrowsAsync<HttpError>(async()=>await client.SendAsync(HttpMethod.Get,path,null,true));Assert.That(headers.Count,Is.EqualTo(2));
        foreach(var suffix in new[]{"",".json","/recipe.json"})Assert.That(PublisherClient.ReferenceFromUrl(Config(),"https://publisher.example/d/abc_123"+suffix),Is.EqualTo("abc_123"));
        foreach(var url in new[]{"https://another.example/d/abc","https://publisher.example/d/abc?key=x","https://publisher.example/d/../abc"})Assert.Throws<HttpError>(()=>PublisherClient.ReferenceFromUrl(Config(),url));
        Assert.ThrowsAsync<HttpError>(async()=>await new PublisherClient(Config(null)).SendAsync(HttpMethod.Get,"/publish/decisions",null,true));
    }
    sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek=>false;
    }
    [Test]
    public void Client_counts_chunked_bytes_and_rejects_non_json_invalid_json_nonfinite_and_oversized_payloads()
    {
        var cases=new[]{("text/html","<html></html>"),("application/json","{\"value\":1e999}"),("application/json","{broken"),("application/json","{\"value\":NaN}"),("application/json","["+new string(' ',PublisherClient.MaxJsonBytes)+"]")};
        foreach(var (type,body) in cases) {
            var client=new PublisherClient(Config(),Handler((_,_)=>{var response=new HttpResponseMessage(HttpStatusCode.OK){Content=new StreamContent(new NonSeekableStream(Encoding.UTF8.GetBytes(body)))};response.Content.Headers.ContentType=new(type);return Task.FromResult(response);}));
            Assert.That((int)Assert.ThrowsAsync<HttpError>(async()=>await client.SendAsync(HttpMethod.Get,"/publish/decisions",null,false))!.StatusCode,Is.EqualTo(502));
        }
        var sent=0;var bounded=new PublisherClient(Config(),Handler((_,_)=>{sent++;return Task.FromResult(Response());}));
        Assert.That((int)Assert.ThrowsAsync<HttpError>(async()=>await bounded.SendAsync(HttpMethod.Post,"/publish/decision",new JsonObject { ["content"]=new string('x',PublisherClient.MaxJsonBytes) },true))!.StatusCode,Is.EqualTo(413));Assert.That(sent,Is.Zero);
        Assert.That((int)Assert.ThrowsAsync<HttpError>(async()=>await bounded.SendAsync(HttpMethod.Post,"/publish/decision",new JsonObject { ["number"]=double.NaN },true))!.StatusCode,Is.EqualTo(400));
        // Thread/project/media uploads are not recipe publications: no 3 MB request cap, longer bound
        Assert.DoesNotThrowAsync(async()=>await bounded.SendAsync(HttpMethod.Post,"/publish/thread",new JsonObject { ["content"]=new string('x',PublisherClient.MaxJsonBytes) },true,upload:true));Assert.That(sent,Is.EqualTo(1));
        Assert.That(bounded.UploadTimeout,Is.GreaterThan(bounded.TotalTimeout));
    }
    [Test]
    public async Task Total_timeout_is_retryable_and_caller_cancellation_remains_cancellation()
    {
        var client=new PublisherClient(Config(),Handler(async(_,token)=>{await Task.Delay(TimeSpan.FromSeconds(5),token);return Response();})){TotalTimeout=TimeSpan.FromMilliseconds(25)};
        Assert.That((int)Assert.ThrowsAsync<HttpError>(async()=>await client.SendAsync(HttpMethod.Get,"/publish/decisions",null,false))!.StatusCode,Is.EqualTo(502));using var cancellation=new CancellationTokenSource();cancellation.Cancel();Assert.CatchAsync<OperationCanceledException>(async()=>await client.SendAsync(HttpMethod.Get,"/publish/decisions",null,false,cancellation.Token));await Task.CompletedTask;
    }
    sealed class Threads : IThreadApi
    {
        public JsonObject? LastWrite;
        public JsonObject? GetThread(long threadId,string? user)=>user=="alice"?new JsonObject { ["id"]=threadId,["title"]="Owned thread",["metadata"]=new JsonObject { ["profile"]="default" },["messages"]=new JsonArray(new JsonObject { ["role"]="user",["content"]="Preserved" }) }:null;
        public Task UpdateThreadAsync(long threadId,JsonObject thread,string? user=null){Assert.That(user,Is.EqualTo("alice"));LastWrite=thread.Clone();return Task.CompletedTask;}
        public JsonObject? GetRequest(string requestId,string? user)=>null;
    }
    [Test]
    public async Task Existing_thread_publication_uses_own_grant_and_does_not_write_metadata_after_provider_failure()
    {
        using var host=new AiChatMigrationTestHost();var fail=false;var seen=new List<string?>();var threads=new Threads();host.Feature.ThreadApi=threads;
        var extension=host.Install(new ShareLlmspyExtension {Enabled=true,HttpHandlerFactory=Handler(async(request,token)=>{seen.Add(request.Headers.Authorization?.Parameter);Assert.That(await request.Content!.ReadAsStringAsync(token),Does.Contain("Preserved"));return Response("{\"publishedUrl\":\"https://publisher.example/chat/1\"}",fail?500:200);})});
        await host.SendAsync("POST","/ext/share_llmspy/config.json","alice",new JsonObject { ["apiKey"]="alice-key" });
        await host.SendAsync("POST","/ext/share_llmspy/thread/1","alice");Assert.That(threads.LastWrite.GetString("publishedUrl"),Is.EqualTo("https://publisher.example/chat/1"));Assert.That(seen.Single(),Is.EqualTo("alice-key"));
        threads.LastWrite=null;fail=true;Assert.ThrowsAsync<HttpError>(async()=>await host.SendAsync("POST","/ext/share_llmspy/thread/1","alice"));Assert.That(threads.LastWrite,Is.Null);
    }
    sealed class Media : IMediaApi
    {
        public JsonObject? LastWrite;
        public List<JsonObject> QueryMedia(JsonObject query,string? user=null)=>user=="alice"?[new JsonObject { ["id"]=1,["url"]="/~cache/aa/fixture.png",["type"]="image" }]:[];
        public Task UpdateMediaAsync(long id,JsonObject media,string? user=null){Assert.That(user,Is.EqualTo("alice"));LastWrite=media.Clone();return Task.CompletedTask;}
    }
    [TestCase(""),TestCase("/chat")]
    public async Task Existing_project_and_media_use_the_originating_account_and_preserve_concurrent_project_edits(string prefix)
    {
        using var host=new AiChatMigrationTestHost(prefix);host.Install(new ProjectsExtension());var media=new Media();host.Feature.MediaApi=media;var fail=false;var calls=new List<string>();
        var extension=host.Install(new ShareLlmspyExtension { Enabled=true,HttpHandlerFactory=Handler(async(request,token)=>{
            Assert.That(request.Headers.Authorization!.Parameter,Is.EqualTo("alice-key"));calls.Add(request.RequestUri!.AbsolutePath);
            Assert.That(await request.Content!.ReadAsStringAsync(token),Does.Contain("Content-Disposition"));
            if(request.RequestUri.AbsolutePath.StartsWith("/publish/project")) {
                var current=host.Feature.ProjectsApi.GetUserProjects("alice").Single().Clone();current["description"]="Edited during upload";
                await host.SendAsync("POST",prefix+"/ext/projects/save/Site","alice",current);
            }
            return Response("{\"publishedUrl\":\"https://publisher.example/shared\"}",fail?500:200);
        })});
        await host.SendAsync("POST",prefix+"/ext/share_llmspy/config.json","alice",new JsonObject { ["apiKey"]="alice-key" });
        await host.SendAsync("POST",prefix+"/ext/projects/save/Site","alice",new JsonObject { ["name"]="Site",["folder"]="site",["publish"]="",["description"]="Before upload" });
        var project=host.Feature.ProjectsApi.GetUserProjects("alice").Single();var dir=ProjectsExtension.GetProjectDir(extension.Ctx.GetUserPath("alice"),project);Directory.CreateDirectory(dir);await File.WriteAllTextAsync(Path.Combine(dir,"index.html"),"Example site");
        await host.SendAsync("POST",prefix+"/ext/share_llmspy/project/Site","alice");var saved=host.Feature.ProjectsApi.GetUserProjects("alice").Single();Assert.That(saved.GetString("description"),Is.EqualTo("Edited during upload"));Assert.That(saved.GetString("publishedUrl"),Is.EqualTo("https://publisher.example/shared"));
        var cache=extension.Ctx.GetCachePath("aa/fixture.png");Directory.CreateDirectory(Path.GetDirectoryName(cache)!);await File.WriteAllBytesAsync(cache,[1,2,3]);
        await host.SendAsync("POST",prefix+"/ext/share_llmspy/media/1","alice");Assert.That(media.LastWrite.GetString("publishedUrl"),Is.EqualTo("https://publisher.example/shared"));
        media.LastWrite=null;fail=true;Assert.ThrowsAsync<HttpError>(async()=>await host.SendAsync("POST",prefix+"/ext/share_llmspy/media/1","alice"));Assert.That(media.LastWrite,Is.Null);Assert.That(calls,Does.Contain("/publish/project/Site"));Assert.That(calls,Does.Contain("/publish/media"));
    }
    [Test]
    public async Task Default_socket_transport_does_not_follow_real_redirects_and_bounds_stalled_response_reads()
    {
        var builder=Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app=builder.Build();var redirected=0;var credentials=new List<string>();
        Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGet(app,"/redirect",async context=>{credentials.Add(context.Request.Headers.Authorization.ToString());context.Response.StatusCode=302;context.Response.Headers.Location="/destination";await context.Response.CompleteAsync();});
        Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGet(app,"/destination",context=>{redirected++;return Task.CompletedTask;});
        Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGet(app,"/slow",async context=>{context.Response.ContentType="application/json";await Microsoft.AspNetCore.Http.HttpResponseWritingExtensions.WriteAsync(context.Response,"{",context.RequestAborted);await context.Response.Body.FlushAsync(context.RequestAborted);try{await Task.Delay(10000,context.RequestAborted);}catch(OperationCanceledException){} });
        await app.StartAsync();try {
            var config=Config();config["baseUrl"]=app.Urls.Single();config["allowHttp"]=true;var client=new PublisherClient(config){ReadTimeout=TimeSpan.FromMilliseconds(75)};
            Assert.That((int)Assert.ThrowsAsync<HttpError>(async()=>await client.SendAsync(HttpMethod.Get,"/redirect",null,true))!.StatusCode,Is.EqualTo(502));Assert.That(redirected,Is.Zero);Assert.That(credentials,Is.EqualTo(new[]{"Bearer fixture"}));
            var clock=System.Diagnostics.Stopwatch.StartNew();Assert.That((int)Assert.ThrowsAsync<HttpError>(async()=>await client.SendAsync(HttpMethod.Get,"/slow",null,false))!.StatusCode,Is.EqualTo(502));Assert.That(clock.Elapsed,Is.LessThan(TimeSpan.FromSeconds(3)));
        }finally{await app.StopAsync();}
    }
}
