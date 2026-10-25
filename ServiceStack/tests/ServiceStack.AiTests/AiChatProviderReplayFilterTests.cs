using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.AiTests;

public class AiChatProviderReplayFilterTests
{
    const string Model = "gemini-3.8-flash";
    ChatFeature feature = null!;
    GoogleProvider provider = null!;

    [SetUp]
    public void SetUp()
    {
        feature = new ChatFeature();
        provider = new GoogleProvider { Id = "google", Feature = feature };
    }

    static JsonArray Parts() => JsonNode.Parse("""
        [{"text":"Thinking","thought":true,"thoughtSignature":"reasoning"},
         {"toolCall":{"toolType":"FILE_SEARCH","id":"search-1"},"thoughtSignature":"search"},
         {"text":"Answer","thoughtSignature":"text"},
         {"functionCall":{"name":"fetch_url","id":"fetch-1","args":{}},"thoughtSignature":"function"},
         {"toolResponse":{"toolType":"FILE_SEARCH","id":"search-1"},"thoughtSignature":"response"},
         {"thoughtSignature":"standalone"},{"futureField":{"value":1}}]
        """)!.AsArray();

    JsonObject Signed(JsonArray? parts = null)
    {
        var response = new JsonObject { ["candidates"] = new JsonArray(new JsonObject {
            ["content"] = new JsonObject { ["role"] = "model", ["parts"] = parts ?? Parts() }, ["finishReason"] = "STOP" }) };
        return provider.ToGeminiResponse(response, new JsonObject { ["model"] = Model }, DateTimeOffset.UtcNow, null, true)
            ["choices"]![0]!["message"]!.DeepClone().AsObject();
    }

    static JsonObject User() => new() { ["role"] = "user", ["content"] = "Follow-up" };
    static JsonObject Tool() => new() { ["role"] = "tool", ["tool_call_id"] = "fetch-1", ["content"] = "ok" };
    static JsonObject Chat(JsonArray messages) => new() { ["model"] = Model, ["messages"] = messages };
    JsonObject Payload(JsonObject chat)
    {
        var original = chat.ToJsonString();
        var result = provider.ToGeminiRequest(chat, new JsonObject(), false);
        Assert.That(chat.ToJsonString(), Is.EqualTo(original));
        return result;
    }
    static void Equal(JsonNode? actual, JsonNode? expected) => Assert.That(actual?.ToJsonString(), Is.EqualTo(expected?.ToJsonString()));

    [Test]
    public void Default_replays_completed_turns_unchanged()
    {
        var payload = Payload(Chat(new JsonArray(Signed(), User())));
        Equal(payload["contents"]![0]!["parts"], Parts());
    }

