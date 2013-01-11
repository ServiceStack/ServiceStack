using System;
using System.Collections.Generic;
#if WINDOWS_PHONE
using ServiceStack.Text.WP;
#endif

namespace ServiceStack.Common
{
    public static class EnumerableExtensions
    {
        public static bool IsEmpty<T>(this ICollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> items)
        {
            return new HashSet<T>(items);
        }

        public static List<To> SafeConvertAll<To, From>(this IEnumerable<From> items, Func<From, To> converter)
        {
            return items == null ? new List<To>() : Extensions.EnumerableExtensions.ConvertAll(items, converter);
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

        public static IEnumerable<T[]> BatchesOf<T>(this IEnumerable<T> sequence, int batchSize)
        {
            var batch = new List<T>(batchSize);
            foreach (var item in sequence)
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    yield return batch.ToArray();
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                yield return batch.ToArray();
                batch.Clear();
            }
        }

        public static Dictionary<TKey, T> ToSafeDictionary<T, TKey>(this IEnumerable<T> list, Func<T, TKey> expr)
        {
            var map = new Dictionary<TKey, T>();
            if (list != null)
            {
                foreach (var item in list)
                {
                    map[expr(item)] = item;
                }
            }
            return map;
        }

        public static Dictionary<TKey, TValue> ToDictionary<T, TKey, TValue>(this IEnumerable<T> list, Func<T, KeyValuePair<TKey, TValue>> map)
        {
            var to = new Dictionary<TKey, TValue>();
            foreach (var item in list)
            {
                var entry = map(item);
                to[entry.Key] = entry.Value;
            }
            return to;
        }

    }
}