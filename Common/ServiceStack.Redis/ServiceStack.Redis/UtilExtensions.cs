using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
	internal static class UtilExtensions
	{
		public static List<T> ConvertEachTo<T>(this IEnumerable<string> list)
		{
			var to = new List<T>();
			foreach (var item in list)
			{
				to.Add(JsonSerializer.DeserializeFromString<T>(item));
			}
			return to;
		}
	} 
}
