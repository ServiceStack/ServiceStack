#nullable enable
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Extensions.Tests;

public class ChatThreadTests
{
    [Test]
    public async Task Signal_completes_promptly_when_thread_is_updated()
    {
        var updates = new ThreadUpdates();
        var sw = Stopwatch.StartNew();

        var signal = updates.NextSignalAsync(1);
        _ = Task.Run(async () =>
        {
            await Task.Delay(150);
            updates.NotifyThreadUpdate(1);
        });

        await signal;
        sw.Stop();

        Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)),
            "the wait should complete on the update, not hang");
    }

    [Test]
    public async Task Signal_does_not_complete_without_a_notification()
    {
        var updates = new ThreadUpdates();
        var signal = updates.NextSignalAsync(2);

        var completed = await Task.WhenAny(signal, Task.Delay(200));
        Assert.That(completed, Is.Not.SameAs(signal), "no notification means the signal must stay pending");
    }

    [Test]
    public async Task A_fresh_signal_blocks_again_after_a_notification()
    {
        var updates = new ThreadUpdates();

        var first = updates.NextSignalAsync(3);
        updates.NotifyThreadUpdate(3);
        await first; // first wakeup consumed

        // the next registered signal must not already be completed
        var second = updates.NextSignalAsync(3);
        var completed = await Task.WhenAny(second, Task.Delay(200));
        Assert.That(completed, Is.Not.SameAs(second),
            "a consumed notification must not carry over to the next wait");
    }

    [Test]
    public async Task Notifications_only_wake_the_matching_thread()
    {
        var updates = new ThreadUpdates();

        var otherSignal = updates.NextSignalAsync(99);
        updates.NotifyThreadUpdate(1); // different thread

        var completed = await Task.WhenAny(otherSignal, Task.Delay(200));
        Assert.That(completed, Is.Not.SameAs(otherSignal),
            "an unrelated notification must not wake this waiter");
    }

    [Test]
    public async Task Thread_lock_serializes_read_modify_write()
    {
        var updates = new ThreadUpdates();
        var running = 0;
        var maxConcurrent = 0;

        await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            using var _lock = await updates.LockThreadAsync(7);
            var current = Interlocked.Increment(ref running);
            maxConcurrent = Math.Max(maxConcurrent, current);
            await Task.Delay(20);
            Interlocked.Decrement(ref running);
        }));

        Assert.That(maxConcurrent, Is.EqualTo(1));
    }

    [Test]
    public void Thread_dto_emits_sortable_string_timestamps_and_parsed_json()
    {
        var thread = new ChatThread
        {
            Id = 3,
            User = "test-user",
            CreatedAt = new DateTime(2026, 7, 24, 9, 8, 7, 654),
            UpdatedAt = new DateTime(2026, 7, 24, 9, 9, 1, 123),
            Title = "Test",
            Model = "llama-3.1-8b-instant",
            Messages = """[{"role":"user","content":"hi"}]""",
            Stats = """{"cost":0.5}""",
            Cost = 0.5,
        };

        var dto = thread.ToDto();

        // the UI string-compares updatedAt when long-polling, so the format must sort correctly
        var updatedAt = dto["updatedAt"]!.GetValue<string>();
        Assert.That(updatedAt, Is.EqualTo("2026-07-24 09:09:01.123000"));
        Assert.That(string.CompareOrdinal(dto["createdAt"]!.GetValue<string>(), updatedAt), Is.LessThan(0));

        // JSON columns come back as parsed nodes, not strings
        Assert.That(dto["messages"], Is.TypeOf<JsonArray>());
        Assert.That(dto["messages"]!.AsArray()[0]!["role"]!.GetValue<string>(), Is.EqualTo("user"));
        Assert.That(dto["stats"]!["cost"]!.GetValue<double>(), Is.EqualTo(0.5));
        Assert.That(dto["cost"]!.GetValue<double>(), Is.EqualTo(0.5));
        Assert.That(dto["error"], Is.Null);

        // the long-poll signature travels with the DTO and matches the entity
        Assert.That(dto["sig"]!.GetValue<string>(), Is.EqualTo(thread.Sig));
    }

    [Test]
    public void Signature_changes_only_when_tracked_content_changes()
    {
        var baseline = new ChatThread { Messages = """[{"role":"user","content":"hi"}]""" };
        var sig = baseline.Sig;

        // unrelated fields don't affect the signature
        Assert.That(new ChatThread
        {
            Messages = baseline.Messages,
            Title = "different",
            Model = "different",
            UpdatedAt = DateTime.UtcNow,
        }.Sig, Is.EqualTo(sig));

        // appending a message changes it (a streamed assistant reply)
        Assert.That(new ChatThread
        {
            Messages = """[{"role":"user","content":"hi"},{"role":"assistant","content":"hello"}]""",
        }.Sig, Is.Not.EqualTo(sig));

        // status, completion and error each change it
        Assert.That(new ChatThread { Messages = baseline.Messages, Status = "Cooking" }.Sig, Is.Not.EqualTo(sig));
        Assert.That(new ChatThread { Messages = baseline.Messages, CompletedAt = DateTime.UtcNow }.Sig, Is.Not.EqualTo(sig));
        Assert.That(new ChatThread { Messages = baseline.Messages, Error = "boom" }.Sig, Is.Not.EqualTo(sig));

        // stable for identical content
        Assert.That(new ChatThread { Messages = baseline.Messages }.Sig, Is.EqualTo(sig));
    }

    [Test]
    public void Populate_from_dto_only_writes_present_keys()
    {
        var thread = new ChatThread { Title = "Original", Model = "model-a", Cost = 1.5 };

        thread.PopulateFrom(ChatJson.ParseObject("""{"title":"Updated","messages":[{"role":"user"}]}"""));

        Assert.That(thread.Title, Is.EqualTo("Updated"));
        Assert.That(thread.Messages, Is.EqualTo("""[{"role":"user"}]"""));
        Assert.That(thread.Model, Is.EqualTo("model-a"), "absent keys must be left alone");
        Assert.That(thread.Cost, Is.EqualTo(1.5));

        // explicit nulls do clear values (used to reset error/completedAt when re-running a chat)
        thread.PopulateFrom(ChatJson.ParseObject("""{"title":null}"""));
        Assert.That(thread.Title, Is.Null);
    }

    [Test]
    public void Truncates_long_strings_when_persisting_provider_responses()
    {
        var longText = new string('x', 12000);
        var response = ChatJson.ParseObject(
            "{\"id\":\"gen-1\",\"choices\":[{\"message\":{\"content\":\"" + longText +
            "\"}}],\"nested\":{\"short\":\"ok\"}}");

        var truncated = AppExtension.TruncateLongStrings(response)!.AsObject();

        Assert.That(truncated["choices"]!.AsArray()[0]!["message"]!["content"]!.GetValue<string>(),
            Is.EqualTo("(12000)"));
        Assert.That(truncated["nested"]!["short"]!.GetValue<string>(), Is.EqualTo("ok"));
        Assert.That(truncated["id"]!.GetValue<string>(), Is.EqualTo("gen-1"));
    }

    [Test]
    public void Prompt_to_title_truncates_and_flattens()
    {
        Assert.That(AppExtension.PromptToTitle("Hello there"), Is.EqualTo("Hello there"));
        Assert.That(AppExtension.PromptToTitle("line1\nline2"), Is.EqualTo("line1 line2"));
        Assert.That(AppExtension.PromptToTitle(null), Is.Null);

        var title = AppExtension.PromptToTitle(new string('a', 100))!;
        Assert.That(title.Length, Is.EqualTo(63));
        Assert.That(title, Does.EndWith("..."));
    }

    [Test]
    public void Extracts_system_prompt_and_last_user_prompt()
    {
        var chat = ChatJson.ParseObject("""
            {"messages":[
                {"role":"system","content":"You are helpful"},
                {"role":"user","content":"first"},
                {"role":"assistant","content":"reply"},
                {"role":"user","content":[{"type":"text","text":"second"}]}
            ]}
            """);

        Assert.That(AppExtension.ChatToSystemPrompt(chat), Is.EqualTo("You are helpful"));
        Assert.That(AppExtension.LastUserPrompt(chat), Is.EqualTo("second"));
    }

    [Test]
    public async Task Text_only_GLM_payloads_flatten_attachments_and_repair_orphan_tool_results()
    {
        var provider = new OpenAiCompatibleProvider
        {
            Id = "zai-coding-plan",
            Models =
            {
                ["glm-test"] = new JsonObject
                {
                    ["family"] = "glm",
                    ["modalities"] = new JsonObject { ["input"] = new JsonArray("text") },
                },
            },
        };
        var chat = ChatJson.ParseObject("""
        {
          "model":"glm-test",
          "messages":[
            {"role":"user","content":[{"type":"text","text":"inspect"},{"type":"image_url","image_url":{"url":"data:image/png;base64,AA=="}}]},
            {"role":"tool","tool_call_id":"orphan","content":"legacy output"},
            {"role":"assistant","content":"ready"}
          ]
        }
        """);

        var outbound = await provider.ProcessChatAsync(chat, provider.Id);
        var messages = outbound.GetArray("messages")!;

        Assert.That(messages.OfType<JsonObject>().All(x => x["content"] is JsonValue), Is.True);
        Assert.That(messages.ToJsonString(), Does.Contain("image attachment omitted"));
        Assert.That(messages.ToJsonString(), Does.Contain("Tool result (orphan)"));
        Assert.That(messages.OfType<JsonObject>().Any(x => x.GetString("role") == "tool"), Is.False);
    }

    // ── The in-flight streaming message lives outside the durable conversation ──

    [Test]
    public void Thread_dto_presents_the_in_flight_message_merged_into_messages()
    {
        var thread = new ChatThread
        {
            Messages = """[{"role":"user","content":"hi"}]""",
            StreamingMessage = """{"role":"assistant","content":"half a re"}""",
        };

        var messages = thread.ToDto()["messages"]!.AsArray();

        // clients read one list, so the in-flight message is merged on the way out and flagged
        Assert.That(messages.Count, Is.EqualTo(2));
        Assert.That(messages[1]!["content"]!.GetValue<string>(), Is.EqualTo("half a re"));
        Assert.That(messages[1]![ChatDtos.StreamingKey]!.GetValue<bool>(), Is.True);

        // each checkpoint must wake the UI's long-poll
        var progressed = new ChatThread
        {
            Messages = thread.Messages,
            StreamingMessage = """{"role":"assistant","content":"half a reply"}""",
        };
        Assert.That(progressed.Sig, Is.Not.EqualTo(thread.Sig));
        Assert.That(new ChatThread { Messages = thread.Messages }.Sig, Is.Not.EqualTo(thread.Sig));
    }

    [Test]
    public void Thread_dto_does_not_duplicate_a_committed_streaming_checkpoint()
    {
        var thread = new ChatThread
        {
            Messages = """[{"role":"assistant","content":"Submitting","timestamp":42,"tool_calls":[{"id":"call_1"}]}]""",
            StreamingMessage = """{"role":"assistant","content":"Submitting","timestamp":42,"model":"test","tool_calls":[{"id":"call_1"}]}""",
        };

        var messages = thread.ToDto().GetArray("messages")!;

        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0]![ChatDtos.StreamingKey], Is.Null);
    }

    [Test]
    public void Echoed_in_flight_messages_are_dropped_before_persisting()
    {
        var messages = ChatJson.ParseObject("""
            {"messages":[
                {"role":"user","content":"hi"},
                {"role":"assistant","content":"half a reply","streaming":true}
            ]}
            """)["messages"]!.AsArray();

        var kept = messages.WithoutStreamingMessages();

        Assert.That(kept.Count, Is.EqualTo(1));
        Assert.That(kept[0]!["role"]!.GetValue<string>(), Is.EqualTo("user"));
    }

    // ── `messages` may only grow unless the caller opts into truncation ──

    static (DbThreadApi Api, ChatDb Db, long ThreadId) CreateThreadApi(JsonArray messages)
    {
        var dbFactory = new OrmLiteConnectionFactory(
            $"DataSource=file:threads{Guid.NewGuid():n}?mode=memory&cache=shared", SqliteDialect.Provider);
        var db = new ChatDb(dbFactory);
        db.InitSchema();
        var now = DateTime.Now;
        var id = db.InsertThread(new ChatThread
        {
            User = ChatDb.DefaultUser,
            CreatedAt = now,
            UpdatedAt = now,
            Title = "t",
            Messages = ChatDtos.ToJson(messages),
        });
        return (new DbThreadApi(db, new ThreadUpdates(), NullLogger.Instance), db, id);
    }

    /// <summary>A thread with two completed turns plus the user prompt being answered</summary>
    static JsonArray History() => ChatJson.ParseObject("""
        {"messages":[
            {"role":"user","content":"turn 1 question","timestamp":1},
            {"role":"assistant","content":"turn 1 answer","timestamp":2},
            {"role":"user","content":"turn 2 question","timestamp":3},
            {"role":"assistant","content":"turn 2 answer","timestamp":4},
            {"role":"user","content":"turn 3 question","timestamp":5}
        ]}
        """)["messages"]!.AsArray();

    static JsonArray StoredMessages(ChatDb db, long id) =>
        db.GetThread(id, ChatDb.DefaultUser)!.ToDto()["messages"]!.AsArray();

    [Test]
    public void Durable_schema_backfills_legacy_history_and_pages_without_duplicates()
    {
        var (_, db, id) = CreateThreadApi(History());

        db.EnsureChatMessages(id);
        db.SyncChatMessages(id, History());

        var bounds = db.GetChatMessageBounds(id);
        Assert.That(bounds.Count, Is.EqualTo(5));
        Assert.That(bounds.First, Is.EqualTo(1));
        Assert.That(bounds.Last, Is.EqualTo(5));
        var page = db.GetChatMessagePage(id, after: 2, take: 2);
        Assert.That(page.Select(x => x.GetLong("_sequence")), Is.EqualTo(new long?[] { 3, 4 }));
        Assert.That(page.Select(x => x.GetString("content")),
            Is.EqualTo(new[] { "turn 2 question", "turn 2 answer" }));
        var sidebar = db.QueryThreads(new JsonObject { ["take"] = 30 }, ChatDb.DefaultUser).Single();
        Assert.That(sidebar.Messages, Is.Null, "sidebar queries must not fetch the legacy history blob");
        Assert.That(db.GetThread(id, ChatDb.DefaultUser, includeMessages: false)!.Messages, Is.Null,
            "window/update reads must not fetch the legacy history blob");
    }

    [Test]
    public void Reconciliation_updates_an_existing_canonical_identity_without_duplicating_it()
    {
        var (_, db, id) = CreateThreadApi(History());
        db.EnsureChatMessages(id);
        var changed = ChatJson.ParseObject(
            """{"role":"user","content":"turn 3 question","timestamp":5,"usage":{"tokens":42}}""");

        db.SyncChatMessages(id, new JsonArray(changed));

        var rows = db.GetActiveMessagesAfter(id, 0);
        Assert.That(rows, Has.Count.EqualTo(5));
        Assert.That(rows[^1].GetObject("usage").GetLong("tokens"), Is.EqualTo(42));
    }

    [Test]
    public void Payload_sizing_and_token_estimation_do_not_take_ownership_of_message_nodes()
    {
        var rows = new System.Collections.Generic.List<JsonObject>
        {
            ChatJson.ParseObject("""{"role":"user","content":"hello","timestamp":1}"""),
            ChatJson.ParseObject("""{"role":"assistant","content":"world","timestamp":2}"""),
        };
        var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
        var limit = typeof(AppExtension).GetMethod("LimitMessagePayload", flags)!;
        var tokens = typeof(AppExtension).GetMethod("Tokens", flags)!;

        var selected = (System.Collections.Generic.List<JsonObject>)limit.Invoke(
            null, new object[] { rows, 1024, false })!;
        _ = tokens.Invoke(null, new object[] { rows });

        Assert.That(rows.All(x => x.Parent == null), Is.True,
            "measurement must use clones because JsonNode permits only one parent");
        Assert.DoesNotThrow(() => _ = new JsonArray(selected.Select(x => (JsonNode)x).ToArray()));
    }

    [Test]
    public void Durable_pages_do_not_split_tool_call_groups()
    {
        var history = ChatJson.ParseObject("""
        {"messages":[
          {"role":"user","content":"inspect","timestamp":1},
          {"role":"assistant","content":"checking","timestamp":2,"tool_calls":[
            {"id":"a","type":"function","function":{"name":"read","arguments":"{}"}},
            {"id":"b","type":"function","function":{"name":"read","arguments":"{}"}}]},
          {"role":"tool","tool_call_id":"a","content":"one","timestamp":3},
          {"role":"tool","tool_call_id":"b","content":"two","timestamp":4},
          {"role":"assistant","content":"done","timestamp":5}
        ]}
        """).GetArray("messages")!;
        var (_, db, id) = CreateThreadApi(history);

        // Both directions deliberately request only the first tool result. The persistence boundary
        // expands it to the originating assistant plus every result in the atomic tool exchange.
        var forwards = db.GetChatMessagePage(id, after: 2, take: 1);
        var backwards = db.GetChatMessagePage(id, before: 4, take: 1);

        Assert.That(forwards.Select(x => x.GetLong("_sequence")), Is.EqualTo(new long?[] { 2, 3, 4 }));
        Assert.That(backwards.Select(x => x.GetLong("_sequence")), Is.EqualTo(new long?[] { 2, 3, 4 }));
    }

    [Test]
    public void Agent_runs_are_claimed_once_and_interrupted_runs_requeue()
    {
        var (_, db, id) = CreateThreadApi(History());
        var runId = db.CreateAgentRun(id, ChatDb.DefaultUser, "test", 42);

        var claimed = db.ClaimAgentRuns("worker-a", 2, 300);
        Assert.That(claimed.Select(x => x.Id), Is.EqualTo(new[] { runId }));
        Assert.That(db.ClaimAgentRuns("worker-b", 2, 300), Is.Empty);
        Assert.That(db.GetAgentRun(runId, ChatDb.DefaultUser)!.Status, Is.EqualTo(AgentRunStatus.Running));

        Assert.That(db.RequeueInterruptedAgentRuns(), Is.EqualTo(1));
        Assert.That(db.GetAgentRun(runId, ChatDb.DefaultUser)!.Status, Is.EqualTo(AgentRunStatus.Queued));
    }

    [Test]
    public async Task Appending_messages_is_allowed()
    {
        var (api, db, id) = CreateThreadApi(History());
        var grown = StoredMessages(db, id).DeepClone().AsArray();
        grown.Add(ChatJson.ParseObject("""{"role":"assistant","content":"answer","timestamp":6}"""));

        await api.UpdateThreadInternalAsync(id, new JsonObject { ["messages"] = grown }, ChatDb.DefaultUser);

        Assert.That(StoredMessages(db, id).Count, Is.EqualTo(6));
    }

    [Test]
    public async Task A_shrinking_write_cannot_erase_history_but_keeps_its_new_messages()
    {
        // a single-turn request must not replace a whole conversation
        var (api, db, id) = CreateThreadApi(History());
        var update = ChatJson.ParseObject("""{"messages":[{"role":"user","content":"new ask","timestamp":99}]}""");

        await api.UpdateThreadInternalAsync(id, update, ChatDb.DefaultUser);

        var stored = StoredMessages(db, id);
        Assert.That(stored.Count, Is.EqualTo(6));
        Assert.That(stored.Take(5).Select(x => x!["content"]!.GetValue<string>()),
            Is.EqualTo(History().Select(x => x!["content"]!.GetValue<string>())));
        Assert.That(stored[^1]!["content"]!.GetValue<string>(), Is.EqualTo("new ask"));
    }

    [Test]
    public async Task Truncate_opts_in_to_rewriting_history()
    {
        // edit/redo/delete/compact legitimately shrink history
        var (api, db, id) = CreateThreadApi(History());
        var kept = new JsonArray(History().Take(3).Select(x => x!.DeepClone()).ToArray());

        await api.UpdateThreadInternalAsync(id, new JsonObject
        {
            ["messages"] = kept,
            [DbThreadApi.TruncateKey] = true,
        }, ChatDb.DefaultUser);

        Assert.That(StoredMessages(db, id).Count, Is.EqualTo(3));
    }

    [Test]
    public async Task An_echoed_in_flight_message_cannot_be_persisted_or_mask_a_shrink()
    {
        var (api, db, id) = CreateThreadApi(History());
        var echoed = new JsonArray(History().Take(3).Select(x => x!.DeepClone()).ToArray());
        echoed.Add(ChatJson.ParseObject("""{"role":"assistant","content":"half","streaming":true}"""));

        await api.UpdateThreadInternalAsync(id, new JsonObject { ["messages"] = echoed }, ChatDb.DefaultUser);

        // filtering the partial happens before the guard, so it can't buy a shrink either
        var stored = StoredMessages(db, id);
        Assert.That(stored.Count, Is.EqualTo(5));
        Assert.That(stored.Any(x => x![ChatDtos.StreamingKey] != null), Is.False);
    }

    [Test]
    public async Task A_stream_checkpoint_leaves_messages_alone()
    {
        var (api, db, id) = CreateThreadApi(History());

        await api.CheckpointStreamAsync(id, ChatJson.ParseObject(
            """{"role":"assistant","content":"partial"}"""), ChatDb.DefaultUser);

        var row = db.GetThread(id, ChatDb.DefaultUser)!;
        Assert.That(ChatDtos.ParseJson(row.Messages)!.AsArray().Count, Is.EqualTo(5));
        Assert.That(ChatDtos.ParseJson(row.StreamingMessage)!["content"]!.GetValue<string>(), Is.EqualTo("partial"));
        // and it's presented to clients as the trailing in-flight message
        Assert.That(StoredMessages(db, id).Count, Is.EqualTo(6));
    }

    [Test]
    public async Task Persists_and_cancels_unsafe_tool_approvals_with_the_thread()
    {
        var dbFactory = new OrmLiteConnectionFactory(
            $"DataSource=file:approvals{Guid.NewGuid():n}?mode=memory&cache=shared", SqliteDialect.Provider);
        var db = new ChatDb(dbFactory);
        db.InitSchema();
        var updates = new ThreadUpdates();
        var threads = new DbThreadApi(db, updates, NullLogger.Instance);
        var feature = new ChatFeature
        {
            ChatDb = db,
            ThreadApi = threads,
            AutoInitSchema = true,
        };
        var coordinator = new ApiToolApprovalCoordinator(new ApiToolsExtension(),
            new ExtensionContext(feature, "api_tools"));
        coordinator.Install();
        var now = DateTime.Now;
        var toolCallMessage = ChatJson.ParseObject("""
            {"role":"assistant","content":"Submitting the call","tool_calls":[{
              "id":"call_1","type":"function","function":{"name":"api_call","arguments":"{\"name\":\"UpdateCustomer\",\"args\":{\"id\":1}}"}
            }]}
            """);
        var threadId = db.InsertThread(new ChatThread
        {
            User = ChatDb.DefaultUser,
            CreatedAt = now,
            UpdatedAt = now,
            Messages = new JsonArray(toolCallMessage.Clone()).ToJsonString(ChatJson.Options),
            StreamingMessage = toolCallMessage.ToJsonString(ChatJson.Options),
        });

        await coordinator.PauseAsync([new PendingChatToolCall
        {
            ToolCallId = "call_1",
            ToolName = "api_call",
            Sequence = 0,
            Arguments = ChatJson.ParseObject("""{"name":"UpdateCustomer","args":{"id":1}}"""),
            Approval = new ChatToolApprovalRequest
            {
                Title = "UpdateCustomer",
                Description = "Updates a customer",
                Safety = ToolSafety.Write,
                Schema = ChatJson.ParseObject("""{"type":"object","properties":{"id":{"type":"integer"}}}"""),
                Arguments = ChatJson.ParseObject("""{"id":1}"""),
                Metadata = new JsonObject { ["apiName"] = "UpdateCustomer" },
            },
        }], new ChatContext { ThreadId = threadId, User = ChatDb.DefaultUser });

        Assert.That(coordinator.HasPending(threadId, ChatDb.DefaultUser), Is.True);
        var pausedThread = db.GetThread(threadId, ChatDb.DefaultUser)!;
        Assert.That(pausedThread.Status, Is.EqualTo("Approval required"));
        Assert.That(pausedThread.StreamingMessage, Is.Null);
        Assert.That(pausedThread.ToDto().GetArray("messages"), Has.Count.EqualTo(1),
            "the committed api_call must not also be merged from its streaming checkpoint");
        using (var conn = db.OpenDb())
        {
            var approval = conn.Select<ChatToolApproval>().Single();
            Assert.That(approval.ToolCallId, Is.EqualTo("call_1"));
            Assert.That(approval.ApiName, Is.EqualTo("UpdateCustomer"));
            Assert.That(approval.Status, Is.EqualTo(ApiToolApprovalStatus.Pending));
            Assert.That(ChatJson.ParseObject(approval.ProposedArgs).GetInt("id"), Is.EqualTo(1));
        }

        await coordinator.CancelThreadAsync(threadId, ChatDb.DefaultUser);

        Assert.That(coordinator.HasPending(threadId, ChatDb.DefaultUser), Is.False);
        using (var conn = db.OpenDb())
            Assert.That(conn.Select<ChatToolApproval>().Single().Status, Is.EqualTo(ApiToolApprovalStatus.Canceled));
    }
}