    [Test]
    public void Plugin_initializer_enables_helper_without_changing_other_instances()
    {
        feature = new ChatFeature {
            Filters = {
                ProviderReplayFilters = { GeminiReplayFilters.ExcludeCompletedTurnToolInvocations }
            }
        };
        provider.Feature = feature;
        var payload = Payload(Chat(new JsonArray(User(), Signed(), Tool(), User())));
        Equal(payload["contents"]![1]!["parts"], JsonNode.Parse("""
            [{"text":"Thinking","thought":true,"thoughtSignature":"reasoning"},
             {"text":"Answer","thoughtSignature":"text"},
             {"functionCall":{"name":"fetch_url","id":"fetch-1","args":{}},"thoughtSignature":"function"},
             {"thoughtSignature":"standalone"},{"futureField":{"value":1}}]
            """));
        Assert.That(payload["contents"]![2]!["parts"]![0]!["functionResponse"]!["id"]!.GetValue<string>(), Is.EqualTo("fetch-1"));
        Assert.That(payload["toolConfig"]!["includeServerSideToolInvocations"]!.GetValue<bool>(), Is.True);
        Assert.That(new ChatFeature().Filters.ProviderReplayFilters, Is.Empty);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Current_loop_is_preserved_with_or_without_a_user_prompt(bool includeUser)
    {
        feature.Filters.ProviderReplayFilters.Add(GeminiReplayFilters.ExcludeCompletedTurnToolInvocations);
        var messages = new JsonArray();
        if (includeUser) messages.Add(User());
        messages.Add(Signed()); messages.Add(Tool());
        var payload = Payload(Chat(messages));
        Equal(payload["contents"]![includeUser ? 1 : 0]!["parts"], Parts());
    }

    [Test]
    public void Extension_registration_runs_filters_in_order_on_each_request()
    {
        var ctx = new ExtensionContext(feature, "test");
        var calls = new List<(string, string, int, int, bool)>();
        ctx.RegisterProviderReplayFilter(replay => {
            calls.Add((replay.Provider.Id, replay.Model, replay.MessageIndex, replay.LastUserMessageIndex, replay.IsCurrentTurn));
            replay.Content["parts"]!.AsArray().Add(new JsonObject { ["text"] = "extension" });
        });
        ctx.RegisterProviderReplayFilter(replay => {
            Assert.That(replay.Content["parts"]!.AsArray().Last()!["text"]!.GetValue<string>(), Is.EqualTo("extension"));
            replay.Content["parts"]!.AsArray().Last()!["text"] = "extension two";
        });
        var chat = Chat(new JsonArray(Signed(), User()));
        for (var i = 0; i < 2; i++)
        {
            var payload = Payload(chat);
            Assert.That(payload["contents"]![0]!["parts"]!.AsArray().Last()!["text"]!.GetValue<string>(), Is.EqualTo("extension two"));
        }
        Assert.That(calls, Is.EqualTo(Enumerable.Repeat(("google", Model, 0, 1, false), 2)));
        Equal(chat["messages"]![0]!["provider_metadata"]!["google"]!["content"]!["parts"], Parts());
    }

    [Test]
    public void Historical_and_current_assistants_use_real_user_boundary_not_function_response()
    {
        var turns = new List<bool>();
        feature.Filters.ProviderReplayFilters.Add(replay => turns.Add(replay.IsCurrentTurn));
        feature.Filters.ProviderReplayFilters.Add(GeminiReplayFilters.ExcludeCompletedTurnToolInvocations);
        var user = User(); user["content"] = JsonNode.Parse("""[{"type":"text","text":"Follow-up"}]""");
        var payload = Payload(Chat(new JsonArray(Signed(), user, Signed(), Tool())));
        Assert.That(turns, Is.EqualTo(new[] { false, true }));
        Equal(payload["contents"]![2]!["parts"], Parts());
        Assert.That(payload["contents"]![0]!["parts"]!.AsArray().Count, Is.EqualTo(5));
    }

    [TestCase("model", "different")]
    [TestCase("provider", "other")]
    public void Mismatched_replay_does_not_invoke_filters(string field, string value)
    {
        var calls = 0;
        feature.Filters.ProviderReplayFilters.Add(_ => calls++);
        var signed = Signed(); signed["provider_metadata"]!["google"]![field] = value;
        Payload(Chat(new JsonArray(signed, User())));
        Assert.That(calls, Is.Zero);
    }

    [Test]
    public void Empty_historical_builtin_message_is_omitted()
    {
        feature.Filters.ProviderReplayFilters.Add(GeminiReplayFilters.ExcludeCompletedTurnToolInvocations);
        var parts = Parts();
        var payload = Payload(Chat(new JsonArray(Signed(new JsonArray(parts[1]!.DeepClone(), parts[4]!.DeepClone())), User())));
        Assert.That(payload["contents"]!.AsArray().Count, Is.EqualTo(1));
        Assert.That(payload["contents"]![0]!["role"]!.GetValue<string>(), Is.EqualTo("user"));
    }

    [Test]
    public void Helper_ignores_other_provider_types()
    {
        var parts = Parts();
        var content = new JsonObject { ["parts"] = parts.DeepClone() };
        GeminiReplayFilters.ExcludeCompletedTurnToolInvocations(new ProviderReplayContext(new ChatProvider(), Model, 0, 1, content));
        Equal(content["parts"], parts);
    }

    [Test]
    public void Filter_exceptions_abort_without_changing_source_history()
    {
        feature.Filters.ProviderReplayFilters.Add(replay => {
            replay.Content["parts"]!.AsArray().Clear();
            throw new InvalidOperationException("extension failed");
        });
        var chat = Chat(new JsonArray(Signed(), User()));
        var before = chat.ToJsonString();
        Assert.That(() => provider.ToGeminiRequest(chat, new JsonObject(), false), Throws.InvalidOperationException.With.Message.EqualTo("extension failed"));
        Assert.That(chat.ToJsonString(), Is.EqualTo(before));
    }

    [Test]
    public void Invalid_parts_are_rejected_and_capability_guard_cannot_be_bypassed()
    {
        feature.Filters.ProviderReplayFilters.Add(replay => replay.Content["parts"] = "invalid");
        Assert.That(() => Payload(Chat(new JsonArray(Signed(), User()))), Throws.ArgumentException);
        feature.Filters.ProviderReplayFilters.Clear();
        feature.Filters.ProviderReplayFilters.Add(GeminiReplayFilters.ExcludeCompletedTurnToolInvocations);
        var chat = Chat(new JsonArray(Signed(), User())); chat["tool_combination"] = false;
        Assert.That(() => Payload(chat), Throws.ArgumentException.With.Message.Contains("requires Gemini tool context circulation"));
    }

    [Test]
    public void Legacy_static_conversion_remains_unfiltered()
    {
        feature.Filters.ProviderReplayFilters.Add(GeminiReplayFilters.ExcludeCompletedTurnToolInvocations);
        var (contents, _) = GoogleProvider.ToGeminiContents(Chat(new JsonArray(Signed(), User())), "google");
        Equal(contents[0]!["parts"], Parts());
    }
}
