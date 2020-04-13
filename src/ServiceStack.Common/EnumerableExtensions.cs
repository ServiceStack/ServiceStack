using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack
{
    public static class EnumerableUtils
    {
        public static object FirstOrDefault(IEnumerable items)
        {
            if (items == null)
                return null;
            foreach (var item in items)
            {
                return item;
            }
            return null;
        }

        public static object ElementAt(IEnumerable items, int index)
        {
            if (items == null)
                return null;
            var i = 0;
            foreach (var item in items)
                if (i++ == index)
                    return item;
            return null;
        }

        public static List<object> Skip(IEnumerable items, int count)
        {
            if (items == null)
                return TypeConstants.EmptyObjectList;
            var to = new List<object>();
            int i = 0;
            foreach (var item in items)
            {
                if (count > i++)
                    continue;

                to.Add(item);
            }
            return to;
        }

        public static List<object> SplitOnFirst(IEnumerable items, out object first)
        {
            first = null;
            if (items == null)
                return TypeConstants.EmptyObjectList;
            var to = new List<object>();
            int i = 0;
            foreach (var item in items)
            {
                if (i++ < 1)
                {
                    first = item;
                    continue;
                }
                to.Add(item);
            }
            return to;
        }

        public static List<object> Take(IEnumerable items, int count)
        {
            if (items == null)
                return TypeConstants.EmptyObjectList;
            var to = new List<object>();
            int i = 0;
            foreach (var item in items)
            {
                if (count > i++)
                {
                    to.Add(item);
                    continue;
                }
                return to;
            }
            return to;
        }

        public static int Count(IEnumerable items)
        {
            if (items == null)
                return 0;
            return items is ICollection c
                ? c.Count
                : items.Cast<object>().Count();
        }

        public static List<object> ToList(IEnumerable items)
        {
            if (items == null)
                return TypeConstants.EmptyObjectList;
            var to = new List<object>();
            foreach (var item in items)
            {
                to.Add(item);
            }
            return to;
        }

        public static IEnumerable NullIfEmpty(IEnumerable items)
        {
            if (items != null)
            {
                foreach (var item in items)
                    return items;
            }
            return null;
        }
    }
    
    public static class EnumerableExtensions
    {
        public static bool IsEmpty<T>(this ICollection<T> collection) => collection == null || collection.Count == 0;

        public static bool IsEmpty<T>(this T[] collection) => collection == null || collection.Length == 0;

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> items) => new HashSet<T>(items);

        public static void Each<T>(this IEnumerable<T> values, Action<T> action)
        {
            if (values == null) return;

            foreach (var value in values)
            {
                action(value);
            }
        }

        public static void Each<T>(this IEnumerable<T> values, Action<int, T> action)
        {
            if (values == null) return;

            var i = 0;
            foreach (var value in values)
            {
                action(i++, value);
            }
        }

        public static void Each<TKey, TValue>(this IDictionary<TKey, TValue> map, Action<TKey, TValue> action)
        {
            if (map == null) return;

            var keys = map.Keys.ToList();
            foreach (var key in keys)
            {
                action(key, map[key]);
            }
        }

        public static List<To> Map<To, From>(this IEnumerable<From> items, Func<From, To> converter)
        {
            if (items == null)
                return new List<To>();

            var list = new List<To>();
            foreach (var item in items)
            {
                list.Add(converter(item));
            }
            return list;
        }

        public static List<To> Map<To>(this System.Collections.IEnumerable items, Func<object, To> converter)
        {
            if (items == null)
                return new List<To>();

            var list = new List<To>();
            foreach (var item in items)
            {
                list.Add(converter(item));
            }
            return list;
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

        public static bool EquivalentTo(this byte[] bytes, byte[] other)
        {
            var compare = 0;
            for (var i = 0; i < other.Length; i++)
                compare |= other[i] ^ bytes[i];

            return compare == 0;
        }

        public static bool EquivalentTo<T>(this T[] array, T[] otherArray, Func<T, T, bool> comparer = null)
        {
            if (array == null || otherArray == null)
                return array == otherArray;

            if (array.Length != otherArray.Length)
                return false;

            if (comparer == null)
                comparer = (v1, v2) => v1.Equals(v2);

            for (var i = 0; i < array.Length; i++)
            {
                if (!comparer(array[i], otherArray[i]))
                    return false;
            }

            return true;
        }

        public static bool EquivalentTo<T>(this IEnumerable<T> thisList, IEnumerable<T> otherList, Func<T, T, bool> comparer = null)
        {
            if (comparer == null)
                comparer = (v1, v2) => v1.Equals(v2);

            if (thisList == null || otherList == null)
                return thisList == otherList;

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

                if (!comparer(item, otherEnum.Current)) return false;
            }
            var hasNoMoreLeftAsWell = !otherEnum.MoveNext();
            return hasNoMoreLeftAsWell;
        }

        public static bool EquivalentTo<K, V>(this IDictionary<K, V> a, IDictionary<K, V> b, Func<V,V,bool> comparer = null)
        {
            if (comparer == null)
                comparer = (v1, v2) => v1.Equals(v2);

            if (a == null || b == null)
                return a == b;

            if (a.Count != b.Count)
                return false;

            foreach (var entry in a)
            {
                V value;
                if (!b.TryGetValue(entry.Key, out value))
                    return false;
                if (entry.Value == null || value == null)
                {
                    if (entry.Value == null && value == null)
                        continue;

                    return false;
                }
                if (!comparer(entry.Value, value))
                    return false;
            }
            return true;
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

        /// <summary>
        /// Return T[0] when enumerable is null, safe to use in enumerations like foreach
        /// </summary>
        public static IEnumerable<T> Safe<T>(this IEnumerable<T> enumerable) => enumerable ?? TypeConstants<T>.EmptyArray;

        public static IEnumerable Safe(this IEnumerable enumerable) => enumerable ?? TypeConstants.EmptyObjectArray;
    }
}