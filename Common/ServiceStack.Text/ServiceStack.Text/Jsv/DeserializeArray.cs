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

namespace ServiceStack.Text.Jsv
{
	public static class DeserializeArrayWithElements
	{
		private static readonly Dictionary<Type, ParseArrayOfElementsDelegate> ParseDelegateCache 
			= new Dictionary<Type, ParseArrayOfElementsDelegate>();

		private delegate object ParseArrayOfElementsDelegate(string value, Func<string, object> parseFn);

		public static Func<string, Func<string, object>, object> GetParseFn(Type type)
		{
			ParseArrayOfElementsDelegate parseFn;
			if (!ParseDelegateCache.TryGetValue(type, out parseFn))
			{
				var genericType = typeof(DeserializeArrayWithElements<>).MakeGenericType(type);

				var mi = genericType.GetMethod("ParseGenericArray",
					BindingFlags.Public | BindingFlags.Static);

				parseFn = (ParseArrayOfElementsDelegate)Delegate.CreateDelegate(typeof(ParseArrayOfElementsDelegate), mi);
				ParseDelegateCache[type] = parseFn;
			}
			return parseFn.Invoke;
		}
	}

	public static class DeserializeArrayWithElements<T>
	{
		public static T[] ParseGenericArray(string value, Func<string, object> elementParseFn)
		{
			if ((value = DeserializeListWithElements.StripList(value)) == null) return null;
			if (value == string.Empty) return new T[0];

			if (value[0] == TypeSerializer.MapStartChar)
			{
				var itemValues = new List<string>();
				var i = 0;
				do
				{
					itemValues.Add(ParseUtils.EatTypeValue(value, ref i));
				} while (++i < value.Length);

				var results = new T[itemValues.Count];
				for (var j=0; j < itemValues.Count; j++)
				{
					results[j] = (T)elementParseFn(itemValues[j]);
				}
				return results;
			}
			else
			{
				var to = new List<T>();
				var valueLength = value.Length;
				for (var i=0; i < valueLength; i++)
				{
					var elementValue = ParseUtils.EatElementValue(value, ref i);
					var listValue = ParseUtils.ParseString(elementValue);
					to.Add((T)elementParseFn(listValue));
				}
				return to.ToArray();
			}
		}
	}

	public static class DeserializeArray
	{
		private static readonly Dictionary<Type, Func<string, object>> ParseDelegateCache 
			= new Dictionary<Type, Func<string, object>>();

		public static Func<string, object> GetParseFn(Type type)
		{
			Func<string, object> parseFn;
			if (!ParseDelegateCache.TryGetValue(type, out parseFn))
			{
				var genericType = typeof(DeserializeArray<>).MakeGenericType(type);

				var mi = genericType.GetMethod("GetParseFn", 
					BindingFlags.Public | BindingFlags.Static);

				var parseFactoryFn = (Func<Func<string, object>>)Delegate.CreateDelegate(
					typeof(Func<Func<string, object>>), mi);
				parseFn = parseFactoryFn();
				ParseDelegateCache[type] = parseFn;
			}

			return parseFn;
		}
	}

	public static class DeserializeArray<T>
	{
		private static readonly Func<string, object> CacheFn;

		static DeserializeArray()
		{
			CacheFn = GetParseFn();
		}

		public static Func<string, object> Parse
		{
			get { return CacheFn; }
		}

		public static Func<string, object> GetParseFn()
		{
			var type = typeof (T);
			if (!type.IsArray)
				throw new ArgumentException(string.Format("Type {0} is not an Array type", type.FullName));

			if (type == typeof(string[]))
				return ParseStringArray;
			if (type == typeof(byte[]))
				return ParseByteArray;

			var elementType = type.GetElementType();
			var elementParseFn = JsvReader.GetParseFn(elementType);
			if (elementParseFn != null)
			{
				var parseFn = DeserializeArrayWithElements.GetParseFn(elementType);
				return value => parseFn(value, elementParseFn);
			}
			return null;
		}

		public static string[] ParseStringArray(string value)
		{
			if ((value = DeserializeListWithElements.StripList(value)) == null) return null;
			return value == string.Empty
			       	? new string[0]
			       	: DeserializeListWithElements.ParseStringList(value).ToArray();
		}

		public static byte[] ParseByteArray(string value)
		{
			if ((value = DeserializeListWithElements.StripList(value)) == null) return null;
			return value == string.Empty
			       	? new byte[0]
			       	: Convert.FromBase64String(value);
		}
	}
}