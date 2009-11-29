using System;
using System.Collections.Generic;

namespace ServiceStack.Common.Extensions
{
	public static class TypeExtensions
	{
		private static readonly Dictionary<Type, List<string>> TypePropertyNamesMap = new Dictionary<Type, List<string>>();

		public static List<string> GetPropertyNames(this Type type)
		{
			lock (TypePropertyNamesMap)
			{
				List<string> propertyNames;
				if (!TypePropertyNamesMap.TryGetValue(type, out propertyNames))
				{
					propertyNames = type.GetProperties().ConvertAll(x => x.Name);
					TypePropertyNamesMap[type] = propertyNames;
				}
				return propertyNames;
			}
		}
	}
}