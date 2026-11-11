using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Redis;

public partial class RedisNativeClient : IRedisStreamCommands, IRedisStreamCommandsAsync
{
    public string XAdd(string stream, RedisStreamField[] fields, string id = "*", long? maxLength = null,
        bool approximate = false)
        => StreamStringCommand(AddArgs(stream, fields, id, maxLength, approximate));

    public ValueTask<string> XAddAsync(string stream, RedisStreamField[] fields, string id = "*",
        long? maxLength = null, bool approximate = false, CancellationToken token = default)
        => StreamStringCommandAsync(AddArgs(stream, fields, id, maxLength, approximate), token);

    public RedisStreamEntry[] XRange(string stream, string start = "-", string end = "+", int? count = null,
        bool reverse = false)
        => StreamEntries(StreamCommand(RangeArgs(stream, start, end, count, reverse)));

    public async ValueTask<RedisStreamEntry[]> XRangeAsync(string stream, string start = "-", string end = "+",
        int? count = null, bool reverse = false, CancellationToken token = default)
        => StreamEntries(await StreamCommandAsync(RangeArgs(stream, start, end, count, reverse), token)
            .ConfigureAwait(false));

    public long XLen(string stream)
        => StreamLongCommand(SimpleStreamArgs("XLEN", stream));

    public ValueTask<long> XLenAsync(string stream, CancellationToken token = default)
        => StreamLongCommandAsync(SimpleStreamArgs("XLEN", stream), token);

    public RedisStreamReadResult[] XRead(RedisStreamPosition[] streams, int? count = null,
        long? blockMilliseconds = null)
        => StreamReads(StreamCommand(ReadArgs(streams, count, blockMilliseconds, null, null, false)));

    public async ValueTask<RedisStreamReadResult[]> XReadAsync(RedisStreamPosition[] streams, int? count = null,
        long? blockMilliseconds = null, CancellationToken token = default)
        => StreamReads(await StreamCommandAsync(ReadArgs(streams, count, blockMilliseconds, null, null, false), token)
            .ConfigureAwait(false));

    public bool XGroupCreate(string stream, string group, string id = "0-0", bool makeStream = false)
        => StreamOk(StreamCommand(GroupCreateArgs(stream, group, id, makeStream)));

    public async ValueTask<bool> XGroupCreateAsync(string stream, string group, string id = "0-0",
        bool makeStream = false, CancellationToken token = default)
        => StreamOk(await StreamCommandAsync(GroupCreateArgs(stream, group, id, makeStream), token)
            .ConfigureAwait(false));

    public long XGroupDestroy(string stream, string group)
        => StreamLong(StreamCommand(SimpleStreamArgs("XGROUP", "DESTROY", stream, group)));

    public async ValueTask<long> XGroupDestroyAsync(string stream, string group, CancellationToken token = default)
        => StreamLong(await StreamCommandAsync(SimpleStreamArgs("XGROUP", "DESTROY", stream, group), token)
            .ConfigureAwait(false));

    public RedisStreamReadResult[] XReadGroup(string group, string consumer, RedisStreamPosition[] streams,
        int? count = null, long? blockMilliseconds = null, bool noAck = false)
        => StreamReads(StreamCommand(GroupReadArgs(streams, count, blockMilliseconds, group, consumer, noAck)));

    public async ValueTask<RedisStreamReadResult[]> XReadGroupAsync(string group, string consumer,
        RedisStreamPosition[] streams, int? count = null, long? blockMilliseconds = null, bool noAck = false,
        CancellationToken token = default)
        => StreamReads(
            await StreamCommandAsync(GroupReadArgs(streams, count, blockMilliseconds, group, consumer, noAck), token)
                .ConfigureAwait(false));

    public long XAck(string stream, string group, string[] ids)
        => StreamLongCommand(IdsArgs("XACK", stream, group, ids));

    public ValueTask<long> XAckAsync(string stream, string group, string[] ids, CancellationToken token = default)
        => StreamLongCommandAsync(IdsArgs("XACK", stream, group, ids), token);

    public RedisStreamPendingSummary XPending(string stream, string group)
        => StreamPendingSummary(StreamCommand(SimpleStreamArgs("XPENDING", stream, group)));

    public async ValueTask<RedisStreamPendingSummary> XPendingAsync(string stream, string group,
        CancellationToken token = default)
        => StreamPendingSummary(await StreamCommandAsync(SimpleStreamArgs("XPENDING", stream, group), token)
            .ConfigureAwait(false));

    public RedisStreamPendingEntry[] XPendingEntries(string stream, string group, string start = "-", string end = "+",
        int count = 100, string consumer = null)
        => StreamPendingEntries(StreamCommand(PendingArgs(stream, group, start, end, count, consumer)));

