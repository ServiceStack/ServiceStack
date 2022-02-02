//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace ServiceStack
{
    public static class ListExtensions
    {
        public static string Join<T>(this IEnumerable<T> values)
        {
            return Join(values, JsWriter.ItemSeperatorString);
        }

        public static string Join<T>(this IEnumerable<T> values, string seperator)
        {
            var sb = StringBuilderThreadStatic.Allocate();
            foreach (var value in values)
            {
                if (sb.Length > 0)
                    sb.Append(seperator);
                sb.Append(value);
            }
            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }

        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        //TODO: make it work
        public static IEnumerable<TFrom> SafeWhere<TFrom>(this List<TFrom> list, Func<TFrom, bool> predicate)
        {
            return list.Where(predicate);
        }

        public static int NullableCount<T>(this List<T> list)
        {
            return list == null ? 0 : list.Count;
        }

        public static void AddIfNotExists<T>(this List<T> list, T item)
        {
            if (!list.Contains(item))
                list.Add(item);
        }

        public static T[] NewArray<T>(this T[] array, T with = null, T without = null) where T : class
        {
            var to = new List<T>(array);

            if (with != null)
                to.Add(with);

            if (without != null)
                to.Remove(without);

            return to.ToArray();
        }

        public static List<T> InList<T>(this T value)
        {
            return new List<T> { value };
        }

        public static T[] InArray<T>(this T value)
        {
            return new[] { value };
        }

        public static List<Type> Add<T>(this List<Type> types)
        {
            types.Add(typeof(T));
            return types;
        }
    }
}