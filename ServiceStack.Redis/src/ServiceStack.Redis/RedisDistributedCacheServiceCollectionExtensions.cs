#if NET10_0_OR_GREATER
using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceStack.Redis;

namespace Microsoft.Extensions.DependencyInjection;

public static class RedisDistributedCacheServiceCollectionExtensions
{
    /// <summary>
    /// Use an already registered IRedisClientsManager. Its existing owner retains disposal responsibility.
    /// Does not replace an existing IDistributedCache registration.
    /// </summary>
    public static IServiceCollection AddServiceStackRedisCache(this IServiceCollection services,
        Action<RedisDistributedCacheOptions> configure = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        var options = Configure(configure);
        services.TryAddSingleton<IDistributedCache>(sp =>
            new RedisDistributedCache(sp.GetRequiredService<IRedisClientsManager>(), options));
        return services;
    }

    /// <summary>Create and own one lazy RedisManagerPool for this cache registration.</summary>
    public static IServiceCollection AddServiceStackRedisCache(this IServiceCollection services,
        string connectionString,
        Action<RedisDistributedCacheOptions> configure = null)
    {
        if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
        return services.AddServiceStackRedisCache(_ => new RedisManagerPool(connectionString), configure);
    }

    /// <summary>
    /// Create a dedicated manager lazily and transfer its ownership to the cache. The factory must return a new manager.
    /// To share a manager registered in DI, use the overload without a factory instead.
    /// </summary>
    public static IServiceCollection AddServiceStackRedisCache(this IServiceCollection services,
        Func<IServiceProvider, IRedisClientsManager> createManager,
        Action<RedisDistributedCacheOptions> configure = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (createManager == null) throw new ArgumentNullException(nameof(createManager));
        var options = Configure(configure);
        services.TryAddSingleton<IDistributedCache>(sp =>
        {
            var manager = createManager(sp) ??
                          throw new InvalidOperationException("The Redis manager factory returned null.");
            try
            {
                return new RedisDistributedCache(manager, options, ownsManager: true);
            }
            catch
            {
                manager.Dispose();
                throw;
            }
        });
        return services;
    }

    private static RedisDistributedCacheOptions Configure(Action<RedisDistributedCacheOptions> configure)
    {
        var options = new RedisDistributedCacheOptions();
        configure?.Invoke(options);
        if (string.IsNullOrWhiteSpace(options.KeyPrefix))
            throw new ArgumentException("A nonempty private cache key prefix is required.", nameof(configure));
        // Do not retain a mutable options instance captured by the caller's configure delegate.
        return new RedisDistributedCacheOptions { KeyPrefix = options.KeyPrefix };
    }
}
#endif