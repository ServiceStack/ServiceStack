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
	internal static class DeserializeListWithElements<TSerializer>
		where TSerializer : ITypeSerializer
	{
		internal static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

		private static readonly Dictionary<Type, ParseListDelegate> ParseDelegateCache 
			= new Dictionary<Type, ParseListDelegate>();

		private delegate object ParseListDelegate(string value, Type createListType, ParseStringDelegate parseFn);

		public static Func<string, Type, ParseStringDelegate, object> GetListTypeParseFn(
			Type createListType, Type elementType,
			ParseStringDelegate parseFn)
		{
			ParseListDelegate parseDelegate;
			if (!ParseDelegateCache.TryGetValue(elementType, out parseDelegate))
			{
				var genericType = typeof(DeserializeListWithElements<,>)
					.MakeGenericType(elementType, typeof(TSerializer));

				var mi = genericType.GetMethod("ParseGenericList",
					BindingFlags.Static | BindingFlags.Public);

				parseDelegate = (ParseListDelegate)Delegate.CreateDelegate(typeof(ParseListDelegate), mi);

				ParseDelegateCache[elementType] = parseDelegate;
			}

			return parseDelegate.Invoke;
		}

		internal static string StripList(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;

			const int startQuotePos = 1;
			const int endQuotePos = 2;
			return value[0] == JsWriter.ListStartChar
			       	? value.Substring(startQuotePos, value.Length - endQuotePos)
			       	: value;
		}

		public static List<string> ParseStringList(string value)
		{
			if ((value = StripList(value)) == null) return null;
			if (value == string.Empty) return new List<string>();

			var to = new List<string>();
			var valueLength = value.Length;

			var i = 0;
			while (i < valueLength)
			{
				var elementValue = Serializer.EatValue(value, ref i);
				var listValue = Serializer.ParseString(elementValue);
				to.Add(listValue);
				Serializer.EatItemSeperatorOrMapEndChar(value, ref i);
			}

			return to;
		}

		public static List<int> ParseIntList(string value)
		{
			if ((value = StripList(value)) == null) return null;
			if (value == string.Empty) return new List<int>();

			var intParts = value.Split(JsWriter.ItemSeperator);
			var intValues = new List<int>(intParts.Length);
			foreach (var intPart in intParts)
			{
				intValues.Add(int.Parse(intPart));
			}
			return intValues;
		}
	}

	internal static class DeserializeListWithElements<T, TSerializer>
		where TSerializer : ITypeSerializer
	{
		private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

		public static IList<T> ParseGenericList(string value, Type createListType, ParseStringDelegate parseFn)
		{
			if ((value = DeserializeListWithElements<TSerializer>.StripList(value)) == null) return null;

			var to = (createListType == null)
			         	? new List<T>()
			         	: (IList<T>)ReflectionExtensions.CreateInstance(createListType);

			if (value == string.Empty) return to;

			if (!string.IsNullOrEmpty(value))
			{
				if (value[0] == JsWriter.MapStartChar)
				{
					var i = 0;
					do
					{
						var itemValue = Serializer.EatTypeValue(value, ref i);
						to.Add((T)parseFn(itemValue));
					} while (++i < value.Length);
				}
				else
				{
					var valueLength = value.Length;

					var i = 0;
					while (i < valueLength)
					{
						var elementValue = Serializer.EatValue(value, ref i);
						var listValue = Serializer.ParseString(elementValue);
						to.Add((T)parseFn(listValue));
						Serializer.EatItemSeperatorOrMapEndChar(value, ref i);
					}

				}
			}
			return to;
		}
	}

	internal static class DeserializeList<TSerializer>
		where TSerializer : ITypeSerializer
	{
		private static readonly Dictionary<Type, ParseStringDelegate> ParseDelegateCache 
			= new Dictionary<Type, ParseStringDelegate>();

		public static ParseStringDelegate GetParseFn(Type listType)
		{
			ParseStringDelegate parseFn;
			if (!ParseDelegateCache.TryGetValue(listType, out parseFn))
			{
				var genericType = typeof(DeserializeList<TSerializer>)
					.MakeGenericType(listType);

				var mi = genericType.GetMethod("GetParseFn",
					BindingFlags.Static | BindingFlags.Public);

				var parseFactoryFn = (Func<ParseStringDelegate>)Delegate.CreateDelegate(
					typeof(Func<ParseStringDelegate>), mi);

				parseFn = parseFactoryFn();
				ParseDelegateCache[listType] = parseFn;
			}

			return parseFn;
		}
	}

	internal static class DeserializeList<T, TSerializer>
		where TSerializer : ITypeSerializer
	{
		private readonly static ParseStringDelegate CacheFn;

		static DeserializeList()
		{
			CacheFn = GetParseFn();
		}

		public static ParseStringDelegate Parse
		{
			get { return CacheFn; }
		}

		public static ParseStringDelegate GetParseFn()
		{
			var listInterface = typeof(T).GetTypeWithGenericInterfaceOf(typeof(IList<>));
			if (listInterface == null)
				throw new ArgumentException(string.Format("Type {0} is not of type IList<>", typeof(T).FullName));

			//optimized access for regularly used types
			if (typeof(T) == typeof(List<string>))
				return DeserializeListWithElements<TSerializer>.ParseStringList;

			if (typeof(T) == typeof(List<int>))
				return DeserializeListWithElements<TSerializer>.ParseIntList;

			var elementType = listInterface.GetGenericArguments()[0];

			var supportedTypeParseMethod = DeserializeListWithElements<TSerializer>.Serializer.GetParseFn(elementType);
			if (supportedTypeParseMethod != null)
			{
				var createListType = typeof(T).HasAnyTypeDefinitionsOf(typeof(List<>), typeof(IList<>))
					? null : typeof(T);

				var parseFn = DeserializeListWithElements<TSerializer>.GetListTypeParseFn(createListType, elementType, supportedTypeParseMethod);
				return value => parseFn(value, createListType, supportedTypeParseMethod);
			}

			return null;
		}

	}

	internal static class DeserializeEnumerable<T, TSerializer>
		where TSerializer : ITypeSerializer
	{
		private readonly static ParseStringDelegate CacheFn;

		static DeserializeEnumerable()
		{
			CacheFn = GetParseFn();
		}

		public static ParseStringDelegate Parse
		{
			get { return CacheFn; }
		}

		public static ParseStringDelegate GetParseFn()
		{
			var enumerableInterface = typeof(T).GetTypeWithGenericInterfaceOf(typeof(IEnumerable<>));
			if (enumerableInterface == null)
				throw new ArgumentException(string.Format("Type {0} is not of type IEnumerable<>", typeof(T).FullName));

			//optimized access for regularly used types
			if (typeof(T) == typeof(IEnumerable<string>))
				return DeserializeListWithElements<TSerializer>.ParseStringList;

			if (typeof(T) == typeof(IEnumerable<int>))
				return DeserializeListWithElements<TSerializer>.ParseIntList;

			var elementType = enumerableInterface.GetGenericArguments()[0];

			var supportedTypeParseMethod = DeserializeListWithElements<TSerializer>.Serializer.GetParseFn(elementType);
			if (supportedTypeParseMethod != null)
			{
				const Type createListTypeWithNull = null;

				var parseFn = DeserializeListWithElements<TSerializer>.GetListTypeParseFn(
					createListTypeWithNull, elementType, supportedTypeParseMethod);

				return value => parseFn(value, createListTypeWithNull, supportedTypeParseMethod);
			}

			return null;
		}

	}
}