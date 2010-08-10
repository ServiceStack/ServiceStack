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
using System.Linq;
using System.Text;
using ServiceStack.Text.Common;

namespace ServiceStack.Text
{
	public static class ListExtensions
	{
		public static string Join<T>(this IEnumerable<T> values)
		{
			return Join(values, JsWriter.ItemSeperatorString);
		}

		public static string Join<T>(this IEnumerable<T> values, string seperator)
		{
			var sb = new StringBuilder();
			foreach (var value in values)
			{
				if (sb.Length > 0)
					sb.Append(seperator);
				sb.Append(value);
			}
			return sb.ToString();
		}

		public static List<T> ConvertEachTo<T>(this IEnumerable<string> list)
		{
			var to = new List<T>();
			foreach (var item in list)
			{
				to.Add(TypeSerializer.DeserializeFromString<T>(item));
			}
			return to;
		}

		public static Dictionary<TKey, TValue> ConvertEachTo<TKey, TValue>(this IDictionary<string, string> map)
		{
			var to = new Dictionary<TKey, TValue>();
			foreach (var item in map)
			{
				to[item.Key.DeserializeFromString<TKey>()] = item.Value.DeserializeFromString<TValue>();
			}
			return to;
		}

		public static bool IsNullOrEmpty<T>(this List<T> list)
		{
			return list == null || list.Count == 0;
		}

		//TODO: make it work
		public static IEnumerable<TFrom> SafeWhere<TFrom>(this List<TFrom> list, Func<TFrom, bool> predicate)
		{
			return list.Where(predicate);
		}

		public static int NullableCount<T>(this List<T> list)
		{
			return list == null ? 0 : list.Count;
		}

		public static void AddIfNotExists<T>(this List<T> list, T item)
		{
			if (!list.Contains(item))
				list.Add(item);
		}
	}
}