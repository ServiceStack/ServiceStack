using System;
using System.Collections;
using System.Collections.Generic;

namespace ServiceStack
{
    public static class AssertExtensions
    {
        public static void ThrowOnFirstNull(params object[] objs)
        {
            foreach (var obj in objs)
            {
                ThrowIfNull(obj);
            }
        }

        public static void ThrowIfNull(this object obj)
        {
            ThrowIfNull(obj, null);
        }

        public static void ThrowIfNull(this object obj, string varName)
        {
            if (obj == null)
                throw new ArgumentNullException(varName ?? "object");
        }

        public static T ThrowIfNull<T>(this T obj, string varName)
        {
            if (obj == null)
                throw new ArgumentNullException(varName ?? "object");

            return obj;
        }

        public static string ThrowIfNullOrEmpty(this string strValue)
        {
            return ThrowIfNullOrEmpty(strValue, null);
        }

        public static string ThrowIfNullOrEmpty(this string strValue, string varName)
        {
            if (string.IsNullOrEmpty(strValue))
                throw new ArgumentNullException(varName ?? "string");

            return strValue;
        }

        public static ICollection ThrowIfNullOrEmpty(this ICollection collection)
        {
            ThrowIfNullOrEmpty(collection, null);

            return collection;
        }

        public static ICollection ThrowIfNullOrEmpty(this ICollection collection, string varName)
        {
            var fieldName = varName ?? "collection";

            if (collection == null)
                throw new ArgumentNullException(fieldName);

            if (collection.Count == 0)
                throw new ArgumentException(fieldName + " is empty");

            return collection;
        }

        public static ICollection<T> ThrowIfNullOrEmpty<T>(this ICollection<T> collection)
        {
            ThrowIfNullOrEmpty(collection, null);

            return collection;
        }

        public static ICollection<T> ThrowIfNullOrEmpty<T>(this ICollection<T> collection, string varName)
        {
            var fieldName = varName ?? "collection";

            if (collection == null)
                throw new ArgumentNullException(fieldName);

            if (collection.Count == 0)
                throw new ArgumentException(fieldName + " is empty");

            return collection;
        }
    }
}