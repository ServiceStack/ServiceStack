using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis;

/// <summary>Optional RESP2 Streams commands. Redis 5+; XAUTOCLAIM requires 6.2+. Group reads mutate state.
/// Use an exclusive writer lease for consumer operations and a dedicated lease for blocking reads.
/// XADD, XACK, XDEL, XTRIM and XLEN support pipelines; other commands require standalone calls.
/// Streams transactions are not supported. Existing raw command APIs remain available.</summary>
public interface IRedisStreamCommands
{
    string XAdd(string stream, RedisStreamField[] fields, string id = "*", long? maxLength = null, bool approximate = false);
    RedisStreamEntry[] XRange(string stream, string start = "-", string end = "+", int? count = null, bool reverse = false);
    long XLen(string stream);
    RedisStreamReadResult[] XRead(RedisStreamPosition[] streams, int? count = null, long? blockMilliseconds = null);
    bool XGroupCreate(string stream, string group, string id = "0-0", bool makeStream = false);
    long XGroupDestroy(string stream, string group);
    RedisStreamReadResult[] XReadGroup(string group, string consumer, RedisStreamPosition[] streams, int? count = null, long? blockMilliseconds = null, bool noAck = false);
    long XAck(string stream, string group, string[] ids);
    RedisStreamPendingSummary XPending(string stream, string group);
    RedisStreamPendingEntry[] XPendingEntries(string stream, string group, string start = "-", string end = "+", int count = 100, string consumer = null);
    RedisStreamEntry[] XClaim(string stream, string group, string consumer, long minIdleMilliseconds, string[] ids);
    RedisStreamAutoClaimResult XAutoClaim(string stream, string group, string consumer, long minIdleMilliseconds, string start = "0-0", int? count = null);
    long XTrim(string stream, long maxLength, bool approximate = false);
    long XDel(string stream, string[] ids);
}
