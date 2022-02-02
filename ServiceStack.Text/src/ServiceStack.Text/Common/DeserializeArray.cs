//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections.Generic;
using System.Threading;

namespace ServiceStack.Text.Common
{
    public static class DeserializeArrayWithElements<TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static Dictionary<Type, ParseArrayOfElementsDelegate> ParseDelegateCache
            = new Dictionary<Type, ParseArrayOfElementsDelegate>();

        public delegate object ParseArrayOfElementsDelegate(ReadOnlySpan<char> value, ParseStringSpanDelegate parseFn);

        public static Func<string, ParseStringDelegate, object> GetParseFn(Type type)
        {
            var func = GetParseStringSpanFn(type);
            return (s, d) => func(s.AsSpan(), v => d(v.ToString()));
        }

        private static readonly Type[] signature = {typeof(ReadOnlySpan<char>), typeof(ParseStringSpanDelegate)};

        public static ParseArrayOfElementsDelegate GetParseStringSpanFn(Type type)
        {
            if (ParseDelegateCache.TryGetValue(type, out var parseFn)) return parseFn.Invoke;

            var genericType = typeof(DeserializeArrayWithElements<,>).MakeGenericType(type, typeof(TSerializer));
            var mi = genericType.GetStaticMethod("ParseGenericArray", signature);
            parseFn = (ParseArrayOfElementsDelegate)mi.CreateDelegate(typeof(ParseArrayOfElementsDelegate));

            Dictionary<Type, ParseArrayOfElementsDelegate> snapshot, newCache;
            do
            {
                snapshot = ParseDelegateCache;
                newCache = new Dictionary<Type, ParseArrayOfElementsDelegate>(ParseDelegateCache);
                newCache[type] = parseFn;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref ParseDelegateCache, newCache, snapshot), snapshot));

            return parseFn.Invoke;
        }
    }

    public static class DeserializeArrayWithElements<T, TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

        public static T[] ParseGenericArray(string value, ParseStringDelegate elementParseFn) =>
            ParseGenericArray(value.AsSpan(), v => elementParseFn(v.ToString()));

        public static T[] ParseGenericArray(ReadOnlySpan<char> value, ParseStringSpanDelegate elementParseFn)
        {
            if ((value = DeserializeListWithElements<TSerializer>.StripList(value)).IsNullOrEmpty()) 
                return value.IsEmpty ? null : new T[0];

            if (value[0] == JsWriter.MapStartChar)
            {
                var itemValues = new List<T>();
                var i = 0;
                do
                {
                    var spanValue = Serializer.EatTypeValue(value, ref i);
                    itemValues.Add((T)elementParseFn(spanValue));
                    Serializer.EatItemSeperatorOrMapEndChar(value, ref i);
                } while (i < value.Length);

                return itemValues.ToArray();
            }
            else
            {
                var to = new List<T>();
                var valueLength = value.Length;

                var i = 0;
                while (i < valueLength)
                {
                    var elementValue = Serializer.EatValue(value, ref i);
                    var listValue = elementValue;
                    to.Add((T)elementParseFn(listValue));
                    if (Serializer.EatItemSeperatorOrMapEndChar(value, ref i)
                        && i == valueLength)
                    {
                        // If we ate a separator and we are at the end of the value, 
                        // it means the last element is empty => add default
                        to.Add(default(T));
                    }
                }

                return to.ToArray();
            }
        }
    }

    internal static class DeserializeArray<TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static Dictionary<Type, ParseStringSpanDelegate> ParseDelegateCache = new Dictionary<Type, ParseStringSpanDelegate>();

        public static ParseStringDelegate GetParseFn(Type type) => v => GetParseStringSpanFn(type)(v.AsSpan());

        public static ParseStringSpanDelegate GetParseStringSpanFn(Type type)
        {
            if (ParseDelegateCache.TryGetValue(type, out var parseFn)) return parseFn;

            var genericType = typeof(DeserializeArray<,>).MakeGenericType(type, typeof(TSerializer));

            var mi = genericType.GetStaticMethod("GetParseStringSpanFn");
            var parseFactoryFn = (Func<ParseStringSpanDelegate>)mi.MakeDelegate(
                typeof(Func<ParseStringSpanDelegate>));
            parseFn = parseFactoryFn();

            Dictionary<Type, ParseStringSpanDelegate> snapshot, newCache;
            do
            {
                snapshot = ParseDelegateCache;
                newCache = new Dictionary<Type, ParseStringSpanDelegate>(ParseDelegateCache) {[type] = parseFn};

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref ParseDelegateCache, newCache, snapshot), snapshot));

            return parseFn;
        }
    }

    internal static class DeserializeArray<T, TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

        private static readonly ParseStringSpanDelegate CacheFn;

        static DeserializeArray()
        {
            CacheFn = GetParseStringSpanFn();
        }

        public static ParseStringDelegate Parse => v => CacheFn(v.AsSpan());

        public static ParseStringSpanDelegate ParseStringSpan => CacheFn;

        public static ParseStringDelegate GetParseFn() => v => GetParseStringSpanFn()(v.AsSpan());

        public static ParseStringSpanDelegate GetParseStringSpanFn()
        {
            var type = typeof(T);
            if (!type.IsArray)
                throw new ArgumentException($"Type {type.FullName} is not an Array type");

            if (type == typeof(string[]))
                return ParseStringArray;
            if (type == typeof(byte[]))
                return v => ParseByteArray(v.ToString());

            var elementType = type.GetElementType();
            var elementParseFn = Serializer.GetParseStringSpanFn(elementType);
            if (elementParseFn != null)
            {
                var parseFn = DeserializeArrayWithElements<TSerializer>.GetParseStringSpanFn(elementType);
                return value => parseFn(value, elementParseFn);
            }
            return null;
        }

        public static string[] ParseStringArray(ReadOnlySpan<char> value)
        {
            if ((value = DeserializeListWithElements<TSerializer>.StripList(value)).IsNullOrEmpty()) 
                return value.IsEmpty ? null : TypeConstants.EmptyStringArray;
            return DeserializeListWithElements<TSerializer>.ParseStringList(value).ToArray();
        }

        public static string[] ParseStringArray(string value) => ParseStringArray(value.AsSpan());

        public static byte[] ParseByteArray(string value) => ParseByteArray(value.AsSpan());

        public static byte[] ParseByteArray(ReadOnlySpan<char> value)
        {
            var isArray = value.Length > 1 && value[0] == '[';
            
            if ((value = DeserializeListWithElements<TSerializer>.StripList(value)).IsNullOrEmpty()) 
                return value.IsEmpty ? null : TypeConstants.EmptyByteArray;

            if ((value = Serializer.UnescapeString(value)).IsNullOrEmpty()) 
                return TypeConstants.EmptyByteArray;
            
            return !isArray 
                ? value.ParseBase64()
                : DeserializeListWithElements<TSerializer>.ParseByteList(value).ToArray();
        }
    }
}