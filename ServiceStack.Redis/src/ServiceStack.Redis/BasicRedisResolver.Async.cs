using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis;

public partial class BasicRedisResolver : IRedisResolverAsync
{
    public ValueTask<RedisClient> CreateMasterClientAsync(int desiredIndex, CancellationToken token = default,
        Action<RedisClient> configure = null)
        => CreateClientAsync(GetReadWriteHost(desiredIndex), token, configure);

    public ValueTask<RedisClient> CreateSlaveClientAsync(int desiredIndex, CancellationToken token = default,
        Action<RedisClient> configure = null)
        => CreateClientAsync(GetReadOnlyHost(desiredIndex), token, configure);

    private ValueTask<RedisClient> CreateClientAsync(RedisEndpoint endpoint, CancellationToken token,
        Action<RedisClient> configure)
    {
        token.ThrowIfCancellationRequested();
        var client = ClientFactory(endpoint);
        try
        {
            configure?.Invoke(client);
            token.ThrowIfCancellationRequested();
            return new ValueTask<RedisClient>(client);
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