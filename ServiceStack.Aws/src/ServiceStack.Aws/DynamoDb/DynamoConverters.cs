// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Aws.Support;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDb
{
    public class DynamoConverters
    {
        public static Func<Type, string> FieldTypeFn { get; set; }
        public static Func<object, DynamoMetadataType, Dictionary<string, AttributeValue>> ToAttributeValuesFn { get; set; }
        public static Func<Type, object, AttributeValue> ToAttributeValueFn { get; set; }
        public static Func<AttributeValue, Type, object> FromAttributeValueFn { get; set; }
        public static Func<object, Type, object> ConvertValueFn { get; set; }

        public Dictionary<Type, IAttributeValueConverter> ValueConverters = new Dictionary<Type, IAttributeValueConverter>
        {
            {typeof(DateTime), new DateTimeConverter() },
        };

        public IAttributeValueConverter EnumConverter = new EnumConverter();

        public virtual string GetFieldName(PropertyInfo pi)
        {
            var dynoAttr = pi.FirstAttribute<DynamoDBPropertyAttribute>();
            if (dynoAttr?.AttributeName != null)
                return dynoAttr.AttributeName;

            var alias = pi.FirstAttribute<AliasAttribute>();
            if (alias?.Name != null)
                return alias.Name;

            return pi.Name;
        }

        public virtual string GetFieldType(Type type)
        {
            string fieldType;

            if (FieldTypeFn != null)
            {
                fieldType = FieldTypeFn(type);
                if (fieldType != null)
                    return fieldType;
            }

            if (DynamoMetadata.FieldTypeMap.TryGetValue(type, out fieldType))
                return fieldType;

            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null && DynamoMetadata.FieldTypeMap.TryGetValue(nullable, out fieldType))
                return fieldType;

            if (type.IsOrHasGenericInterfaceTypeOf(typeof(IDictionary<,>)))
                return DynamoType.Map;

            if (type.IsOrHasGenericInterfaceTypeOf(typeof(ICollection<>)))
                return DynamoType.List;

            if (type.IsUserType())
                return DynamoType.Map;

            return DynamoType.String;
        }

        public virtual object ConvertValue(object value, Type type)
        {
            if (type.IsInstanceOfType(value))
                return value;

            var to = ConvertValueFn?.Invoke(value, type);
            if (to != null)
                return to;

            if (value is Dictionary<string, AttributeValue> mapValue)
                return FromMapAttributeValue(mapValue, type);

            if (value is List<AttributeValue> listValue)
                return FromListAttributeValue(listValue, type);

            return value.ConvertTo(type);
        }

        public virtual void GetHashAndRangeKeyFields(Type type, PropertyInfo[] props, out PropertyInfo hash, out PropertyInfo range)
        {
            hash = null;
            range = null;

            if (props.Length == 0)
                return;

            hash = GetHashKey(props);
            range = props.FirstOrDefault(x => x.HasAttribute<DynamoDBRangeKeyAttribute>())
                 ?? props.FirstOrDefault(x => x.HasAttribute<RangeKeyAttribute>())
                 ?? props.FirstOrDefault(x => x.Name == DynamoProperty.RangeKey);

            //If there's only a single FK attribute that's not overridden by specific Hash or Range attrs
            //Set the hash key as the FK to keep related records in the same hash and 
            //Set the range key as the PK to uniquely defined the record
            var referenceAttrProps = props.Where(x => x.HasAttribute<ReferencesAttribute>()).ToList();
            if (hash == null && range == null && referenceAttrProps.Count == 1)
            {
                hash = referenceAttrProps[0];
                range = GetPrimaryKey(props) ?? props[0];
            }
            else if (hash == null)
            {
                var compositeKey = type.FirstAttribute<CompositeKeyAttribute>();
                if (compositeKey != null && compositeKey.FieldNames.Count > 0)
                {
                    if (compositeKey.FieldNames.Count > 2)
                        throw new ArgumentException("Only max of 2 fields allowed in [CompositeIndex] for defining Hash and Range Key");

                    var hashField = compositeKey.FieldNames[0];
                    hash = props.FirstOrDefault(x => x.Name == hashField);
                    if (hash == null)
                        throw new ArgumentException($"Could not find Hash Key field '{hashField}' in CompositeIndex");

                    if (compositeKey.FieldNames.Count == 2)
                    {
                        var rangeField = compositeKey.FieldNames[1];
                        range = props.FirstOrDefault(x => x.Name == rangeField);
                        if (range == null)
                            throw new ArgumentException($"Could not find Range Key field '{rangeField}' in CompositeIndex");
                    }
                }
                else
                {
                    //Otherwise set the Id as the hash key if hash key is not explicitly defined
                    hash = GetPrimaryKey(props);
                    if(hash == null) throw new ArgumentException("Could not determine which property should be the Hash Key. Please refer to https://github.com/ServiceStack/PocoDynamo#table-definition for details on defining hash and range keys.");
                }
            }
        }

        private static PropertyInfo GetHashKey(PropertyInfo[] props)
        {
            return props.FirstOrDefault(x => x.HasAttribute<DynamoDBHashKeyAttribute>())
                   ?? props.FirstOrDefault(x => x.HasAttribute<HashKeyAttribute>())
                   ?? props.FirstOrDefault(x => x.Name == DynamoProperty.HashKey);
        }

        private static PropertyInfo GetPrimaryKey(PropertyInfo[] props)
        {
            return props.FirstOrDefault(x =>
                    x.HasAttribute<PrimaryKeyAttribute>() ||
                    x.HasAttribute<AutoIncrementAttribute>())
                ?? props.FirstOrDefault(x => x.Name.EqualsIgnoreCase(IdUtils.IdField));
        }

        public virtual Dictionary<string, AttributeValue> ToAttributeKeyValue(IPocoDynamo db, DynamoMetadataField field, object hash)
        {
            using (AwsClientUtils.GetJsScope())
            {
                return new Dictionary<string, AttributeValue> {
                    { field.Name, ToAttributeValue(db, field.Type, field.DbType, hash) },
                };
            }
        }

        public virtual Dictionary<string, AttributeValue> ToAttributeKeyValue(IPocoDynamo db, DynamoMetadataType table, DynamoId id)
        {
            using (AwsClientUtils.GetJsScope())
            {
                return new Dictionary<string, AttributeValue> {
                    { table.HashKey.Name, ToAttributeValue(db, table.HashKey.Type, table.HashKey.DbType, id.Hash) },
                    { table.RangeKey.Name, ToAttributeValue(db, table.RangeKey.Type, table.RangeKey.DbType, id.Range) },
                };
            }
        }

        public virtual Dictionary<string, AttributeValue> ToAttributeKeyValue(IPocoDynamo db, DynamoMetadataType table, object hash, object range)
        {
            using (AwsClientUtils.GetJsScope())
            {
                var to = new Dictionary<string, AttributeValue> {
                    { table.HashKey.Name, ToAttributeValue(db, table.HashKey.Type, table.HashKey.DbType, hash) },
                };

                if (range != null)
                    to[table.RangeKey.Name] = ToAttributeValue(db, table.RangeKey.Type, table.RangeKey.DbType, range);

                return to;
            }
        }

        public virtual Dictionary<string, AttributeValue> ToAttributeKey(IPocoDynamo db, DynamoMetadataType table, object instance)
        {
            using (AwsClientUtils.GetJsScope())
            {
                var field = table.HashKey;
                var to = new Dictionary<string, AttributeValue> {
                    { field.Name, ToAttributeValue(db, field.Type, field.DbType, field.GetValue(instance)) },
                };

                if (table.RangeKey != null)
                {
                    field = table.RangeKey;
                    to[field.Name] = ToAttributeValue(db, field.Type, field.DbType, field.GetValue(instance));
                }

                return to;
            }
        }

        public virtual Dictionary<string, AttributeValue> ToAttributeValues(IPocoDynamo db, object instance, DynamoMetadataType table)
        {
            var ret = ToAttributeValuesFn?.Invoke(instance, table);
            if (ret != null)
                return ret;

            using (AwsClientUtils.GetJsScope())
            {
                var to = new Dictionary<string, AttributeValue>();

                foreach (var field in table.Fields)
                {
                    var value = field.GetValue(instance);

                    value = ApplyFieldBehavior(db, table, field, instance, value);

                    if (value == null)
                    {
                        if (DynamoConfig.ExcludeNullValues || field.ExcludeNullValue)
                            continue;
                    }

                    to[field.Name] = ToAttributeValue(db, field.Type, field.DbType, value);
                }

                return to;
            }
        }

        public virtual Dictionary<string, AttributeValueUpdate> ToNonDefaultAttributeValueUpdates(IPocoDynamo db, object instance, DynamoMetadataType table)
        {
            using (AwsClientUtils.GetJsScope())
            {
                var to = new Dictionary<string, AttributeValueUpdate>();
                foreach (var field in table.Fields)
                {
                    if (field.IsHashKey || field.IsRangeKey)
                        continue;

                    var value = field.GetValue(instance);

                    if (value == null)
                        continue;

                    to[field.Name] = new AttributeValueUpdate(ToAttributeValue(db, field.Type, field.DbType, value), DynamoAttributeAction.Put);
                }
                return to;
            }
        }

        private static object ApplyFieldBehavior(IPocoDynamo db, DynamoMetadataType type, DynamoMetadataField field, object instance, object value)
        {
            if (type == null || field == null || !field.IsAutoIncrement)
                return value;

            var needsId = IsNumberDefault(value);
            if (!needsId)
                return value;

            var nextId = db.Sequences.Increment(type.Name);
            return field.SetValue(instance, nextId);
        }

        public static bool IsNumberDefault(object value)
        {
            return value == null || 0 == (long)Convert.ChangeType(value, typeof(long));
        }

        public virtual AttributeValue ToAttributeValue(IPocoDynamo db, Type fieldType, string dbType, object value)
        {
            var attrVal = ToAttributeValueFn?.Invoke(fieldType, value);
            if (attrVal != null)
                return attrVal;

            if (value == null)
                return new AttributeValue { NULL = true };

            var valueConverter = GetValueConverter(fieldType);
            if (valueConverter != null)
                return valueConverter.ToAttributeValue(value, fieldType);

            switch (dbType)
            {
                case DynamoType.String:
                    var str = value as string 
                        ?? ((value as DateTimeOffset?)?.ToString(CultureInfo.InvariantCulture) ?? value.ToString());
                    return str == "" //DynamoDB throws on String.Empty
                        ? new AttributeValue { NULL = true } 
                        : new AttributeValue { S = str };
                case DynamoType.Number:
                    return new AttributeValue
                    {
                        N = value is string numStr
                            ? numStr
                            : DynamicNumber.GetNumber(value.GetType()).ToString(value)
                    };
                case DynamoType.Bool:
                    return new AttributeValue { BOOL = (bool)value };
                case DynamoType.Binary:
                    return value is MemoryStream stream
                        ? new AttributeValue { B = stream }
                        : value is Stream
                            ? new AttributeValue { B = new MemoryStream(((Stream)value).ReadFully()) }
                            : new AttributeValue { B = new MemoryStream((byte[])value) };
                case DynamoType.NumberSet:
                    return ToNumberSetAttributeValue(value);
                case DynamoType.StringSet:
                    return ToStringSetAttributeValue(value);
                case DynamoType.List:
                    return ToListAttributeValue(db, value);
                case DynamoType.Map:
                    return ToMapAttributeValue(db, value);
                default:
                    return new AttributeValue { S = value.ToJsv() };
            }
        }

        public virtual AttributeValue ToNumberSetAttributeValue(object value)
        {
            var to = new AttributeValue { NS = value.ConvertTo<List<string>>() };
            //DynamoDB does not support empty sets
            //http://docs.amazonaws.cn/en_us/amazondynamodb/latest/developerguide/DataModel.html
            if (to.NS.Count == 0)
                to.NULL = true;
            return to;
        }

        public virtual AttributeValue ToStringSetAttributeValue(object value)
        {
            var to = new AttributeValue { SS = value.ConvertTo<List<string>>() };
            //DynamoDB does not support empty sets
            //http://docs.amazonaws.cn/en_us/amazondynamodb/latest/developerguide/DataModel.html
            if (to.SS.Count == 0)
                to.NULL = true;
            return to;
        }

        public virtual AttributeValue ToMapAttributeValue(IPocoDynamo db, object oMap)
        {
            var map = oMap as IDictionary
                ?? oMap.ToObjectDictionary();

            var meta = DynamoMetadata.GetType(oMap.GetType());

            var to = new Dictionary<string, AttributeValue>();
            foreach (var key in map.Keys)
            {
                var value = map[key];
                if (value != null)
                {
                    value = ApplyFieldBehavior(db,
                        meta,
                        meta?.GetField((string)key),
                        oMap,
                        value);
                }

                to[key.ToString()] = value != null
                    ? ToAttributeValue(db, value.GetType(), GetFieldType(value.GetType()), value)
                    : new AttributeValue { NULL = true };
            }
            return new AttributeValue { M = to, IsMSet = true };
        }

        public virtual object FromMapAttributeValue(Dictionary<string, AttributeValue> map, Type type)
        {
            var from = new Dictionary<string, object>();

            var metaType = DynamoMetadata.GetType(type);
            if (metaType == null)
            {
                var toMap = (IDictionary)type.CreateInstance();
                var genericDict = type.GetTypeWithGenericTypeDefinitionOf(typeof(IDictionary<,>));
                if (genericDict != null)
                {
                    var genericArgs = genericDict.GetGenericArguments();
                    var keyType = genericArgs[0];
                    var valueType = genericArgs[1];

                    foreach (var entry in map)
                    {
                        var key = ConvertValue(entry.Key, keyType);
                        toMap[key] = FromAttributeValue(entry.Value, valueType);
                    }

                    return toMap;
                }

                throw new ArgumentException("Unknown Map Type " + type.Name);
            }

            foreach (var field in metaType.Fields)
            {
                if (!map.TryGetValue(field.Name, out var attrValue))
                    continue;

                from[field.Name] = FromAttributeValue(attrValue, field.Type);
            }

            var to = from.FromObjectDictionary(type);
            return to;
        }

        public virtual AttributeValue ToListAttributeValue(IPocoDynamo db, object oList)
        {
            var list = ((IEnumerable)oList).Map(x => x);
            if (list.Count <= 0)
                return new AttributeValue { L = new List<AttributeValue>(), IsLSet = true };

            var elType = list[0].GetType();
            var elMeta = DynamoMetadata.GetType(elType);
            if (elMeta != null)
            {
                var autoIncrFields = elMeta.Fields.Where(x => x.IsAutoIncrement).ToList();
                foreach (var field in autoIncrFields)
                {
                    //Avoid N+1 by fetching a batch of ids
                    var autoIds = db.Sequences.GetNextSequences(elMeta, list.Count);
                    for (var i = 0; i < list.Count; i++)
                    {
                        var instance = list[i];
                        var value = field.GetValue(instance);
                        if (IsNumberDefault(value))
                            field.SetValue(instance, autoIds[i]);
                    }
                }
            }

            var values = list.Map(x => ToAttributeValue(db, x.GetType(), GetFieldType(x.GetType()), x));
            return new AttributeValue { L = values };
        }

        public virtual object FromListAttributeValue(List<AttributeValue> attrs, Type toType)
        {
            var elType = toType.GetCollectionType();
            var from = attrs.Map(x => FromAttributeValue(x, elType));
            var to = TranslateListWithElements.TryTranslateCollections(
                from.GetType(), toType, from);
            return to;
        }

        public virtual T FromAttributeValues<T>(DynamoMetadataType table, Dictionary<string, AttributeValue> attributeValues)
        {
            var to = typeof(T).CreateInstance<T>();
            return PopulateFromAttributeValues(to, table, attributeValues);
        }

        public virtual T PopulateFromAttributeValues<T>(T to, DynamoMetadataType table, Dictionary<string, AttributeValue> attributeValues)
        {
            foreach (var entry in attributeValues)
            {
                var field = table.Fields.FirstOrDefault(x => x.Name == entry.Key);
                if (field?.SetValueFn == null)
                    continue;

                var attrValue = entry.Value;
                var fieldType = field.Type;

                var value = FromAttributeValue(attrValue, fieldType);
                if (value == null)
                    continue;

                field.SetValueFn(to, value);
            }

            return to;
        }

        IAttributeValueConverter GetValueConverter(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (!ValueConverters.TryGetValue(type, out var valueConverter))
            {
                if (type.IsEnum)
                    return EnumConverter;
            }
            return valueConverter;
        }

        private object FromAttributeValue(AttributeValue attrValue, Type fieldType)
        {
            var valueConverter = GetValueConverter(fieldType);
            if (valueConverter != null)
                return valueConverter.FromAttributeValue(attrValue, fieldType);

            var value = FromAttributeValueFn != null
                ? FromAttributeValueFn(attrValue, fieldType) ?? GetAttributeValue(attrValue)
                : GetAttributeValue(attrValue);

            return value == null
                ? null
                : ConvertValue(value, fieldType);
        }

        public virtual object GetAttributeValue(AttributeValue attr)
        {
            if (attr == null || attr.NULL)
                return null;
            if (attr.S != null)
                return attr.S;
            if (attr.N != null)
                return attr.N;
            if (attr.B != null)
                return attr.B;
            if (attr.IsBOOLSet)
                return attr.BOOL;
            if (attr.IsLSet)
                return attr.L;
            if (attr.IsMSet)
                return attr.M;
            if (attr.SS.Count > 0)
                return attr.SS;
            if (attr.NS.Count > 0)
                return attr.NS;
            if (attr.BS.Count > 0)
                return attr.BS;

            return null;
        }

    }

}