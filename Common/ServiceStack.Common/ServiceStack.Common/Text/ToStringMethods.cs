using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text
{
	internal delegate string CollectionToStringDelegate(object oList, Func<object, string> toStringFn);

	internal delegate string ToStringDelegate(object value);

	public static class ToStringMethods
	{
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
				if (type == typeof(DateTime))
					return value => DateTimeToString((DateTime)value);

				if (type == typeof(DateTime?))
					return value => value == null ? null : DateTimeToString((DateTime)value);

				return BuiltinToString;
			}

			if (type.IsArray)
			{
				if (type == typeof(byte[]))
				{
					return x => BytesToString((byte[])x);
				}
				if (type == typeof(string[]))
				{
					return x => ToStringListMethods.StringArrayToString((string[])x);
				}

				return ToStringListMethods.GetArrayToStringMethod(type.GetElementType());
			}

			if (type.IsGenericType())
			{
				var listInterfaces = type.FindInterfaces(
					(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null);
				if (listInterfaces.Length > 0)
					return ToStringListMethods.GetToStringMethod(type);
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

			if (type.IsClass)
			{
				var typeToStringMethod = TypeToStringMethods.GetToStringMethod(type);
				if (typeToStringMethod != null)
				{
					return typeToStringMethod;
				}
			}

			return BuiltinToString;
		}

		static Dictionary<DateTime, string> dateTimeValues = new Dictionary<DateTime, string>();

		/// <summary>
		/// DateTime.ToString() is really slow, need to cache values or return ticks.
		/// </summary>
		/// <param name="dateTime">The date time.</param>
		/// <returns></returns>
		public static string DateTimeToString(DateTime dateTime)
		{
			return DateTimeSerializer.ToShortestXsdDateTimeString(dateTime);

			string dateTimeString;
			lock (dateTimeValues)
			{
				if (!dateTimeValues.TryGetValue(dateTime, out dateTimeString))
				{
					if (dateTimeValues.Count > 100)
					{
						dateTimeValues = new Dictionary<DateTime, string>();
					}
					dateTimeString = dateTime.ToString();
					dateTimeValues[dateTime] = dateTimeString;
				}
			}
			return dateTimeString;
		}

		public static string StringToString(string value)
		{
			return value.ToCsvField();
		}

		public static string BuiltinToString(object value)
		{
			return value == null ? null : value.ToString();
		}

		public static string BytesToString(byte[] byteValue)
		{
			return byteValue == null ? null : Encoding.Default.GetString(byteValue);
		}

		public static string IEnumerableToString(IEnumerable valueCollection)
		{
			Func<object,string> toStringFn = null;

			var sb = new StringBuilder();
			foreach (var valueItem in valueCollection)
			{
				if (toStringFn == null)
				{
					toStringFn = GetToStringMethod(valueItem.GetType());
				}

				var elementValueString = toStringFn(valueItem);
				if (sb.Length > 0)
				{
					sb.Append(TextExtensions.ItemSeperator);
				}
				sb.Append(elementValueString);
			}
			return sb.ToString();
		}

		public static string IDictionaryToString(IDictionary valueDictionary)
		{
			Func<object,string> toStringKeyFn = null;
			Func<object,string> toStringValueFn = null;

			var sb = new StringBuilder();
			foreach (var key in valueDictionary.Keys)
			{
				var dictionaryValue = valueDictionary[key];
				if (toStringKeyFn == null)
				{
					toStringKeyFn = GetToStringMethodToCache(key.GetType());
				}
				if (toStringValueFn == null)
				{
					toStringValueFn = GetToStringMethodToCache(dictionaryValue.GetType());
				}
				var keyString = toStringKeyFn(key);
				var valueString = dictionaryValue != null ? toStringValueFn(dictionaryValue) : string.Empty;

				if (sb.Length > 0)
				{
					sb.Append(TextExtensions.ItemSeperator);
				}
				sb.Append(keyString)
					.Append(TextExtensions.KeyValueSeperator)
					.Append(valueString);
			}

			sb.Insert(0, TextExtensions.MapStartChar);
			sb.Append(TextExtensions.MapEndChar);
			return sb.ToString();
		}

	}
}