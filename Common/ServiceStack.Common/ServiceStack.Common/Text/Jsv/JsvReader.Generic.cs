using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text.Jsv
{
	public static class JsvReader
	{
		private static readonly Dictionary<Type, Func<Func<string, object>>> ParseFnCache =
			new Dictionary<Type, Func<Func<string, object>>>();

		public static Func<string, object> GetParseMethod(Type type)
		{
			Func<Func<string, object>> parseFn;
			lock (ParseFnCache)
			{
				if (!ParseFnCache.TryGetValue(type, out parseFn))
				{
					var genericType = typeof(JsvReader<>).MakeGenericType(type);
					var mi = genericType.GetMethod("GetParseMethod",
						BindingFlags.Public | BindingFlags.Static);

					parseFn = (Func<Func<string, object>>)Delegate.CreateDelegate(
						typeof(Func<Func<string, object>>), mi);
					ParseFnCache.Add(type, parseFn);					
				}
			}
			return parseFn();
		}
	}

	public static class JsvReader<T>
	{
		private static readonly Func<string, object> ReadFn;

		static JsvReader()
		{
			ReadFn = GetParseMethod();
		}

		public static Func<string, object> GetParseMethod()
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
				return ParseStringArrayMethods.GetParseMethod(type);
			}

			var builtInMethod = ParseStringBuiltinMethods.GetParseMethod(type);
			if (builtInMethod != null)
				return builtInMethod;

			if (type.IsGenericType())
			{
				var listInterfaces = type.FindInterfaces(
					(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null);
				if (listInterfaces.Length > 0)
					return ParseStringListMethods.GetParseMethod(type);

				var mapInterfaces = type.FindInterfaces(
					(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>), null);
				if (mapInterfaces.Length > 0)
					return ParseStringDictionaryMethods.GetParseMethod(type);

				var collectionInterfaces = type.FindInterfaces(
					(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>), null);

				if (collectionInterfaces.Length > 0)
					return ParseStringCollectionMethods.GetParseMethod(type);
			}

			var staticParseMethod = ParseStringStaticParseMethod.GetParseMethod(type);
			if (staticParseMethod != null)
				return staticParseMethod;

			var typeConstructor = ParseStringTypeMethod.GetParseMethod(type);
			if (typeConstructor != null)
				return typeConstructor;

			var stringConstructor = ParseStringTypeConstructor.GetParseMethod(type);
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