    public async ValueTask<RedisStreamPendingEntry[]> XPendingEntriesAsync(string stream, string group,
        string start = "-", string end = "+", int count = 100, string consumer = null,
        CancellationToken token = default)
        => StreamPendingEntries(await StreamCommandAsync(PendingArgs(stream, group, start, end, count, consumer), token)
            .ConfigureAwait(false));

    public RedisStreamEntry[] XClaim(string stream, string group, string consumer, long minIdleMilliseconds,
        string[] ids)
        => StreamEntries(StreamCommand(ClaimArgs(stream, group, consumer, minIdleMilliseconds, ids)));

    public async ValueTask<RedisStreamEntry[]> XClaimAsync(string stream, string group, string consumer,
        long minIdleMilliseconds, string[] ids, CancellationToken token = default)
        => StreamEntries(await StreamCommandAsync(ClaimArgs(stream, group, consumer, minIdleMilliseconds, ids), token)
            .ConfigureAwait(false));

    public RedisStreamAutoClaimResult XAutoClaim(string stream, string group, string consumer, long minIdleMilliseconds,
        string start = "0-0", int? count = null)
        => StreamAutoClaim(StreamCommand(AutoClaimArgs(stream, group, consumer, minIdleMilliseconds, start, count)));

    public async ValueTask<RedisStreamAutoClaimResult> XAutoClaimAsync(string stream, string group, string consumer,
        long minIdleMilliseconds, string start = "0-0", int? count = null, CancellationToken token = default)
        => StreamAutoClaim(
            await StreamCommandAsync(AutoClaimArgs(stream, group, consumer, minIdleMilliseconds, start, count), token)
                .ConfigureAwait(false));

    public long XTrim(string stream, long maxLength, bool approximate = false)
        => StreamLongCommand(TrimArgs(stream, maxLength, approximate));

    public ValueTask<long> XTrimAsync(string stream, long maxLength, bool approximate = false,
        CancellationToken token = default)
        => StreamLongCommandAsync(TrimArgs(stream, maxLength, approximate), token);

    public long XDel(string stream, string[] ids)
        => StreamLongCommand(IdsArgs("XDEL", stream, null, ids));

    public ValueTask<long> XDelAsync(string stream, string[] ids, CancellationToken token = default)
        => StreamLongCommandAsync(IdsArgs("XDEL", stream, null, ids), token);

    private void AssertStreamPrimitive()
    {
        if (Transaction != null) throw new NotSupportedException("Streams commands do not support transactions.");
    }

    private string StreamStringCommand(byte[][] args)
    {
        AssertStreamPrimitive();
        return SendExpectString(args);
    }

    private ValueTask<string> StreamStringCommandAsync(byte[][] args, CancellationToken token)
    {
        AssertStreamPrimitive();
        return SendExpectStringAsync(token, args);
    }

    private long StreamLongCommand(byte[][] args)
    {
        AssertStreamPrimitive();
        return SendExpectLong(args);
    }

    private ValueTask<long> StreamLongCommandAsync(byte[][] args, CancellationToken token)
    {
        AssertStreamPrimitive();
        return SendExpectLongAsync(token, args);
    }

    internal void AssertStreamStandalone()
    {
        if (Pipeline != null || PipelineAsync != null || Transaction != null)
            throw new NotSupportedException("Structured Streams commands require a standalone connection.");
    }

    private RedisData StreamCommand(byte[][] args)
    {
        AssertStreamStandalone();
        return SendExpectComplexResponse(args);
    }

    private async ValueTask<RedisData> StreamCommandAsync(byte[][] args, CancellationToken token)
    {
        AssertStreamStandalone();
        token.ThrowIfCancellationRequested();
        try
        {
            return await SendExpectComplexResponseAsync(token, args).ConfigureAwait(false);
        }
        catch (Exception e) when (token.IsCancellationRequested &&
                                  (e is OperationCanceledException || e.InnerException is OperationCanceledException))
        {
            // The transport closes uncertain protocol state before wrapping cancellation.
            // Surface caller cancellation consistently without replaying the command.
            token.ThrowIfCancellationRequested();
            throw;
        }
    }

    private static byte[][] SimpleStreamArgs(params string[] args)
    {
        if (args.Any(x => x == null)) throw new ArgumentNullException(nameof(args));
        return args.Select(x => x.ToUtf8Bytes()).ToArray();
    }

    private static void PositiveStreamCount(int? count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
    }

    private static void AddCount(List<byte[]> args, int? count)
    {
        PositiveStreamCount(count);
        if (count != null)
        {
            args.Add("COUNT".ToUtf8Bytes());
            args.Add(count.Value.ToUtf8Bytes());
        }
    }

