#nullable enable
using System;
using System.Diagnostics;
using System.IO;
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
[NonParallelizable]
public class AiChatMigrationJevWebTests
{
    sealed class AppHost():AppHostBase(nameof(AiChatMigrationJevWebTests),typeof(ChatFeature).Assembly){public override void Configure(){}}
    sealed class HeaderAuth:IChatAuth
    {
        public string? FallbackUser;
        public bool IsEnabled=>true;
        public string? GetUserName(IRequest req)=>req.Headers["X-Migration-User"]??FallbackUser;
        public string? AssertUserName(IRequest req)=>GetUserName(req)??throw new UnauthorizedAccessException();
        public (bool,JsonObject?) CheckAuth(IRequest req)=>(GetUserName(req)!=null,null);
        public Task<JsonObject?> GetAuthInfoAsync(IRequest req)=>Task.FromResult<JsonObject?>(null);
        public Task SignOutAsync(IRequest req)=>Task.CompletedTask;
        public bool IsAdmin(IRequest req)=>false;
    }
    static string Fixture(string prefix)=>"""
<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><link rel="stylesheet" href="PREFIX/ui/app.css"><link rel="stylesheet" href="PREFIX/ui/components/ModelPicker.css"><script type="importmap">{"imports":{"vue":"PREFIX/ui/lib/vue.mjs","@servicestack/client":"PREFIX/ui/lib/servicestack-client.mjs","@servicestack/vue":"PREFIX/ui/lib/servicestack-vue.mjs","/ui/lazy.mjs":"PREFIX/ui/lazy.mjs","/ui/components/ModelPicker.mjs":"PREFIX/ui/components/ModelPicker.mjs","/ui/components/CheckBox.mjs":"PREFIX/ui/components/CheckBox.mjs","/ui/utils.mjs":"PREFIX/ui/utils.mjs"}}</script></head><body><div id="app" style="height:100vh"></div><pre id="result">RUNNING</pre><script type="module">
import {createApp,reactive,nextTick} from 'vue'
import ServiceStackVue from '@servicestack/vue'
import icons from 'PREFIX/ui/modules/icons.mjs'
import ModelPicker from 'PREFIX/ui/components/ModelPicker.mjs'
import JevPage from 'PREFIX/ext/jev/JevPage.mjs'
const wait=()=>new Promise(resolve=>setTimeout(resolve,20)),until=async fn=>{for(let i=0;i<500&&!fn();i++)await wait();if(!fn())throw Error('Timed out waiting for real C# studio')},assert=(v,m)=>{if(!v)throw Error(m)}
let error=null
const auth=reactive({userName:'alice'}),scope=name=>({state:{},get:(url,options={})=>fetch('PREFIX/ext/'+name+url,{...options,headers:{...options.headers,'X-Migration-User':auth.userName}})})
const theme=(await (await fetch('PREFIX/themes')).json()).light
const ctx={ai:{base:'PREFIX',auth,resolvePath:path=>'PREFIX'+path},state:reactive({models:[],selectedModel:null,config:{extensions:[]}}),scope,openModal(){},toast(){}}
const app=createApp(JevPage);app.use(ServiceStackVue);app.component('ModelPicker',ModelPicker);icons.install({components:value=>Object.entries(value).forEach(([name,c])=>app.component(name,c))});app.provide('ctx',ctx);Object.assign(app.config.globalProperties,{$styles:theme.styles});app.config.errorHandler=e=>error=e
try{
 const vm=app.mount('#app');await until(()=>!vm.loading&&vm.current);assert(!error,error?.message);await vm.select('sentiment');assert(vm.status.available,'OpenRouter decision capability absent')
 vm.loadExample(vm.current.recipe.examples[0]);await nextTick();assert(!document.querySelector('[data-jev-run-button]').disabled,'Actual example input did not enable run')
 await vm.runCurrent();await until(()=>vm.current.pending&&!['pending','running'].includes(vm.current.pending.status));assert(vm.current.result?.status==='succeeded','Run failed: '+JSON.stringify(vm.current.pending));assert(Object.keys(vm.current.result.answers).length===Object.keys(vm.current.recipe.questions).length,'Answer keys changed')
 const runId=vm.current.result.id,oldCount=vm.current.recipe.examples.length;vm.saveExample();vm.commitExample('Actual C# recorded result');assert(vm.current.recipe.examples.length===oldCount+1,'Actual example not saved to draft');await vm.save();assert(!vm.error,vm.error)
 const get=async(path,user='alice')=>{const r=await fetch('PREFIX/ext/jev'+path,{headers:{'X-Migration-User':user}});return {status:r.status,json:await r.json()}}
 const saved=await get('/recipes/sentiment');assert(saved.json.document.examples.at(-1).execution.status==='succeeded','Recorded example not persisted');assert((await get('/runs/'+runId,'bob')).status===404,'Run leaked across users');assert((await get('/recipes/sentiment','bob')).json.document.examples.length===oldCount,'Recipe save leaked across users')
 await vm.loadHistory();assert(vm.history.items.some(x=>x.id===runId),'Real C# history not visible');vm.view='run';await nextTick();assert(document.querySelector('[data-jev-page]')||document.querySelector('[data-jev-run-button]'),'Studio disappeared');assert(!error,error?.message)
 document.getElementById('result').textContent='PASS: Real C# Decision Studio run polling recorded example persistence and identity isolation';app.unmount()
}catch(e){document.getElementById('result').textContent='FAIL: '+e.message+' '+(error?.stack||'')}
</script></body></html>
""".Replace("PREFIX",prefix);
    [TestCase(""),TestCase("/chat")]
    public async Task Verbatim_studio_uses_real_CSharp_routes_and_persists_actual_examples(string prefix)
    {
        var data=Path.Combine(Path.GetTempPath(),"ai-chat-jev-web-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(data);
        var builder=WebApplication.CreateBuilder();builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());builder.Services.AddSingleton<ServiceStack.Data.IDbConnectionFactory>(new OrmLiteConnectionFactory(Path.Combine(data,"chat.sqlite"),SqliteDialect.Provider));
        var auth=new HeaderAuth();var feature=new ChatFeature {RoutePrefix=prefix,AppDataPath=data,ChatAuth=auth,EnableProviders=["migration-none"],Config=ChatJson.ParseObject("""{"defaults":{"summarize":null},"providers":{}}"""),DisableExtensions=["git","gemini","gallery","analytics","voice"]};
        var vector=JsonNode.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"AiChatFixtures","jev-contract-vectors.json")))!["recipes"]!.AsArray().Single(x=>x!["name"]!.GetValue<string>()=="sentiment")!;
        feature.Extensions.OfType<JevExtension>().Single().DecisionHandlerFactory=()=>new AiChatMigrationHttpHandler((req,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(vector["response"]!.ToJsonString())}));
        feature.Routes.AddGet("/migration/jev-browser",_=>Task.FromResult<object?>(ChatResult.Html(Fixture(prefix))));feature.Routes.AddGet("/migration/register",_=>Task.FromResult<object?>(ChatResult.Html("<!doctype html><title>Isolated registration</title>")));builder.Services.AddPlugin(feature);
        await using var app=builder.Build();app.UseServiceStack(new AppHost(),options=>options.MapEndpoints());
        try {
            await app.StartAsync();feature.Providers["openrouter"]=new ChatProvider {Id="openrouter",ApiKey="fixture-router"};using var http=new HttpClient {BaseAddress=new Uri(app.Urls.Single())};using(var denied=await http.GetAsync(prefix+"/ext/jev/status"))Assert.That(denied.StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));
            auth.FallbackUser="alice";
            var info=new ProcessStartInfo("chromium"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var arg in new[]{"--headless","--no-sandbox","--disable-gpu","--user-data-dir="+Path.Combine(data,"chromium"),"--virtual-time-budget=15000","--dump-dom",new Uri(http.BaseAddress!,prefix+"/migration/jev-browser").ToString()})info.ArgumentList.Add(arg);
            using var process=Process.Start(info)!;var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();try{await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));}catch{process.Kill(true);throw;}var html=await output;Assert.That(html,Does.Contain("<pre id=\"result\">PASS: Real C# Decision Studio"),html+"\n"+await errors);
        }finally{await app.StopAsync();if(Directory.Exists(data))Directory.Delete(data,true);}
    }
}
