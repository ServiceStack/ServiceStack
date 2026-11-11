using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Redis;

public partial class RedisClient
{
    // Older Redis servers have no SET PXAT. The fallback must keep the condition,
    // value and absolute deadline in one atomic operation as well.
    private const string SetAbsoluteExpiryScript =
        "local result; " +
        "if ARGV[3] == '' then result = redis.call('SET', KEYS[1], ARGV[1]); " +
        "else result = redis.call('SET', KEYS[1], ARGV[1], ARGV[3]); end; " +
        "if not result then return 0; end; " +
        "redis.call('PEXPIREAT', KEYS[1], ARGV[2]); return 1;";

    private bool useLegacyAbsoluteExpiry;

    private byte[][] AbsoluteExpiryArgs(string key, byte[] value, DateTime expiresAt, bool? exists, out long deadline)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (value.Length > OneGb)
            throw new ArgumentException("value exceeds 1G", nameof(value));
        deadline = ConvertToServerDate(expiresAt).ToUnixTimeMs();
        var args = new List<byte[]>
            { Commands.Set, key.ToUtf8Bytes(), value, "PXAT".ToUtf8Bytes(), deadline.ToUtf8Bytes() };
        if (exists != null)
            args.Add(exists.Value ? Commands.Xx : Commands.Nx);
        return args.ToArray();
    }

    private static byte[][] LegacyAbsoluteExpiryArgs(string key, byte[] value, long deadline, bool? exists) =>
        new[]
        {
            key.ToUtf8Bytes(), value, deadline.ToUtf8Bytes(),
            (exists == null ? "" : exists.Value ? "XX" : "NX").ToUtf8Bytes()
        };

    private bool SetCacheValueWithAbsoluteExpiry(string key, byte[] value, DateTime expiresAt, bool? exists = null)
    {
        var args = AbsoluteExpiryArgs(key, value, expiresAt, exists, out var deadline);
        if (!useLegacyAbsoluteExpiry && deadline > 0)
        {
            try
            {
                return SendExpectString(args) == "OK";
            }
            catch (RedisResponseException e) when (e.Message.StartsWith("syntax error",
                                                       StringComparison.OrdinalIgnoreCase))
            {
                // A syntax error rejects SET without writing; do not fall back on
                // network failures, ACL errors or other possibly uncertain outcomes.
                useLegacyAbsoluteExpiry = true;
            }
        }

        return base.EvalInt(SetAbsoluteExpiryScript, 1, LegacyAbsoluteExpiryArgs(key, value, deadline, exists)) == 1;
    }

    private async ValueTask<bool> SetCacheValueWithAbsoluteExpiryAsync(string key, byte[] value,
        DateTime expiresAt, bool? exists, CancellationToken token)
    {
        var args = AbsoluteExpiryArgs(key, value, expiresAt, exists, out var deadline);
        if (!useLegacyAbsoluteExpiry && deadline > 0)
        {
            try
            {
                return await SendExpectStringAsync(token, args).ConfigureAwait(false) == "OK";
            }
            catch (RedisResponseException e) when (e.Message.StartsWith("syntax error",
                                                       StringComparison.OrdinalIgnoreCase))
            {
                useLegacyAbsoluteExpiry = true;
            }
        }

        return await NativeAsync.EvalIntAsync(SetAbsoluteExpiryScript, 1,
            LegacyAbsoluteExpiryArgs(key, value, deadline, exists), token).ConfigureAwait(false) == 1;
    }
}