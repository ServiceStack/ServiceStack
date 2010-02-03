using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceStack.Text.Jsv
{
	public static class JsvReader
	{
		private static readonly Dictionary<Type, ParseFactoryDelegate> ParseFnCache =
			new Dictionary<Type, ParseFactoryDelegate>();

		public static Func<string, object> GetParseFn(Type type)
		{
			ParseFactoryDelegate parseFactoryFn;
			lock (ParseFnCache)
			{
				if (!ParseFnCache.TryGetValue(type, out parseFactoryFn))
				{
					var genericType = typeof(JsvReader<>).MakeGenericType(type);
					var mi = genericType.GetMethod("GetParseFn",
						BindingFlags.Public | BindingFlags.Static);

					parseFactoryFn = (ParseFactoryDelegate)Delegate.CreateDelegate(
						typeof(ParseFactoryDelegate), mi);

					ParseFnCache.Add(type, parseFactoryFn);					
				}
			}
			return parseFactoryFn();
		}
	}

	public static class JsvReader<T>
	{
		private static readonly Func<string, object> ReadFn;

		static JsvReader()
		{
			ReadFn = GetParseFn();
		}

		public static Func<string, object> GetParseFn()
		{
			var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

			if (type.IsEnum)
			{
				return x => Enum.Parse(type, x);
			}

			if (type == typeof(string))
				return x => x.FromCsvField();

			if (type == typeof(object))
				return x => x;

			if (type.IsEnum)
				return x => Enum.Parse(type, x);

			if (type.IsArray)
			{
				return DeserializeArray<T>.Parse;
			}

			var builtInMethod = DeserializeBuiltin<T>.Parse;
			if (builtInMethod != null)
				return builtInMethod;

			if (type.IsGenericType())
			{
				var listInterfaces = type.FindInterfaces(
					(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null);
				if (listInterfaces.Length > 0)
					return DeserializeList<T>.Parse;

				var mapInterfaces = type.FindInterfaces(
					(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>), null);
				if (mapInterfaces.Length > 0)
					return DeserializeDictionary.GetParseMethod(type);

				var collectionInterfaces = type.FindInterfaces(
					(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>), null);

				if (collectionInterfaces.Length > 0)
					return DeserializeCollection.GetParseMethod(type);
			}

			var staticParseMethod = StaticParseMethod<T>.Parse;
			if (staticParseMethod != null)
				return staticParseMethod;

			var typeConstructor = DeserializeType.GetParseMethod(type);
			if (typeConstructor != null)
				return typeConstructor;

			var stringConstructor = DeserializeTypeUtils.GetParseMethod(type);
			if (stringConstructor != null) return stringConstructor;

			return null;
		}

		public static object Parse(string value)
		{
			return value == null 
			       	? null 
			       	: ReadFn(value);
		}
	}
}