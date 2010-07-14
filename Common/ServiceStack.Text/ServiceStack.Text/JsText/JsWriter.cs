using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text.JsText
{
	public class JsWriter<TSerializer>
		where TSerializer : ITypeSerializer, new()
	{
		private readonly ITypeSerializer serializer;

		public JsWriter()
		{
			this.serializer = new TSerializer();
			this.SpecialTypes = new Dictionary<Type, Action<TextWriter, object>>
	  		{
	  			{ typeof(Uri), serializer.WriteObjectString },
	  			{ typeof(Type), WriteType },
	  			{ typeof(Exception), serializer.WriteException },
	  		};
		}

		public void WriteString(TextWriter writer, object value)
		{
			writer.Write(((string)value).ToCsvField());
		}

		public Action<TextWriter, object> GetValueTypeToStringMethod(Type type)
		{
			if (type == typeof(DateTime))
				return serializer.WriteDateTime;

			if (type == typeof(DateTime?))
				return serializer.WriteNullableDateTime;

			if (type == typeof(Guid))
				return serializer.WriteGuid;

			if (type == typeof(Guid?))
				return serializer.WriteNullableGuid;

			if (type == typeof(float) || type == typeof(float?))
				return serializer.WriteFloat;

			if (type == typeof(double) || type == typeof(double?))
				return serializer.WriteDouble;

			if (type == typeof(decimal) || type == typeof(decimal?))
				return serializer.WriteDecimal;

			return serializer.WriteBuiltIn;
		}

		public Action<TextWriter, object> GetWriteFn<T>()
		{
			if (typeof(T) == typeof(string))
			{
				return WriteString;
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
					return (w, x) => WriteLists.WriteStringArray(serializer, w, x);

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
					return WriteLists<T, TSerializer>.Write;

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
				var typeToStringMethod = WriteType<T, TSerializer>.Write;
				if (typeToStringMethod != null)
				{
					return typeToStringMethod;
				}
			}

			return serializer.WriteBuiltIn;
		}


		public Dictionary<Type, Action<TextWriter, object>> SpecialTypes;

		public Action<TextWriter, object> GetSpecialWriteFn(Type type)
		{
			Action<TextWriter, object> writeFn = null;
			if (SpecialTypes.TryGetValue(type, out writeFn))
				return writeFn;

			if (type.IsInstanceOfType(typeof(Type)))
				return WriteType;

			if (type.IsInstanceOf(typeof(Exception)))
				return serializer.WriteException;

			return null;
		}

		public Func<string, object> GetSpecialParseMethod(Type type)
		{
			if (type == typeof(Uri))
				return x => new Uri(x.FromCsvField());

			//Warning: typeof(object).IsInstanceOfType(typeof(Type)) == True??
			if (type.IsInstanceOfType(typeof(Type)))
				return ParseType;

			if (type == typeof(Exception))
				return x => new Exception(x);

			if (type.IsInstanceOf(typeof(Exception)))
				return DeserializeTypeUtils.GetParseMethod(type);

			return null;
		}

		public Type ParseType(string assemblyQualifiedName)
		{
			return Type.GetType(assemblyQualifiedName.FromCsvField());
		}

		public void WriteType(TextWriter writer, object value)
		{
			serializer.WriteRawString(writer, ((Type)value).AssemblyQualifiedName);
		}

	}
}