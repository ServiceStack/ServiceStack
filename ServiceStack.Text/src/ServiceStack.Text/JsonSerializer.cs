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
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;

namespace ServiceStack.Text;

/// <summary>
/// Creates an instance of a Type from a string value
/// </summary>
public static class JsonSerializer
{
    static JsonSerializer()
    {
        JsConfig.InitStatics();
    }

    public static int BufferSize = 1024;

    [Obsolete("Use JsConfig.UTF8Encoding")]
    public static UTF8Encoding UTF8Encoding
    {
        get => JsConfig.UTF8Encoding;
        set => JsConfig.UTF8Encoding = value;
    }

    public static Action<object> OnSerialize { get; set; }

    public static T DeserializeFromString<T>(string value)
    {
        return JsonReader<T>.Parse(value) is T obj ? obj : default(T);
    }

    public static T DeserializeFromSpan<T>(ReadOnlySpan<char> value)
    {
        return JsonReader<T>.Parse(value) is T obj ? obj : default(T);
    }

    public static T DeserializeFromReader<T>(TextReader reader)
    {
        return DeserializeFromString<T>(reader.ReadToEnd());
    }

    public static object DeserializeFromSpan(Type type, ReadOnlySpan<char> value)
    {
        return value.IsEmpty
            ? null
            : JsonReader.GetParseSpanFn(type)(value);
    }

    public static object DeserializeFromString(string value, Type type)
    {
        return string.IsNullOrEmpty(value)
            ? null
            : JsonReader.GetParseFn(type)(value);
    }

    public static object DeserializeFromReader(TextReader reader, Type type)
    {
        return DeserializeFromString(reader.ReadToEnd(), type);
    }
        
    public static string SerializeToString<T>(T value)
    {
        if (value == null || value is Delegate) return null;
        if (typeof(T) == typeof(object))
        {
            return SerializeToString(value, value.GetType());
        }
        if (typeof(T).IsAbstract || typeof(T).IsInterface)
        {
            var prevState = JsState.IsWritingDynamic;
            JsState.IsWritingDynamic = true;
            var result = SerializeToString(value, value.GetType());
            JsState.IsWritingDynamic = prevState;
            return result;
        }

        var writer = StringWriterThreadStatic.Allocate();
        if (typeof(T) == typeof(string))
        {
            JsonUtils.WriteString(writer, value as string);
        }
        else
        {
            WriteObjectToWriter(value, JsonWriter<T>.GetRootObjectWriteFn(value), writer);
        }
        return StringWriterThreadStatic.ReturnAndFree(writer);
    }

    public static string SerializeToString(object value, Type type)
    {
        if (value == null) return null;

        var writer = StringWriterThreadStatic.Allocate();
        if (type == typeof(string))
        {
            JsonUtils.WriteString(writer, value as string);
        }
        else
        {
            OnSerialize?.Invoke(value);
            WriteObjectToWriter(value, JsonWriter.GetWriteFn(type), writer);
        }
        return StringWriterThreadStatic.ReturnAndFree(writer);
    }

    public static void SerializeToWriter<T>(T value, TextWriter writer)
    {
        if (value == null) return;
        if (typeof(T) == typeof(string))
        {
            JsonUtils.WriteString(writer, value as string);
        }
        else if (typeof(T) == typeof(object))
        {
            SerializeToWriter(value, value.GetType(), writer);
        }
        else if (typeof(T).IsAbstract || typeof(T).IsInterface)
        {
            var prevState = JsState.IsWritingDynamic;
            JsState.IsWritingDynamic = false;
            SerializeToWriter(value, value.GetType(), writer);
            JsState.IsWritingDynamic = prevState;
        }
        else
        {
            WriteObjectToWriter(value, JsonWriter<T>.GetRootObjectWriteFn(value), writer);
        }
    }

    public static void SerializeToWriter(object value, Type type, TextWriter writer)
    {
        if (value == null) return;
        if (type == typeof(string))
        {
            JsonUtils.WriteString(writer, value as string);
            return;
        }

        OnSerialize?.Invoke(value);
        WriteObjectToWriter(value, JsonWriter.GetWriteFn(type), writer);
    }

