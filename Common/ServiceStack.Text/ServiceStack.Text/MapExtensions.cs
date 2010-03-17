using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Text
{
	public static class MapExtensions
	{
		public static string Join<K, V>(this Dictionary<K, V> values)
		{
			return Join(values, TypeSerializer.ItemSeperatorString, TypeSerializer.MapKeySeperatorString);
		}

		public static string Join<K, V>(this Dictionary<K, V> values, string itemSeperator, string keySeperator)
		{
			var sb = new StringBuilder();
			foreach (var entry in values)
			{
				if (sb.Length > 0)
					sb.Append(itemSeperator);

				sb.Append(entry.Key).Append(keySeperator).Append(entry.Value);
			}
			return sb.ToString();
		}
	}
}