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
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

/// <summary>
/// Verifies SSE delta accumulation reassembles a standard non-streaming OpenAI response,
/// matching llms-py's handle_stream_response.
/// </summary>
public class AiChatStreamTests
{
    static OpenAiCompatibleProvider CreateProvider()
    {
        var provider = new OpenAiCompatibleProvider();
        provider.Populate(ChatJson.ParseObject(
            """
            {
                "id": "test",
                "api": "https://example.org/v1",
                "api_key": "sk-test",
                "models": { "test-model": { "id": "test-model", "name": "Test Model" } }
            }
            """));
        return provider;
    }

    static HttpResponseMessage SseResponse(string sse) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(sse, Encoding.UTF8, "text/event-stream")
    };

    static JsonObject Chat() => ChatJson.ParseObject(
        """{"model":"test-model","messages":[{"role":"user","content":"hi"}]}""");

    [Test]
    public async Task Accumulates_content_deltas()
    {
        const string sse = """
            data: {"id":"gen-1","created":1700000000,"model":"test-model","choices":[{"index":0,"delta":{"role":"assistant","content":"Hello"}}]}

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":", "}}]}

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":"world!"},"finish_reason":"stop"}]}

            data: {"id":"gen-1","usage":{"prompt_tokens":10,"completion_tokens":5,"total_tokens":15}}

            data: [DONE]
            """;

        var res = await CreateProvider().HandleStreamResponseAsync(
            SseResponse(sse), Chat(), DateTimeOffset.UtcNow, new ChatContext());

        Assert.That(res, Is.Not.Null);
        Assert.That(res!["id"]!.GetValue<string>(), Is.EqualTo("gen-1"));
        Assert.That(res["object"]!.GetValue<string>(), Is.EqualTo("chat.completion"));
        Assert.That(res["model"]!.GetValue<string>(), Is.EqualTo("test-model"));

        var choice = res["choices"]!.AsArray()[0]!.AsObject();
        Assert.That(choice["finish_reason"]!.GetValue<string>(), Is.EqualTo("stop"));
        Assert.That(choice["message"]!["role"]!.GetValue<string>(), Is.EqualTo("assistant"));
        Assert.That(choice["message"]!["content"]!.GetValue<string>(), Is.EqualTo("Hello, world!"));

        Assert.That(res["usage"]!["prompt_tokens"]!.GetValue<int>(), Is.EqualTo(10));
        Assert.That(res["usage"]!["completion_tokens"]!.GetValue<int>(), Is.EqualTo(5));
    }

    [Test]
    public async Task Accumulates_reasoning_deltas()
    {
        const string sse = """
            data: {"id":"gen-1","choices":[{"index":0,"delta":{"reasoning_content":"Let me "}}]}

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"reasoning_content":"think."}}]}

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":"42"},"finish_reason":"stop"}]}

            data: [DONE]
            """;

        var res = await CreateProvider().HandleStreamResponseAsync(
            SseResponse(sse), Chat(), DateTimeOffset.UtcNow, new ChatContext());

        var message = res!["choices"]!.AsArray()[0]!["message"]!.AsObject();
        Assert.That(message["reasoning_content"]!.GetValue<string>(), Is.EqualTo("Let me think."));
        Assert.That(message["content"]!.GetValue<string>(), Is.EqualTo("42"));
    }

    [Test]
    public async Task Merges_indexed_tool_call_fragments()
    {
        const string sse = """
            data: {"id":"gen-1","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"id":"call_1","type":"function","function":{"name":"get_weather","arguments":""}}]}}]}

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":"{\"city\":"}}]}}]}

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":"\"Perth\"}"}}]}}]}

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"tool_calls":[{"index":1,"id":"call_2","type":"function","function":{"name":"calc","arguments":"{}"}}]},"finish_reason":"tool_calls"}]}

            data: [DONE]
            """;

        var res = await CreateProvider().HandleStreamResponseAsync(
            SseResponse(sse), Chat(), DateTimeOffset.UtcNow, new ChatContext());

        var choice = res!["choices"]!.AsArray()[0]!.AsObject();
        Assert.That(choice["finish_reason"]!.GetValue<string>(), Is.EqualTo("tool_calls"));

        var toolCalls = choice["message"]!["tool_calls"]!.AsArray();
        Assert.That(toolCalls.Count, Is.EqualTo(2));

        var first = toolCalls[0]!.AsObject();
        Assert.That(first["id"]!.GetValue<string>(), Is.EqualTo("call_1"));
        Assert.That(first["type"]!.GetValue<string>(), Is.EqualTo("function"));
        Assert.That(first["function"]!["name"]!.GetValue<string>(), Is.EqualTo("get_weather"));
        Assert.That(first["function"]!["arguments"]!.GetValue<string>(), Is.EqualTo("""{"city":"Perth"}"""));

        // tool calls are ordered by their delta index
        Assert.That(toolCalls[1]!["id"]!.GetValue<string>(), Is.EqualTo("call_2"));
    }

    [Test]
    public async Task Ignores_comments_and_blank_lines_and_captures_cost()
    {
        const string sse = """
            : keep-alive comment

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":"ok"},"finish_reason":"stop"}]}

            data: {"id":"gen-1","usage":{"prompt_tokens":3,"completion_tokens":1},"cost":0.00042}

            data: [DONE]
            """;

        var res = await CreateProvider().HandleStreamResponseAsync(
            SseResponse(sse), Chat(), DateTimeOffset.UtcNow, new ChatContext());

        Assert.That(res!["choices"]!.AsArray()[0]!["message"]!["content"]!.GetValue<string>(), Is.EqualTo("ok"));
        Assert.That(res["cost"]!.GetValue<double>(), Is.EqualTo(0.00042));
    }

    [Test]
    public async Task Defaults_usage_and_finish_reason_when_absent()
    {
        const string sse = """
            data: {"choices":[{"index":0,"delta":{"content":"hi"}}]}

            data: [DONE]
            """;

        var res = await CreateProvider().HandleStreamResponseAsync(
            SseResponse(sse), Chat(), DateTimeOffset.UtcNow, new ChatContext());

        Assert.That(res!["choices"]!.AsArray()[0]!["finish_reason"]!.GetValue<string>(), Is.EqualTo("stop"));
        Assert.That(res["usage"]!["total_tokens"]!.GetValue<int>(), Is.EqualTo(0));
        // falls back to the requested model when the stream omits it
        Assert.That(res["model"]!.GetValue<string>(), Is.EqualTo("test-model"));
    }

    [Test]
    public void Surfaces_provider_error_messages()
    {
        var errorRes = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("""{"error":{"message":"Rate limit exceeded"}}""")
        };

        var ex = Assert.ThrowsAsync<Exception>(async () =>
            await CreateProvider().HandleStreamResponseAsync(
                errorRes, Chat(), DateTimeOffset.UtcNow, new ChatContext()));
        Assert.That(ex!.Message, Is.EqualTo("Rate limit exceeded"));
    }

    [Test]
    public async Task Checkpoints_the_in_flight_message_while_it_streams()
    {
        const string sse = """
            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":"Hello"}}]}

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":" world"},"finish_reason":"stop"}]}

            data: [DONE]
            """;

        var (provider, threads) = CreateStreamingProvider();

        var res = await provider.HandleStreamResponseAsync(SseResponse(sse), Chat(), DateTimeOffset.UtcNow,
            new ChatContext { ThreadId = 42, User = "test-user" });

        Assert.That(res, Is.Not.Null);
        Assert.That(threads.Checkpoints, Is.Not.Empty);

        // the final checkpoint carries the full accumulated assistant message
        var (threadId, message, user) = threads.Checkpoints[^1];
        Assert.That(threadId, Is.EqualTo(42));
        Assert.That(user, Is.EqualTo("test-user"));
        Assert.That(message["role"]!.GetValue<string>(), Is.EqualTo("assistant"));
        Assert.That(message["content"]!.GetValue<string>(), Is.EqualTo("Hello world"));
        Assert.That(message["model"]!.GetValue<string>(), Is.EqualTo("test-model"));
        // the streamed partial must carry a timestamp so the UI doesn't render "Invalid Date"
        Assert.That(message["timestamp"], Is.Not.Null);
        Assert.That(message["timestamp"]!.GetValue<long>(), Is.GreaterThan(0));
    }

    // ── Streaming resilience: however a stream fails, the durable conversation is untouched ──

    static (OpenAiCompatibleProvider Provider, CapturingThreadApi Threads) CreateStreamingProvider(
        TimeSpan? checkpointInterval = null)
    {
        var threads = new CapturingThreadApi();
        var provider = CreateProvider();
        provider.Feature = new ChatFeature { ThreadApi = threads };
        if (checkpointInterval is { } interval)
            provider.Feature.Limits.StreamCheckpointInterval = interval;
        return (provider, threads);
    }

    [Test]
    public async Task Streaming_never_writes_the_durable_messages()
    {
        // the whole point: the streaming path structurally cannot reach the conversation
        const string sse = """
            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":"Hello"}}]}

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":" world"},"finish_reason":"stop"}]}

            data: [DONE]
            """;

        var (provider, threads) = CreateStreamingProvider();

        await provider.HandleStreamResponseAsync(SseResponse(sse), Chat(), DateTimeOffset.UtcNow,
            new ChatContext { ThreadId = 42 });

        Assert.That(threads.Updates, Is.Empty, "streaming must not write messages");
        Assert.That(threads.Checkpoints, Is.Not.Empty);
    }

    [Test]
    public void Surfaces_an_error_chunk_reported_mid_stream()
    {
        // an upstream failure reported as an SSE chunk must not look like a clean stop
        const string sse = """
            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":"partial answer"}}]}

            data: {"error":{"code":429,"message":"kimi-k3 is temporarily rate-limited upstream"}}

            data: [DONE]
            """;

        var (provider, threads) = CreateStreamingProvider();

        var ex = Assert.ThrowsAsync<Exception>(async () =>
            await provider.HandleStreamResponseAsync(
                SseResponse(sse), Chat(), DateTimeOffset.UtcNow, new ChatContext { ThreadId = 42 }));
        Assert.That(ex!.Message, Is.EqualTo("kimi-k3 is temporarily rate-limited upstream"));

        // what streamed before the failure is kept, as a checkpoint
        Assert.That(threads.Updates, Is.Empty);
        Assert.That(threads.Checkpoints[^1].Message["content"]!.GetValue<string>(), Is.EqualTo("partial answer"));
    }

    [Test]
    public void Surfaces_an_error_reported_on_a_choice()
    {
        const string sse = """
            data: {"id":"gen-1","choices":[{"index":0,"error":{"message":"upstream provider error"}}]}

            data: [DONE]
            """;

        var ex = Assert.ThrowsAsync<Exception>(async () =>
            await CreateProvider().HandleStreamResponseAsync(
                SseResponse(sse), Chat(), DateTimeOffset.UtcNow, new ChatContext()));
        Assert.That(ex!.Message, Is.EqualTo("upstream provider error"));
    }

    [Test]
    public void A_dying_stream_flushes_what_it_produced_and_writes_nothing_else()
    {
        const string sse = """
            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":"chunk-A "}}]}

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":"chunk-B "}}]}
            """;

        // a checkpoint interval longer than the test keeps the second chunk in memory,
        // so only the flush on failure can persist it
        var (provider, threads) = CreateStreamingProvider(TimeSpan.FromMinutes(1));

        Assert.ThrowsAsync<IOException>(async () =>
            await provider.HandleStreamResponseAsync(DyingSseResponse(sse), Chat(), DateTimeOffset.UtcNow,
                new ChatContext { ThreadId = 42 }));

        Assert.That(threads.Updates, Is.Empty, "a dying stream must not write messages");
        Assert.That(threads.Checkpoints[^1].Message["content"]!.GetValue<string>(), Is.EqualTo("chunk-A chunk-B "));
    }

    [Test]
    public void A_stream_that_dies_before_any_content_writes_nothing()
    {
        const string sse = """
            data: {"id":"gen-1","choices":[{"index":0,"delta":{"role":"assistant"}}]}
            """;

        var (provider, threads) = CreateStreamingProvider();

        Assert.ThrowsAsync<IOException>(async () =>
            await provider.HandleStreamResponseAsync(DyingSseResponse(sse), Chat(), DateTimeOffset.UtcNow,
                new ChatContext { ThreadId = 42 }));

        Assert.That(threads.Updates, Is.Empty);
        Assert.That(threads.Checkpoints, Is.Empty, "an empty response must not be persisted");
    }

    [Test]
    public async Task Checkpoints_are_throttled_to_the_interval_with_a_final_write()
    {
        const string sse = """
            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":"a"}}]}

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":"b"}}]}

            data: {"id":"gen-1","choices":[{"index":0,"delta":{"content":"c"},"finish_reason":"stop"}]}

            data: [DONE]
            """;

        var (provider, threads) = CreateStreamingProvider(TimeSpan.FromMinutes(1));

        await provider.HandleStreamResponseAsync(SseResponse(sse), Chat(), DateTimeOffset.UtcNow,
            new ChatContext { ThreadId = 42 });

        // the first chunk lands immediately so output appears at once, the rest are throttled
        // until the final write, which bypasses the interval
        Assert.That(threads.Checkpoints.Select(x => x.Message["content"]!.GetValue<string>()),
            Is.EqualTo(new[] { "a", "abc" }));
    }

    [Test]
    public async Task Sse_reads_time_out_between_chunks_without_a_total_timeout()
    {
        // a stalled provider must fail its read rather than hang for the length of the response
        var res = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StallingStream()) };
        await using var sse = await SseReader.CreateAsync(res, new ChatContext(),
            TimeSpan.FromMilliseconds(100), "test");

        Assert.ThrowsAsync<TimeoutException>(async () => await sse.ReadLineAsync());
    }

    /// <summary>Stands in for the real ThreadApi, recording which column each write touched</summary>
    class CapturingThreadApi : IThreadApi
    {
        public List<(long ThreadId, JsonObject Thread, string? User)> Updates { get; } = [];
        public List<(long ThreadId, JsonObject Message, string? User)> Checkpoints { get; } = [];

        public JsonObject? GetThread(long threadId, string? user) => null;
        public JsonObject? GetRequest(string requestId, string? user) => null;

        public Task UpdateThreadAsync(long threadId, JsonObject thread, string? user = null)
        {
            Updates.Add((threadId, thread.DeepClone().AsObject(), user));
            return Task.CompletedTask;
        }

        public Task CheckpointStreamAsync(long threadId, JsonObject message, string? user = null)
        {
            Checkpoints.Add((threadId, message.DeepClone().AsObject(), user));
            return Task.CompletedTask;
        }
    }

    /// <summary>An SSE response whose connection drops after the last line it has</summary>
    static HttpResponseMessage DyingSseResponse(string sse) => new(HttpStatusCode.OK)
    {
        Content = new StreamContent(new DyingStream(Encoding.UTF8.GetBytes(sse + "\n")))
    };

    /// <summary>A response body that yields its content, then fails like a dropped connection</summary>
    class DyingStream(byte[] content) : Stream
    {
        int pos;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (pos >= content.Length)
                throw new IOException("The response ended prematurely");
            var read = Math.Min(count, content.Length - pos);
            Array.Copy(content, pos, buffer, offset, read);
            pos += read;
            return read;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => pos; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>A response body that never produces a chunk</summary>
    class StallingStream : Stream
    {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            await Task.Delay(Timeout.Infinite, token);
            return 0;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
