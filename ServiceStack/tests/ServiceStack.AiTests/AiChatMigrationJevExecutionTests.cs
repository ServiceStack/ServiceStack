#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
namespace ServiceStack.AiTests;
public class AiChatMigrationJevExecutionTests
{
    static JsonObject Vector=>JsonNode.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"AiChatFixtures","jev-contract-vectors.json")))!["recipes"]!.AsArray().First(x=>x!["name"]!.GetValue<string>()=="support")!.AsObject().Clone();
    static HttpResponseMessage Response(string body,int status=200)=>new((HttpStatusCode)status){Content=new StringContent(body)};
    static JsonObject Payload()=>DecisionRecipeValidator.Compile(Vector["document"],Vector["document"]!["examples"]![0]!["input"]);
    static DecisionClient Client(AiChatMigrationTestHost host,Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> handler)
    {
        host.Feature.Providers["openrouter"]=new ChatProvider {Id="openrouter",ApiKey="fixture-router",Headers={["HTTP-Referer"]="https://fixture.example"}};
        return new DecisionClient(host.Feature,()=>new AiChatMigrationHttpHandler(handler));
    }
    [Test]
    public async Task Raw_decision_transport_uses_exact_endpoint_headers_one_attempt_and_no_chat_history()
    {
        using var host=new AiChatMigrationTestHost();var calls=0;var client=Client(host,async(request,token)=>{calls++;Assert.That(request.RequestUri!.ToString(),Is.EqualTo(DecisionClient.Endpoint));Assert.That(request.Method,Is.EqualTo(HttpMethod.Post));Assert.That(request.Headers.Authorization!.Parameter,Is.EqualTo("fixture-router"));Assert.That(request.Headers.GetValues("HTTP-Referer").Single(),Is.EqualTo("https://fixture.example"));Assert.That(JsonNode.DeepEquals(JsonNode.Parse(await request.Content!.ReadAsStringAsync(token)),Payload()),Is.True);return Response(Vector["response"]!.ToJsonString());});
        var (raw,answers)=await client.DecideAsync(Payload(),CancellationToken.None);Assert.That(calls,Is.EqualTo(1));Assert.That(JsonNode.DeepEquals(answers,Vector["answers"]),Is.True);Assert.That(host.Db.QueryThreads(new JsonObject(),"alice"),Is.Empty);
    }
    [TestCase(401),TestCase(402),TestCase(429),TestCase(400),TestCase(413),TestCase(302),TestCase(500)]
    public void Provider_errors_never_retry_paid_decisions(int status)
    {
        using var host=new AiChatMigrationTestHost();var calls=0;var client=Client(host,(_,_)=>{calls++;return Task.FromResult(Response("{}",status));});var error=Assert.ThrowsAsync<JevDecisionException>(async()=>await client.DecideAsync(Payload(),CancellationToken.None));Assert.That(error!.Status,Is.EqualTo(status));Assert.That(calls,Is.EqualTo(1));Assert.That(error.Message,Does.Not.Contain("fixture-router"));
    }
    [Test]
    public void Malformed_probabilities_retain_raw_locally_but_oversized_invalid_and_nonfinite_responses_fail_bounded()
    {
        using var host=new AiChatMigrationTestHost();var response=Vector["response"]!.AsObject();response["answers"]!["is_bug"]!["noul"]=2;
        var client=Client(host,(_,_)=>Task.FromResult(Response(response.ToJsonString())));Assert.That(Assert.ThrowsAsync<JevDecisionException>(async()=>await client.DecideAsync(Payload(),CancellationToken.None))!.Raw,Is.Not.Null);
        foreach(var body in new[]{"{broken","{\"value\":1e999}","["+new string(' ',DecisionClient.MaxResponseBytes)+"]"}){
            client=Client(host,(_,_)=>Task.FromResult(Response(body)));var error=Assert.ThrowsAsync<JevDecisionException>(async()=>await client.DecideAsync(Payload(),CancellationToken.None));Assert.That(error!.Raw,Is.Null);
        }
        host.Feature.Providers.Clear();Assert.That(Assert.ThrowsAsync<JevDecisionException>(async()=>await client.DecideAsync(Payload(),CancellationToken.None))!.Status,Is.EqualTo(503));
    }
    static JsonObject Submit(JevStore store,DecisionExecutor executor)
    {
        var doc=Vector["document"]!.AsObject();var inputs=doc["examples"]![0]!["input"]!;var run=store.Submit("submission",doc,inputs,Payload(),executor.Owner).Run;executor.Launch(store,run);return run;
    }
    static async Task Wait(Func<bool> predicate)
    {
        bool Ready(){try{return predicate();}catch(JevConflictException){return false;}}
        for(var i=0;i<200;i++){if(Ready())return;await Task.Delay(5);}Assert.That(Ready(),Is.True);
    }
    [Test]
    public async Task Cancellation_and_shutdown_win_over_late_responses_and_never_automatically_resubmit()
    {
        using var host=new AiChatMigrationTestHost();var calls=0;var response=new TaskCompletionSource<(JsonNode,JsonObject)>(TaskCreationOptions.RunContinuationsAsynchronously);var executor=new DecisionExecutor((_,_)=>{calls++;return response.Task;});var store=new JevStore(host.DirectoryPath);var run=Submit(store,executor);var id=run.GetString("id")!;
        await Wait(()=>store.Run(id)!.GetString("status")=="running");executor.Cancel(store,id);response.SetResult((Vector["response"]!,Vector["answers"]!.AsObject()));await executor.CloseAsync();Assert.That(store.Run(id)!.GetString("status"),Is.EqualTo("cancelled"));Assert.That(calls,Is.EqualTo(1));Assert.That(new JevStore(host.DirectoryPath).Run(id)!.GetString("status"),Is.EqualTo("cancelled"));
        response=new(TaskCreationOptions.RunContinuationsAsynchronously);executor=new DecisionExecutor((_,_)=>response.Task);store=new JevStore(Path.Combine(host.DirectoryPath,"shutdown"));run=Submit(store,executor);id=run.GetString("id")!;await Wait(()=>store.Run(id)!.GetString("status")=="running");await executor.CloseAsync();response.TrySetResult((Vector["response"]!,Vector["answers"]!.AsObject()));Assert.That(store.Run(id)!.GetString("status"),Is.EqualTo("interrupted"));
    }
    [Test]
    public async Task Execution_timeout_keeps_the_provider_request_single_and_preserves_failed_history()
    {
        using var host=new AiChatMigrationTestHost();var calls=0;var executor=new DecisionExecutor(async(_,token)=>{calls++;await Task.Delay(10000,token);return(Vector["response"]!,Vector["answers"]!.AsObject());}) {ExecutionTimeout=TimeSpan.FromMilliseconds(25)};
        var store=new JevStore(host.DirectoryPath);var run=Submit(store,executor);await Wait(()=>store.Run(run.GetString("id")!)!.GetString("status")=="failed");await executor.CloseAsync();Assert.That(calls,Is.EqualTo(1));Assert.That(store.History()["items"]!.AsArray(),Has.Count.EqualTo(1));
    }
    sealed class TextProvider(Func<JsonObject,ChatContext,Task<JsonObject>> send):ChatProvider
    {
        public override Task<JsonObject> ChatAsync(JsonObject chat,ChatContext context)=>send(chat,context);
    }
    static JsonObject Reply(string content)=>new(){["choices"]=new JsonArray(new JsonObject {["message"]=new JsonObject {["role"]="assistant",["content"]=content}}),["usage"]=new JsonObject {["prompt_tokens"]=1,["completion_tokens"]=2}};
    [Test]
    public async Task Authoring_drops_invented_examples_and_improvement_keeps_compatible_inputs_without_stale_expectations()
    {
        using var host=new AiChatMigrationTestHost();var ext=host.Install(new JevExtension());var original=Vector["document"]!.AsObject();var generated=original.Clone();generated["questions"]!["is_bug"]!["instructions"]="New instructions";var calls=0;
        host.Feature.Providers["text"]=new TextProvider((chat,context)=>{calls++;Assert.That(context.User,Is.EqualTo("alice"));Assert.That(context.ModelOnly&&context.NoStore&&context.NoHistory,Is.True);Assert.That(context.Tools,Is.EqualTo("none"));return Task.FromResult(Reply(generated.ToJsonString()));}){Id="text",Models={["text-model"]=new JsonObject {["id"]="text-model"}}};
        ext.Authoring.ReadAsset=_=>"Frozen create recipe prompt";var body=new JsonObject {["model"]="text-model",["goal"]="Improve support handling",["recipe"]=original.Clone()};
        var created=await ext.Authoring.GenerateAsync("alice",body,false,CancellationToken.None);Assert.That(created["recipe"]!["examples"]!.AsArray(),Is.Empty);
        var improved=await ext.Authoring.GenerateAsync("alice",body,true,CancellationToken.None);Assert.That(improved["recipe"]!["examples"]!.AsArray(),Has.Count.EqualTo(original["examples"]!.AsArray().Count));foreach(var example in improved["recipe"]!["examples"]!.AsArray())Assert.That(example!.AsObject().ContainsKey("expected"),Is.False);Assert.That(calls,Is.EqualTo(2));
        generated["schemaVersion"]=2;var invalid=await ext.Authoring.GenerateAsync("alice",body,false,CancellationToken.None);Assert.That(invalid["recipe"],Is.Null);Assert.That(invalid.GetString("diagnostic"),Does.Contain("version"));Assert.That(invalid.GetString("draft")!.Length,Is.LessThanOrEqualTo(10000));
    }
    [Test]
    public async Task Example_names_send_only_summary_input_answers_use_two_slots_and_never_change_history()
    {
        using var host=new AiChatMigrationTestHost();var ext=host.Install(new JevExtension());host.Feature.Config["defaults"]!["summarize"]=new JsonObject {["model"]="text-model",["tools"]=new JsonArray(),["metadata"]=new JsonObject {["private"]=true}};
        var active=0;var maximum=0;host.Feature.Providers["text"]=new TextProvider(async(chat,context)=>{
            Assert.That(chat.ContainsKey("metadata"),Is.False);Assert.That(chat.ContainsKey("tools"),Is.False);Assert.That(context.ModelOnly&&context.NoStore&&context.NoHistory,Is.True);
            var source=JsonNode.Parse(chat["messages"]![1]!["content"]!.GetValue<string>())!.AsObject();Assert.That(source.Select(p=>p.Key),Is.EquivalentTo(new[]{"recipe","input","answers"}));var count=Interlocked.Increment(ref active);maximum=Math.Max(maximum,count);await Task.Delay(25);Interlocked.Decrement(ref active);return Reply("Example name: \"  Specific  input result  \"");
        }){Id="text",Models={["text-model"]=new JsonObject {["id"]="text-model",["limit"]=new JsonObject {["context"]=4096}}}};
        var run=new JsonObject {["status"]="succeeded",["recipe"]=Vector["document"]!.DeepClone(),["input"]=Vector["document"]!["examples"]![0]!["input"]!.DeepClone(),["answers"]=Vector["answers"]!.DeepClone(),["response"]=new JsonObject {["private"]="raw"}};
        var before=run.ToJsonString();var names=await Task.WhenAll(Enumerable.Range(0,4).Select(_=>ext.Authoring.NameAsync("alice",run,CancellationToken.None)));Assert.That(maximum,Is.EqualTo(2));Assert.That(names.All(n=>n.GetString("label")=="Specific input result"),Is.True);Assert.That(run.ToJsonString(),Is.EqualTo(before));host.Feature.Config["defaults"]!["summarize"]=null;Assert.That((int)Assert.ThrowsAsync<HttpError>(async()=>await ext.Authoring.NameAsync("alice",run,CancellationToken.None))!.StatusCode,Is.EqualTo(503));
    }
}
