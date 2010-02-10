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
	}
}