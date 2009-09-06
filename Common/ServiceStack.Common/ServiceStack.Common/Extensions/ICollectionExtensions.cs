using System.Collections.Generic;

namespace ServiceStack.Common.Extensions
{
	public static class CollectionExtensions
	{
		public static bool IsEmpty<T>(this ICollection<T> collection)
		{
			return collection == null || collection.Count == 0;
		}

	}
}