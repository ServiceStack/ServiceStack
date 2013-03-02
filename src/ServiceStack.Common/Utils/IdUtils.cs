using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using ServiceStack.Common.Reflection;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Text;

namespace ServiceStack.Common.Utils
{
    public static class IdUtils<T>
    {
        internal static Func<T, object> CanGetId;

        static IdUtils()
        {

#if !SILVERLIGHT && !MONOTOUCH && !XBOX
            var hasIdInterfaces = typeof(T).FindInterfaces(
                (t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IHasId<>), null);

            if (hasIdInterfaces.Length > 0)
            {
                CanGetId = HasId<T>.GetId;
                return;
            }
#endif

            if (typeof(T).IsClass())
            {
                if (typeof(T).GetPropertyInfo(IdUtils.IdField) != null
                    && typeof(T).GetPropertyInfo(IdUtils.IdField).GetMethodInfo() != null)
                {
                    CanGetId = HasPropertyId<T>.GetId;
                    return;
                }

                foreach (var pi in typeof(T).GetPublicProperties()
                    .Where(pi => pi.CustomAttributes()
                             .Cast<Attribute>()
                             .Any(attr => attr.GetType().Name == "PrimaryKeyAttribute")))
                {
                    CanGetId = StaticAccessors<T>.ValueUnTypedGetPropertyTypeFn(pi);
                    return;
                }
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
            var pi = typeof(TEntity).GetPropertyInfo(IdUtils.IdField);
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

#if MONOTOUCH || SILVERLIGHT
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
            return entity.GetType().GetPropertyInfo(IdField).GetMethodInfo().Invoke(entity, new object[0]);
        }

        public static object GetId<T>(this T entity)
        {
            return IdUtils<T>.GetId(entity);
        }

        public static string CreateUrn<T>(object id)
        {
            return string.Format("urn:{0}:{1}", typeof(T).Name.ToLower(), id);
        }

        public static string CreateUrn(Type type, object id)
        {
            return string.Format("urn:{0}:{1}", type.Name.ToLower(), id);
        }

        public static string CreateUrn<T>(this T entity)
        {
            var id = GetId(entity);
            return string.Format("urn:{0}:{1}", typeof(T).Name.ToLower(), id);
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

            var path = string.Format("{1}{0}{2}{0}{3}{0}{4}", Text.StringExtensions.DirSeparatorChar,
                rootDir, dir1, dir2, idValue);

            return path;
        }

    }

}