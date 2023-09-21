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
using System.Runtime.Serialization;
using System.Threading;
using ServiceStack.Text.Json;

namespace ServiceStack.Text.Common;

internal static class DeserializeKeyValuePair<TSerializer>
    where TSerializer : ITypeSerializer
{
    private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

    const int KeyIndex = 0;
    const int ValueIndex = 1;

    public static ParseStringDelegate GetParseMethod(Type type) => v => GetParseStringSpanMethod(type)(v.AsSpan());

    public static ParseStringSpanDelegate GetParseStringSpanMethod(Type type)
    {
        var mapInterface = type.GetTypeWithGenericInterfaceOf(typeof(KeyValuePair<,>));

        var keyValuePairArgs = mapInterface.GetGenericArguments();
        var keyTypeParseMethod = Serializer.GetParseStringSpanFn(keyValuePairArgs[KeyIndex]);
        if (keyTypeParseMethod == null) return null;

        var valueTypeParseMethod = Serializer.GetParseStringSpanFn(keyValuePairArgs[ValueIndex]);
        if (valueTypeParseMethod == null) return null;

        var createMapType = type.HasAnyTypeDefinitionsOf(typeof(KeyValuePair<,>))
            ? null : type;

        return value => ParseKeyValuePairType(value, createMapType, keyValuePairArgs, keyTypeParseMethod, valueTypeParseMethod);
    }

    public static object ParseKeyValuePair<TKey, TValue>(
        string value, Type createMapType,
        ParseStringDelegate parseKeyFn, ParseStringDelegate parseValueFn) =>
        ParseKeyValuePair<TKey, TValue>(value.AsSpan(), createMapType,
            v => parseKeyFn(v.ToString()), v => parseValueFn(v.ToString()));

    public static object ParseKeyValuePair<TKey, TValue>(
        ReadOnlySpan<char> value, Type createMapType,
        ParseStringSpanDelegate parseKeyFn, ParseStringSpanDelegate parseValueFn)
    {
        if (value.IsEmpty) return default(KeyValuePair<TKey, TValue>);

        var index = VerifyAndGetStartIndex(value, createMapType);

        if (JsonTypeSerializer.IsEmptyMap(value, index)) return new KeyValuePair<TKey, TValue>();
        var keyValue = default(TKey);
        var valueValue = default(TValue);

        var valueLength = value.Length;
        while (index < valueLength)
        {
            var key = Serializer.EatMapKey(value, ref index);
            Serializer.EatMapKeySeperator(value, ref index);
            var keyElementValue = Serializer.EatTypeValue(value, ref index);

            if (key.CompareIgnoreCase("key".AsSpan()))
                keyValue = (TKey)parseKeyFn(keyElementValue);
            else if (key.CompareIgnoreCase("value".AsSpan()))
                valueValue = (TValue)parseValueFn(keyElementValue);
            else if (!key.SequenceEqual(JsConfig.TypeAttr.AsSpan()))
                throw new SerializationException("Incorrect KeyValuePair property: " + key.ToString());

            Serializer.EatItemSeperatorOrMapEndChar(value, ref index);
        }

        return new KeyValuePair<TKey, TValue>(keyValue, valueValue);
    }

    private static int VerifyAndGetStartIndex(ReadOnlySpan<char> value, Type createMapType)
    {
        var index = 0;
        if (!Serializer.EatMapStartChar(value, ref index))
        {
            //Don't throw ex because some KeyValueDataContractDeserializer don't have '{}'
            Tracer.Instance.WriteDebug("WARN: Map definitions should start with a '{0}', expecting serialized type '{1}', got string starting with: {2}",
                JsWriter.MapStartChar, createMapType != null ? createMapType.Name : "Dictionary<,>", value.Substring(0, value.Length < 50 ? value.Length : 50));
        }
        return index;
    }

    private static Dictionary<string, ParseKeyValuePairDelegate> ParseDelegateCache
        = new Dictionary<string, ParseKeyValuePairDelegate>();

    private delegate object ParseKeyValuePairDelegate(ReadOnlySpan<char> value, Type createMapType,
        ParseStringSpanDelegate keyParseFn, ParseStringSpanDelegate valueParseFn);

    public static object ParseKeyValuePairType(string value, Type createMapType, Type[] argTypes,
        ParseStringDelegate keyParseFn, ParseStringDelegate valueParseFn) =>
        ParseKeyValuePairType(value.AsSpan(), createMapType, argTypes,
            v => keyParseFn(v.ToString()), v => valueParseFn(v.ToString()));

    static readonly Type[] signature = { typeof(ReadOnlySpan<char>), typeof(Type), typeof(ParseStringSpanDelegate), typeof(ParseStringSpanDelegate) };

    public static object ParseKeyValuePairType(ReadOnlySpan<char> value, Type createMapType, Type[] argTypes,
        ParseStringSpanDelegate keyParseFn, ParseStringSpanDelegate valueParseFn)
    {
        var key = GetTypesKey(argTypes);
        if (ParseDelegateCache.TryGetValue(key, out var parseDelegate))
            return parseDelegate(value, createMapType, keyParseFn, valueParseFn);

        var mi = typeof(DeserializeKeyValuePair<TSerializer>).GetStaticMethod("ParseKeyValuePair", signature);
        var genericMi = mi.MakeGenericMethod(argTypes);
        parseDelegate = (ParseKeyValuePairDelegate)genericMi.MakeDelegate(typeof(ParseKeyValuePairDelegate));

        Dictionary<string, ParseKeyValuePairDelegate> snapshot, newCache;
        do
        {
            snapshot = ParseDelegateCache;
            newCache = new Dictionary<string, ParseKeyValuePairDelegate>(ParseDelegateCache);
            newCache[key] = parseDelegate;

        } while (!ReferenceEquals(
                     Interlocked.CompareExchange(ref ParseDelegateCache, newCache, snapshot), snapshot));

        return parseDelegate(value, createMapType, keyParseFn, valueParseFn);
    }

    private static string GetTypesKey(params Type[] types)
    {
        var sb = StringBuilderThreadStatic.Allocate();
        foreach (var type in types)
        {
            if (sb.Length > 0)
                sb.Append(">");

            sb.Append(type.FullName);
        }
        return StringBuilderThreadStatic.ReturnAndFree(sb);
    }
}