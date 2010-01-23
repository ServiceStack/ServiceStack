using System;
using System.Reflection;

namespace ServiceStack.OrmLite
{
	public interface IPropertyInvoker
	{
		Func<object, Type, object> ConvertValueFn { get; set; }

		void SetPropertyValue(PropertyInfo propertyInfo, Type fieldType, object onInstance, object withValue);

		object GetPropertyValue(PropertyInfo propertyInfo, object fromInstance);
	}
}