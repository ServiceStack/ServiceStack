using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Redis;

public static partial class RedisClientExtensions
{
    private static IRedisHashExpiryCommands HashExpiryCommands(object client) => client as IRedisHashExpiryCommands
        ?? throw new NotSupportedException("Client does not implement IRedisHashExpiryCommands.");
    private static IRedisHashExpiryCommandsAsync HashExpiryCommandsAsync(object client) => client as IRedisHashExpiryCommandsAsync
        ?? throw new NotSupportedException("Client does not implement IRedisHashExpiryCommandsAsync.");

    private static byte[] HashExpiryKey(string hashId)
        => (hashId ?? throw new ArgumentNullException(nameof(hashId))).ToUtf8Bytes();

    private static byte[][] HashExpiryFields(string[] fields)
    {
        if (fields == null) throw new ArgumentNullException(nameof(fields));
        var bytes = new byte[fields.Length][];
        for (var i = 0; i < fields.Length; i++)
            bytes[i] = (fields[i] ?? throw new ArgumentException("Fields cannot contain null.", nameof(fields))).ToUtf8Bytes();
        return bytes;
    }

    public static long[] HExpire(this IRedisNativeClient client, byte[] hashId, long seconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommands(client).HExpire(hashId, seconds, fields, condition);

    public static long[] HExpire(this IRedisNativeClient client, string hashId, long seconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommands(client).HExpire(HashExpiryKey(hashId), seconds, fields, condition);

    public static long[] HExpire(this IRedisClient client, string hashId, long seconds, string[] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommands(client).HExpire(HashExpiryKey(hashId), seconds, HashExpiryFields(fields), condition);

    public static ValueTask<long[]> HExpireAsync(this IRedisNativeClientAsync client, byte[] hashId, long seconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HExpireAsync(hashId, seconds, fields, condition, token);

    public static ValueTask<long[]> HExpireAsync(this IRedisNativeClientAsync client, string hashId, long seconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HExpireAsync(HashExpiryKey(hashId), seconds, fields, condition, token);

    public static ValueTask<long[]> HExpireAsync(this IRedisClientAsync client, string hashId, long seconds, string[] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HExpireAsync(HashExpiryKey(hashId), seconds, HashExpiryFields(fields), condition, token);

    public static long[] HPExpire(this IRedisNativeClient client, byte[] hashId, long milliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommands(client).HPExpire(hashId, milliseconds, fields, condition);

    public static long[] HPExpire(this IRedisNativeClient client, string hashId, long milliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommands(client).HPExpire(HashExpiryKey(hashId), milliseconds, fields, condition);

    public static long[] HPExpire(this IRedisClient client, string hashId, long milliseconds, string[] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommands(client).HPExpire(HashExpiryKey(hashId), milliseconds, HashExpiryFields(fields), condition);

    public static ValueTask<long[]> HPExpireAsync(this IRedisNativeClientAsync client, byte[] hashId, long milliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPExpireAsync(hashId, milliseconds, fields, condition, token);

    public static ValueTask<long[]> HPExpireAsync(this IRedisNativeClientAsync client, string hashId, long milliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPExpireAsync(HashExpiryKey(hashId), milliseconds, fields, condition, token);

    public static ValueTask<long[]> HPExpireAsync(this IRedisClientAsync client, string hashId, long milliseconds, string[] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPExpireAsync(HashExpiryKey(hashId), milliseconds, HashExpiryFields(fields), condition, token);

    public static long[] HExpireAt(this IRedisNativeClient client, byte[] hashId, long unixTimeSeconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommands(client).HExpireAt(hashId, unixTimeSeconds, fields, condition);

    public static long[] HExpireAt(this IRedisNativeClient client, string hashId, long unixTimeSeconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommands(client).HExpireAt(HashExpiryKey(hashId), unixTimeSeconds, fields, condition);

    public static long[] HExpireAt(this IRedisClient client, string hashId, long unixTimeSeconds, string[] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommands(client).HExpireAt(HashExpiryKey(hashId), unixTimeSeconds, HashExpiryFields(fields), condition);

    public static ValueTask<long[]> HExpireAtAsync(this IRedisNativeClientAsync client, byte[] hashId, long unixTimeSeconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HExpireAtAsync(hashId, unixTimeSeconds, fields, condition, token);

    public static ValueTask<long[]> HExpireAtAsync(this IRedisNativeClientAsync client, string hashId, long unixTimeSeconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HExpireAtAsync(HashExpiryKey(hashId), unixTimeSeconds, fields, condition, token);

    public static ValueTask<long[]> HExpireAtAsync(this IRedisClientAsync client, string hashId, long unixTimeSeconds, string[] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HExpireAtAsync(HashExpiryKey(hashId), unixTimeSeconds, HashExpiryFields(fields), condition, token);

    public static long[] HPExpireAt(this IRedisNativeClient client, byte[] hashId, long unixTimeMilliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommands(client).HPExpireAt(hashId, unixTimeMilliseconds, fields, condition);

    public static long[] HPExpireAt(this IRedisNativeClient client, string hashId, long unixTimeMilliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommands(client).HPExpireAt(HashExpiryKey(hashId), unixTimeMilliseconds, fields, condition);

    public static long[] HPExpireAt(this IRedisClient client, string hashId, long unixTimeMilliseconds, string[] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommands(client).HPExpireAt(HashExpiryKey(hashId), unixTimeMilliseconds, HashExpiryFields(fields), condition);

    public static ValueTask<long[]> HPExpireAtAsync(this IRedisNativeClientAsync client, byte[] hashId, long unixTimeMilliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPExpireAtAsync(hashId, unixTimeMilliseconds, fields, condition, token);

    public static ValueTask<long[]> HPExpireAtAsync(this IRedisNativeClientAsync client, string hashId, long unixTimeMilliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPExpireAtAsync(HashExpiryKey(hashId), unixTimeMilliseconds, fields, condition, token);

    public static ValueTask<long[]> HPExpireAtAsync(this IRedisClientAsync client, string hashId, long unixTimeMilliseconds, string[] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPExpireAtAsync(HashExpiryKey(hashId), unixTimeMilliseconds, HashExpiryFields(fields), condition, token);

    public static long[] HTtl(this IRedisNativeClient client, byte[] hashId, byte[][] fields)
        => HashExpiryCommands(client).HTtl(hashId, fields);

    public static long[] HTtl(this IRedisNativeClient client, string hashId, byte[][] fields)
        => HashExpiryCommands(client).HTtl(HashExpiryKey(hashId), fields);

    public static long[] HTtl(this IRedisClient client, string hashId, string[] fields)
        => HashExpiryCommands(client).HTtl(HashExpiryKey(hashId), HashExpiryFields(fields));

    public static ValueTask<long[]> HTtlAsync(this IRedisNativeClientAsync client, byte[] hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HTtlAsync(hashId, fields, token);

    public static ValueTask<long[]> HTtlAsync(this IRedisNativeClientAsync client, string hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HTtlAsync(HashExpiryKey(hashId), fields, token);

    public static ValueTask<long[]> HTtlAsync(this IRedisClientAsync client, string hashId, string[] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HTtlAsync(HashExpiryKey(hashId), HashExpiryFields(fields), token);

    public static long[] HPTtl(this IRedisNativeClient client, byte[] hashId, byte[][] fields)
        => HashExpiryCommands(client).HPTtl(hashId, fields);

    public static long[] HPTtl(this IRedisNativeClient client, string hashId, byte[][] fields)
        => HashExpiryCommands(client).HPTtl(HashExpiryKey(hashId), fields);

    public static long[] HPTtl(this IRedisClient client, string hashId, string[] fields)
        => HashExpiryCommands(client).HPTtl(HashExpiryKey(hashId), HashExpiryFields(fields));

    public static ValueTask<long[]> HPTtlAsync(this IRedisNativeClientAsync client, byte[] hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPTtlAsync(hashId, fields, token);

    public static ValueTask<long[]> HPTtlAsync(this IRedisNativeClientAsync client, string hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPTtlAsync(HashExpiryKey(hashId), fields, token);

    public static ValueTask<long[]> HPTtlAsync(this IRedisClientAsync client, string hashId, string[] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPTtlAsync(HashExpiryKey(hashId), HashExpiryFields(fields), token);

    public static long[] HExpireTime(this IRedisNativeClient client, byte[] hashId, byte[][] fields)
        => HashExpiryCommands(client).HExpireTime(hashId, fields);

    public static long[] HExpireTime(this IRedisNativeClient client, string hashId, byte[][] fields)
        => HashExpiryCommands(client).HExpireTime(HashExpiryKey(hashId), fields);

    public static long[] HExpireTime(this IRedisClient client, string hashId, string[] fields)
        => HashExpiryCommands(client).HExpireTime(HashExpiryKey(hashId), HashExpiryFields(fields));

    public static ValueTask<long[]> HExpireTimeAsync(this IRedisNativeClientAsync client, byte[] hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HExpireTimeAsync(hashId, fields, token);

    public static ValueTask<long[]> HExpireTimeAsync(this IRedisNativeClientAsync client, string hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HExpireTimeAsync(HashExpiryKey(hashId), fields, token);

    public static ValueTask<long[]> HExpireTimeAsync(this IRedisClientAsync client, string hashId, string[] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HExpireTimeAsync(HashExpiryKey(hashId), HashExpiryFields(fields), token);

    public static long[] HPExpireTime(this IRedisNativeClient client, byte[] hashId, byte[][] fields)
        => HashExpiryCommands(client).HPExpireTime(hashId, fields);

    public static long[] HPExpireTime(this IRedisNativeClient client, string hashId, byte[][] fields)
        => HashExpiryCommands(client).HPExpireTime(HashExpiryKey(hashId), fields);

    public static long[] HPExpireTime(this IRedisClient client, string hashId, string[] fields)
        => HashExpiryCommands(client).HPExpireTime(HashExpiryKey(hashId), HashExpiryFields(fields));

    public static ValueTask<long[]> HPExpireTimeAsync(this IRedisNativeClientAsync client, byte[] hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPExpireTimeAsync(hashId, fields, token);

    public static ValueTask<long[]> HPExpireTimeAsync(this IRedisNativeClientAsync client, string hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPExpireTimeAsync(HashExpiryKey(hashId), fields, token);

    public static ValueTask<long[]> HPExpireTimeAsync(this IRedisClientAsync client, string hashId, string[] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPExpireTimeAsync(HashExpiryKey(hashId), HashExpiryFields(fields), token);

    public static long[] HPersist(this IRedisNativeClient client, byte[] hashId, byte[][] fields)
        => HashExpiryCommands(client).HPersist(hashId, fields);

    public static long[] HPersist(this IRedisNativeClient client, string hashId, byte[][] fields)
        => HashExpiryCommands(client).HPersist(HashExpiryKey(hashId), fields);

    public static long[] HPersist(this IRedisClient client, string hashId, string[] fields)
        => HashExpiryCommands(client).HPersist(HashExpiryKey(hashId), HashExpiryFields(fields));

    public static ValueTask<long[]> HPersistAsync(this IRedisNativeClientAsync client, byte[] hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPersistAsync(hashId, fields, token);

    public static ValueTask<long[]> HPersistAsync(this IRedisNativeClientAsync client, string hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPersistAsync(HashExpiryKey(hashId), fields, token);

    public static ValueTask<long[]> HPersistAsync(this IRedisClientAsync client, string hashId, string[] fields, CancellationToken token = default)
        => HashExpiryCommandsAsync(client).HPersistAsync(HashExpiryKey(hashId), HashExpiryFields(fields), token);

}
