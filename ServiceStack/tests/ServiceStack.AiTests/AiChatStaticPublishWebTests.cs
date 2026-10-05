#nullable enable
using System.Diagnostics;
using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.OrmLite;

namespace ServiceStack.AiTests;

[NonParallelizable]
public class AiChatStaticPublishWebTests
{
    sealed class AppHost() : AppHostBase(nameof(AiChatStaticPublishWebTests), typeof(ChatFeature).Assembly)
    {
        public override void Configure() { }
    }

    [TestCase(""), TestCase("/chat")]
    public async Task Default_sharing_registers_static_configuration_and_UI_without_remote_routes(string prefix)
    {
        var data = Path.Combine(Path.GetTempPath(), "ai-chat-share-defaults-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(data);
        var webroot = Path.Combine(data, "wwwroot");
        Directory.CreateDirectory(webroot);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = webroot });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());
        builder.Services.AddSingleton<ServiceStack.Data.IDbConnectionFactory>(new OrmLiteConnectionFactory(Path.Combine(data, "chat.sqlite"), SqliteDialect.Provider));
        var feature = new ChatFeature {
            RoutePrefix = prefix, AppDataPath = Path.Combine(data, "app"), RequireAuth = false, EnableProviders = ["migration-none"],
            Config = new JsonObject { ["defaults"] = new JsonObject { ["summarize"] = null }, ["providers"] = new JsonObject() },
            DisableExtensions = ["git", "gemini", "gallery", "analytics", "voice"],
        };
        Assert.That(feature.ShareStatic.Enabled, Is.True);
        Assert.That(feature.ShareLlmspy.Enabled, Is.False);
        feature.Routes.AddGet("/migration/static-browser", _ => Task.FromResult<object?>(ChatResult.Html(Fixture(prefix, feature.ProjectsApi.GetUserProjects(null).Single(), true, false))));
        builder.Services.AddPlugin(feature);
        await using var app = builder.Build();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting(); // Static exports must run before Chat endpoint selection.
        app.UseServiceStack(new AppHost(), options => options.MapEndpoints());
        try {
            await app.StartAsync();
            using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            using var response = await http.GetAsync(prefix + "/ext/share_static/config.json");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var config = ChatJson.ParseObject(await response.Content.ReadAsStringAsync());
            Assert.That(config.GetBool("enabled"), Is.True);
            Assert.That(config.GetString("directory"), Is.EqualTo(Path.Combine(webroot, "p")));
            Assert.That(config.GetString("basePath"), Is.EqualTo("/p/"));
            Assert.That(config.GetString("baseUrl"), Is.EqualTo(""));
            Assert.That((await http.GetAsync(prefix + "/ext/share_static/index.mjs")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That((await http.GetAsync(prefix + "/ext/share_llmspy/config.json")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(feature.InstalledExtensionNames, Does.Contain("share_static").And.Not.Contain("share_llmspy"));
            Assert.That(feature.UiExtensions.OfType<JsonObject>().Select(x => x.GetString("id")), Does.Contain("share_static").And.Not.Contain("share_llmspy"));
            Assert.That(feature.PublisherApi.Available, Is.False);
            var servedAi = await http.GetStringAsync(prefix + "/ui/ai.mjs");
            Assert.That(servedAi, Does.Contain("const staticPublishUseCurrentOrigin = true"));
            using var saved = await http.PostAsync(prefix + "/ext/projects/save/Site", new StringContent("{\"name\":\"Site\",\"folder\":\"site\",\"publish\":\"missing-build\"}", System.Text.Encoding.UTF8, "application/json"));
            Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var project = feature.ProjectsApi.GetUserProjects(null).Single();
            var source = Path.Combine(ProjectsExtension.GetProjectDir(feature.AppData.GetUserPath(null), project), "dist");
            Directory.CreateDirectory(source);
            await File.WriteAllTextAsync(Path.Combine(source, "index.html"), "<html><head></head><body>Default webroot export</body></html>");
            var info = new ProcessStartInfo("chromium") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { "--headless", "--no-sandbox", "--disable-gpu", "--user-data-dir=" + Path.Combine(data, "chromium"), "--virtual-time-budget=15000", "--dump-dom", new Uri(http.BaseAddress, prefix + "/migration/static-browser").ToString() }) info.ArgumentList.Add(argument);
            using var process = Process.Start(info)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30)); } catch { process.Kill(true); throw; }
            Assert.That(await output, Does.Contain("<pre id=\"result\">PASS: C# static publishing UI"), await output + "\n" + await errors);
            var publication = feature.ProjectsApi.GetUserProjects(null).Single().GetObject("staticPublication");
            Assert.That(publication.GetString("publishedPath"), Is.EqualTo(Path.Combine(webroot, "p", "default", "site")));
            Assert.That(await http.GetStringAsync("/p/default/site/"), Does.Contain("<base href=\"/p/default/site/\">").And.Contain("Default webroot export"));
        } finally {
            await app.StopAsync();
            Directory.Delete(data, true);
        }
    }

    static string Fixture(string prefix, JsonObject project, bool staticEnabled, bool remoteEnabled) => """
<!doctype html><html><head><meta charset="utf-8"><script type="importmap">{"imports":{"vue":"PREFIX/ui/lib/vue.mjs","@servicestack/client":"PREFIX/ui/lib/servicestack-client.mjs"}}</script></head><body><div id="app"></div><pre id="result">RUNNING</pre><script type="module">
import {createApp,reactive,ref,nextTick} from 'vue'
import {AppContext} from 'PREFIX/ui/ctx.mjs'
import {o as ai} from 'PREFIX/ui/ai.mjs'
import SharePanel from 'PREFIX/ui/modules/SharePanel.mjs'
window.requestAnimationFrame=callback=>setTimeout(()=>callback(performance.now()),16)
const assert=(v,m)=>{if(!v)throw Error(m)},wait=async()=>{await new Promise(r=>setTimeout(r,100));await nextTick()}
const errors=[],posts=[]
const ctx=Object.assign(Object.create(AppContext.prototype),{_components:{},top:reactive({}),shareOptions:reactive({}),layout:reactive({}),state:reactive({projects:[PROJECT_JSON],prefs:{project:'Site'}}),threads:{currentThread:ref(null)},ai:{auth:{},resolveStaticPublishUrl:ai.resolveStaticPublishUrl},utils:{toKebabCase:s=>s.toLowerCase()},gallery:{setLightboxFooters(){},setAudioActions(){}},projects:{getProject:name=>ctx.state.projects.find(p=>p.name===name),saveProject:async()=>{throw Error('Unexpected save')}}})
const scopes={}
ctx.scope=id=>scopes[id]??=({ctx,state:reactive({}),setState(v){Object.assign(this.state,v)},toast(){},setError:e=>errors.push(e),async request(method,path,body){const r=await fetch('PREFIX/ext/'+id+path,{method,headers:{'Content-Type':'application/json'},body:body?JSON.stringify(body):undefined});return ai.createJsonResult(r)},getJson(path){return this.request('GET',path)},postJson(path,body){posts.push(id+path);return this.request('POST',path,body)}})
ctx.projects.saveProject=async(name,project)=>{const result=await ctx.scope('projects').request('POST','/save/'+encodeURIComponent(name),project);if(result.response)ctx.state.projects=result.response;return result}
const staticEnabled=STATIC_ENABLED,remoteEnabled=REMOTE_ENABLED
if(staticEnabled){const extension=(await import('PREFIX/ext/share_static/index.mjs')).default;extension.install(ctx);await extension.load(ctx)}
if(remoteEnabled){const extension=(await import('PREFIX/ext/share_llmspy/index.mjs')).default;extension.install(ctx);await extension.load(ctx)}
const button=label=>[...document.querySelectorAll('button')].find(b=>b.textContent.trim()===label)
try {
 const app=createApp({components:{SharePanel},template:'<component v-for="(icon,id) in $ctx.visibleComponents($ctx.top)" :key="id" :is="icon.component" :data-top-icon="id"/><SharePanel/>'});app.provide('ctx',ctx);app.config.globalProperties.$styles=new Proxy({},{get:()=>''});app.config.globalProperties.$state=ctx.state;app.config.globalProperties.$ctx=ctx;app.config.errorHandler=e=>errors.push(e);app.component('ErrorSummary',{template:'<div></div>'});app.mount('#app');await wait()
 assert(!!ctx.top.share===(staticEnabled||remoteEnabled),'Core icon follows registered options')
 assert(!!document.querySelector('[data-top-icon=share]')===(staticEnabled||remoteEnabled),'Rendered icon follows available tabs')
 assert(document.querySelectorAll('[role=tab]').length===Number(staticEnabled)+Number(remoteEnabled),'Independent tab registration')
 if(staticEnabled){
 assert(document.querySelector('[role=tab][aria-selected=true]').textContent==='Folder','Default folder tab')
 assert(!document.querySelector('iframe'),'Folder requires no publisher account')
 button('Publish folder').click();for(let i=0;i<100&&!document.querySelector('[role=alert]');i++)await wait()
 const failure=document.querySelector('[role=alert]');assert(failure?.textContent.includes('missing-build')&&failure.textContent.includes('Build the project first'),'Real C# failure explains missing source directory');assert(failure.textContent.includes('Destination:'),'Failure includes publication context');assert(!ctx.state.error,'Publishing error stays out of chat')
 document.querySelector('[aria-label="Dismiss publishing error"]').click();await wait();assert(!document.querySelector('[role=alert]'),'Extension dismisses its error')
 const input=document.querySelector('input[type=text]');input.value='dist';input.dispatchEvent(new Event('input',{bubbles:true}));await wait()
 button('Publish folder').click();for(let i=0;i<100&&!button('Update folder');i++)await wait()
 assert(button('Update folder'),'Real C# publication succeeded')
 assert(!document.querySelector('[role=alert]'),'Successful retry clears error')
 const publication=ctx.state.projects[0].staticPublication,publishedUrl=publication.publishedUrl||ai.resolveStaticPublishUrl(publication.urlPath),link=document.querySelector('a[href="'+publishedUrl+'"]')
 assert(link?.target==='_blank'&&link.rel.includes('noopener'),'Published link opens a new window')
 const status=[...document.querySelectorAll('span[title]')].find(s=>s.textContent.startsWith('Published '))
 assert(status&&/^Published \d+[smhdwMy] ago$/.test(status.textContent),'Compact relative timestamp')
 assert(status.title===new Date(publication.publishedAt).toLocaleString(),'Full date and time tooltip')
 assert(status.parentElement.textContent.includes('to ~/default/site'),'Concise publication destination');assert(!document.querySelector('#app').textContent.includes(publication.publishedPath),'Export root hidden')
 assert(posts.length===2&&posts.every(path=>path==='share_static/project/'+ctx.state.projects[0].id+'/folder'),'Only local publish requests')
 assert(publishedUrl&&!publishedUrl.includes('/chat/p/'),'Project URL uses static mount independently of Chat route prefix')
 await fetch(publishedUrl,{mode:'no-cors'})
 }
 if(remoteEnabled){button('ai.llmspy.org').click();await wait();assert(document.querySelector('iframe'),'Remote account remains optional tab')}
 if(staticEnabled){button('Folder').click();await wait();assert(!document.querySelector('iframe'),'Return to local publishing')}
 if(staticEnabled&&!remoteEnabled){
 document.querySelector('[data-top-icon=share]').dispatchEvent(new MouseEvent('click',{bubbles:true}));await wait();assert(ctx.layout.top==='SharePanel','Share icon opens panel')
 ctx.threads.currentThread.value={id:'unassigned',projectId:null};await wait();assert(!document.querySelector('[data-top-icon=share]'),'No icon when Folder is hidden');assert(!ctx.layout.top,'Empty panel closes')
 ctx.threads.currentThread.value=null;ctx.state.prefs.project=null;await wait();assert(!document.querySelector('[data-top-icon=share]'),'No icon without selected project')
 ctx.state.prefs.project='Site';await wait();assert(document.querySelector('[data-top-icon=share]'),'Icon returns when project selected')
 }
 if(staticEnabled&&remoteEnabled){
 const assertRemoteOnly=()=>{assert(document.querySelector('[data-top-icon=share]'),'Remote tab keeps icon visible');assert(document.querySelectorAll('[role=tab]').length===1,'Folder hidden without a project');assert(document.querySelector('[role=tab][aria-selected=true]').textContent==='ai.llmspy.org','Remote selected when Folder is hidden');assert(document.querySelector('iframe'),'Remote panel shown');assert(!document.querySelector('#app').textContent.includes('Select a project or open a chat'),'No missing-project message')}
 ctx.state.prefs.project=null;await wait();assertRemoteOnly()
 ctx.state.prefs.project='Site';await wait();assert(button('Folder'),'Selected project restores Folder')
 ctx.threads.currentThread.value={id:'unassigned',projectId:null};await wait();assertRemoteOnly()
 ctx.threads.currentThread.value={id:'project-chat',projectId:ctx.state.projects[0].id};ctx.state.prefs.project=null;await wait();assert(button('Folder'),'Chat project restores Folder')
 }
 assert(errors.length===0,'No UI errors');app.unmount()
 document.getElementById('result').textContent='PASS: C# static publishing UI, optional account, project link and relative timestamp'
}catch(e){document.getElementById('result').textContent='FAIL: '+e.stack}
</script></body></html>
""".Replace("PROJECT_JSON", project.ToJsonString()).Replace("PREFIX", prefix)
        .Replace("STATIC_ENABLED",staticEnabled?"true":"false").Replace("REMOTE_ENABLED",remoteEnabled?"true":"false");

    [TestCase("",true,true),TestCase("/chat",true,true)]
    [TestCase("",true,false),TestCase("/chat",true,false)]
    [TestCase("",false,true),TestCase("/chat",false,true)]
    [TestCase("",false,false),TestCase("/chat",false,false)]
    public async Task Synced_UI_registers_independent_sharing_and_exports_survive_host_shutdown(string prefix, bool staticEnabled, bool remoteEnabled)
    {
        var data = Path.Combine(Path.GetTempPath(), "ai-chat-static-web-" + Guid.NewGuid().ToString("N"));
        var publicRoot = Path.Combine(data, "www"); Directory.CreateDirectory(publicRoot);
        var staticBuilder = WebApplication.CreateBuilder(); staticBuilder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var staticApp = staticBuilder.Build();
        using var files = new PhysicalFileProvider(publicRoot);
        staticApp.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        staticApp.UseStaticFiles(new StaticFileOptions { FileProvider = files });
        await staticApp.StartAsync();
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());
        builder.Services.AddSingleton<ServiceStack.Data.IDbConnectionFactory>(new OrmLiteConnectionFactory(Path.Combine(data, "chat.sqlite"), SqliteDialect.Provider));
        var feature = new ChatFeature {
            RoutePrefix = prefix, AppDataPath = Path.Combine(data, "app"), RequireAuth = false, EnableProviders = ["migration-none"],
            Config = new JsonObject {
                ["defaults"] = new JsonObject { ["summarize"] = null }, ["providers"] = new JsonObject(),
            },
            DisableExtensions = ["git", "gemini", "gallery", "analytics", "voice"],
        };
        if(!staticEnabled)feature.DisableExtensions.Add("share_static");
        if(!remoteEnabled)feature.DisableExtensions.Add("share_llmspy");
        feature.ShareStatic.StaticPublish=new StaticPublishConfig {Directory=Path.Combine(publicRoot,"p"),BaseUrl=staticApp.Urls.Single()+"/p"};
        var publisher = feature.ShareLlmspy;
        publisher.Enabled = remoteEnabled;
        publisher.HttpHandlerFactory = () => throw new AssertionException("Unexpected remote publisher request");
        feature.Routes.AddGet("/migration/static-browser", _ => Task.FromResult<object?>(ChatResult.Html(Fixture(prefix, feature.ProjectsApi.GetUserProjects(null).Single(),staticEnabled,remoteEnabled))));
        builder.Services.AddPlugin(feature);
        await using var app = builder.Build(); app.UseServiceStack(new AppHost(), options => options.MapEndpoints());
        try {
            await app.StartAsync();
            if(remoteEnabled)new PublisherConfiguration(publisher.Ctx).Save(null, new JsonObject { ["baseUrl"] = app.Urls.Single(), ["allowHttp"] = true });
            using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            using var saved = await http.PostAsync(prefix + "/ext/projects/save/Site", new StringContent("{\"name\":\"Site\",\"folder\":\"site\",\"publish\":\"dist\"}", System.Text.Encoding.UTF8, "application/json"));
            Assert.That(saved.StatusCode, Is.EqualTo(HttpStatusCode.OK), await saved.Content.ReadAsStringAsync());
            var project = feature.ProjectsApi.GetUserProjects(null).Single();
            var source = Path.Combine(ProjectsExtension.GetProjectDir(feature.AppData.GetUserPath(null), project), "dist");
            Directory.CreateDirectory(source);
            await File.WriteAllTextAsync(Path.Combine(source, "index.html"), "<html><head></head><body><img src='/logo.png'></body></html>");
            await File.WriteAllBytesAsync(Path.Combine(source, "logo.png"), [1, 2, 3]);
            if (staticEnabled) {
                project["publish"] = "missing-build";
                using var configured = await http.PostAsync(prefix + "/ext/projects/save/Site", new StringContent(project.ToJsonString(), System.Text.Encoding.UTF8, "application/json"));
                Assert.That(configured.StatusCode, Is.EqualTo(HttpStatusCode.OK), await configured.Content.ReadAsStringAsync());
            }
            var info = new ProcessStartInfo("chromium") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { "--headless", "--no-sandbox", "--disable-gpu", "--user-data-dir=" + Path.Combine(data, "chromium"), "--virtual-time-budget=15000", "--dump-dom", new Uri(http.BaseAddress, prefix + "/migration/static-browser").ToString() }) info.ArgumentList.Add(argument);
            using var process = Process.Start(info)!;
            var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30)); } catch { process.Kill(true); throw; }
            Assert.That(await output, Does.Contain("<pre id=\"result\">PASS: C# static publishing UI"), await output + "\n" + await errors);
            var publication = feature.ProjectsApi.GetUserProjects(null).Single().GetObject("staticPublication");
            if(staticEnabled) {
            Assert.That(publication.GetString("urlPath"), Is.EqualTo("/p/default/site/"));
            await app.StopAsync();
            using var independent = new HttpClient();
            Assert.That(await independent.GetStringAsync(publication.GetString("publishedUrl")), Does.Contain("<base href=\"/p/default/site/\">"));
            Assert.That(await independent.GetByteArrayAsync(publication.GetString("publishedUrl") + "logo.png"), Is.EqualTo(new byte[] { 1, 2, 3 }));
            } else Assert.That(publication,Is.Null);
            foreach(var (name,enabled) in new[]{("share_static",staticEnabled),("share_llmspy",remoteEnabled)})
                Assert.That(feature.InstalledExtensionNames.Contains(name),Is.EqualTo(enabled));
        }
        finally {
            await app.StopAsync(); await staticApp.StopAsync();
            if (Directory.Exists(data)) Directory.Delete(data, true);
        }
    }
}
