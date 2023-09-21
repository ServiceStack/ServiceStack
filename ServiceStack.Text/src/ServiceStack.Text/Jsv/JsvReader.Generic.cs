//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Jsv;

public static class JsvReader
{
    internal static readonly JsReader<JsvTypeSerializer> Instance = new();

    private static Dictionary<Type, ParseFactoryDelegate> ParseFnCache = new();

    public static ParseStringDelegate GetParseFn(Type type) => v => GetParseStringSpanFn(type)(v.AsSpan());

    public static ParseStringSpanDelegate GetParseSpanFn(Type type) => v => GetParseStringSpanFn(type)(v);

    public static ParseStringSpanDelegate GetParseStringSpanFn(Type type)
    {
        ParseFnCache.TryGetValue(type, out var parseFactoryFn);

        if (parseFactoryFn != null) return parseFactoryFn();

        var genericType = typeof(JsvReader<>).MakeGenericType(type);
        var mi = genericType.GetStaticMethod(nameof(GetParseStringSpanFn));
        parseFactoryFn = (ParseFactoryDelegate)mi.MakeDelegate(typeof(ParseFactoryDelegate));

        Dictionary<Type, ParseFactoryDelegate> snapshot, newCache;
        do
        {
            snapshot = ParseFnCache;
            newCache = new Dictionary<Type, ParseFactoryDelegate>(ParseFnCache) {
                [type] = parseFactoryFn
            };

        } while (!ReferenceEquals(
                     Interlocked.CompareExchange(ref ParseFnCache, newCache, snapshot), snapshot));

        return parseFactoryFn();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void InitAot<T>()
    {
        Text.Jsv.JsvReader.Instance.GetParseFn<T>();
        Text.Jsv.JsvReader<T>.Parse(default(ReadOnlySpan<char>));
        Text.Jsv.JsvReader<T>.GetParseFn();
        Text.Jsv.JsvReader<T>.GetParseStringSpanFn();
    }
}

internal static class JsvReader<T>
{
    private static ParseStringSpanDelegate ReadFn;

    static JsvReader()
    {
        Refresh();
    }

    public static void Refresh()
    {
        JsConfig.InitStatics();

        if (JsvReader.Instance == null)
            return;

        ReadFn = JsvReader.Instance.GetParseStringSpanFn<T>();
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
            if (typeof(T).IsInterface)
            {
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