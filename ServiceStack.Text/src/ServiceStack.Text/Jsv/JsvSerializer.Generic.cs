//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Jsv
{
    internal class JsvSerializer<T>
    {
        Dictionary<Type, ParseStringDelegate> DeserializerCache = new Dictionary<Type, ParseStringDelegate>();

        public T DeserializeFromString(string value, Type type)
        {
            if (DeserializerCache.TryGetValue(type, out var parseFn)) return (T)parseFn(value);

            var genericType = typeof(T).MakeGenericType(type);
            var mi = genericType.GetMethodInfo("DeserializeFromString", new[] { typeof(string) });
            parseFn = (ParseStringDelegate)mi.MakeDelegate(typeof(ParseStringDelegate));

            Dictionary<Type, ParseStringDelegate> snapshot, newCache;
            do
            {
                snapshot = DeserializerCache;
                newCache = new Dictionary<Type, ParseStringDelegate>(DeserializerCache);
                newCache[type] = parseFn;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref DeserializerCache, newCache, snapshot), snapshot));

            return (T)parseFn(value);
        }

        public T DeserializeFromString(string value)
        {
            if (typeof(T) == typeof(string)) return (T)(object)value;

            return (T)JsvReader<T>.Parse(value);
        }

        public void SerializeToWriter(T value, TextWriter writer)
        {
            JsvWriter<T>.WriteObject(writer, value);
        }

        public string SerializeToString(T value)
        {
            if (value == null) return null;
            if (value is string) return value as string;

            var writer = StringWriterThreadStatic.Allocate();
            JsvWriter<T>.WriteObject(writer, value);
            return StringWriterThreadStatic.ReturnAndFree(writer);
        }
    }
}