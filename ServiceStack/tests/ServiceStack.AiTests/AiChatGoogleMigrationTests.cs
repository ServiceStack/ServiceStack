#nullable enable
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.AiTests;

public class AiChatGoogleMigrationTests
{
    static GoogleProvider CreateProvider()
    {
        var provider = new GoogleProvider();
        provider.Populate(ChatJson.ParseObject("""
            {"id":"google","api_key":"test",
             "thinking_config":{"thinkingBudget":1024,"includeThoughts":true}}
            """));
        return provider;
    }

    [TestCase("gemini-3.8-flash")]
    [TestCase("gemini-3.5-flash-lite")]
    [TestCase("gemini-flash-latest")]
    [TestCase("gemini-flash-lite-latest")]
    public void New_models_and_aliases_omit_deprecated_parameters(string model)
    {
        var provider = CreateProvider();
        var chat = ChatJson.ParseObject("""
            {"messages":[{"role":"user","content":"Hello"}],
             "temperature":0.7,"top_p":0.9,"top_logprobs":40,"candidate_count":2,
             "max_completion_tokens":100}
            """);
        chat["model"] = model;
        var original = chat.ToJsonString();
        var providerConfig = provider.ThinkingConfig!.ToJsonString();
        var body = provider.ToGeminiRequest(chat, ChatJson.ParseObject("""{"reasoning":true}"""), false);
        var config = body["generationConfig"]!.AsObject();
        Assert.That(config.ToJsonString(), Is.EqualTo(
            """{"maxOutputTokens":100,"thinkingConfig":{"includeThoughts":true,"thinkingLevel":"MEDIUM"}}"""));
        Assert.That(chat.ToJsonString(), Is.EqualTo(original));
        Assert.That(provider.ThinkingConfig.ToJsonString(), Is.EqualTo(providerConfig));
    }

    [TestCase("""{"thinking_level":"high","thinkingConfig":{"thinking_level":"low","thinkingLevel":"medium","thinking_budget":1024}}""", "HIGH")]
    [TestCase("""{"thinkingLevel":"low","thinkingConfig":{"thinking_level":"high"}}""", "LOW")]
    [TestCase("""{"thinkingConfig":{"thinking_level":"low","thinkingLevel":"high"}}""", "LOW")]
    [TestCase("""{"thinkingConfig":{"thinkingLevel":"High"}}""", "HIGH")]
    [TestCase("""{"thinkingConfig":{"thinking_level":"MINIMAL"}}""", "LOW")]
    public void Thinking_levels_use_rest_enums_and_remove_nested_aliases(string options, string expected)
    {
        var chat = ChatJson.ParseObject(options);
        chat["model"] = "gemini-3.8-flash";
        chat["messages"] = new JsonArray();
        var original = chat.ToJsonString();
        var body = CreateProvider().ToGeminiRequest(chat, new JsonObject(), false);
        var thinking = body["generationConfig"]!["thinkingConfig"]!.AsObject();
        Assert.That(thinking.Count, Is.EqualTo(1));
        Assert.That(thinking["thinkingLevel"]!.GetValue<string>(), Is.EqualTo(expected));
        Assert.That(chat.ToJsonString(), Is.EqualTo(original));
    }

    [Test]
    public void Explicit_top_level_thinking_works_without_provider_defaults()
    {
        var provider = CreateProvider();
        provider.ThinkingConfig = null;
        var body = provider.ToGeminiRequest(ChatJson.ParseObject("""
            {"model":"gemini-3.8-flash","thinking_level":"high","messages":[]}
            """), new JsonObject(), false);
        Assert.That(body["generationConfig"]!["thinkingConfig"]!.ToJsonString(),
            Is.EqualTo("""{"includeThoughts":true,"thinkingLevel":"HIGH"}"""));
    }

    [Test]
    public void Legacy_models_keep_sampling_and_thinking_budget()
    {
        var body = CreateProvider().ToGeminiRequest(ChatJson.ParseObject("""
            {"model":"gemini-2.5-flash","messages":[],"temperature":0.7,"top_p":0.9,
             "top_logprobs":40,"thinkingConfig":{"thinkingBudget":2048,"includeThoughts":true}}
            """), new JsonObject(), false);
        Assert.That(body["generationConfig"]!.ToJsonString(), Is.EqualTo("""
            {"temperature":0.7,"topP":0.9,"topK":40,"thinkingConfig":{"thinkingBudget":2048,"includeThoughts":true}}
            """));
    }

    [Test]
    public void Tool_history_echoes_rest_ids_without_duplicate_text_or_interactions_fields()
    {
        var chat = ChatJson.ParseObject("""
            {"messages":[
                {"role":"assistant","tool_calls":[{"id":"native_123","function":{"name":"weather","arguments":"{}"}}]},
                {"role":"tool","tool_call_id":"native_123","content":"{\"temp\":20}"}
            ]}
            """);
        var (contents, _) = GoogleProvider.ToGeminiContents(chat);
        var call = contents[0]!["parts"]![0]!["functionCall"]!;
        var parts = contents[1]!["parts"]!.AsArray();
        Assert.That(call["id"]!.GetValue<string>(), Is.EqualTo("native_123"));
        Assert.That(parts.Count, Is.EqualTo(1));
        Assert.That(parts[0]!["functionResponse"]!.ToJsonString(), Is.EqualTo(
            """{"name":"weather","response":{"temp":20},"id":"native_123"}"""));
    }

    [Test]
    public void Tool_results_can_use_explicit_name_when_history_mapping_is_missing()
    {
        var (contents, _) = GoogleProvider.ToGeminiContents(ChatJson.ParseObject("""
            {"messages":[{"role":"tool","name":"weather","tool_call_id":"native_123","content":"sunny"}]}
            """));
        Assert.That(contents[0]!["parts"]![0]!["functionResponse"]!.ToJsonString(), Is.EqualTo(
            """{"name":"weather","response":{"content":"sunny"},"id":"native_123"}"""));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Provider_native_tool_ids_survive_response_conversion(bool stream)
    {
        const string responseJson = """
            {"candidates":[{"content":{"parts":[{"functionCall":{"id":"native_123","name":"weather","args":{}}}]},"finishReason":"STOP"}]}
            """;
        var provider = CreateProvider();
        var chat = ChatJson.ParseObject("""{"model":"gemini-3.8-flash","messages":[]}""");
        JsonObject? response;
        if (stream)
        {
            using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("data: " + responseJson + "\n\n", Encoding.UTF8, "text/event-stream"),
            };
            response = await provider.HandleGeminiStreamAsync(httpResponse, chat, DateTimeOffset.UtcNow, new ChatContext());
        }
        else
            response = provider.ToGeminiResponse(ChatJson.ParseObject(responseJson), chat, DateTimeOffset.UtcNow, new ChatContext());
        Assert.That(response!["choices"]![0]!["message"]!["tool_calls"]![0]!["id"]!.GetValue<string>(),
            Is.EqualTo("native_123"));
    }
}