    internal static void ValidateStreamFields(RedisStreamField[] fields)
    {
        if (fields == null) throw new ArgumentNullException(nameof(fields));
        if (fields.Length == 0) throw new ArgumentException("XADD requires at least one field.", nameof(fields));
        foreach (var field in fields)
            if (field?.Name == null || field.Value == null)
                throw new ArgumentException("Stream field names and values cannot be null.", nameof(fields));
    }

    private static byte[][] AddArgs(string stream, RedisStreamField[] fields, string id, long? maxLength,
        bool approximate)
    {
        ValidateStreamFields(fields);
        if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
        if (approximate && maxLength == null) throw new ArgumentException("Approximate trimming requires maxLength.");
        var args = new List<byte[]>(SimpleStreamArgs("XADD", stream));
        if (maxLength != null)
        {
            args.Add("MAXLEN".ToUtf8Bytes());
            if (approximate) args.Add("~".ToUtf8Bytes());
            args.Add(maxLength.Value.ToUtf8Bytes());
        }

        args.Add((id ?? throw new ArgumentNullException(nameof(id))).ToUtf8Bytes());
        foreach (var field in fields)
        {
            args.Add(field.Name);
            args.Add(field.Value);
        }

        return args.ToArray();
    }

    private static byte[][] RangeArgs(string stream, string start, string end, int? count, bool reverse)
    {
        var args = new List<byte[]>(SimpleStreamArgs(reverse ? "XREVRANGE" : "XRANGE", stream, reverse ? end : start,
            reverse ? start : end));
        AddCount(args, count);
        return args.ToArray();
    }

    private static byte[][] GroupReadArgs(RedisStreamPosition[] streams, int? count, long? blockMilliseconds,
        string group, string consumer, bool noAck)
    {
        if (group == null) throw new ArgumentNullException(nameof(group));
        return ReadArgs(streams, count, blockMilliseconds, group, consumer, noAck);
    }

    private static byte[][] ReadArgs(RedisStreamPosition[] streams, int? count, long? blockMilliseconds, string group,
        string consumer, bool noAck)
    {
        if (streams == null) throw new ArgumentNullException(nameof(streams));
        if (streams.Length == 0)
            throw new ArgumentException("At least one stream position is required.", nameof(streams));
        if (blockMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(blockMilliseconds));
        var args = new List<byte[]> { (group == null ? "XREAD" : "XREADGROUP").ToUtf8Bytes() };
        if (group != null)
        {
            args.AddRange(SimpleStreamArgs("GROUP", group, consumer));
        }

        AddCount(args, count);
        if (blockMilliseconds != null)
            args.AddRange(SimpleStreamArgs("BLOCK", blockMilliseconds.Value.ToString(CultureInfo.InvariantCulture)));
        if (noAck) args.Add("NOACK".ToUtf8Bytes());
        args.Add("STREAMS".ToUtf8Bytes());
        if (streams.Any(x => x?.Stream == null || x.Id == null))
            throw new ArgumentException("Stream positions cannot contain null.", nameof(streams));
        args.AddRange(streams.Select(x => x.Stream.ToUtf8Bytes()));
        args.AddRange(streams.Select(x => x.Id.ToUtf8Bytes()));
        return args.ToArray();
    }

    private static byte[][] GroupCreateArgs(string stream, string group, string id, bool makeStream)
    {
        var args = new List<byte[]>(SimpleStreamArgs("XGROUP", "CREATE", stream, group, id));
        if (makeStream) args.Add("MKSTREAM".ToUtf8Bytes());
        return args.ToArray();
    }

    private static byte[][] IdsArgs(string command, string stream, string group, string[] ids)
    {
        if (command == "XACK" && group == null) throw new ArgumentNullException(nameof(group));
        if (ids == null) throw new ArgumentNullException(nameof(ids));
        if (ids.Length == 0 || ids.Any(x => x == null))
            throw new ArgumentException("At least one non-null stream ID is required.", nameof(ids));
        return SimpleStreamArgs((group == null ? new[] { command, stream } : new[] { command, stream, group })
            .Concat(ids).ToArray());
    }

    private static byte[][] PendingArgs(string stream, string group, string start, string end, int count,
        string consumer)
    {
        PositiveStreamCount(count);
        var args = new List<byte[]>(SimpleStreamArgs("XPENDING", stream, group, start, end,
            count.ToString(CultureInfo.InvariantCulture)));
        if (consumer != null) args.Add(consumer.ToUtf8Bytes());
        return args.ToArray();
    }

