//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Dynamic;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;
using System.Linq;

using System.Reflection;
using System.Reflection.Emit;

namespace ServiceStack
{
    public static class DeserializeDynamic<TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

        private static readonly ParseStringSpanDelegate CachedParseFn;
        static DeserializeDynamic()
        {
            CachedParseFn = ParseDynamic;
        }

        public static ParseStringDelegate Parse => v => CachedParseFn(v.AsSpan());

        public static ParseStringSpanDelegate ParseStringSpan => CachedParseFn;

        public static IDynamicMetaObjectProvider ParseDynamic(string value) => ParseDynamic(value.AsSpan());

        public static IDynamicMetaObjectProvider ParseDynamic(ReadOnlySpan<char> value)
        {
            var index = VerifyAndGetStartIndex(value, typeof(ExpandoObject));

            var result = new ExpandoObject();

            if (JsonTypeSerializer.IsEmptyMap(value)) return result;

            var container = (IDictionary<String, Object>)result;

            var tryToParsePrimitiveTypes = JsConfig.TryToParsePrimitiveTypeValues;

            var valueLength = value.Length;
            while (index < valueLength)
            {
                var keyValue = Serializer.EatMapKey(value, ref index);
                Serializer.EatMapKeySeperator(value, ref index);
                var elementValue = Serializer.EatValue(value, ref index);

                var mapKey = Serializer.UnescapeString(keyValue).ToString();

                if (JsonUtils.IsJsObject(elementValue))
                {
                    container[mapKey] = ParseDynamic(elementValue);
                }
                else if (JsonUtils.IsJsArray(elementValue))
                {
                    container[mapKey] = DeserializeList<List<object>, TSerializer>.ParseStringSpan(elementValue);
                }
                else if (tryToParsePrimitiveTypes)
                {
                    container[mapKey] = DeserializeType<TSerializer>.ParsePrimitive(elementValue) ?? Serializer.UnescapeString(elementValue).Value();
                }
                else
                {
                    container[mapKey] = Serializer.UnescapeString(elementValue).Value();
                }

                Serializer.EatItemSeperatorOrMapEndChar(value, ref index);
            }

            return result;
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
    }

    public class DynamicJson : DynamicObject
    {
        private readonly IDictionary<string, object> _hash = new Dictionary<string, object>();

        public static string Serialize(dynamic instance)
        {
            var json = JsonSerializer.SerializeToString(instance);
            return json;
        }

        public static dynamic Deserialize(string json)
        {
            // Support arbitrary nesting by using JsonObject
            var deserialized = JsonSerializer.DeserializeFromString<JsonObject>(json);
            var hash = deserialized.ToUnescapedDictionary().ToObjectDictionary();
            return new DynamicJson(hash);
        }

        public DynamicJson(IEnumerable<KeyValuePair<string, object>> hash)
        {
            _hash.Clear();
            foreach (var entry in hash)
            {
                _hash.Add(Underscored(entry.Key), entry.Value);
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var name = Underscored(binder.Name);
            _hash[name] = value;
            return _hash[name] == value;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = Underscored(binder.Name);
            return YieldMember(name, out result);
        }

        public override string ToString()
        {
            return JsonSerializer.SerializeToString(_hash);
        }

        private bool YieldMember(string name, out object result)
        {
            if (_hash.ContainsKey(name))
            {
                var json = _hash[name].ToString();
                if (json.TrimStart(' ').StartsWith("{", StringComparison.Ordinal))
                {
                    result = Deserialize(json);
                    return true;
                }
                else if (json.TrimStart(' ').StartsWith("[", StringComparison.Ordinal))
                {
                    result = JsonArrayObjects.Parse(json).Select(a =>
                    {
                        var hash = a.ToDictionary<KeyValuePair<string, string>, string, object>(entry => entry.Key, entry => entry.Value);
                        return new DynamicJson(hash);
                    }).ToArray();
                    return true;
                }
                result = json;
                return _hash[name] == result;
            }
            result = null;
            return false;
        }

        internal static string Underscored(string pascalCase)
        {
            return Underscored(pascalCase.ToCharArray());
        }

        internal static string Underscored(IEnumerable<char> pascalCase)
        {
            var sb = StringBuilderCache.Allocate();
            var i = 0;
            foreach (var c in pascalCase)
            {
                if (char.IsUpper(c) && i > 0)
                {
                    sb.Append("_");
                }
                sb.Append(c);
                i++;
            }
            return StringBuilderCache.ReturnAndFree(sb).ToLowerInvariant();
        }
    }
}
