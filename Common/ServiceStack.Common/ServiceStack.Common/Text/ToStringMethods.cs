using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text
{
	public delegate void WriteListDelegate(TextWriter writer, object oList, Action<TextWriter, object> toStringFn);
	internal delegate void WriteGenericListDelegate<T>(TextWriter writer, IList<T> list, Action<TextWriter, object> toStringFn);

	internal delegate void WriteDelegate(TextWriter writer, object value);

	public static class ToStringMethods
	{

		public static Action<TextWriter, object> GetToStringMethod<T>()
		{
			var type = typeof(T);

			return GetToStringMethod(type);
		}

		private static readonly Dictionary<Type, Action<TextWriter, object>> ToStringMethodCache 
			= new Dictionary<Type, Action<TextWriter, object>>();

		public static Action<TextWriter, object> GetToStringMethod(Type type)
		{
			Action<TextWriter, object> toStringMethod;
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

		private static Action<TextWriter, object> GetToStringMethodToCache(Type type)
		{
			if (type == typeof(string))
			{
				return (w, x) => WriteString(w, (string)x);
			}

			if (type.IsValueType)
			{
				if (type == typeof(DateTime))
					return (w, x) => WriteDateTime(w, (DateTime)x);

				if (type == typeof(DateTime?))
					return (w, x) => WriteDateTime(w, (DateTime?)x);

				if (type == typeof(Guid))
					return (w, x) => WriteGuid(w, (Guid)x);

				if (type == typeof(Guid?))
					return (w, x) => WriteGuid(w, (Guid?)x);

				return WriteBuiltIn;
			}

			if (type.IsArray)
			{
				if (type == typeof(byte[]))
					return (w, x) => WriteBytes(w, (byte[])x);

				if (type == typeof(string[]))
					return (w, x) => ToStringListMethods.WriteStringArray(w, (string[])x);

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
					return (w, x) => WriteIDictionary(w, (IDictionary)x);
				}

				return (w, x) => ToStringListMethods.WriteIEnumerable(w, (IEnumerable)x);
			}

			var isEnumerable = type.IsAssignableFrom(typeof(IEnumerable))
				|| type.FindInterfaces((x, y) => x == typeof(IEnumerable), null).Length > 0;

			if (isEnumerable)
			{
				return (w, x) => ToStringListMethods.WriteIEnumerable(w, (IEnumerable)x);
			}

			if (type.IsClass)
			{
				var typeToStringMethod = TypeToStringMethods.GetToStringMethod(type);
				if (typeToStringMethod != null)
				{
					return typeToStringMethod;
				}
			}

			return WriteBuiltIn;
		}

		public static void WriteString(TextWriter writer, string value)
		{
			writer.Write(value.ToCsvField());
		}

		public static void WriteDateTime(TextWriter writer, DateTime dateTime)
		{
			writer.Write(DateTimeSerializer.ToShortestXsdDateTimeString(dateTime));
		}

		public static void WriteDateTime(TextWriter writer, DateTime? dateTime)
		{
			if (dateTime == null) return;
			WriteDateTime(writer, dateTime.Value);
		}

		public static void WriteGuid(TextWriter writer, Guid value)
		{
			writer.Write(value.ToString("N"));
		}

		public static void WriteGuid(TextWriter writer, Guid? guid)
		{
			if (guid == null) return;
			WriteGuid(writer, guid.Value);
		}

		public static void WriteBuiltIn(TextWriter writer, object value)
		{
			if (value == null) return;
			writer.Write(value);
		}

		public static void WriteBytes(TextWriter writer, byte[] byteValue)
		{
			if (byteValue == null) return;
			writer.Write(Convert.ToBase64String(byteValue));
		}

		public static void WriteItemSeperatorIfRanOnce(TextWriter writer, ref bool ranOnce)
		{
			if (ranOnce)
				writer.Write(TypeSerializer.ItemSeperator);
			else
				ranOnce = true;
		}

		public static void WriteIDictionary(TextWriter writer, IDictionary map)
		{
			Action<TextWriter, object> writeKeyFn = null;
			Action<TextWriter, object> writeValueFn = null;

			writer.Write(TypeSerializer.MapStartChar);

			var ranOnce = false;
			foreach (var key in map.Keys)
			{
				var dictionaryValue = map[key];
				if (writeKeyFn == null)
					writeKeyFn = GetToStringMethodToCache(key.GetType());

				if (writeValueFn == null)
					writeValueFn = GetToStringMethodToCache(dictionaryValue.GetType());


				WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

				writeKeyFn(writer, key);

				writer.Write(TypeSerializer.MapKeySeperator);

				writeValueFn(writer, dictionaryValue ?? TypeSerializer.MapNullValue);
			}

			writer.Write(TypeSerializer.MapEndChar);
		}

		public static void WriteGenericIDictionary<K, V>(
			TextWriter writer, 
			IDictionary<K, V> map, 
			Action<TextWriter, object> writeKeyFn,
			Action<TextWriter, object> writeValueFn)
		{
			writer.Write(TypeSerializer.MapStartChar);

			var ranOnce = false;
			foreach (var key in map.Keys)
			{
				var mapValue = map[key];

				WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

				writeKeyFn(writer, key);

				writer.Write(TypeSerializer.MapKeySeperator);

				writeValueFn(writer, mapValue);
			}

			writer.Write(TypeSerializer.MapEndChar);
		}

	}
}