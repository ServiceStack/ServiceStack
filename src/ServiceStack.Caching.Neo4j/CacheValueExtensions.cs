using ServiceStack.Text;

namespace ServiceStack.Caching.Neo4j
{
    internal static class CacheValueExtensions
    {
        public static string Serialize<T>(this T value)
        {
            return new JsvStringSerializer().SerializeToString(value);
        }

        public static T Deserialize<T>(this ICacheEntry cacheEntry)
        {
            return cacheEntry?.Data == null 
                ? default 
                : new JsvStringSerializer().DeserializeFromString<T>(cacheEntry.Data);
        }
    }
}