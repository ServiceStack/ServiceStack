#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Text;
using JsonObject = System.Text.Json.Nodes.JsonObject;

namespace ServiceStack.Extensions.Tests;

/// <summary>Captures the JSON the pipeline hands a provider and replies with a canned response</summary>
public class FakeChatProvider(JsonObject reply) : ChatProvider
{
    public JsonObject? ReceivedChat { get; private set; }
    public ChatContext? ReceivedContext { get; private set; }

    public override Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        ReceivedChat = chat;
        ReceivedContext = context;
        return Task.FromResult(reply);
    }
}

/// <summary>Replies with each scripted response in turn, driving the tool loop</summary>
public class ScriptedChatProvider(JsonObject[] replies) : ChatProvider
{
    public int Calls { get; private set; }
    public List<JsonObject> ReceivedChats { get; } = [];

    public override Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
    {
        ReceivedChats.Add(chat.Clone());
        return Task.FromResult(replies[Calls++].Clone());
    }
}

public class CapturingApprovalCoordinator : IChatToolApprovalCoordinator
{
    public List<PendingChatToolCall> Calls { get; } = [];
    public Task PauseAsync(IReadOnlyList<PendingChatToolCall> calls, ChatContext context)
    {
        Calls.AddRange(calls);
        return Task.CompletedTask;
    }
    public bool HasPending(long threadId, string? user) => Calls.Count > 0;
    public Task CancelThreadAsync(long threadId, string? user) => Task.CompletedTask;
}

public class AiChatClientTests
{
    const string Model = "kimi-k2";

    /// <summary>A provider whose first reply requests a tool, driving the orchestrator's tool loop</summary>
    static (ChatFeature Feature, ScriptedChatProvider Provider) CreateToolLoopFeature()
    {
        var provider = new ScriptedChatProvider([
            ChatJson.ParseObject("""
            {
              "id": "1", "object": "chat.completion", "created": 1, "model": "kimi-k2",
              "choices": [{ "index": 0, "finish_reason": "tool_calls", "message": {
                "role": "assistant", "content": "",
                "tool_calls": [{ "id": "call_1", "type": "function",
                  "function": { "name": "get_weather", "arguments": "{\"city\":\"Sydney\"}" } }]
              }}],
              "usage": { "prompt_tokens": 10, "completion_tokens": 4, "total_tokens": 14 }
            }
            """),
            ChatJson.ParseObject("""
            {
              "id": "2", "object": "chat.completion", "created": 2, "model": "kimi-k2",
              "choices": [{ "index": 0, "finish_reason": "stop", "message": {
                "role": "assistant", "content": "It is sunny." }}],
              "usage": { "prompt_tokens": 20, "completion_tokens": 5, "total_tokens": 25 }
            }
            """),
        ])
        {
            Id = "fake",
            Name = "fake",
            // the orchestrator only runs tools when the model advertises support
            Models = { [Model] = new JsonObject { ["id"] = Model, ["tool_call"] = true } },
        };
        var feature = new ChatFeature { Providers = { ["fake"] = provider } };
        feature.Tools.Register(new ChatTool
        {
            Definition = new JsonObject {
                ["type"] = "function",
                ["function"] = new JsonObject {
                    ["name"] = "get_weather",
                    ["description"] = "Get the weather",
                    ["parameters"] = new JsonObject { ["type"] = "object" },
                },
            },
            Handler = (args, _) => Task.FromResult<object?>($"sunny in {args.GetString("city")}"),
        });
        return (feature, provider);
    }

    static (ChatFeature Feature, FakeChatProvider Provider) CreateFeature(JsonObject? reply = null)
    {
        var provider = new FakeChatProvider(reply ?? Reply())
        {
            Id = "fake",
            Name = "fake",
            Models = { [Model] = new JsonObject { ["id"] = Model } },
        };
        var feature = new ChatFeature { Providers = { ["fake"] = provider } };
        return (feature, provider);
    }

