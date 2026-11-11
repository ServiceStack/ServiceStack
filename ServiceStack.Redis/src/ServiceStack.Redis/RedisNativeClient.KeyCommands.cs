using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Redis;

public partial class RedisNativeClient : IRedisNativeKeyCommands, IRedisNativeKeyCommandsAsync
{
    public byte[] GetEx(string key, RedisGetExOptions options = null) => SendExpectData(GetExArgs(key, options));

    public byte[] GetDel(string key) => SendExpectData(GetDelArgs(key));

    public long Unlink(params string[] keys) => SendExpectLong(UnlinkArgs(keys));

    public ValueTask<byte[]> GetExAsync(string key, RedisGetExOptions options = null, CancellationToken token = default)
        => SendExpectDataAsync(token, GetExArgs(key, options));

    public ValueTask<byte[]> GetDelAsync(string key, CancellationToken token = default)
        => SendExpectDataAsync(token, GetDelArgs(key));

    public ValueTask<long> UnlinkAsync(string[] keys, CancellationToken token = default)
        => SendExpectLongAsync(token, UnlinkArgs(keys));

    public ValueTask<long> UnlinkAsync(string key, CancellationToken token = default)
        => UnlinkAsync([key], token);

    private static byte[][] GetDelArgs(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        return [Commands.GetDel, key.ToUtf8Bytes()];
    }

    private static byte[][] GetExArgs(string key, RedisGetExOptions options)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        byte[] expiry = null;
        long? amount = null;
        var count = 0;
        if (options != null)
        {
            AddOption(options.ExpirySeconds, Commands.Ex);
            AddOption(options.ExpiryMilliseconds, Commands.Px);
            AddOption(options.ExpiryAtSeconds, "EXAT".ToUtf8Bytes());
            AddOption(options.ExpiryAtMilliseconds, "PXAT".ToUtf8Bytes());
            if (options.Persist)
            {
                count++;
                expiry = Commands.Persist;
            }
        }

        if (count > 1)
            throw new ArgumentException("Specify at most one GETEX expiration option.", nameof(options));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "GETEX expiration must be positive.");
        if (expiry == null)
            return [Commands.GetEx, key.ToUtf8Bytes()];
        return amount == null
            ? [Commands.GetEx, key.ToUtf8Bytes(), expiry]
            : [Commands.GetEx, key.ToUtf8Bytes(), expiry, amount.Value.ToUtf8Bytes()];

        void AddOption(long? value, byte[] command)
        {
            if (value == null) return;
            count++;
            amount = value;
            expiry = command;
        }
    }

    private static byte[][] UnlinkArgs(string[] keys)
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));
        if (keys.Length == 0) throw new ArgumentException("UNLINK requires at least one key.", nameof(keys));
        foreach (var key in keys)
            if (key == null)
                throw new ArgumentException("UNLINK keys cannot contain null.", nameof(keys));
        return MergeCommandWithArgs(Commands.Unlink, keys);
    }
}