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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ServiceStack.Text.Jsv
{
	public static class JsvWriter
	{
		private static readonly Dictionary<Type, Action<TextWriter, object>> WriteFnCache =
			new Dictionary<Type, Action<TextWriter, object>>();

		public static Action<TextWriter, object> GetWriteFn(Type type)
		{
			try
			{
				Action<TextWriter, object> writeFn;
				lock (WriteFnCache)
				{
					if (!WriteFnCache.TryGetValue(type, out writeFn))
					{
						var genericType = typeof(JsvWriter<>).MakeGenericType(type);
						var mi = genericType.GetMethod("WriteFn",
							BindingFlags.Public | BindingFlags.Static);

						var writeFactoryFn = (Func<Action<TextWriter, object>>)Delegate.CreateDelegate(
							typeof(Func<Action<TextWriter, object>>), mi);
						writeFn = writeFactoryFn();
						WriteFnCache.Add(type, writeFn);
					}
				}
				return writeFn;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}
		}

		public static void WriteLateBoundObject(TextWriter writer, object value)
		{
			if (value == null) return;
			var writeFn = GetWriteFn(value.GetType());
			writeFn(writer, value);
		}

		public static bool ShouldUseDefaultToStringMethod(Type type)
		{
			return type != typeof(DateTime)
				   || type != typeof(DateTime?)
				   || type != typeof(Guid)
				   || type != typeof(Guid?)
				   || type != typeof(float) || type != typeof(float?)
				   || type != typeof(double) || type != typeof(double?)
				   || type != typeof(decimal) || type != typeof(decimal?);
		}

		public static Action<TextWriter, object> GetValueTypeToStringMethod(Type type)
		{
			if (type == typeof(DateTime))
				return WriterUtils.WriteDateTime;

			if (type == typeof(DateTime?))
				return WriterUtils.WriteNullableDateTime;

			if (type == typeof(Guid))
				return WriterUtils.WriteGuid;

			if (type == typeof(Guid?))
				return WriterUtils.WriteNullableGuid;

			if (type == typeof(float) || type == typeof(float?))
				return WriterUtils.WriteFloat;

			if (type == typeof(double) || type == typeof(double?))
				return WriterUtils.WriteDouble;

			if (type == typeof(decimal) || type == typeof(decimal?))
				return WriterUtils.WriteDecimal;

			return WriterUtils.WriteBuiltIn;
		}
	}

	/// <summary>
	/// Implement the serializer using a more static approach
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public static class JsvWriter<T>
	{
		private static readonly Action<TextWriter, object> CacheFn;

		public static Action<TextWriter, object> WriteFn()
		{
			return CacheFn;
		}

		static JsvWriter()
		{
			CacheFn = GetWriteFn();
		}

		private static Action<TextWriter, object> GetWriteFn()
		{
			if (typeof(T) == typeof(object))
			{
				return JsvWriter.WriteLateBoundObject;
			}

			if (typeof(T) == typeof(string))
			{
				return WriteString;
			}

			if (typeof(T).IsValueType)
			{
				return JsvWriter.GetValueTypeToStringMethod(typeof(T));
			}

			var specialWriteFn = SpecialTypeUtils.GetWriteFn(typeof(T));
			if (specialWriteFn != null)
			{
				return specialWriteFn;
			}

			if (typeof(T).IsArray)
			{
				if (typeof(T) == typeof(byte[]))
					return WriteLists.WriteBytes;

				if (typeof(T) == typeof(string[]))
					return WriteLists.WriteStringArray;

				if (typeof(T) == typeof(int[]))
					return WriteListsOfElements<int>.WriteGenericArrayValueType;
				if (typeof(T) == typeof(long[]))
					return WriteListsOfElements<long>.WriteGenericArrayValueType;

				var elementType = typeof(T).GetElementType();
				var writeFn = WriteListsOfElements.GetGenericWriteArray(elementType);
				return writeFn;
			}

			if (typeof(T).IsGenericType())
			{
				var listInterfaces = typeof(T).FindInterfaces(
					(t, critera) => t.IsGenericType
									&& t.GetGenericTypeDefinition() == typeof(IList<>), null);

				if (listInterfaces.Length > 0)
					return WriteLists<T>.Write;

				var mapInterfaces = typeof(T).FindInterfaces(
					(t, critera) => t.IsGenericType
									&& t.GetGenericTypeDefinition() == typeof(IDictionary<,>), null);

				if (mapInterfaces.Length > 0)
				{
					var mapTypeArgs = mapInterfaces[0].GetGenericArguments();
					var writeFn = WriteDictionary.GetWriteGenericDictionary(
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
					var writeFn = WriteListsOfElements.GetGenericWriteEnumerable(elementType);
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
					return WriteDictionary.WriteIDictionary;
				}

				return WriteLists.WriteIEnumerable;
			}

			var isEnumerable = typeof(T).IsAssignableFrom(typeof(IEnumerable))
							   || typeof(T).FindInterfaces((x, y) => x == typeof(IEnumerable), null).Length > 0;

			if (isEnumerable)
			{
				return WriteLists.WriteIEnumerable;
			}

			if (typeof(T).IsClass || typeof(T).IsInterface)
			{
				var typeToStringMethod = WriteType<T>.Write;
				if (typeToStringMethod != null)
				{
					return typeToStringMethod;
				}
			}

			return WriterUtils.WriteBuiltIn;
		}

		public static void WriteString(TextWriter writer, object value)
		{
			writer.Write(((string)value).ToCsvField());
		}

		public static void WriteObject(TextWriter writer, object value)
		{
			CacheFn(writer, value);
		}

	}
}