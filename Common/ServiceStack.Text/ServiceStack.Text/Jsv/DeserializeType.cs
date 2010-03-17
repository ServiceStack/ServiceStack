using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceStack.Text.Jsv
{
	public static class DeserializeType
	{
		public static Func<string, object> GetParseMethod(Type type)
		{
			if (!type.IsClass) return null;

			var propertyInfos = type.GetProperties();
			if (propertyInfos.Length == 0) return null;


			var setterMap = new Dictionary<string, Action<object, object>>();
			var map = new Dictionary<string, Func<string, object>>();

			foreach (var propertyInfo in propertyInfos)
			{
				map[propertyInfo.Name] = JsvReader.GetParseFn(propertyInfo.PropertyType);
				setterMap[propertyInfo.Name] = GetSetPropertyMethod(type, propertyInfo);
			}

			var ctorFn = ReflectionExtensions.GetConstructorMethodToCache(type);
			return value => StringToType(value, ctorFn, setterMap, map);
		}

		private static object StringToType(string strType, Func<object> ctorFn,
		                                   IDictionary<string, Action<object, object>> setterMap, IDictionary<string, Func<string, object>> parseStringFnMap)
		{
			if (strType[0] != TypeSerializer.MapStartChar)
				throw new SerializationException(string.Format(
					"Type definitions should start with a '{0}'", TypeSerializer.MapStartChar));

			var instance = ctorFn();
			string propertyName;

			try
			{
				var strTypeLength = strType.Length;
				for (var i=1; i < strTypeLength; i++)
				{
					propertyName = ParseUtils.EatMapKey(strType, ref i);
					i++;
					var propertyValueString = ParseUtils.EatMapValue(strType, ref i);

					var parseStringFn = parseStringFnMap[propertyName];
					if (parseStringFn == null) continue;
					var propertyValue = parseStringFn(propertyValueString);
					var setterFn = setterMap[propertyName];

					if (setterFn != null)
					{
						setterFn(instance, propertyValue);
					}
				}
			}
			catch (Exception ex)
			{
				throw;
			}
			return instance;
		}

		public static Action<object, object> GetSetPropertyMethod(Type type, PropertyInfo propertyInfo)
		{
			var setMethodInfo = propertyInfo.GetSetMethod(true);
			if (setMethodInfo == null) return null;
			var oInstanceParam = Expression.Parameter(typeof(object), "oInstanceParam");
			var oValueParam = Expression.Parameter(typeof(object), "oValueParam");

			var instanceParam = Expression.Convert(oInstanceParam, type);
			var useType = propertyInfo.PropertyType;

			var valueParam = Expression.Convert(oValueParam, useType);
			var exprCallPropertySetFn = Expression.Call(instanceParam, setMethodInfo, valueParam);

			var propertySetFn = Expression.Lambda<Action<object, object>>
				(
					exprCallPropertySetFn,
					oInstanceParam,
					oValueParam
				).Compile();

			return propertySetFn;
		}
	}
}