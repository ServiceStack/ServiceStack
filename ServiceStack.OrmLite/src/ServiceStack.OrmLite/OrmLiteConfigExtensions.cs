//
// ServiceStack.OrmLite: Light-weight POCO ORM for .NET and Mono
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

internal static class OrmLiteConfigExtensions
{
    private static Dictionary<Type, ModelDefinition> typeModelDefinitionMap = new Dictionary<Type, ModelDefinition>();

    internal static bool CheckForIdField(IEnumerable<PropertyInfo> objProperties)
    {
        // Not using Linq.Where() and manually iterating through objProperties just to avoid dependencies on System.Xml??
        foreach (var objProperty in objProperties)
        {
            if (objProperty.Name != OrmLiteConfig.IdField) continue;
            return true;
        }
        return false;
    }

    internal static void ClearCache()
    {
        typeModelDefinitionMap = new Dictionary<Type, ModelDefinition>();
    }

    internal static ModelDefinition GetModelDefinition(this Type modelType)
    {
        if (modelType == null)
            return null;
        if (typeModelDefinitionMap.TryGetValue(modelType, out var modelDef))
            return modelDef;

        if (modelType.IsValueType || modelType == typeof(string))
            return null;

        var modelAliasAttr = modelType.FirstAttribute<AliasAttribute>();
        var schemaAttr = modelType.FirstAttribute<SchemaAttribute>();

        var preCreates = modelType.AllAttributes<PreCreateTableAttribute>();
        var postCreates = modelType.AllAttributes<PostCreateTableAttribute>();
        var preDrops = modelType.AllAttributes<PreDropTableAttribute>();
        var postDrops = modelType.AllAttributes<PostDropTableAttribute>();

        string JoinSql(List<string> statements)
        {
            if (statements.Count == 0)
                return null;
            var sb = StringBuilderCache.Allocate();
            foreach (var sql in statements)
            {
                if (sb.Length > 0)
                    sb.AppendLine(";");
                sb.Append(sql);
            }
            var to = StringBuilderCache.ReturnAndFree(sb);
            return to;
        }

        modelDef = new ModelDefinition
        {
            ModelType = modelType,
            Name = modelType.Name,
            Alias = modelAliasAttr?.Name,
            Schema = schemaAttr?.Name,
            PreCreateTableSql = JoinSql(preCreates.Map(x => x.Sql)),
            PostCreateTableSql = JoinSql(postCreates.Map(x => x.Sql)),
            PreDropTableSql = JoinSql(preDrops.Map(x => x.Sql)),
            PostDropTableSql = JoinSql(postDrops.Map(x => x.Sql)),
        };

        modelDef.CompositeIndexes.AddRange(
            modelType.AllAttributes<CompositeIndexAttribute>().ToList());

        modelDef.UniqueConstraints.AddRange(
            modelType.AllAttributes<UniqueConstraintAttribute>().ToList());

        var objProperties = modelType.GetProperties(
            BindingFlags.Public | BindingFlags.Instance).ToList();

        var hasPkAttr = objProperties.Any(p => p.HasAttributeCached<PrimaryKeyAttribute>());

        var hasIdField = CheckForIdField(objProperties);

        var i = 0;
        var propertyInfoIdx = 0;
        foreach (var propertyInfo in objProperties)
        {
            if (propertyInfo.GetIndexParameters().Length > 0)
                continue; //Is Indexer

            var sequenceAttr = propertyInfo.FirstAttribute<SequenceAttribute>();
            var computeAttr = propertyInfo.FirstAttribute<ComputeAttribute>();
            var computedAttr = propertyInfo.FirstAttribute<ComputedAttribute>();
            var persistedAttr = propertyInfo.FirstAttribute<PersistedAttribute>();
            var customSelectAttr = propertyInfo.FirstAttribute<CustomSelectAttribute>();
            var decimalAttribute = propertyInfo.FirstAttribute<DecimalLengthAttribute>();
            var belongToAttribute = propertyInfo.FirstAttribute<BelongToAttribute>();
            var referenceAttr = propertyInfo.FirstAttribute<ReferenceAttribute>();
            var referenceFieldAttr = propertyInfo.FirstAttribute<ReferenceFieldAttribute>();

            var isRowVersion = propertyInfo.Name == ModelDefinition.RowVersionName
                               && (propertyInfo.PropertyType == typeof(ulong) || propertyInfo.PropertyType == typeof(byte[]));

            var isNullableType = propertyInfo.PropertyType.IsNullableType();

            var isNullable = (!propertyInfo.PropertyType.IsValueType || isNullableType)
                             && !propertyInfo.HasAttributeNamed(nameof(RequiredAttribute));

            var propertyType = isNullableType
                ? Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                : propertyInfo.PropertyType;


            Type treatAsType = null;

            if (propertyType.IsEnum)
            {
                var enumKind = Converters.EnumConverter.GetEnumKind(propertyType);
                if (enumKind == EnumKind.Int)
                    treatAsType = Enum.GetUnderlyingType(propertyType);
                else if (enumKind == EnumKind.Char)
                    treatAsType = typeof(char);
            }

            var isReference = referenceAttr != null || referenceFieldAttr != null;
            var isIgnored = propertyInfo.HasAttributeCached<IgnoreAttribute>() || isReference || computedAttr != null;

            var isFirst = !isIgnored && i++ == 0;

            var isAutoId = propertyInfo.HasAttributeCached<AutoIdAttribute>();

            var isPrimaryKey = (!hasPkAttr && (propertyInfo.Name == OrmLiteConfig.IdField || (!hasIdField && isFirst)))
                               || propertyInfo.HasAttributeNamed(nameof(PrimaryKeyAttribute))
                               || isAutoId;

            var isAutoIncrement = isPrimaryKey && propertyInfo.HasAttributeCached<AutoIncrementAttribute>();

            if (isAutoIncrement && propertyInfo.PropertyType == typeof(Guid))
                throw new NotSupportedException($"[AutoIncrement] is only valid for integer properties for {modelType.Name}.{propertyInfo.Name} Guid property use [AutoId] instead");

            if (isAutoId && (propertyInfo.PropertyType == typeof(int) || propertyInfo.PropertyType == typeof(long)))
                throw new NotSupportedException($"[AutoId] is only valid for Guid properties for {modelType.Name}.{propertyInfo.Name} integer property use [AutoIncrement] instead");

            var aliasAttr = propertyInfo.FirstAttribute<AliasAttribute>();

            var indexAttr = propertyInfo.FirstAttribute<IndexAttribute>();
            var isIndex = indexAttr != null;
            var isUnique = isIndex && indexAttr.Unique;

            var stringLengthAttr = propertyInfo.CalculateStringLength(decimalAttribute);

            var defaultValueAttr = propertyInfo.FirstAttribute<DefaultAttribute>();

            var referencesAttr = propertyInfo.FirstAttribute<ReferencesAttribute>();
            var fkAttr = propertyInfo.FirstAttribute<ForeignKeyAttribute>();
            var customFieldAttr = propertyInfo.FirstAttribute<CustomFieldAttribute>();
            var chkConstraintAttr = propertyInfo.FirstAttribute<CheckConstraintAttribute>();

            var order = propertyInfoIdx++;
            if (customFieldAttr != null && customFieldAttr.Order != 0)
            {
                order = customFieldAttr.Order;
            }

            var fieldDefinition = new FieldDefinition
            {
                ModelDef = modelDef,
                Name = propertyInfo.Name,
                Alias = aliasAttr?.Name,
                FieldType = propertyType,
                FieldTypeDefaultValue = isNullable ? null : propertyType.GetDefaultValue(),
                TreatAsType = treatAsType,
                PropertyInfo = propertyInfo,
                IsNullable = isNullable,
                IsPrimaryKey = isPrimaryKey,
                AutoIncrement = isPrimaryKey && isAutoIncrement,
                AutoId = isAutoId,
                IsIndexed = !isPrimaryKey && isIndex,
                IsUniqueIndex = isUnique,
                IsClustered = indexAttr?.Clustered == true,
                IsNonClustered = indexAttr?.NonClustered == true,
                IndexName = indexAttr?.Name,
                IsRowVersion = isRowVersion,
                IgnoreOnInsert = propertyInfo.HasAttributeCached<IgnoreOnInsertAttribute>(),
                IgnoreOnUpdate = propertyInfo.HasAttributeCached<IgnoreOnUpdateAttribute>(),
                ReturnOnInsert = propertyInfo.HasAttributeCached<ReturnOnInsertAttribute>(),
                FieldLength = stringLengthAttr?.MaximumLength,
                DefaultValue = defaultValueAttr?.DefaultValue,
                DefaultValueConstraint = defaultValueAttr?.WithConstraint,
                CheckConstraint = chkConstraintAttr?.Constraint,
                IsUniqueConstraint = propertyInfo.HasAttributeCached<UniqueAttribute>(),
                ForeignKey = fkAttr == null
                    ? referencesAttr != null ? new ForeignKeyConstraint(referencesAttr.Type) : null
                    : new ForeignKeyConstraint(fkAttr.Type, fkAttr.OnDelete, fkAttr.OnUpdate, fkAttr.ForeignKeyName),
                IsReference = isReference,
                ReferenceSelfId = referenceAttr?.SelfId,
                ReferenceRefId = referenceAttr?.RefId,
                GetValueFn = propertyInfo.CreateGetter(),
                SetValueFn = propertyInfo.CreateSetter(),
                Sequence = sequenceAttr?.Name,
                IsComputed = computeAttr != null || computedAttr != null || customSelectAttr != null,
                IsPersisted = persistedAttr != null,
                ComputeExpression = computeAttr != null ? computeAttr.Expression : string.Empty,
                CustomSelect = customSelectAttr?.Sql,
                CustomInsert = propertyInfo.FirstAttribute<CustomInsertAttribute>()?.Sql,
                CustomUpdate = propertyInfo.FirstAttribute<CustomUpdateAttribute>()?.Sql,
                Scale = decimalAttribute?.Scale,
                BelongToModelName = belongToAttribute?.BelongToTableType.GetModelDefinition().ModelName,
                CustomFieldDefinition = customFieldAttr?.Sql,
                IsRefType = propertyType.IsRefType(),
                Order = order
            };

            if (referenceFieldAttr != null)
            {
                fieldDefinition.FieldReference = new FieldReference(fieldDefinition) {
                    RefModel = referenceFieldAttr.Model,
                    RefId = referenceFieldAttr.Id,
                    RefField = referenceFieldAttr.Field ?? propertyInfo.Name,
                };
            }

            if (isIgnored)
                modelDef.IgnoredFieldDefinitions.Add(fieldDefinition);
            else
                modelDef.FieldDefinitions.Add(fieldDefinition);

            if (isRowVersion)
                modelDef.RowVersion = fieldDefinition;
        }

        modelDef.AfterInit();

        Dictionary<Type, ModelDefinition> snapshot, newCache;
        do
        {
            snapshot = typeModelDefinitionMap;
            newCache = new Dictionary<Type, ModelDefinition>(typeModelDefinitionMap) { [modelType] = modelDef };

        } while (!ReferenceEquals(
                     Interlocked.CompareExchange(ref typeModelDefinitionMap, newCache, snapshot), snapshot));

        LicenseUtils.AssertValidUsage(LicenseFeature.OrmLite, QuotaType.Tables, typeModelDefinitionMap.Count);

        return modelDef;
    }

    public static StringLengthAttribute CalculateStringLength(this PropertyInfo propertyInfo, DecimalLengthAttribute decimalAttribute)
    {
        var attr = propertyInfo.FirstAttribute<StringLengthAttribute>();
        if (attr != null) return attr;

        var componentAttr = propertyInfo.FirstAttribute<System.ComponentModel.DataAnnotations.StringLengthAttribute>();
        if (componentAttr != null)
            return new StringLengthAttribute(componentAttr.MaximumLength);

        return decimalAttribute != null ? new StringLengthAttribute(decimalAttribute.Precision) : null;
    }
}