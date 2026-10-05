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
public class AiChatMigrationAgentsWebTests
{
    sealed class AppHost():AppHostBase(nameof(AiChatMigrationAgentsWebTests),typeof(ChatFeature).Assembly){public override void Configure(){}}
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
<!doctype html><html><head><meta charset="utf-8"><link rel="stylesheet" href="PREFIX/ui/app.css"><link rel="stylesheet" href="PREFIX/ui/components/ModelPicker.css"><script type="importmap">{"imports":{"vue":"PREFIX/ui/lib/vue.mjs","@servicestack/client":"PREFIX/ui/lib/servicestack-client.mjs","@servicestack/vue":"PREFIX/ui/lib/servicestack-vue.mjs","/ui/lazy.mjs":"PREFIX/ui/lazy.mjs","/ui/utils.mjs":"PREFIX/ui/utils.mjs"}}</script></head><body><div id="app"></div><pre id="result">RUNNING</pre><script>for(const event of ['error','unhandledrejection'])window.addEventListener(event,e=>document.getElementById('result').textContent='FAIL: '+(e.error?.stack||e.reason?.stack||e.message||e.reason)+' '+e.filename+':'+e.lineno)</script><script type="module">
import {createApp,reactive,nextTick} from 'vue'
import ServiceStackVue from '@servicestack/vue'
import icons from 'PREFIX/ui/modules/icons.mjs'
import ModelPicker from 'PREFIX/ui/components/ModelPicker.mjs'
import {CheckBox} from 'PREFIX/ui/components/CheckBox.mjs'
import agents from 'PREFIX/ext/agents/index.mjs'
const assert=(v,m)=>{if(!v)throw Error(m)},wait=()=>new Promise(r=>setTimeout(r,20)),until=async fn=>{for(let i=0;i<300&&!fn();i++)await wait();assert(fn(),'Timed out waiting for profile manager')}
const components={},prefs=reactive({}),state=reactive({config:{defaultModel:'gpt-5.5'},models:await(await fetch('PREFIX/models')).json(),themes:await(await fetch('PREFIX/themes')).json(),agents:{},skills:{}})
const ctx={state,prefs:{},utils:{idToName:s=>s,pluralize:(s,n)=>n===1?s:s+'s'},changeProfile(){},resolveThemes:x=>x,getDefaultAgentAvatar:()=> 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg"/%3E',setState:v=>Object.assign(state,v),setGlobals:v=>Object.assign(ctx,v),components:v=>Object.assign(components,v),modals(){},setComposerTop(){},setThreadHeaders(){},setThreadFooters(){},chatRequestFilters:[],toast(){}}
ctx.scope=()=>({ctx,prefs,baseUrl:'PREFIX/ext/agents',setPrefs:v=>Object.assign(prefs,v),get:path=>fetch('PREFIX/ext/agents/'+path),getJson:path=>send(path),postJson:(path,body)=>send(path,body)})
async function send(path,body){const r=await fetch('PREFIX/ext/agents'+(path?'/'+path:''),{method:body?'POST':'GET',headers:{'Content-Type':'application/json'},body:body?JSON.stringify(body):undefined});const value=await r.json();if(!r.ok)throw Error(value.responseStatus?.message||'HTTP '+r.status);return {response:value}}
agents.install(ctx);await agents.load(ctx);let done=0,error=null
const app=createApp(components.ProfilesManagerModal,{onDone:()=>done++});app.use(ServiceStackVue);app.component('ModelPicker',ModelPicker);app.component('CheckBox',CheckBox);icons.install({components:value=>Object.entries(value).forEach(([name,c])=>app.component(name,c))});app.provide('ctx',ctx);Object.assign(app.config.globalProperties,{$ctx:ctx,$styles:state.themes.light.styles});app.config.errorHandler=e=>error=e
try{
 const vm=app.mount('#app');await nextTick();assert(state.models.some(m=>m.id==='subscription-model'),'Account catalog not visible');
 vm.isModelPickerOpen=true;await until(()=>document.querySelector('dialog.llms-model-dialog[open]'));const search=document.querySelector('input[aria-label="Search models"]');assert(document.activeElement===search,'Picker search did not receive focus');search.value='Subscription Model';search.dispatchEvent(new Event('input',{bubbles:true}));await nextTick();const card=[...document.querySelectorAll('.llms-model-card')].find(x=>x.textContent.includes('subscription-model'));assert(card,'Subscription card absent');card.click();await nextTick();assert(vm.editForm.model==='Subscription Model','Shared picker returned wrong model value');assert(!document.querySelector('dialog.llms-model-dialog[open]'),'Picker did not close');await vm.saveForm();assert(ctx.agents.getProfileOverride('default').model==='Subscription Model','Default preference did not persist');await vm.selectProfile(vm.defaultProfileItem);assert(vm.editForm.model==='Subscription Model','Preference reload failed');
 const created=await ctx.agents.createProfile('Migration Profile');await vm.selectProfile(vm.agentProfiles.find(p=>p.id===created.id));vm.editForm.model='Subscription Model';await vm.saveForm();await ctx.agents.load();await vm.selectProfile(vm.agentProfiles.find(p=>p.id===created.id));assert(vm.editForm.model==='Subscription Model','Custom server profile override round-trip failed');
 vm.isModelPickerOpen=true;await until(()=>document.querySelector('dialog.llms-model-dialog[open]'));vm.handleEscape({stopPropagation(){}});await nextTick();assert(!vm.isModelPickerOpen,'Escape did not close picker');assert(done===2,'Escape closed profile manager');app.unmount();assert(!error,error?.message);assert(!document.querySelector('dialog.llms-model-dialog'),'Picker leaked after unmount');document.getElementById('result').textContent='PASS: Real C# profile model override shared picker subscription selection focus escape and cleanup'
}catch(e){document.getElementById('result').textContent='FAIL: '+e.message+' '+(error?.stack||'')}
</script></body></html>
""".Replace("PREFIX",prefix);
    [TestCase(""),TestCase("/chat")]
    public async Task Verbatim_agents_round_trip_profile_models_and_subscription_picker(string prefix)
    {
        var data=Path.Combine(Path.GetTempPath(),"ai-chat-agents-web-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(data);var builder=WebApplication.CreateBuilder();builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());builder.Services.AddSingleton<ServiceStack.Data.IDbConnectionFactory>(new OrmLiteConnectionFactory(Path.Combine(data,"chat.sqlite"),SqliteDialect.Provider));var auth=new UserAuth();
        var feature=new ChatFeature {RoutePrefix=prefix,AppDataPath=data,ChatAuth=auth,EnableProviders=["migration-none"],Config=ChatJson.ParseObject("""{"defaults":{"summarize":null},"providers":{}}"""),DisableExtensions=["git","gemini","gallery","analytics","voice"]};var subscription=feature.Extensions.OfType<OpenAiAuthExtension>().Single();subscription.Options.HttpHandlerFactory=()=>new AiChatMigrationHttpHandler((_,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"models\":[{\"slug\":\"subscription-model\",\"display_name\":\"Subscription Model\",\"visibility\":\"list\"}]}")}));feature.Routes.AddGet("/migration/agents-browser",_=>Task.FromResult<object?>(ChatResult.Html(Page(prefix))));builder.Services.AddPlugin(feature);
        await using var app=builder.Build();app.UseServiceStack(new AppHost(),options=>options.MapEndpoints());
        try{
            await app.StartAsync();auth.User="alice";subscription.Credentials.Save("alice",new JsonObject {["client_id"]="issued-client",["subject"]="account-alice",["access_token"]="fixture-access",["expires_at"]=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+3600});subscription.Activate();using var http=new HttpClient {BaseAddress=new Uri(app.Urls.Single())};
            var info=new ProcessStartInfo("chromium"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var arg in new[]{"--headless","--no-sandbox","--disable-gpu","--enable-logging=stderr","--user-data-dir="+Path.Combine(data,"chromium"),"--virtual-time-budget=15000","--dump-dom",new Uri(http.BaseAddress!,prefix+"/migration/agents-browser").ToString()})info.ArgumentList.Add(arg);using var process=Process.Start(info)!;var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();try{await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));}catch{process.Kill(true);throw;}var html=await output;Assert.That(html,Does.Contain("<pre id=\"result\">PASS: Real C# profile model override"),html+"\n"+await errors);
            var persisted=ChatJson.ParseObject(File.ReadAllText(Path.Combine(data,"user","alice","profiles","migration-profile","config.json")));Assert.That(persisted.GetString("model"),Is.EqualTo("Subscription Model"));Assert.That(feature.Providers["openai"].ProviderModel(persisted.GetString("model")!),Is.EqualTo("subscription-model"));
            using var bob=new HttpRequestMessage(HttpMethod.Get,prefix+"/ext/agents");bob.Headers.Add("X-Migration-User","bob");using var bobResult=await http.SendAsync(bob);Assert.That(await bobResult.Content.ReadAsStringAsync(),Does.Not.Contain("migration-profile"));
        }finally{await app.StopAsync();if(Directory.Exists(data))Directory.Delete(data,true);}
    }
}
