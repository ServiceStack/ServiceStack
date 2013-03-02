using System;
using System.Reflection;
using ServiceStack.Text;

namespace ServiceStack.Common.Reflection
{

    public static class PropertyAccessor
    {
        public static Func<TEntity, object> GetPropertyFn<TEntity>(string propertyName)
        {
            return new PropertyAccessor<TEntity>(propertyName).GetPropertyFn();
        }

        //public static Func<object, object> GetPropertyFnByType(Type type, string propertyName)
        //{
        //    var mi = typeof(PropertyAccessor).GetMethod("GetPropertyFn");
        //    var genericMi = mi.MakeGenericMethod(type);
        //    var getPropertyFn = genericMi.Invoke(null, new object[] { propertyName });

        //    return (Func<object, object>)getPropertyFn;
        //}

        public static Action<TEntity, object> SetPropertyFn<TEntity>(string propertyName)
        {
            return new PropertyAccessor<TEntity>(propertyName).SetPropertyFn();
        }

        //public static Action<object, object> SetPropertyFnByType(Type type, string propertyName)
        //{
        //    var mi = typeof(PropertyAccessor).GetMethod("SetPropertyFn");
        //    var genericMi = mi.MakeGenericMethod(type);
        //    var setPropertyFn = genericMi.Invoke(null, new object[] { propertyName });

        //    return (Action<object, object>)setPropertyFn;
        //}
    }

    public class PropertyAccessor<TEntity>
    {
        readonly PropertyInfo pi;
        public string Name { get; set; }
        public Type PropertyType { get; set; }

        private readonly Func<TEntity, object> getPropertyFn;
        private readonly Action<TEntity, object> setPropertyFn;

        public PropertyAccessor(string propertyName)
        {
            this.pi = typeof(TEntity).GetPropertyInfo(propertyName);
            this.Name = propertyName;
            this.PropertyType = pi.PropertyType;

            getPropertyFn = StaticAccessors<TEntity>.ValueUnTypedGetPropertyTypeFn(pi);
            setPropertyFn = StaticAccessors<TEntity>.ValueUnTypedSetPropertyTypeFn(pi);
        }

        public Func<TEntity, object> GetPropertyFn()
        {
            return getPropertyFn;
        }

        public Action<TEntity, object> SetPropertyFn()
        {
            return setPropertyFn;
        }

        /// <summary>
        /// Func to get the Strongly-typed field
        /// </summary>
        public Func<TEntity, TId> TypedGetPropertyFn<TId>()
        {
            return StaticAccessors<TEntity>.TypedGetPropertyFn<TId>(pi);
        }

        /// <summary>
        /// Required to cast the return ValueType to an object for caching
        /// </summary>
        public Func<TEntity, object> ValueTypedGetPropertyFn<TId>()
        {
            return StaticAccessors<TEntity>.ValueUnTypedGetPropertyFn<TId>(pi);
        }

        public Func<object, object> UnTypedGetPropertyFn<TId>()
        {
            return StaticAccessors<TEntity>.UnTypedGetPropertyFn<TId>(pi);
        }

        /// <summary>
        /// Func to set the Strongly-typed field
        /// </summary>
        public Action<TEntity, TId> TypedSetPropertyFn<TId>()
        {
            return StaticAccessors<TEntity>.TypedSetPropertyFn<TId>(pi);
        }

        /// <summary>
        /// Required to cast the ValueType to an object for caching
        /// </summary>
        public Action<TEntity, object> ValueTypesSetPropertyFn<TId>()
        {
            return StaticAccessors<TEntity>.ValueUnTypedSetPropertyFn<TId>(pi);
        }

        /// <summary>
        /// Required to cast the ValueType to an object for caching
        /// </summary>
        public Action<object, object> UnTypedSetPropertyFn<TId>()
        {
            return StaticAccessors<TEntity>.UnTypedSetPropertyFn<TId>(pi);
        }
    }
}