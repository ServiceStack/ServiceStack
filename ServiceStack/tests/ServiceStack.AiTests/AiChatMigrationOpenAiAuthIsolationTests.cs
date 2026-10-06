#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
namespace ServiceStack.AiTests;
public partial class AiChatMigrationOpenAiAuthTests
{
    [Test]
    public void Shared_picker_display_names_resolve_against_the_originating_account_catalog()
    {
        using var f=new Fixture();var provider=f.Provider;
        provider.Discover(new JsonArray(new JsonObject {["id"]="alice-model",["name"]="Same display name"}),"alice");
        provider.Discover(new JsonArray(new JsonObject {["id"]="bob-model",["name"]="Same display name"}),"bob");
        Assert.That(provider.ProviderModel("Same display name"),Is.Not.Null);
        var chat=f.Chat();chat["model"]="Same display name";
        Assert.That(provider.Payload(chat,"alice").GetString("model"),Is.EqualTo("alice-model"));
        Assert.That(provider.Payload(chat,"bob").GetString("model"),Is.EqualTo("bob-model"));
    }
    [Test]
    public async Task Simultaneous_user_flows_and_process_wide_refresh_keep_their_origin_identity()
    {
        using var f=new Fixture();f.Connect("alice");var alice=f.Authorization;var aliceUrl=f.Callback(code:"alice");f.Connect("bob");var bob=f.Authorization;var bobUrl=f.Callback(code:"bob");
        await f.Ext.Flows.Identity.VerifyAsync(f.Jwt(f.Claims()),"issued-client",null,CancellationToken.None);
        f.Override=async(request,token)=>{var form=Query("https://fixture.test/?"+await request.Content!.ReadAsStringAsync(token));var user=form["code"];var claims=f.Claims((user=="alice"?alice:bob)["nonce"],"account-"+user);claims["email"]=user+"@example.test";var tokens=f.Tokens();tokens["id_token"]=f.Jwt(claims);tokens["access_token"]=user+"-access";return Response(tokens.ToJsonString());};
        await Task.WhenAll(f.Ext.Flows.CallbackAsync("alice",aliceUrl,CancellationToken.None),f.Ext.Flows.CallbackAsync("bob",bobUrl,CancellationToken.None));Assert.That(f.Ext.Credentials.Load("alice").GetString("subject"),Is.EqualTo("account-alice"));Assert.That(f.Ext.Credentials.Load("bob").GetString("subject"),Is.EqualTo("account-bob"));
        f.Host.Clock.Now+=TimeSpan.FromMinutes(57);var otherStore=new OpenAiSubscriptionStore(f.Ext.Ctx);var other=new OpenAiSubscriptionFlow(f.Ext.Options,otherStore);var calls=0;f.Override=async(_,token)=>{Interlocked.Increment(ref calls);await Task.Delay(25,token);return Response(new JsonObject {["access_token"]="new-access",["refresh_token"]="new-refresh",["expires_in"]=3600,["scope"]=f.Scope}.ToJsonString());};
        await Task.WhenAll(f.Ext.Flows.ValidCredentialsAsync("alice",CancellationToken.None),other.ValidCredentialsAsync("alice",CancellationToken.None));Assert.That(calls,Is.EqualTo(1));Assert.That(otherStore.Load("alice").GetString("access_token"),Is.EqualTo("new-access"));other.Close();
    }
    [Test]
    public async Task Disconnect_during_model_listing_cannot_repopulate_account_cache()
    {
        using var f=new Fixture();f.Save();var started=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var released=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);f.Override=async(_,_)=>{started.SetResult();await released.Task;return Response("{\"models\":[{\"slug\":\"private\",\"visibility\":\"list\"}]}");};var listing=f.Ext.AccountModelsAsync("alice",CancellationToken.None);await started.Task;f.Ext.Flows.Disconnect("alice");released.SetResult();Assert.ThrowsAsync<HttpError>(async()=>await listing);Assert.ThrowsAsync<HttpError>(async()=>await f.Ext.AccountModelsAsync("alice",CancellationToken.None));
    }
    [Test]
    public void Disconnect_during_response_and_missing_access_token_stop_subscription_without_fallback()
    {
        using var f=new Fixture();f.Save();f.Override=(_,_)=>{f.Ext.Flows.Disconnect("alice");return Task.FromResult(Response(Stream()));};Assert.ThrowsAsync<ChatProviderRequestException>(async()=>await f.Provider.ChatAsync(f.Chat(),f.Context()));Assert.That(f.Ext.Credentials.Load("alice"),Is.Null);
        f.Save();var invalid=f.Ext.Credentials.Load("alice")!;invalid["access_token"]="";f.Ext.Credentials.Save("alice",invalid);Assert.ThrowsAsync<ChatProviderRequestException>(async()=>await f.Provider.ChatAsync(f.Chat(),f.Context()));
    }
    [TestCase("empty"),TestCase("array"),TestCase("oversized")]
    public void Manual_callback_body_shape_and_size_are_bounded(string kind)
    {
        using var f=new Fixture();JsonNode body=kind=="array"?new JsonArray():new JsonObject {["url_or_code"]=kind=="oversized"?new string('x',32769):""};Assert.ThrowsAsync<HttpError>(async()=>await f.Host.SendAsync("POST","/ext/openai_auth/callback_manual","alice",body));Assert.That(f.TokenCalls,Is.Zero);
    }
    [Test]
    public async Task Explicit_local_import_verifies_registration_and_remains_identity_scoped()
    {
        using var f=new Fixture();var tokens=f.Tokens();var access=f.Claims();access["scope"]=f.Scope;access["client_id"]="issued-client";tokens["access_token"]=f.Jwt(access);var imported=new JsonObject {["client_id"]="issued-client",["scope"]=f.Scope,["tokens"]=tokens};var path=Path.Combine(f.Host.DirectoryPath,"selected-registration.json");File.WriteAllText(path,imported.ToJsonString());f.Ext.Options.LocalCredentialsPath=user=>user=="alice"?path:null;f.Ext.Options.CanImportLocalCredentials=req=>req.UserName=="alice";
        var result=(JsonObject)(await f.Host.SendAsync("POST","/ext/openai_auth/import_codex","alice",new JsonObject()))!;Assert.That(result.GetBool("connected"),Is.True);Assert.That(f.Ext.Credentials.Load("alice").GetString("subject"),Is.EqualTo("account-alice"));Assert.ThrowsAsync<HttpError>(async()=>await f.Host.SendAsync("POST","/ext/openai_auth/import_codex","bob",new JsonObject()));Assert.That(f.Ext.Credentials.Load("bob"),Is.Null);
        imported["client_id"]="dynamic_agent_client";File.WriteAllText(path,imported.ToJsonString());Assert.ThrowsAsync<HttpError>(async()=>await f.Host.SendAsync("POST","/ext/openai_auth/import_codex","alice",new JsonObject()));
    }
    [Test]
    public void Credential_store_rejects_linked_ancestor_and_shutdown_expires_pending_flows()
    {
        using var f=new Fixture();f.Connect();f.Ext.Flows.Close();Assert.That(f.Ext.Flows.HasPending("alice"),Is.False);Assert.Throws<OperationCanceledException>(()=>f.Connect());
        if(OperatingSystem.IsWindows())return;var path=Path.GetDirectoryName(f.Ext.Credentials.PathFor("alice"))!;var outside=Path.Combine(f.Host.DirectoryPath,"outside");Directory.CreateDirectory(outside);Directory.CreateDirectory(Path.GetDirectoryName(path)!);Directory.CreateSymbolicLink(path,outside);Assert.Throws<InvalidOperationException>(()=>f.Save());Assert.That(Directory.GetFiles(outside),Is.Empty);
    }
    [TestCase("image"), TestCase("audio")]
    public async Task Subscription_disables_API_key_for_non_text_output_until_disconnect(string modality)
    {
        using var f = new Fixture();
        f.Save();
        var calls = 0;
        f.Host.Feature.Providers["openai"] = new FallbackProvider(() => calls++) { Id = "openai", ApiKey = "fixture-api" };
        var provider = f.Provider;
        var chat = f.Chat();
        chat["modalities"] = new JsonArray("text", modality);
        var error = Assert.ThrowsAsync<ChatProviderRequestException>(async () => await provider.ChatAsync(chat, f.Context()));
        Assert.That(error!.Message, Does.Contain("OpenAI API key is disabled"));
        Assert.That(calls, Is.Zero);
        Assert.That(f.ResponseCalls, Is.Zero);
        var status = (JsonObject)(await f.Host.SendAsync("GET", "/ext/openai_auth/status", "alice"))!;
        Assert.That(status.GetBool("has_api_key"), Is.True);
        Assert.That(status.GetBool("api_key_disabled"), Is.True);
        Assert.That(status.GetBool("api_key_active"), Is.False);
        await provider.ChatAsync(chat, f.Context("bob"));
        Assert.That(calls, Is.EqualTo(1));
        f.Ext.Flows.Disconnect("alice");
        await provider.ChatAsync(chat, f.Context());
        Assert.That(calls, Is.EqualTo(2));
        status = (JsonObject)(await f.Host.SendAsync("GET", "/ext/openai_auth/status", "alice"))!;
        Assert.That(status.GetBool("api_key_disabled"), Is.False);
        Assert.That(status.GetBool("api_key_active"), Is.True);
        Assert.That(provider.BaseProvider!.ApiKey, Is.EqualTo("fixture-api"));
    }
}
