using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Text
{
	public static class ListExtensions
	{
		public static string Join<T>(this IEnumerable<T> values)
		{
			return Join(values, TypeSerializer.ItemSeperatorString);
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
	}
}