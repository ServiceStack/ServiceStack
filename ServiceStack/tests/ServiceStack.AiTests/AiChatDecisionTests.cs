#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack.AiTests;

/// <summary>IChatClient.CreateDecisionAsync and POST /v1/decisions over OpenRouter's Decisions API</summary>
[NonParallelizable]
public class AiChatDecisionTests
{
    static CreateDecision Example() => new() {
        State = "Help! My payouts have been failing for 3 days.",
        Questions = {
            ["is_urgent"] = DecisionQuestion.Noul("Does this message convey urgency?", "Explicitly time-sensitive", "No urgency expressed"),
            ["department"] = DecisionQuestion.Choice("Which team should handle this?", new() {
                ["billing"] = "Payments, invoicing, refunds",
                ["technical"] = "Bugs, outages, integrations",
                ["sales"] = "Pricing, upgrades, new accounts",
            }),
            ["frustration"] = DecisionQuestion.Score("How frustrated is the customer?", "Calm", "Frustrated", "Very angry"),
        },
    };

    const string ExampleResponse = """
        {"id":"gen-decision-1","model":"typesafe/jev-1.13","provider":"TypeSafe",
         "answers":{
           "is_urgent":{"type":"noul","noul":0.91},
           "department":{"type":"choice","choice":"billing","confidence":0.84,"probabilities":{"billing":0.86,"technical":0.12,"sales":0.02}},
           "frustration":{"type":"score","score":1.6,"confidence":0.7,"probabilities":{"0":0.05,"1":0.3,"2":0.65}}},
         "usage":{"input_tokens":120,"output_tokens":9,"cost":0.00042}}
        """;

    static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    sealed class Recorder
    {
        public readonly List<(Uri Uri, string? Key, JsonObject Body)> Calls = [];
        public Func<HttpResponseMessage> Reply = () => Json(ExampleResponse);
        public Func<HttpMessageHandler> Handler => () => new AiChatMigrationHttpHandler(async (request, token) => {
            Calls.Add((request.RequestUri!, request.Headers.Authorization?.Parameter,
                JsonNode.Parse(await request.Content!.ReadAsStringAsync(token))!.AsObject()));
            var response = Reply();
            response.RequestMessage ??= request;
            return response;
        });
    }

    static (ChatFeature Feature, Recorder Remote) Create(string api = "https://openrouter.ai/api/v1")
    {
        var remote = new Recorder();
        var feature = new ChatFeature { DecisionHandlerFactory = remote.Handler };
        feature.Providers["openrouter"] = new ChatProvider { Id = "openrouter", Api = api, ApiKey = "router-key" };
        return (feature, remote);
    }

    [Test]
    public async Task Typed_client_sends_the_openrouter_request_and_returns_validated_typed_answers()
    {
        var (feature, remote) = Create();
        IChatClient client = new ChatClient(feature);

        var decision = await client.CreateDecisionAsync(Example());

        var call = remote.Calls.Single();
        Assert.That(call.Uri.ToString(), Is.EqualTo("https://openrouter.ai/api/alpha/decisions"));
        Assert.That(call.Key, Is.EqualTo("router-key"));
        Assert.That(JsonNode.DeepEquals(call.Body, JsonNode.Parse("""
            {"model":"~typesafe/jev-latest","state":"Help! My payouts have been failing for 3 days.",
             "questions":{
               "is_urgent":{"type":"noul","instructions":"Does this message convey urgency?","criteria":{"true":"Explicitly time-sensitive","false":"No urgency expressed"}},
               "department":{"type":"choice","instructions":"Which team should handle this?","criteria":{"billing":"Payments, invoicing, refunds","technical":"Bugs, outages, integrations","sales":"Pricing, upgrades, new accounts"}},
               "frustration":{"type":"score","instructions":"How frustrated is the customer?","criteria":["Calm","Frustrated","Very angry"]}}}
            """)), Is.True, call.Body.ToJsonString());

        Assert.That(decision.Id, Is.EqualTo("gen-decision-1"));
        Assert.That(decision.Model, Is.EqualTo("typesafe/jev-1.13"));
        Assert.That(decision.Provider, Is.EqualTo("TypeSafe"));
        Assert.That(decision.Noul("is_urgent"), Is.EqualTo(0.91));
        Assert.That(decision.Choice("department"), Is.EqualTo("billing"));
        Assert.That(decision.Answers["department"].Probabilities!["technical"], Is.EqualTo(0.12));
        Assert.That(decision.Score("frustration"), Is.EqualTo(1.6));
        // the scale legend is filled in from the submitted criteria
        Assert.That(decision.Answers["frustration"].Legend, Is.EqualTo(new Dictionary<string, string> { ["0"] = "Calm", ["1"] = "Frustrated", ["2"] = "Very angry" }));
        Assert.That(decision.Usage!.InputTokens, Is.EqualTo(120));
        Assert.That(decision.Usage.Cost, Is.EqualTo(0.00042));
        Assert.Throws<KeyNotFoundException>(() => decision.Choice("is_urgent"));
    }