    public static void SerializeToStream<T>(T value, Stream stream)
    {
        if (value == null) return;
        if (typeof(T) == typeof(object))
        {
            SerializeToStream(value, value.GetType(), stream);
        }
        else if (typeof(T).IsAbstract || typeof(T).IsInterface)
        {
            var prevState = JsState.IsWritingDynamic;
            JsState.IsWritingDynamic = false;
            SerializeToStream(value, value.GetType(), stream);
            JsState.IsWritingDynamic = prevState;
        }
        else
        {
            var writer = new StreamWriter(stream, JsConfig.UTF8Encoding, BufferSize, leaveOpen:true);
            WriteObjectToWriter(value, JsonWriter<T>.GetRootObjectWriteFn(value), writer);
            writer.Flush();
        }
    }

    public static void SerializeToStream(object value, Type type, Stream stream)
    {
        OnSerialize?.Invoke(value);
        var writer = new StreamWriter(stream, JsConfig.UTF8Encoding, BufferSize, leaveOpen:true);
        WriteObjectToWriter(value, JsonWriter.GetWriteFn(type), writer);
        writer.Flush();
    }

    public static Task SerializeToStreamAsync(object value, Stream stream) =>
        SerializeToStreamAsync(value, value.GetType(), stream);
    
    public static async Task SerializeToStreamAsync(object value, Type type, Stream stream)
    {
        OnSerialize?.Invoke(value);
        using var ms = MemoryStreamFactory.GetStream();
        using var writer = new StreamWriter(ms, JsConfig.UTF8Encoding, BufferSize, leaveOpen:true);
        WriteObjectToWriter(value, JsonWriter.GetWriteFn(type), writer);
        await writer.FlushAsync();
        ms.Position = 0;
        await ms.CopyToAsync(stream).ConfigAwait();
    }

    private static void WriteObjectToWriter(object value, WriteObjectDelegate serializeFn, TextWriter writer)
    {
        if (!JsConfig.Indent)
        {
            serializeFn(writer, value);
        }
        else
        {
            var sb = StringBuilderCache.Allocate();
            using var captureJson = new StringWriter(sb);
            serializeFn(captureJson, value);
            captureJson.Flush();
            var json = StringBuilderCache.ReturnAndFree(sb);
            var indentJson = json.IndentJson();
            writer.Write(indentJson);
        }
    }
    public static T DeserializeFromStream<T>(Stream stream)
    {
        return (T)MemoryProvider.Instance.Deserialize(stream, typeof(T), DeserializeFromSpan);
    }

    public static object DeserializeFromStream(Type type, Stream stream)
    {
        return MemoryProvider.Instance.Deserialize(stream, type, DeserializeFromSpan);
    }

    public static Task<object> DeserializeFromStreamAsync(Type type, Stream stream)
    {
        return MemoryProvider.Instance.DeserializeAsync(stream, type, DeserializeFromSpan);
    }

    public static async Task<T> DeserializeFromStreamAsync<T>(Stream stream)
    {
        var obj = await MemoryProvider.Instance.DeserializeAsync(stream, typeof(T), DeserializeFromSpan).ConfigAwait();
        return (T)obj;
    }

    public static T DeserializeResponse<T>(WebRequest webRequest)
    {
        using var webRes = PclExport.Instance.GetResponse(webRequest);
        using var stream = webRes.GetResponseStream();
        return DeserializeFromStream<T>(stream);
    }

    public static object DeserializeResponse<T>(Type type, WebRequest webRequest)
    {
        using var webRes = PclExport.Instance.GetResponse(webRequest);
        using var stream = webRes.GetResponseStream();
        return DeserializeFromStream(type, stream);
    }

    public static T DeserializeRequest<T>(WebRequest webRequest)
    {
        using var webRes = PclExport.Instance.GetResponse(webRequest);
        return DeserializeResponse<T>(webRes);
    }

    public static object DeserializeRequest(Type type, WebRequest webRequest)
    {
        using var webRes = PclExport.Instance.GetResponse(webRequest);
        return DeserializeResponse(type, webRes);
    }

    public static T DeserializeResponse<T>(WebResponse webResponse)
    {
        using var stream = webResponse.GetResponseStream();
        return DeserializeFromStream<T>(stream);
    }

    public static object DeserializeResponse(Type type, WebResponse webResponse)
    {
        using var stream = webResponse.GetResponseStream();
        return DeserializeFromStream(type, stream);
    }
}

public class JsonStringSerializer : IStringSerializer
{
    public To DeserializeFromString<To>(string serializedText) => 
        JsonSerializer.DeserializeFromString<To>(serializedText);
    public object DeserializeFromString(string serializedText, Type type) => 
        JsonSerializer.DeserializeFromString(serializedText, type);
    public string SerializeToString<TFrom>(TFrom @from) => 
        JsonSerializer.SerializeToString(@from);
}