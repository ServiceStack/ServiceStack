#if NET10_0_OR_GREATER
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace ServiceStack.Redis;

/// <summary>
/// Binary IDistributedCache backed by exclusively leased ServiceStack.Redis writer clients.
/// Requires Redis 5+ and EVAL, EVALSHA, TIME, HGET, HEXISTS, HMGET, HMSET, PEXPIRE, PERSIST and DEL permissions.
/// </summary>
public sealed class RedisDistributedCache : IDistributedCache, IDisposable
{
    private readonly IRedisClientsManager manager;
    private readonly IRedisClientsManagerAsync asyncManager;
    private readonly bool ownsManager;
    private readonly string prefix;
    private int disposed;
    private const long UnixEpochTicks = 621355968000000000;

    // One key owns both the raw payload and expiry metadata. Mutations/refreshes are atomic.
    private const string NowScript =
        "local t = redis.call('TIME'); local now = tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000); ";

    private const string SetScript = NowScript +
        "local absolute = tonumber(ARGV[2]); local sliding = tonumber(ARGV[3]); local ttl = -1; " +
        "if absolute >= 0 then ttl = absolute - now; end; " +
        "if absolute >= 0 and ttl <= 0 then redis.call('DEL', KEYS[1]); return 0; end; " +
        "if sliding > 0 and (ttl < 0 or sliding < ttl) then ttl = sliding; end; " +
        "redis.call('HMSET', KEYS[1], 'data', ARGV[1], 'absolute', ARGV[2], 'sliding', ARGV[3]); " +
        "if ttl >= 0 then redis.call('PEXPIRE', KEYS[1], ttl); else redis.call('PERSIST', KEYS[1]); end; return 1;";

    private const string AccessScript =
        "local function miss() if ARGV[1] == '1' then return {false}; else return 0; end; end; " +
        "local data; if ARGV[1] == '1' then data = redis.call('HGET', KEYS[1], 'data'); " +
        "if not data then return miss(); end; " +
        "elseif redis.call('HEXISTS', KEYS[1], 'data') == 0 then return miss(); end; " +
        "local row = redis.call('HMGET', KEYS[1], 'absolute', 'sliding'); " +
        "local absolute = tonumber(row[1] or ''); local sliding = tonumber(row[2] or ''); " +
        "if not absolute or not sliding or absolute < -1 or sliding < -1 or sliding == 0 then " +
        "return redis.error_reply('Invalid ServiceStack distributed cache metadata'); end; " +
        "local ttl = sliding; " +
        "if absolute >= 0 then " + NowScript +
        "local remaining = absolute - now; if remaining <= 0 then redis.call('DEL', KEYS[1]); return miss(); end; " +
        "if ttl < 0 or remaining < ttl then ttl = remaining; end; end; " +
        "if sliding > 0 then redis.call('PEXPIRE', KEYS[1], ttl); end; " +
        "if ARGV[1] == '1' then return {data}; else return 1; end;";

    // Run the cached script by SHA1 so each call doesn't resend the script body. NOSCRIPT means it did not run,
    // so falling back to EVAL (which also caches the script) cannot apply a mutation twice.
    private static readonly string SetScriptSha = Sha1(SetScript);
    private static readonly string AccessScriptSha = Sha1(AccessScript);

    public RedisDistributedCache(IRedisClientsManager manager, RedisDistributedCacheOptions options = null,
        bool ownsManager = false)
    {
        this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        asyncManager = manager as IRedisClientsManagerAsync
                       ?? throw new ArgumentException("Manager must implement IRedisClientsManagerAsync.",
                           nameof(manager));
        prefix = (options ?? new RedisDistributedCacheOptions()).KeyPrefix;
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("A nonempty private cache key prefix is required.", nameof(options));
        this.ownsManager = ownsManager;
    }

    public byte[] Get(string key)
    {
        var args = AccessArgs(key, true);
        using var client = manager.GetClient();
        return Payload(EvalScript(Native(client), AccessScript, AccessScriptSha, args));
    }

    public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
    {
        var args = AccessArgs(key, true);
        token.ThrowIfCancellationRequested();
        await using var client = await asyncManager.GetClientAsync(token).ConfigureAwait(false);
        try
        {
            return Payload(await EvalScriptAsync(NativeAsync(client), AccessScript, AccessScriptSha, args, token).ConfigureAwait(false));
        }
        catch (Exception e) when (CallerCancelled(e, token))
        {
            token.ThrowIfCancellationRequested();
            throw;
        }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var args = SetArgs(key, value, options);
        using var client = manager.GetClient();
        EvalScriptInt(Native(client), SetScript, SetScriptSha, args);
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        var args = SetArgs(key, value, options);
        token.ThrowIfCancellationRequested();
        await using var client = await asyncManager.GetClientAsync(token).ConfigureAwait(false);
        try
        {
            await EvalScriptIntAsync(NativeAsync(client), SetScript, SetScriptSha, args, token).ConfigureAwait(false);
        }
        catch (Exception e) when (CallerCancelled(e, token))
        {
            token.ThrowIfCancellationRequested();
            throw;
        }
    }