    [Test]
    public async Task Structured_state_and_explicit_user_session_and_routing_are_forwarded_only_when_set()
    {
        var (feature, remote) = Create();
        var client = new ChatClient(feature);
        var request = Example();
        request.State = new Dictionary<string, object> { ["subject"] = "Payouts", ["body"] = "Failing for 3 days" };
        await client.CreateDecisionAsync(request);
        Assert.That(remote.Calls[0].Body["state"]!["subject"]!.GetValue<string>(), Is.EqualTo("Payouts"));
        foreach (var field in new[] { "user", "session_id", "provider" })
            Assert.That(remote.Calls[0].Body.ContainsKey(field), Is.False, field);

        request.User = "customer-42"; request.SessionId = "ticket-7";
        request.Provider = new() { ["sort"] = "latency" };
        request.State = JsonNode.Parse("""[{"from":"customer","text":"Help!"}]""")!;
        await client.CreateDecisionAsync(request);
        var body = remote.Calls[1].Body;
        Assert.That(body["user"]!.GetValue<string>(), Is.EqualTo("customer-42"));
        Assert.That(body["session_id"]!.GetValue<string>(), Is.EqualTo("ticket-7"));
        Assert.That(body["provider"]!["sort"]!.GetValue<string>(), Is.EqualTo("latency"));
        Assert.That(body["state"]![0]!["text"]!.GetValue<string>(), Is.EqualTo("Help!"));
    }

    [Test]
    public void Invalid_requests_are_rejected_before_any_paid_call()
    {
        var (feature, remote) = Create();
        var client = new ChatClient(feature);
        var invalid = new List<Action<CreateDecision>> {
            r => r.State = " ",
            r => r.Questions.Clear(),
            r => r.Questions["is_urgent"].Type = "boolean",
            r => r.Questions["is_urgent"].Criteria = new Dictionary<string, string> { ["true"] = "Urgent" },
            r => r.Questions["department"].Criteria = new Dictionary<string, string>(),
            r => r.Questions["frustration"].Criteria = new List<string>(),
            r => r.Questions["frustration"].Instructions = "",
            r => r.User = new string('u', 257),
        };
        foreach (var change in invalid)
        {
            var request = Example(); change(request);
            var error = Assert.ThrowsAsync<HttpError>(async () => await client.CreateDecisionAsync(request));
            Assert.That(error!.Status, Is.EqualTo(400), error.Message);
        }
        Assert.That(remote.Calls, Is.Empty);
    }

    [TestCase(401, 502)] [TestCase(403, 502)] [TestCase(402, 402)] [TestCase(429, 429)] [TestCase(400, 400)] [TestCase(500, 502)] [TestCase(529, 502)]
    public void Provider_errors_are_mapped_and_never_retried(int providerStatus, int status)
    {
        var (feature, remote) = Create();
        remote.Reply = () => Json("{}", (HttpStatusCode)providerStatus);
        var error = Assert.ThrowsAsync<ChatDecisionException>(async () => await new ChatClient(feature).CreateDecisionAsync(Example()));
        Assert.That(error!.Status, Is.EqualTo(status));
        Assert.That(error.ProviderStatus, Is.EqualTo(providerStatus));
        Assert.That(error.Message, Does.Not.Contain("router-key"));
        Assert.That(remote.Calls, Has.Count.EqualTo(1));
    }

