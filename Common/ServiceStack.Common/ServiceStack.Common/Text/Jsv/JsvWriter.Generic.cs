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
			if (typeof(T) == typeof(string))
			{
				return (w, x) => ToStringMethods.WriteString(w, (string)x); 
			}

			if (typeof(T).IsValueType)
			{
				if (typeof(T) == typeof(DateTime))
					return (w, x) => ToStringMethods.WriteDateTime(w, (DateTime)x);

				if (typeof(T) == typeof(DateTime?))
					return (w, x) => ToStringMethods.WriteDateTime(w, (DateTime?)x);

				if (typeof(T) == typeof(Guid))
					return (w, x) => ToStringMethods.WriteGuid(w, (Guid)x);

				if (typeof(T) == typeof(Guid?))
					return (w, x) => ToStringMethods.WriteGuid(w, (Guid?)x);

				return ToStringMethods.WriteBuiltIn;
			}


			if (typeof(T).IsArray)
			{
				if (typeof(T) == typeof(byte[]))
					return (w, x) => ToStringMethods.WriteBytes(w, (byte[])x);

				if (typeof(T) == typeof(string[]))
					return (w, x) => ToStringListMethods.WriteStringArray(w, (string[])x);

				return ToStringListMethods.GetArrayToStringMethod(typeof(T).GetElementType());
			}

			if (typeof(T).IsGenericType())
			{
				var listInterfaces = typeof(T).FindInterfaces(
					(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null);
				if (listInterfaces.Length > 0)
					return ToStringListMethods.GetToStringMethod(typeof(T));
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

		public static void WriteObject(TextWriter writer, object value)
		{
			WriteFn(writer, value);
		}

	}
}