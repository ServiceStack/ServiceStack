#nullable enable
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

/// <summary>
/// Anthropic uses a different wire format to OpenAI in all three directions, so each is pinned here:
/// request translation, the typed SSE event stream, and the content-block response.
/// </summary>
public class AiChatAnthropicTests
{
    static AnthropicProvider CreateProvider()
    {
        var provider = new AnthropicProvider();
        provider.Populate(ChatJson.ParseObject(
            """
            {
                "id": "anthropic",
                "api_key": "sk-ant-test",
                "models": {
                    "claude-sonnet-4-5": {
                        "id": "claude-sonnet-4-5",
                        "name": "Claude Sonnet 4.5",
                        "tool_call": true,
                        "cost": { "input": 3, "output": 15 }
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
    public void Uses_messages_endpoint_and_x_api_key_auth()
    {
        var provider = CreateProvider();
        Assert.That(provider.ChatUrl, Is.EqualTo("https://api.anthropic.com/v1/messages"));
        // Anthropic authenticates with x-api-key, NOT Authorization: Bearer
        Assert.That(provider.Headers.ContainsKey("Authorization"), Is.False);
        Assert.That(provider.Headers["x-api-key"], Is.EqualTo("sk-ant-test"));
        Assert.That(provider.Headers["anthropic-version"], Is.EqualTo("2023-06-01"));
    }

    [Test]
    public void Hoists_system_prompt_out_of_messages()
    {
        var chat = ChatJson.ParseObject("""
            {"messages":[
                {"role":"system","content":"You are helpful"},
                {"role":"system","content":"Be terse"},
                {"role":"user","content":"hi"}
            ]}
            """);

        var (system, messages) = AnthropicProvider.ToAnthropicMessages(chat);

        Assert.That(system, Is.EqualTo("You are helpful\nBe terse"));
        Assert.That(messages.Count, Is.EqualTo(1), "system messages must not remain in messages[]");
        Assert.That(messages[0]!["role"]!.GetValue<string>(), Is.EqualTo("user"));
        // plain text stays a bare string when there are no other blocks
        Assert.That(messages[0]!["content"]!.GetValue<string>(), Is.EqualTo("hi"));
    }

    [Test]
    public void Converts_tool_calls_and_folds_tool_results_into_a_user_message()
    {
        var chat = ChatJson.ParseObject("""
            {"messages":[
                {"role":"user","content":"weather?"},
                {"role":"assistant","content":"","tool_calls":[
                    {"id":"call_1","type":"function","function":{"name":"get_weather","arguments":"{\"city\":\"Perth\"}"}}
                ]},
                {"role":"tool","tool_call_id":"call_1","content":"22C"}
            ]}
            """);

        var (_, messages) = AnthropicProvider.ToAnthropicMessages(chat);

        var assistant = messages[1]!.AsObject();
        Assert.That(assistant["role"]!.GetValue<string>(), Is.EqualTo("assistant"));
        var toolUse = assistant["content"]!.AsArray()[0]!.AsObject();
        Assert.That(toolUse["type"]!.GetValue<string>(), Is.EqualTo("tool_use"));
        Assert.That(toolUse["id"]!.GetValue<string>(), Is.EqualTo("call_1"));
        Assert.That(toolUse["name"]!.GetValue<string>(), Is.EqualTo("get_weather"));
        // arguments are a JSON string in OpenAI but a parsed object in Anthropic
        Assert.That(toolUse["input"]!["city"]!.GetValue<string>(), Is.EqualTo("Perth"));

        // Anthropic requires tool results inside a user message
        var toolResultMsg = messages[2]!.AsObject();
        Assert.That(toolResultMsg["role"]!.GetValue<string>(), Is.EqualTo("user"));
        var toolResult = toolResultMsg["content"]!.AsArray()[0]!.AsObject();
        Assert.That(toolResult["type"]!.GetValue<string>(), Is.EqualTo("tool_result"));
        Assert.That(toolResult["tool_use_id"]!.GetValue<string>(), Is.EqualTo("call_1"));
    }

    [Test]
    public void Converts_images_to_base64_source_blocks()
    {
        // a 1x1 PNG, declared (wrongly) as jpeg to prove the bytes win
        const string pngB64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
        var chat = ChatJson.ParseObject(
            "{\"messages\":[{\"role\":\"user\",\"content\":["
            + "{\"type\":\"text\",\"text\":\"what is this?\"},"
            + "{\"type\":\"image_url\",\"image_url\":{\"url\":\"data:image/jpeg;base64," + pngB64 + "\"}}"
            + "]}]}");

        var (_, messages) = AnthropicProvider.ToAnthropicMessages(chat);
        var blocks = messages[0]!["content"]!.AsArray();

        Assert.That(blocks[0]!["type"]!.GetValue<string>(), Is.EqualTo("text"));
        var image = blocks[1]!.AsObject();
        Assert.That(image["type"]!.GetValue<string>(), Is.EqualTo("image"));
        Assert.That(image["source"]!["type"]!.GetValue<string>(), Is.EqualTo("base64"));
        // detected from magic bytes, not the (stale) declared type
        Assert.That(image["source"]!["media_type"]!.GetValue<string>(), Is.EqualTo("image/png"));
    }

    [Test]
    public void Detects_image_media_type_from_magic_bytes()
    {
        Assert.That(AnthropicProvider.DetectImageMediaType(
            "iVBORw0KGgoAAAANSUhEUg"), Is.EqualTo("image/png"));
        Assert.That(AnthropicProvider.DetectImageMediaType(
            Convert.ToBase64String([0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0])), Is.EqualTo("image/jpeg"));
        Assert.That(AnthropicProvider.DetectImageMediaType(
            Convert.ToBase64String("GIF89a12345678"u8.ToArray())), Is.EqualTo("image/gif"));
        // unrecognised bytes fall back to the declared type
        Assert.That(AnthropicProvider.DetectImageMediaType(
            Convert.ToBase64String("not an image!!!!"u8.ToArray()), "image/webp"), Is.EqualTo("image/webp"));
    }

    [Test]
    public async Task Accumulates_typed_sse_events_into_an_openai_response()
    {
        // Anthropic streams typed events, not choice deltas
        const string sse = """
            event: message_start
            data: {"type":"message_start","message":{"id":"msg_1","model":"claude-sonnet-4-5","usage":{"input_tokens":25}}}

            event: content_block_start
            data: {"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}

            event: content_block_delta
            data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hello"}}

            event: content_block_delta
            data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":", world!"}}

            event: message_delta
            data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"output_tokens":7}}

            event: message_stop
            data: {"type":"message_stop"}
            """;

        var res = await CreateProvider().HandleAnthropicStreamAsync(
            SseResponse(sse), ChatJson.ParseObject("""{"model":"claude-sonnet-4-5","messages":[]}"""),
            DateTimeOffset.UtcNow, new ChatContext());

        Assert.That(res, Is.Not.Null);
        Assert.That(res!["id"]!.GetValue<string>(), Is.EqualTo("msg_1"));
        Assert.That(res["model"]!.GetValue<string>(), Is.EqualTo("claude-sonnet-4-5"));

        var choice = res["choices"]!.AsArray()[0]!.AsObject();
        Assert.That(choice["message"]!["content"]!.GetValue<string>(), Is.EqualTo("Hello, world!"));
        Assert.That(choice["finish_reason"]!.GetValue<string>(), Is.EqualTo("end_turn"));

        Assert.That(res["usage"]!["prompt_tokens"]!.GetValue<long>(), Is.EqualTo(25));
        Assert.That(res["usage"]!["completion_tokens"]!.GetValue<long>(), Is.EqualTo(7));
        Assert.That(res["usage"]!["total_tokens"]!.GetValue<long>(), Is.EqualTo(32));
        // pricing metadata comes from the model catalogue
        Assert.That(res["metadata"]!["pricing"]!.GetValue<string>(), Is.EqualTo("3/15"));
    }

    [Test]
    public async Task Accumulates_thinking_deltas()
    {
        const string sse = """
            data: {"type":"content_block_start","index":0,"content_block":{"type":"thinking","thinking":"Let me "}}

            data: {"type":"content_block_delta","index":0,"delta":{"type":"thinking_delta","thinking":"work it out."}}

            data: {"type":"content_block_delta","index":1,"delta":{"type":"text_delta","text":"42"}}

            data: {"type":"message_stop"}
            """;

        var res = await CreateProvider().HandleAnthropicStreamAsync(
            SseResponse(sse), ChatJson.ParseObject("""{"model":"claude-sonnet-4-5","messages":[]}"""),
            DateTimeOffset.UtcNow, new ChatContext());

        var message = res!["choices"]!.AsArray()[0]!["message"]!.AsObject();
        Assert.That(message["thinking"]!.GetValue<string>(), Is.EqualTo("Let me work it out."));
        Assert.That(message["content"]!.GetValue<string>(), Is.EqualTo("42"));
    }

    [Test]
    public async Task Assembles_tool_use_from_input_json_deltas()
    {
        const string sse = """
            data: {"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"toolu_1","name":"get_weather"}}

            data: {"type":"content_block_delta","index":0,"delta":{"type":"input_json_delta","partial_json":"{\"city\":"}}

            data: {"type":"content_block_delta","index":0,"delta":{"type":"input_json_delta","partial_json":"\"Perth\"}"}}

            data: {"type":"message_delta","delta":{"stop_reason":"tool_use"}}

            data: {"type":"message_stop"}
            """;

        var res = await CreateProvider().HandleAnthropicStreamAsync(
            SseResponse(sse), ChatJson.ParseObject("""{"model":"claude-sonnet-4-5","messages":[]}"""),
            DateTimeOffset.UtcNow, new ChatContext());

        var choice = res!["choices"]!.AsArray()[0]!.AsObject();
        Assert.That(choice["finish_reason"]!.GetValue<string>(), Is.EqualTo("tool_use"));
        var toolCall = choice["message"]!["tool_calls"]!.AsArray()[0]!.AsObject();
        Assert.That(toolCall["id"]!.GetValue<string>(), Is.EqualTo("toolu_1"));
        Assert.That(toolCall["function"]!["name"]!.GetValue<string>(), Is.EqualTo("get_weather"));
        // fragments are reassembled into the OpenAI arguments string
        Assert.That(toolCall["function"]!["arguments"]!.GetValue<string>(), Is.EqualTo("""{"city":"Perth"}"""));
    }

    [Test]
    public void Surfaces_streaming_error_events()
    {
        const string sse = """
            data: {"type":"error","error":{"type":"overloaded_error","message":"Overloaded"}}
            """;

        var ex = Assert.ThrowsAsync<Exception>(async () =>
            await CreateProvider().HandleAnthropicStreamAsync(
                SseResponse(sse), ChatJson.ParseObject("""{"model":"claude-sonnet-4-5","messages":[]}"""),
                DateTimeOffset.UtcNow, new ChatContext()));
        Assert.That(ex!.Message, Is.EqualTo("Overloaded"));
    }

    [Test]
    public void Maps_content_blocks_to_an_openai_response()
    {
        var response = ChatJson.ParseObject("""
            {
                "id": "msg_2",
                "model": "claude-sonnet-4-5",
                "stop_reason": "end_turn",
                "content": [
                    {"type":"thinking","thinking":"pondering"},
                    {"type":"text","text":"The answer"},
                    {"type":"tool_use","id":"toolu_9","name":"calc","input":{"expression":"1+1"}}
                ],
                "usage": {"input_tokens":10,"output_tokens":4}
            }
            """);

        var res = CreateProvider().ToAnthropicResponse(response,
            ChatJson.ParseObject("""{"model":"claude-sonnet-4-5"}"""), DateTimeOffset.UtcNow, new ChatContext());

        var message = res["choices"]!.AsArray()[0]!["message"]!.AsObject();
        Assert.That(message["content"]!.GetValue<string>(), Is.EqualTo("The answer"));
        Assert.That(message["thinking"]!.GetValue<string>(), Is.EqualTo("pondering"));
        Assert.That(message["tool_calls"]!.AsArray()[0]!["function"]!["arguments"]!.GetValue<string>(),
            Is.EqualTo("""{"expression":"1+1"}"""));
        Assert.That(res["usage"]!["total_tokens"]!.GetValue<long>(), Is.EqualTo(14));
    }
}