    /// <summary>Parsed rather than composed, so numbers behave like a real provider's JSON</summary>
    static JsonObject Reply(string content = "Hello!") => ChatJson.ParseObject($$"""
        {
          "id": "chatcmpl-123", "object": "chat.completion", "created": 1770000000,
          "model": "{{Model}}", "provider": "fake",
          "choices": [{
            "index": 0, "finish_reason": "stop",
            "message": { "role": "assistant", "content": "{{content}}" }
          }],
          "usage": { "prompt_tokens": 12, "completion_tokens": 34, "total_tokens": 46 }
        }
        """);

    static ChatCompletion Request() => new()
    {
        Model = Model,
        Messages = [
            Message.Text("Hi"),
        ],
    };

    // ── Typed request -> OpenAI JSON ──

    [Test]
    public void Converts_a_typed_request_to_OpenAI_wire_format()
    {
        var chat = ChatClient.ToChatJson(new ChatCompletion
        {
            Model = Model,
            Temperature = 0.7,
            MaxCompletionTokens = 512,
            Messages = [
                Message.Image(imageUrl: "https://x/y.png", text: "What is this?")
            ],
        });

        Assert.That(chat.GetString("model"), Is.EqualTo(Model));
        Assert.That(chat.GetDouble("temperature"), Is.EqualTo(0.7));
        // [DataMember] names win over the property names
        Assert.That(chat.GetInt("max_completion_tokens"), Is.EqualTo(512));
        // unset optionals are omitted rather than sent as null
        Assert.That(chat.ContainsKey("top_p"), Is.False);
        Assert.That(chat.ContainsKey("seed"), Is.False);

        var content = (chat.GetArray("messages")![0] as JsonObject).GetArray("content")!;
        Assert.That(content.Count, Is.EqualTo(2));
        // polymorphic parts keep their own shape, with no __type pollution
        Assert.That((content[0] as JsonObject).GetString("type"), Is.EqualTo("text"));
        Assert.That((content[0] as JsonObject).GetString("text"), Is.EqualTo("What is this?"));
        Assert.That((content[1] as JsonObject).GetString("type"), Is.EqualTo("image_url"));
        Assert.That((content[1] as JsonObject).GetObject("image_url").GetString("url"),
            Is.EqualTo("https://x/y.png"));
        Assert.That((content[0] as JsonObject)!.ContainsKey("__type"), Is.False);
    }

    // ── Provider JSON -> typed response ──

    [Test]
    public void Converts_a_provider_response_to_the_typed_DTO()
    {
        var res = ChatClient.FromChatJson(ChatJson.ParseObject("""
        {
          "id": "chatcmpl-123", "object": "chat.completion", "created": 1770000000,
          "model": "kimi-k2", "provider": "groq",
          "choices": [{
            "index": 0, "finish_reason": "stop",
            "message": { "role": "assistant", "content": "Sunny.", "reasoning": "thinking" }
          }],
          "usage": {
            "prompt_tokens": 12, "completion_tokens": 34, "total_tokens": 46,
            "completion_tokens_details": { "reasoning_tokens": 7 }
          },
          "a_field_the_dto_does_not_model": { "a": 1 }
        }
        """));

        Assert.That(res.Id, Is.EqualTo("chatcmpl-123"));
        Assert.That(res.Object, Is.EqualTo("chat.completion"));
        Assert.That(res.Created, Is.EqualTo(1770000000L));
        Assert.That(res.Model, Is.EqualTo(Model));
        Assert.That(res.Provider, Is.EqualTo("groq"));
        Assert.That(res.Choices, Has.Count.EqualTo(1));
        Assert.That(res.Choices[0].FinishReason, Is.EqualTo("stop"));
        Assert.That(res.Choices[0].Message.Role, Is.EqualTo("assistant"));
        Assert.That(res.Choices[0].Message.Content, Is.EqualTo("Sunny."));
        Assert.That(res.Choices[0].Message.Reasoning, Is.EqualTo("thinking"));
        Assert.That(res.Usage.PromptTokens, Is.EqualTo(12));
        Assert.That(res.Usage.CompletionTokens, Is.EqualTo(34));
        Assert.That(res.Usage.TotalTokens, Is.EqualTo(46));
        Assert.That(res.Usage.CompletionTokensDetails?.ReasoningTokens, Is.EqualTo(7));
    }

