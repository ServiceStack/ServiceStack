#nullable enable
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
namespace ServiceStack.AiTests;
public class AiChatMigrationJevImportTests
{
    static string Recipe=>File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"AiChatFixtures","jev-contract-vectors.json"));
    static string Document=>JsonNode.Parse(Recipe)!["recipes"]![0]!["document"]!.ToJsonString();
    static HttpResponseMessage Response(string text,int code=200)=>new((HttpStatusCode)code){Content=new StringContent(text)};
    [Test]
    public void Publisher_aliases_normalize_and_credentials_and_unsafe_schemes_are_rejected()
    {
        var config=new JsonObject {["baseUrl"]="https://publisher.example"};
        foreach(var suffix in new[]{"",".json","/recipe.json"})Assert.That(DecisionImporter.Url(config,JsonValue.Create("https://publisher.example/d/ABC_1"+suffix+"?view=1#download")),Is.EqualTo("https://publisher.example/d/ABC_1.json"));
        foreach(var url in new[]{"file:///tmp/recipe.json","https://alice:secret@example.org/doc.json","https://example.org\\evil/doc.json","relative.json"})Assert.Throws<JevValidationException>(()=>DecisionImporter.Url(config,JsonValue.Create(url)));
    }
    [Test]
    public async Task Redirects_are_revalidated_anonymous_and_bounded_to_six_attempts()
    {
        using var host=new AiChatMigrationTestHost();var policies=0;var calls=0;host.Feature.ValidateDownloadUrl=url=>{policies++;Assert.That(url,Does.StartWith("https://fixture.example/"));};
        var importer=new DecisionImporter(host.Feature,()=>new AiChatMigrationHttpHandler((req,_)=>{calls++;Assert.That(req.Headers.Authorization,Is.Null);Assert.That(req.Headers.Contains("Cookie"),Is.False);var res=Response(calls==1?"":Document,calls==1?302:200);if(calls==1)res.Headers.Location=new Uri("/next.json",UriKind.Relative);else res.Content.Headers.ContentDisposition=new("attachment"){FileNameStar="Straße.json"};return Task.FromResult(res);}));
        var result=await importer.DownloadAsync("https://fixture.example/start",CancellationToken.None);Assert.That(result.GetString("filename"),Is.EqualTo("Straße.json"));Assert.That(calls,Is.EqualTo(2));Assert.That(policies,Is.EqualTo(2));
        calls=0;importer=new DecisionImporter(host.Feature,()=>new AiChatMigrationHttpHandler((_,_)=>{calls++;var res=Response("",302);res.Headers.Location=new Uri("/loop",UriKind.Relative);return Task.FromResult(res);}));Assert.ThrowsAsync<JevValidationException>(async()=>await importer.DownloadAsync("https://fixture.example/loop",CancellationToken.None));Assert.That(calls,Is.EqualTo(6));
    }
    [Test]
    public void Redirect_target_policy_prevents_the_second_request()
    {
        using var host=new AiChatMigrationTestHost();var calls=0;host.Feature.ValidateDownloadUrl=url=>{if(new Uri(url).Host!="fixture.example")throw new UnauthorizedAccessException("Host download policy");};
        var importer=new DecisionImporter(host.Feature,()=>new AiChatMigrationHttpHandler((_,_)=>{calls++;var res=Response("",307);res.Headers.Location=new Uri("https://blocked.example/private");return Task.FromResult(res);}));Assert.ThrowsAsync<UnauthorizedAccessException>(async()=>await importer.DownloadAsync("https://fixture.example/start",CancellationToken.None));Assert.That(calls,Is.EqualTo(1));
    }
    sealed class Chunked(string value):HttpContent
    {
        protected override bool TryComputeLength(out long length){length=0;return false;}
        protected override Task SerializeToStreamAsync(Stream stream,TransportContext? context)=>stream.WriteAsync(Encoding.UTF8.GetBytes(value)).AsTask();
    }
    [Test]
    public void Chunked_oversize_invalid_envelopes_and_nonfinite_JSON_are_rejected()
    {
        using var host=new AiChatMigrationTestHost();foreach(var body in new[]{new string(' ',DecisionRecipeValidator.MaxBytes+1),"{bad", "{\"document\":"+Document+"}","{\"schemaVersion\":1e999}"}){
            var importer=new DecisionImporter(host.Feature,()=>new AiChatMigrationHttpHandler((_,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new Chunked(body)})));Assert.ThrowsAsync<JevValidationException>(async()=>await importer.DownloadAsync("https://fixture.example/recipe.json",CancellationToken.None));
        }
    }
    [Test]
    public void Timeout_is_bounded_and_caller_cancellation_is_preserved()
    {
        using var host=new AiChatMigrationTestHost();var importer=new DecisionImporter(host.Feature,()=>new AiChatMigrationHttpHandler(async(_,token)=>{await Task.Delay(10000,token);return Response(Document);})){TotalTimeout=TimeSpan.FromMilliseconds(20)};
        Assert.ThrowsAsync<JevValidationException>(async()=>await importer.DownloadAsync("https://fixture.example/start",CancellationToken.None));using var cancelled=new CancellationTokenSource();cancelled.Cancel();Assert.CatchAsync<OperationCanceledException>(async()=>await importer.DownloadAsync("https://fixture.example/start",cancelled.Token));
    }
}
