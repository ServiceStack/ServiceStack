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
				  list.Add((To) item);
			  }
			  return list;
		  }
    }
}