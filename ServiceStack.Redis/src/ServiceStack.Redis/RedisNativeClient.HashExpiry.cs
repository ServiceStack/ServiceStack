using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Redis;

public partial class RedisNativeClient : IRedisHashExpiryCommands, IRedisHashExpiryCommandsAsync
{
    public long[] HExpire(byte[] hashId, long seconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommand(HashExpiryArgs(Commands.HExpire, hashId, fields, seconds, condition), fields.Length);

    public ValueTask<long[]> HExpireAsync(byte[] hashId, long seconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandAsync(HashExpiryArgs(Commands.HExpire, hashId, fields, seconds, condition), fields.Length, token);

    public long[] HPExpire(byte[] hashId, long milliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommand(HashExpiryArgs(Commands.HPExpire, hashId, fields, milliseconds, condition), fields.Length);

    public ValueTask<long[]> HPExpireAsync(byte[] hashId, long milliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandAsync(HashExpiryArgs(Commands.HPExpire, hashId, fields, milliseconds, condition), fields.Length, token);

    public long[] HExpireAt(byte[] hashId, long unixTimeSeconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommand(HashExpiryArgs(Commands.HExpireAt, hashId, fields, unixTimeSeconds, condition), fields.Length);

    public ValueTask<long[]> HExpireAtAsync(byte[] hashId, long unixTimeSeconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandAsync(HashExpiryArgs(Commands.HExpireAt, hashId, fields, unixTimeSeconds, condition), fields.Length, token);

    public long[] HPExpireAt(byte[] hashId, long unixTimeMilliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
        => HashExpiryCommand(HashExpiryArgs(Commands.HPExpireAt, hashId, fields, unixTimeMilliseconds, condition), fields.Length);

    public ValueTask<long[]> HPExpireAtAsync(byte[] hashId, long unixTimeMilliseconds, byte[][] fields, RedisHashExpiryCondition condition = RedisHashExpiryCondition.None, CancellationToken token = default)
        => HashExpiryCommandAsync(HashExpiryArgs(Commands.HPExpireAt, hashId, fields, unixTimeMilliseconds, condition), fields.Length, token);

    public long[] HTtl(byte[] hashId, byte[][] fields)
        => HashExpiryCommand(HashExpiryArgs(Commands.HTtl, hashId, fields), fields.Length);

    public ValueTask<long[]> HTtlAsync(byte[] hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandAsync(HashExpiryArgs(Commands.HTtl, hashId, fields), fields.Length, token);

    public long[] HPTtl(byte[] hashId, byte[][] fields)
        => HashExpiryCommand(HashExpiryArgs(Commands.HPTtl, hashId, fields), fields.Length);

    public ValueTask<long[]> HPTtlAsync(byte[] hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandAsync(HashExpiryArgs(Commands.HPTtl, hashId, fields), fields.Length, token);

    public long[] HExpireTime(byte[] hashId, byte[][] fields)
        => HashExpiryCommand(HashExpiryArgs(Commands.HExpireTime, hashId, fields), fields.Length);

    public ValueTask<long[]> HExpireTimeAsync(byte[] hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandAsync(HashExpiryArgs(Commands.HExpireTime, hashId, fields), fields.Length, token);

    public long[] HPExpireTime(byte[] hashId, byte[][] fields)
        => HashExpiryCommand(HashExpiryArgs(Commands.HPExpireTime, hashId, fields), fields.Length);

    public ValueTask<long[]> HPExpireTimeAsync(byte[] hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandAsync(HashExpiryArgs(Commands.HPExpireTime, hashId, fields), fields.Length, token);

    public long[] HPersist(byte[] hashId, byte[][] fields)
        => HashExpiryCommand(HashExpiryArgs(Commands.HPersist, hashId, fields), fields.Length);

    public ValueTask<long[]> HPersistAsync(byte[] hashId, byte[][] fields, CancellationToken token = default)
        => HashExpiryCommandAsync(HashExpiryArgs(Commands.HPersist, hashId, fields), fields.Length, token);

    private static readonly byte[] HashExpiryFieldsToken = "FIELDS".ToUtf8Bytes();

    private static byte[][] HashExpiryArgs(byte[] command, byte[] hashId, byte[][] fields, long? value = null,
        RedisHashExpiryCondition condition = RedisHashExpiryCondition.None)
    {
        if (hashId == null) throw new ArgumentNullException(nameof(hashId));
        if (fields == null) throw new ArgumentNullException(nameof(fields));
        if (fields.Length == 0) throw new ArgumentException("At least one hash field is required.", nameof(fields));
        foreach (var field in fields)
            if (field == null) throw new ArgumentException("Hash fields cannot contain null.", nameof(fields));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Hash field expiration must be nonnegative.");
        var option = condition switch
        {
            RedisHashExpiryCondition.None => null,
            RedisHashExpiryCondition.NX => "NX",
            RedisHashExpiryCondition.XX => "XX",
            RedisHashExpiryCondition.GT => "GT",
            RedisHashExpiryCondition.LT => "LT",
            _ => throw new ArgumentOutOfRangeException(nameof(condition))
        };
        var args = new List<byte[]>(fields.Length + 6) { command, hashId };
        if (value != null) args.Add(value.Value.ToString(CultureInfo.InvariantCulture).ToUtf8Bytes());
        if (option != null) args.Add(option.ToUtf8Bytes());
        args.Add(HashExpiryFieldsToken);
        args.Add(fields.Length.ToString(CultureInfo.InvariantCulture).ToUtf8Bytes());
        args.AddRange(fields);
        return args.ToArray();
    }

    private void AssertHashExpiryStandalone()
    {
        if (IsDisposed) throw new ObjectDisposedException(nameof(RedisNativeClient));
        if (Pipeline != null || PipelineAsync != null || Transaction != null)
            throw new NotSupportedException("Hash-field expiration commands require standalone calls.");
    }

    private long[] HashExpiryCommand(byte[][] args, int expected)
    {
        AssertHashExpiryStandalone();
        try { return SendReceive(args, () => ReadHashExpiryResults(expected)); }
        catch (RedisResponseException) { throw; } // A complete server error reply leaves the protocol aligned.
        catch { AbortConnection(); throw; }
    }

    private async ValueTask<long[]> HashExpiryCommandAsync(byte[][] args, int expected, CancellationToken token)
    {
        AssertHashExpiryStandalone();
        try
        {
            return await SendReceiveAsync(args, async useToken =>
            {
                // Interrupt framework reads that have no effective cancellation overload.
                using var cancellation = CloseSocketOnCancellation(useToken);
                try { return await ReadHashExpiryResultsAsync(expected, useToken).ConfigureAwait(false); }
                catch { useToken.ThrowIfCancellationRequested(); throw; }
            }, token).ConfigureAwait(false);
        }
        catch (RedisResponseException) { throw; }
        catch { AbortConnection(); throw; }
    }

    private static void CheckHashExpiryCount(int prefix, string line, int expected)
    {
        if (prefix != '*' || !int.TryParse(line, NumberStyles.None, CultureInfo.InvariantCulture, out var count)
            || count != expected)
            throw new FormatException("Invalid hash-field expiry result count.");
    }

    private static long ParseHashExpiryInteger(int prefix, string line)
    {
        if (prefix != ':' || !long.TryParse(line, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            throw new FormatException("Invalid hash-field expiry integer response.");
        return value;
    }

    private long[] ReadHashExpiryResults(int expected)
    {
        var prefix = SafeReadByte(nameof(ReadHashExpiryResults));
        if (prefix == -1) throw new IOException("Redis connection closed before a hash-field expiry response.");
        var line = ReadHashExpiryLine();
        if (prefix == '-') throw CreateResponseError(StripErrPrefix(line));
        CheckHashExpiryCount(prefix, line, expected);
        var values = new long[expected];
        for (var i = 0; i < expected; i++)
        {
            prefix = SafeReadByte(nameof(ReadHashExpiryResults));
            if (prefix == -1) throw new IOException("Redis connection closed during a hash-field expiry response.");
            values[i] = ParseHashExpiryInteger(prefix, ReadHashExpiryLine());
        }
        return values;
    }

    private async ValueTask<long[]> ReadHashExpiryResultsAsync(int expected, CancellationToken token)
    {
        var prefix = await SafeReadByteAsync(token).ConfigureAwait(false);
        if (prefix == -1) throw new IOException("Redis connection closed before a hash-field expiry response.");
        var line = await ReadHashExpiryLineAsync(token).ConfigureAwait(false);
        if (prefix == '-') throw CreateResponseError(StripErrPrefix(line));
        CheckHashExpiryCount(prefix, line, expected);
        var values = new long[expected];
        for (var i = 0; i < expected; i++)
        {
            prefix = await SafeReadByteAsync(token).ConfigureAwait(false);
            if (prefix == -1) throw new IOException("Redis connection closed during a hash-field expiry response.");
            values[i] = ParseHashExpiryInteger(prefix, await ReadHashExpiryLineAsync(token).ConfigureAwait(false));
        }
        return values;
    }

    // Require a complete CRLF line. Do not use the legacy EOF reader, which reconnects synchronously.
    private string ReadHashExpiryLine()
    {
        var line = new StringBuilder();
        while (true)
        {
            var value = SafeReadByte(nameof(ReadHashExpiryLine));
            if (value == -1) throw new IOException("Redis connection closed during a hash-field expiry reply.");
            if (value == '\r')
            {
                if (SafeReadByte(nameof(ReadHashExpiryLine)) != '\n') throw new FormatException("Invalid Redis reply terminator.");
                return line.ToString();
            }
            if (value == '\n') throw new FormatException("Invalid Redis reply terminator.");
            line.Append((char)value);
        }
    }

    private async ValueTask<string> ReadHashExpiryLineAsync(CancellationToken token)
    {
        var line = new StringBuilder();
        while (true)
        {
            var value = await SafeReadByteAsync(token).ConfigureAwait(false);
            if (value == -1) throw new IOException("Redis connection closed during a hash-field expiry reply.");
            if (value == '\r')
            {
                if (await SafeReadByteAsync(token).ConfigureAwait(false) != '\n') throw new FormatException("Invalid Redis reply terminator.");
                return line.ToString();
            }
            if (value == '\n') throw new FormatException("Invalid Redis reply terminator.");
            line.Append((char)value);
        }
    }
}
