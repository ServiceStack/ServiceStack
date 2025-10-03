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
using System.Threading;

namespace ServiceStack.Text.Common;

public static class DeserializeDictionary<TSerializer>
    where TSerializer : ITypeSerializer
{
    private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

    const int KeyIndex = 0;
    const int ValueIndex = 1;

    public static ParseStringDelegate GetParseMethod(Type type) => v => GetParseStringSpanMethod(type)(v.AsSpan());

    public static ParseStringSpanDelegate GetParseStringSpanMethod(Type type)
    {
        var mapInterface = type.GetTypeWithGenericInterfaceOf(typeof(IDictionary<,>));
        if (mapInterface == null)
        {
            var fn = PclExport.Instance.GetDictionaryParseStringSpanMethod<TSerializer>(type);
            if (fn != null)
                return fn;

            if (type == typeof(IDictionary))
            {
                return GetParseStringSpanMethod(typeof(Dictionary<object, object>));
            }
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return s => ParseIDictionary(s, type);
            }

            throw new ArgumentException($"Type {type.FullName} is not of type IDictionary<,>");
        }

        //optimized access for regularly used types
        if (type == typeof(Dictionary<string, string>))
            return ParseStringDictionary;
        if (type == typeof(JsonObject))
            return ParseJsonObject;
        if (typeof(JsonObject).IsAssignableFrom(type))
        {
            var method = typeof(DeserializeDictionary<TSerializer>).GetMethod("ParseInheritedJsonObject");
            method = method.MakeGenericMethod(type);
            return Delegate.CreateDelegate(typeof(ParseStringSpanDelegate), method) as ParseStringSpanDelegate;
        }

        var dictionaryArgs = mapInterface.GetGenericArguments();
        var keyTypeParseMethod = Serializer.GetParseStringSpanFn(dictionaryArgs[KeyIndex]);
        if (keyTypeParseMethod == null) return null;

        var valueTypeParseMethod = Serializer.GetParseStringSpanFn(dictionaryArgs[ValueIndex]);
        if (valueTypeParseMethod == null) return null;

        var createMapType = type.HasAnyTypeDefinitionsOf(typeof(Dictionary<,>), typeof(IDictionary<,>))
            ? null : type;

        return value => ParseDictionaryType(value, createMapType, dictionaryArgs, keyTypeParseMethod, valueTypeParseMethod);
    }

    public static JsonObject ParseJsonObject(string value) => ParseJsonObject(value.AsSpan());
        
    public static T ParseInheritedJsonObject<T>(ReadOnlySpan<char> value) where T : JsonObject, new()
    {
        if (value.Length == 0)
            return null;

        var index = VerifyAndGetStartIndex(value, typeof(T));

        var result = new T();

        if (Json.JsonTypeSerializer.IsEmptyMap(value, index)) return result;

        var valueLength = value.Length;
        while (index < valueLength)
        {
            var keyValue = Serializer.EatMapKey(value, ref index);
            Serializer.EatMapKeySeperator(value, ref index);
            var elementValue = Serializer.EatValue(value, ref index);
            if (keyValue.IsEmpty) continue;

            var mapKey = keyValue.ToString();
            var mapValue = elementValue.Value();

            result[mapKey] = mapValue;

            Serializer.EatItemSeperatorOrMapEndChar(value, ref index);
        }

        return result;
    }

    public static JsonObject ParseJsonObject(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
            return null;

        var index = VerifyAndGetStartIndex(value, typeof(JsonObject));

        var result = new JsonObject();

        if (Json.JsonTypeSerializer.IsEmptyMap(value, index)) return result;

        var valueLength = value.Length;
        while (index < valueLength)
        {
            var keyValue = Serializer.EatMapKey(value, ref index);
            Serializer.EatMapKeySeperator(value, ref index);
            var elementValue = Serializer.EatValue(value, ref index);
            if (keyValue.IsEmpty) continue;

            var mapKey = keyValue.ToString();
            var mapValue = elementValue.Value();

            result[mapKey] = mapValue;

            Serializer.EatItemSeperatorOrMapEndChar(value, ref index);
        }

        return result;
    }

    public static Dictionary<string, string> ParseStringDictionary(string value) => ParseStringDictionary(value.AsSpan());

    public static Dictionary<string, string> ParseStringDictionary(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return null;

        var index = VerifyAndGetStartIndex(value, typeof(Dictionary<string, string>));

        var result = new Dictionary<string, string>();

        if (Json.JsonTypeSerializer.IsEmptyMap(value, index)) return result;

        var valueLength = value.Length;
        while (index < valueLength)
        {
            var keyValue = Serializer.EatMapKey(value, ref index);
            Serializer.EatMapKeySeperator(value, ref index);
            var elementValue = Serializer.EatValue(value, ref index);
            if (keyValue.IsEmpty) continue;

            var mapKey = Serializer.UnescapeString(keyValue);
            var mapValue = Serializer.UnescapeString(elementValue);

            result[mapKey.ToString()] = mapValue.Value();

            Serializer.EatItemSeperatorOrMapEndChar(value, ref index);
        }

        return result;
    }

    public static IDictionary ParseIDictionary(string value, Type dictType) => ParseIDictionary(value.AsSpan(), dictType);

    public static IDictionary ParseIDictionary(ReadOnlySpan<char> value, Type dictType)
    {
        if (value.IsEmpty) return null;

        var index = VerifyAndGetStartIndex(value, dictType);

        var valueParseMethod = Serializer.GetParseStringSpanFn(typeof(object));
        if (valueParseMethod == null) return null;

        var to = (IDictionary)dictType.CreateInstance();

        if (Json.JsonTypeSerializer.IsEmptyMap(value, index)) return to;

        var valueLength = value.Length;
        while (index < valueLength)
        {
            var keyValue = Serializer.EatMapKey(value, ref index);
            Serializer.EatMapKeySeperator(value, ref index);
            var elementStartIndex = index;
            var elementValue = Serializer.EatTypeValue(value, ref index);
            if (keyValue.IsEmpty) continue;

            var mapKey = valueParseMethod(keyValue);

            if (elementStartIndex < valueLength)
            {
                Serializer.EatWhitespace(value, ref elementStartIndex);
                to[mapKey] = DeserializeType<TSerializer>.ParsePrimitive(elementValue.Value(), value[elementStartIndex]);
            }
            else
            {
                to[mapKey] = valueParseMethod(elementValue);
            }

            Serializer.EatItemSeperatorOrMapEndChar(value, ref index);
        }

        return to;
    }

    public static IDictionary<TKey, TValue> ParseDictionary<TKey, TValue>(
        string value, Type createMapType,
        ParseStringDelegate parseKeyFn, ParseStringDelegate parseValueFn)
    {
        return ParseDictionary<TKey, TValue>(value.AsSpan(),
            createMapType,
            v => parseKeyFn(v.ToString()),
            v => parseValueFn(v.ToString())
        );
    }


    public static IDictionary<TKey, TValue> ParseDictionary<TKey, TValue>(
        ReadOnlySpan<char> value, Type createMapType,
        ParseStringSpanDelegate parseKeyFn, ParseStringSpanDelegate parseValueFn)
    {
        if (value.IsEmpty) return null;

        var to = (createMapType == null)
            ? new Dictionary<TKey, TValue>()
            : (IDictionary<TKey, TValue>)createMapType.CreateInstance();

        var objDeserializer = Json.JsonTypeSerializer.Instance.ObjectDeserializer;
        if (to is Dictionary<string, object> && objDeserializer != null && typeof(TSerializer) == typeof(Json.JsonTypeSerializer))
            return (IDictionary<TKey,TValue>) objDeserializer(value);

        var config = JsConfig.GetConfig();

        var tryToParseItemsAsDictionaries =
            config.ConvertObjectTypesIntoStringDictionary && typeof(TValue) == typeof(object);
        var tryToParseItemsAsPrimitiveTypes =
            config.TryToParsePrimitiveTypeValues && typeof(TValue) == typeof(object);

        var index = VerifyAndGetStartIndex(value, createMapType);

        if (Json.JsonTypeSerializer.IsEmptyMap(value, index)) return to;

        var valueLength = value.Length;
        while (index < valueLength)
        {
            var keyValue = Serializer.EatMapKey(value, ref index);
            Serializer.EatMapKeySeperator(value, ref index);
            var elementStartIndex = index;
            var elementValue = Serializer.EatTypeValue(value, ref index);
            if (keyValue.IsNullOrEmpty()) continue;

            TKey mapKey = (TKey)parseKeyFn(keyValue);

            if (tryToParseItemsAsDictionaries)
            {
                Serializer.EatWhitespace(value, ref elementStartIndex);
                if (elementStartIndex < valueLength && value[elementStartIndex] == JsWriter.MapStartChar)
                {
                    var tmpMap = ParseDictionary<TKey, TValue>(elementValue, createMapType, parseKeyFn, parseValueFn);
                    if (tmpMap != null && tmpMap.Count > 0)
                    {
                        to[mapKey] = (TValue)tmpMap;
                    }
                }
                else if (elementStartIndex < valueLength && value[elementStartIndex] == JsWriter.ListStartChar)
                {
                    to[mapKey] = (TValue)DeserializeList<List<object>, TSerializer>.ParseStringSpan(elementValue);
                }
                else
                {
                    to[mapKey] = (TValue)(tryToParseItemsAsPrimitiveTypes && elementStartIndex < valueLength
                        ? DeserializeType<TSerializer>.ParsePrimitive(elementValue.Value(), value[elementStartIndex])
                        : parseValueFn(elementValue).Value());
                }
            }
            else
            {
                if (tryToParseItemsAsPrimitiveTypes && elementStartIndex < valueLength)
                {
                    Serializer.EatWhitespace(value, ref elementStartIndex);
                    to[mapKey] = (TValue)DeserializeType<TSerializer>.ParsePrimitive(elementValue.Value(), value[elementStartIndex]);
                }
                else
                {
                    to[mapKey] = (TValue)parseValueFn(elementValue).Value();
                }
            }

            Serializer.EatItemSeperatorOrMapEndChar(value, ref index);
        }

        return to;
    }

    private static int VerifyAndGetStartIndex(ReadOnlySpan<char> value, Type createMapType)
    {
        var index = 0;
        if (value.Length > 0 && !Serializer.EatMapStartChar(value, ref index))
        {
            //Don't throw ex because some KeyValueDataContractDeserializer don't have '{}'
            Tracer.Instance.WriteDebug("WARN: Map definitions should start with a '{0}', expecting serialized type '{1}', got string starting with: {2}",
                JsWriter.MapStartChar, createMapType != null ? createMapType.Name : "Dictionary<,>", value.Substring(0, value.Length < 50 ? value.Length : 50));
        }
        return index;
    }

    private static Dictionary<TypesKey, ParseDictionaryDelegate> ParseDelegateCache = new();

    private delegate object ParseDictionaryDelegate(ReadOnlySpan<char> value, Type createMapType,
        ParseStringSpanDelegate keyParseFn, ParseStringSpanDelegate valueParseFn);

    public static object ParseDictionaryType(string value, Type createMapType, Type[] argTypes,
        ParseStringDelegate keyParseFn, ParseStringDelegate valueParseFn) =>
        ParseDictionaryType(value.AsSpan(), createMapType, argTypes,
            v => keyParseFn(v.ToString()), v => valueParseFn(v.ToString()));

    static readonly Type[] signature = {typeof(ReadOnlySpan<char>), typeof(Type), typeof(ParseStringSpanDelegate), typeof(ParseStringSpanDelegate)};

    public static object ParseDictionaryType(ReadOnlySpan<char> value, Type createMapType, Type[] argTypes,
        ParseStringSpanDelegate keyParseFn, ParseStringSpanDelegate valueParseFn)
    {
        var key = new TypesKey(argTypes[0], argTypes[1]);
        if (ParseDelegateCache.TryGetValue(key, out var parseDelegate))
            return parseDelegate(value, createMapType, keyParseFn, valueParseFn);

        var mi = typeof(DeserializeDictionary<TSerializer>).GetStaticMethod("ParseDictionary", signature);
        var genericMi = mi.MakeGenericMethod(argTypes);
        parseDelegate = (ParseDictionaryDelegate)genericMi.MakeDelegate(typeof(ParseDictionaryDelegate));

        Dictionary<TypesKey, ParseDictionaryDelegate> snapshot, newCache;
        do
        {
            snapshot = ParseDelegateCache;
            newCache = new Dictionary<TypesKey, ParseDictionaryDelegate>(ParseDelegateCache) {
                [key] = parseDelegate
            };

        } while (!ReferenceEquals(
                     Interlocked.CompareExchange(ref ParseDelegateCache, newCache, snapshot), snapshot));

        return parseDelegate(value, createMapType, keyParseFn, valueParseFn);
    }

    struct TypesKey
    {
        Type Type1 { get; }
        Type Type2 { get; }

        readonly int hashcode;

        public TypesKey(Type type1, Type type2)
        {
            Type1 = type1;
            Type2 = type2;
            unchecked
            {
                hashcode = Type1.GetHashCode() ^ (37 * Type2.GetHashCode());
            }
        }

        public override bool Equals(object obj)
        {
            var types = (TypesKey)obj;

            return Type1 == types.Type1 && Type2 == types.Type2;
        }

        public override int GetHashCode() => hashcode;
    }
}