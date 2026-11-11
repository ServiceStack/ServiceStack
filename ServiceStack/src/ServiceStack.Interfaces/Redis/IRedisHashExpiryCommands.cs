namespace ServiceStack.Redis;

/// <summary>
/// Optional native hash-field expiry commands. Requires Redis 7.4+ or a compatible server (Valkey 9+).
/// Keys and field names are binary. Results are per requested field in input order, including duplicates.
/// Expiry updates return -2 (missing), 0 (condition failed), 1 (set), or 2 (immediately deleted).
/// TTL/time queries return -2 (missing), -1 (persistent), or a time in the command's units.
/// HPERSIST returns -2 (missing), -1 (already persistent), or 1 (expiry removed).
/// Mutations require writer leases. Standalone only: pipelines/transactions are rejected locally.
/// No emulation or server-version guessing; unsupported commands and ACL errors propagate.
/// Existing client interfaces intentionally do not inherit this capability.
/// </summary>
public interface IRedisHashExpiryCommands
{
    long[] HExpire(byte[] hashId, long seconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None);
    long[] HPExpire(byte[] hashId, long milliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None);
    long[] HExpireAt(byte[] hashId, long unixTimeSeconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None);
    long[] HPExpireAt(byte[] hashId, long unixTimeMilliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None);
    long[] HTtl(byte[] hashId, byte[][] fields);
    long[] HPTtl(byte[] hashId, byte[][] fields);
    long[] HExpireTime(byte[] hashId, byte[][] fields);
    long[] HPExpireTime(byte[] hashId, byte[][] fields);
    long[] HPersist(byte[] hashId, byte[][] fields);
}
