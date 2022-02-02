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
    internal static class DeserializeCollection<TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

        public static ParseStringDelegate GetParseMethod(Type type) => v => GetParseStringSpanMethod(type)(v.AsSpan());

        public static ParseStringSpanDelegate GetParseStringSpanMethod(Type type)
        {
            var collectionInterface = type.GetTypeWithGenericInterfaceOf(typeof(ICollection<>));
            if (collectionInterface == null)
                throw new ArgumentException(string.Format("Type {0} is not of type ICollection<>", type.FullName));

            //optimized access for regularly used types
            if (type.HasInterface(typeof(ICollection<string>)))
                return value => ParseStringCollection(value, type);

            if (type.HasInterface(typeof(ICollection<int>)))
                return value => ParseIntCollection(value, type);

            var elementType = collectionInterface.GetGenericArguments()[0];
            var supportedTypeParseMethod = Serializer.GetParseStringSpanFn(elementType);
            if (supportedTypeParseMethod != null)
            {
                var createCollectionType = type.HasAnyTypeDefinitionsOf(typeof(ICollection<>))
                    ? null : type;

                return value => ParseCollectionType(value, createCollectionType, elementType, supportedTypeParseMethod);
            }

            return null;
        }

        public static ICollection<string> ParseStringCollection(string value, Type createType) => ParseStringCollection(value.AsSpan(), createType);

        public static ICollection<string> ParseStringCollection(ReadOnlySpan<char> value, Type createType)
        {
            var items = DeserializeArrayWithElements<string, TSerializer>.ParseGenericArray(value, Serializer.ParseString);
            return CollectionExtensions.CreateAndPopulate(createType, items);
        }

        public static ICollection<int> ParseIntCollection(string value, Type createType) => ParseIntCollection(value.AsSpan(), createType);

        public static ICollection<int> ParseIntCollection(ReadOnlySpan<char> value, Type createType)
        {
            var items = DeserializeArrayWithElements<int, TSerializer>.ParseGenericArray(value, x => x.ParseInt32());
            return CollectionExtensions.CreateAndPopulate(createType, items);
        }

        public static ICollection<T> ParseCollection<T>(string value, Type createType, ParseStringDelegate parseFn) =>
            ParseCollection<T>(value.AsSpan(), createType, v => parseFn(v.ToString()));

        public static ICollection<T> ParseCollection<T>(ReadOnlySpan<char> value, Type createType, ParseStringSpanDelegate parseFn)
        {
            if (value.IsEmpty) return null;

            var items = DeserializeArrayWithElements<T, TSerializer>.ParseGenericArray(value, parseFn);
            return CollectionExtensions.CreateAndPopulate(createType, items);
        }

        private static Dictionary<Type, ParseCollectionDelegate> ParseDelegateCache
            = new Dictionary<Type, ParseCollectionDelegate>();

        private delegate object ParseCollectionDelegate(ReadOnlySpan<char> value, Type createType, ParseStringSpanDelegate parseFn);

        public static object ParseCollectionType(string value, Type createType, Type elementType, ParseStringDelegate parseFn) =>
            ParseCollectionType(value.AsSpan(), createType, elementType, v => parseFn(v.ToString()));


        static Type[] arguments = { typeof (ReadOnlySpan<char>), typeof(Type), typeof(ParseStringSpanDelegate) };
        
        public static object ParseCollectionType(ReadOnlySpan<char> value, Type createType, Type elementType, ParseStringSpanDelegate parseFn)
        {
            if (ParseDelegateCache.TryGetValue(elementType, out var parseDelegate))
                return parseDelegate(value, createType, parseFn);

            var mi = typeof(DeserializeCollection<TSerializer>).GetStaticMethod("ParseCollection", arguments);
            var genericMi = mi.MakeGenericMethod(new[] { elementType });
            parseDelegate = (ParseCollectionDelegate)genericMi.MakeDelegate(typeof(ParseCollectionDelegate));

            Dictionary<Type, ParseCollectionDelegate> snapshot, newCache;
            do
            {
                snapshot = ParseDelegateCache;
                newCache = new Dictionary<Type, ParseCollectionDelegate>(ParseDelegateCache);
                newCache[elementType] = parseDelegate;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref ParseDelegateCache, newCache, snapshot), snapshot));

            return parseDelegate(value, createType, parseFn);
        }
    }
}