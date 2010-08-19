using System;
using System.Collections.Generic;

namespace ServiceStack.Text.Common
{
	internal class JsReader<TSerializer>
		where TSerializer : ITypeSerializer
	{
		private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

		public ParseStringDelegate GetParseFn<T>()
		{
			var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

			if (type.IsEnum)
			{
				return x => Enum.Parse(type, x, false);
			}

			if (type == typeof(string))
				return Serializer.ParseString;

			if (type == typeof(object))
				return x => x;

			var specialParseFn = ParseUtils.GetSpecialParseMethod(type);
			if (specialParseFn != null)
				return specialParseFn;

			if (type.IsEnum)
				return x => Enum.Parse(type, x, false);

			if (type.IsArray)
			{
				return DeserializeArray<T, TSerializer>.Parse;
			}

			var builtInMethod = DeserializeBuiltin<T>.Parse;
			if (builtInMethod != null)
				return value => builtInMethod(Serializer.ParseRawString(value));

			if (type.IsGenericType())
			{
				if (type.IsOrHasGenericInterfaceTypeOf(typeof(IList<>)))
					return DeserializeList<T, TSerializer>.Parse;

				if (type.IsOrHasGenericInterfaceTypeOf(typeof(IDictionary<,>)))
					return DeserializeDictionary<TSerializer>.GetParseMethod(type);

				if (type.IsOrHasGenericInterfaceTypeOf(typeof(ICollection<>)))
					return DeserializeCollection<TSerializer>.GetParseMethod(type);

				if (type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>)))
					return DeserializeEnumerable<T, TSerializer>.Parse;
			}

			var staticParseMethod = StaticParseMethod<T>.Parse;
			if (staticParseMethod != null)
				return value => staticParseMethod(Serializer.ParseRawString(value));

			var typeConstructor = DeserializeType<TSerializer>.GetParseMethod(type);
			if (typeConstructor != null)
				return typeConstructor;

			var stringConstructor = DeserializeTypeUtils.GetParseMethod(type);
			if (stringConstructor != null) return stringConstructor;

			return null;
		}
		
	}
}