#nullable enable
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.AiTests;

public class AiChatGoogleToolCombinationTests
{
    const string Model = "gemini-3.8-flash";

    static GoogleProvider Provider() => new() { Id = "google" };

    static JsonObject Chat() => ChatJson.ParseObject("""
        {"model":"gemini-3.8-flash","messages":[{"role":"user","content":"Search then fetch"}],"tools":[
          {"type":"file_search","file_search":{"file_search_store_names":["fileSearchStores/docs"]}},
          {"type":"function","function":{"name":"fetch_url","parameters":{"type":"object","properties":{}}}}
        ]}
        """);

    static JsonArray Parts() => JsonNode.Parse("""
        [
          {"text":"Thinking","thought":true,"thoughtSignature":"reasoning-signature"},
          {"toolCall":{"toolType":"FILE_SEARCH","id":"search-1"},"thoughtSignature":"search-signature"},
          {"text":"Fetching next","thoughtSignature":"text-signature"},
          {"functionCall":{"id":"fetch-1","name":"fetch_url","args":{"url":"https://example.com"}},"thoughtSignature":"function-signature"},
          {"toolResponse":{"toolType":"FILE_SEARCH","id":"search-1"},"thoughtSignature":"response-signature"},
          {"thoughtSignature":"standalone-signature"}
        ]
        """)!.AsArray();

    static JsonObject Response(JsonArray parts, JsonObject? grounding = null) => new()
    {
        ["candidates"] = new JsonArray(new JsonObject
        {
            ["index"] = 0, ["content"] = new JsonObject { ["role"] = "model", ["parts"] = parts.DeepClone() },
            ["groundingMetadata"] = grounding?.DeepClone(), ["finishReason"] = "STOP",
        }),
    };

    static JsonObject Message(JsonObject response) => response["choices"]![0]!["message"]!.AsObject();

    [TestCase(Model)]
    [TestCase("gemini-3.5-flash-lite")]
    [TestCase("gemini-3.1-pro-preview")]
    [TestCase("gemini-4-argon")]
    [TestCase("gemini-flash-latest")]
    [TestCase("gemini-flash-lite-latest")]
    public void Supported_models_include_both_tools_and_validated_mode(string model)
    {
        var chat = Chat(); chat["model"] = model;
        var before = chat.ToJsonString();
        var body = Provider().ToGeminiRequest(chat, new JsonObject(), false);
        Assert.That(body["tools"]![0]!["file_search"], Is.Not.Null);
        Assert.That(body["tools"]![0]!["function_declarations"]![0]!["name"]!.GetValue<string>(), Is.EqualTo("fetch_url"));
        Assert.That(body["toolConfig"]!.ToJsonString(), Is.EqualTo(
            """{"includeServerSideToolInvocations":true,"functionCallingConfig":{"mode":"VALIDATED"}}"""));
        Assert.That(chat.ToJsonString(), Is.EqualTo(before));
    }

    [Test]
    public void Capabilities_resolve_request_then_model_then_provider_and_keep_legacy_behavior()
    {
        var provider = Provider(); var chat = Chat(); chat["model"] = "gemini-2.5-flash";
        var legacy = provider.ToGeminiRequest(chat, new JsonObject(), false);
        Assert.That(legacy["toolConfig"], Is.Null);
        Assert.That(legacy["tools"]![0]!["function_declarations"], Is.Null);
        provider.ToolCombination = true;
        Assert.That(provider.ToGeminiRequest(chat, new JsonObject(), false)["toolConfig"], Is.Not.Null);
        var info = new JsonObject { ["tool_combination"] = false };
        Assert.That(provider.ToGeminiRequest(chat, info, false)["toolConfig"], Is.Null);
        chat["tool_combination"] = true;
        Assert.That(provider.ToGeminiRequest(chat, info, false)["toolConfig"], Is.Not.Null);
        chat["tools"]!.AsArray().RemoveAt(1);
        Assert.That(provider.ToGeminiRequest(chat, info, false)["toolConfig"], Is.Null);
    }

