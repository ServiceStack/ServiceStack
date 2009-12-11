using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ServiceStack.OrmLite
{
	//TODO: use cached delegates, keep reflection around as it works everywhere
	public interface IPropertyInvoker
	{
		Func<object, Type, object> ConvertValueFn { get; set; }

		void SetPropertyValue(PropertyInfo propertyInfo, Type fieldType, object onInstance, object withValue);

		object GetPropertyValue(PropertyInfo propertyInfo, object fromInstance);
	}

	public class ReflectionPropertyInvoker
		: IPropertyInvoker
	{
		public static readonly ReflectionPropertyInvoker Instance = new ReflectionPropertyInvoker();

		public Func<object, Type, object> ConvertValueFn { get; set; }

		public void SetPropertyValue(PropertyInfo propertyInfo, Type fieldType, object onInstance, object withValue)
		{
			var convertedValue = ConvertValueFn(withValue, fieldType);
			propertyInfo.GetSetMethod().Invoke(onInstance, new[] { convertedValue });
		}

		public object GetPropertyValue(PropertyInfo propertyInfo, object fromInstance)
		{
			var value = propertyInfo.GetGetMethod().Invoke(fromInstance, new object[] { });
			return value;
		}
	}

	public class ExpressionPropertyInvoker
		: IPropertyInvoker
	{
		public static readonly ExpressionPropertyInvoker Instance = new ExpressionPropertyInvoker();

		public Func<object, Type, object> ConvertValueFn { get; set; }

		private readonly Dictionary<PropertyInfo, Action<object, object>> propertySetFnMap = new Dictionary<PropertyInfo, Action<object, object>>();
		private readonly Dictionary<PropertyInfo, Func<object, object>> propertyGetFnMap = new Dictionary<PropertyInfo, Func<object, object>>();

		public void SetPropertyValue(PropertyInfo propertyInfo, Type fieldType, object onInstance, object withValue)
		{
			var convertedValue = ConvertValueFn(withValue, fieldType);

			Action<object, object> propertySetFn;
			if (!propertySetFnMap.TryGetValue(propertyInfo, out propertySetFn))
			{
				var setMethodInfo = propertyInfo.GetSetMethod();
				var oInstanceParam = Expression.Parameter(typeof(object), "oInstanceParam");
				var oValueParam = Expression.Parameter(typeof(object), "oValueParam");

				var instanceParam = Expression.Convert(oInstanceParam, onInstance.GetType());
				var useType = convertedValue != null ? convertedValue.GetType() : propertyInfo.PropertyType;
				var valueParam = Expression.Convert(oValueParam, useType);
				var exprCallPropertySetFn = Expression.Call(instanceParam, setMethodInfo, valueParam);

				propertySetFn = Expression.Lambda<Action<object, object>>
				(
					exprCallPropertySetFn,
					oInstanceParam,
					oValueParam
				).Compile();

				propertySetFnMap[propertyInfo] = propertySetFn;
			}

			propertySetFn(onInstance, convertedValue);
		}

		public object GetPropertyValue(PropertyInfo propertyInfo, object fromInstance)
		{
			Func<object, object> propertyGetFn;
			if (!propertyGetFnMap.TryGetValue(propertyInfo, out propertyGetFn))
			{
				var getMethodInfo = propertyInfo.GetGetMethod();
				var oInstanceParam = Expression.Parameter(typeof(object), "oInstanceParam");
				var instanceParam = Expression.Convert(oInstanceParam, fromInstance.GetType());

				var exprCallPropertyGetFn = Expression.Call(instanceParam, getMethodInfo);
				var oExprCallPropertyGetFn = Expression.Convert(exprCallPropertyGetFn, typeof(object));

				propertyGetFn = Expression.Lambda<Func<object, object>>
				(
					oExprCallPropertyGetFn,
					oInstanceParam
				).Compile();

				propertyGetFnMap[propertyInfo] = propertyGetFn;
			}
			return propertyGetFn(fromInstance);
		}
	}

}