    // ── End to end through the real pipeline ──

    [Test]
    public async Task Runs_a_completion_through_the_chat_pipeline()
    {
        var (feature, provider) = CreateFeature();
        var client = new ChatClient(feature);

        var res = await client.ChatAsync(Request());

        // the provider was handed the converted request
        Assert.That(provider.ReceivedChat.GetString("model"), Is.EqualTo(Model));
        var messages = provider.ReceivedChat.GetArray("messages")!;
        Assert.That((messages[0] as JsonObject).GetString("role"), Is.EqualTo("user"));

        // and its reply came back typed
        Assert.That(res.Id, Is.EqualTo("chatcmpl-123"));
        Assert.That(res.Choices[0].Message.Content, Is.EqualTo("Hello!"));
        Assert.That(res.Usage.TotalTokens, Is.EqualTo(46));
    }

    [Test]
    public void Throws_when_no_provider_serves_the_model()
    {
        var (feature, _) = CreateFeature();
        var client = new ChatClient(feature);

        var request = Request();
        request.Model = "not-a-model";

        Assert.That(async () => await client.ChatAsync(request),
            Throws.Exception.Message.Contains("not-a-model"));
    }

    [Test]
    public async Task Flows_the_cancellation_token_into_the_pipeline()
    {
        var (feature, provider) = CreateFeature();
        var client = new ChatClient(feature);
        using var cts = new CancellationTokenSource();

        await client.ChatAsync(Request(), cts.Token);

        Assert.That(provider.ReceivedContext!.CancellationToken, Is.EqualTo(cts.Token));
    }

    [Test]
    public async Task Reads_the_same_metadata_keys_as_the_HTTP_service()
    {
        var (feature, provider) = CreateFeature();
        var client = new ChatClient(feature);

        var request = Request();
        // a typed DTO's Metadata is Dictionary<string,string>, so these arrive as strings
        request.Metadata = new Dictionary<string, string> {
            ["user"] = "bob",
            ["threadId"] = "42",
            ["tools"] = "none",
            ["nostore"] = "true",
        };

        await client.ChatAsync(request);

        var context = provider.ReceivedContext!;
        Assert.That(context.User, Is.EqualTo("bob"));
        Assert.That(context.ThreadId, Is.EqualTo(42));
        Assert.That(context.Tools, Is.EqualTo("none"));
        Assert.That(context.NoStore, Is.True);
        // nostore implies nohistory, as in the HTTP service
        Assert.That(context.NoHistory, Is.True);
    }

    [Test]
    public async Task Defaults_to_no_user_and_all_tools()
    {
        var (feature, provider) = CreateFeature();
        var client = new ChatClient(feature);

        await client.ChatAsync(Request());

        var context = provider.ReceivedContext!;
        Assert.That(context.User, Is.Null);
        Assert.That(context.Tools, Is.EqualTo("all"));
        Assert.That(context.NoStore, Is.False);
        Assert.That(context.NoHistory, Is.False);
    }

    [Test]
    public void Reads_real_JSON_booleans_in_metadata_too()
    {
        // the HTTP path parses raw OpenAI JSON, where these are real booleans
        var chat = ChatJson.ParseObject("""
            { "model": "kimi-k2", "metadata": { "threadId": 7, "nohistory": true } }
            """);

        var context = ChatContext.FromChat(chat, user: "ann");

        Assert.That(context.User, Is.EqualTo("ann"));
        Assert.That(context.ThreadId, Is.EqualTo(7));
        Assert.That(context.NoHistory, Is.True);
        Assert.That(context.NoStore, Is.False);
    }

