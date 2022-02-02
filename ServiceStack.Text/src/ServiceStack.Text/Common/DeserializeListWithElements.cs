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
using System.Collections.ObjectModel;
using System.Threading;

namespace ServiceStack.Text.Common
{
    public static class DeserializeListWithElements<TSerializer>
        where TSerializer : ITypeSerializer
    {
        internal static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

        private static Dictionary<Type, ParseListDelegate> ParseDelegateCache
            = new Dictionary<Type, ParseListDelegate>();

        public delegate object ParseListDelegate(ReadOnlySpan<char> value, Type createListType, ParseStringSpanDelegate parseFn);

        public static Func<string, Type, ParseStringDelegate, object> GetListTypeParseFn(
            Type createListType, Type elementType, ParseStringDelegate parseFn)
        {
            var func = GetListTypeParseStringSpanFn(createListType, elementType, v => parseFn(v.ToString()));
            return (s, t, d) => func(s.AsSpan(), t, v => d(v.ToString()));
        }

        private static readonly Type[] signature = {typeof(ReadOnlySpan<char>), typeof(Type), typeof(ParseStringSpanDelegate)};

        public static ParseListDelegate GetListTypeParseStringSpanFn(
            Type createListType, Type elementType, ParseStringSpanDelegate parseFn)
        {
            if (ParseDelegateCache.TryGetValue(elementType, out var parseDelegate))
                return parseDelegate.Invoke;

            var genericType = typeof(DeserializeListWithElements<,>).MakeGenericType(elementType, typeof(TSerializer));
            var mi = genericType.GetStaticMethod("ParseGenericList", signature);
            parseDelegate = (ParseListDelegate)mi.MakeDelegate(typeof(ParseListDelegate));

            Dictionary<Type, ParseListDelegate> snapshot, newCache;
            do
            {
                snapshot = ParseDelegateCache;
                newCache = new Dictionary<Type, ParseListDelegate>(ParseDelegateCache) { [elementType] = parseDelegate };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref ParseDelegateCache, newCache, snapshot), snapshot));

