#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
namespace ServiceStack.AiTests;
[NonParallelizable]
public class AiChatMigrationReleaseTests
{
    sealed class AppHost():AppHostBase(nameof(AiChatMigrationReleaseTests),typeof(ChatFeature).Assembly)
    {
        public override void Configure()
        {
            if(GetPlugin<ApiKeysFeature>() is not {} keys)return;
            using var db=Resolve<IDbConnectionFactory>().OpenDbConnection();keys.InitSchema(db);if(keys.ApiKeyCount(db)>0)return;
            keys.Insert(db,new(){Key="migration-manager-key",UserId="key-manager",Scopes=["Manager"],Name="Isolated release check"});
            keys.Insert(db,new(){Key="migration-employee-key",UserId="key-employee",Scopes=["Employee"],Name="Isolated release check"});
        }
    }
    [Test]
    public async Task Legacy_schema_and_projects_upgrade_then_complete_stopped_backup_restore()
    {
        using var host=new AiChatMigrationTestHost();
        var backup=Path.Combine(Path.GetTempPath(),"ai-chat-release-backup-"+Guid.NewGuid().ToString("N"));
        var projectFile=Path.Combine(host.DirectoryPath,"user","alice","projects","projects.json");Directory.CreateDirectory(Path.GetDirectoryName(projectFile)!);
        File.WriteAllText(projectFile,"""[{"id":"existing-project","name":"Existing","folder":"existing","showInSidebar":true}]""");
        long documentId,threadId;
        using(var db=host.Db.OpenDb()){
            db.CreateTableIfNotExists<LegacyPreMigrationDocument>();
            documentId=db.Insert(new LegacyPreMigrationDocument {User="alice",FilestoreId=12,SourceId=7,SourceScopeId=7,SourceKey="existing.md",Name="fileSearchStores/existing/documents/original",State="ACTIVE"},selectIdentity:true);
            threadId=db.Insert(new ChatThread {User="alice",ProjectId="existing-project",Title="Existing history",Messages="[]",LastActivityAt=new DateTime(2026,10,1)},selectIdentity:true);
            db.Insert(new ServiceStack.AI.ChatMessage {ThreadId=threadId,Sequence=1,Role="user",Message="""{"role":"user","content":"Keep original history"}""",Active=true});
            Assert.That(db.GetTableColumns<LegacyPreMigrationDocument>().Select(c=>c.ColumnName),Does.Not.Contain("SourceManifestPath"));
        }
        static void Copy(string from,string to){Directory.CreateDirectory(to);foreach(var file in Directory.GetFiles(from,"*",SearchOption.AllDirectories)){var dest=Path.Combine(to,Path.GetRelativePath(from,file));Directory.CreateDirectory(Path.GetDirectoryName(dest)!);File.Copy(file,dest,true);}}
        try{
            // No active host/background worker and every connection is closed for both copies.
            host.Feature.RunShutdownHandlers();Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Copy(host.DirectoryPath,backup);
            var gemini=new GeminiDb(host.Db);gemini.InitSchema();
            var upgraded=gemini.GetDocument(documentId,"alice")!;Assert.That(upgraded.SourceScopeId,Is.LessThan(0));Assert.That(upgraded.Name,Is.EqualTo("fileSearchStores/existing/documents/original"));Assert.That(upgraded.SourceId,Is.EqualTo(7));Assert.That(gemini.GetDocument(documentId,"bob"),Is.Null);
            var projects=host.Install(new ProjectsExtension());Assert.That(projects.GetUserProjects("alice").Single().GetString("id"),Is.EqualTo("existing-project"));
            await host.SendAsync("PATCH","/ext/projects/archive/existing-project","alice",new JsonObject {["archived"]=true});
            using(var db=host.Db.OpenDb()){Assert.That(db.GetTableColumns<LegacyPreMigrationDocument>().Select(c=>c.ColumnName),Does.Contain("SourceManifestPath"));Assert.That(db.Single<ServiceStack.AI.ChatMessage>(x=>x.ThreadId==threadId)!.Message,Does.Contain("Keep original history"));Assert.That(db.SingleById<ChatThread>(threadId)!.LastActivityAt,Is.EqualTo(new DateTime(2026,10,1)));}
            host.Feature.RunShutdownHandlers();Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(host.DirectoryPath,true);Copy(backup,host.DirectoryPath);
            foreach(var file in Directory.GetFiles(backup,"*",SearchOption.AllDirectories)){var restored=Path.Combine(host.DirectoryPath,Path.GetRelativePath(backup,file));Assert.That(SHA256.HashData(File.ReadAllBytes(restored)),Is.EqualTo(SHA256.HashData(File.ReadAllBytes(file))));}
            using(var db=host.Db.OpenDb()){Assert.That(db.GetTableColumns<LegacyPreMigrationDocument>().Select(c=>c.ColumnName),Does.Not.Contain("SourceManifestPath"));Assert.That(db.TableExists<ChatDocumentIdentity>(),Is.False);var original=db.SingleById<LegacyPreMigrationDocument>(documentId)!;Assert.That(original.SourceScopeId,Is.EqualTo(7));Assert.That(original.Name,Is.EqualTo(upgraded.Name));Assert.That(db.Single<ServiceStack.AI.ChatMessage>(x=>x.ThreadId==threadId)!.Message,Does.Contain("Keep original history"));}
            Assert.That(JsonNode.Parse(File.ReadAllText(projectFile))![0]!["archived"],Is.Null);
        }finally{if(Directory.Exists(backup))Directory.Delete(backup,true);}
    }
    [Test]
    public void Packaged_shared_assets_match_the_current_sync_manifest()
    {
        var assembly=typeof(ChatFeature).Assembly;
        using var manifestStream=assembly.GetManifestResourceStream("ServiceStack.AI.chat.shared-assets.json");
        Assert.That(manifestStream,Is.Not.Null,"Run sync.sh to generate the current shared-asset manifest");
        using var manifestReader=new StreamReader(manifestStream!);
        var manifest=JsonNode.Parse(manifestReader.ReadToEnd())!["files"]!.AsObject();
        Assert.That(manifest.Count,Is.GreaterThan(0));
        foreach(var pair in manifest){var parts=pair.Key.Split('/');var logical=string.Join(".",parts.Select((part,i)=>i==parts.Length-1?part:part.Replace('-','_')));var name=assembly.GetManifestResourceNames().SingleOrDefault(n=>n.EndsWith("."+logical,StringComparison.Ordinal));using var stream=name==null?null:assembly.GetManifestResourceStream(name);Assert.That(stream,Is.Not.Null,"Missing embedded resource: "+pair.Key);Assert.That(Convert.ToHexString(SHA256.HashData(stream!)).ToLowerInvariant(),Is.EqualTo(pair.Value!.GetValue<string>()),"Stale resource: "+pair.Key);}
        Assert.That(manifest.ContainsKey("chat/ui/modules/model-selector.mjs"),Is.True);
    }
    static async Task<JsonNode> Json(HttpClient client,string path)
    {using var response=await client.GetAsync(path);response.EnsureSuccessStatusCode();return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;}
    [TestCase(""),TestCase("/chat")]
    public async Task Real_host_cookie_api_key_roles_named_connection_and_restart_keep_identity(string prefix)
    {
        var data=Path.Combine(Path.GetTempPath(),"ai-chat-release-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(data);var named=Path.Combine(data,"named-chat.sqlite");var defaultDb=Path.Combine(data,"host.sqlite");
        try{for(var restart=0;restart<2;restart++){
            var factory=new OrmLiteConnectionFactory(defaultDb,SqliteDialect.Provider);factory.RegisterConnection("migration-chat",named,SqliteDialect.Provider);
            var builder=WebApplication.CreateBuilder();builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Host.UseDefaultServiceProvider(o=>o.ValidateOnBuild=false);builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(data,"keys")));builder.Services.AddAuthentication("migration-cookie").AddCookie("migration-cookie",options=>{options.Cookie.Name="migration-chat-cookie";});builder.Services.AddSingleton<IDbConnectionFactory>(factory);builder.Services.AddPlugin(new ApiKeysFeature());
            var feature=new ChatFeature {DisableAdminUi=true,RoutePrefix=prefix,RequireAuth=true,RequiredRole="Manager",NamedConnection="migration-chat",AppDataPath=data,EnableProviders=["migration-none"],Config=ChatJson.ParseObject("""{"defaults":{"summarize":null},"providers":{}}"""),DisableExtensions=["git","gemini","gallery","analytics","voice"]};builder.Services.AddPlugin(feature);builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());
            await using var app=builder.Build();app.UseAuthentication();app.MapPost("/migration/login/{name}",async(HttpContext context,string name)=>{var role=name=="employee"?"Employee":"Manager";await context.SignInAsync("migration-cookie",new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,name),new Claim(ClaimTypes.Name,name),new Claim(ClaimTypes.Role,role)},"migration-cookie")));return Results.Ok();});app.UseServiceStack(new AppHost(),options=>options.MapEndpoints());
            try{
                await app.StartAsync();var url=new Uri(app.Urls.Single());using var anonymous=new HttpClient {BaseAddress=url};Assert.That((await anonymous.GetAsync(prefix+"/ext/openai_auth/status")).StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));
                using var manager=new HttpClient(new HttpClientHandler {CookieContainer=new CookieContainer()}){BaseAddress=url};(await manager.PostAsync("/migration/login/manager",null)).EnsureSuccessStatusCode();var status=await Json(manager,prefix+"/ext/openai_auth/status");Assert.That(status["has_codex_auth"]!.GetValue<bool>(),Is.False);
                using var employee=new HttpClient(new HttpClientHandler {CookieContainer=new CookieContainer()}){BaseAddress=url};(await employee.PostAsync("/migration/login/employee",null)).EnsureSuccessStatusCode();Assert.That((await employee.GetAsync(prefix+"/ext/openai_auth/status")).StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));
                using var keyClient=new HttpClient {BaseAddress=url};keyClient.DefaultRequestHeaders.Authorization=new("Bearer","migration-manager-key");Assert.That((await keyClient.GetAsync(prefix+"/ext/openai_auth/status")).StatusCode,Is.EqualTo(HttpStatusCode.OK));keyClient.DefaultRequestHeaders.Authorization=new("Bearer","migration-employee-key");Assert.That((await keyClient.GetAsync(prefix+"/ext/openai_auth/status")).StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));
                var recipes=await Json(manager,prefix+"/ext/jev/recipes");Assert.That(recipes,Is.Not.Null);var preferences=feature.Extensions.OfType<OpenAiAuthExtension>().Single();if(restart==0)preferences.Credentials.Save("manager",new JsonObject {["client_id"]="issued-client",["subject"]="account-manager",["access_token"]="protected-fixture",["expires_at"]=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+3600});else{Assert.That((await Json(manager,prefix+"/ext/openai_auth/status"))["connected"]!.GetValue<bool>(),Is.True);Assert.That(feature.Providers["openai"],Is.TypeOf<OpenAiSubscriptionProvider>());}
                using var hostDb=factory.OpenDbConnection();using var chatDb=factory.OpenDbConnection("migration-chat");Assert.That(hostDb.TableExists<ChatThread>(),Is.False);Assert.That(chatDb.TableExists<ChatThread>(),Is.True);
            }finally{await app.StopAsync();}
        }}finally{if(Directory.Exists(data))Directory.Delete(data,true);}
    }
    [TestCase(""),TestCase("/chat")]
    public async Task Unauthenticated_default_mode_and_disabled_extensions_keep_safe_capabilities(string prefix)
    {
        var data=Path.Combine(Path.GetTempPath(),"ai-chat-release-default-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(data);var builder=WebApplication.CreateBuilder();builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Host.UseDefaultServiceProvider(o=>o.ValidateOnBuild=false);builder.Services.AddSingleton<IDbConnectionFactory>(new OrmLiteConnectionFactory(Path.Combine(data,"chat.sqlite"),SqliteDialect.Provider));var feature=new ChatFeature {DisableAdminUi=true,RoutePrefix=prefix,RequireAuth=false,AppDataPath=data,EnableProviders=["migration-none"],Config=ChatJson.ParseObject("""{"defaults":{"summarize":null},"providers":{}}"""),DisableExtensions=["git","gemini","gallery","analytics","voice","publish"]};builder.Services.AddPlugin(feature);builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());await using var app=builder.Build();app.UseServiceStack(new AppHost(),options=>options.MapEndpoints());
        try{await app.StartAsync();using var http=new HttpClient {BaseAddress=new Uri(app.Urls.Single())};Assert.That((await http.GetAsync(prefix+"/ext/openai_auth/status")).StatusCode,Is.EqualTo(HttpStatusCode.OK));using var connect=await http.PostAsync(prefix+"/ext/openai_auth/connect",new StringContent("{}"));connect.EnsureSuccessStatusCode();Assert.That(feature.Extensions.OfType<OpenAiAuthExtension>().Single().Flows.HasPending("default"),Is.True);using var disconnect=await http.PostAsync(prefix+"/ext/openai_auth/disconnect",new StringContent("{}"));disconnect.EnsureSuccessStatusCode();Assert.That(feature.PublisherApi.Available,Is.False);Assert.That(feature.GitProvisioner.Clone,Is.False);Assert.That(feature.UiExtensions.OfType<JsonObject>().Any(x=>x.GetString("id")=="git"||x.GetString("id")=="publish"),Is.False);Assert.That(await Json(http,prefix+"/ext/jev/recipes"),Is.Not.Null);}
        finally{await app.StopAsync();if(Directory.Exists(data))Directory.Delete(data,true);}
    }
}

// Baseline 42a187a9 document shape: prove upgrade and restoration against its actual old schema.
[Alias("ChatDocument")]
[UniqueConstraint(nameof(FilestoreId),nameof(SourceScopeId),nameof(SourceKey))]
public class LegacyPreMigrationDocument
{
    [AutoIncrement]
    public long Id { get; set; }

    [Index]
    public long FilestoreId { get; set; }

    [Alias("user"), Index]
    public string? User { get; set; }

    [Index]
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>SHA256-based cache filename, e.g. "2388...72ef.pdf"</summary>
    public string? Filename { get; set; }
    /// <summary>Cache url, e.g. "/~cache/23/2388...72ef.pdf"</summary>
    public string? Url { get; set; }
    [Index]
    public string? Hash { get; set; }
    public long? Size { get; set; }

    /// <summary>Original filename (also the Gemini document's displayName)</summary>
    public string? DisplayName { get; set; }
    /// <summary>Gemini resource name, e.g. "fileSearchStores/my-docs-xxx/documents/yyy"</summary>
    public string? Name { get; set; }
    /// <summary>Gemini CustomMetadata[] as stored by the sync: [{"key":"id","numeric_value":1},...]</summary>
    public string? CustomMetadata { get; set; }    // JSON

    public string? CreateTime { get; set; }
    public string? UpdateTime { get; set; }
    public long? SizeBytes { get; set; }
    public string? MimeType { get; set; }

    /// <summary>
    /// STATE_UNSPECIFIED | STATE_PENDING | STATE_ACTIVE | STATE_FAILED as reported by Gemini, plus the
    /// local-only sync verdicts MISSING_FROM_REMOTE, MISSING_METADATA, METADATA_MISMATCH, DUPLICATE_FILE.
    /// </summary>
    [Index]
    public string? State { get; set; }

    /// <summary>User-defined folder, surfaced in the UI and as a "category=" metadata_filter</summary>
    [Index]
    public string? Category { get; set; }
    public string? SourceUrl { get; set; }

    [Index]
    public long? SourceId { get; set; }
    /// <summary>Non-null source scope used by the portable unique constraint (SourceId ?? 0).</summary>
    [Default(0)]
    public long SourceScopeId { get; set; }
    [Index]
    public string? SourceKey { get; set; }
    public string? SourceEtag { get; set; }
    public string? ContentHash { get; set; }
    public string? MetadataHash { get; set; }
    public string? ExtractorVer { get; set; }
    public DateTime? TombstonedAt { get; set; }

    public string? CategoryPath { get; set; }       // JSON string[]
    [Index]
    public string? DocType { get; set; }
    [Index]
    public string? Status { get; set; }
    [Index]
    public string? Locale { get; set; }
    [Index]
    public string? Product { get; set; }
    public string? Versions { get; set; }           // JSON string[]
    public long? SourceUpdatedAt { get; set; }      // epoch seconds
    public string? Tags { get; set; }               // JSON string[]

    public DateTime? StartedAt { get; set; }
    public DateTime? UploadedAt { get; set; }

    /// <summary>Desired and completed signatures for the independent local Search index.</summary>
    [Index]
    public string? SearchHash { get; set; }
    public string? SearchIndexedHash { get; set; }
    public DateTime? SearchStartedAt { get; set; }
    public DateTime? SearchIndexedAt { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? SearchError { get; set; }
    public int? SearchRetries { get; set; }

    public string? Metadata { get; set; }          // JSON
    [StringLength(StringLengthAttribute.MaxText)]
    public string? Error { get; set; }
    public string? Ref { get; set; }
}
