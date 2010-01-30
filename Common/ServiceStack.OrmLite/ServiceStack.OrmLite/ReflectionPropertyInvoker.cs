//
// ServiceStack.OrmLite: Light-weight POCO ORM for .NET and Mono
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

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

			var propertySetMethod = propertyInfo.GetSetMethod();
			if (propertySetMethod == null) return;

			propertySetMethod.Invoke(onInstance, new[] { convertedValue });
		}

		public object GetPropertyValue(PropertyInfo propertyInfo, object fromInstance)
		{
			var value = propertyInfo.GetGetMethod().Invoke(fromInstance, new object[] { });
			return value;
		}
	}
}