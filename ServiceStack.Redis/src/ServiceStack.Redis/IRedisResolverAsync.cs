using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis;

/// <summary>Optional asynchronous resolution/role verification for client managers. Existing resolvers remain compatible.</summary>
public interface IRedisResolverAsync
{
    ValueTask<RedisClient> CreateMasterClientAsync(int desiredIndex, CancellationToken token = default,
        Action<RedisClient> configure = null);

    ValueTask<RedisClient> CreateSlaveClientAsync(int desiredIndex, CancellationToken token = default,
        Action<RedisClient> configure = null);
}

public static class RedisResolverAsyncExtensions
{
    public static ValueTask<RedisClient> CreateMasterClientAsync(this IRedisResolver resolver, int desiredIndex,
        CancellationToken token = default, Action<RedisClient> configure = null)
        => CreateAsync(resolver, desiredIndex, true, token, configure);

    public static ValueTask<RedisClient> CreateSlaveClientAsync(this IRedisResolver resolver, int desiredIndex,
        CancellationToken token = default, Action<RedisClient> configure = null)
        => CreateAsync(resolver, desiredIndex, false, token, configure);

    private static async ValueTask<RedisClient> CreateAsync(IRedisResolver resolver, int index, bool master,
        CancellationToken token, Action<RedisClient> configure)
    {
        token.ThrowIfCancellationRequested();
        if (resolver is IRedisResolverAsync asyncResolver)
            return master
                ? await asyncResolver.CreateMasterClientAsync(index, token, configure).ConfigureAwait(false)
                : await asyncResolver.CreateSlaveClientAsync(index, token, configure).ConfigureAwait(false);
        // Custom and Sentinel resolvers keep their existing policy; opt in to IRedisResolverAsync for nonblocking resolution.
        var client = master ? resolver.CreateMasterClient(index) : resolver.CreateSlaveClient(index);
        try
        {
            configure?.Invoke(client);
            token.ThrowIfCancellationRequested();
            return client;
        }
        catch
        {
            client.AbortConnection();
            client.ClientManager = null;
            client.Dispose();
            throw;
        }
    }
}