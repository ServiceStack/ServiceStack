using System;
using System.Reflection;

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
		public PropertyAccessor(string propertyName)
		{
			this.pi = typeof(TEntity).GetProperty(propertyName);
		}

		public Func<TEntity, object> GetPropertyFn()
		{
			return StaticAccessors<TEntity>.ValueUnTypedGetPropertyTypeFn(pi);
		}

		public Action<TEntity, object> SetPropertyFn()
		{
			return StaticAccessors<TEntity>.ValueUnTypedSetPropertyTypeFn(pi);
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