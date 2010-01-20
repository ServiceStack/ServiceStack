using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Common.Utils;

namespace ServiceStack.Common.Text
{
	public static class ParseStringCollectionMethods
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

			var supportedTypeParseMethod = ParseStringMethods.GetParseMethod(elementType);
			if (supportedTypeParseMethod != null)
			{
				return value => ParseCollectionType(value, type, elementType, supportedTypeParseMethod);
			}

			return null;
		}

		public static ICollection<string> ParseStringCollection(string value, Type createType)
		{
			var collection = (ICollection<string>)ReflectionUtils.CreateInstance(createType);
			collection.CopyTo(ParseStringArrayMethods.ParseArray<string>(value, ParseStringMethods.ParseString), 0);
			return collection;
		}

		public static ICollection<int> ParseIntCollection(string value, Type createType)
		{
			var collection = (ICollection<int>)ReflectionUtils.CreateInstance(createType);
			collection.CopyTo(ParseStringArrayMethods.ParseArray<int>(value, x => int.Parse(x)), 0);
			return collection;
		}

		public static ICollection<T> ParseCollection<T>(string value, Type createType, Func<string, object> parseFn)
		{
			var collection = (ICollection<T>)ReflectionUtils.CreateInstance(createType);
			collection.CopyTo(ParseStringArrayMethods.ParseArray<T>(value, parseFn), 0);
			return collection;
		}

		private static readonly Dictionary<Type, ParseCollectionDelegate> ParseDelegateCache 
			= new Dictionary<Type, ParseCollectionDelegate>();

		private delegate object ParseCollectionDelegate(string value, Type createType, Func<string, object> parseFn);

		public static object ParseCollectionType(string value, Type createType, Type elementType, Func<string, object> parseFn)
		{
			var mi = typeof(ParseStringCollectionMethods).GetMethod("ParseCollection", BindingFlags.Static | BindingFlags.Public);
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