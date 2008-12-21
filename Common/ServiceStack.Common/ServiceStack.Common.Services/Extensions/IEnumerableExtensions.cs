using System;
using System.Collections.Generic;

namespace ServiceStack.Common.Services.Extensions
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
    }
}