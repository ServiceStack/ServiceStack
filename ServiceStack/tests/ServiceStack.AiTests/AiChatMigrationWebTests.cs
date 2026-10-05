#nullable enable
using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack.AiTests;

/// <summary>Real ServiceStack route/auth/serialization checks, in addition to handler fixtures.</summary>
[NonParallelizable]
public class AiChatMigrationWebTests
{
    class AppHost() : AppHostBase(nameof(AiChatMigrationWebTests), typeof(ChatFeature).Assembly)
    {
        public override void Configure() { }
    }
    sealed class HeaderAuth : IChatAuth
    {
        public string? FallbackUser;
        public bool IsEnabled => true;
        public string? GetUserName(IRequest req) => req.Headers["X-Migration-User"] ?? FallbackUser;
        public string? AssertUserName(IRequest req) => GetUserName(req) ?? throw new UnauthorizedAccessException();
        public (bool, JsonObject?) CheckAuth(IRequest req) => (GetUserName(req) != null, null);
        public Task<JsonObject?> GetAuthInfoAsync(IRequest req) => Task.FromResult<JsonObject?>(null);
        public Task SignOutAsync(IRequest req) => Task.CompletedTask;
        public bool IsAdmin(IRequest req) => false;
    }
    static async Task<HttpResponseMessage> Send(HttpClient client, string method, string path, string? user, JsonNode? body = null)
    {
        using var req = new HttpRequestMessage(new HttpMethod(method), path);
        if (user != null) req.Headers.Add("X-Migration-User", user);
        if (body != null) req.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        return await client.SendAsync(req);
    }

    static string BrowserFixture(string prefix) => """
<!doctype html><html><head><meta charset="utf-8"><link rel="stylesheet" href="PREFIX/ui/app.css">
<script type="importmap">{"imports":{"vue":"PREFIX/ui/lib/vue.min.mjs"}}</script></head>
<body><div id="app"></div><pre id="result">RUNNING</pre><script type="module">
import {createApp,reactive,ref,nextTick} from 'vue'
import extension from 'PREFIX/ext/projects/index.mjs'
const components={}, shown=ref(true)
const request=async(method,url,body,options={})=>{
 const response=await fetch('PREFIX/ext/projects'+url,{method,signal:options.signal,headers:body?{'Content-Type':'application/json'}:{},body:body?JSON.stringify(body):undefined})
 const json=await response.json()
 if(!response.ok)return {error:json.responseStatus||{message:'Request failed'}}
 return {response:json}
}
const ctx={state:reactive({projects:[],prefs:{}}),ai:{auth:{userName:'alice'}},
 chat:{drafts:{state:reactive({key:'local:origin'}),list:()=>[],fresh:()=>{}}},threads:{clearCurrentThread(){}},
 utils:{toKebabCase:s=>s.trim().toLowerCase().replace(/[^\w\s-]/g,'').replace(/[\s_]+/g,'-')},
 projectCreationRequest:{startNew:true,onCreated:async()=>{}},
 components:c=>Object.assign(components,c),modals(){},toast(){},to(){},openModal(){},
 setState:s=>Object.assign(ctx.state,s),setGlobals:s=>Object.assign(ctx,s),scope:()=>({ctx,
 getJson:(url,options)=>request('GET',url,null,options),postJson:(url,body)=>request('POST',url,body)})}
extension.install(ctx)
const app=createApp({components:{Manager:components.ProjectsManagerModal},setup:()=>({shown}),template:'<Manager v-if="shown" @done="shown=false"/>'})
app.provide('ctx',ctx);Object.assign(app.config.globalProperties,{$styles:{},$state:ctx.state});app.mount('#app')
const wait=()=>new Promise(resolve=>setTimeout(resolve,50))
try{
 for(let i=0;i<100&&!document.getElementById('project-create-name');i++)await wait()
 const name=document.getElementById('project-create-name');if(!name)throw new Error('Creation form did not mount')
 if(document.querySelector('[data-project-create-sources]'))throw new Error('Disabled Git advertised by real host')
 name.value='Browser project';name.dispatchEvent(new Event('input',{bubbles:true}));await nextTick();await wait()
 const button=[...document.querySelectorAll('button')].find(b=>b.textContent.trim()==='Create project')
 if(!button||button.disabled)throw new Error('Creation form cannot submit')
 button.click()
 for(let i=0;i<100&&!ctx.state.projects.some(p=>p.name==='Browser project');i++)await wait()
 const saved=await request('GET','/projects.json')
 if(!saved.response.some(p=>p.name==='Browser project'))throw new Error('Browser creation was not persisted by C#')
 const project=saved.response.find(p=>p.name==='Browser project')
 const workspace=await request('GET','/explorer?projectId='+encodeURIComponent(project.id))
 if(workspace.error||!workspace.response.roots.length)throw new Error('Created workspace unavailable')
 document.getElementById('result').textContent='PASS: Real C# project creation and explorer through verbatim UI'
}catch(error){document.getElementById('result').textContent='FAIL: '+error.message}
</script></body></html>
""".Replace("PREFIX", prefix);