    // ── DTO wire-format fixes ──

    [Test]
    public void Serializes_a_tool_definition_with_its_JSON_Schema_parameters()
    {
        var chat = ChatClient.ToChatJson(new ChatCompletion
        {
            Model = Model,
            Tools = [
                new Tool {
                    Type = ToolType.Function,
                    Function = new AiToolFunction {
                        Name = "get_weather",
                        Description = "Get the weather",
                        Parameters = new Dictionary<string, object> {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object> {
                                ["city"] = new Dictionary<string, object> {
                                    ["type"] = "string", ["description"] = "The city",
                                },
                            },
                            ["required"] = new List<string> { "city" },
                        },
                    },
                },
            ],
        });

        var fn = (chat.GetArray("tools")![0] as JsonObject).GetObject("function")!;
        Assert.That(fn.GetString("name"), Is.EqualTo("get_weather"));
        Assert.That(fn.GetString("description"), Is.EqualTo("Get the weather"));

        // nested JSON Schema stays real JSON rather than collapsing to a JSV string
        var schema = fn.GetObject("parameters")!;
        Assert.That(schema.GetString("type"), Is.EqualTo("object"));
        var city = schema.GetObject("properties").GetObject("city")!;
        Assert.That(city.GetString("type"), Is.EqualTo("string"));
        Assert.That(city.GetString("description"), Is.EqualTo("The city"));
        Assert.That(schema.GetArray("required")![0]!.GetValue<string>(), Is.EqualTo("city"));
    }

    [Test]
    public void Serializes_response_format_in_OpenAI_shape()
    {
        var chat = ChatClient.ToChatJson(new ChatCompletion
        {
            Model = Model,
            ResponseFormat = new AiResponseFormat { Type = ResponseFormat.JsonObject },
        });

        // {"response_format":{"type":"json_object"}}, not a doubly-nested "response_format"
        Assert.That(chat.GetObject("response_format").GetString("type"), Is.EqualTo("json_object"));
    }

    [Test]
    public void Parses_a_tool_call_into_a_typed_function()
    {
        var res = ChatClient.FromChatJson(ChatJson.ParseObject("""
        {
          "id": "1", "object": "chat.completion", "created": 1, "model": "kimi-k2",
          "choices": [{
            "index": 0, "finish_reason": "tool_calls",
            "message": { "role": "assistant", "content": "", "tool_calls": [{
              "id": "call_1", "type": "function",
              "function": { "name": "get_weather", "arguments": "{\"city\":\"Sydney\"}" }
            }]}
          }]
        }
        """));

        var toolCall = res.Choices[0].Message.ToolCalls![0];
        Assert.That(toolCall.Id, Is.EqualTo("call_1"));
        Assert.That(toolCall.Type, Is.EqualTo("function"));
        Assert.That(toolCall.Function.Name, Is.EqualTo("get_weather"));
        // arguments stay valid JSON the caller can parse, not a JSV blob
        Assert.That(toolCall.Function.Arguments, Is.EqualTo("""{"city":"Sydney"}"""));
        Assert.That(ChatJson.ParseObject(toolCall.Function.Arguments).GetString("city"),
            Is.EqualTo("Sydney"));
    }

    [Test]
    public void Reads_token_counts_and_timestamps_beyond_int_range()
    {
        var res = ChatClient.FromChatJson(ChatJson.ParseObject("""
        {
          "id": "1", "object": "chat.completion", "created": 1770000000, "model": "kimi-k2",
          "choices": [{
            "index": 0, "finish_reason": "stop",
            "message": { "role": "assistant", "content": "hi",
              "audio": { "id": "a", "data": "x", "transcript": "t", "expires_at": 2200000000 } }
          }],
          "usage": {
            "prompt_tokens": 3000000000, "completion_tokens": 5, "total_tokens": 3000000005,
            "prompt_tokens_details": { "cached_tokens": 2500000000 }
          }
        }
        """));

        // these silently parsed as 0 while the DTOs used int
        Assert.That(res.Usage.PromptTokens, Is.EqualTo(3000000000L));
        Assert.That(res.Usage.TotalTokens, Is.EqualTo(3000000005L));
        Assert.That(res.Usage.PromptTokensDetails?.CachedTokens, Is.EqualTo(2500000000L));
        Assert.That(res.Choices[0].Message.Audio?.ExpiresAt, Is.EqualTo(2200000000L)); // past 2038
    }

