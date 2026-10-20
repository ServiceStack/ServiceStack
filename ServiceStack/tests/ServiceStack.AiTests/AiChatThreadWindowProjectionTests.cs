using System.Text;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.OrmLite;
using ServiceStack.Testing;

namespace ServiceStack.AiTests;

[NonParallelizable]
public class AiChatThreadWindowProjectionTests
{
    BasicAppHost appHost = null!;
    AiChatMigrationTestHost host = null!;
    JsonArray original = null!;
    long id;

    [SetUp]
    public void SetUp()
    {
        appHost = new BasicAppHost();
        appHost.Init();
        host = new AiChatMigrationTestHost();
        host.Feature.Providers["hold"] = new HoldProvider { Id = "hold" };
        host.Install(new AppExtension());
        original = History();
        id = host.Db.InsertThread(new ChatThread { User = "alice", Title = "Test", Model = "model", Messages = original.ToJsonString() });
        host.Db.EnsureChatMessages(id);
    }

    [TearDown]
    public void TearDown()
    {
        host.Dispose();
        appHost.Dispose();
    }

    sealed class HoldProvider : ChatProvider
    {
        public override string? ProviderModel(string model) => model;
        public override JsonObject? ModelInfo(string model) => new() { ["id"] = model, ["limit"] = new JsonObject { ["context"] = 10_000_000 } };
        public override async Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context)
        {
            await Task.Delay(Timeout.Infinite, context.CancellationToken);
            throw new InvalidOperationException("The fixture should be cancelled at shutdown");
        }
    }

    static JsonArray History()
    {
        var roles = new[] { "user", "system", "assistant", "tool", "assistant", "user", "assistant" };
        var result = new JsonArray();
        var signature = new string('x', 72_000);
        for (var i = 0; i < roles.Length; i++)
        {
            var message = new JsonObject { ["role"] = roles[i], ["content"] = $"Message {i}", ["timestamp"] = i + 1 };
            if (roles[i] == "assistant")
                message["provider_metadata"] = new JsonObject { ["google"] = new JsonObject { ["provider"] = "google", ["model"] = "gemini-3.8-flash",
                    ["content"] = new JsonObject { ["role"] = "model", ["parts"] = new JsonArray(new JsonObject { ["text"] = $"Message {i}", ["thoughtSignature"] = signature }) } } };
            result.Add(message);
        }
        result[2]!["tool_calls"] = new JsonArray(new JsonObject { ["id"] = "fetch-1", ["type"] = "function", ["thoughtSignature"] = signature,
            ["extra_content"] = new JsonObject { ["google"] = new JsonObject { ["thought_signature"] = signature } },
            ["function"] = new JsonObject { ["name"] = "fetch_url", ["arguments"] = "{}" } });
        result[3]!["tool_call_id"] = "fetch-1";
        result[6]!["groundingMetadata"] = ChatJson.ParseObject("""{"groundingChunks":[{"retrievedContext":{"uri":"document"}}]}""");
        return result;
    }

    async Task<JsonObject> Get(string suffix = "") => (JsonObject)(await host.SendAsync("GET", $"/ext/app/threads/{id}{suffix}", "alice"))!;
    JsonArray Stored() => new(host.Db.GetActiveMessagesAfter(id, 0).Select(m => {
        var copy = m.DeepClone().AsObject(); copy.Remove("_sequence"); return (JsonNode)copy;
    }).ToArray());
    async Task Patch(JsonArray messages, bool truncate = false) => await host.SendAsync("PATCH", $"/ext/app/threads/{id}", "alice",
        new JsonObject { ["messages"] = messages.DeepClone(), ["truncate"] = truncate });
    async Task Submit(JsonArray messages, bool truncate = false) => await host.SendAsync("POST", $"/ext/app/threads/{id}/chat", "alice",
        new JsonObject { ["messages"] = messages.DeepClone(), ["truncate"] = truncate, ["submissionId"] = Guid.NewGuid().ToString() });
    static void Equal(JsonNode? actual, JsonNode? expected) => Assert.That(actual?.ToJsonString(), Is.EqualTo(expected?.ToJsonString()));

    [Test]
    public async Task Short_signed_history_includes_latest_reply_without_returning_replay_data()
    {
        var before = Stored();
        var dto = await Get();
        Assert.That(dto.GetLong("messageCount"), Is.EqualTo(7));
        Assert.That(dto.GetArray("messages")!.Select(m => m!["_sequence"]!.GetValue<long>()), Is.EqualTo(Enumerable.Range(1, 7)));
        Assert.That(Encoding.UTF8.GetByteCount(dto.ToJsonString()), Is.LessThan(15_000));
        Assert.That(dto.ToJsonString(), Does.Not.Contain("provider_metadata").And.Not.Contain("thoughtSignature").And.Not.Contain("thought_signature"));
        Equal(dto["messages"]![6]!["groundingMetadata"], original[6]!["groundingMetadata"]);
        Equal(Stored(), before);
        Assert.That(host.Feature.ThreadApi.GetThread(id, "alice")!.ToJsonString(), Does.Contain("provider_metadata"));
    }

    [TestCase("?allMessages=true")]
    [TestCase("/messages?take=100&maxBytes=65536")]
    [TestCase("/messages?before=8&take=100&maxBytes=65536")]
    [TestCase("/updates?sig=stale")]
    public async Task Full_history_pages_and_poll_use_lightweight_projection(string suffix)
    {
        var dto = await Get(suffix);
        Assert.That(dto.GetArray("messages")!.Count, Is.EqualTo(7));
        Assert.That(dto.ToJsonString(), Does.Not.Contain("provider_metadata").And.Not.Contain("thoughtSignature"));
    }

    [Test]
    public async Task Previews_and_streaming_checkpoints_hide_replay_state()
    {
        var previews = (JsonArray)(await host.SendAsync("GET", "/ext/app/threads", "alice"))!;
        Assert.That(previews.ToJsonString(), Does.Not.Contain("provider_metadata"));
        Assert.That(previews[0]!["messages"]![0]!["timestamp"]!.GetValue<int>(), Is.EqualTo(7));
        var streaming = original[6]!.DeepClone().AsObject(); streaming["timestamp"] = 8;
        await host.Feature.ThreadApi.CheckpointStreamAsync(id, streaming, "alice");
        var dto = await Get();
        Assert.That(dto["messages"]!.AsArray().Last()!["streaming"]!.GetValue<bool>(), Is.True);
        Assert.That(dto.ToJsonString(), Does.Not.Contain("provider_metadata"));
        Equal(ChatDtos.ParseJson(host.Db.GetThread(id, "alice")!.StreamingMessage), streaming);
    }

    [Test]
    public async Task Sse_connected_event_uses_lightweight_projection()
    {
        var row = host.Db.GetThread(id, "alice", includeMessages: false)!;
        row.CompletedAt = DateTime.Now;
        host.Db.UpdateThreadFields(row, [nameof(ChatThread.CompletedAt)], "alice");
        var stream = (ChatStreamResult)(await host.SendAsync("GET", $"/ext/app/threads/{id}/updates/stream", "alice"))!;
        var request = new ServiceStack.Host.BasicRequest();
        await stream.Write(request.Response);
        var output = Encoding.UTF8.GetString(((MemoryStream)request.Response.OutputStream).ToArray());
        Assert.That(output, Does.Contain("event: connected").And.Contain("\"timestamp\":7").And.Not.Contain("provider_metadata").And.Not.Contain("thoughtSignature"));
    }

    [Test]
    public async Task Full_history_recovers_reply_missing_from_compatibility_projection()
    {
        var row = host.Db.GetThread(id, "alice")!;
        row.Messages = new JsonArray(original.Take(6).Select(m => m!.DeepClone()).ToArray()).ToJsonString();
        host.Db.UpdateThreadFields(row, [nameof(ChatThread.Messages)], "alice");
        var dto = await Get("?allMessages=true");
        Assert.That(dto.GetArray("messages")!.Count, Is.EqualTo(7));
        Assert.That(dto["messages"]![6]!["timestamp"]!.GetValue<long>(), Is.EqualTo(7));
    }

    [Test]
    public async Task Latest_oversized_tool_group_is_complete_and_ranges_expose_the_gap()
    {
        var messages = new JsonArray(
            new JsonObject { ["role"] = "user", ["content"] = new string('a', 180_000), ["timestamp"] = 1 },
            new JsonObject { ["role"] = "assistant", ["content"] = new string('b', 500_000), ["timestamp"] = 2 },
            new JsonObject { ["role"] = "assistant", ["content"] = new string('c', 400_000), ["timestamp"] = 3,
                ["tool_calls"] = new JsonArray(ChatJson.ParseObject("""{"id":"a","function":{"name":"fetch_url","arguments":"{}"}}""")) },
            new JsonObject { ["role"] = "tool", ["content"] = "result", ["tool_call_id"] = "a", ["timestamp"] = 4 });
        await Patch(messages, true);
        var dto = await Get();
        Assert.That(dto.GetArray("messages")!.Select(m => m!["timestamp"]!.GetValue<long>()), Is.EqualTo(new[] { 1, 3, 4 }));
        Assert.That(dto["messageWindow"]!["ranges"]!.AsArray().Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Partial_equal_count_followup_preserves_hidden_answer_and_signatures()
    {
        var incoming = new JsonArray(original.Take(6).Select(m => (JsonNode)ChatClientHistory.Message(m!.AsObject())).ToArray());
        incoming.Add(new JsonObject { ["role"] = "user", ["content"] = "Continue", ["timestamp"] = 8 });
        await Submit(incoming);
        Equal(new JsonArray(Stored().Take(7).Select(m => m!.DeepClone()).ToArray()), original);
        var compatibility = ChatDtos.ParseJson(host.Db.GetThread(id, "alice")!.Messages)!.AsArray();
        Equal(new JsonArray(compatibility.Take(7).Select(m => m!.DeepClone()).ToArray()), original);
    }

    [Test]
    public async Task Normal_echo_cannot_replace_existing_content_even_with_more_messages()
    {
        var projected = (await Get("?allMessages=true")).GetArray("messages")!;
        projected[2]!["content"] = "stale";
        projected.Add(new JsonObject { ["role"] = "user", ["content"] = "Next", ["timestamp"] = 8 });
        await Patch(projected);
        Equal(new JsonArray(Stored().Take(7).Select(m => m!.DeepClone()).ToArray()), original);
    }

    [Test]
    public async Task Explicit_edit_delete_and_redo_hydrate_surviving_signed_messages()
    {
        var projected = (await Get("?allMessages=true")).GetArray("messages")!;
        projected.RemoveAt(5);
        await Patch(projected, true);
        Equal(Stored()[2], original[2]);
        Equal(Stored()[4], original[4]);
        Equal(Stored()[5], original[6]);
        projected = (await Get("?allMessages=true")).GetArray("messages")!;
        projected[2]!["content"] = "Edited assistant";
        await Patch(projected, true);
        Assert.That(Stored()[2]!["provider_metadata"], Is.Null);
        Assert.That(Stored()[2]!["tool_calls"]![0]!["thoughtSignature"], Is.Null);
        Equal(Stored()[4], original[4]);
    }

    [Test]
    public async Task Redo_from_partial_window_retains_canonical_prefix()
    {
        var incoming = new JsonArray(ChatClientHistory.Message(original[0]!.AsObject()), ChatClientHistory.Message(original[5]!.AsObject()));
        incoming[1]!["content"] = "Edited user";
        await Submit(incoming, true);
        Assert.That(Stored().Count, Is.EqualTo(6));
        Equal(new JsonArray(Stored().Take(5).Select(m => m!.DeepClone()).ToArray()), new JsonArray(original.Take(5).Select(m => m!.DeepClone()).ToArray()));
        Assert.That(Stored()[5]!["content"]!.GetValue<string>(), Is.EqualTo("Edited user"));
    }

    [Test]
    public async Task Followup_and_database_reload_preserve_exact_Gemini_replay_parts()
    {
        var provider = new GoogleProvider { Id = "google" };
        var parts = JsonNode.Parse("""[{"toolCall":{"toolType":"FILE_SEARCH","id":"search-1"},"thoughtSignature":"search-signature"},{"text":"Answer","thoughtSignature":"text-signature"},{"toolResponse":{"toolType":"FILE_SEARCH","id":"search-1"}},{"thoughtSignature":"standalone"}]""")!.AsArray();
        var chat = new JsonObject { ["model"] = "gemini-3.8-flash", ["messages"] = new JsonArray() };
        var response = new JsonObject { ["candidates"] = new JsonArray(new JsonObject { ["content"] = new JsonObject { ["role"] = "model", ["parts"] = parts.DeepClone() }, ["finishReason"] = "STOP" }) };
        var message = provider.ToGeminiResponse(response, chat, DateTimeOffset.UtcNow, null, true)["choices"]![0]!["message"]!.DeepClone();
        message!["timestamp"] = 2;
        id = host.Db.InsertThread(new ChatThread { User = "alice", Title = "Native Gemini", Model = "model",
            Messages = new JsonArray(original[0]!.DeepClone(), message).ToJsonString() });
        var incoming = (await Get()).GetArray("messages")!;
        incoming.Add(new JsonObject { ["role"] = "user", ["content"] = "Next", ["timestamp"] = 3 });
        await Submit(incoming);
        var reloaded = new ChatDb(new OrmLiteConnectionFactory(Path.Combine(host.DirectoryPath, "chat.sqlite"), SqliteDialect.Provider));
        chat["messages"] = new JsonArray(reloaded.GetActiveMessagesAfter(id, 0).Select(m => (JsonNode)m).ToArray());
        var payload = provider.ToGeminiRequest(chat, new JsonObject(), false);
        Equal(payload["contents"]![1]!["parts"], parts);
    }

    [Test]
    public void Projection_preserves_media_citations_tool_data_and_unrelated_vendor_extras()
    {
        var message = original[2]!.DeepClone().AsObject();
        message["content"] = JsonNode.Parse("""[{"type":"image_url","image_url":{"url":"data:image/png;base64,abc"}}]""");
        message["tool_calls"]![0]!["extra_content"]!["other"] = new JsonObject { ["value"] = 1 };
        var projected = ChatClientHistory.Message(message);
        Equal(projected["content"], message["content"]);
        Equal(projected["tool_calls"]![0]!["extra_content"], ChatJson.ParseObject("""{"other":{"value":1}}"""));
        Assert.That(message["provider_metadata"], Is.Not.Null);
        var thread = new JsonObject { ["toolHistory"] = new JsonArray(message.DeepClone()),
            ["providerResponse"] = new JsonObject { ["choices"] = new JsonArray(new JsonObject { ["message"] = message.DeepClone() }), ["tool_history"] = new JsonArray(message.DeepClone()) } };
        Assert.That(ChatClientHistory.Thread(thread).ToJsonString(), Does.Not.Contain("provider_metadata").And.Not.Contain("thoughtSignature"));
    }

    [Test]
    public async Task Other_users_cannot_read_or_reconcile_signed_history()
    {
        var before = Stored();
        var result = await host.SendAsync("GET", $"/ext/app/threads/{id}", "bob");
        Assert.That(result!.ToString(), Is.Empty);
        Assert.ThrowsAsync<HttpError>(async () => await host.SendAsync("PATCH", $"/ext/app/threads/{id}", "bob",
            new JsonObject { ["messages"] = new JsonArray() }));
        Equal(Stored(), before);
    }
}