            return parseDelegate.Invoke;
        }

        public static ReadOnlySpan<char> StripList(ReadOnlySpan<char> value)
        {
            if (value.IsNullOrEmpty())
                return default;

            value = value.Trim();

            const int startQuotePos = 1;
            const int endQuotePos = 2;
            var ret = value[0] == JsWriter.ListStartChar
                    ? value.Slice(startQuotePos, value.Length - endQuotePos)
                    : value;
            var val = ret.AdvancePastWhitespace();
            if (val.Length == 0)
                return TypeConstants.EmptyStringSpan;
            return val;
        }

        public static List<string> ParseStringList(string value)
        {
            return ParseStringList(value.AsSpan());
        }

        public static List<string> ParseStringList(ReadOnlySpan<char> value)
        {
            if ((value = StripList(value)).IsNullOrEmpty())
                return value.IsEmpty ? null : new List<string>();

            var to = new List<string>();
            var valueLength = value.Length;

            var i = 0;
            while (i < valueLength)
            {
                var elementValue = Serializer.EatValue(value, ref i);
                var listValue = Serializer.UnescapeString(elementValue);
                to.Add(listValue.Value());
                if (Serializer.EatItemSeperatorOrMapEndChar(value, ref i) && i == valueLength)
                {
                    // If we ate a separator and we are at the end of the value, 
                    // it means the last element is empty => add default
                    to.Add(null);
                }
            }

            return to;
        }

        public static List<int> ParseIntList(string value) => ParseIntList(value.AsSpan());

        public static List<int> ParseIntList(ReadOnlySpan<char> value)
        {
            if ((value = StripList(value)).IsNullOrEmpty()) 
                return value.IsEmpty ? null : new List<int>();

            var to = new List<int>();
            var valueLength = value.Length;

            var i = 0;
            while (i < valueLength)
            {
                var elementValue = Serializer.EatValue(value, ref i);
                to.Add(MemoryProvider.Instance.ParseInt32(elementValue));
                Serializer.EatItemSeperatorOrMapEndChar(value, ref i);
            }

            return to;
        }

        public static List<byte> ParseByteList(string value) => ParseByteList(value.AsSpan());

        public static List<byte> ParseByteList(ReadOnlySpan<char> value)
        {
            if ((value = StripList(value)).IsNullOrEmpty()) 
                return value.IsEmpty ? null : new List<byte>();

            var to = new List<byte>();
            var valueLength = value.Length;

            var i = 0;
            while (i < valueLength)
            {
                var elementValue = Serializer.EatValue(value, ref i);
                to.Add(MemoryProvider.Instance.ParseByte(elementValue));
                Serializer.EatItemSeperatorOrMapEndChar(value, ref i);
            }

            return to;
        }
    }

    public static class DeserializeListWithElements<T, TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

        public static ICollection<T> ParseGenericList(string value, Type createListType, ParseStringDelegate parseFn)
        {
            return ParseGenericList(value.AsSpan(), createListType, v => parseFn(v.ToString()));
        }

        public static ICollection<T> ParseGenericList(ReadOnlySpan<char> value, Type createListType, ParseStringSpanDelegate parseFn)
        {
            var isReadOnly = createListType != null && (createListType.IsGenericType && createListType.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>));
            var to = (createListType == null || isReadOnly)
                ? new List<T>()
                : (ICollection<T>)createListType.CreateInstance();

            var objSerializer = Json.JsonTypeSerializer.Instance.ObjectDeserializer;
            if (to is List<object> && objSerializer != null && typeof(TSerializer) == typeof(Json.JsonTypeSerializer))
                return (ICollection<T>)objSerializer(value);

            if ((value = DeserializeListWithElements<TSerializer>.StripList(value)).IsEmpty) 
                return null;

            if (value.IsNullOrEmpty())
                return isReadOnly ? (ICollection<T>)Activator.CreateInstance(createListType, to) : to;

            var tryToParseItemsAsPrimitiveTypes =
                typeof(T) == typeof(object) && JsConfig.TryToParsePrimitiveTypeValues;

            if (!value.IsNullOrEmpty())
            {
                var valueLength = value.Length;
                var i = 0;
                Serializer.EatWhitespace(value, ref i);
                if (i < valueLength && value[i] == JsWriter.MapStartChar)
                {
                    do
                    {
                        var itemValue = Serializer.EatTypeValue(value, ref i);
                        if (!itemValue.IsEmpty)
                        {
                            to.Add((T)parseFn(itemValue));
                        }
                        else
                        {
                            to.Add(default);
                        }
                        Serializer.EatWhitespace(value, ref i);
                    } while (++i < value.Length);
                }
                else
                {
                    
                    while (i < valueLength)
                    {
                        var startIndex = i;
                        var elementValue = Serializer.EatValue(value, ref i);
                        var listValue = elementValue;
                        var isEmpty = listValue.IsNullOrEmpty();
                        if (!isEmpty)
                        {
                            if (tryToParseItemsAsPrimitiveTypes)
                            {
                                Serializer.EatWhitespace(value, ref startIndex);
                                to.Add((T)DeserializeType<TSerializer>.ParsePrimitive(elementValue.Value(), value[startIndex]));
                            }
                            else
                            {
                                to.Add((T)parseFn(elementValue));
                            }
                        }

                        if (Serializer.EatItemSeperatorOrMapEndChar(value, ref i) && i == valueLength)
                        {
                            // If we ate a separator and we are at the end of the value, 
                            // it means the last element is empty => add default
                            to.Add(default);
                            continue;
                        }

                        if (isEmpty)
                            to.Add(default);
                    }

                }
            }

            //TODO: 8-10-2011 -- this CreateInstance call should probably be moved over to ReflectionExtensions, 
            //but not sure how you'd like to go about caching constructors with parameters -- I would probably build a NewExpression, .Compile to a LambdaExpression and cache
            return isReadOnly ? (ICollection<T>)Activator.CreateInstance(createListType, to) : to;
        }
    }

    public static class DeserializeList<T, TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ParseStringSpanDelegate CacheFn;

        static DeserializeList()
        {
            CacheFn = GetParseStringSpanFn();
        }

        public static ParseStringDelegate Parse => v => CacheFn(v.AsSpan());

        public static ParseStringSpanDelegate ParseStringSpan => CacheFn;

        public static ParseStringDelegate GetParseFn() => v => GetParseStringSpanFn()(v.AsSpan());

        public static ParseStringSpanDelegate GetParseStringSpanFn()
        {
            var listInterface = typeof(T).GetTypeWithGenericInterfaceOf(typeof(IList<>));
            if (listInterface == null)
                throw new ArgumentException($"Type {typeof(T).FullName} is not of type IList<>");

            //optimized access for regularly used types
            if (typeof(T) == typeof(List<string>))
                return DeserializeListWithElements<TSerializer>.ParseStringList;

            if (typeof(T) == typeof(List<int>))
                return DeserializeListWithElements<TSerializer>.ParseIntList;
            
            var elementType = listInterface.GetGenericArguments()[0];

            var supportedTypeParseMethod = DeserializeListWithElements<TSerializer>.Serializer.GetParseStringSpanFn(elementType);
            if (supportedTypeParseMethod != null)
            {
                var createListType = typeof(T).HasAnyTypeDefinitionsOf(typeof(List<>), typeof(IList<>))
                    ? null : typeof(T);

                var parseFn = DeserializeListWithElements<TSerializer>.GetListTypeParseStringSpanFn(createListType, elementType, supportedTypeParseMethod);
                return value => parseFn(value, createListType, supportedTypeParseMethod);
            }

            return null;
        }

    }

    internal static class DeserializeEnumerable<T, TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ParseStringSpanDelegate CacheFn;

        static DeserializeEnumerable()
        {
            CacheFn = GetParseStringSpanFn();
        }

        public static ParseStringDelegate Parse => v => CacheFn(v.AsSpan());

        public static ParseStringSpanDelegate ParseStringSpan => CacheFn;

        public static ParseStringDelegate GetParseFn() => v => GetParseStringSpanFn()(v.AsSpan());

        public static ParseStringSpanDelegate GetParseStringSpanFn()
        {
            var enumerableInterface = typeof(T).GetTypeWithGenericInterfaceOf(typeof(IEnumerable<>));
            if (enumerableInterface == null)
                throw new ArgumentException($"Type {typeof(T).FullName} is not of type IEnumerable<>");

            //optimized access for regularly used types
            if (typeof(T) == typeof(IEnumerable<string>))
                return DeserializeListWithElements<TSerializer>.ParseStringList;

            if (typeof(T) == typeof(IEnumerable<int>))
                return DeserializeListWithElements<TSerializer>.ParseIntList;

            var elementType = enumerableInterface.GetGenericArguments()[0];

            var supportedTypeParseMethod = DeserializeListWithElements<TSerializer>.Serializer.GetParseStringSpanFn(elementType);
            if (supportedTypeParseMethod != null)
            {
                const Type createListTypeWithNull = null; //Use conversions outside this class. see: Queue

                var parseFn = DeserializeListWithElements<TSerializer>.GetListTypeParseStringSpanFn(
                    createListTypeWithNull, elementType, supportedTypeParseMethod);

                return value => parseFn(value, createListTypeWithNull, supportedTypeParseMethod);
            }

            return null;
        }

    }
}