    public void Refresh(string key)
    {
        var args = AccessArgs(key, false);
        using var client = manager.GetClient();
        EvalScriptInt(Native(client), AccessScript, AccessScriptSha, args);
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        var args = AccessArgs(key, false);
        token.ThrowIfCancellationRequested();
        await using var client = await asyncManager.GetClientAsync(token).ConfigureAwait(false);
        try
        {
            await EvalScriptIntAsync(NativeAsync(client), AccessScript, AccessScriptSha, args, token).ConfigureAwait(false);
        }
        catch (Exception e) when (CallerCancelled(e, token))
        {
            token.ThrowIfCancellationRequested();
            throw;
        }
    }

    public void Remove(string key)
    {
        var redisKey = Key(key);
        using var client = manager.GetClient();
        Native(client).Del(redisKey);
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        var redisKey = Key(key);
        token.ThrowIfCancellationRequested();
        await using var client = await asyncManager.GetClientAsync(token).ConfigureAwait(false);
        try
        {
            await NativeAsync(client).DelAsync(redisKey, token).ConfigureAwait(false);
        }
        catch (Exception e) when (CallerCancelled(e, token))
        {
            token.ThrowIfCancellationRequested();
            throw;
        }
    }

    private string Key(string key)
    {
        if (Volatile.Read(ref disposed) != 0) throw new ObjectDisposedException(nameof(RedisDistributedCache));
        if (key == null) throw new ArgumentNullException(nameof(key));
        return prefix + key;
    }

    private byte[][] AccessArgs(string key, bool get) => new[] { Bytes(Key(key)), Bytes(get ? "1" : "0") };

    private byte[][] SetArgs(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var redisKey = Key(key);
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (options == null) throw new ArgumentNullException(nameof(options));
        var relative = options.AbsoluteExpirationRelativeToNow;
        var absolute = options.AbsoluteExpiration;
        var sliding = options.SlidingExpiration;
        var now = DateTimeOffset.UtcNow;
        // Matches the Microsoft provider convention: relative absolute expiry takes precedence if both are set.
        if (relative.HasValue) absolute = now.Add(relative.Value);
        if (absolute.HasValue && absolute.Value <= now)
            throw new ArgumentOutOfRangeException(nameof(options), "Absolute expiration must be in the future.");
        var deadline = absolute.HasValue ? CeilingMilliseconds(absolute.Value.UtcDateTime.Ticks - UnixEpochTicks) : -1;
        var interval = sliding.HasValue ? CeilingMilliseconds(sliding.Value.Ticks) : -1;
        return [
            Bytes(redisKey), value, Bytes(deadline.ToString(CultureInfo.InvariantCulture)),
            Bytes(interval.ToString(CultureInfo.InvariantCulture))
        ];
    }

    private static long CeilingMilliseconds(long ticks) =>
        ticks / TimeSpan.TicksPerMillisecond + (ticks % TimeSpan.TicksPerMillisecond > 0 ? 1 : 0);

    private static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);

    private static string Sha1(string script)
    {
        using var sha1 = SHA1.Create();
        return Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(script))).ToLowerInvariant();
    }

    private static bool IsNoScript(RedisResponseException e) => e.Message.StartsWith("NOSCRIPT", StringComparison.Ordinal);

    private static byte[][] EvalScript(IRedisNativeClient client, string script, string sha, byte[][] args)
    {
        try { return client.EvalSha(sha, 1, args); }
        catch (RedisResponseException e) when (IsNoScript(e)) { return client.Eval(script, 1, args); }
    }

    private static long EvalScriptInt(IRedisNativeClient client, string script, string sha, byte[][] args)
    {
        try { return client.EvalShaInt(sha, 1, args); }
        catch (RedisResponseException e) when (IsNoScript(e)) { return client.EvalInt(script, 1, args); }
    }

    private static async ValueTask<byte[][]> EvalScriptAsync(IRedisNativeClientAsync client, string script, string sha,
        byte[][] args, CancellationToken token)
    {
        try { return await client.EvalShaAsync(sha, 1, args, token).ConfigureAwait(false); }
        catch (RedisResponseException e) when (IsNoScript(e))
        {
            return await client.EvalAsync(script, 1, args, token).ConfigureAwait(false);
        }
    }

    private static async ValueTask<long> EvalScriptIntAsync(IRedisNativeClientAsync client, string script, string sha,
        byte[][] args, CancellationToken token)
    {
        try { return await client.EvalShaIntAsync(sha, 1, args, token).ConfigureAwait(false); }
        catch (RedisResponseException e) when (IsNoScript(e))
        {
            return await client.EvalIntAsync(script, 1, args, token).ConfigureAwait(false);
        }
    }

    private static byte[] Payload(byte[][] result)
    {
        if (result == null || result.Length != 1) throw new FormatException("Invalid distributed cache response.");
        return result[0]; // A nil bulk value is a miss; zero bytes is a stored empty value.
    }

    private static IRedisNativeClient Native(IRedisClient client) => client as IRedisNativeClient
        ?? throw new NotSupportedException("The leased client must implement IRedisNativeClient.");

    private static IRedisNativeClientAsync NativeAsync(IRedisClientAsync client) => client as IRedisNativeClientAsync
        ?? throw new NotSupportedException("The leased client must implement IRedisNativeClientAsync.");

    private static bool CallerCancelled(Exception e, CancellationToken token) => token.IsCancellationRequested
        && (e is OperationCanceledException || e.InnerException is OperationCanceledException);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0 && ownsManager) manager.Dispose();
    }
}
#endif