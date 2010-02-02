using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text.Jsv
{
	public static class JsvWriter
	{
		private static readonly Dictionary<Type, Func<Action<TextWriter, object>>> WriteFnCache =
			new Dictionary<Type, Func<Action<TextWriter, object>>>();

		public static Action<TextWriter, object> GetWriteFn(Type type)
		{
			Func<Action<TextWriter, object>> writeFn;
			lock (WriteFnCache)
			{
				if (!WriteFnCache.TryGetValue(type, out writeFn))
				{
					var genericType = typeof(JsvWriter<>).MakeGenericType(type);
					var mi = genericType.GetMethod("GetWriteFn",
						BindingFlags.Public | BindingFlags.Static);

					writeFn = (Func<Action<TextWriter, object>>)Delegate.CreateDelegate(
						typeof(Func<Action<TextWriter, object>>), mi);
					WriteFnCache.Add(type, writeFn);
				}
			}
			return writeFn();
		}

		public static void WriteLateBoundObject(TextWriter writer, object value)
		{
			if (value == null) return;
			var writeFn = GetWriteFn(value.GetType());
			writeFn(writer, value);
		}

		public static bool ShouldUseDefaultToStringMethod(Type type)
		{
			return type != typeof (DateTime)
			       || type != typeof (DateTime?)
				   || type != typeof(Guid)
				   || type != typeof(Guid?);
		}

		public static Action<TextWriter, object> GetValueTypeToStringMethod(Type type)
		{
			if (type == typeof(DateTime))
				return (w, x) => ToStringMethods.WriteDateTime(w, (DateTime)x);

			if (type == typeof(DateTime?))
				return (w, x) => ToStringMethods.WriteDateTime(w, (DateTime?)x);

			if (type == typeof(Guid))
				return (w, x) => ToStringMethods.WriteGuid(w, (Guid)x);

			if (type == typeof(Guid?))
				return (w, x) => ToStringMethods.WriteGuid(w, (Guid?)x);

			return ToStringMethods.WriteBuiltIn;
		}
	}

	/// <summary>
	/// Implement the serializer using a more static approach
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public static class JsvWriter<T>
	{
		private static readonly Action<TextWriter, object> WriteFn;

		static JsvWriter()
		{
			WriteFn = GetWriteFn();
		}

		public static Action<TextWriter, object> GetWriteFn()
		{
			if (typeof(T) == typeof(object))
			{
				return JsvWriter.WriteLateBoundObject;
			}

			if (typeof(T) == typeof(string))
			{
				return (w, x) => WriteString(w, (string)x); 
			}

			if (typeof(T).IsValueType)
			{
				return JsvWriter.GetValueTypeToStringMethod(typeof(T));
			}


			if (typeof(T).IsArray)
			{
				if (typeof(T) == typeof(byte[]))
					return (w, x) => ToStringListMethodsCache.WriteBytes(w, (byte[])x);

				if (typeof(T) == typeof(string[]))
					return (w, x) => ToStringListMethodsCache.WriteStringArray(w, (string[])x);

				if (typeof(T) == typeof(int[]))
					return (w, x) => ToStringListMethods<int>.WriteGenericArrayValueType(w, (int[])x);
				if (typeof(T) == typeof(long[]))
					return (w, x) => ToStringListMethods<long>.WriteGenericArrayValueType(w, (long[])x);

				var elementType = typeof (T).GetElementType();
				var writeFn = ToStringListMethodsCache.GetGenericWriteArray(elementType);
				return writeFn;
			}

			if (typeof(T).IsGenericType())
			{
				var listInterfaces = typeof(T).FindInterfaces(
					(t, critera) => t.IsGenericType
						&& t.GetGenericTypeDefinition() == typeof(IList<>), null);

				if (listInterfaces.Length > 0)
					return ToStringListMethods<T>.GetToStringMethod();

				var mapInterfaces = typeof(T).FindInterfaces(
					(t, critera) => t.IsGenericType
						&& t.GetGenericTypeDefinition() == typeof(IDictionary<,>), null);

				if (mapInterfaces.Length > 0)
				{
					var mapTypeArgs = mapInterfaces[0].GetGenericArguments();
					var writeFn = ToStringDictionaryMethods.GetWriteGenericDictionary(
						mapTypeArgs[0], mapTypeArgs[1]);

					var keyWriteFn = JsvWriter.GetWriteFn(mapTypeArgs[0]);
					var valueWriteFn = JsvWriter.GetWriteFn(mapTypeArgs[1]);

					return (w, x) => writeFn(w, x, keyWriteFn, valueWriteFn);
				}

				var enumerableInterfaces = typeof(T).FindInterfaces(
					(t, critera) => t.IsGenericType
						&& t.GetGenericTypeDefinition() == typeof(IEnumerable<>), null);

				if (enumerableInterfaces.Length > 0)
				{
					var elementType = enumerableInterfaces[0].GetGenericArguments()[0];
					var writeFn = ToStringListMethodsCache.GetGenericWriteEnumerable(elementType);
					return writeFn;
				}
			}

			var isCollection = typeof(T).FindInterfaces((x, y) => x == typeof(ICollection), null).Length > 0;
			if (isCollection)
			{
				var isDictionary = typeof(T).IsAssignableFrom(typeof(IDictionary))
					|| typeof(T).FindInterfaces((x, y) => x == typeof(IDictionary), null).Length > 0;

				if (isDictionary)
				{
					return (w, x) => ToStringMethods.WriteIDictionary(w, (IDictionary)x);
				}

				return (w, x) => ToStringListMethods.WriteIEnumerable(w, (IEnumerable)x);
			}

			var isEnumerable = typeof(T).IsAssignableFrom(typeof(IEnumerable))
				|| typeof(T).FindInterfaces((x, y) => x == typeof(IEnumerable), null).Length > 0;

			if (isEnumerable)
			{
				return (w, x) => ToStringListMethods.WriteIEnumerable(w, (IEnumerable)x);
			}

			if (typeof(T).IsClass)
			{
				var typeToStringMethod = TypeToStringMethods<T>.GetToStringMethod();
				if (typeToStringMethod != null)
				{
					return typeToStringMethod;
				}
			}

			return ToStringMethods.WriteBuiltIn;
		}

		public static void WriteString(TextWriter writer, string value)
		{
			writer.Write(value.ToCsvField());
		}


		public static void WriteObject(TextWriter writer, object value)
		{
			WriteFn(writer, value);
		}

	}
}