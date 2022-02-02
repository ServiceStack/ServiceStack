using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
	internal static class UtilExtensions
	{
		public static List<T> ConvertEachTo<T>(this List<string> list)
		{
			var to = new List<T>(list.Count);
			foreach (var item in list)
			{
				to.Add(JsonSerializer.DeserializeFromString<T>(item));
			}
			return to;
		}
	} 
}
