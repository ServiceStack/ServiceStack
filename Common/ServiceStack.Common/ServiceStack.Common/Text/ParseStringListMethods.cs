using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;

namespace ServiceStack.Common.Text
{
	public static class ParseStringListMethods
	{
		public static Func<string, object> GetParseMethod(Type type)
		{
			var listInterfaces = type.FindInterfaces(
				(t, critera) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null);

			if (listInterfaces.Length == 0)
				throw new ArgumentException(string.Format("Type {0} is not of type IList<>", type.FullName));

			//optimized access for regularly used types
			if (type == typeof(List<string>))
				return ParseStringList;

			if (type == typeof(List<int>))
				return ParseIntList;

			var elementType = listInterfaces[0].GetGenericArguments()[0];

			var supportedTypeParseMethod = ParseStringMethods.GetParseMethod(elementType);
			if (supportedTypeParseMethod != null)
			{
				var createListType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) ? null : type;
				return value => ParseListType(value, createListType, elementType, supportedTypeParseMethod);
			}

			return null;
		}

		private static string StripList(string value)
		{
			if (string.IsNullOrEmpty(value)) 
				return null;

			return value[0] == TextExtensions.ListStartChar 
				? value.Substring(1, value.Length - 2) 
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
				var elementValue = ParseStringMethods.EatValue(value, ref i);
				var listValue = ParseStringMethods.ParseString(elementValue);
				to.Add(listValue);
			}

			return to;
		}

		public static List<int> ParseIntList(string value)
		{
			if ((value = StripList(value)) == null) return null;
			if (value == string.Empty) return new List<int>();

			return value.Split(TextExtensions.ItemSeperator).ConvertAll(x => int.Parse(x));
		}

		public static IList<T> ParseList<T>(string value, Type createListType, Func<string, object> parseFn)
		{
			if ((value = StripList(value)) == null) return null;
			if (value == string.Empty) return new List<T>();

			var to = (createListType == null)
				? new List<T>()
				: (IList<T>)ReflectionUtils.CreateInstance(createListType);

			if (!string.IsNullOrEmpty(value))
			{
				if (value[0] == TextExtensions.TypeStartChar)
				{
					var i = 0;
					do
					{
						var itemValue = ParseStringMethods.EatTypeValue(value, ref i);
						to.Add((T) parseFn(itemValue));
					} while (++i < value.Length);
				}
				else
				{
					var valueLength = value.Length;
					for (var i=0; i < valueLength; i++)
					{
						var elementValue = ParseStringMethods.EatValue(value, ref i);
						var listValue = ParseStringMethods.ParseString(elementValue);
						to.Add((T)parseFn(listValue));
					}
				}
			}
			return to;
		}

		private static readonly Dictionary<Type, ParseListDelegate> ParseDelegateCache 
			= new Dictionary<Type, ParseListDelegate>();

		private delegate object ParseListDelegate(string value, Type createListType, Func<string, object> parseFn);

		public static object ParseListType(string value, Type createListType, Type elementType, Func<string, object> parseFn)
		{
			var mi = typeof(ParseStringListMethods).GetMethod("ParseList", BindingFlags.Static | BindingFlags.Public);
			ParseListDelegate parseDelegate;

			if (!ParseDelegateCache.TryGetValue(elementType, out parseDelegate))
			{
				var genericMi = mi.MakeGenericMethod(new[] { elementType });
				parseDelegate = (ParseListDelegate)Delegate.CreateDelegate(typeof(ParseListDelegate), genericMi);
				ParseDelegateCache[elementType] = parseDelegate;
			}

			return parseDelegate(value, createListType, parseFn);
		}
	}
}