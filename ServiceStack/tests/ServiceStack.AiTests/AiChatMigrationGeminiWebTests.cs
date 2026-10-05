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
public class AiChatMigrationGeminiWebTests
{
    sealed class AppHost() : AppHostBase(nameof(AiChatMigrationGeminiWebTests),typeof(ChatFeature).Assembly)
    {
        public override void Configure() { }
    }
    sealed class HeaderAuth : IChatAuth
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
    sealed class Factory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)=>new(new AiChatMigrationHttpHandler((_,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{}") } )));
    }
    static string Fixture(string prefix,string manifest,long store)=>"""
<!doctype html><html><head><meta charset="utf-8"><link rel="stylesheet" href="PREFIX/ui/app.css">
<script type="importmap">{"imports":{"vue":"PREFIX/ui/lib/vue.mjs","@servicestack/client":"PREFIX/ui/lib/servicestack-client.mjs","@servicestack/vue":"PREFIX/ui/lib/servicestack-vue.mjs"}}</script></head>
<body><div id="app"></div><pre id="result">RUNNING</pre><script type="module">
import {createApp,h,nextTick} from 'vue'
import {ImportPanel,initImport} from 'PREFIX/ext/gemini/import.mjs'
import {initMetadata} from 'PREFIX/ext/gemini/metadata.mjs'
import {JsonSchemaForm} from '@servicestack/vue'
let error=null
const request=async(method,url,body)=>{
 const response=await fetch('PREFIX/ext/gemini'+url,{method,headers:body?{'Content-Type':'application/json'}:{},body:body?JSON.stringify(body):undefined})
 const json=await response.json();return response.ok?{response:json}:{error:json.responseStatus||json}
}
const ext={prefs:{},setPrefs(){},setError:e=>error=e,getJson:url=>request('GET',url),postJson:(url,body)=>request('POST',url,body),patchJson:(url,body)=>request('PATCH',url,body)}
initImport(ext);initMetadata(ext,{})
const app=createApp({render:()=>h(ImportPanel,{ref:'panel',storeId:STORE,routeTab:'folder',facets:{},presetCategory:'Docs'})})
app.component('JsonSchemaForm',JsonSchemaForm);app.config.globalProperties.$styles={};app.config.errorHandler=e=>error={message:e.message}
const wait=()=>new Promise(resolve=>setTimeout(resolve,25))
try{
 const vm=app.mount('#app').$refs.panel
 for(let i=0;i<100&&!vm.tabs.length;i++)await wait()
 const loaded=await request('POST','/sources/load',{filestoreId:STORE,path:MANIFEST});if(loaded.error)throw new Error(JSON.stringify(loaded.error))
 await vm.editSource(loaded.response);vm.name='Browser saved import';await vm.saveImport();if(error)throw new Error(error.message||JSON.stringify(error))
 const saved=await request('GET','/sources/'+loaded.response.id);if(saved.response.name!=='Browser saved import')throw new Error('Browser save not persisted by C#')
 if(saved.response.lastRunId)throw new Error('Saving settings ran extraction')
 const repeated=await request('POST','/sources/load',{filestoreId:STORE,path:MANIFEST});if(repeated.response.id!==loaded.response.id)throw new Error('Repeated browser load duplicated source')
 await request('DELETE','/sources/'+loaded.response.id+'?documents=keep');const again=await request('POST','/sources/load',{filestoreId:STORE,path:MANIFEST})
 if(again.error||again.response.name!=='Browser saved import')throw new Error('Portable reload lost saved settings')
 document.getElementById('result').textContent='PASS: Real C# saved-import load edit and reload through verbatim Gemini UI'
}catch(e){document.getElementById('result').textContent='FAIL: '+e.message}
</script></body></html>
""".Replace("PREFIX",prefix).Replace("MANIFEST",JsonValue.Create(manifest)!.ToJsonString()).Replace("STORE",store.ToString(System.Globalization.CultureInfo.InvariantCulture));

    static async Task<HttpResponseMessage> Send(HttpClient client,string method,string path,string? user,JsonNode? body=null)
    {
        using var req=new HttpRequestMessage(new HttpMethod(method),path);if(user!=null)req.Headers.Add("X-Migration-User",user);
        if(body!=null)req.Content=new StringContent(body.ToJsonString(),Encoding.UTF8,"application/json");return await client.SendAsync(req);
    }
    [TestCase(""),TestCase("/chat")]
    public async Task Real_host_preserves_prefix_auth_and_saved_import_browser_contract(string prefix)
    {
        var data=Path.Combine(Path.GetTempPath(),"ai-chat-gemini-http-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(data);
        var root=Path.Combine(data,"source");Directory.CreateDirectory(root);var manifest=Path.Combine(root,"import.json");File.WriteAllText(manifest,"""{"version":1,"source":{"name":"Docs","type":"folder","config":{"path":"."}},"metadata":{"defaults":{},"rules":[]}}""");
        var builder=WebApplication.CreateBuilder();builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());
        builder.Services.AddSingleton<ServiceStack.Data.IDbConnectionFactory>(new OrmLiteConnectionFactory(Path.Combine(data,"chat.sqlite"),SqliteDialect.Provider));builder.Services.AddSingleton<IHttpClientFactory>(new Factory());
        var auth=new HeaderAuth();var feature=new ChatFeature { RoutePrefix=prefix,AppDataPath=data,ChatAuth=auth,EnableProviders=["migration-none"],Config=ChatJson.ParseObject("""{"defaults":{"summarize":null},"providers":{}}"""),DisableExtensions=["git","gallery","analytics","voice"],Variables={ ["GOOGLE_API_KEY"]="fixture",["GEMINI_API_KEY"]="fixture" } };
        feature.Tools.AllowedDirectories=[data];long storeId=0;
        feature.Routes.AddGet("/migration/gemini-browser",_=>Task.FromResult<object?>(ChatResult.Html(Fixture(prefix,manifest,storeId))));builder.Services.AddPlugin(feature);
        await using var app=builder.Build();app.UseServiceStack(new AppHost(),options=>options.MapEndpoints());
        try
        {
            await app.StartAsync();using var http=new HttpClient { BaseAddress=new Uri(app.Urls.Single()) };var db=new GeminiDb(feature.ChatDb!);storeId=db.InsertFilestore(new ChatFilestore { User="alice",Name="fileSearchStores/fixture",DisplayName="Docs",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow });
            using(var denied=await Send(http,"POST",prefix+"/ext/gemini/sources/load",null,new JsonObject { ["path"]=manifest,["filestoreId"]=storeId }))Assert.That(denied.StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));
            using(var wrongUser=await Send(http,"POST",prefix+"/ext/gemini/sources/load","bob",new JsonObject { ["path"]=manifest,["filestoreId"]=storeId }))Assert.That(wrongUser.StatusCode,Is.EqualTo(HttpStatusCode.BadRequest));
            auth.FallbackUser="alice";
            var info=new ProcessStartInfo("chromium") { UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true };
            foreach(var arg in new[]{"--headless","--no-sandbox","--disable-gpu","--user-data-dir="+Path.Combine(data,"chromium"),"--virtual-time-budget=15000","--dump-dom",new Uri(http.BaseAddress!,prefix+"/migration/gemini-browser").ToString()})info.ArgumentList.Add(arg);
            using var process=Process.Start(info)!;var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();try{await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));}catch{process.Kill(true);throw;}
            var html=await output;Assert.That(html,Does.Contain("<pre id=\"result\">PASS: Real C# saved-import"),html+"\n"+await errors);
            Assert.That(db.QuerySources(storeId,"alice").Single().Name,Is.EqualTo("Browser saved import"));Assert.That(db.QueryAllDocuments(storeId,"alice"),Is.Empty);
        }
        finally {await app.StopAsync();feature.RunShutdownHandlers();if(Directory.Exists(data))Directory.Delete(data,true);}
    }
}