    [TestCase("auto", "VALIDATED")]
    [TestCase("none", "NONE")]
    [TestCase("required", "ANY")]
    public void Tool_choice_uses_supported_modes(string choice, string expected)
    {
        var chat = Chat(); chat["tool_choice"] = choice;
        Assert.That(Provider().ToGeminiRequest(chat, new JsonObject(), false)["toolConfig"]!["functionCallingConfig"]!["mode"]!
            .GetValue<string>(), Is.EqualTo(expected));
        chat["tool_choice"] = ChatJson.ParseObject("""{"type":"function","function":{"name":"fetch_url"}}""");
        Assert.That(Provider().ToGeminiRequest(chat, new JsonObject(), false)["toolConfig"]!["functionCallingConfig"]!
            ["allowedFunctionNames"]![0]!.GetValue<string>(), Is.EqualTo("fetch_url"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Complete_ordered_parts_replay_once_and_parallel_results_share_a_user_turn(bool stream)
    {
        var provider = Provider(); var chat = Chat(); var parts = Parts();
        var second = parts[3]!.DeepClone(); second["functionCall"]!["id"] = "fetch-2"; parts.Add(second);
        JsonObject response;
        if (stream)
        {
            var sse = string.Join("\n\n", parts.Select(part => "data: " + Response(new JsonArray(part!.DeepClone())).ToJsonString()));
            using var http = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(sse, Encoding.UTF8, "text/event-stream") };
            response = (await provider.HandleGeminiStreamAsync(http, chat, DateTimeOffset.UtcNow, new ChatContext(), true))!;
        }
        else response = provider.ToGeminiResponse(Response(parts), chat, DateTimeOffset.UtcNow, null, true);
        var message = Message(response);
        Assert.That(message["tool_calls"]!.AsArray().Count, Is.EqualTo(2));
        Assert.That(message["tool_calls"]![0]!["thoughtSignature"]!.GetValue<string>(), Is.EqualTo("function-signature"));
        Assert.That(message["provider_metadata"]!["google"]!["content"]!["parts"]!.ToJsonString(), Is.EqualTo(parts.ToJsonString()));
        chat["messages"]!.AsArray().Add(message.DeepClone());
        foreach (var id in new[] { "fetch-1", "fetch-2" })
            chat["messages"]!.AsArray().Add(new JsonObject { ["role"] = "tool", ["tool_call_id"] = id, ["content"] = "ok" });
        var before = chat.ToJsonString();
        var body = provider.ToGeminiRequest(chat, new JsonObject(), false);
        Assert.That(body["contents"]![1]!["parts"]!.ToJsonString(), Is.EqualTo(parts.ToJsonString()));
        Assert.That(body["contents"]!.AsArray().Count, Is.EqualTo(3));
        Assert.That(body["contents"]![2]!["role"]!.GetValue<string>(), Is.EqualTo("user"));
        Assert.That(body["contents"]![2]!["parts"]!.AsArray().Select(p => p!["functionResponse"]!["id"]!.GetValue<string>()),
            Is.EqualTo(new[] { "fetch-1", "fetch-2" }));
        Assert.That(chat.ToJsonString(), Is.EqualTo(before));
    }

    [Test]
    public async Task Final_and_builtin_only_messages_keep_state_and_other_providers_strip_it()
    {
        var provider = Provider(); var chat = Chat(); var parts = Parts(); parts.RemoveAt(3);
        var message = Message(provider.ToGeminiResponse(Response(parts), chat, DateTimeOffset.UtcNow, null, true));
        Assert.That(message["tool_calls"], Is.Null);
        Assert.That(StreamCheckpointWriter.PayloadLength(new JsonObject { ["provider_metadata"] = message["provider_metadata"]!.DeepClone() }), Is.GreaterThan(0));
        chat["messages"]!.AsArray().Add(message.DeepClone()); chat["tools"]!.AsArray().RemoveAt(1);
        Assert.That(provider.ToGeminiRequest(chat, new JsonObject(), false)["toolConfig"], Is.Not.Null);
        chat["tool_combination"] = false;
        Assert.Throws<ArgumentException>(() => provider.ToGeminiRequest(chat, new JsonObject(), false));
        chat.Remove("tool_combination");
        chat["model"] = "gemini-3.1-pro-preview";
        Assert.That(provider.ToGeminiRequest(chat, new JsonObject(), false)["contents"]![1]!["parts"]!.AsArray()
            .OfType<JsonObject>().Any(p => p.ContainsKey("toolCall")), Is.False);
        chat["tool_combination"] = true;
        var generic = new OpenAiCompatibleProvider { Id = "other" };
        var projected = await generic.ProcessChatAsync(chat.DeepClone().AsObject());
        Assert.That(projected["tool_combination"], Is.Null);
        Assert.That(projected["messages"]![1]!["provider_metadata"], Is.Null);
        Assert.That(message["provider_metadata"], Is.Not.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Citations_are_preserved_separately_from_builtin_parts(bool stream)
    {
        var parts = Parts(); parts.RemoveAt(3);
        var grounding = ChatJson.ParseObject("""
            {"groundingChunks":[{"retrievedContext":{"uri":"https://example.com/doc","title":"Doc"}}],
             "groundingSupports":[{"segment":{"startIndex":0,"endIndex":6},"groundingChunkIndices":[0]}]}
            """);
        foreach (var metadata in new JsonObject?[] { null, grounding })
        {
            var candidate = Response(parts, metadata);
            var provider = Provider(); var chat = Chat();
            JsonObject response;
            if (stream)
            {
                using var http = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("data: " + candidate.ToJsonString() + "\n\n") };
                response = (await provider.HandleGeminiStreamAsync(http, chat, DateTimeOffset.UtcNow, new ChatContext(), true))!;
            }
            else response = provider.ToGeminiResponse(candidate, chat, DateTimeOffset.UtcNow, null, true);
            var message = Message(response);
            Assert.That(message["groundingMetadata"] != null, Is.EqualTo(metadata != null));
            Assert.That(message["provider_metadata"]!["google"]!["content"]!["groundingMetadata"], Is.Null);
        }
    }

    [Test]
    public void Canonical_message_json_retains_long_signatures_and_replays_after_reload()
    {
        using var host = new AiChatMigrationTestHost();
        var provider = Provider(); var chat = Chat(); var parts = Parts();
        parts[1]!["thoughtSignature"] = new string('x', 220_000);
        var message = Message(provider.ToGeminiResponse(Response(parts), chat, DateTimeOffset.UtcNow, null, true));
        message["timestamp"] = 2;
        var thread = host.Db.InsertThread(new ChatThread { User = "alice", Messages = "[]" });
        host.Db.SyncChatMessages(thread, new JsonArray(message.DeepClone()));
        var reloaded = host.Db.GetActiveMessagesAfter(thread, 0);
        chat["messages"] = new JsonArray(reloaded.Select(p => (JsonNode)p.DeepClone()).ToArray());
        Assert.That(provider.ToGeminiRequest(chat, new JsonObject(), false)["contents"]![0]!["parts"]!.ToJsonString(), Is.EqualTo(parts.ToJsonString()));
        Assert.That(DurableAgentUtils.CountTokensApprox(message), Is.GreaterThan(70_000));
    }

    [Test]
    public async Task Compaction_keeps_signed_recent_turns_and_excludes_raw_state_from_summary_input()
    {
        using var host = new AiChatMigrationTestHost();
        host.Feature.Config["defaults"]!["compact"] = ChatJson.ParseObject("""
            {"model":"compact","messages":[{"role":"user","content":"{messages_json}"}]}
            """);
        var summarizer = new SummaryProvider();
        host.Feature.Providers["summary"] = summarizer;
        var app = host.Install(new AppExtension());
        var parts = Parts(); parts[1]!["thoughtSignature"] = new string('x', 220_000);
        var signed = Message(Provider().ToGeminiResponse(Response(parts), Chat(), DateTimeOffset.UtcNow, null, true));
        var old = signed.DeepClone().AsObject(); old["content"] = new string('z', 50_000);
        var messages = new JsonArray(old, new JsonObject { ["role"] = "user", ["content"] = "Continue" }, signed.DeepClone());
        var compacted = await app.CompactMessagesAsync(messages, 1000, 8000, "alice", 1, null, default);
        Assert.That(compacted.Last()!["provider_metadata"]!.ToJsonString(), Is.EqualTo(signed["provider_metadata"]!.ToJsonString()));
        Assert.That(summarizer.LastChat, Is.Not.Null);
        Assert.That(summarizer.LastChat!.ToJsonString(), Does.Not.Contain("provider_metadata"));
        Assert.That(old["provider_metadata"], Is.Not.Null);
    }

    sealed class SummaryProvider : ChatProvider
    {
        public JsonObject? LastChat;
        public override string? ProviderModel(string model) => model == "compact" ? model : null;
        public override JsonObject? ModelInfo(string model) => new() { ["id"] = "compact" };
        public override Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
        {
            LastChat = chat.DeepClone().AsObject();
            return Task.FromResult(ChatJson.ParseObject("""
                {"choices":[{"message":{"role":"assistant","content":"{\"messages\":[{\"role\":\"system\",\"content\":\"Summary\"}]}"}}],
                 "usage":{"prompt_tokens":100,"completion_tokens":10}}
                """));
        }
    }

    sealed class Checkpoints : IThreadApi
    {
        public List<JsonObject> Messages = [];
        public JsonObject? GetThread(long id, string? user) => null;
        public JsonObject? GetRequest(string id, string? user) => null;
        public Task UpdateThreadAsync(long id, JsonObject thread, string? user = null) => throw new AssertionException("Streaming must not write canonical history");
        public Task CheckpointStreamAsync(long id, JsonObject message, string? user = null)
        {
            Messages.Add(message.DeepClone().AsObject());
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Builtin_only_stream_checkpoints_keep_signatures_and_ignore_other_candidates()
    {
        var provider = Provider(); var checkpoints = new Checkpoints();
        provider.Feature = new ChatFeature { ThreadApi = checkpoints };
        var parts = new JsonArray(Parts()[1]!.DeepClone(), Parts()[4]!.DeepClone());
        var response = Response(parts);
        response["candidates"]!.AsArray().Add(new JsonObject
        {
            ["index"] = 1, ["content"] = new JsonObject { ["parts"] = Parts() },
        });
        using var http = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("data: " + response.ToJsonString() + "\n\n") };
        var result = Message((await provider.HandleGeminiStreamAsync(http, Chat(), DateTimeOffset.UtcNow,
            new ChatContext { ThreadId = 42 }, true))!);
        Assert.That(result["tool_calls"], Is.Null);
        Assert.That(checkpoints.Messages, Is.Not.Empty);
        Assert.That(checkpoints.Messages.Last()["provider_metadata"]!["google"]!["content"]!["parts"]!.ToJsonString(), Is.EqualTo(parts.ToJsonString()));
    }

    [Explicit("Billable Gemini verification; requires an isolated test store and its expected code in environment variables")]
    [TestCase(Model, false)]
    [TestCase(Model, true)]
    [TestCase("gemini-3.5-flash-lite", false)]
    [TestCase("gemini-3.5-flash-lite", true)]
    [TestCase("gemini-3.1-pro-preview", false)]
    [TestCase("gemini-3.1-pro-preview", true)]
    public async Task Live_round_trip_with_file_search_and_custom_function(string model, bool stream)
    {
        var key = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        var store = Environment.GetEnvironmentVariable("GEMINI_TOOL_COMBINATION_STORE");
        var code = Environment.GetEnvironmentVariable("GEMINI_TOOL_COMBINATION_CODE");
        Assert.That(key, Is.Not.Null.And.Not.Empty);
        Assert.That(store, Does.StartWith("fileSearchStores/"));
        Assert.That(code, Is.Not.Null.And.Not.Empty);
        var provider = Provider();
        provider.Populate(new JsonObject { ["id"] = "google", ["api_key"] = key });
        var chat = Chat(); chat["model"] = model; chat["stream"] = stream;
        chat["max_completion_tokens"] = 4096;
        chat["thinkingConfig"] = new JsonObject { ["thinkingLevel"] = "LOW" };
        chat["messages"]![0]!["content"] = "Find the private launch code in the Verification document using File Search. "
            + "Then call verify_code with that code. Finally report the code and verification result.";
        chat["tools"]![0]!["file_search"]!["file_search_store_names"]![0] = store;
        chat["tools"]![1]!["function"] = ChatJson.ParseObject("""
            {"name":"verify_code","description":"Verify the launch code retrieved from the document.",
             "parameters":{"type":"object","properties":{"code":{"type":"string"}},"required":["code"]}}
            """);
        var verified = false; var builtinParts = 0; JsonObject? final = null;
        for (var turn = 0; turn < 5; turn++)
        {
            var message = Message(await provider.ChatAsync(chat, new ChatContext())).DeepClone().AsObject();
            chat["messages"]!.AsArray().Add(message);
            builtinParts += message["provider_metadata"]!["google"]!["content"]!["parts"]!.AsArray()
                .OfType<JsonObject>().Count(p => p.ContainsKey("toolCall") || p.ContainsKey("toolResponse"));
            if (message["tool_calls"] is not JsonArray { Count: > 0 } calls) { final = message; break; }
            foreach (var call in calls.OfType<JsonObject>())
            {
                Assert.That(call["function"]!["name"]!.GetValue<string>(), Is.EqualTo("verify_code"));
                var args = ChatJson.ParseObject(call["function"]!["arguments"]!.GetValue<string>());
                Assert.That(args["code"]!.GetValue<string>(), Is.EqualTo(code));
                verified = true;
                chat["messages"]!.AsArray().Add(new JsonObject
                {
                    ["role"] = "tool", ["tool_call_id"] = call["id"]!.GetValue<string>(),
                    ["content"] = new JsonObject { ["verified"] = true, ["code"] = code }.ToJsonString(),
                });
            }
        }
        Assert.That(verified, Is.True);
        Assert.That(builtinParts, Is.GreaterThan(0));
        Assert.That(final, Is.Not.Null);
        Assert.That(final!["content"]!.GetValue<string>(), Does.Contain(code!));
        var baseline = chat.DeepClone().AsObject();
        baseline["tools"]!.AsArray().RemoveAt(1);
        baseline["messages"] = new JsonArray(new JsonObject
        {
            ["role"] = "user", ["content"] = "According to the Verification document, what is the private launch code? Cite the document.",
        });
        var plain = Message(await provider.ChatAsync(baseline, new ChatContext()));
        Assert.That(plain["content"]!.GetValue<string>(), Does.Contain(code!));
        Console.WriteLine(new JsonObject
        {
            ["model"] = model, ["stream"] = stream, ["verified"] = verified, ["builtinParts"] = builtinParts,
            ["combinedCitations"] = final["groundingMetadata"] != null, ["fileSearchOnlyCitations"] = plain["groundingMetadata"] != null,
        }.ToJsonString());
    }
}