    static async Task VerifyBrowserAsync(Uri uri, string data)
    {
        var info = new ProcessStartInfo("chromium") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "--headless", "--no-sandbox", "--disable-gpu", "--user-data-dir=" + Path.Combine(data, "chromium"), "--virtual-time-budget=15000", "--dump-dom", uri.ToString() }) info.ArgumentList.Add(arg);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Chromium could not start");
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30)); }
        catch { process.Kill(entireProcessTree: true); throw; }
        var html = await output;
        Assert.That(html, Does.Contain("<pre id=\"result\">PASS: Real C# project creation and explorer"), html + "\n" + await errors);
    }

    [TestCase("")]
    [TestCase("/chat")]
    public async Task Real_host_serves_owned_project_creation_explorer_archive_and_spa(string prefix)
    {
        var data = Path.Combine(Path.GetTempPath(), "ai-chat-http-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(data);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());
        builder.Services.AddSingleton<ServiceStack.Data.IDbConnectionFactory>(new OrmLiteConnectionFactory(Path.Combine(data, "test.sqlite"), SqliteDialect.Provider));
        var auth = new HeaderAuth();
        var feature = new ChatFeature { RoutePrefix = prefix, AppDataPath = data, ChatAuth = auth,
            EnableProviders = ["migration-none"], Config = ChatJson.ParseObject("""{"defaults":{"summarize":null},"providers":{}}"""),
            DisableExtensions = ["git", "gemini", "gallery", "analytics", "voice"] };
        feature.Routes.AddGet("/migration/project-browser", _ => Task.FromResult<object?>(ChatResult.Html(BrowserFixture(prefix))));
        builder.Services.AddPlugin(feature);
        await using var app = builder.Build();
        app.UseServiceStack(new AppHost(), options => options.MapEndpoints());
        try
        {
            await app.StartAsync();
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            using (var denied = await Send(client, "GET", prefix + "/ext/projects/creation/options", null))
                Assert.That(denied.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            var create = new JsonObject { ["requestId"] = Guid.NewGuid().ToString(), ["project"] = new JsonObject { ["name"] = "HTTP Project", ["folder"] = "http-project" }, ["source"] = new JsonObject { ["kind"] = "new" } };
            using var accepted = await Send(client, "POST", prefix + "/ext/projects/create", "alice", create);
            Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            var operation = ChatJson.ParseObject(await accepted.Content.ReadAsStringAsync());
            var id = operation.GetString("id")!;
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (operation.GetString("state") != "succeeded" && DateTime.UtcNow < deadline)
            {
                await Task.Delay(25);
                using var poll = await Send(client, "GET", prefix + "/ext/projects/creation/operations/" + id, "alice");
                operation = ChatJson.ParseObject(await poll.Content.ReadAsStringAsync());
            }
            Assert.That(operation.GetString("state"), Is.EqualTo("succeeded"), operation.ToJsonString());
            using (var deniedOwner = await Send(client, "GET", prefix + "/ext/projects/creation/operations/" + id, "bob"))
                Assert.That(deniedOwner.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            var projectId = operation.GetObject("result").GetObject("project").GetString("id")!;
            using var explorer = await Send(client, "GET", prefix + "/ext/projects/explorer?projectId=" + projectId, "alice");
            Assert.That(explorer.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(ChatJson.ParseObject(await explorer.Content.ReadAsStringAsync())["entries"]!.AsArray(), Is.Empty);
            using var archive = await Send(client, "PATCH", prefix + "/ext/projects/archive/" + projectId, "alice", new JsonObject { ["archived"] = true });
            Assert.That(archive.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var stale = await Send(client, "POST", prefix + "/ext/projects/order", "alice", new JsonObject { ["ids"] = new JsonArray(projectId) });
            Assert.That(stale.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            var error = ChatJson.ParseObject(await stale.Content.ReadAsStringAsync());
            Assert.That(error.GetObject("responseStatus"), Is.Not.Null);
            using var spa = await client.GetAsync(prefix + "/chat/migration-direct-navigation");
            Assert.That(spa.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await spa.Content.ReadAsStringAsync(), Does.Contain("/ui/index.mjs"));
            using (var asset = await client.GetAsync(prefix + "/ui/ai.mjs"))
                Assert.That(asset.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            if (prefix.Length == 0)
            {
                // Unprefixed Chat claims only its own /ui files; other /ui paths reach ServiceStack's API Explorer
                using var explorerUi = await client.GetAsync("/ui/MigrationNotAChatAsset");
                Assert.That(await explorerUi.Content.ReadAsStringAsync(), Does.Not.Contain("/ui/index.mjs"));
            }
            auth.FallbackUser = "alice";
            await VerifyBrowserAsync(new Uri(client.BaseAddress!, prefix + "/migration/project-browser"), data);
        }
        finally
        {
            await app.StopAsync();
            // Dispose the web host before deleting SQLite files on Windows.
            await app.DisposeAsync();
            if (Directory.Exists(data)) Directory.Delete(data, true);
        }
    }
}