    private static byte[][] ClaimArgs(string stream, string group, string consumer, long minIdle, string[] ids)
    {
        if (minIdle < 0) throw new ArgumentOutOfRangeException(nameof(minIdle));
        var tail = IdsArgs("XCLAIM", stream, group, ids).Skip(3);
        return SimpleStreamArgs("XCLAIM", stream, group, consumer, minIdle.ToString(CultureInfo.InvariantCulture))
            .Concat(tail).ToArray();
    }

    private static byte[][] AutoClaimArgs(string stream, string group, string consumer, long minIdle, string start,
        int? count)
    {
        if (minIdle < 0) throw new ArgumentOutOfRangeException(nameof(minIdle));
        var args = new List<byte[]>(SimpleStreamArgs("XAUTOCLAIM", stream, group, consumer,
            minIdle.ToString(CultureInfo.InvariantCulture), start));
        AddCount(args, count);
        return args.ToArray();
    }

    private static byte[][] TrimArgs(string stream, long maxLength, bool approximate)
    {
        if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
        return SimpleStreamArgs(approximate
            ? new[] { "XTRIM", stream, "MAXLEN", "~", maxLength.ToString(CultureInfo.InvariantCulture) }
            : new[] { "XTRIM", stream, "MAXLEN", maxLength.ToString(CultureInfo.InvariantCulture) });
    }

    private static string StreamText(RedisData data) => data?.Data.FromUtf8Bytes();
    private static long StreamLong(RedisData data) => long.Parse(StreamText(data), CultureInfo.InvariantCulture);
    private static bool StreamOk(RedisData data) => StreamText(data) == "OK";

    private static RedisStreamEntry[] StreamEntries(RedisData data)
    {
        if (data?.Children == null) return Array.Empty<RedisStreamEntry>();
        return data.Children.Select(row =>
        {
            // Redis 6.2 can report a nil claim entry after its payload was deleted, without an ID.
            if (row.IsNull) return new RedisStreamEntry { Id = null, Fields = null };
            if (row.Children?.Count != 2) throw new FormatException("Invalid stream entry response.");
            var fields = row.Children[1].IsNull ? null : row.Children[1].Children;
            if (fields != null && fields.Count % 2 != 0) throw new FormatException("Invalid stream field pairs.");
            return new RedisStreamEntry
            {
                Id = StreamText(row.Children[0]),
                Fields = fields == null
                    ? null
                    : Enumerable.Range(0, fields.Count / 2).Select(i => new RedisStreamField
                    {
                        Name = fields[i * 2].Data, Value = fields[i * 2 + 1].Data
                    }).ToArray()
            };
        }).ToArray();
    }

    private static RedisStreamReadResult[] StreamReads(RedisData data)
        => data?.Children == null
            ? Array.Empty<RedisStreamReadResult>()
            : data.Children.Select(row =>
            {
                if (row.Children?.Count != 2) throw new FormatException("Invalid stream read response.");
                return new RedisStreamReadResult
                    { Stream = StreamText(row.Children[0]), Entries = StreamEntries(row.Children[1]) };
            }).ToArray();

    private static RedisStreamPendingSummary StreamPendingSummary(RedisData data)
    {
        if (data.Children?.Count != 4) throw new FormatException("Invalid pending summary response.");
        var c = data.Children;
        return new RedisStreamPendingSummary
        {
            Count = StreamLong(c[0]), LowestId = StreamText(c[1]), HighestId = StreamText(c[2]),
            Consumers = c[3].Children?.Select(x => new RedisStreamConsumerPending
                            { Consumer = StreamText(x.Children[0]), Count = StreamLong(x.Children[1]) }).ToArray() ??
                        Array.Empty<RedisStreamConsumerPending>()
        };
    }

    private static RedisStreamPendingEntry[] StreamPendingEntries(RedisData data)
        => data.Children?.Select(x =>
        {
            if (x.Children?.Count != 4) throw new FormatException("Invalid pending entry response.");
            return new RedisStreamPendingEntry
            {
                Id = StreamText(x.Children[0]), Consumer = StreamText(x.Children[1]),
                IdleMilliseconds = StreamLong(x.Children[2]), DeliveryCount = StreamLong(x.Children[3])
            };
        }).ToArray() ?? Array.Empty<RedisStreamPendingEntry>();

    private static RedisStreamAutoClaimResult StreamAutoClaim(RedisData data)
    {
        if (data.Children == null || data.Children.Count < 2 || data.Children.Count > 3)
            throw new FormatException("Invalid XAUTOCLAIM response.");
        return new RedisStreamAutoClaimResult
        {
            NextStartId = StreamText(data.Children[0]), Entries = StreamEntries(data.Children[1]),
            DeletedIds = data.Children.Count == 3
                ? data.Children[2].Children?.Select(StreamText).ToArray() ?? Array.Empty<string>()
                : Array.Empty<string>()
        };
    }
}