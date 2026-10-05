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
public partial class AiChatMigrationOpenAiAuthTests
{
    sealed class AuthAppHost():AppHostBase(nameof(AiChatMigrationOpenAiAuthTests),typeof(ChatFeature).Assembly){public override void Configure(){}}
    sealed class BrowserAuth:IChatAuth
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
    static string BrowserPage(string prefix)=>"""
<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><link rel="stylesheet" href="PREFIX/ui/app.css"><script type="importmap">{"imports":{"vue":"PREFIX/ui/lib/vue.mjs"}}</script></head><body><div id="app"></div><pre id="result">RUNNING</pre><script type="module">
import {createApp,reactive,nextTick} from 'vue'
import OpenAiSettings from 'PREFIX/ext/openai_auth/OpenAiSettings.mjs'
const assert=(v,m)=>{if(!v)throw Error(m)},wait=()=>new Promise(r=>setTimeout(r,20)),until=async fn=>{for(let i=0;i<300&&!fn();i++)await wait();assert(fn(),'Timed out waiting for settings')}
const auth=reactive({userName:'alice'}),events=[],scope=name=>({getJson:url=>send(name,url),postJson:(url,body)=>send(name,url,body)})
async function send(name,url,body){const r=await fetch('PREFIX/ext/'+name+url,{method:body?'POST':'GET',headers:{'X-Migration-User':auth.userName,'Content-Type':'application/json'},body:body?JSON.stringify(body):undefined});const value=await r.json();return r.ok?{response:value}:{error:{message:value.responseStatus?.message||'HTTP '+r.status}}}
const ctx={scope,toast:value=>events.push(value)},theme=(await(await fetch('PREFIX/themes')).json()).light,app=createApp(OpenAiSettings);app.provide('ctx',ctx);app.config.globalProperties.$styles=theme.styles;let error=null;app.config.errorHandler=e=>error=e;window.confirm=()=>true
try{
 const vm=app.mount('#app');await until(()=>!vm.loading);assert(!vm.status.connected,'Initial user connected');assert(!vm.status.has_codex_auth,'Operator credentials advertised');assert(!document.getElementById('app').textContent.includes('Import from Codex CLI'),'Operator import rendered')
 const authUrl=await vm.connect(false);assert(authUrl,'No sign-in URL');vm.showManual=true;await nextTick();assert(document.querySelector('input[placeholder*="auth/callback"]'),'Manual callback control absent');
 const grant=await(await fetch('PREFIX/migration/issue?auth='+encodeURIComponent(authUrl))).json();vm.manualInput=grant.callback;await vm.submitManual();assert(!vm.manualError,vm.manualError);assert(vm.status.connected&&vm.status.email==='alice@example.test','Manual callback failed');assert(document.getElementById('app').textContent.includes('Connected'),'Connected badge absent')
 const status=await send('openai_auth','/status');assert(!JSON.stringify(status).includes('alice-access'),'Token leaked in settings');auth.userName='bob';assert(!(await send('openai_auth','/status')).response.connected,'Another user acquired grant');auth.userName='alice'
 await vm.disconnect();assert(!vm.status.connected,'Disconnect failed');assert(events.length>=2,'Missing feedback');app.unmount();assert(!error,error?.message);document.getElementById('result').textContent='PASS: Real C# subscription manual callback settings identity isolation disconnect and cleanup'
}catch(e){document.getElementById('result').textContent='FAIL: '+e.message+' '+(error?.stack||'')}
</script></body></html>
""".Replace("PREFIX",prefix);
    [TestCase(""),TestCase("/chat")]
    public async Task Verbatim_subscription_settings_complete_controlled_manual_callback(string prefix)
    {
        using var issuer=new Fixture();var data=Path.Combine(Path.GetTempPath(),"ai-chat-auth-web-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(data);
        var builder=WebApplication.CreateBuilder();builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());builder.Services.AddSingleton<ServiceStack.Data.IDbConnectionFactory>(new OrmLiteConnectionFactory(Path.Combine(data,"chat.sqlite"),SqliteDialect.Provider));
        var auth=new BrowserAuth();var feature=new ChatFeature {RoutePrefix=prefix,AppDataPath=data,ChatAuth=auth,EnableProviders=["migration-none"],Config=ChatJson.ParseObject("""{"defaults":{"summarize":null},"providers":{}}"""),DisableExtensions=["git","gemini","gallery","analytics","voice"]};feature.Extensions.OfType<OpenAiAuthExtension>().Single().Options=issuer.Ext.Options;
        feature.Routes.AddGet("/migration/auth-browser",_=>Task.FromResult<object?>(ChatResult.Html(BrowserPage(prefix))));
        feature.Routes.AddGet("/migration/issue",req=>{issuer.Authorization=Query(req.Request.QueryString["auth"]!);return Task.FromResult<object?>(new JsonObject {["callback"]=issuer.Callback()});});builder.Services.AddPlugin(feature);
        await using var app=builder.Build();app.UseServiceStack(new AuthAppHost(),options=>options.MapEndpoints());
        try{
            await app.StartAsync();using var http=new HttpClient {BaseAddress=new Uri(app.Urls.Single())};using(var denied=await http.GetAsync(prefix+"/ext/openai_auth/status"))Assert.That(denied.StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));auth.User="alice";
            var info=new ProcessStartInfo("chromium"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var arg in new[]{"--headless","--no-sandbox","--disable-gpu","--user-data-dir="+Path.Combine(data,"chromium"),"--virtual-time-budget=15000","--dump-dom",new Uri(http.BaseAddress!,prefix+"/migration/auth-browser").ToString()})info.ArgumentList.Add(arg);
            using var process=Process.Start(info)!;var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();try{await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));}catch{process.Kill(true);throw;}var html=await output;Assert.That(html,Does.Contain("<pre id=\"result\">PASS: Real C# subscription manual callback"),html+"\n"+await errors);
            Assert.That(feature.Providers["openai"],Is.TypeOf<OpenAiSubscriptionProvider>());Assert.That(issuer.TokenCalls,Is.EqualTo(1));
        }finally{await app.StopAsync();if(Directory.Exists(data))Directory.Delete(data,true);}
    }
}
