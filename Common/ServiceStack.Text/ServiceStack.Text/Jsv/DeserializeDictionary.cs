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
using System.Reflection;
using System.Text;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Jsv
{
	public static class DeserializeDictionary
	{
		const int KeyIndex = 0;
		const int ValueIndex = 1;

		public static Func<string, object> GetParseMethod(Type type)
		{
			var mapInterface = type.GetTypeWithGenericInterfaceOf(typeof(IDictionary<,>));
			if (mapInterface == null)
				throw new ArgumentException(string.Format("Type {0} is not of type IDictionary<,>", type.FullName));

			//optimized access for regularly used types
			if (type == typeof(Dictionary<string, string>))
			{
				return ParseStringDictionary;
			}

			var dictionaryArgs =  mapInterface.GetGenericArguments();

			var keyTypeParseMethod = JsvReader.GetParseFn(dictionaryArgs[KeyIndex]);
			if (keyTypeParseMethod == null) return null;

			var valueTypeParseMethod = JsvReader.GetParseFn(dictionaryArgs[ValueIndex]);
			if (valueTypeParseMethod == null) return null;

			var createMapType = type.HasAnyTypeDefinitionsOf(typeof(Dictionary<,>), typeof(IDictionary<,>))
				? null : type;
			
			return value => ParseDictionaryType(value, createMapType, dictionaryArgs, keyTypeParseMethod, valueTypeParseMethod);
		}

		private static string StripMap(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;

			return value[0] == JsWriter.MapStartChar
			       	? value.Substring(1, value.Length - 2)
			       	: value;
		}

		public static Dictionary<string, string> ParseStringDictionary(string value)
		{
			if ((value = StripMap(value)) == null) return null;
			if (value == string.Empty) return new Dictionary<string, string>();

			var result = new Dictionary<string, string>();

			var valueLength = value.Length;
			for (var i=0; i < valueLength; i++)
			{
				var keyValue = ParseUtils.EatMapKey(value, ref i);
				i++;
				var elementValue = ParseUtils.EatMapValue(value, ref i);

				var mapKey = ParseUtils.ParseString(keyValue);
				var mapValue = ParseUtils.ParseString(elementValue);

				result[mapKey] = mapValue;
			}

			return result;
		}

		public static IDictionary<TKey, TValue> ParseDictionary<TKey, TValue>(string value, Type createMapType,
		                                                                      Func<string, object> parseKeyFn, Func<string, object> parseValueFn)
		{
			if ((value = StripMap(value)) == null) return null;

			var to = (createMapType == null)
						? new Dictionary<TKey, TValue>()
						: (IDictionary<TKey, TValue>)ReflectionExtensions.CreateInstance(createMapType);

			if (value == string.Empty) return to;

			var valueLength = value.Length;
			for (var i=0; i < valueLength; i++)
			{
				var keyValue = ParseUtils.EatMapKey(value, ref i);
				i++;
				var elementValue = ParseUtils.EatMapValue(value, ref i);

				var mapKey = (TKey)parseKeyFn(keyValue);
				var mapValue = (TValue)parseValueFn(elementValue);

				to[mapKey] = mapValue;
			}

			return to;
		}

		private static readonly Dictionary<string, ParseDictionaryDelegate> ParseDelegateCache 
			= new Dictionary<string, ParseDictionaryDelegate>();

		private delegate object ParseDictionaryDelegate(string value, Type createMapType,
		                                                Func<string, object> keyParseFn, Func<string, object> valueParseFn);

		public static object ParseDictionaryType(string value, Type createMapType, Type[] argTypes,
		                                         Func<string, object> keyParseFn, Func<string, object> valueParseFn)
		{
			var mi = typeof(DeserializeDictionary)
				.GetMethod("ParseDictionary", BindingFlags.Static | BindingFlags.Public);

			ParseDictionaryDelegate parseDelegate;
			var key = GetTypesKey(argTypes);
			if (!ParseDelegateCache.TryGetValue(key, out parseDelegate))
			{
				var genericMi = mi.MakeGenericMethod(argTypes);
				parseDelegate = (ParseDictionaryDelegate)Delegate.CreateDelegate(
				                                         	typeof(ParseDictionaryDelegate), genericMi);

				ParseDelegateCache[key] = parseDelegate;
			}

			return parseDelegate(value, createMapType, keyParseFn, valueParseFn);
		}

		private static string GetTypesKey(params Type[] types)
		{
			var sb = new StringBuilder();
			foreach (var type in types)
			{
				if (sb.Length > 0)
					sb.Append(">");

				sb.Append(type.FullName);
			}
			return sb.ToString();
		}
	}
}