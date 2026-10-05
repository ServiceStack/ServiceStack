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
public class AiChatMigrationPublishWebTests
{
    sealed class AppHost():AppHostBase(nameof(AiChatMigrationPublishWebTests),typeof(ChatFeature).Assembly){public override void Configure(){}}
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
<!doctype html><html><head><meta charset="utf-8"><link rel="stylesheet" href="PREFIX/ui/app.css"><script type="importmap">{"imports":{"vue":"PREFIX/ui/lib/vue.mjs"}}</script></head><body><div id="app"></div><pre id="result">RUNNING</pre><script type="module">
import {createApp,h,reactive,nextTick} from 'vue'
import PublisherAccount,{registrationMessage,registrationUrl} from 'PREFIX/ext/share_llmspy/PublisherAccount.mjs'
const wait=()=>new Promise(resolve=>setTimeout(resolve,20))
const auth=reactive({userName:'alice'}),account=reactive({baseUrl:location.origin,registerUrl:location.origin+'/PREFIXREGISTER',apiKey:null})
let calls=0,error=null,connected=0
const ext={async postJson(url,body){calls++;const res=await fetch('PREFIX/ext/share_llmspy'+url,{method:'POST',headers:{'Content-Type':'application/json','X-Migration-User':auth.userName},body:JSON.stringify(body)});const json=await res.json();return res.ok?{response:json}:{error:json}},setState({publish}){Object.assign(account,publish)},setError:e=>error=e}
const app=createApp({render:()=>h(PublisherAccount,{account,onConnected:()=>connected++})});app.provide('ctx',{ai:{auth},scope:()=>ext});app.config.errorHandler=e=>error=e
try {
 app.mount('#app');document.querySelector('button').click();await nextTick();let frame=document.querySelector('iframe'),url=new URL(frame.src),nonce=url.searchParams.get('nonce')
 if(url.searchParams.get('callerOrigin')!==location.origin||url.searchParams.get('username')!=='alice'||!nonce)throw new Error('Registration parameters missing')
 const send=(origin,source,n,data={})=>window.dispatchEvent(new MessageEvent('message',{origin,source,data:{type:'register-success',nonce:n,apiKey:'alice-key',userName:'Alice',userId:'alice-id',...data}}))
 send('https://wrong.example',frame.contentWindow,nonce);send(location.origin,window,nonce);send(location.origin,frame.contentWindow,'wrong');await wait();if(calls)throw new Error('Untrusted registration accepted')
 auth.userName='bob';send(location.origin,frame.contentWindow,nonce);await wait();if(calls)throw new Error('Stale owner registration accepted')
 auth.userName='alice';send(location.origin,frame.contentWindow,nonce);for(let i=0;i<100&&!connected;i++)await wait();if(error||connected!==1||calls!==1||account.apiKey!=='ali******-key')throw new Error('Valid registration did not persist')
 const get=async user=>{const r=await fetch('PREFIX/ext/share_llmspy/config.json',{headers:{'X-Migration-User':user}});return r.json()}
 if((await get('bob')).apiKey)throw new Error('Account leaked to bob')
 app.unmount();auth.userName='bob';Object.assign(account,{apiKey:null,userName:null});let bobConnected=0
 const bob=createApp({render:()=>h(PublisherAccount,{account,onConnected:()=>bobConnected++})});bob.provide('ctx',{ai:{auth},scope:()=>ext});bob.mount('#app');document.querySelector('button').click();await nextTick();frame=document.querySelector('iframe');nonce=new URL(frame.src).searchParams.get('nonce')
 send(location.origin,frame.contentWindow,nonce,{apiKey:'bob-key',userName:'Bob',userId:'bob-id'});for(let i=0;i<100&&!bobConnected;i++)await wait()
 if(bobConnected!==1||(await get('alice')).userName!=='Alice'||(await get('bob')).userName!=='Bob')throw new Error('Account switching failed')
 document.getElementById('result').textContent='PASS: Real C# publisher registration origin nonce owner checks and account switching'
}catch(e){document.getElementById('result').textContent='FAIL: '+e.message}
</script></body></html>
""".Replace("PREFIXREGISTER",prefix.TrimStart('/')+"/migration/register").Replace("PREFIX",prefix);
    [TestCase(""),TestCase("/chat")]
    public async Task Verbatim_publisher_component_validates_registration_and_keeps_accounts_separate(string prefix)
    {
        var data=Path.Combine(Path.GetTempPath(),"ai-chat-publish-web-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(data);
        var builder=WebApplication.CreateBuilder();builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());builder.Services.AddSingleton<ServiceStack.Data.IDbConnectionFactory>(new OrmLiteConnectionFactory(Path.Combine(data,"chat.sqlite"),SqliteDialect.Provider));
        var auth=new HeaderAuth();var feature=new ChatFeature {RoutePrefix=prefix,AppDataPath=data,ChatAuth=auth,EnableProviders=["migration-none"],Config=ChatJson.ParseObject("""{"defaults":{"summarize":null},"providers":{}}"""),DisableExtensions=["git","gemini","gallery","analytics","voice"]};
        feature.Extensions.OfType<ShareLlmspyExtension>().Single().Enabled=true;
        feature.Routes.AddGet("/migration/publish-browser",_=>Task.FromResult<object?>(ChatResult.Html(Fixture(prefix))));feature.Routes.AddGet("/migration/register",_=>Task.FromResult<object?>(ChatResult.Html("<!doctype html><title>Isolated registration</title>")));builder.Services.AddPlugin(feature);
        await using var app=builder.Build();app.UseServiceStack(new AppHost(),options=>options.MapEndpoints());
        try {
            await app.StartAsync();new PublisherConfiguration(feature.Extensions.OfType<ShareLlmspyExtension>().Single().Ctx).Save(null,new JsonObject { ["baseUrl"]=app.Urls.Single(),["allowHttp"]=true });using var http=new HttpClient {BaseAddress=new Uri(app.Urls.Single())};using(var denied=await http.GetAsync(prefix+"/ext/share_llmspy/config.json"))Assert.That(denied.StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));
            auth.FallbackUser="alice";
            var info=new ProcessStartInfo("chromium"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var arg in new[]{"--headless","--no-sandbox","--disable-gpu","--user-data-dir="+Path.Combine(data,"chromium"),"--virtual-time-budget=15000","--dump-dom",new Uri(http.BaseAddress!,prefix+"/migration/publish-browser").ToString()})info.ArgumentList.Add(arg);
            using var process=Process.Start(info)!;var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();try{await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));}catch{process.Kill(true);throw;}var html=await output;Assert.That(html,Does.Contain("<pre id=\"result\">PASS: Real C# publisher registration"),html+"\n"+await errors);
        }finally{await app.StopAsync();if(Directory.Exists(data))Directory.Delete(data,true);}
    }
}