    [Test]
    public void Incomplete_answers_keep_the_raw_response_and_timeouts_are_not_replayed()
    {
        var (feature, remote) = Create();
        remote.Reply = () => Json(ExampleResponse.Replace("\"choice\":\"billing\"", "\"choice\":\"legal\""));
        var invalid = Assert.ThrowsAsync<ChatDecisionException>(async () => await new ChatClient(feature).CreateDecisionAsync(Example()));
        Assert.That(invalid!.Status, Is.EqualTo(502));
        Assert.That(invalid.Raw, Is.Not.Null);

        var calls = 0;
        feature.DecisionHandlerFactory = () => new AiChatMigrationHttpHandler(async (_, token) => {
            calls++;
            await Task.Delay(Timeout.Infinite, token);
            return Json(ExampleResponse);
        });
        var decisions = feature.Decisions;
        decisions.RequestTimeout = TimeSpan.FromMilliseconds(50);
        var timeout = Assert.ThrowsAsync<ChatDecisionException>(async () =>
            await decisions.CreateAsync(DecisionJson.ToJson(Example())));
        Assert.That(timeout!.Status, Is.EqualTo(504));
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void Unconfigured_openrouter_is_unavailable_and_the_endpoint_follows_its_api_root()
    {
        var (feature, remote) = Create();
        feature.Providers["openrouter"].ApiKey = null;
        Assert.That(Assert.ThrowsAsync<ChatDecisionException>(async () => await new ChatClient(feature).CreateDecisionAsync(Example()))!.Status, Is.EqualTo(503));
        feature.Providers.Clear();
        Assert.That(Assert.ThrowsAsync<ChatDecisionException>(async () => await new ChatClient(feature).CreateDecisionAsync(Example()))!.Status, Is.EqualTo(503));
        Assert.That(remote.Calls, Is.Empty);

        Assert.That(ChatDecisions.EndpointFor(new ChatProvider { Api = "https://gateway.example/api/v1/" }), Is.EqualTo("https://gateway.example/api/alpha/decisions"));
        Assert.That(ChatDecisions.EndpointFor(new ChatProvider { Api = "" }), Is.EqualTo(ChatDecisions.Endpoint));
        Assert.That(ChatDecisions.EndpointFor(new ChatProvider { Api = "http://insecure.example/api/v1" }), Is.EqualTo(ChatDecisions.Endpoint));
    }

    [Test]
    public void Other_IChatClient_implementations_keep_compiling_and_report_unsupported()
    {
        IChatClient client = new ChatOnlyClient();
        Assert.ThrowsAsync<NotSupportedException>(async () => await client.CreateDecisionAsync(Example()));
    }
    sealed class ChatOnlyClient : IChatClient
    {
        public Task<ChatResponse> ChatAsync(ChatCompletion request, CancellationToken token = default) => throw new NotImplementedException();
    }

    class AppHost() : AppHostBase(nameof(AiChatDecisionTests), typeof(ChatFeature).Assembly)
    {
        public override void Configure() { }
    }
    sealed class HeaderAuth : IChatAuth
    {
        public bool IsEnabled => true;
        public string? GetUserName(IRequest req) => req.Headers["X-Decision-User"];
        public string? AssertUserName(IRequest req) => GetUserName(req) ?? throw new UnauthorizedAccessException();
        public (bool, JsonObject?) CheckAuth(IRequest req) => (GetUserName(req) != null, null);
        public Task<JsonObject?> GetAuthInfoAsync(IRequest req) => Task.FromResult<JsonObject?>(null);
        public Task SignOutAsync(IRequest req) => Task.CompletedTask;
        public bool IsAdmin(IRequest req) => false;
    }

    [Test]
    public async Task Http_endpoint_requires_auth_and_forwards_free_form_json_verbatim()
    {
        var data = Path.Combine(Path.GetTempPath(), "ai-chat-decisions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(data);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());
        builder.Services.AddSingleton<ServiceStack.Data.IDbConnectionFactory>(new OrmLiteConnectionFactory(Path.Combine(data, "test.sqlite"), SqliteDialect.Provider));
        var remote = new Recorder();
        var feature = new ChatFeature { AppDataPath = data, ChatAuth = new HeaderAuth(), DecisionHandlerFactory = remote.Handler,
            EnableProviders = ["migration-none"], Config = ChatJson.ParseObject("""{"defaults":{"summarize":null},"providers":{}}"""),
            DisableExtensions = ["git", "gemini", "gallery", "analytics", "voice"] };
        builder.Services.AddPlugin(feature);
        await using var app = builder.Build();
        app.UseServiceStack(new AppHost(), options => options.MapEndpoints());
        try
        {
            await app.StartAsync();
            feature.Providers["openrouter"] = new ChatProvider { Id = "openrouter", Api = "https://openrouter.ai/api/v1", ApiKey = "router-key" };
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            // instructions as a JSON object, and object state, must reach OpenRouter unchanged
            var body = DecisionJson.ToJson(Example());
            body["state"] = JsonNode.Parse("""{"subject":"Payouts","tags":["billing",3]}""");
            body["questions"]!["is_urgent"]!["instructions"] = JsonNode.Parse("""{"focus":"time pressure"}""");

            async Task<HttpResponseMessage> Post(string? user)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/decisions") {
                    Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
                request.Headers.Accept.ParseAdd("application/json");
                if (user != null) request.Headers.Add("X-Decision-User", user);
                return await client.SendAsync(request);
            }

            using (var anonymous = await Post(null))
                Assert.That(anonymous.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(remote.Calls, Is.Empty);

            using var response = await Post("alice");
            var text = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), text);
            Assert.That(JsonNode.DeepEquals(remote.Calls.Single().Body, body), Is.True, remote.Calls.Single().Body.ToJsonString());
            var json = JsonNode.Parse(text)!.AsObject();
            Assert.That(json["answers"]!["department"]!["choice"]!.GetValue<string>(), Is.EqualTo("billing"));
            Assert.That(json["usage"]!["output_tokens"]!.GetValue<int>(), Is.EqualTo(9));
            // the server-side caller's user is never forwarded to OpenRouter implicitly
            Assert.That(remote.Calls.Single().Body.ContainsKey("user"), Is.False);
            // the typed DTO deserializes the same response
            var typed = ServiceStack.Text.JsonSerializer.DeserializeFromString<DecisionResponse>(text);
            Assert.That(typed.Answers["is_urgent"].Noul, Is.EqualTo(0.91));

            remote.Reply = () => Json("{}", HttpStatusCode.Unauthorized);
            using var providerAuth = await Post("alice");
            // OpenRouter rejecting its key is a gateway error, not the caller's authentication
            Assert.That(providerAuth.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
            if (Directory.Exists(data)) Directory.Delete(data, true);
        }
    }
}
