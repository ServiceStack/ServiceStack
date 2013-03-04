using System;
using System.Collections;
using System.Collections.Generic;
using Proxy = ServiceStack.Common;

namespace ServiceStack.Common.Extensions
{
    [Obsolete("Use ServiceStack.Common.ByteArrayExtensions")]
    public static class CollectionExtensions
    {
        public static bool IsEmpty<T>(this ICollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }
        
        public static List<To> ConvertAll<To>(this ICollection items, Func<object, To> converter)
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