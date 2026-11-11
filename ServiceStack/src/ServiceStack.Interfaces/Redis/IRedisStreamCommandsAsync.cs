using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis;

/// <summary>Optional RESP2 Streams commands. Redis 5+; XAUTOCLAIM requires 6.2+. Group reads mutate state.
/// Use an exclusive writer lease for consumer operations and a dedicated lease for blocking reads.
/// XADD, XACK, XDEL, XTRIM and XLEN support pipelines; other commands require standalone calls.
/// Streams transactions are not supported. Existing raw command APIs remain available.</summary>
public interface IRedisStreamCommandsAsync
{
    ValueTask<string> XAddAsync(string stream, RedisStreamField[] fields, string id = "*", long? maxLength = null, bool approximate = false, CancellationToken token = default);
    ValueTask<RedisStreamEntry[]> XRangeAsync(string stream, string start = "-", string end = "+", int? count = null, bool reverse = false, CancellationToken token = default);
    ValueTask<long> XLenAsync(string stream, CancellationToken token = default);
    ValueTask<RedisStreamReadResult[]> XReadAsync(RedisStreamPosition[] streams, int? count = null, long? blockMilliseconds = null, CancellationToken token = default);
    ValueTask<bool> XGroupCreateAsync(string stream, string group, string id = "0-0", bool makeStream = false, CancellationToken token = default);
    ValueTask<long> XGroupDestroyAsync(string stream, string group, CancellationToken token = default);
    ValueTask<RedisStreamReadResult[]> XReadGroupAsync(string group, string consumer, RedisStreamPosition[] streams, int? count = null, long? blockMilliseconds = null, bool noAck = false, CancellationToken token = default);
    ValueTask<long> XAckAsync(string stream, string group, string[] ids, CancellationToken token = default);
    ValueTask<RedisStreamPendingSummary> XPendingAsync(string stream, string group, CancellationToken token = default);
    ValueTask<RedisStreamPendingEntry[]> XPendingEntriesAsync(string stream, string group, string start = "-", string end = "+", int count = 100, string consumer = null, CancellationToken token = default);
    ValueTask<RedisStreamEntry[]> XClaimAsync(string stream, string group, string consumer, long minIdleMilliseconds, string[] ids, CancellationToken token = default);
    ValueTask<RedisStreamAutoClaimResult> XAutoClaimAsync(string stream, string group, string consumer, long minIdleMilliseconds, string start = "0-0", int? count = null, CancellationToken token = default);
    ValueTask<long> XTrimAsync(string stream, long maxLength, bool approximate = false, CancellationToken token = default);
    ValueTask<long> XDelAsync(string stream, string[] ids, CancellationToken token = default);
}
