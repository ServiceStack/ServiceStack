using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text
{
	public class TypeToStringMethods
	{
		public static string ToString(object value)
		{
			if (value == null) return null;
			var toStringFn = GetToStringMethod(value.GetType());
			return toStringFn(value);
		}

		public static Func<object, string> GetToStringMethod(Type type)
		{
			if (!type.IsClass) return null;

			var propertyInfos = type.GetProperties();
			if (propertyInfos.Length == 0) return null;

			var propertyInfosLength = propertyInfos.Length;
			var propertyNames = new string[propertyInfos.Length];

			var getterFns = new Func<object, object>[propertyInfosLength];
			var toStringFns = new Func<object, string>[propertyInfosLength];

			for (var i = 0; i<propertyInfosLength; i++)
			{
				var propertyInfo = propertyInfos[i];
				propertyNames[i] = propertyInfo.Name;

				getterFns[i] = GetPropertyValueMethod(type, propertyInfo);
				toStringFns[i] = ToStringMethods.GetToStringMethod(propertyInfo.PropertyType);
			}

			return value => TypeToString(value, propertyNames, getterFns, toStringFns);
		}

		public static string TypeToString(object value, string[] propertyNames,
			Func<object, object>[] getterFns, Func<object, string>[] toStringFns)
		{
			var sb = new StringBuilder();

			var propertyNamesLength = propertyNames.Length;
			for (var i = 0; i < propertyNamesLength; i++)
			{
				var propertyName = propertyNames[i];

				var propertyValue = getterFns[i](value);
				if (propertyValue == null) continue;

				if (sb.Length > 0) sb.Append(TextExtensions.PropertyItemSeperator);

				var propertyValueString = toStringFns[i](propertyValue);

				sb.Append(propertyName)
					.Append(TextExtensions.PropertyNameSeperator)
					.Append(propertyValueString);
			}
			sb.Insert(0, TextExtensions.TypeStartChar);
			sb.Append(TextExtensions.TypeEndChar);

			return sb.ToString();
		}

		public static Func<object, object> GetPropertyValueMethod(Type type, PropertyInfo propertyInfo)
		{
			var getMethodInfo = propertyInfo.GetGetMethod();
			var oInstanceParam = Expression.Parameter(typeof(object), "oInstanceParam");
			var instanceParam = Expression.Convert(oInstanceParam, type);

			var exprCallPropertyGetFn = Expression.Call(instanceParam, getMethodInfo);
			var oExprCallPropertyGetFn = Expression.Convert(exprCallPropertyGetFn, typeof(object));

			var propertyGetFn = Expression.Lambda<Func<object, object>>
			(
				oExprCallPropertyGetFn,
				oInstanceParam
			).Compile();

			return propertyGetFn;
		}
	}

}