    [Test]
    public void Keeps_the_fields_the_pipeline_and_providers_add()
    {
        // tool_history/cost/usage.duration are added by ChatOrchestrator; reasoning_content and
        // images by GoogleProvider; thinking by AnthropicProvider; timestamp by every provider
        var res = ChatClient.FromChatJson(ChatJson.ParseObject("""
        {
          "id": "1", "object": "chat.completion", "created": 1770000000, "model": "kimi-k2",
          "provider": "google",
          "cost": 0.0042,
          "choices": [{
            "index": 0, "finish_reason": "stop",
            "message": {
              "role": "assistant", "content": "Here it is.", "timestamp": 1770000000123,
              "reasoning_content": "gemini reasoning",
              "thinking": "anthropic reasoning",
              "images": [{ "type": "image_url", "image_url": { "url": "/~cache/ab/x.png" } }],
              "audios": [{ "type": "audio_url", "audio_url": { "url": "/~cache/cd/y.mp3" } }]
            }
          }],
          "tool_history": [
            { "role": "assistant", "content": "", "timestamp": 1770000000001,
              "tool_calls": [{ "id": "call_1", "type": "function",
                "function": { "name": "gen_image", "arguments": "{}" } }] },
            { "role": "tool", "tool_call_id": "call_1", "content": "ok",
              "images": [{ "type": "image_url", "image_url": { "url": "/~cache/ab/x.png" } }] }
          ],
          "usage": {
            "prompt_tokens": 12, "completion_tokens": 34, "total_tokens": 46, "duration": 9
          }
        }
        """));

        Assert.That(res.Cost, Is.EqualTo(0.0042));
        Assert.That(res.Usage.Duration, Is.EqualTo(9));

        var msg = res.Choices[0].Message;
        Assert.That(msg.Timestamp, Is.EqualTo(1770000000123L));
        Assert.That(msg.ReasoningContent, Is.EqualTo("gemini reasoning"));
        Assert.That(msg.Thinking, Is.EqualTo("anthropic reasoning"));
        // media parts resolve to their concrete content types
        Assert.That(msg.Images![0], Is.TypeOf<AiImageContent>());
        Assert.That(((AiImageContent)msg.Images[0]).ImageUrl.Url, Is.EqualTo("/~cache/ab/x.png"));
        Assert.That(msg.Audios![0], Is.TypeOf<AiAudioUrlContent>());
        Assert.That(((AiAudioUrlContent)msg.Audios[0]).AudioUrl.Url, Is.EqualTo("/~cache/cd/y.mp3"));

        Assert.That(res.ToolHistory, Has.Count.EqualTo(2));
        Assert.That(res.ToolHistory![0].ToolCalls![0].Function.Name, Is.EqualTo("gen_image"));
        Assert.That(res.ToolHistory[1].Role, Is.EqualTo("tool"));
        Assert.That(res.ToolHistory[1].ToolCallId, Is.EqualTo("call_1"));
        Assert.That(res.ToolHistory[1].Images![0], Is.TypeOf<AiImageContent>());
    }

