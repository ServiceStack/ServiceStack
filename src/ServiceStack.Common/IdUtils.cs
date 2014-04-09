using System;
using System.Linq;
using System.Reflection;
using ServiceStack.Model;
using ServiceStack.Reflection;
using ServiceStack.Text;

namespace ServiceStack
{
    public static class IdUtils<T>
    {
        internal static Func<T, object> CanGetId;

        static IdUtils()
        {

#if !SL5 && !IOS && !XBOX
            var hasIdInterfaces = typeof(T).FindInterfaces(
                (t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IHasId<>), null);

            if (hasIdInterfaces.Length > 0)
            {
                CanGetId = HasId<T>.GetId;
                return;
            }
#endif

            if (typeof(T).IsClass() || typeof(T).IsInterface)
            {
                foreach (var pi in typeof(T).GetPublicProperties()
                    .Where(pi => pi.AllAttributes<Attribute>()
                             .Any(attr => attr.GetType().Name == "PrimaryKeyAttribute")))
                {
                    CanGetId = StaticAccessors<T>.ValueUnTypedGetPropertyTypeFn(pi);
                    return;
                }

                var piId = typeof(T).GetIdProperty();
                if (piId != null
                    && piId.GetMethodInfo() != null)
                {
                    CanGetId = HasPropertyId<T>.GetId;
                    return;
                }
            }

            if (typeof(T) == typeof(object))
            {
                CanGetId = x => {
                    var piId = x.GetType().GetIdProperty();
                    if (piId != null && piId.GetMethodInfo() != null)
                        return x.GetObjectId();

                    return x.GetHashCode();
                };
                return;
            }

            CanGetId = x => x.GetHashCode();
        }

        public static object GetId(T entity)
        {
            return CanGetId(entity);
        }
    }

    internal static class HasPropertyId<TEntity>
    {
        private static readonly Func<TEntity, object> GetIdFn;

        static HasPropertyId()
        {
            var pi = typeof(TEntity).GetIdProperty();
            GetIdFn = StaticAccessors<TEntity>.ValueUnTypedGetPropertyTypeFn(pi);
        }

        public static object GetId(TEntity entity)
        {
            return GetIdFn(entity);
        }
    }

    internal static class HasId<TEntity>
    {
        private static readonly Func<TEntity, object> GetIdFn;

        static HasId()
        {

#if IOS || SL5
            GetIdFn = HasPropertyId<TEntity>.GetId;
#else
            var hasIdInterfaces = typeof(TEntity).FindInterfaces(
                (t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IHasId<>), null);

            var genericArg = hasIdInterfaces[0].GetGenericArguments()[0];
            var genericType = typeof(HasIdGetter<,>).MakeGenericType(typeof(TEntity), genericArg);

            var oInstanceParam = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "oInstanceParam");
            var exprCallStaticMethod = System.Linq.Expressions.Expression.Call
                (
                    genericType,
                    "GetId",
                    new Type[0],
                    oInstanceParam
                );
            GetIdFn = System.Linq.Expressions.Expression.Lambda<Func<TEntity, object>>
                (
                    exprCallStaticMethod,
                    oInstanceParam
                ).Compile();
#endif
        }

        public static object GetId(TEntity entity)
        {
            return GetIdFn(entity);
        }
    }

    internal class HasIdGetter<TEntity, TId>
        where TEntity : IHasId<TId>
    {
        public static object GetId(TEntity entity)
        {
            return entity.Id;
        }
    }

    public static class IdUtils
    {
        public const string IdField = "Id";

        public static object GetObjectId(this object entity)
        {
            return entity.GetType().GetIdProperty().GetMethodInfo().Invoke(entity, new object[0]);
        }

        public static object ToId<T>(this T entity)
        {
            return entity.GetId();
        }

        public static string ToUrn<T>(this T entity)
        {
            return entity.CreateUrn();
        }

        public static string ToSafePathCacheKey<T>(this string idValue)
        {
            return IdUtils.CreateCacheKeyPath<T>(idValue);
        }

        public static string ToUrn<T>(this object id)
        {
            return IdUtils.CreateUrn<T>(id);
        }

        public static object GetId<T>(this T entity)
        {
            return IdUtils<T>.GetId(entity);
        }

        public static string CreateUrn<T>(object id)
        {
            return string.Format("urn:{0}:{1}", typeof(T).Name.ToLowerInvariant(), id);
        }

        public static string CreateUrn(Type type, object id)
        {
            return string.Format("urn:{0}:{1}", type.Name.ToLowerInvariant(), id);
        }

        public static string CreateUrn<T>(this T entity)
        {
            var id = GetId(entity);
            return string.Format("urn:{0}:{1}", typeof(T).Name.ToLowerInvariant(), id);
        }

        public static string CreateCacheKeyPath<T>(string idValue)
        {
            if (idValue.Length < 4)
            {
                idValue = idValue.PadLeft(4, '0');
            }
            idValue = idValue.Replace(" ", "-");

            var rootDir = typeof(T).Name;
            var dir1 = idValue.Substring(0, 2);
            var dir2 = idValue.Substring(2, 2);

            var path = string.Format("{1}{0}{2}{0}{3}{0}{4}", PclExport.Instance.DirSep,
                rootDir, dir1, dir2, idValue);

            return path;
        }

        public static PropertyInfo GetIdProperty(this Type type)
        {
            foreach (var pi in type.GetPropertyInfos())
            {
                if (string.Equals(IdField, pi.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return pi;
                }
            }
            return null;
        }

    }

}