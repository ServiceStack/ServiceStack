using System;
using System.Reflection;

namespace ServiceStack.OrmLite
{
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
}