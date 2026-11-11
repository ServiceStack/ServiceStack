using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis;

public partial class RedisResolver : IRedisResolverAsync
{
    public virtual ValueTask<RedisClient> CreateMasterClientAsync(int desiredIndex, CancellationToken token = default,
        Action<RedisClient> configure = null)
        => CreateRedisClientAsync(GetReadWriteHost(desiredIndex), true, token, configure);

    public virtual ValueTask<RedisClient> CreateSlaveClientAsync(int desiredIndex, CancellationToken token = default,
        Action<RedisClient> configure = null)
        => CreateRedisClientAsync(GetReadOnlyHost(desiredIndex), false, token, configure);

    public virtual async ValueTask<RedisClient> CreateRedisClientAsync(RedisEndpoint config, bool master,
        CancellationToken token = default, Action<RedisClient> configure = null)
    {
        if (HasLegacyFactoryOverride())
            return await ConfigureLegacyClientAsync(() => CreateRedisClient(config, master), token, configure).ConfigureAwait(false);
        var started = Stopwatch.StartNew();
        Exception firstError = null;
        var attempt = 0;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            RedisClient client = null;
            var verifying = false;
            try
            {
                client = ClientFactory(config);
                configure?.Invoke(client);
                token.ThrowIfCancellationRequested();
                if (master && RedisConfig.VerifyMasterConnections)
                {
                    verifying = true;
                    client = await GetValidMasterAsync(client, config, token, configure).ConfigureAwait(false);
                }

                token.ThrowIfCancellationRequested();
                var result = client;
                client = null;
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                throw;
            } // Do not restart a connection setup deadline.
            catch (Exception ex)
            {
                token.ThrowIfCancellationRequested();
                if (!verifying || !RedisConfig.RetryReconnectOnFailedMasters) throw;
                firstError ??= ex;
            }
            finally
            {
                CloseLookupClient(client);
            }

            var remaining = config.RetryTimeout - started.ElapsedMilliseconds;
            if (remaining <= 0)
                throw new TimeoutException(
                    $"Could not resolve master instance within {config.RetryTimeout}ms RetryTimeout", firstError);
            await Task.Delay((int)Math.Min(remaining, RedisNativeClient.GetBackOffMultiplier(++attempt)), token)
                .ConfigureAwait(false);
        }
    }

    // Resolved once per resolver type rather than by reflection on every client creation.
    private static readonly ConcurrentDictionary<Type, bool> legacyFactoryOverrides = new();

    private bool HasLegacyFactoryOverride() => legacyFactoryOverrides.GetOrAdd(GetType(), static type =>
    {
        var implementation = type.GetMethod(nameof(CreateRedisClient), [typeof(RedisEndpoint), typeof(bool)]);
        return implementation?.DeclaringType != typeof(RedisResolver)
            && implementation?.GetBaseDefinition().DeclaringType == typeof(RedisResolver);
    });

    private static ValueTask<RedisClient> ConfigureLegacyClientAsync(Func<RedisClient> create,
        CancellationToken token, Action<RedisClient> configure)
    {
        token.ThrowIfCancellationRequested();
        var client = create(); // Preserve existing resolver policy, including its synchronous execution.
        try
        {
            configure?.Invoke(client);
            token.ThrowIfCancellationRequested();
            return new ValueTask<RedisClient>(client);
        }
        catch { CloseLookupClient(client); throw; }
    }

    private async ValueTask<RedisClient> GetValidMasterAsync(RedisClient client, RedisEndpoint config,
        CancellationToken token, Action<RedisClient> configure)
    {
        var role = await VerifyMasterRoleAsync(client, token).ConfigureAwait(false);
        if (role == RedisServerRole.Master) return client;
        Interlocked.Increment(ref RedisState.TotalInvalidMasters);
        log.Error($"Redis Master Host '{config.GetHostString()}' is {role}. Resetting allHosts...");
        var newMasters = new List<RedisEndpoint>();
        var newReplicas = new List<RedisEndpoint>();
        RedisClient masterClient = null;
        try
        {
            foreach (var host in allHosts.ToArray())
            {
                token.ThrowIfCancellationRequested();
                RedisClient probe = null;
                try
                {
                    probe = ClientFactory(host);
                    configure?.Invoke(probe);
                    // After configure: manager settings must not replace the shorter host-lookup timeout.
                    probe.ConnectTimeout = RedisConfig.HostLookupTimeoutMs;
                    var probeRole = await VerifyMasterRoleAsync(probe, token).ConfigureAwait(false);
                    if (probeRole == RedisServerRole.Master)
                    {
                        newMasters.Add(host);
                        if (masterClient == null)
                        {
                            masterClient = probe;
                            probe = null;
                        }
                    }
                    else if (probeRole == RedisServerRole.Slave) newReplicas.Add(host);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    token.ThrowIfCancellationRequested();
                }
                finally
                {
                    CloseLookupClient(probe);
                }
            }

            token.ThrowIfCancellationRequested();
            if (masterClient == null)
            {
                Interlocked.Increment(ref RedisState.TotalNoMastersFound);
                throw new InvalidDataException("No master found in: " +
                                               string.Join(", ", allHosts.Select(x => x.GetHostString())));
            }

            ResetMasters(newMasters);
            ResetSlaves(newReplicas);
            CloseLookupClient(client);
            var result = masterClient;
            masterClient = null;
            return result;
        }
        finally
        {
            CloseLookupClient(masterClient);
        }
    }

    private static async ValueTask<RedisServerRole> VerifyMasterRoleAsync(RedisClient client, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (client.ConnectTimeout > 0) deadline.CancelAfter(client.ConnectTimeout);
        try
        {
            await client.ConnectAsync(deadline.Token).ConfigureAwait(false);
            using var cancellation = client.CloseSocketOnCancellation(deadline.Token);
            return await ((IRedisClientAsync)client).GetServerRoleAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (deadline.IsCancellationRequested)
        {
            token.ThrowIfCancellationRequested();
            throw new TimeoutException(
                $"Redis connection/role verification to '{client.GetHostString()}' exceeded ConnectTimeout {client.ConnectTimeout}ms",
                ex);
        }
    }

    private static void CloseLookupClient(RedisClient client)
    {
        if (client == null) return;
        client.AbortConnection();
        client.ClientManager = null;
        client.Dispose();
    }
}