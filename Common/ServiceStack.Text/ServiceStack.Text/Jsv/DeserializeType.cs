//
// http://code.google.com/p/servicestack/wiki/TypeSerializer
// ServiceStack.Text: .NET C# POCO Type Text Serializer.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

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
			return value => StringToType(type, value, ctorFn, setterMap, map);
		}

		private static object StringToType(Type type, string strType, 
			Func<object> ctorFn,
			IDictionary<string, Action<object, object>> setterMap, 
			IDictionary<string, Func<string, object>> parseStringFnMap)
		{
			if (strType[0] != TypeSerializer.MapStartChar)
				throw new SerializationException(string.Format(
					"Type definitions should start with a '{0}', expecting serialized type '{1}', got string starting with: {2}", 
					TypeSerializer.MapStartChar, type.Name, strType.Substring(0, strType.Length < 50 ? strType.Length : 50)));

			var instance = ctorFn();
			string propertyName;

			try
			{
				if (strType == TypeSerializer.EmptyMap) return null;
				var strTypeLength = strType.Length;
				for (var i=1; i < strTypeLength; i++)
				{
					propertyName = ParseUtils.EatMapKey(strType, ref i);
					i++;
					var propertyValueString = ParseUtils.EatMapValue(strType, ref i);

					Func<string, object> parseStringFn;
					parseStringFnMap.TryGetValue(propertyName, out parseStringFn);
					if (parseStringFn == null) continue;

					var propertyValue = parseStringFn(propertyValueString);

					Action<object, object> setterFn;
					setterMap.TryGetValue(propertyName, out setterFn);
					if (setterFn == null) continue;

					setterFn(instance, propertyValue);
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