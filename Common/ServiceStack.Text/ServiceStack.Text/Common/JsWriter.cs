using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using ServiceStack.Text.Json;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text.Common
{
	public static class JsWriter
	{
		public const char MapStartChar = '{';
		public const char MapKeySeperator = ':';
		public const char ItemSeperator = ',';
		public const char MapEndChar = '}';
		public const string MapNullValue = "\"\"";
		public const string EmptyMap = "{}";

		public const char ListStartChar = '[';
		public const char ListEndChar = ']';

		public const char QuoteChar = '"';
		public const string QuoteString = "\"";
		public const string ItemSeperatorString = ",";
		public const string MapKeySeperatorString = ":";

		public static readonly char[] CsvChars = new[] { ItemSeperator, QuoteChar };
		public static readonly char[] EscapeChars = new[] { QuoteChar, MapKeySeperator, ItemSeperator, MapStartChar, MapEndChar, ListStartChar, ListEndChar, };

		private const int LengthFromLargestChar = '}' + 1;
		private static readonly bool[] EscapeCharFlags = new bool[LengthFromLargestChar];

		static JsWriter()
		{
			foreach (var escapeChar in EscapeChars)
			{
				EscapeCharFlags[escapeChar] = true;
			}
		}

		/// <summary>
		/// micro optimizations: using flags instead of value.IndexOfAny(EscapeChars)
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool HasAnyEscapeChars(string value)
		{
			var len = value.Length;
			for (var i = 0; i < len; i++)
			{
				var c = value[i];
				if (c >= LengthFromLargestChar || !EscapeCharFlags[c]) continue;
				return true;
			}
			return false;
		}

		internal static void WriteItemSeperatorIfRanOnce(TextWriter writer, ref bool ranOnce)
		{
			if (ranOnce)
				writer.Write(ItemSeperator);
			else
				ranOnce = true;

			foreach (var escapeChar in EscapeChars)
			{
				EscapeCharFlags[escapeChar] = true;
			}
		}

		internal static bool ShouldUseDefaultToStringMethod(Type type)
		{
			return type == typeof(byte) || type == typeof(byte?)
					|| type == typeof(short) || type == typeof(short?)
					|| type == typeof(ushort) || type == typeof(ushort?)
					|| type == typeof(int) || type == typeof(int?)
					|| type == typeof(uint) || type == typeof(uint?)
					|| type == typeof(long) || type == typeof(long?)
					|| type == typeof(ulong) || type == typeof(ulong?)
					|| type == typeof(bool) || type == typeof(bool?)
					|| type != typeof(DateTime)
					|| type != typeof(DateTime?)
					|| type != typeof(Guid)
					|| type != typeof(Guid?)
					|| type != typeof(float) || type != typeof(float?)
					|| type != typeof(double) || type != typeof(double?)
					|| type != typeof(decimal) || type != typeof(decimal?);
		}

		internal static ITypeSerializer GetTypeSerializer<TSerializer>()
		{
			if (typeof(TSerializer) == typeof(JsvTypeSerializer))
				return JsvTypeSerializer.Instance;

			if (typeof(TSerializer) == typeof(JsonTypeSerializer))
				return JsonTypeSerializer.Instance;

			throw new NotSupportedException(typeof(TSerializer).Name);
		}
	}

	internal class JsWriter<TSerializer>
		where TSerializer : ITypeSerializer
	{
		private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

		public JsWriter()
		{
			this.SpecialTypes = new Dictionary<Type, WriteObjectDelegate>
        	{
        		{ typeof(Uri), Serializer.WriteObjectString },
        		{ typeof(Type), WriteType },
        		{ typeof(Exception), Serializer.WriteException },
        	};
		}

		public WriteObjectDelegate GetValueTypeToStringMethod(Type type)
		{
			if (type == typeof(byte) || type == typeof(byte?)
				|| type == typeof(short) || type == typeof(short?)
				|| type == typeof(ushort) || type == typeof(ushort?)
				|| type == typeof(int) || type == typeof(int?)
				|| type == typeof(uint) || type == typeof(uint?)
				|| type == typeof(long) || type == typeof(long?)
				|| type == typeof(ulong) || type == typeof(ulong?)
				)
				return Serializer.WriteInteger;

			if (type == typeof(bool) || type == typeof(bool?))
				return Serializer.WriteBool;

			if (type == typeof(DateTime))
				return Serializer.WriteDateTime;

			if (type == typeof(DateTime?))
				return Serializer.WriteNullableDateTime;

			if (type == typeof(Guid))
				return Serializer.WriteGuid;

			if (type == typeof(Guid?))
				return Serializer.WriteNullableGuid;

			if (type == typeof(float) || type == typeof(float?))
				return Serializer.WriteFloat;

			if (type == typeof(double) || type == typeof(double?))
				return Serializer.WriteDouble;

			if (type == typeof(decimal) || type == typeof(decimal?))
				return Serializer.WriteDecimal;

			return Serializer.WriteBuiltIn;
		}

		internal WriteObjectDelegate GetWriteFn<T>()
		{
			if (typeof(T) == typeof(string))
			{
				return Serializer.WriteObjectString;
			}

			if (typeof(T).IsValueType)
			{
				return GetValueTypeToStringMethod(typeof(T));
			}

			var specialWriteFn = GetSpecialWriteFn(typeof(T));
			if (specialWriteFn != null)
			{
				return specialWriteFn;
			}

			if (typeof(T).IsArray)
			{
				if (typeof(T) == typeof(byte[]))
					return WriteLists.WriteBytes;

				if (typeof(T) == typeof(string[]))
					return (w, x) => WriteLists.WriteStringArray(Serializer, w, x);

				if (typeof(T) == typeof(int[]))
					return WriteListsOfElements<int, TSerializer>.WriteGenericArrayValueType;
				if (typeof(T) == typeof(long[]))
					return WriteListsOfElements<long, TSerializer>.WriteGenericArrayValueType;

				var elementType = typeof(T).GetElementType();
				var writeFn = WriteListsOfElements<TSerializer>.GetGenericWriteArray(elementType);
				return writeFn;
			}

			if (typeof(T).IsGenericType())
			{
				if (typeof(T).IsOrHasGenericInterfaceTypeOf(typeof(IList<>)))
					return WriteLists<T, TSerializer>.Write;

				var mapInterface = typeof(T).GetTypeWithGenericTypeDefinitionOf(typeof(IDictionary<,>));
				if (mapInterface != null)
				{
					var mapTypeArgs = mapInterface.GetGenericArguments();
					var writeFn = WriteDictionary<TSerializer>.GetWriteGenericDictionary(
						mapTypeArgs[0], mapTypeArgs[1]);

					var keyWriteFn = Serializer.GetWriteFn(mapTypeArgs[0]);
					var valueWriteFn = Serializer.GetWriteFn(mapTypeArgs[1]);

					return (w, x) => writeFn(w, x, keyWriteFn, valueWriteFn);
				}

				var enumerableInterface = typeof(T).GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));
				if (enumerableInterface != null)
				{
					var elementType = enumerableInterface.GetGenericArguments()[0];
					var writeFn = WriteListsOfElements<TSerializer>.GetGenericWriteEnumerable(elementType);
					return writeFn;
				}
			}

			var isCollection = typeof(T).IsOrHasGenericInterfaceTypeOf(typeof(ICollection));
			if (isCollection)
			{
				var isDictionary = typeof(T).IsAssignableFrom(typeof(IDictionary))
					|| typeof(T).HasInterface(typeof(IDictionary));
				if (isDictionary)
				{
					return WriteDictionary<TSerializer>.WriteIDictionary;
				}

				return WriteListsOfElements<TSerializer>.WriteIEnumerable;
			}

			var isEnumerable = typeof(T).IsAssignableFrom(typeof(IEnumerable))
				|| typeof(T).HasInterface(typeof(IEnumerable));

			if (isEnumerable)
			{
				return WriteListsOfElements<TSerializer>.WriteIEnumerable;
			}

			if (typeof(T).IsClass || typeof(T).IsInterface)
			{
				var typeToStringMethod = WriteType<T, TSerializer>.Write;
				if (typeToStringMethod != null)
				{
					return typeToStringMethod;
				}
			}

			return Serializer.WriteBuiltIn;
		}


		public Dictionary<Type, WriteObjectDelegate> SpecialTypes;

		public WriteObjectDelegate GetSpecialWriteFn(Type type)
		{
			WriteObjectDelegate writeFn = null;
			if (SpecialTypes.TryGetValue(type, out writeFn))
				return writeFn;

			if (type.IsInstanceOfType(typeof(Type)))
				return WriteType;

			if (type.IsInstanceOf(typeof(Exception)))
				return Serializer.WriteException;

			return null;
		}

		public void WriteType(TextWriter writer, object value)
		{
			Serializer.WriteRawString(writer, ((Type)value).AssemblyQualifiedName);
		}

	}
}