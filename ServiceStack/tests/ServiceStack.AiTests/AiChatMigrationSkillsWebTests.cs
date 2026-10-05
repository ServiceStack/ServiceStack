#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
[NonParallelizable]
public class AiChatMigrationSkillsWebTests
{
    sealed class AppHost():AppHostBase(nameof(AiChatMigrationSkillsWebTests),typeof(ChatFeature).Assembly){public override void Configure(){}}
    sealed class UserAuth:IChatAuth
    {
        public string? User;
        public bool IsEnabled=>true;
        public string? GetUserName(IRequest req)=>req.Headers["X-Migration-User"]??User;
        public string? AssertUserName(IRequest req)=>GetUserName(req)??throw new UnauthorizedAccessException();
        public (bool,JsonObject?) CheckAuth(IRequest req)=>(GetUserName(req)!=null,null);
        public Task<JsonObject?> GetAuthInfoAsync(IRequest req)=>Task.FromResult<JsonObject?>(null);
        public Task SignOutAsync(IRequest req)=>Task.CompletedTask;
        public bool IsAdmin(IRequest req)=>false;
    }
    static string Page(string prefix)=>"""
<!doctype html><html><head><meta charset="utf-8"><link rel="stylesheet" href="PREFIX/ui/app.css"><script type="importmap">{"imports":{"vue":"PREFIX/ui/lib/vue.mjs","vue-router":"PREFIX/ui/lib/vue-router.min.mjs","@servicestack/client":"PREFIX/ui/lib/servicestack-client.mjs","@servicestack/vue":"PREFIX/ui/lib/servicestack-vue.mjs","/ui/lazy.mjs":"PREFIX/ui/lazy.mjs","/ui/utils.mjs":"PREFIX/ui/utils.mjs","/ui/modules/explorerState.mjs":"PREFIX/ui/modules/explorerState.mjs"}}</script></head><body><div id="app"></div><pre id="result">RUNNING</pre><script>for(const event of ['error','unhandledrejection'])window.addEventListener(event,e=>document.getElementById('result').textContent='FAIL: '+(e.error?.stack||e.reason?.stack||e.message||e.reason))</script><script type="module">
import {createApp,reactive,nextTick} from 'vue'
import {createRouter,createWebHistory} from 'vue-router'
import ServiceStackVue from '@servicestack/vue'
import skills from 'PREFIX/ext/skills/index.mjs'
import {installWorkspaceNavigation} from 'PREFIX/ui/modules/explorerState.mjs'
const assert=(v,m)=>{if(!v)throw Error(m)},wait=()=>new Promise(r=>setTimeout(r,20)),until=async fn=>{for(let i=0;i<300&&!fn();i++)await wait();assert(fn(),'Timed out waiting for skills')}
const components={},state=reactive({skills:{}}),workspace=await(await fetch('PREFIX/migration/workspace')).json(),theme=(await(await fetch('PREFIX/themes')).json()).light
let releaseSlow=null,slowRequested=false
const ctx={state,prefs:{},components:v=>Object.assign(components,v),setState:v=>Object.assign(state,v),setError:e=>{throw Error(e.message)},setLeftIcons(){},setTopIcons(){},routes:[],chatRequestFilters:[],tools:{isToolEnabled:()=>true},utils:{pluralize:(s,n)=>n===1?s:s+'s'},togglePath(){}}
ctx.scope=()=>({ctx,getJson:async path=>{if(path.includes('slow.md')){slowRequested=true;await new Promise(r=>releaseSlow=r)}return send(path)},postJson:(path,body)=>send(path,body),deleteJson:path=>send(path,null,'DELETE')})
async function send(path,body,method){const r=await fetch('PREFIX/ext/skills'+(path==='/'?'':path),{method:method||(body?'POST':'GET'),headers:{'Content-Type':'application/json'},body:body?JSON.stringify(body):undefined});const value=await r.json();return r.ok?{response:value}:{error:{message:value.responseStatus?.message||'HTTP '+r.status}}}
skills.install(ctx);await skills.load(ctx);let vm=null,error=null;const router=createRouter({history:createWebHistory('PREFIX'||'/'),routes:[...ctx.routes.map(r=>r.path==='/skills'?{...r,component:{...r.component,mounted(){vm=this}}}:r),{path:'/other',component:{template:'<p>Other</p>'}}]});installWorkspaceNavigation(router)
const app=createApp({template:'<RouterView/>'});app.use(ServiceStackVue);app.use(router);for(const [name,c] of Object.entries(components))app.component(name,c);app.provide('ctx',ctx);Object.assign(app.config.globalProperties,{$ctx:ctx,$styles:theme.styles});app.config.errorHandler=e=>error=e
try{
 await router.push({path:'/skills',query:{workspace:'1',workspacePath:workspace.root,workspaceView:'files',workspaceProject:'retained'}});await router.isReady();app.mount('#app');await until(()=>vm);assert(Object.keys(state.skills).length>0,'Installed skills absent')
 vm.newSkillName='migration-skill';await vm.createSkill();assert(!vm.createError,vm.createError);await until(()=>vm.selectedSkill?.name==='migration-skill');assert(router.currentRoute.value.query.workspacePath===workspace.root,'Workspace lost after selecting skill');
 vm.newFilePath='references/source.md';await vm.addFile();assert(!vm.addFileError,vm.addFileError);vm.editContent='Actual C# skill source';await vm.saveFile();await until(()=>!vm.loadingFile);await vm.selectDirectory('references');await nextTick();assert(vm.selectedDirectory==='references','Directory selection failed');assert(router.currentRoute.value.query.workspaceProject==='retained','Workspace project lost');assert(!router.currentRoute.value.query.file,'File query survived directory selection');await vm.selectFile('references/source.md');await until(()=>!vm.loadingFile&&vm.fileContent==='Actual C# skill source');
 const explorer=await(await fetch('PREFIX/ext/projects/explorer?path='+encodeURIComponent(workspace.root))).json();assert(explorer.roots?.length,'Projects explorer lost configured workspace');const forbidden=await fetch('PREFIX/ext/projects/explorer?path='+encodeURIComponent('/etc'));assert(!forbidden.ok,'Skill workspace query widened permissions');
 await send('/file/migration-skill',{path:'slow.md',content:'Late old source'});await vm.selectFile('slow.md');await until(()=>slowRequested);await vm.selectFile('references/source.md');await until(()=>!vm.loadingFile&&vm.fileContent==='Actual C# skill source');releaseSlow();await wait();assert(vm.fileContent==='Actual C# skill source','Late source upload changed current file');
 vm.startEdit();vm.editContent='Unsaved';window.confirm=()=>false;await router.push('/other');assert(router.currentRoute.value.path==='/skills','Unsaved route guard failed');window.confirm=()=>true;await router.push('/other');assert(router.currentRoute.value.query.workspace==='1','Workspace state lost on page navigation');await router.push({path:'/skills',query:{skill:'migration-skill',file:'references/source.md'}});await until(()=>vm?.fileContent==='Actual C# skill source');assert(!vm.isEditing,'Old edit/dialog survived route change');app.unmount();assert(!error,error?.message);document.getElementById('result').textContent='PASS: Real C# skills workspace directory source edit navigation guards stale requests and cleanup'
}catch(e){document.getElementById('result').textContent='FAIL: '+e.message+' '+(error?.stack||'')}
</script></body></html>
""".Replace("PREFIX",prefix);
    [TestCase(""),TestCase("/chat")]
    public async Task Verbatim_skills_preserve_workspace_navigation_and_user_file_operations(string prefix)
    {
        var data=Path.Combine(Path.GetTempPath(),"ai-chat-skills-web-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(data);var builder=WebApplication.CreateBuilder();builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());builder.Services.AddSingleton<ServiceStack.Data.IDbConnectionFactory>(new OrmLiteConnectionFactory(Path.Combine(data,"chat.sqlite"),SqliteDialect.Provider));var auth=new UserAuth();
        var feature=new ChatFeature {RoutePrefix=prefix,AppDataPath=data,ChatAuth=auth,EnableProviders=["migration-none"],Config=ChatJson.ParseObject("""{"defaults":{"summarize":null},"providers":{}}"""),DisableExtensions=["git","gemini","gallery","analytics","voice"]};feature.Tools.AllowedDirectories=[data];feature.Routes.AddGet("/migration/workspace",_=>Task.FromResult<object?>(new JsonObject {["root"]=feature.GetUserWorkspace("alice")}));/* non-admins browse only their own workspace */feature.Routes.AddGet("/migration/skills-browser",_=>Task.FromResult<object?>(ChatResult.Html(Page(prefix))));builder.Services.AddPlugin(feature);
        await using var app=builder.Build();app.UseServiceStack(new AppHost(),options=>options.MapEndpoints());
        try{
            await app.StartAsync();auth.User="alice";using var http=new HttpClient {BaseAddress=new Uri(app.Urls.Single())};
            var info=new ProcessStartInfo("chromium"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var arg in new[]{"--headless","--no-sandbox","--disable-gpu","--enable-logging=stderr","--user-data-dir="+Path.Combine(data,"chromium"),"--virtual-time-budget=15000","--dump-dom",new Uri(http.BaseAddress!,prefix+"/migration/skills-browser").ToString()})info.ArgumentList.Add(arg);using var process=Process.Start(info)!;var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();try{await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));}catch{process.Kill(true);throw;}var html=await output;Assert.That(html,Does.Contain("<pre id=\"result\">PASS: Real C# skills workspace"),html+"\n"+await errors);
            Assert.That(File.ReadAllText(Path.Combine(data,"user","alice","skills","migration-skill","references","source.md")),Is.EqualTo("Actual C# skill source"));
            using var bob=new HttpRequestMessage(HttpMethod.Get,prefix+"/ext/skills");bob.Headers.Add("X-Migration-User","bob");using var bobResult=await http.SendAsync(bob);Assert.That(await bobResult.Content.ReadAsStringAsync(),Does.Not.Contain("migration-skill"));

        }finally{await app.StopAsync();if(Directory.Exists(data))Directory.Delete(data,true);}
    }
}
