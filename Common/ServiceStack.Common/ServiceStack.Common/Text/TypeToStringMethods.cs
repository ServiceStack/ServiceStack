using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

			var getterMap = new Dictionary<string, Func<object, object>>();
			var map = new Dictionary<string, Func<object, string>>();

			foreach (var propertyInfo in propertyInfos)
			{
				map[propertyInfo.Name] = ToStringMethods.GetToStringMethod(propertyInfo.PropertyType);
				getterMap[propertyInfo.Name] = GetPropertyValueMethod(type, propertyInfo);
			}

			return value => TypeToString(value, getterMap, map);
		}

		public static string TypeToString(object value, 
			Dictionary<string, Func<object, object>> getterMap, Dictionary<string, Func<object, string>> toStringMap)
		{
			var sb = new StringBuilder();

			//var timePoints = new List<KeyValuePair<string, long>>();
			//var stopWatch = new Stopwatch();
			//stopWatch.Start();
			
			//timePoints.Add(new KeyValuePair<string, long>("Begin TypeToString:" + value.GetType().Name, stopWatch.ElapsedTicks));
			foreach (var getterEntry in getterMap)
			{
				if (sb.Length > 0) sb.Append(TextExtensions.PropertyItemSeperator);

				var propertyValue = getterEntry.Value(value);
				var propertyValueString = toStringMap[getterEntry.Key](propertyValue);
				sb.Append(getterEntry.Key)
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

