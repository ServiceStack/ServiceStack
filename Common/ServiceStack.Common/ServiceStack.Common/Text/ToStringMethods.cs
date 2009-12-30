using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text
{
	internal static class ToStringMethods
	{
		const char FieldSeperator = ',';
		const char KeySeperator = ':';

		public static string ToString(object value)
		{
			var toStringMethod = GetToStringMethod(value.GetType());
			return toStringMethod(value);
		}

		public static Func<object, string> GetToStringMethod<T>()
		{
			var type = typeof(T);

			return GetToStringMethod(type);
		}

		private static readonly Dictionary<Type, Func<object, string>> ToStringMethodCache 
			= new Dictionary<Type, Func<object, string>>();

		public static Func<object, string> GetToStringMethod(Type type)
		{
			Func<object, string> toStringMethod;
			lock (ToStringMethodCache)
			{
				if (!ToStringMethodCache.TryGetValue(type, out toStringMethod))
				{
					toStringMethod = GetToStringMethodToCache(type);
					ToStringMethodCache[type] = toStringMethod;
				}
			}

			return toStringMethod;
		}

		private static Func<object, string> GetToStringMethodToCache(Type type)
		{
			if (type == typeof(string))
			{
				return x => StringToString((string)x);
			}

			if (type.IsValueType)
			{
				return BuiltinToString;
			}

			if (type == typeof(byte[]))
			{
				return BytesToString;
			}

			var isCollection = type.FindInterfaces((x, y) => x == typeof(ICollection), null).Length > 0;
			if (isCollection)
			{
				var isDictionary = type.IsAssignableFrom(typeof(IDictionary))
				                   || type.FindInterfaces((x, y) => x == typeof(IDictionary), null).Length > 0;

				if (isDictionary)
				{
					return obj => IDictionaryToString((IDictionary)obj);
				}
				
				return obj => IEnumerableToString((IEnumerable)obj);
			}

			var isEnumerable = type.IsAssignableFrom(typeof(IEnumerable))
			                   || type.FindInterfaces((x, y) => x == typeof(IEnumerable), null).Length > 0;

			if (isEnumerable)
			{
				return obj => IEnumerableToString((IEnumerable)obj);
			}

			return BuiltinToString;
		}

		public static string StringToString(object value)
		{
			return StringToString((string)value);
		}

		public static string StringToString(string value)
		{
			return value.ToSafeString();
		}

		public static string BuiltinToString(object value)
		{
			return value.ToString();
		}

		public static string BytesToString(object byteValue)
		{
			return BytesToString((byte[])byteValue);
		}

		public static string BytesToString(byte[] byteValue)
		{
			return byteValue == null ? null : Encoding.Default.GetString(byteValue);
		}

		public static string IEnumerableToString(IEnumerable valueCollection)
		{
			var sb = new StringBuilder();
			foreach (var valueItem in valueCollection)
			{
				var elementValueString = ToString(valueItem);
				if (sb.Length > 0)
				{
					sb.Append(FieldSeperator);
				}
				sb.Append(elementValueString);
			}
			return sb.ToString();
		}

		public static string IDictionaryToString(IDictionary valueDictionary)
		{
			var sb = new StringBuilder();
			foreach (var key in valueDictionary.Keys)
			{
				var keyString = ToString(key);
				var dictionaryValue = valueDictionary[key];
				var valueString = dictionaryValue != null ? ToString(dictionaryValue) : string.Empty;

				if (sb.Length > 0)
				{
					sb.Append(FieldSeperator);
				}
				sb.Append(keyString)
					.Append(KeySeperator)
					.Append(valueString);
			}
			return sb.ToString();
		}

	}
}