using System;
using System.Reflection;

namespace ServiceStack.OrmLite
{
	public class PropertyInvoker : IPropertyInvoker
	{
#if !NO_EXPRESSIONS
		public Func<object, Type, object> ConvertValueFn
		{
			get { return ExpressionPropertyInvoker.Instance.ConvertValueFn; }
			set { ExpressionPropertyInvoker.Instance.ConvertValueFn = value; }
		}

		public void SetPropertyValue(PropertyInfo propertyInfo, Type fieldType, object onInstance, object withValue)
		{
			ExpressionPropertyInvoker.Instance.SetPropertyValue(propertyInfo, fieldType, onInstance, withValue);
		}

		public object GetPropertyValue(PropertyInfo propertyInfo, object fromInstance)
		{
			return ExpressionPropertyInvoker.Instance.GetPropertyValue(propertyInfo, fromInstance);
		}
#else
		public Func<object, Type, object> ConvertValueFn
		{
			get { return ReflectionPropertyInvoker.Instance.ConvertValueFn; }
			set { ReflectionPropertyInvoker.Instance.ConvertValueFn = value; }
		}

		public void SetPropertyValue(PropertyInfo propertyInfo, Type fieldType, object onInstance, object withValue)
		{
			ReflectionPropertyInvoker.Instance.SetPropertyValue(propertyInfo, fieldType, onInstance, withValue);
		}

		public object GetPropertyValue(PropertyInfo propertyInfo, object fromInstance)
		{
			return ReflectionPropertyInvoker.Instance.GetPropertyValue(propertyInfo, fromInstance);
		}
#endif
	}
}