using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text
{
	public static class ParseStringDictionaryMethods
	{
		const int KeyIndex = 0;
		const int ValueIndex = 1;

		public static Func<string, object> GetParseMethod(Type type)
		{
			var mapInterfaces = type.FindInterfaces(
				(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>), null);

			if (mapInterfaces.Length == 0)
				throw new ArgumentException(string.Format("Type {0} is not of type IDictionary<,>", type.FullName));

			//optimized access for regularly used types
			if (type == typeof(Dictionary<string, string>))
			{
				return ParseStringDictionary;
			}

			var dictionaryArgs =  mapInterfaces[0].GetGenericArguments();

			var keyTypeParseMethod = ParseStringMethods.GetParseMethod(dictionaryArgs[KeyIndex]);
			if (keyTypeParseMethod == null) return null;

			var valueTypeParseMethod = ParseStringMethods.GetParseMethod(dictionaryArgs[ValueIndex]);
			if (valueTypeParseMethod == null) return null;

			var createMapType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ? null : type;
			return value => ParseDictionaryType(value, createMapType, dictionaryArgs, keyTypeParseMethod, valueTypeParseMethod);
		}

		public static Dictionary<string, string> ParseStringDictionary(string value)
		{
			var result = new Dictionary<string, string>();

			var entryValues = value.Split(TextExtensions.ItemSeperator);
			foreach (var entryValue in entryValues)
			{
				var keyValuePair = entryValue.Split(TextExtensions.KeyValueSeperator);
				var mapKey = ParseStringMethods.ParseString(keyValuePair[KeyIndex]);
				var mapValue = ParseStringMethods.ParseString(keyValuePair[ValueIndex]);
				result[mapKey] = mapValue;
			}

			return result;
		}

		public static IDictionary<TKey, TValue> ParseDictionary<TKey, TValue>(string value, Type createMapType,
			Func<string, object> parseKeyFn, Func<string, object> parseValueFn)
		{
			var to = (createMapType == null)
				? new Dictionary<TKey, TValue>()
				: (IDictionary<TKey, TValue>)Activator.CreateInstance(createMapType);

			var entryValues = value.Split(TextExtensions.ItemSeperator);
			foreach (var entryValue in entryValues)
			{
				var keyValuePair = entryValue.Split(TextExtensions.KeyValueSeperator);
				var mapKey = (TKey)parseKeyFn(keyValuePair[KeyIndex]);
				var mapValue = (TValue)parseValueFn(keyValuePair[ValueIndex]);
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
			var mi = typeof(ParseStringDictionaryMethods)
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