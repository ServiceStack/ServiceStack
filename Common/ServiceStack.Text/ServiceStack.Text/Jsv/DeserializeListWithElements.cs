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
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Jsv
{
	public static class DeserializeListWithElements
	{
		private static readonly Dictionary<Type, ParseListDelegate> ParseDelegateCache 
			= new Dictionary<Type, ParseListDelegate>();

		private delegate object ParseListDelegate(string value, Type createListType, Func<string, object> parseFn);

		public static Func<string, Type, Func<string, object>, object> GetListTypeParseFn(
			Type createListType, Type elementType,
			Func<string, object> parseFn)
		{
			ParseListDelegate parseDelegate;
			if (!ParseDelegateCache.TryGetValue(elementType, out parseDelegate))
			{
				var genericType = typeof(DeserializeListWithElements<>).MakeGenericType(elementType);

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
			for (var i=0; i < valueLength; i++)
			{
				var elementValue = ParseUtils.EatElementValue(value, ref i);
				var listValue = ParseUtils.ParseString(elementValue);
				to.Add(listValue);
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
	
	public static class DeserializeListWithElements<T>
	{
		public static IList<T> ParseGenericList(string value, Type createListType, Func<string, object> parseFn)
		{
			if ((value = DeserializeListWithElements.StripList(value)) == null) return null;

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
						var itemValue = ParseUtils.EatTypeValue(value, ref i);
						to.Add((T)parseFn(itemValue));
					} while (++i < value.Length);
				}
				else
				{
					var valueLength = value.Length;
					for (var i=0; i < valueLength; i++)
					{
						var elementValue = ParseUtils.EatElementValue(value, ref i);
						var listValue = ParseUtils.ParseString(elementValue);
						to.Add((T)parseFn(listValue));
					}
				}
			}
			return to;
		}
	}

	public static class DeserializeList
	{
		private static readonly Dictionary<Type, Func<string, object>> ParseDelegateCache 
			= new Dictionary<Type, Func<string, object>>();

		public static Func<string, object> GetParseFn(Type listType)
		{
			Func<string, object> parseFn;
			if (!ParseDelegateCache.TryGetValue(listType, out parseFn))
			{
				var genericType = typeof(DeserializeList<>).MakeGenericType(listType);

				var mi = genericType.GetMethod("GetParseFn",
					BindingFlags.Static | BindingFlags.Public);

				var parseFactoryFn = (Func<Func<string, object>>)Delegate.CreateDelegate(
					typeof(Func<Func<string, object>>), mi);

				parseFn = parseFactoryFn();
				ParseDelegateCache[listType] = parseFn;
			}

			return parseFn;
		}
	}

	public static class DeserializeList<T>
	{
		private readonly static Func<string, object> CacheFn;

		static DeserializeList()
		{
			CacheFn = GetParseFn();
		}

		public static Func<string, object> Parse
		{
			get { return CacheFn; }
		}

		public static Func<string, object> GetParseFn()
		{
			var type = typeof(T);

			var listInterface = type.GetTypeWithGenericInterfaceOf(typeof(IList<>));
			if (listInterface == null)
				throw new ArgumentException(string.Format("Type {0} is not of type IList<>", type.FullName));

			//optimized access for regularly used types
			if (type == typeof(List<string>))
				return DeserializeListWithElements.ParseStringList;

			if (type == typeof(List<int>))
				return DeserializeListWithElements.ParseIntList;

			var elementType = listInterface.GetGenericArguments()[0];

			var supportedTypeParseMethod = JsvReader.GetParseFn(elementType);
			if (supportedTypeParseMethod != null)
			{
				var createListType = type.HasAnyTypeDefinitionsOf(typeof(List<>), typeof(IList<>))
					? null : type;

				var parseFn = DeserializeListWithElements.GetListTypeParseFn(createListType, elementType, supportedTypeParseMethod);
				return value => parseFn(value, createListType, supportedTypeParseMethod);
			}

			return null;
		}

	}

	public static class DeserializeEnumerable<T>
	{
		private readonly static Func<string, object> CacheFn;

		static DeserializeEnumerable()
		{
			CacheFn = GetParseFn();
		}

		public static Func<string, object> Parse
		{
			get { return CacheFn; }
		}

		public static Func<string, object> GetParseFn()
		{
			var type = typeof(T);

			var enumerableInterface = type.GetTypeWithGenericInterfaceOf(typeof(IEnumerable<>));
			if (enumerableInterface == null)
				throw new ArgumentException(string.Format("Type {0} is not of type IEnumerable<>", type.FullName));

			//optimized access for regularly used types
			if (type == typeof(IEnumerable<string>))
				return DeserializeListWithElements.ParseStringList;

			if (type == typeof(IEnumerable<int>))
				return DeserializeListWithElements.ParseIntList;

			var elementType = enumerableInterface.GetGenericArguments()[0];

			var supportedTypeParseMethod = JsvReader.GetParseFn(elementType);
			if (supportedTypeParseMethod != null)
			{
				const Type createListTypeWithNull = null;

				var parseFn = DeserializeListWithElements.GetListTypeParseFn(createListTypeWithNull, elementType, supportedTypeParseMethod);
				return value => parseFn(value, createListTypeWithNull, supportedTypeParseMethod);
			}

			return null;
		}

	}

}