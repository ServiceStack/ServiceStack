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

namespace ServiceStack.Text.Common
{
	internal static class DeserializeType<TSerializer>
		where TSerializer : ITypeSerializer
	{
		private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

		public static ParseStringDelegate GetParseMethod(Type type)
		{
			if (!type.IsClass) return null;

			var propertyInfos = type.GetProperties();
			if (propertyInfos.Length == 0)
			{
				if (type.IsDto())
				{
					var emptyCtorFn = ReflectionExtensions.GetConstructorMethodToCache(type);
					return value => emptyCtorFn();
				}
				return null;
			}


			var setterMap = new Dictionary<string, SetPropertyDelegate>();
			var map = new Dictionary<string, ParseStringDelegate>();

			foreach (var propertyInfo in propertyInfos)
			{
				map[propertyInfo.Name] = Serializer.GetParseFn(propertyInfo.PropertyType);
				setterMap[propertyInfo.Name] = GetSetPropertyMethod(type, propertyInfo);
			}

			var ctorFn = ReflectionExtensions.GetConstructorMethodToCache(type);
			return value => StringToType(type, value, ctorFn, setterMap, map);
		}

		private static object StringToType(Type type, string strType, 
           EmptyCtorDelegate ctorFn,
		   IDictionary<string, SetPropertyDelegate> setterMap,
		   IDictionary<string, ParseStringDelegate> parseStringFnMap)
		{
			var index = 0;

			if (!Serializer.EatMapStartChar(strType, ref index))
				throw new SerializationException(string.Format(
					"Type definitions should start with a '{0}', expecting serialized type '{1}', got string starting with: {2}",
					JsWriter.MapStartChar, type.Name, strType.Substring(0, strType.Length < 50 ? strType.Length : 50)));


			var instance = ctorFn();
			string propertyName;
			ParseStringDelegate parseStringFn;
			SetPropertyDelegate setterFn;

			if (strType == JsWriter.EmptyMap) return instance;
			var strTypeLength = strType.Length;

			while (index < strTypeLength)
			{
				propertyName = Serializer.EatMapKey(strType, ref index);

				Serializer.EatMapKeySeperator(strType, ref index);

				var propertyValueString = Serializer.EatValue(strType, ref index);

				parseStringFnMap.TryGetValue(propertyName, out parseStringFn);

				if (parseStringFn != null)
				{
					var propertyValue = parseStringFn(propertyValueString);

					setterMap.TryGetValue(propertyName, out setterFn);

					if (setterFn != null)
					{
						setterFn(instance, propertyValue);
					}
				}

				Serializer.EatItemSeperatorOrMapEndChar(strType, ref index);
			}

			return instance;
		}

		public static SetPropertyDelegate GetSetPropertyMethod(Type type, PropertyInfo propertyInfo)
		{
			var setMethodInfo = propertyInfo.GetSetMethod(true);
			if (setMethodInfo == null) return null;
			var oInstanceParam = Expression.Parameter(typeof(object), "oInstanceParam");
			var oValueParam = Expression.Parameter(typeof(object), "oValueParam");

			var instanceParam = Expression.Convert(oInstanceParam, type);
			var useType = propertyInfo.PropertyType;

			var valueParam = Expression.Convert(oValueParam, useType);
			var exprCallPropertySetFn = Expression.Call(instanceParam, setMethodInfo, valueParam);

			var propertySetFn = Expression.Lambda<SetPropertyDelegate>
				(
					exprCallPropertySetFn,
					oInstanceParam,
					oValueParam
				).Compile();

			return propertySetFn;
		}
	}
}