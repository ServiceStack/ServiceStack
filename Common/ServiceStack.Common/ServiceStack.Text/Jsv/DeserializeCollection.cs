using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceStack.Text.Jsv
{
	public static class DeserializeCollection
	{
		public static Func<string, object> GetParseMethod(Type type)
		{
			var collectionInterfaces = type.FindInterfaces(
				(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>), null);

			if (collectionInterfaces.Length == 0)
				throw new ArgumentException(string.Format("Type {0} is not of type ICollection<>", type.FullName));

			//optimized access for regularly used types
			if (type.FindInterfaces((t, critera) => t == typeof(ICollection<string>), null).Length > 0)
				return value => ParseStringCollection(value, type);

			if (type.FindInterfaces((t, critera) => t == typeof(ICollection<int>), null).Length > 0)
				return value => ParseIntCollection(value, type);

			var elementType =  collectionInterfaces[0].GetGenericArguments()[0];

			var supportedTypeParseMethod = JsvReader.GetParseFn(elementType);
			if (supportedTypeParseMethod != null)
			{
				return value => ParseCollectionType(value, type, elementType, supportedTypeParseMethod);
			}

			return null;
		}

		public static ICollection<string> ParseStringCollection(string value, Type createType)
		{
			var collection = (ICollection<string>)ReflectionExtensions.CreateInstance(createType);
			collection.CopyTo(DeserializeArrayWithElements<string>.ParseGenericArray(value, ParseUtils.ParseString), 0);
			return collection;
		}

		public static ICollection<int> ParseIntCollection(string value, Type createType)
		{
			var collection = (ICollection<int>)ReflectionExtensions.CreateInstance(createType);
			collection.CopyTo(DeserializeArrayWithElements<int>.ParseGenericArray(value, x => int.Parse(x)), 0);
			return collection;
		}

		public static ICollection<T> ParseCollection<T>(string value, Type createType, Func<string, object> parseFn)
		{
			if (value == null) return null;
			var collection = (ICollection<T>)ReflectionExtensions.CreateInstance(createType);
			collection.CopyTo(DeserializeArrayWithElements<T>.ParseGenericArray(value, parseFn), 0);
			return collection;
		}

		private static readonly Dictionary<Type, ParseCollectionDelegate> ParseDelegateCache 
			= new Dictionary<Type, ParseCollectionDelegate>();

		private delegate object ParseCollectionDelegate(string value, Type createType, Func<string, object> parseFn);

		public static object ParseCollectionType(string value, Type createType, Type elementType, Func<string, object> parseFn)
		{
			var mi = typeof(DeserializeCollection).GetMethod("ParseCollection", BindingFlags.Static | BindingFlags.Public);
			ParseCollectionDelegate parseDelegate;

			if (!ParseDelegateCache.TryGetValue(elementType, out parseDelegate))
			{
				var genericMi = mi.MakeGenericMethod(new[] { elementType });
				parseDelegate = (ParseCollectionDelegate) Delegate.CreateDelegate(typeof(ParseCollectionDelegate), genericMi);
				ParseDelegateCache[elementType] = parseDelegate;
			}

			return parseDelegate(value, createType, parseFn);
		}
	}
}