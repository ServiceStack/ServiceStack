#if NET10_0_OR_GREATER
namespace ServiceStack.Redis;

/// <summary>Options copied when the adapter is constructed.</summary>
public class RedisDistributedCacheOptions
{
    /// <summary>
    /// Private namespace for version-1 hash entries. Use a distinct prefix per application/cache.
    /// Existing ServiceStack cache keys and other Redis providers' layouts are not shared.
    /// </summary>
    public string KeyPrefix { get; set; } = "ss:cache:v1:";
}
#endif
