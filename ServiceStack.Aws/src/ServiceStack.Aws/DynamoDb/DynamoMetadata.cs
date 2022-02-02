// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Amazon.DynamoDBv2.DataModel;
using ServiceStack.Aws.Support;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Aws.DynamoDb
{
    public class DynamoMetadata
    {
        public static DynamoConverters Converters = new DynamoConverters();

        public static readonly Dictionary<Type, string> FieldTypeMap = new Dictionary<Type, string>()
            .Set<string>(DynamoType.String)
            .Set<bool>(DynamoType.Bool)
            .Set<byte[]>(DynamoType.Binary)
            .Set<Stream>(DynamoType.Binary)
            .Set<MemoryStream>(DynamoType.Binary)
            .Set<byte>(DynamoType.Number)
            .Set<sbyte>(DynamoType.Number)
            .Set<short>(DynamoType.Number)
            .Set<ushort>(DynamoType.Number)
            .Set<int>(DynamoType.Number)
            .Set<uint>(DynamoType.Number)
            .Set<long>(DynamoType.Number)
            .Set<ulong>(DynamoType.Number)
            .Set<float>(DynamoType.Number)
            .Set<double>(DynamoType.Number)
            .Set<decimal>(DynamoType.Number)
            .Set<HashSet<string>>(DynamoType.StringSet)
            .Set<HashSet<int>>(DynamoType.NumberSet)
            .Set<HashSet<long>>(DynamoType.NumberSet)
            .Set<HashSet<double>>(DynamoType.NumberSet)
            .Set<HashSet<float>>(DynamoType.NumberSet)
            .Set<HashSet<decimal>>(DynamoType.NumberSet);

        public static HashSet<DynamoMetadataType> Types;

        public static void Reset()
        {
            Types = null;
        }

        public static DynamoMetadataType GetTable<T>()
        {
            return GetTable(typeof(T));
        }

        public static DynamoMetadataType GetTable(Type table)
        {
            var metadata = Types.FirstOrDefault(x => x.Type == table);
            if (metadata == null || !metadata.IsTable)
                throw new ArgumentNullException(nameof(table), "Table has not been registered: " + table.Name);

            return metadata;
        }

        public static DynamoMetadataType TryGetTable(Type table)
        {
            var metadata = Types.FirstOrDefault(x => x.Type == table);
            if (metadata == null || !metadata.IsTable)
                return null;
            return metadata;
        }

        public static List<DynamoMetadataType> GetTables()
        {
            return Types?.Where(x => x.IsTable).ToList() ?? new List<DynamoMetadataType>();
        }

        public static DynamoMetadataType GetType<T>()
        {
            return GetType(typeof(T));
        }

        public static DynamoMetadataType GetType(Type type)
        {
            var metadata = Types.FirstOrDefault(x => x.Type == type);
            if (metadata != null)
                return metadata;

            if (type.IsValueType)
                return null;

            RegisterTypes(type);

            var metaType = Types.FirstOrDefault(x => x.Type == type);
            return metaType;
        }

        public static void RegisterTables(IEnumerable<Type> tables)
        {
            foreach (var table in tables)
            {
                RegisterTable(table);
            }
        }

        public static DynamoMetadataType RegisterTable<T>()
        {
            return RegisterTable(typeof (T));
        }

        // Should only be called at StartUp
        public static DynamoMetadataType RegisterTable(Type type)
        {
            if (Types == null)
                Types = new HashSet<DynamoMetadataType>();

            Types.RemoveWhere(x => x.Type == type);

            var table = ToMetadataTable(type);
            Types.Add(table);

            var tableCount = Types.Count(x => x.IsTable);
            LicenseUtils.AssertValidUsage(LicenseFeature.Aws, QuotaType.Tables, tableCount);

            RegisterTypes(type.GetReferencedTypes());

            return table;
        }

        public static void RegisterTypes(params Type[] refTypes)
        {
            var metadatas = refTypes.Where(x => !x.IsValueType 
                && !x.IsSystemType() 
                && Types.All(t => t.Type != x))
            .Map(ToMetadataType);

            // Make thread-safe to allow usage at runtime
            HashSet<DynamoMetadataType> snapshot, newCache;
            do
            {
                snapshot = Types;
                newCache = new HashSet<DynamoMetadataType>(Types);
                foreach (var metadata in metadatas)
                {
                    newCache.Add(metadata);
                }
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref Types, newCache, snapshot), snapshot));
        }

        private static PropertyInfo[] GetTableProperties(Type type)
        {
            var props = type.GetPublicProperties()
                .Where(x => x.CanRead && x.CanWrite && !x.HasAttribute<IgnoreAttribute>())
                .ToArray();
            return props;
        }

        private static DynamoMetadataType ToMetadataType(Type type)
        {
            var alias = type.FirstAttribute<AliasAttribute>();
            var props = GetTableProperties(type);

            var metadata = new DynamoMetadataType
            {
                Type = type,
                Name = alias != null ? alias.Name : type.Name,
            };
            metadata.Fields = props.Map(p =>
                new DynamoMetadataField
                {
                    Parent = metadata,
                    Type = p.PropertyType,
                    PropertyInfo = p,
                    Name = Converters.GetFieldName(p),
                    DbType = Converters.GetFieldType(p.PropertyType),
                    IsAutoIncrement = p.HasAttribute<AutoIncrementAttribute>(),
                    SetValueFn = p.CreateSetter(),
                    GetValueFn = p.CreateGetter(),
                }).ToArray();

            return metadata;
        }

        private static DynamoMetadataType ToMetadataTable(Type type)
        {
            var alias = type.FirstAttribute<AliasAttribute>();
            var props = GetTableProperties(type);
            Converters.GetHashAndRangeKeyFields(type, props, out var hash, out var range);

            var provision = type.FirstAttribute<ProvisionedThroughputAttribute>();

            // If its a generic type, the type name will contain illegal characters (not accepted as DynamoDB table name)
            // so, remove the Tilde and make the type name unique to the runtime type of the generic
            string genericTypeNameAlias = null;
            if (type.IsGenericType)
            {
                var indexOfTilde = type.Name.IndexOf("`", StringComparison.Ordinal);
                indexOfTilde = indexOfTilde < 1 ? type.Name.Length - 1 : indexOfTilde;
                genericTypeNameAlias = type.Name.Substring(0, indexOfTilde) + type.GetGenericArguments().Select(t => t.Name).Join();
            }

            var metadata = new DynamoMetadataType
            {
                Type = type,
                IsTable = true,
                Name = alias != null ? alias.Name : genericTypeNameAlias ?? type.Name,
                ReadCapacityUnits = provision?.ReadCapacityUnits,
                WriteCapacityUnits = provision?.WriteCapacityUnits,
            };
            metadata.Fields = props.Map(p =>
                new DynamoMetadataField
                {
                    Parent = metadata,
                    Type = p.PropertyType,
                    PropertyInfo = p,
                    Name = Converters.GetFieldName(p),
                    DbType = Converters.GetFieldType(p.PropertyType),
                    IsHashKey = p == hash,
                    IsRangeKey = p == range,
                    ExcludeNullValue = p.HasAttribute<IndexAttribute>() || p.HasAttribute<ExcludeNullValueAttribute>(),
                    IsAutoIncrement = p.HasAttribute<AutoIncrementAttribute>(),
                    SetValueFn = p.CreateSetter(),
                    GetValueFn = p.CreateGetter(),
                }).ToArray();

            metadata.HashKey = metadata.Fields.FirstOrDefault(x => x.IsHashKey);
            metadata.RangeKey = metadata.Fields.FirstOrDefault(x => x.IsRangeKey);

            if (metadata.HashKey == null)
                throw new ArgumentException($"Could not infer Hash Key in Table '{type.Name}'");

            var hashField = metadata.HashKey.Name;

            metadata.LocalIndexes = props.Where(x => x.HasAttribute<IndexAttribute>()).Map(x =>
            {
                var indexProjection = x.FirstAttribute<ProjectionTypeAttribute>();
                var projectionType = indexProjection?.ProjectionType ?? DynamoProjectionType.Include;
                return new DynamoLocalIndex
                {
                    Name = $"{metadata.Name}{x.Name}Index",
                    HashKey = metadata.HashKey,
                    RangeKey = metadata.GetField(x.Name),
                    ProjectionType = projectionType,
                    ProjectedFields = projectionType == DynamoProjectionType.Include 
                        ? new[] { x.Name } 
                        : new string[0],
                };
            });

            metadata.GlobalIndexes = new List<DynamoGlobalIndex>();

            var references = type.AllAttributes<ReferencesAttribute>();
            foreach (var attr in references)
            {
                var localIndex = attr.Type.GetTypeWithGenericInterfaceOf(typeof(ILocalIndex<>));
                if (localIndex != null)
                    metadata.LocalIndexes.Add(CreateLocalIndex(type, metadata, hashField, attr.Type));

                var globalIndex = attr.Type.GetTypeWithGenericInterfaceOf(typeof(IGlobalIndex<>));
                if (globalIndex != null)
                    metadata.GlobalIndexes.Add(CreateGlobalIndex(type, metadata, attr.Type));
            }

            return metadata;
        }

        private static DynamoLocalIndex CreateLocalIndex(Type type, DynamoMetadataType metadata, string hashField, Type indexType)
        {
            var indexProps = indexType.GetPublicProperties();
            var indexProp = indexProps.FirstOrDefault(x =>
                x.HasAttribute<IndexAttribute>() || x.HasAttribute<DynamoDBRangeKeyAttribute>());

            if (indexProp == null)
                throw new ArgumentException($"Missing [Index]. Could not infer Range Key in index '{indexType}'.");

            var indexAlias = indexType.FirstAttribute<AliasAttribute>();
            var rangeKey = metadata.GetField(indexProp.Name);
            if (rangeKey == null)
                throw new ArgumentException($"Range Key '{indexProp.Name}' was not found on Table '{type.Name}'");

            var indexProjection = indexType.FirstAttribute<ProjectionTypeAttribute>();
            var projectionType = indexProjection?.ProjectionType ?? DynamoProjectionType.Include;

            return new DynamoLocalIndex
            {
                IndexType = indexType,
                Name = indexAlias != null ? indexAlias.Name : indexType.Name,
                HashKey = metadata.HashKey,
                RangeKey = rangeKey,
                ProjectionType = projectionType,
                ProjectedFields = projectionType == DynamoProjectionType.Include 
                    ? indexProps.Where(x => x.Name != hashField).Select(x => x.Name).ToArray()
                    : new string[0],
            };
        }

        private static DynamoGlobalIndex CreateGlobalIndex(Type type, DynamoMetadataType metadata, Type indexType)
        {
            var indexProps = indexType.GetPublicProperties();

            Converters.GetHashAndRangeKeyFields(indexType, indexProps, out var indexHash, out var indexRange);

            var hashKey = metadata.GetField(indexHash.Name);
            if (hashKey == null)
                throw new ArgumentException($"Hash Key '{indexHash.Name}' was not found on Table '{type.Name}'");

            if (indexRange == null)
                indexRange = indexProps.FirstOrDefault(x => x.HasAttribute<IndexAttribute>());

            var rangeKey = indexRange != null
                ? metadata.GetField(indexRange.Name)
                : null;

            var indexAlias = indexType.FirstAttribute<AliasAttribute>();

            var indexProvision = indexType.FirstAttribute<ProvisionedThroughputAttribute>();

            var indexProjection = indexType.FirstAttribute<ProjectionTypeAttribute>();
            var projectionType = indexProjection?.ProjectionType ?? DynamoProjectionType.Include;

            return new DynamoGlobalIndex
            {
                IndexType = indexType,
                Name = indexAlias != null ? indexAlias.Name : indexType.Name,
                HashKey = hashKey,
                RangeKey = rangeKey,
                ProjectionType = projectionType,
                ProjectedFields = projectionType == DynamoProjectionType.Include
                    ? indexProps.Where(x => x.Name != indexHash.Name).Select(x => x.Name).ToArray()
                    : new string[0],
                ReadCapacityUnits = indexProvision?.ReadCapacityUnits ?? metadata.ReadCapacityUnits,
                WriteCapacityUnits = indexProvision?.WriteCapacityUnits ?? metadata.WriteCapacityUnits,
            };
        }
    }

    public class DynamoMetadataType
    {
        public string Name { get; set; }

        public bool IsTable { get; set; }

        public Type Type { get; set; }

        public DynamoMetadataField[] Fields { get; set; }

        public DynamoMetadataField HashKey { get; set; }

        public DynamoMetadataField RangeKey { get; set; }

        public List<DynamoLocalIndex> LocalIndexes { get; set; }

        public List<DynamoGlobalIndex> GlobalIndexes { get; set; }

        public int? ReadCapacityUnits { get; set; }

        public int? WriteCapacityUnits { get; set; }

        public DynamoMetadataField GetField(string fieldName)
        {
            return Fields.FirstOrDefault(x => x.PropertyInfo.Name == fieldName)
                ?? Fields.FirstOrDefault(x => x.Name == fieldName);
        }

        public bool HasField(string fieldName)
        {
            return GetField(fieldName) != null;
        }

        public DynamoMetadataField GetField(Type type)
        {
            return Fields.FirstOrDefault(x => x.Type == type);
        }

        public DynamoIndex GetIndex(Type indexType)
        {
            return (DynamoIndex)this.LocalIndexes.FirstOrDefault(x => x.IndexType == indexType)
                ?? this.GlobalIndexes.FirstOrDefault(x => x.IndexType == indexType);
        }

        public DynamoIndex GetIndexByField(string fieldName)
        {
            return (DynamoIndex)this.LocalIndexes.FirstOrDefault(x => x.RangeKey != null && x.RangeKey.Name == fieldName)
                ?? this.GlobalIndexes.FirstOrDefault(x => x.RangeKey != null && x.RangeKey.Name == fieldName);
        }
    }

    public class DynamoMetadataField
    {
        public DynamoMetadataType Parent { get; set; }

        public Type Type { get; set; }
        public PropertyInfo PropertyInfo { get; set; }

        public string Name { get; set; }

        public string DbType { get; set; }

        public bool IsHashKey { get; set; }

        public bool IsRangeKey { get; set; }

        public bool IsAutoIncrement { get; set; }

        public bool ExcludeNullValue { get; set; }

        public GetMemberDelegate GetValueFn { get; set; }

        public SetMemberDelegate SetValueFn { get; set; }

        public object GetValue(object onInstance)
        {
            return GetValueFn?.Invoke(onInstance);
        }

        public object SetValue(object instance, object value)
        {
            if (SetValueFn == null)
                return value;

            if (value != null && value.GetType() != Type)
                value = Convert.ChangeType(value, Type);

            SetValueFn(instance, value);

            return value;
        }
    }

    public static class DynamoProjectionType
    {
        public const string KeysOnly = "KEYS_ONLY";
        public const string Include = "INCLUDE";
        public const string All = "ALL";
    }

    public class DynamoIndex
    {
        public Type IndexType { get; set; }
        public string Name { get; set; }
        public DynamoMetadataField HashKey { get; set; }
        public DynamoMetadataField RangeKey { get; set; }
        public string ProjectionType { get; set; }
        public string[] ProjectedFields { get; set; }
    }

    public class DynamoLocalIndex : DynamoIndex
    {
    }

    public class DynamoGlobalIndex : DynamoIndex
    {
        public long? ReadCapacityUnits { get; set; }
        public long? WriteCapacityUnits { get; set; }
    }
}