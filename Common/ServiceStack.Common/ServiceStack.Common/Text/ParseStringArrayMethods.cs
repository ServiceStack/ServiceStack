using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text
{
	public static class ParseStringArrayMethods
	{
		public static Func<string, object> GetParseMethod(Type type)
		{
			if (!type.IsArray)
				throw new ArgumentException(string.Format("Type {0} is not an Array type", type.FullName));


			if (type == typeof(string[]))
				return ParseStringArray;
			if (type == typeof(byte[]))
				return ParseByteArray;

			var elementType = type.GetElementType();
			var supportedParseMethod = ParseStringMethods.GetParseMethod(elementType);
			if (supportedParseMethod != null)
			{
				return value => ParseArrayType(elementType, value, supportedParseMethod);
			}
			return null;
		}

		public static string[] ParseStringArray(string value)
		{
			if (value == null) return null;
			return value == string.Empty
					? new string[0]
					: ParseStringListMethods.ParseStringList(value).ToArray();
		}

		public static byte[] ParseByteArray(string value)
		{
			if (value == null) return null;
			return value == string.Empty
					? new byte[0]
					: Encoding.Default.GetBytes(value);
		}

		private static readonly Dictionary<Type, ParseArrayDelegate> ParseDelegateCache 
			= new Dictionary<Type, ParseArrayDelegate>();

		private delegate object ParseArrayDelegate(string value, Func<string, object> parseFn);

		public static object ParseArrayType(Type type, string value, Func<string, object> parseFn)
		{
			ParseArrayDelegate parseDelegate;
			if (!ParseDelegateCache.TryGetValue(type, out parseDelegate))
			{
				var mi = typeof(ParseStringArrayMethods).GetMethod(
					"ParseArray", BindingFlags.Static | BindingFlags.Public);

				var genericArgs = new[] { type };
				var genericMi = mi.MakeGenericMethod(genericArgs);

				parseDelegate = (ParseArrayDelegate) Delegate.CreateDelegate(typeof(ParseArrayDelegate), genericMi);
				ParseDelegateCache[type] = parseDelegate;
			}

			return parseDelegate(value, parseFn);
		}

		public static T[] ParseArray<T>(string value, Func<string, object> parseFn)
		{
			if (value == null) return null;
			if (value == string.Empty) return new T[0];

			if (value[0] == TextExtensions.TypeStartChar)
			{
				var itemValues = new List<string>();
				var i = 0;
				do
				{
					itemValues.Add(ParseStringMethods.EatTypeValue(value, ref i));
				} while (++i < value.Length);

				var results = new T[itemValues.Count];
				for (var j=0; j < itemValues.Count; j++)
				{
					results[j] = (T)parseFn(itemValues[j]);
				}
				return results;
			}
			else
			{
				var to = new List<T>();
				var valueLength = value.Length;
				for (var i=0; i < valueLength; i++)
				{
					var elementValue = ParseStringMethods.EatValue(value, ref i);
					var listValue = ParseStringMethods.ParseString(elementValue);
					to.Add((T)parseFn(listValue));
				}
				return to.ToArray();
			}

		}
	}
}