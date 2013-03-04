using System;
using System.Collections;
using System.Collections.Generic;
#if WINDOWS_PHONE
using ServiceStack.Text.WP;
#endif

using Proxy = ServiceStack.Common.EnumerableExtensions;

namespace ServiceStack.Common.Extensions
{
    /// <summary>
    /// These extensions have a potential to conflict with the LINQ extensions methods so
    /// leaving the implmentation in the 'Extensions' sub-namespace to force explicit opt-in
    /// </summary>
    [Obsolete("Use ServiceStack.Common.EnumerableExtensions")]
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> values, Action<T> action)
        {
            foreach (var value in values)
            {
                action(value);
            }
        }

        public static List<To> ConvertAll<To>(this IEnumerable items, Func<object, To> converter)
        {
            var list = new List<To>();
            foreach (var item in items)
            {
                list.Add(converter(item));
            }
            return list;
        }

        public static object First(this IEnumerable items)
        {
            foreach (var item in items)
            {
                return item;
            }
            return null;
        }

        public static List<To> ToList<To>(this IEnumerable items)
        {
            var list = new List<To>();
            foreach (var item in items)
            {
                list.Add((To)item);
            }
            return list;
        }

        public static List<To> ConvertAll<To, From>(this IEnumerable<From> items, Func<From, To> converter)
        {
            var list = new List<To>();
            foreach (var item in items)
            {
                list.Add(converter(item));
            }
            return list;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> items)
        {
            return Proxy.ToHashSet(items);
        }

        public static List<To> SafeConvertAll<To, From>(this IEnumerable<From> items, Func<From, To> converter)
        {
            return Proxy.SafeConvertAll(items, converter);
        }

        public static List<object> ToObjects<T>(this IEnumerable<T> items)
        {
            var to = new List<object>();
            foreach (var item in items)
            {
                to.Add(item);
            }
            return to;
        }

        public static string FirstNonDefaultOrEmpty(this IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return null;
        }

        public static T FirstNonDefault<T>(this IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                if (!Equals(value, default(T))) return value;
            }
            return default(T);
        }

        public static bool EquivalentTo<T>(this IEnumerable<T> thisList, IEnumerable<T> otherList)
        {
            if (thisList == null || otherList == null) return thisList == otherList;

            var otherEnum = otherList.GetEnumerator();
            foreach (var item in thisList)
            {
                if (!otherEnum.MoveNext()) return false;
                
                var thisIsDefault = Equals(item, default(T));
                var otherIsDefault = Equals(otherEnum.Current, default(T));
                if (thisIsDefault || otherIsDefault)
                {
                    return thisIsDefault && otherIsDefault;
                }
                
                if (!item.Equals(otherEnum.Current)) return false;
            }
            var hasNoMoreLeftAsWell = !otherEnum.MoveNext();
            return hasNoMoreLeftAsWell;
        }

    }
}