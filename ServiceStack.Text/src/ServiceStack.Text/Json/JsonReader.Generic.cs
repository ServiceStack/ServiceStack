//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Json;

public static class JsonReader
{
    public static readonly JsReader<JsonTypeSerializer> Instance = new();

    private static Dictionary<Type, ParseFactoryDelegate> ParseFnCache = new();

    internal static ParseStringDelegate GetParseFn(Type type) => v => GetParseStringSpanFn(type)(v.AsSpan());

    internal static ParseStringSpanDelegate GetParseSpanFn(Type type) => v => GetParseStringSpanFn(type)(v);

    internal static ParseStringSpanDelegate GetParseStringSpanFn(Type type)
    {
        ParseFnCache.TryGetValue(type, out var parseFactoryFn);

        if (parseFactoryFn != null)
            return parseFactoryFn();

        var genericType = typeof(JsonReader<>).MakeGenericType(type);
        var mi = genericType.GetStaticMethod(nameof(GetParseStringSpanFn));    
        parseFactoryFn = (ParseFactoryDelegate)mi.MakeDelegate(typeof(ParseFactoryDelegate));

        Dictionary<Type, ParseFactoryDelegate> snapshot, newCache;
        do
        {
            snapshot = ParseFnCache;
            newCache = new Dictionary<Type, ParseFactoryDelegate>(ParseFnCache)
            {
                [type] = parseFactoryFn
            };

        } while (!ReferenceEquals(
                     Interlocked.CompareExchange(ref ParseFnCache, newCache, snapshot), snapshot));

        return parseFactoryFn();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void InitAot<T>()
    {
        Text.Json.JsonReader.Instance.GetParseFn<T>();
        Text.Json.JsonReader<T>.Parse(TypeConstants.NullStringSpan);
        Text.Json.JsonReader<T>.GetParseFn();
        Text.Json.JsonReader<T>.GetParseStringSpanFn();
    }
}

internal static class JsonReader<T>
{
    private static ParseStringSpanDelegate ReadFn;

    static JsonReader()
    {
        Refresh();
    }

    public static void Refresh()
    {
        JsConfig.InitStatics();

        if (JsonReader.Instance == null)
            return;

        ReadFn = JsonReader.Instance.GetParseStringSpanFn<T>();
        JsConfig.AddUniqueType(typeof(T));
    }

    public static ParseStringDelegate GetParseFn() => ReadFn != null 
        ? (ParseStringDelegate)(v => ReadFn(v.AsSpan())) 
        : Parse;

    public static ParseStringSpanDelegate GetParseStringSpanFn() => ReadFn ?? Parse;

    public static object Parse(string value) => value != null 
        ? Parse(value.AsSpan())
        : null;

    public static object Parse(ReadOnlySpan<char> value)
    {
        TypeConfig<T>.Init();

        value = value.WithoutBom();
            
        if (ReadFn == null)
        {
            if (typeof(T).IsAbstract || typeof(T).IsInterface)
            {
                if (value.IsNullOrEmpty()) return null;
                var concreteType = DeserializeType<JsonTypeSerializer>.ExtractType(value);
                if (concreteType != null)
                {
                    return JsonReader.GetParseStringSpanFn(concreteType)(value);
                }
                throw new NotSupportedException("Can not deserialize interface type: "
                                                + typeof(T).Name);
            }

            Refresh();
        }

        return !value.IsEmpty
            ? ReadFn(value)
            : null;
    }
}