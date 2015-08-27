//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !(PCL || LITE)

using System;
using System.Collections.Generic;
using System.Dynamic;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;
using System.Linq;
using System.Text;

namespace ServiceStack
{
    public static class DeserializeDynamic<TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

        private static readonly ParseStringDelegate CachedParseFn;
        static DeserializeDynamic()
        {
            CachedParseFn = ParseDynamic;
        }

        public static ParseStringDelegate Parse
        {
            get { return CachedParseFn; }
        }

        public static IDynamicMetaObjectProvider ParseDynamic(string value)
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

                var mapKey = Serializer.UnescapeString(keyValue);

                if (JsonUtils.IsJsObject(elementValue))
                {
                    container[mapKey] = ParseDynamic(elementValue);
                }
                else if (JsonUtils.IsJsArray(elementValue))
                {
                    container[mapKey] = DeserializeList<List<object>, TSerializer>.Parse(elementValue);
                }
                else if (tryToParsePrimitiveTypes)
                {
                    container[mapKey] = DeserializeType<TSerializer>.ParsePrimitive(elementValue) ?? Serializer.UnescapeString(elementValue);
                }
                else
                {
                    container[mapKey] = Serializer.UnescapeString(elementValue);
                }

                Serializer.EatItemSeperatorOrMapEndChar(value, ref index);
            }

            return result;
        }

        private static int VerifyAndGetStartIndex(string value, Type createMapType)
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

//TODO: Workout how to fix broken CoreCLR SL5 build that uses dynamic
#if !(SL5 && CORECLR)

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
            var hash = deserialized.ToDictionary<KeyValuePair<string, string>, string, object>(entry => entry.Key, entry => entry.Value);
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
            var sb = new StringBuilder();
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
            return sb.ToString().ToLowerInvariant();
        }
    }
#endif
}

#endif
