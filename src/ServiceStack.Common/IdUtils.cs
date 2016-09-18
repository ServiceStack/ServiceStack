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
#if NETSTANDARD1_3
            var hasIdInterfaces = typeof(T).GetTypeInfo().ImplementedInterfaces.Where(t => t.GetTypeInfo().IsGenericType 
                && t.GetTypeInfo().GetGenericTypeDefinition() == typeof(IHasId<>)).ToArray();
#else
            var hasIdInterfaces = typeof(T).FindInterfaces(
                (t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IHasId<>), null);
#endif
            if (hasIdInterfaces.Length > 0)
            {
                CanGetId = HasId<T>.GetId;
                return;
            }
#endif

            if (typeof(T).IsClass() || typeof(T).IsInterface())
            {
                foreach (var pi in typeof(T).GetPublicProperties()
                    .Where(pi => pi.AllAttributes<Attribute>()
                             .Any(attr => attr.GetType().Name == "PrimaryKeyAttribute")))
                {
                    CanGetId = StaticAccessors<T>.ValueUnTypedGetPropertyTypeFn(pi);
                    return;
                }

                var piId = typeof(T).GetIdProperty();
                if (piId?.GetMethodInfo() != null)
                {
                    CanGetId = HasPropertyId<T>.GetId;
                    return;
                }
            }

            if (typeof(T) == typeof(object))
            {
                CanGetId = x =>
                {
                    var piId = x.GetType().GetIdProperty();
                    if (piId?.GetMethodInfo() != null)
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
#if NETSTANDARD1_3
            var hasIdInterfaces = typeof(TEntity).GetTypeInfo().ImplementedInterfaces.Where(t => t.GetTypeInfo().IsGenericType 
                && t.GetTypeInfo().GetGenericTypeDefinition() == typeof(IHasId<>)).ToArray();
#else
            var hasIdInterfaces = typeof(TEntity).FindInterfaces(
                (t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IHasId<>), null);
#endif
            var genericArg = hasIdInterfaces[0].GetGenericArguments()[0];
            var genericType = typeof(HasIdGetter<,>).MakeGenericType(typeof(TEntity), genericArg);

            var oInstanceParam = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "oInstanceParam");
            var exprCallStaticMethod = System.Linq.Expressions.Expression.Call
                (
                    genericType,
                    "GetId",
                    TypeConstants.EmptyTypeArray,
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
            return entity.GetType().GetIdProperty().GetMethodInfo().Invoke(entity, TypeConstants.EmptyObjectArray);
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
            return CreateCacheKeyPath<T>(idValue);
        }

        public static string ToUrn<T>(this object id)
        {
            return CreateUrn<T>(id);
        }

        public static object GetId<T>(this T entity)
        {
            return IdUtils<T>.GetId(entity);
        }

        public static string CreateUrn<T>(object id)
        {
            return $"urn:{typeof(T).Name.ToLowerInvariant()}:{id}";
        }

        public static string CreateUrn(Type type, object id)
        {
            return $"urn:{type.Name.ToLowerInvariant()}:{id}";
        }

        public static string CreateUrn<T>(this T entity)
        {
            var id = GetId(entity);
            return $"urn:{typeof(T).Name.ToLowerInvariant()}:{id}";
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

            var path = $"{rootDir}{PclExport.Instance.DirSep}{dir1}{PclExport.Instance.DirSep}{dir2}{PclExport.Instance.DirSep}{idValue}";

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