//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Json;

public static class JsonWriter
{
    public static readonly JsWriter<JsonTypeSerializer> Instance = new();

    private static Dictionary<Type, WriteObjectDelegate> WriteFnCache = new();

    internal static void RemoveCacheFn(Type forType)
    {
        Dictionary<Type, WriteObjectDelegate> snapshot, newCache;
        do
        {
            snapshot = WriteFnCache;
            newCache = new Dictionary<Type, WriteObjectDelegate>(WriteFnCache);
            newCache.Remove(forType);

        } while (!ReferenceEquals(
                     Interlocked.CompareExchange(ref WriteFnCache, newCache, snapshot), snapshot));
    }

    internal static WriteObjectDelegate GetWriteFn(Type type)
    {
        try
        {
            if (WriteFnCache.TryGetValue(type, out var writeFn)) return writeFn;

            var genericType = typeof(JsonWriter<>).MakeGenericType(type);
            var mi = genericType.GetStaticMethod("WriteFn");
            var writeFactoryFn = (Func<WriteObjectDelegate>)mi.MakeDelegate(typeof(Func<WriteObjectDelegate>));
            writeFn = writeFactoryFn();

            Dictionary<Type, WriteObjectDelegate> snapshot, newCache;
            do
            {
                snapshot = WriteFnCache;
                newCache = new Dictionary<Type, WriteObjectDelegate>(WriteFnCache)
                {
                    [type] = writeFn
                };

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

    private static Dictionary<Type, TypeInfo> JsonTypeInfoCache = new Dictionary<Type, TypeInfo>();

    internal static TypeInfo GetTypeInfo(Type type)
    {
        try
        {
            if (JsonTypeInfoCache.TryGetValue(type, out var writeFn)) return writeFn;

            var genericType = typeof(JsonWriter<>).MakeGenericType(type);
            var mi = genericType.GetStaticMethod("GetTypeInfo");
            var writeFactoryFn = (Func<TypeInfo>)mi.MakeDelegate(typeof(Func<TypeInfo>));
            writeFn = writeFactoryFn();

            Dictionary<Type, TypeInfo> snapshot, newCache;
            do
            {
                snapshot = JsonTypeInfoCache;
                newCache = new Dictionary<Type, TypeInfo>(JsonTypeInfoCache)
                {
                    [type] = writeFn
                };

            } while (!ReferenceEquals(
                         Interlocked.CompareExchange(ref JsonTypeInfoCache, newCache, snapshot), snapshot));

            return writeFn;
        }
        catch (Exception ex)
        {
            Tracer.Instance.WriteError(ex);
            throw;
        }
    }

    internal static void WriteLateBoundObject(TextWriter writer, object value)
    {
        if (value == null)
        {
            writer.Write(JsonUtils.Null);
            return;
        }

        try
        {
            if (!JsState.Traverse(value))
                return;

            var type = value.GetType();
            var writeFn = type == typeof(object)
                ? WriteType<object, JsonTypeSerializer>.WriteObjectType
                : GetWriteFn(type);

            var prevState = JsState.IsWritingDynamic;
            JsState.IsWritingDynamic = true;
            writeFn(writer, value);
            JsState.IsWritingDynamic = prevState;
        }
        finally
        {
            JsState.UnTraverse();
        }
    }

    internal static WriteObjectDelegate GetValueTypeToStringMethod(Type type)
    {
        return Instance.GetValueTypeToStringMethod(type);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void InitAot<T>()
    {
        Text.Json.JsonWriter<T>.WriteFn();
        Text.Json.JsonWriter.Instance.GetWriteFn<T>();
        Text.Json.JsonWriter.Instance.GetValueTypeToStringMethod(typeof(T));
        JsWriter.GetTypeSerializer<Text.Json.JsonTypeSerializer>().GetWriteFn<T>();
    }
}

public class TypeInfo
{
    internal bool EncodeMapKey;
}

/// <summary>
/// Implement the serializer using a more static approach
/// </summary>
/// <typeparam name="T"></typeparam>
public static class JsonWriter<T>
{
    internal static TypeInfo TypeInfo;
    private static WriteObjectDelegate CacheFn;

    public static void Reset()
    {
        JsonWriter.RemoveCacheFn(typeof(T));
        Refresh();
    }

    public static void Refresh()
    {
        if (JsonWriter.Instance == null)
            return;

        CacheFn = typeof(T) == typeof(object)
            ? JsonWriter.WriteLateBoundObject
            : JsonWriter.Instance.GetWriteFn<T>();
        JsConfig.AddUniqueType(typeof(T));
    }

    public static WriteObjectDelegate WriteFn()
    {
        return CacheFn ?? WriteObject;
    }

    public static TypeInfo GetTypeInfo()
    {
        return TypeInfo;
    }

    static JsonWriter()
    {
        if (JsonWriter.Instance == null)
            return;

        var isNumeric = typeof(T).IsNumericType();
        TypeInfo = new TypeInfo
        {
            EncodeMapKey = typeof(T) == typeof(bool) || isNumeric,
        };

        CacheFn = typeof(T) == typeof(object)
            ? JsonWriter.WriteLateBoundObject
            : JsonWriter.Instance.GetWriteFn<T>();
    }

    public static void WriteObject(TextWriter writer, object value)
    {
        TypeConfig<T>.Init();

        try
        {
            if (!JsState.Traverse(value))
                return;

            CacheFn(writer, value);
        }
        finally
        {
            JsState.UnTraverse();
        }
    }

    public static void WriteRootObject(TextWriter writer, object value)
    {
        GetRootObjectWriteFn(value)(writer, value);
    }

    public static WriteObjectDelegate GetRootObjectWriteFn(object value)
    {
        TypeConfig<T>.Init();
        JsonSerializer.OnSerialize?.Invoke(value);

        JsState.Depth = 0;
        return CacheFn;
    }
}