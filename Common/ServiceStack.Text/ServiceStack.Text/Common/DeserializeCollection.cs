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
using System.Collections.Generic;
using System.Reflection;

namespace ServiceStack.Text.Common
{
	internal static class DeserializeCollection<TSerializer>
		where TSerializer : ITypeSerializer
	{
		private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

		public static ParseStringDelegate GetParseMethod(Type type)
		{
			var collectionInterface = type.GetTypeWithGenericInterfaceOf(typeof(ICollection<>));
			if (collectionInterface == null)
				throw new ArgumentException(string.Format("Type {0} is not of type ICollection<>", type.FullName));

			//optimized access for regularly used types
			if (type.FindInterfaces((t, critera) => t == typeof(ICollection<string>), null).Length > 0)
				return value => ParseStringCollection(value, type);

			if (type.FindInterfaces((t, critera) => t == typeof(ICollection<int>), null).Length > 0)
				return value => ParseIntCollection(value, type);

			var elementType =  collectionInterface.GetGenericArguments()[0];

			var supportedTypeParseMethod = Serializer.GetParseFn(elementType);
			if (supportedTypeParseMethod != null)
			{
				var createCollectionType = type.HasAnyTypeDefinitionsOf(typeof(ICollection<>))
				                           	? null : type;

				return value => ParseCollectionType(value, createCollectionType, elementType, supportedTypeParseMethod);
			}

			return null;
		}

		public static ICollection<string> ParseStringCollection(string value, Type createType)
		{
			var items = DeserializeArrayWithElements<string, TSerializer>.ParseGenericArray(value, Serializer.ParseString);
			return CreateAndPopulate(createType, items);
		}

		public static ICollection<int> ParseIntCollection(string value, Type createType)
		{
			var items = DeserializeArrayWithElements<int, TSerializer>.ParseGenericArray(value, x => int.Parse(x));
			return CreateAndPopulate(createType, items);
		}

		public static ICollection<T> ParseCollection<T>(string value, Type createType, ParseStringDelegate parseFn)
		{
			if (value == null) return null;

			var items = DeserializeArrayWithElements<T, TSerializer>.ParseGenericArray(value, parseFn);
			return CreateAndPopulate(createType, items);
		}

		private static ICollection<T> CreateAndPopulate<T>(Type ofCollectionType, T[] withItems)
		{
			if (ofCollectionType == null) return new List<T>(withItems);

			var genericTypeDefinition = ofCollectionType.GetGenericTypeDefinition();
			if (genericTypeDefinition == typeof(HashSet<T>))
				return new HashSet<T>(withItems);
			if (genericTypeDefinition == typeof(LinkedList<T>))
				return new LinkedList<T>(withItems);

			var collection = (ICollection<T>)ReflectionExtensions.CreateInstance(ofCollectionType);
			foreach (var item in withItems)
			{
				collection.Add(item);
			}
			return collection;
		}

		private static readonly Dictionary<Type, ParseCollectionDelegate> ParseDelegateCache 
			= new Dictionary<Type, ParseCollectionDelegate>();

		private delegate object ParseCollectionDelegate(string value, Type createType, ParseStringDelegate parseFn);

		public static object ParseCollectionType(string value, Type createType, Type elementType, ParseStringDelegate parseFn)
		{
			var mi = typeof(DeserializeCollection<TSerializer>)
				.GetMethod("ParseCollection", BindingFlags.Static | BindingFlags.Public);

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