    [Test]
    public async Task Surfaces_the_tool_loops_history_and_cost_end_to_end()
    {
        // first reply asks for a tool, second is the final answer — the real orchestrator loop
        var (feature, provider) = CreateToolLoopFeature();
        var client = new ChatClient(feature);

        var res = await client.ChatAsync(Request());

        Assert.That(res.Choices[0].Message.Content, Is.EqualTo("It is sunny."));
        // the orchestrator recorded the assistant tool-call turn and the tool result
        Assert.That(res.ToolHistory, Is.Not.Null.And.Count.EqualTo(2));
        Assert.That(res.ToolHistory![0].ToolCalls![0].Function.Name, Is.EqualTo("get_weather"));
        Assert.That(res.ToolHistory[1].Role, Is.EqualTo("tool"));
        Assert.That(res.ToolHistory[1].ToolCallId, Is.EqualTo("call_1"));
        // and aggregated usage across both requests
        Assert.That(res.Usage.CompletionTokens, Is.EqualTo(9)); // 4 + 5
        Assert.That(res.Usage.Duration, Is.Not.Null);
        Assert.That(provider.Calls, Is.EqualTo(2));
    }

    [Test]
    public async Task Pauses_a_guarded_tool_before_its_handler_executes()
    {
        var provider = new ScriptedChatProvider([ChatJson.ParseObject("""
        {
          "id":"approval-1", "model":"kimi-k2",
          "choices":[{"finish_reason":"tool_calls","message":{"role":"assistant","content":"","tool_calls":[{
            "id":"call_unsafe","type":"function","function":{"name":"unsafe_tool","arguments":"{\"value\":1}"}
          }]}}],
          "usage":{"prompt_tokens":7,"completion_tokens":3,"total_tokens":10}
        }
        """)])
        {
            Id = "fake",
            Models = { [Model] = new JsonObject { ["id"] = Model, ["tool_call"] = true } },
        };
        var approvals = new CapturingApprovalCoordinator();
        var executed = false;
        var approvalFilterCalled = false;
        var feature = new ChatFeature
        {
            Providers = { ["fake"] = provider },
            ToolApprovalCoordinator = approvals,
        };
        feature.Filters.ChatApprovalFilters.Add((_, _) =>
        {
            approvalFilterCalled = true;
            return Task.CompletedTask;
        });
        feature.Tools.Register(new ChatTool
        {
            Definition = ChatJson.ParseObject("""
                {"type":"function","function":{"name":"unsafe_tool","parameters":{"type":"object"}}}
                """),
            Handler = (_, _) =>
            {
                executed = true;
                return Task.FromResult<object?>("executed");
            },
            ApprovalHandler = (args, _) => Task.FromResult<ChatToolApprovalRequest?>(new()
            {
                Title = "Unsafe tool",
                Safety = ToolSafety.Destructive,
                Schema = new JsonObject { ["type"] = "object" },
                Arguments = args.Clone(),
            }),
        });
        var chat = ChatJson.ParseObject("""
            {"model":"kimi-k2","messages":[{"role":"user","content":"do it"}],"metadata":{"threadId":42}}
            """);

        var response = await feature.ChatCompletionAsync(chat, ChatContext.FromChat(chat, "ann"));

        Assert.That(executed, Is.False);
        Assert.That(provider.Calls, Is.EqualTo(1));
        Assert.That(approvals.Calls, Has.Count.EqualTo(1));
        Assert.That(approvals.Calls[0].ToolCallId, Is.EqualTo("call_unsafe"));
        Assert.That(approvals.Calls[0].Approval.Arguments.GetInt("value"), Is.EqualTo(1));
        Assert.That(approvalFilterCalled, Is.True);
        Assert.That(response.GetObject("usage").GetLong("total_tokens"), Is.EqualTo(10));
    }

