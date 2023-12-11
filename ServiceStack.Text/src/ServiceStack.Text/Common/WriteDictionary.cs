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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ServiceStack.Text.Json;

namespace ServiceStack.Text.Common;

internal delegate void WriteMapDelegate(
    TextWriter writer,
    object oMap,
    WriteObjectDelegate writeKeyFn,
    WriteObjectDelegate writeValueFn);

internal static class WriteDictionary<TSerializer>
    where TSerializer : ITypeSerializer
{
    private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

    internal class MapKey
    {
        internal Type KeyType;
        internal Type ValueType;

        public MapKey(Type keyType, Type valueType)
        {
            KeyType = keyType;
            ValueType = valueType;
        }

        public bool Equals(MapKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.KeyType, KeyType) && Equals(other.ValueType, ValueType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(MapKey)) return false;
            return Equals((MapKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((KeyType != null ? KeyType.GetHashCode() : 0) * 397) ^ (ValueType != null ? ValueType.GetHashCode() : 0);
            }
        }
    }

    static Dictionary<MapKey, WriteMapDelegate> CacheFns = new Dictionary<MapKey, WriteMapDelegate>();

    public static Action<TextWriter, object, WriteObjectDelegate, WriteObjectDelegate>
        GetWriteGenericDictionary(Type keyType, Type valueType)
    {
        var mapKey = new MapKey(keyType, valueType);
        if (CacheFns.TryGetValue(mapKey, out var writeFn)) return writeFn.Invoke;

        var genericType = typeof(ToStringDictionaryMethods<,,>).MakeGenericType(keyType, valueType, typeof(TSerializer));
        var mi = genericType.GetStaticMethod("WriteIDictionary");
        writeFn = (WriteMapDelegate)mi.MakeDelegate(typeof(WriteMapDelegate));

        Dictionary<MapKey, WriteMapDelegate> snapshot, newCache;
        do
        {
            snapshot = CacheFns;
            newCache = new Dictionary<MapKey, WriteMapDelegate>(CacheFns);
            newCache[mapKey] = writeFn;

        } while (!ReferenceEquals(
                     Interlocked.CompareExchange(ref CacheFns, newCache, snapshot), snapshot));

        return writeFn.Invoke;
    }

    public static void WriteIDictionary(TextWriter writer, object oMap)
    {
        WriteObjectDelegate writeKeyFn = null;
        WriteObjectDelegate writeValueFn = null;

        writer.Write(JsWriter.MapStartChar);
        var encodeMapKey = false;
        Type lastKeyType = null;
        Type lastValueType = null;

        var map = (IDictionary)oMap;
        var ranOnce = false;
        foreach (var key in map.Keys)
        {
            var dictionaryValue = map[key];

            var isNull = (dictionaryValue == null);
            if (isNull && !Serializer.IncludeNullValuesInDictionaries) continue;

            var keyType = key.GetType();
            if (writeKeyFn == null || lastKeyType != keyType)
            {
                lastKeyType = keyType;
                writeKeyFn = Serializer.GetWriteFn(keyType);
                encodeMapKey = Serializer.GetTypeInfo(keyType).EncodeMapKey;
            }

            JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

            JsState.WritingKeyCount++;
            try
            {
                if (encodeMapKey)
                {
                    JsState.IsWritingValue = true; //prevent ""null""
                    try
                    {
                        writer.Write(JsWriter.QuoteChar);
                        writeKeyFn(writer, key);
                        writer.Write(JsWriter.QuoteChar);
                    }
                    finally
                    {
                        JsState.IsWritingValue = false;
                    }
                }
                else
                {
                    writeKeyFn(writer, key);
                }
            }
            finally
            {
                JsState.WritingKeyCount--;
            }

            writer.Write(JsWriter.MapKeySeperator);

            if (isNull)
            {
                writer.Write(JsonUtils.Null);
            }
            else
            {
                var valueType = dictionaryValue.GetType();
                if (writeValueFn == null || lastValueType != valueType)
                {
                    lastValueType = valueType;
                    writeValueFn = Serializer.GetWriteFn(valueType);
                }

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

        writer.Write(JsWriter.MapEndChar);
    }
}

public static class ToStringDictionaryMethods<TKey, TValue, TSerializer>
    where TSerializer : ITypeSerializer
{
    private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

    public static void WriteIDictionary(
        TextWriter writer,
        object oMap,
        WriteObjectDelegate writeKeyFn,
        WriteObjectDelegate writeValueFn)
    {
        if (writer == null) return; //AOT
        WriteGenericIDictionary(writer, (IDictionary<TKey, TValue>)oMap, writeKeyFn, writeValueFn);
    }

    public static void WriteGenericIDictionary(
        TextWriter writer,
        IDictionary<TKey, TValue> map,
        WriteObjectDelegate writeKeyFn,
        WriteObjectDelegate writeValueFn)
    {
        if (map == null)
        {
            writer.Write(JsonUtils.Null);
            return;
        }

        if (map is JsonObject jsonObject)
            map = (IDictionary<TKey,TValue>) jsonObject.ToUnescapedDictionary();
            
        writer.Write(JsWriter.MapStartChar);

        var encodeMapKey = Serializer.GetTypeInfo(typeof(TKey)).EncodeMapKey;

        var ranOnce = false;
        foreach (var kvp in map)
        {
            var isNull = (kvp.Value == null);
            if (isNull && !Serializer.IncludeNullValuesInDictionaries) continue;

            JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

            JsState.WritingKeyCount++;
            try
            {
                if (encodeMapKey)
                {
                    JsState.IsWritingValue = true; //prevent ""null""
                    try
                    {
                        writer.Write(JsWriter.QuoteChar);
                        writeKeyFn(writer, kvp.Key);
                        writer.Write(JsWriter.QuoteChar);
                    }
                    finally
                    {
                        JsState.IsWritingValue = false;
                    }
                }
                else
                {
                    writeKeyFn(writer, kvp.Key);
                }
            }
            finally
            {
                JsState.WritingKeyCount--;
            }

            writer.Write(JsWriter.MapKeySeperator);

            if (isNull)
            {
                writer.Write(JsonUtils.Null);
            }
            else
            {
                JsState.IsWritingValue = true;
                try
                {
                    writeValueFn(writer, kvp.Value);
                }
                finally
                {
                    JsState.IsWritingValue = false;
                }
            }
        }

        writer.Write(JsWriter.MapEndChar);
    }
}