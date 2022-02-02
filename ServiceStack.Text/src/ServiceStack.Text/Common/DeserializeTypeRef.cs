using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using ServiceStack.Text.Support;

namespace ServiceStack.Text.Common
{
    internal static class DeserializeTypeRef
    {
        internal static SerializationException CreateSerializationError(Type type, string strType)
        {
            return new SerializationException(String.Format(
            "Type definitions should start with a '{0}', expecting serialized type '{1}', got string starting with: {2}",
            JsWriter.MapStartChar, type.Name, strType.Substring(0, strType.Length < 50 ? strType.Length : 50)));
        }

        internal static SerializationException GetSerializationException(string propertyName, string propertyValueString, Type propertyType, Exception e)
        {
            var serializationException = new SerializationException($"Failed to set property '{propertyName}' with '{propertyValueString}'", e);
            if (propertyName != null)
            {
                serializationException.Data.Add("propertyName", propertyName);
            }
            if (propertyValueString != null)
            {
                serializationException.Data.Add("propertyValueString", propertyValueString);
            }
            if (propertyType != null)
            {
                serializationException.Data.Add("propertyType", propertyType);
            }
            return serializationException;
        }

        private static Dictionary<Type, KeyValuePair<string, TypeAccessor>[]> TypeAccessorsCache = new Dictionary<Type, KeyValuePair<string, TypeAccessor>[]>();

        internal static KeyValuePair<string, TypeAccessor>[] GetCachedTypeAccessors(Type type, ITypeSerializer serializer)
        {
            if (TypeAccessorsCache.TryGetValue(type, out var typeAccessors))
                return typeAccessors;

            var typeConfig = new TypeConfig(type);
            typeAccessors = GetTypeAccessors(typeConfig, serializer);

            Dictionary<Type, KeyValuePair<string, TypeAccessor>[]> snapshot, newCache;
            do
            {
                snapshot = TypeAccessorsCache;
                newCache = new Dictionary<Type, KeyValuePair<string, TypeAccessor>[]>(TypeAccessorsCache) {
                    [type] = typeAccessors
                };
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref TypeAccessorsCache, newCache, snapshot), snapshot));

            return typeAccessors;
        }

        internal static KeyValuePair<string, TypeAccessor>[] GetTypeAccessors(TypeConfig typeConfig, ITypeSerializer serializer)
        {
            var type = typeConfig.Type;

            var propertyInfos = type.GetSerializableProperties();
            var fieldInfos = type.GetSerializableFields();
            if (propertyInfos.Length == 0 && fieldInfos.Length == 0) 
                return default;

            var accessors = new KeyValuePair<string, TypeAccessor>[propertyInfos.Length + fieldInfos.Length];
            var i = 0;

            if (propertyInfos.Length != 0)
            {
                for (; i < propertyInfos.Length; i++)
                {
                    var propertyInfo = propertyInfos[i];
                    var propertyName = propertyInfo.Name;
                    var dcsDataMember = propertyInfo.GetDataMember();
                    if (dcsDataMember?.Name != null)
                    {
                        propertyName = dcsDataMember.Name;
                    }

                    accessors[i] = new KeyValuePair<string, TypeAccessor>(propertyName, TypeAccessor.Create(serializer, typeConfig, propertyInfo));
                }
            }

            if (fieldInfos.Length != 0)
            {
                for (var j=0; j < fieldInfos.Length; j++)
                {
                    var fieldInfo = fieldInfos[j];
                    var fieldName = fieldInfo.Name;
                    var dcsDataMember = fieldInfo.GetDataMember();
                    if (dcsDataMember?.Name != null)
                    {
                        fieldName = dcsDataMember.Name;
                    }

                    accessors[i + j] = new KeyValuePair<string, TypeAccessor>(fieldName, TypeAccessor.Create(serializer, typeConfig, fieldInfo));
                }
            }
            
            Array.Sort(accessors, (x,y) => string.Compare(x.Key, y.Key, StringComparison.OrdinalIgnoreCase));
            return accessors;
        }
   }

    //The same class above but JSON-specific to enable inlining in this hot class.
}