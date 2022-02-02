using System;
using System.Globalization;
using System.IO;

namespace ServiceStack.Text
{
    /// <summary>
    /// Reusable StringWriter ThreadStatic Cache
    /// </summary>
    public static class StringWriterCache
    {
        [ThreadStatic]
        static StringWriter cache;

        public static StringWriter Allocate()
        {
            var ret = cache;
            if (ret == null)
                return new StringWriter(CultureInfo.InvariantCulture);

            var sb = ret.GetStringBuilder();
            sb.Length = 0;
            cache = null;  //don't re-issue cached instance until it's freed
            return ret;
        }

        public static void Free(StringWriter writer)
        {
            cache = writer;
        }

        public static string ReturnAndFree(StringWriter writer)
        {
            var ret = writer.ToString();
            cache = writer;
            return ret;
        }
    }

    /// <summary>
    /// Alternative Reusable StringWriter ThreadStatic Cache
    /// </summary>
    public static class StringWriterCacheAlt
    {
        [ThreadStatic]
        static StringWriter cache;

        public static StringWriter Allocate()
        {
            var ret = cache;
            if (ret == null)
                return new StringWriter(CultureInfo.InvariantCulture);

            var sb = ret.GetStringBuilder();
            sb.Length = 0;
            cache = null;  //don't re-issue cached instance until it's freed
            return ret;
        }

        public static void Free(StringWriter writer)
        {
            cache = writer;
        }

        public static string ReturnAndFree(StringWriter writer)
        {
            var ret = writer.ToString();
            cache = writer;
            return ret;
        }
    }

    //Use separate cache internally to avoid reallocations and cache misses
    internal static class StringWriterThreadStatic
    {
        [ThreadStatic]
        static StringWriter cache;

        public static StringWriter Allocate()
        {
            var ret = cache;
            if (ret == null)
                return new StringWriter(CultureInfo.InvariantCulture);

            var sb = ret.GetStringBuilder();
            sb.Length = 0;
            cache = null;  //don't re-issue cached instance until it's freed
            return ret;
        }

        public static void Free(StringWriter writer)
        {
            cache = writer;
        }

        public static string ReturnAndFree(StringWriter writer)
        {
            var ret = writer.ToString();
            cache = writer;
            return ret;
        }
    }
}