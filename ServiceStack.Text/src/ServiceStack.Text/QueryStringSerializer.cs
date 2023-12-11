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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;
using ServiceStack.Text.Jsv;

namespace ServiceStack;

public static class QueryStringSerializer
{
    static QueryStringSerializer()
    {
        JsConfig.InitStatics();
        Instance = new JsWriter<JsvTypeSerializer>();
    }

    internal static readonly JsWriter<JsvTypeSerializer> Instance;

    private static Dictionary<Type, WriteObjectDelegate> WriteFnCache = new();

    public static WriteComplexTypeDelegate ComplexTypeStrategy { get; set; }

    internal static WriteObjectDelegate GetWriteFn(Type type)
    {
        try
        {
            if (WriteFnCache.TryGetValue(type, out var writeFn)) return writeFn;

            var genericType = typeof(QueryStringWriter<>).MakeGenericType(type);
            var mi = genericType.GetStaticMethod("WriteFn");
            var writeFactoryFn = (Func<WriteObjectDelegate>)mi.MakeDelegate(
                typeof(Func<WriteObjectDelegate>));

            writeFn = writeFactoryFn();

            Dictionary<Type, WriteObjectDelegate> snapshot, newCache;
            do
            {
                snapshot = WriteFnCache;
                newCache = new Dictionary<Type, WriteObjectDelegate>(WriteFnCache);
                newCache[type] = writeFn;

            } while (!ReferenceEquals(
                         Interlocked.CompareExchange(ref WriteFnCache, newCache, snapshot), snapshot));

            return writeFn;
        }
        catch (Exception ex)
        {
            Tracer.Instance.WriteError(ex);
            throw;
        }
    }

    public static void WriteLateBoundObject(TextWriter writer, object value)
    {
        if (value == null) return;
        var writeFn = GetWriteFn(value.GetType());
        writeFn(writer, value);
    }

    internal static WriteObjectDelegate GetValueTypeToStringMethod(Type type)
    {
        return Instance.GetValueTypeToStringMethod(type);
    }

    public static string SerializeToString<T>(T value)
    {
        var writer = StringWriterThreadStatic.Allocate();
        GetWriteFn(value.GetType())(writer, value);
        return StringWriterThreadStatic.ReturnAndFree(writer);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void InitAot<T>()
    {
        QueryStringWriter<T>.WriteFn();
    }
}

/// <summary>
/// Implement the serializer using a more static approach
/// </summary>
/// <typeparam name="T"></typeparam>
public static class QueryStringWriter<T>
{
    private static readonly WriteObjectDelegate CacheFn;

    public static WriteObjectDelegate WriteFn()
    {
        return CacheFn;
    }

    static QueryStringWriter()
    {
        if (typeof(T) == typeof(object))
        {
            CacheFn = QueryStringSerializer.WriteLateBoundObject;
        }
        else if (typeof(T).IsAssignableFrom(typeof(IDictionary))
                 || typeof(T).HasInterface(typeof(IDictionary)))
        {
            CacheFn = WriteIDictionary;
        }
        else
        {
            var isEnumerable = typeof(T).IsAssignableFrom(typeof(IEnumerable))
                               || typeof(T).HasInterface(typeof(IEnumerable));

            if ((typeof(T).IsClass || typeof(T).IsInterface)
                && !isEnumerable)
            {
                var canWriteType = WriteType<T, JsvTypeSerializer>.Write;
                if (canWriteType != null)
                {
                    CacheFn = WriteType<T, JsvTypeSerializer>.WriteQueryString;
                    return;
                }
            }

            CacheFn = QueryStringSerializer.Instance.GetWriteFn<T>();
        }
    }

    public static void WriteObject(TextWriter writer, object value)
    {
        if (writer == null) return;
        CacheFn(writer, value);
    }

    private static readonly ITypeSerializer Serializer = JsvTypeSerializer.Instance;
    public static void WriteIDictionary(TextWriter writer, object oMap)
    {
        WriteObjectDelegate writeKeyFn = null;
        WriteObjectDelegate writeValueFn = null;

        try
        {
            JsState.QueryStringMode = true;

            var isObjectDictionary = typeof(T) == typeof(Dictionary<string, object>);
            var map = (IDictionary)oMap;
            var ranOnce = false;
            foreach (var key in map.Keys)
            {
                var dictionaryValue = map[key];
                if (dictionaryValue == null) 
                    continue;

                if (writeKeyFn == null)
                {
                    var keyType = key.GetType();
                    writeKeyFn = Serializer.GetWriteFn(keyType);
                }

                if (writeValueFn == null || isObjectDictionary)
                {
                    writeValueFn = dictionaryValue is string
                        ? (w,x) => w.Write(((string)x).UrlEncode())
                        : Serializer.GetWriteFn(dictionaryValue.GetType());
                }

                if (ranOnce)
                    writer.Write("&");
                else
                    ranOnce = true;

                JsState.WritingKeyCount++;
                try
                {
                    JsState.IsWritingValue = false;

                    writeKeyFn(writer, key);
                }
                finally
                {
                    JsState.WritingKeyCount--;
                }

                writer.Write("=");

                JsState.IsWritingValue = true;
                try
                {
                    writeValueFn(writer, dictionaryValue);
                }
                finally
                {
                    JsState.IsWritingValue = false;
                }
            }
        }
        finally
        {
            JsState.QueryStringMode = false;
        }
    }
}

public delegate bool WriteComplexTypeDelegate(TextWriter writer, string propertyName, object obj);

internal class PropertyTypeConfig
{
    public TypeConfig TypeConfig;
    public Action<string, TextWriter, object> WriteFn;
}

internal class PropertyTypeConfig<T>
{
    public static PropertyTypeConfig Config;

    static PropertyTypeConfig()
    {
        Config = new PropertyTypeConfig
        {
            TypeConfig = TypeConfig<T>.GetState(),
            WriteFn = WriteType<T, JsvTypeSerializer>.WriteComplexQueryStringProperties,
        };
    }
}

public static class QueryStringStrategy
{
    static readonly ConcurrentDictionary<Type, PropertyTypeConfig> typeConfigCache = new();

    public static bool FormUrlEncoded(TextWriter writer, string propertyName, object obj)
    {
        if (obj is IDictionary map)
        {
            var i = 0;
            foreach (var key in map.Keys)
            {
                if (i++ > 0)
                    writer.Write('&');

                var value = map[key];
                writer.Write(propertyName);
                writer.Write('[');
                writer.Write(key.ToString());
                writer.Write("]=");

                if (value == null)
                {
                    writer.Write(JsonUtils.Null);
                }
                else if (value is string strValue && strValue == string.Empty) { /*ignore*/ }
                else
                {
                    var writeFn = JsvWriter.GetWriteFn(value.GetType());
                    writeFn(writer, value);
                }
            }

            return true;
        }

        var typeConfig = typeConfigCache.GetOrAdd(obj.GetType(), t =>
        {
            var genericType = typeof(PropertyTypeConfig<>).MakeGenericType(t);
            var fi = genericType.Fields().First(x => x.Name == "Config");

            var config = (PropertyTypeConfig)fi.GetValue(null);
            return config;
        });

        typeConfig.WriteFn(propertyName, writer, obj);

        return true;
    }
}