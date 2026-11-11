using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Redis;

/// <summary>
/// Writer-only key commands. Requires the optional IRedisNativeKeyCommands capability (implemented by RedisClient).
/// Server errors, including unsupported commands and ACL denials, are propagated without a fallback.
/// </summary>
public static partial class RedisClientExtensions
{
    private static IRedisNativeKeyCommands KeyCommands(object client) => client as IRedisNativeKeyCommands
        ?? throw new NotSupportedException("This client does not implement IRedisNativeKeyCommands.");

    private static IRedisNativeKeyCommandsAsync KeyCommandsAsync(object client) =>
        client as IRedisNativeKeyCommandsAsync
        ?? throw new NotSupportedException("This client does not implement IRedisNativeKeyCommandsAsync.");

    public static byte[] GetEx(this IRedisNativeClient redis, string key, RedisGetExOptions options = null)
        => KeyCommands(redis).GetEx(key, options);

    public static byte[] GetDel(this IRedisNativeClient redis, string key) => KeyCommands(redis).GetDel(key);

    public static long Unlink(this IRedisNativeClient redis, params string[] keys) => KeyCommands(redis).Unlink(keys);

    public static ValueTask<byte[]> GetExAsync(this IRedisNativeClientAsync redis, string key,
        RedisGetExOptions options = null, CancellationToken token = default)
        => KeyCommandsAsync(redis).GetExAsync(key, options, token);

    public static ValueTask<byte[]> GetDelAsync(this IRedisNativeClientAsync redis, string key,
        CancellationToken token = default)
        => KeyCommandsAsync(redis).GetDelAsync(key, token);

    public static ValueTask<long> UnlinkAsync(this IRedisNativeClientAsync redis, string[] keys,
        CancellationToken token = default)
        => KeyCommandsAsync(redis).UnlinkAsync(keys, token);

    public static ValueTask<long> UnlinkAsync(this IRedisNativeClientAsync redis, string key,
        CancellationToken token = default)
        => KeyCommandsAsync(redis).UnlinkAsync(new[] { key }, token);

    /// <summary>GETEX: returns a UTF-8 string while updating its expiry atomically. Use a writer connection.</summary>
    public static string GetAndExpireValue(this IRedisClient redis, string key, RedisGetExOptions options = null)
        => KeyCommands(redis).GetEx(key, options).FromUtf8Bytes();

    /// <summary>GETDEL: returns and deletes a UTF-8 string atomically. Use a writer connection.</summary>
    public static string GetAndDeleteValue(this IRedisClient redis, string key)
        => KeyCommands(redis).GetDel(key).FromUtf8Bytes();

    public static long Unlink(this IRedisClient redis, params string[] keys) => KeyCommands(redis).Unlink(keys);

    public static async ValueTask<string> GetAndExpireValueAsync(this IRedisClientAsync redis, string key,
        RedisGetExOptions options = null, CancellationToken token = default)
        => (await KeyCommandsAsync(redis).GetExAsync(key, options, token).ConfigureAwait(false)).FromUtf8Bytes();

    public static async ValueTask<string> GetAndDeleteValueAsync(this IRedisClientAsync redis, string key,
        CancellationToken token = default)
        => (await KeyCommandsAsync(redis).GetDelAsync(key, token).ConfigureAwait(false)).FromUtf8Bytes();

    public static ValueTask<long> UnlinkAsync(this IRedisClientAsync redis, string[] keys,
        CancellationToken token = default)
        => KeyCommandsAsync(redis).UnlinkAsync(keys, token);

    public static ValueTask<long> UnlinkAsync(this IRedisClientAsync redis, string key,
        CancellationToken token = default)
        => KeyCommandsAsync(redis).UnlinkAsync(new[] { key }, token);
}