using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Json;

public class JsonlSerializer
{
    public static Encoding UseEncoding { get; set; } = PclExport.Instance.GetUTF8Encoding(false);
    private static Dictionary<Type, WriteObjectDelegate> WriteFnCache = new();
    internal const string DeserializationNotImplemented = "Deserialization not yet implemented for jsonl, use json endpoint instead";

    internal static WriteObjectDelegate GetWriteFn(Type type)
    {
        try
        {
            if (WriteFnCache.TryGetValue(type, out var writeFn)) 
                return writeFn;

            var genericType = typeof(JsonlSerializer<>).MakeGenericType(type);
            var mi = genericType.GetStaticMethod("WriteFn");
            var writeFactoryFn = (Func<WriteObjectDelegate>)mi.MakeDelegate(
                typeof(Func<WriteObjectDelegate>));

            writeFn = writeFactoryFn();

            Dictionary<Type, WriteObjectDelegate> snapshot, newCache;
            do
            {
                snapshot = WriteFnCache;
                newCache = new Dictionary<Type, WriteObjectDelegate>(WriteFnCache) {
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
    
    public static void SerializeToStream(object obj, Stream stream)
    {
        if (obj == null) return;
        var writer = new StreamWriter(stream, UseEncoding);
        var writeFn = GetWriteFn(obj.GetType());
        writeFn(writer, obj);
        writer.Flush();
    }

    public static object DeserializeFromStream(Type type, Stream stream)
    {
        if (stream == null) return null;
        using var reader = new StreamReader(stream, UseEncoding);
        return DeserializeFromString(type, reader.ReadToEnd());
    }

    public static object DeserializeFromString(Type type, string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        // var hold = JsState.IsCsv;
        // JsState.IsCsv = true;
        try
        {
            var fn = GetReadFn(type);
            var result = fn(text);
            var converted = ConvertFrom(type, result);
            return converted;
        }
        finally
        {
            // JsState.IsCsv = hold;
        }
    }

    internal static object ConvertFrom(Type type, object results) => 
        throw new NotImplementedException(DeserializationNotImplemented);

    private static Dictionary<Type, ParseStringDelegate> ReadFnCache = new();
    internal static ParseStringDelegate GetReadFn(Type type)
    {
        try
        {
            if (ReadFnCache.TryGetValue(type, out var writeFn)) return writeFn;

            var genericType = typeof(JsonlSerializer<>).MakeGenericType(type);
            var mi = genericType.GetStaticMethod("ReadFn");
            var writeFactoryFn = (Func<ParseStringDelegate>)mi.MakeDelegate(
                typeof(Func<ParseStringDelegate>));

            writeFn = writeFactoryFn();

            Dictionary<Type, ParseStringDelegate> snapshot, newCache;
            do
            {
                snapshot = ReadFnCache;
                newCache = new Dictionary<Type, ParseStringDelegate>(ReadFnCache) {[type] = writeFn};

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref ReadFnCache, newCache, snapshot), snapshot));

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

    public static object ReadLateBoundObject(Type type, string value)
    {
        if (value == null) return null;
        var readFn = GetReadFn(type);
        return readFn(value);
    }
    
}

public static class JsonlSerializer<T>
{
    private static readonly WriteObjectDelegate WriteCacheFn;
    private static readonly ParseStringDelegate ReadCacheFn;

    static JsonlSerializer()
    {
        if (typeof(T) == typeof(object))
        {
            WriteCacheFn = JsonlSerializer.WriteLateBoundObject;
            ReadCacheFn = str => JsonlSerializer.ReadLateBoundObject(typeof(T), str);
        }
        else
        {
            WriteCacheFn = GetWriteFn();
            ReadCacheFn = GetReadFn();
        }
    }

    public static ParseStringDelegate ReadFn() => throw new NotImplementedException(JsonlSerializer.DeserializationNotImplemented);
    private static ParseStringDelegate GetReadFn() => null;
    public static WriteObjectDelegate WriteFn() => WriteCacheFn;

    private static GetMemberDelegate valueGetter = null;
    private static WriteObjectDelegate writeElementFn = null;

    private static WriteObjectDelegate GetWriteFn()
    {
        PropertyInfo firstCandidate = null;
        Type bestCandidateEnumerableType = null;
        PropertyInfo bestCandidate = null;

        if (typeof(T).IsValueType)
        {
            return JsonWriter<T>.WriteObject;
        }

        //If type is an enumerable property itself write that
        bestCandidateEnumerableType = typeof(T).GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));
        if (bestCandidateEnumerableType != null)
        {
            var dictionaryOrKvps = typeof(T).HasInterface(typeof(IEnumerable<KeyValuePair<string, object>>))
                                || typeof(T).HasInterface(typeof(IEnumerable<KeyValuePair<string, string>>));
            if (dictionaryOrKvps)
            {
                return WriteSelf;
            }

            var elementType = bestCandidateEnumerableType.GetGenericArguments()[0];
            writeElementFn = CreateWriteFn(elementType);

            return WriteEnumerableType;
        }

        //Look for best candidate property if DTO
        if (typeof(T).IsDto() || typeof(T).HasAttribute<CsvAttribute>())
        {
            var properties = TypeConfig<T>.Properties;
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.Name == "ResponseStatus") continue;

                if (propertyInfo.PropertyType == typeof(string)
                    || propertyInfo.PropertyType.IsValueType
                    || propertyInfo.PropertyType == typeof(byte[]))
                    continue;

                if (firstCandidate == null)
                {
                    firstCandidate = propertyInfo;
                }

                var enumProperty = propertyInfo.PropertyType
                    .GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));

                if (enumProperty != null)
                {
                    bestCandidateEnumerableType = enumProperty;
                    bestCandidate = propertyInfo;
                    break;
                }
            }
        }

        //If is not DTO or no candidates exist, write self
        var noCandidatesExist = bestCandidate == null && firstCandidate == null;
        if (noCandidatesExist)
        {
            return WriteSelf;
        }

        //If is DTO and has an enumerable property serialize that
        if (bestCandidateEnumerableType != null)
        {
            valueGetter = bestCandidate.CreateGetter();
            var elementType = bestCandidateEnumerableType.GetGenericArguments()[0];
            writeElementFn = CreateWriteFn(elementType);

            return WriteEnumerableProperty;
        }

        //If is DTO and has non-enumerable, reference type property serialize that
        valueGetter = firstCandidate.CreateGetter();
        writeElementFn = CreateWriteRowFn(firstCandidate.PropertyType);

        return WriteNonEnumerableType;
    }

    private static WriteObjectDelegate CreateWriteFn(Type elementType) => CreateWriterFn(elementType, "WriteObject");

    private static WriteObjectDelegate CreateWriteRowFn(Type elementType) => CreateWriterFn(elementType, "WriteObjectRow");
    
    private static WriteObjectDelegate CreateWriterFn(Type elementType, string methodName)
    {
        var genericType = typeof(JsonlWriter<>).MakeGenericType(elementType);
        var mi = genericType.GetStaticMethod(methodName);
        var writeFn = (WriteObjectDelegate)mi.MakeDelegate(typeof(WriteObjectDelegate));
        return writeFn;
    }

    public static void WriteEnumerableType(TextWriter writer, object obj)
    {
        writeElementFn(writer, obj);
    }

    public static void WriteSelf(TextWriter writer, object obj)
    {
        JsonSerializer.SerializeToWriter(obj, writer);
    }

    public static void WriteEnumerableProperty(TextWriter writer, object obj)
    {
        if (obj == null) return; //AOT

        var enumerableProperty = valueGetter(obj);
        writeElementFn(writer, enumerableProperty);
    }

    public static void WriteNonEnumerableType(TextWriter writer, object obj)
    {
        var nonEnumerableType = valueGetter(obj);
        writeElementFn(writer, nonEnumerableType);
    }
    
    public static void WriteRow(TextWriter writer, object row)
    {
        if (writer == null) return; //AOT

        JsonSerializer.SerializeToWriter(row, writer);
        writer.WriteLine();
    }

    public static void Write(TextWriter writer, IEnumerable<List<string>> rows)
    {
        if (writer == null) return; //AOT
        foreach (var row in rows)
        {
            WriteRow(writer, row);
        }
    }
}

public class JsonlWriter
{
}

public class JsonlWriter<T>
{
    public static void WriteObject(TextWriter writer, object records)
    {
        if (writer == null) return; //AOT

        var rows = (IEnumerable<T>)records;
        foreach (var row in rows)
        {
            WriteObjectRow(writer, row);
        }
    }

    public static void WriteObjectRow(TextWriter writer, object record)
    {
        if (writer == null) return; //AOT
        JsonSerializer.SerializeToWriter(record, writer);
        writer.WriteLine();
    }
}