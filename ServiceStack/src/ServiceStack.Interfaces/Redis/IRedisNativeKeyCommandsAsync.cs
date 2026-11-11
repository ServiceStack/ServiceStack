using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis;

/// <summary>Async counterpart of IRedisNativeKeyCommands; use a writer connection.</summary>
public interface IRedisNativeKeyCommandsAsync
{
    ValueTask<byte[]> GetExAsync(string key, RedisGetExOptions options = null, CancellationToken token = default);
    ValueTask<byte[]> GetDelAsync(string key, CancellationToken token = default);
    ValueTask<long> UnlinkAsync(string[] keys, CancellationToken token = default);
}
