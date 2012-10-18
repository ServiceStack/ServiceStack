using System;
using System.Reflection;

namespace ServiceStack.Common.Reflection
{

#if MONOTOUCH || SILVERLIGHT
    public static class StaticAccessors
    {
    }
#else
    using System.Linq.Expressions;
    
    public static class StaticAccessors
    {
        public static Func<object, object> GetValueGetter(Type type, PropertyInfo propertyInfo)
        {
            if (type != propertyInfo.DeclaringType)
            {
                throw new ArgumentException();
            }

            var instance = Expression.Parameter(typeof(object), "i");
            var convertInstance = Expression.TypeAs(instance, propertyInfo.DeclaringType);
            var property = Expression.Property(convertInstance, propertyInfo);
            var convertProperty = Expression.TypeAs(property, typeof(object));
            return Expression.Lambda<Func<object, object>>(convertProperty, instance).Compile();
        }

        public static Func<T, object> GetValueGetter<T>(this PropertyInfo propertyInfo)
        {
            if (typeof(T) != propertyInfo.DeclaringType)
            {
                throw new ArgumentException();
            }

            var instance = Expression.Parameter(propertyInfo.DeclaringType, "i");
            var property = Expression.Property(instance, propertyInfo);
            var convert = Expression.TypeAs(property, typeof(object));
            return Expression.Lambda<Func<T, object>>(convert, instance).Compile();
        }

        public static Action<T, object> GetValueSetter<T>(this PropertyInfo propertyInfo)
        {
            if (typeof(T) != propertyInfo.DeclaringType)
            {
                throw new ArgumentException();
            }

            var instance = Expression.Parameter(propertyInfo.DeclaringType, "i");
            var argument = Expression.Parameter(typeof(object), "a");
            var setterCall = Expression.Call(
                instance,
                propertyInfo.GetSetMethod(),
                Expression.Convert(argument, propertyInfo.PropertyType));

            return Expression.Lambda<Action<T, object>>
                (
                    setterCall, instance, argument
                ).Compile();
        }
    }

#endif

    public static class StaticAccessors<TEntity>
    {
        /// <summary>
        /// Func to get the Strongly-typed field
        /// </summary>
        public static Func<TEntity, TId> TypedGetPropertyFn<TId>(PropertyInfo pi)
        {
            var mi = pi.GetGetMethod();
            return (Func<TEntity, TId>)Delegate.CreateDelegate(typeof(Func<TEntity, TId>), mi);
        }

        /// <summary>
        /// Required to cast the return ValueType to an object for caching
        /// </summary>
        public static Func<TEntity, object> ValueUnTypedGetPropertyFn<TId>(PropertyInfo pi)
        {
            var typedPropertyFn = TypedGetPropertyFn<TId>(pi);
            return x => typedPropertyFn(x);
        }

        public static Func<TEntity, object> ValueUnTypedGetPropertyTypeFn(PropertyInfo pi)
        {
            var mi = typeof(StaticAccessors<TEntity>).GetMethod("TypedGetPropertyFn");
            var genericMi = mi.MakeGenericMethod(pi.PropertyType);
            var typedGetPropertyFn = (Delegate)genericMi.Invoke(null, new[] { pi });

#if MONOTOUCH || SILVERLIGHT
            return x => typedGetPropertyFn.Method.Invoke(x, new object[] { });
#else
            var typedMi = typedGetPropertyFn.Method;
            var paramFunc = Expression.Parameter(typeof(object), "oFunc");
            var expr = Expression.Lambda<Func<TEntity, object>> (
                    Expression.Convert(
                        Expression.Call(
                            Expression.Convert(paramFunc, typedMi.DeclaringType),
                            typedMi
                        ),
                        typeof(object)
                    ),
                    paramFunc
                );
            return expr.Compile();
#endif
        }

        public static Func<object, object> UnTypedGetPropertyFn<TId>(PropertyInfo pi)
        {
            var typedPropertyFn = TypedGetPropertyFn<TId>(pi);
            return x => typedPropertyFn((TEntity)x);
        }

        /// <summary>
        /// Func to set the Strongly-typed field
        /// </summary>
        public static Action<TEntity, TId> TypedSetPropertyFn<TId>(PropertyInfo pi)
        {
            var mi = pi.GetSetMethod();
            return (Action<TEntity, TId>)Delegate.CreateDelegate(typeof(Action<TEntity, TId>), mi);
        }

        /// <summary>
        /// Required to cast the ValueType to an object for caching
        /// </summary>
        public static Action<TEntity, object> ValueUnTypedSetPropertyFn<TId>(PropertyInfo pi)
        {
            var typedPropertyFn = TypedSetPropertyFn<TId>(pi);
            return (x, y) => typedPropertyFn(x, (TId)y);
        }

        public static Action<TEntity, object> ValueUnTypedSetPropertyTypeFn(PropertyInfo pi)
        {
            var mi = typeof(StaticAccessors<TEntity>).GetMethod("TypedSetPropertyFn");
            var genericMi = mi.MakeGenericMethod(pi.PropertyType);
            var typedSetPropertyFn = (Delegate)genericMi.Invoke(null, new[] { pi });

#if MONOTOUCH || SILVERLIGHT
            return (x, y) => typedSetPropertyFn.Method.Invoke(x, new[] { y });
#else
            var typedMi = typedSetPropertyFn.Method;
            var paramFunc = Expression.Parameter(typeof(object), "oFunc");
            var paramValue = Expression.Parameter(typeof(object), "oValue");
            var expr = Expression.Lambda<Action<TEntity, object>>(
                    Expression.Call(
                        Expression.Convert(paramFunc, typedMi.DeclaringType),
                        typedMi,
                        Expression.Convert(paramValue, pi.PropertyType)
                    ),
                    paramFunc,
                    paramValue
                );
            return expr.Compile();
#endif
        }

        /// <summary>
        /// Required to cast the ValueType to an object for caching
        /// </summary>
        public static Action<object, object> UnTypedSetPropertyFn<TId>(PropertyInfo pi)
        {
            var typedPropertyFn = TypedSetPropertyFn<TId>(pi);
            return (x, y) => typedPropertyFn((TEntity)x, (TId)y);
        }
    }
}