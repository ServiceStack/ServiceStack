using System;
using System.Collections;
using System.Collections.Generic;

using Proxy = ServiceStack.Common.AssertExtensions;

namespace ServiceStack.Common.Extensions
{
    [Obsolete("Use ServiceStack.Common.AssertExtensions")]
    public static class AssertExtensions
    {
        public static void ThrowOnFirstNull(params object[] objs)
        {
            Proxy.ThrowOnFirstNull(objs);
        }

        public static void ThrowIfNull(this object obj)
        {
            Proxy.ThrowIfNull(obj);
        }

        public static void ThrowIfNull(this object obj, string varName)
        {
            Proxy.ThrowIfNull(obj, varName);
        }

        public static void ThrowIfNullOrEmpty(this string strValue)
        {
            Proxy.ThrowIfNullOrEmpty(strValue);
        }

        public static void ThrowIfNullOrEmpty(this string strValue, string varName)
        {
            Proxy.ThrowIfNullOrEmpty(strValue, varName);
        }

        public static void ThrowIfNullOrEmpty(this ICollection collection)
        {
            Proxy.ThrowIfNullOrEmpty(collection);
        }

        public static void ThrowIfNullOrEmpty(this ICollection collection, string varName)
        {
            Proxy.ThrowIfNullOrEmpty(collection, varName);
        }

        public static void ThrowIfNullOrEmpty<T>(this ICollection<T> collection)
        {
            Proxy.ThrowIfNullOrEmpty(collection);
        }

        public static void ThrowIfNullOrEmpty<T>(this ICollection<T> collection, string varName)
        {
            Proxy.ThrowIfNullOrEmpty(collection, varName);
        }
         
    }
}