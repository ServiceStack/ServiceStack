#nullable enable
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

public class AiChatGoogleTests
{
    static GoogleProvider CreateProvider()
    {
        var provider = new GoogleProvider();
        provider.Populate(ChatJson.ParseObject(
            """
            {
                "id": "google",
                "api_key": "AIza-test",
                "thinking_config": { "thinkingBudget": 1024, "includeThoughts": true },
                "models": {
                    "gemini-2.5-flash": {
                        "id": "gemini-2.5-flash",
                        "name": "Gemini 2.5 Flash",
                        "tool_call": true,
                        "cost": { "input": 0.3, "output": 2.5 }
                    }
                }
            }
            """));
        return provider;
    }

    static HttpResponseMessage SseResponse(string sse) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(sse, Encoding.UTF8, "text/event-stream")
    };

    [Test]
    public void Authenticates_with_query_key_not_authorization_header()
    {
        var provider = CreateProvider();
        // Google rejects the Authorization header — the key goes in the query string
        Assert.That(provider.Headers.ContainsKey("Authorization"), Is.False);
        Assert.That(provider.ApiKey, Is.EqualTo("AIza-test"));
    }

    [Test]
    public void Accepts_any_gemini_model_id()
    {
        var provider = CreateProvider();
        // catalogued model
        Assert.That(provider.ProviderModel("gemini-2.5-flash"), Is.EqualTo("gemini-2.5-flash"));
        // gemini-* ids pass through even when not in the catalogue
        Assert.That(provider.ProviderModel("gemini-3-pro-preview"), Is.EqualTo("gemini-3-pro-preview"));
        Assert.That(provider.ModelInfo("gemini-3-pro-preview")!["id"]!.GetValue<string>(),
            Is.EqualTo("gemini-3-pro-preview"));
        // non-gemini ids still resolve normally (i.e. not at all here)
        Assert.That(provider.ProviderModel("gpt-4o"), Is.Null);
    }

    [Test]
    public void Sanitize_parameters_strips_fields_gemini_rejects()
    {
        var schema = ChatJson.ParseObject("""
            {
                "$schema": "https://json-schema.org/draft/2020-12/schema",
                "type": "object",
                "additionalProperties": false,
                "properties": {
                    "city": { "type": "string", "additionalProperties": false },
                    "tags": {
                        "type": "array",
                        "items": { "type": "string", "$schema": "x" }
                    },
                    "either": {
                        "anyOf": [
                            { "type": "string", "additionalProperties": false },
                            { "type": "null" }
                        ]
                    }
                },
                "required": ["city"]
            }
            """);

        var sanitized = GoogleProvider.SanitizeParameters(schema)!.AsObject();

        // forbidden keywords removed at every level
        Assert.That(sanitized.ContainsKey("$schema"), Is.False);
        Assert.That(sanitized.ContainsKey("additionalProperties"), Is.False);
        Assert.That(sanitized["properties"]!["city"]!.AsObject().ContainsKey("additionalProperties"), Is.False);
        Assert.That(sanitized["properties"]!["tags"]!["items"]!.AsObject().ContainsKey("$schema"), Is.False);
        Assert.That(sanitized["properties"]!["either"]!["anyOf"]!.AsArray()[0]!.AsObject()
            .ContainsKey("additionalProperties"), Is.False);

        // everything else is preserved
        Assert.That(sanitized["type"]!.GetValue<string>(), Is.EqualTo("object"));
        Assert.That(sanitized["properties"]!["city"]!["type"]!.GetValue<string>(), Is.EqualTo("string"));
        Assert.That(sanitized["required"]!.AsArray()[0]!.GetValue<string>(), Is.EqualTo("city"));

        // the original is not mutated
        Assert.That(schema.ContainsKey("$schema"), Is.True);
    }

    [Test]
    public void Request_includes_sanitized_function_declarations()
    {
        var chat = ChatJson.ParseObject("""
            {
                "model": "gemini-2.5-flash",
                "messages": [{"role":"user","content":"weather in Perth?"}],
                "tools": [{
                    "type": "function",
                    "function": {
                        "name": "get_weather",
                        "description": "Get weather",
                        "parameters": {
                            "$schema": "https://json-schema.org/draft/2020-12/schema",
                            "type": "object",
                            "additionalProperties": false,
                            "properties": { "city": { "type": "string" } },
                            "required": ["city"]
                        }
                    }
                }]
            }
            """);
        var modelInfo = ChatJson.ParseObject("""{"id":"gemini-2.5-flash","tool_call":true}""");

        var body = CreateProvider().ToGeminiRequest(chat, modelInfo, hasMediaModality: false);

        var declarations = body["tools"]!.AsArray()[0]!["function_declarations"]!.AsArray();
        Assert.That(declarations.Count, Is.EqualTo(1));
        var declaration = declarations[0]!.AsObject();
        Assert.That(declaration["name"]!.GetValue<string>(), Is.EqualTo("get_weather"));
        // the schema must be sanitized on the way out or Gemini 400s
        var parameters = declaration["parameters"]!.AsObject();
        Assert.That(parameters.ContainsKey("$schema"), Is.False);
        Assert.That(parameters.ContainsKey("additionalProperties"), Is.False);
        Assert.That(parameters["properties"]!["city"]!["type"]!.GetValue<string>(), Is.EqualTo("string"));
    }

    [Test]
    public void Request_opts_in_to_server_side_tools_when_combined_with_function_calling()
    {
        var chat = ChatJson.ParseObject("""
            {
                "model": "gemini-2.5-flash",
                "messages": [{"role":"user","content":"hi"}],
                "tools": [
                    {"type":"file_search","file_search":{"file_search_store_names":["fileSearchStores/docs"]}},
                    {"type":"function","function":{"name":"noop","parameters":{"type":"object"}}}
                ]
            }
            """);
        var modelInfo = ChatJson.ParseObject("""{"id":"gemini-2.5-flash","tool_call":true}""");

        var body = CreateProvider().ToGeminiRequest(chat, modelInfo, hasMediaModality: false);

        var geminiTools = body["tools"]!.AsArray()[0]!.AsObject();
        Assert.That(geminiTools["file_search"]!["file_search_store_names"]!.AsArray()[0]!.GetValue<string>(),
            Is.EqualTo("fileSearchStores/docs"));
        Assert.That(geminiTools["function_declarations"]!.AsArray().Count, Is.EqualTo(1));
        // Gemini 400s mixing built-in tools with function calling without this opt-in
        Assert.That(body["toolConfig"]!["includeServerSideToolInvocations"]!.GetValue<bool>(), Is.True);

        // only needed when both kinds of tools are sent
        var functionsOnly = ChatJson.ParseObject("""
            {
                "model": "gemini-2.5-flash",
                "messages": [{"role":"user","content":"hi"}],
                "tools": [{"type":"function","function":{"name":"noop","parameters":{"type":"object"}}}]
            }
            """);
        var functionsBody = CreateProvider().ToGeminiRequest(functionsOnly, modelInfo, hasMediaModality: false);
        Assert.That(functionsBody.ContainsKey("toolConfig"), Is.False);
    }

    [Test]
    public void Request_omits_tools_when_the_model_cannot_call_them()
    {
        var chat = ChatJson.ParseObject("""
            {
                "model": "gemini-2.5-flash",
                "messages": [{"role":"user","content":"hi"}],
                "tools": [{"type":"function","function":{"name":"noop","parameters":{"type":"object"}}}]
            }
            """);

        // model that explicitly opts out of tool_call support
        var body = CreateProvider().ToGeminiRequest(chat,
            ChatJson.ParseObject("""{"id":"gemini-2.5-flash","tool_call":false}"""), hasMediaModality: false);
        Assert.That(body.ContainsKey("tools"), Is.False);

        // but gemini models support tool calls by default when the catalogue doesn't say
        var defaultBody = CreateProvider().ToGeminiRequest(chat,
            ChatJson.ParseObject("""{"id":"gemini-2.5-flash"}"""), hasMediaModality: false);
        Assert.That(defaultBody.ContainsKey("tools"), Is.True);

        // Gemini also can't combine tools with media output
        var mediaBody = CreateProvider().ToGeminiRequest(chat,
            ChatJson.ParseObject("""{"id":"gemini-2.5-flash","tool_call":true}"""), hasMediaModality: true);
        Assert.That(mediaBody.ContainsKey("tools"), Is.False);
    }

    [Test]
    public void Request_maps_generation_config_and_thinking()
    {
        var chat = ChatJson.ParseObject("""
            {
                "model": "gemini-2.5-flash",
                "messages": [{"role":"user","content":"hi"}],
                "max_completion_tokens": 512,
                "temperature": 0.7,
                "top_p": 0.9
            }
            """);
        var modelInfo = ChatJson.ParseObject("""{"id":"gemini-2.5-flash","reasoning":true,"thinking":true}""");

        var body = CreateProvider().ToGeminiRequest(chat, modelInfo, hasMediaModality: false);
        var config = body["generationConfig"]!.AsObject();

        // OpenAI names map to Gemini's generationConfig names
        Assert.That(config["maxOutputTokens"]!.GetValue<int>(), Is.EqualTo(512));
        Assert.That(config["temperature"]!.GetValue<double>(), Is.EqualTo(0.7));
        Assert.That(config["topP"]!.GetValue<double>(), Is.EqualTo(0.9));
        // thinking models get the configured thinkingConfig
        Assert.That(config["thinkingConfig"]!["thinkingBudget"]!.GetValue<int>(), Is.EqualTo(1024));

        // explicitly disabling thinking suppresses it
        chat["enable_thinking"] = false;
        var noThinking = CreateProvider().ToGeminiRequest(chat, modelInfo, hasMediaModality: false);
        Assert.That(noThinking["generationConfig"]!.AsObject().ContainsKey("thinkingConfig"), Is.False);
    }

    [Test]
    public void Converts_messages_to_gemini_contents()
    {
        var chat = ChatJson.ParseObject("""
            {"messages":[
                {"role":"system","content":"You are helpful"},
                {"role":"user","content":"hi"},
                {"role":"assistant","content":"hello"}
            ]}
            """);

        var (contents, systemPrompt) = GoogleProvider.ToGeminiContents(chat);

        Assert.That(systemPrompt, Is.EqualTo("You are helpful"));
        Assert.That(contents.Count, Is.EqualTo(2), "the system message is hoisted out");
        Assert.That(contents[0]!["role"]!.GetValue<string>(), Is.EqualTo("user"));
        Assert.That(contents[0]!["parts"]!.AsArray()[0]!["text"]!.GetValue<string>(), Is.EqualTo("hi"));
        // OpenAI's "assistant" is Gemini's "model"
        Assert.That(contents[1]!["role"]!.GetValue<string>(), Is.EqualTo("model"));
    }

    [Test]
    public void Converts_tool_calls_and_responses_by_function_name()
    {
        var chat = ChatJson.ParseObject("""
            {"messages":[
                {"role":"user","content":"weather?"},
                {"role":"assistant","tool_calls":[
                    {"id":"call_1","type":"function","function":{"name":"get_weather","arguments":"{\"city\":\"Perth\"}"}}
                ]},
                {"role":"tool","tool_call_id":"call_1","content":"{\"temp\":22}"}
            ]}
            """);

        var (contents, _) = GoogleProvider.ToGeminiContents(chat);

        var functionCall = contents[1]!["parts"]!.AsArray()[0]!["functionCall"]!.AsObject();
        Assert.That(functionCall["name"]!.GetValue<string>(), Is.EqualTo("get_weather"));
        // OpenAI's arguments string becomes Gemini's args object
        Assert.That(functionCall["args"]!["city"]!.GetValue<string>(), Is.EqualTo("Perth"));

        // Gemini identifies tool results by function name, not by call id
        var functionResponse = contents[2]!["parts"]!.AsArray()[0]!["functionResponse"]!.AsObject();
        Assert.That(contents[2]!["role"]!.GetValue<string>(), Is.EqualTo("function"));
        Assert.That(functionResponse["name"]!.GetValue<string>(), Is.EqualTo("get_weather"));
        Assert.That(functionResponse["response"]!["temp"]!.GetValue<int>(), Is.EqualTo(22));
    }

    [Test]
    public void Converts_images_to_inline_data_parts()
    {
        var chat = ChatJson.ParseObject("""
            {"messages":[{"role":"user","content":[
                {"type":"text","text":"describe"},
                {"type":"image_url","image_url":{"url":"data:image/png;base64,AAAA"}}
            ]}]}
            """);

        var (contents, _) = GoogleProvider.ToGeminiContents(chat);
        var parts = contents[0]!["parts"]!.AsArray();

        // parts keep the order of the source content
        Assert.That(parts[0]!["text"]!.GetValue<string>(), Is.EqualTo("describe"));
        var inline = parts[1]!["inline_data"]!.AsObject();
        Assert.That(inline["mime_type"]!.GetValue<string>(), Is.EqualTo("image/png"));
        Assert.That(inline["data"]!.GetValue<string>(), Is.EqualTo("AAAA"));
    }

    [Test]
    public void Rejects_unresolved_image_urls()
    {
        // ProcessChatAsync resolves urls to data URIs first; anything else is a bug worth surfacing
        var chat = ChatJson.ParseObject("""
            {"messages":[{"role":"user","content":[
                {"type":"image_url","image_url":{"url":"https://example.org/cat.png"}}
            ]}]}
            """);

        var ex = Assert.Throws<Exception>(() => GoogleProvider.ToGeminiContents(chat));
        Assert.That(ex!.Message, Does.Contain("Image was not downloaded"));
    }

    [Test]
    public async Task Accumulates_sse_candidates_and_separates_thoughts()
    {
        // Gemini streams with ?alt=sse, so ordinary data: lines carrying generateContent payloads
        const string sse = """
            data: {"responseId":"resp_1","modelVersion":"gemini-2.5-flash","candidates":[{"content":{"parts":[{"text":"Thinking about it","thought":true}]}}]}

            data: {"candidates":[{"content":{"parts":[{"text":"Hello"}]}}]}

            data: {"candidates":[{"content":{"parts":[{"text":", world!"}]},"finishReason":"STOP"}],"usageMetadata":{"promptTokenCount":12,"candidatesTokenCount":5}}
            """;

        var res = await CreateProvider().HandleGeminiStreamAsync(
            SseResponse(sse), ChatJson.ParseObject("""{"model":"gemini-2.5-flash","messages":[]}"""),
            DateTimeOffset.UtcNow, new ChatContext());

        Assert.That(res, Is.Not.Null);
        Assert.That(res!["id"]!.GetValue<string>(), Is.EqualTo("resp_1"));
        Assert.That(res["model"]!.GetValue<string>(), Is.EqualTo("gemini-2.5-flash"));

        var message = res["choices"]!.AsArray()[0]!["message"]!.AsObject();
        Assert.That(message["content"]!.GetValue<string>(), Is.EqualTo("Hello, world!"));
        // parts flagged thought:true are reasoning, not content
        Assert.That(message["reasoning_content"]!.GetValue<string>(), Is.EqualTo("Thinking about it"));

        Assert.That(res["usage"]!["prompt_tokens"]!.GetValue<long>(), Is.EqualTo(12));
        Assert.That(res["usage"]!["completion_tokens"]!.GetValue<long>(), Is.EqualTo(5));
        Assert.That(res["usage"]!["total_tokens"]!.GetValue<long>(), Is.EqualTo(17));
    }

    [Test]
    public async Task Streams_function_calls()
    {
        const string sse = """
            data: {"candidates":[{"content":{"parts":[{"functionCall":{"name":"get_weather","args":{"city":"Perth"}}}]},"finishReason":"STOP"}]}
            """;

        var res = await CreateProvider().HandleGeminiStreamAsync(
            SseResponse(sse), ChatJson.ParseObject("""{"model":"gemini-2.5-flash","messages":[]}"""),
            DateTimeOffset.UtcNow, new ChatContext());

        var toolCall = res!["choices"]!.AsArray()[0]!["message"]!["tool_calls"]!.AsArray()[0]!.AsObject();
        Assert.That(toolCall["function"]!["name"]!.GetValue<string>(), Is.EqualTo("get_weather"));
        // Gemini's args object becomes OpenAI's arguments string
        Assert.That(toolCall["function"]!["arguments"]!.GetValue<string>(), Is.EqualTo("""{"city":"Perth"}"""));
        Assert.That(toolCall["type"]!.GetValue<string>(), Is.EqualTo("function"));
    }

    [Test]
    public void Maps_candidates_to_an_openai_response()
    {
        var response = ChatJson.ParseObject("""
            {
                "responseId": "resp_2",
                "modelVersion": "gemini-2.5-flash",
                "candidates": [{
                    "content": { "parts": [
                        {"text":"reasoning here","thought":true},
                        {"text":"The answer"}
                    ]},
                    "finishReason": "STOP"
                }],
                "usageMetadata": { "promptTokenCount": 8, "candidatesTokenCount": 3 }
            }
            """);

        var res = CreateProvider().ToGeminiResponse(response,
            ChatJson.ParseObject("""{"model":"gemini-2.5-flash"}"""), DateTimeOffset.UtcNow, new ChatContext());

        var message = res["choices"]!.AsArray()[0]!["message"]!.AsObject();
        Assert.That(message["content"]!.GetValue<string>(), Is.EqualTo("The answer"));
        Assert.That(message["reasoning_content"]!.GetValue<string>(), Is.EqualTo("reasoning here"));
        Assert.That(res["usage"]!["total_tokens"]!.GetValue<long>(), Is.EqualTo(11));
        // pricing metadata resolves from the model catalogue
        Assert.That(res["metadata"]!["pricing"]!.GetValue<string>(), Is.EqualTo("0.3/2.5"));
    }
}
