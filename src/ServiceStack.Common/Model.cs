using ServiceStack.Common.Utils;

namespace ServiceStack
{
    public static class Model
    {
        public static object ToId<T>(this T entity)
        {
            return entity.GetId();
        }

        public static string ToUrn<T>(object id)
        {
            return IdUtils.CreateUrn<T>(id);
        }

        public static string ToUrn<T>(this T entity)
        {
            return entity.CreateUrn();
        }

        public static string ToSafePathCacheKey<T>(string idValue)
        {
            return IdUtils.CreateCacheKeyPath<T>(idValue);
        }
    }
}