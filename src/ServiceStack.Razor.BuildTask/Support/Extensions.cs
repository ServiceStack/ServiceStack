using System;
using System.Collections.Generic;

namespace ServiceStack
{
    // Some extension methods copied various parts of SS
    public static class Extensions
    {
        public static void Each<T>(this IEnumerable<T> values, Action<T> action)
        {
            if (values == null) return;

            foreach (var value in values)
            {
                action(value);
            }
        }

        public static string GetOperationName(this Type type)
        {
            return type.FullName != null //can be null, e.g. generic types
                ? type.FullName.Replace(type.Namespace + ".", "").Replace("+", ".")
                : type.Name;
        }
    }
}