    [Test]
    public async Task Guarded_tool_fails_closed_without_an_interactive_coordinator()
    {
        var provider = new ScriptedChatProvider([
            ChatJson.ParseObject("""
            {"id":"1","model":"kimi-k2","choices":[{"finish_reason":"tool_calls","message":{"role":"assistant","content":"","tool_calls":[{
              "id":"call_unsafe","type":"function","function":{"name":"unsafe_tool","arguments":"{}"}
            }]}}]}
            """),
            ChatJson.ParseObject("""
            {"id":"2","model":"kimi-k2","choices":[{"finish_reason":"stop","message":{"role":"assistant","content":"I could not run it."}}]}
            """),
        ])
        {
            Id = "fake",
            Models = { [Model] = new JsonObject { ["id"] = Model, ["tool_call"] = true } },
        };
        var executed = false;
        var feature = new ChatFeature { Providers = { ["fake"] = provider } };
        feature.Tools.Register(new ChatTool
        {
            Definition = ChatJson.ParseObject("""
                {"type":"function","function":{"name":"unsafe_tool","parameters":{"type":"object"}}}
                """),
            Handler = (_, _) =>
            {
                executed = true;
                return Task.FromResult<object?>("executed");
            },
            ApprovalHandler = (_, _) => Task.FromResult<ChatToolApprovalRequest?>(new()
            {
                Title = "Unsafe tool",
                Safety = ToolSafety.Write,
                Schema = new JsonObject { ["type"] = "object" },
                Arguments = new JsonObject(),
            }),
        });
        var chat = ChatJson.ParseObject("""
            {"model":"kimi-k2","messages":[{"role":"user","content":"do it"}]}
            """);

        var response = await feature.ChatCompletionAsync(chat, ChatContext.FromChat(chat, "ann"));

        Assert.That(executed, Is.False);
        Assert.That(response.GetArray("choices")![0]!["message"]!["content"]!.GetValue<string>(),
            Is.EqualTo("I could not run it."));
        var messages = provider.ReceivedChats[1].GetArray("messages")!;
        Assert.That(messages.OfType<JsonObject>().Single(x => x.GetString("role") == "tool").GetString("content"),
            Does.Contain("requires interactive approval"));
    }

    [Test]
    public async Task Explicit_tool_execution_bypasses_model_approval_preflight()
    {
        var executed = false;
        var feature = new ChatFeature();
        feature.Tools.Register(new ChatTool
        {
            Definition = ChatJson.ParseObject("""
                {"type":"function","function":{"name":"unsafe_tool","parameters":{"type":"object"}}}
                """),
            Handler = (_, _) =>
            {
                executed = true;
                return Task.FromResult<object?>("executed");
            },
            ApprovalHandler = (_, _) => Task.FromResult<ChatToolApprovalRequest?>(new()
            {
                Title = "Unsafe tool",
                Safety = ToolSafety.Write,
                Schema = new JsonObject { ["type"] = "object" },
                Arguments = new JsonObject(),
            }),
        });

        var result = await feature.ExecToolAsync("unsafe_tool", new JsonObject(), new ChatContext());

        Assert.That(executed, Is.True);
        Assert.That(result.Content, Is.EqualTo("executed"));

        executed = false;
        var modelContext = new ChatContext();
        modelContext.Items[ChatContext.RejectToolsRequiringApproval] = true;
        result = await feature.ExecToolAsync("unsafe_tool", new JsonObject(), modelContext);
        Assert.That(executed, Is.False);
        Assert.That(result.Content, Does.Contain("requires interactive approval"));
    }

    [Test]
    public void GetLong_reads_a_value_built_from_a_CSharp_int()
    {
        Assert.That(new JsonObject { ["n"] = 12 }.GetLong("n"), Is.EqualTo(12L));
        Assert.That(new JsonObject { ["n"] = 12L }.GetLong("n"), Is.EqualTo(12L));
        Assert.That(ChatJson.ParseObject("""{"n":12}""").GetLong("n"), Is.EqualTo(12L));
    }

    [Test]
    public void Never_takes_the_user_from_the_request()
    {
        // guards against a client selecting whose data a completion is attributed to
        var chat = ChatJson.ParseObject("""
            { "model": "kimi-k2", "metadata": { "user": "admin" } }
            """);

        Assert.That(ChatContext.FromChat(chat, user: "bob").User, Is.EqualTo("bob"));
        Assert.That(ChatContext.FromChat(chat, user: null).User, Is.Null);
    }
}
