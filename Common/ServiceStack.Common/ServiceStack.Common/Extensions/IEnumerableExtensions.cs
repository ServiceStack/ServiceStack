using System;
using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.Common.Extensions
{
	public static class IEnumerableExtensions
	{
		public static IEnumerable<To> ConvertAll<To, From>(this IEnumerable<From> items, Func<From, To> converter)
		{
			var list = new List<To>();
			foreach (var item in items)
			{
				list.Add(converter(item));
			}
			return list;
		}

		public static List<To> ToList<To>(this IEnumerable items)
		{
			var list = new List<To>();
			foreach (var item in items)
			{
				list.Add((To)item);
			}
			return list;
		}

		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> items)
		{
			return new HashSet<T>(items);
		}

		public static string FirstNonDefaultOrEmpty(this IEnumerable<string> values)
		{
			foreach (var value in values)
			{
				if (!string.IsNullOrEmpty(value)) return value;
			}
			return null;
		}

		public static T FirstNonDefault<T>(this IEnumerable<T> values)
		{
			foreach (var value in values)
			{
				if (!Equals(value, default(T))) return value;
			}
			return default(T);
		}

	}
}