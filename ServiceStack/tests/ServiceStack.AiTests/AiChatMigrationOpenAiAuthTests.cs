#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Host;
namespace ServiceStack.AiTests;

[NonParallelizable]
public partial class AiChatMigrationOpenAiAuthTests
{
    static Dictionary<string,string> Query(string url)=>new Uri(url).Query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Split('=',2)).ToDictionary(x=>Uri.UnescapeDataString(x[0]),x=>Uri.UnescapeDataString(x.Length==2?x[1]:""));
    static HttpResponseMessage Response(string body,int status=200)=>new((HttpStatusCode)status){Content=new StringContent(body,Encoding.UTF8,"application/json")};
    static string Event(JsonObject value)=>"data: "+value.ToJsonString()+"\n\n";
    static string Stream()=>Event(new(){["type"]="response.created",["response"]=new JsonObject {["id"]="resp-fixture",["model"]="gpt-5.5",["created_at"]=100}})
        +Event(new(){["type"]="response.output_text.delta",["delta"]="Hello"})+Event(new(){["type"]="response.reasoning_summary_text.delta",["delta"]="Consider"})
        +Event(new(){["type"]="response.completed",["response"]=new JsonObject {["status"]="completed",["usage"]=new JsonObject {["input_tokens"]=5,["output_tokens"]=7,["input_tokens_details"]=new JsonObject {["cached_tokens"]=2},["output_tokens_details"]=new JsonObject {["reasoning_tokens"]=3}}}});
    sealed class Fixture:IDisposable
    {
        public AiChatMigrationTestHost Host {get;}
        public OpenAiAuthExtension Ext {get;}
        readonly RSA rsa=RSA.Create(2048);
        public Dictionary<string,string> Authorization=new();
        public Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>>? Override;
        public Action<JsonObject>? ChangeClaims;
        public string Scope="openid chatgpt.tokens.use.direct";
        public int TokenCalls,ResponseCalls,ModelCalls;
        public List<Dictionary<string,string>> Forms=new();
        public List<string?> Bearers=new();
        public Fixture(string prefix="")
        {
            Host=new(prefix);Ext=Host.Install(new OpenAiAuthExtension {Options=new(){Clock=Host.Clock,HttpHandlerFactory=()=>new AiChatMigrationHttpHandler(Send)}});
        }
        public JsonObject Claims(string nonce="",string sub="account-alice")=>new(){["iss"]=Ext.Options.Issuer,["aud"]="issued-client",["sub"]=sub,["exp"]=Host.Clock.Now.ToUnixTimeSeconds()+3600,["nonce"]=nonce,["email"]="alice@example.test",["name"]="Alice",["https://api.openai.com/auth"]=new JsonObject {["chatgpt_account_id"]=sub,["chatgpt_plan_type"]="plus"}};
        public string Jwt(JsonObject claims,string alg="RS256",string kid="fixture")
        {
            var header=OpenAiSubscriptionIdentity.Base64Url(Encoding.UTF8.GetBytes(new JsonObject {["alg"]=alg,["kid"]=kid}.ToJsonString()));var body=OpenAiSubscriptionIdentity.Base64Url(Encoding.UTF8.GetBytes(claims.ToJsonString()));var input=header+"."+body;
            return input+"."+OpenAiSubscriptionIdentity.Base64Url(rsa.SignData(Encoding.ASCII.GetBytes(input),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1));
        }
        public JsonObject Tokens(bool refresh=false)
        {
            var claims=Claims(Authorization.GetValueOrDefault("nonce")??"");ChangeClaims?.Invoke(claims);
            return new(){["access_token"]=refresh?"rotated-access":"alice-access",["refresh_token"]=refresh?"rotated-refresh":"alice-refresh",["id_token"]=Jwt(claims),["expires_in"]=3600,["scope"]=Scope};
        }
        async Task<HttpResponseMessage> Send(HttpRequestMessage request,CancellationToken token)
        {
            if(Override!=null)return await Override(request,token);
            if(request.RequestUri!.AbsolutePath.EndsWith("/oauth/token"))
            {
                TokenCalls++;var form=Query("https://fixture.test/?"+await request.Content!.ReadAsStringAsync(token));Forms.Add(form);return Response(Tokens(form["grant_type"]=="refresh_token").ToJsonString());
            }
            if(request.RequestUri.AbsolutePath.EndsWith("/jwks.json"))
            {
                var key=rsa.ExportParameters(false);return Response(new JsonObject {["keys"]=new JsonArray(new JsonObject {["kid"]="fixture",["kty"]="RSA",["alg"]="RS256",["use"]="sig",["n"]=OpenAiSubscriptionIdentity.Base64Url(key.Modulus!),["e"]=OpenAiSubscriptionIdentity.Base64Url(key.Exponent!)})}.ToJsonString());
            }
            if(request.RequestUri.AbsolutePath.EndsWith("/models"))
            {
                ModelCalls++;Bearers.Add(request.Headers.Authorization?.Parameter);return Response("{\"models\":[{\"slug\":\"visible-first\",\"display_name\":\"First\",\"visibility\":\"list\"},{\"slug\":\"hidden\",\"visibility\":\"hidden\"},{\"slug\":\"visible-second\",\"visibility\":\"list\"}]}");
            }
            ResponseCalls++;Bearers.Add(request.Headers.Authorization?.Parameter);return Response(Stream());
        }
        public JsonObject Connect(string user="alice")
        {var result=Ext.Flows.Connect(user);Authorization=Query(result.GetString("auth_url")!);return result;}
        public string Callback(string? state=null,string client="issued-client",string code="fixture-code")=>Ext.Options.RedirectUri+"?code="+code+"&state="+Uri.EscapeDataString(state??Authorization["state"])+"&client_id="+client;
        public async Task Grant(string user="alice") {Connect(user);await Ext.Flows.CallbackAsync(user,Callback(),CancellationToken.None);Ext.Activate();}
        public void Save(string user="alice",long expires=3600,string token="alice-access")=>Ext.Credentials.Save(user,new JsonObject {["client_id"]="issued-client",["subject"]="account-"+user,["access_token"]=token,["refresh_token"]="fixture-refresh",["scope"]=Scope,["expires_at"]=Host.Clock.Now.ToUnixTimeSeconds()+expires,["account_id"]="account-"+user,["account"]=new JsonObject {["email"]=user+"@example.test"}});
        public OpenAiSubscriptionProvider Provider {get{Ext.Activate();return (OpenAiSubscriptionProvider)Host.Feature.Providers["openai"];}}
        public ChatContext Context(string user="alice",CancellationToken token=default)=>new(){User=user,ModelOnly=true,NoStore=true,NoHistory=true,Tools="none",CancellationToken=token,Request=new BasicRequest()};
        public JsonObject Chat()=>new(){["model"]="gpt-5.5",["messages"]=new JsonArray(new JsonObject {["role"]="user",["content"]="Hi"})};
        public void Dispose(){Host.Dispose();rsa.Dispose();}
    }
    [TestCase(""),TestCase("/chat")]
    public async Task Routes_use_origin_identity_manual_capability_and_do_not_expose_tokens(string prefix)
    {
        using var f=new Fixture(prefix);var root=prefix+"/ext/openai_auth/";Assert.That(((ChatResult)(await f.Host.SendAsync("GET",root+"status",null))!).Status,Is.EqualTo(401));
        var connected=(JsonObject)(await f.Host.SendAsync("POST",root+"connect","alice",new JsonObject()))!;f.Authorization=Query(connected.GetString("auth_url")!);
        Assert.That(connected.GetBool("manual_callback"),Is.True);Assert.That(connected.GetBool("automatic_callback"),Is.False);
        await f.Host.SendAsync("POST",root+"callback_manual","alice",new JsonObject {["url_or_code"]=f.Callback()});
        var status=(JsonObject)(await f.Host.SendAsync("GET",root+"status","alice"))!;Assert.That(status.GetBool("connected"),Is.True);Assert.That(status.GetString("email"),Is.EqualTo("alice@example.test"));Assert.That(status.ToJsonString(),Does.Not.Contain("alice-access").And.Not.Contain("alice-refresh").And.Not.Contain("id_token"));
        Assert.That(((JsonObject)(await f.Host.SendAsync("GET",root+"status","bob"))!).GetBool("connected"),Is.False);Assert.That(status.GetBool("has_codex_auth"),Is.False);
        await f.Host.SendAsync("POST",root+"disconnect","bob",new JsonObject());Assert.That(f.Ext.Credentials.Load("alice"),Is.Not.Null);
        await f.Host.SendAsync("POST",root+"disconnect","alice",new JsonObject());Assert.That(f.Ext.Credentials.Load("alice"),Is.Null);
    }
    [Test]
    public async Task Dynamic_registration_pkce_resource_host_identity_and_private_atomic_grant_are_preserved()
    {
        using var f=new Fixture();f.Connect();var auth=f.Authorization;Assert.That(auth["client_id"],Is.EqualTo("dynamic_agent_client"));Assert.That(auth["code_challenge_method"],Is.EqualTo("S256"));Assert.That(auth["nonce"],Has.Length.GreaterThan(40));Assert.That(auth["scope"],Does.Contain("chatgpt.tokens.use.direct"));Assert.That(auth["resource"],Is.EqualTo("https://api.openai.com/v1"));
        await f.Ext.Flows.CallbackAsync("alice",f.Callback(),CancellationToken.None);var form=f.Forms.Single();Assert.That(form["client_id"],Is.EqualTo("issued-client"));Assert.That(form["resource"],Is.EqualTo(auth["resource"]));Assert.That(OpenAiSubscriptionIdentity.Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(form["code_verifier"]))),Is.EqualTo(auth["code_challenge"]));
        var path=f.Ext.Credentials.PathFor("alice");if(!OperatingSystem.IsWindows())Assert.That(File.GetUnixFileMode(path),Is.EqualTo(UnixFileMode.UserRead|UnixFileMode.UserWrite));Assert.That(Directory.GetFiles(Path.GetDirectoryName(path)!,"*.tmp"),Is.Empty);
        var host=f.Ext.Credentials.HostId();f.Connect();Assert.That(f.Authorization["client_id"],Is.EqualTo("issued-client"));Assert.That(f.Authorization.ContainsKey("agent_name_hint"),Is.False);Assert.That(f.Authorization.ContainsKey("id_token_hint"),Is.False);Assert.That(f.Authorization["ext_agent_host_id"],Is.EqualTo(host));
    }
    [TestCase("wrong-state"),TestCase("other-user"),TestCase("wrong-host"),TestCase("wrong-port"),TestCase("wrong-path"),TestCase("fragment"),TestCase("duplicate"),TestCase("bare-code"),TestCase("expired")]
    public void Invalid_callback_is_rejected_before_token_exchange(string kind)
    {
        using var f=new Fixture();f.Connect();var url=f.Callback();var user="alice";
        switch(kind){case "wrong-state":url=f.Callback("wrong");break;case "other-user":user="bob";break;case "wrong-host":url=url.Replace("127.0.0.1","localhost");break;case "wrong-port":url=url.Replace(":1455",":1456");break;case "wrong-path":url=url.Replace("/auth/callback","/different");break;case "fragment":url+="#state";break;case "duplicate":url+="&state=second";break;case "bare-code":url="fixture-code";break;case "expired":f.Host.Clock.Now+=TimeSpan.FromMinutes(11);break;}
        Assert.ThrowsAsync<HttpError>(async()=>await f.Ext.Flows.CallbackAsync(user,url,CancellationToken.None));Assert.That(f.TokenCalls,Is.Zero);Assert.That(f.Ext.Credentials.Load("alice"),Is.Null);
    }
    [TestCase("nonce"),TestCase("issuer"),TestCase("audience"),TestCase("expired"),TestCase("not-before"),TestCase("subject"),TestCase("azp"),TestCase("scope")]
    public void Invalid_identity_and_ungranted_plan_scope_never_persist(string kind)
    {
        using var f=new Fixture();f.Connect();f.ChangeClaims=claims=>{switch(kind){case "nonce":claims["nonce"]="other";break;case "issuer":claims["iss"]="https://evil.example";break;case "audience":claims["aud"]="other-client";break;case "expired":claims["exp"]=f.Host.Clock.Now.ToUnixTimeSeconds();break;case "not-before":claims["nbf"]=f.Host.Clock.Now.ToUnixTimeSeconds()+60;break;case "subject":claims["sub"]="";break;case "azp":claims["aud"]=new JsonArray("issued-client","other");claims["azp"]="other";break;}};if(kind=="scope")f.Scope="openid profile";
        Assert.ThrowsAsync<HttpError>(async()=>await f.Ext.Flows.CallbackAsync("alice",f.Callback(),CancellationToken.None));Assert.That(f.Ext.Credentials.Load("alice"),Is.Null);Assert.That(f.Ext.Flows.HasPending("alice"),Is.False);
    }
    [TestCase("HS256","fixture"),TestCase("RS256","unknown")]
    public void Unexpected_jwt_algorithm_or_key_fails(string alg,string kid)
    {
        using var f=new Fixture();Assert.ThrowsAsync<HttpError>(async()=>await f.Ext.Flows.Identity.VerifyAsync(f.Jwt(f.Claims(),alg,kid),"issued-client",null,CancellationToken.None));
    }
    [Test]
    public async Task Signature_tampering_callback_replay_and_cross_account_reauthorization_fail()
    {
        using var f=new Fixture();var jwt=f.Jwt(f.Claims());var parts=jwt.Split('.');var changed=f.Claims();changed["sub"]="evil";parts[1]=OpenAiSubscriptionIdentity.Base64Url(Encoding.UTF8.GetBytes(changed.ToJsonString()));Assert.ThrowsAsync<HttpError>(async()=>await f.Ext.Flows.Identity.VerifyAsync(string.Join('.',parts),"issued-client",null,CancellationToken.None));
        await f.Grant();var prior=f.Ext.Credentials.Load("alice")!.ToJsonString();Assert.ThrowsAsync<HttpError>(async()=>await f.Ext.Flows.CallbackAsync("alice",f.Callback(),CancellationToken.None));Assert.That(f.TokenCalls,Is.EqualTo(1));
        f.Connect();f.ChangeClaims=c=>c["sub"]="account-bob";Assert.ThrowsAsync<HttpError>(async()=>await f.Ext.Flows.CallbackAsync("alice",f.Callback(),CancellationToken.None));Assert.That(f.Ext.Credentials.Load("alice")!.ToJsonString(),Is.EqualTo(prior));
    }
    [TestCase(false),TestCase(true)]
    public async Task Disconnect_or_new_login_wins_over_late_authorization_exchange(bool newLogin)
    {
        using var f=new Fixture();f.Connect();var original=f.Callback();var reply=f.Tokens().ToJsonString();var started=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // Cache real fixture keys before pausing only the token response.
        await f.Ext.Flows.Identity.VerifyAsync(f.Jwt(f.Claims()),"issued-client",null,CancellationToken.None);
        f.Override=async(_,_)=>{started.SetResult();await release.Task;return Response(reply);};var callback=f.Ext.Flows.CallbackAsync("alice",original,CancellationToken.None);await started.Task;
        if(newLogin)f.Connect();else f.Ext.Flows.Disconnect("alice");release.SetResult();Assert.ThrowsAsync<HttpError>(async()=>await callback);Assert.That(f.Ext.Credentials.Load("alice"),Is.Null);
    }
    [Test]
    public async Task Concurrent_refresh_serializes_rotation_and_disconnect_prevents_resurrection()
    {
        using var f=new Fixture();await f.Grant();f.Host.Clock.Now+=TimeSpan.FromMinutes(57);var initial=f.TokenCalls;var tasks=Enumerable.Range(0,12).Select(_=>f.Ext.Flows.ValidCredentialsAsync("alice",CancellationToken.None)).ToArray();var grants=await Task.WhenAll(tasks);Assert.That(f.TokenCalls,Is.EqualTo(initial+1));Assert.That(grants.All(g=>g.GetString("access_token")=="rotated-access"),Is.True);Assert.That(f.Forms.Last()["refresh_token"],Is.EqualTo("alice-refresh"));Assert.That(f.Ext.Credentials.Load("alice").GetString("refresh_token"),Is.EqualTo("rotated-refresh"));
        f.Host.Clock.Now+=TimeSpan.FromMinutes(57);var started=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);f.Override=async(_,_)=>{started.SetResult();await release.Task;return Response(f.Tokens(true).ToJsonString());};var refresh=f.Ext.Flows.ValidCredentialsAsync("alice",CancellationToken.None);await started.Task;f.Ext.Flows.Disconnect("alice");release.SetResult();Assert.ThrowsAsync<HttpError>(async()=>await refresh);Assert.That(f.Ext.Credentials.Load("alice"),Is.Null);
    }
    [Test]
    public async Task Expired_grants_never_fall_back_to_operator_credentials_and_missing_grants_can_use_api_key()
    {
        using var f=new Fixture();var calls=0;f.Host.Feature.Providers["openai"]=new FallbackProvider(()=>calls++){Id="openai",ApiKey="fixture-api"};f.Save(expires:-1);var expired=f.Ext.Credentials.Load("alice")!;expired["refresh_token"]=null;f.Ext.Credentials.Save("alice",expired);
        Assert.ThrowsAsync<ChatProviderRequestException>(async()=>await f.Provider.ChatAsync(f.Chat(),f.Context()));Assert.That(calls,Is.Zero);
        await f.Provider.ChatAsync(f.Chat(),f.Context("bob"));Assert.That(calls,Is.EqualTo(1));Assert.That(f.Bearers,Is.Empty);Assert.That(f.Provider.Headers,Is.Empty);
    }
    sealed class FallbackProvider(Action called):ChatProvider
    {
        public FallbackProvider():this(()=>{}){}
        public override Task<JsonObject> ChatAsync(JsonObject chat,ChatContext context){called();return Task.FromResult(new JsonObject {["choices"]=new JsonArray(new JsonObject {["message"]=new JsonObject {["role"]="assistant",["content"]="API"}})});}
        public override string? Validate()=>null;
        public override string? ProviderModel(string model)=>model;
        public override JsonObject? ModelInfo(string model)=>new(){["id"]=model};
        // A host API key is only the fallback provider's configuration.
        public new string? ApiKey {get=>base.ApiKey;set=>base.ApiKey=value;}
    }
    [Test]
    public async Task Earliest_refresh_does_not_refresh_valid_grant_and_refresh_failure_is_not_hidden_when_expired()
    {
        using var f=new Fixture();f.Save(expires:100);var grant=f.Ext.Credentials.Load("alice")!;grant["earliest_refresh_at"]=f.Host.Clock.Now.ToUnixTimeSeconds()+120;f.Ext.Credentials.Save("alice",grant);await f.Ext.Flows.ValidCredentialsAsync("alice",CancellationToken.None);Assert.That(f.TokenCalls,Is.Zero);
        f.Host.Clock.Now+=TimeSpan.FromSeconds(121);f.Override=(_,_)=>Task.FromResult(Response("{\"secret\":\"fixture-refresh\"}",401));var error=Assert.ThrowsAsync<HttpError>(async()=>await f.Ext.Flows.ValidCredentialsAsync("alice",CancellationToken.None));Assert.That(error!.Message,Does.Not.Contain("fixture-refresh"));
    }
    [Test]
    public async Task Reload_keeps_subscription_only_users_without_API_key_or_shared_authentication_headers()
    {
        using var f=new Fixture();f.Save();await f.Ext.LoadAsync(f.Ext.Ctx);Assert.That(f.Host.Feature.Providers["openai"],Is.TypeOf<OpenAiSubscriptionProvider>());f.Host.Feature.Providers.Clear();await f.Ext.ReloadProvidersAsync(f.Ext.Ctx);Assert.That(f.Provider.ApiKey,Is.Null);Assert.That(f.Provider.Headers,Is.Empty);Assert.That(f.Ext.Credentials.Load("bob"),Is.Null);
    }
    [Test]
    public async Task Account_catalog_is_scoped_cached_cloned_and_retains_server_order_and_visibility()
    {
        using var f=new Fixture();f.Save();f.Save("bob",token:"bob-access");var alice=await f.Ext.AccountModelsAsync("alice",CancellationToken.None);Assert.That(alice.Select(x=>x!["id"]!.GetValue<string>()),Is.EqualTo(new[]{"visible-first","visible-second"}));alice.Clear();Assert.That(await f.Ext.AccountModelsAsync("alice",CancellationToken.None),Has.Count.EqualTo(2));await f.Ext.AccountModelsAsync("bob",CancellationToken.None);Assert.That(f.Bearers,Is.EqualTo(new[]{"alice-access","bob-access"}));Assert.That(f.ModelCalls,Is.EqualTo(2));f.Ext.Flows.Disconnect("alice");Assert.ThrowsAsync<HttpError>(async()=>await f.Ext.AccountModelsAsync("alice",CancellationToken.None));
    }
    [Test]
    public void Local_operator_import_is_disabled_without_both_host_resolver_and_identity_authorization()
    {
        using var f=new Fixture();Assert.ThrowsAsync<HttpError>(async()=>await f.Host.SendAsync("POST","/ext/openai_auth/import_codex","alice",new JsonObject()));f.Ext.Options.LocalCredentialsPath=_=>"/does/not/exist";Assert.ThrowsAsync<HttpError>(async()=>await f.Host.SendAsync("POST","/ext/openai_auth/import_codex","alice",new JsonObject()));
    }
    [TestCase(302),TestCase(401),TestCase(429),TestCase(500)]
    public void Token_errors_are_redacted_and_never_automatically_exchanged_twice(int status)
    {
        using var f=new Fixture();f.Connect();var calls=0;f.Override=(_,_)=>{calls++;return Task.FromResult(Response("alice-refresh very-secret",status));};var error=Assert.ThrowsAsync<HttpError>(async()=>await f.Ext.Flows.CallbackAsync("alice",f.Callback(),CancellationToken.None));Assert.That(error!.Message,Does.Not.Contain("very-secret").And.Not.Contain("alice-refresh"));Assert.That(calls,Is.EqualTo(1));Assert.That(f.Ext.Flows.HasPending("alice"),Is.False);
    }
    [TestCase("invalid"),TestCase("oversized"),TestCase("nonfinite")]
    public void Credential_json_is_bounded_and_strict(string kind)
    {
        using var f=new Fixture();var body=kind=="invalid"?"{broken":kind=="oversized"?new string(' ',1024*1024+1):"{\"value\":1e999}";f.Override=(_,_)=>Task.FromResult(Response(body));Assert.ThrowsAsync<HttpError>(async()=>await new OpenAiSubscriptionHttp(f.Ext.Options).GetAsync(f.Ext.Options.ModelsUrl,CancellationToken.None));
    }
    [Test]
    public async Task Credential_timeout_and_caller_cancellation_remain_distinct()
    {
        using var f=new Fixture();f.Ext.Options.TokenTimeout=TimeSpan.FromMilliseconds(20);f.Override=async(_,token)=>{await Task.Delay(10000,token);return Response("{}");};Assert.That((int)Assert.ThrowsAsync<HttpError>(async()=>await new OpenAiSubscriptionHttp(f.Ext.Options).GetAsync(f.Ext.Options.ModelsUrl,CancellationToken.None))!.StatusCode,Is.EqualTo(504));using var cancel=new CancellationTokenSource();cancel.Cancel();Assert.CatchAsync<OperationCanceledException>(async()=>await new OpenAiSubscriptionHttp(f.Ext.Options).GetAsync(f.Ext.Options.ModelsUrl,cancel.Token));await Task.CompletedTask;
    }
    [Test]
    public async Task Responses_translate_text_reasoning_usage_and_zero_cost_without_mutating_chat_or_history()
    {
        using var f=new Fixture();f.Save();var chat=f.Chat();var original=chat.ToJsonString();var ctx=f.Context();var result=await f.Provider.ChatAsync(chat,ctx);Assert.That(result["choices"]![0]!["message"]!["content"]!.GetValue<string>(),Is.EqualTo("Hello"));Assert.That(result["choices"]![0]!["message"]!["reasoning_content"]!.GetValue<string>(),Is.EqualTo("Consider"));Assert.That(result["usage"]!["total_tokens"]!.GetValue<long>(),Is.EqualTo(12));Assert.That(result.GetDouble("cost"),Is.Zero);Assert.That(result.GetObject("metadata").GetString("pricing"),Is.EqualTo("0/0"));Assert.That(f.Bearers,Is.EqualTo(new[]{"alice-access"}));Assert.That(chat.ToJsonString(),Is.EqualTo(original));Assert.That(f.Host.Db.QueryThreads(new JsonObject(),"alice"),Is.Empty);
    }
    [Test]
    public void Payload_preserves_atomic_tool_units_and_clones_multimodal_inputs()
    {
        using var f=new Fixture();var messages=ChatJson.ParseObject("""{"messages":[{"role":"system","content":"rules"},{"role":"user","content":[{"type":"text","text":"hi"},{"type":"image_url","image_url":{"url":"data:image/png;base64,AA=="}}]},{"role":"assistant","content":null,"tool_calls":[{"id":"call-one","type":"function","function":{"name":"calc","arguments":"{}"}}]},{"role":"tool","tool_call_id":"call-one","content":{"value":2}}],"tools":[{"type":"function","function":{"name":"calc","strict":true,"parameters":{"type":"object"}}}],"model":"openai/gpt-5.5","reasoning_effort":"high","tool_choice":{"type":"function","function":{"name":"calc"}}}""");var before=messages.ToJsonString();var payload=f.Provider.Payload(messages);Assert.That(payload.GetBool("store"),Is.False);Assert.That(payload.GetBool("stream"),Is.True);Assert.That(payload["input"]![1]!["content"]![1]!["type"]!.GetValue<string>(),Is.EqualTo("input_image"));Assert.That(payload["input"]![2]!["call_id"]!.GetValue<string>(),Is.EqualTo("call-one"));Assert.That(payload["input"]![3]!["call_id"]!.GetValue<string>(),Is.EqualTo("call-one"));Assert.That(payload["tools"]![0]!["strict"]!.GetValue<bool>(),Is.True);Assert.That(payload["reasoning"]!["effort"]!.GetValue<string>(),Is.EqualTo("high"));Assert.That(messages.ToJsonString(),Is.EqualTo(before));
    }
    [TestCase("response.failed"),TestCase("response.incomplete"),TestCase("error"),TestCase("eof"),TestCase("malformed")]
    public void Uncertain_streams_are_never_retried_by_chat_orchestrator_or_failed_over(string type)
    {
        using var f=new Fixture();f.Save();var calls=0;f.Override=(_,_)=>{calls++;return Task.FromResult(Response(type=="eof"?Event(new(){["type"]="response.output_text.delta",["delta"]="partial"}):type=="malformed"?"data: {bad\n\n":Event(new(){["type"]=type,["code"]="subscription_sharing_usage_limit_exceeded"})));};f.Host.Feature.Limits.Retries=3;_ = f.Provider;
        var error=Assert.ThrowsAsync<ChatProviderRequestException>(async()=>await f.Host.Feature.ChatCompletionAsync(f.Chat(),f.Context()));Assert.That(error!.Message,Does.Not.Contain("alice-access"));Assert.That(calls,Is.EqualTo(1));
    }
    [TestCase(false),TestCase(true)]
    public async Task Unauthorized_responses_refresh_once_and_do_not_retry_a_second_rejection(bool rejectAgain)
    {
        using var f=new Fixture();await f.Grant();var calls=0;f.Override=async(request,token)=>{
            if(request.RequestUri!.AbsolutePath.EndsWith("/oauth/token")){f.TokenCalls++;var form=Query("https://fixture.test/?"+await request.Content!.ReadAsStringAsync(token));Assert.That(form["client_id"],Is.EqualTo("issued-client"));return Response(f.Tokens(true).ToJsonString());}
            calls++;return Response(Stream(),calls==1||rejectAgain?401:200);
        };
        if(rejectAgain)Assert.ThrowsAsync<ChatProviderRequestException>(async()=>await f.Host.Feature.ChatCompletionAsync(f.Chat(),f.Context()));else await f.Host.Feature.ChatCompletionAsync(f.Chat(),f.Context());Assert.That(calls,Is.EqualTo(2));Assert.That(f.TokenCalls,Is.EqualTo(2));
    }
    [Test]
    public async Task Function_argument_deltas_and_done_produce_one_atomic_call()
    {
        using var f=new Fixture();f.Save();var stream=Event(new(){["type"]="response.output_item.added",["output_index"]=1,["item"]=new JsonObject {["type"]="function_call",["call_id"]="call-one",["name"]="calc",["arguments"]=""}})+Event(new(){["type"]="response.function_call_arguments.delta",["output_index"]=1,["delta"]="{\"a\":"})+Event(new(){["type"]="response.function_call_arguments.done",["output_index"]=1,["arguments"]="{\"a\":2}"})+Event(new(){["type"]="response.completed",["response"]=new JsonObject {["status"]="completed"}});f.Override=(_,_)=>Task.FromResult(Response(stream));var result=await f.Provider.ChatAsync(f.Chat(),f.Context());Assert.That(result["choices"]![0]!["finish_reason"]!.GetValue<string>(),Is.EqualTo("tool_calls"));Assert.That(result["choices"]![0]!["message"]!["tool_calls"]![0]!["function"]!["arguments"]!.GetValue<string>(),Is.EqualTo("{\"a\":2}"));
    }
    [Test]
    public void Cancellation_stops_orchestration_without_a_retry()
    {
        using var f=new Fixture();f.Save();using var cancel=new CancellationTokenSource();var calls=0;f.Override=async(_,token)=>{calls++;cancel.Cancel();await Task.Delay(10000,token);return Response(Stream());};_ = f.Provider;Assert.CatchAsync<OperationCanceledException>(async()=>await f.Host.Feature.ChatCompletionAsync(f.Chat(),f.Context(token:cancel.Token)));Assert.That(calls,Is.EqualTo(1));
    }
}
