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
using System.Collections;
using System.Reflection;

namespace ServiceStack.OrmLite;

public class FieldDefinition
{
    public ModelDefinition ModelDef { get; set; }
    public string Name { get; set; }

    public string Alias { get; set; }

    public string FieldName => this.Alias ?? this.Name;

    public Type FieldType { get; set; }

    public object FieldTypeDefaultValue { get; set; }

    public Type TreatAsType { get; set; }

    public Type ColumnType => TreatAsType ?? FieldType;

    public PropertyInfo PropertyInfo { get; set; }

    public bool IsPrimaryKey { get; set; }

    public bool AutoIncrement { get; set; }

    public bool AutoId { get; set; }

    public bool IsNullable { get; set; }

    public bool IsIndexed { get; set; }

    public bool IsUniqueIndex { get; set; }

    public bool IsClustered { get; set; }

    public bool IsNonClustered { get; set; }
        
    public string IndexName { get; set; }

    public bool IsRowVersion { get; set; }

    public int? FieldLength { get; set; }  // Precision for Decimal Type

    public int? Scale { get; set; }  //  for decimal type

    public string DefaultValue { get; set; }
    public string DefaultValueConstraint { get; set; }

    public string CheckConstraint { get; set; }

    public bool IsUniqueConstraint { get; set; }

    public int Order { get; set; }
        
    public ForeignKeyConstraint ForeignKey { get; set; }

    public GetMemberDelegate GetValueFn { get; set; }

    public SetMemberDelegate SetValueFn { get; set; }

    public object GetValue(object instance)
    {
        var type = instance.GetType(); 
        if (PropertyInfo.DeclaringType?.IsAssignableFrom(type) != true)
        {
            if (instance is IDictionary d)
                return d[Name];

            var accessor = TypeProperties.Get(type).GetAccessor(Name);
            return accessor?.PublicGetter(instance);
        }
            
        return this.GetValueFn?.Invoke(instance);
    }

    public void SetValue(object instance, object value)
    {
        if (instance is IDictionary d)
        {
            d[Name] = value;
            return;
        }
            
        this.SetValueFn?.Invoke(instance, value);
    }

    public string GetQuotedName(IOrmLiteDialectProvider dialectProvider)
    {
        return IsRowVersion
            ? dialectProvider.GetRowVersionSelectColumn(this).ToString()
            : dialectProvider.GetQuotedColumnName(this);
    }

    public string GetQuotedValue(object fromInstance, IOrmLiteDialectProvider dialect = null)
    {
        var value = GetValue(fromInstance);
        return (dialect ?? OrmLiteConfig.DialectProvider).GetQuotedValue(value, ColumnType);
    }

    public string Sequence { get; set; }

    public bool IsComputed { get; set; }
    public bool IsPersisted { get; set; }

    public string ComputeExpression { get; set; }

    public string CustomSelect { get; set; }
    public string CustomInsert { get; set; }
    public string CustomUpdate { get; set; }

    public bool RequiresAlias => Alias != null || CustomSelect != null;

    public string BelongToModelName { get; set; }

    public bool IsReference { get; set; }
        
    /// <summary>
    /// Whether the PK for the Reference Table is a field on the same table
    /// </summary>
    public string ReferenceSelfId { get; set; }
        
    /// <summary>
    /// The PK to use for the Reference Table (e.g. what ReferenceSelfId references) 
    /// </summary>
    public string ReferenceRefId { get; set; }
        
    /// <summary>
    /// References a Field on another Table
    /// [ReferenceField(typeof(Target), nameof(TargetId))]
    /// public TargetFieldType TargetFieldName { get; set; }
    /// </summary>
    public FieldReference FieldReference { get; set; }

    public string CustomFieldDefinition { get; set; }

    public bool IsRefType { get; set; }

    public bool IgnoreOnUpdate { get; set; }

    public bool IgnoreOnInsert { get; set; }

    public bool ReturnOnInsert { get; set; }

    public override string ToString() => Name;

    public bool ShouldSkipInsert() => IgnoreOnInsert || AutoIncrement || (IsComputed && !IsPersisted) || IsRowVersion;

    public bool ShouldSkipUpdate() => IgnoreOnUpdate || (IsComputed && !IsPersisted);

    public bool ShouldSkipDelete() => (IsComputed && !IsPersisted);

    public bool IsSelfRefField(FieldDefinition fieldDef)
    {
        return (fieldDef.Alias != null && IsSelfRefField(fieldDef.Alias))
               || IsSelfRefField(fieldDef.Name);
    }

    public bool IsSelfRefField(string name)
    {
        return (Alias != null && Alias + "Id" == name)
               || Name + "Id" == name;
    }

    public FieldDefinition Clone(Action<FieldDefinition> modifier = null)
    {
        var fieldDef = new FieldDefinition
        {
            Name = Name,
            Alias = Alias,
            FieldType = FieldType,
            FieldTypeDefaultValue = FieldTypeDefaultValue,
            TreatAsType = TreatAsType,
            PropertyInfo = PropertyInfo,
            IsPrimaryKey = IsPrimaryKey,
            AutoIncrement = AutoIncrement,
            AutoId = AutoId,
            IsNullable = IsNullable,
            IsIndexed = IsIndexed,
            IsUniqueIndex = IsUniqueIndex,
            IsClustered = IsClustered,
            IsNonClustered = IsNonClustered,
            IsRowVersion = IsRowVersion,
            FieldLength = FieldLength,
            Scale = Scale,
            DefaultValue = DefaultValue,
            DefaultValueConstraint = DefaultValueConstraint,
            CheckConstraint = CheckConstraint,
            IsUniqueConstraint = IsUniqueConstraint,
            ForeignKey = ForeignKey,
            GetValueFn = GetValueFn,
            SetValueFn = SetValueFn,
            Sequence = Sequence,
            IsComputed = IsComputed,
            IsPersisted = IsPersisted, 
            ComputeExpression = ComputeExpression,
            CustomSelect = CustomSelect,
            BelongToModelName = BelongToModelName,
            IsReference = IsReference,
            ReferenceRefId = ReferenceRefId, 
            ReferenceSelfId = ReferenceSelfId, 
            FieldReference = FieldReference,
            CustomFieldDefinition = CustomFieldDefinition,
            IsRefType = IsRefType,
        };

        modifier?.Invoke(fieldDef);
        return fieldDef;
    }
}

public class ForeignKeyConstraint
{
    public ForeignKeyConstraint(Type type, string onDelete = null, string onUpdate = null, string foreignKeyName = null)
    {
        ReferenceType = type;
        OnDelete = onDelete;
        OnUpdate = onUpdate;
        ForeignKeyName = foreignKeyName;
    }

    public Type ReferenceType { get; private set; }
    public string OnDelete { get; private set; }
    public string OnUpdate { get; private set; }
    public string ForeignKeyName { get; private set; }

    public string GetForeignKeyName(ModelDefinition modelDef, ModelDefinition refModelDef, INamingStrategy namingStrategy, FieldDefinition fieldDef)
    {
        if (ForeignKeyName.IsNullOrEmpty())
        {
            var modelName = modelDef.IsInSchema
                ? $"{modelDef.Schema}_{namingStrategy.GetTableName(modelDef)}"
                : namingStrategy.GetTableName(modelDef.ModelName);

            var refModelName = refModelDef.IsInSchema
                ? $"{refModelDef.Schema}_{namingStrategy.GetTableName(refModelDef)}"
                : namingStrategy.GetTableName(refModelDef);

            var fkName = $"FK_{modelName}_{refModelName}_{fieldDef.FieldName}";
            return namingStrategy.ApplyNameRestrictions(fkName);
        }
        return ForeignKeyName;
    }
}

public class FieldReference
{
    public FieldDefinition FieldDef { get; }

    public FieldReference(FieldDefinition fieldDef) => FieldDef = fieldDef;

    /// <summary>
    /// Foreign Key Table name
    /// </summary>
    public Type RefModel { get; set; }

    private ModelDefinition refModelDef;
    public ModelDefinition RefModelDef => refModelDef ??= RefModel.GetModelDefinition();
    
    /// <summary>
    /// The Field name on current Model to use for the Foreign Key Table Lookup 
    /// </summary>
    public string RefId { get; set; }

    private FieldDefinition refIdFieldDef;
    public FieldDefinition RefIdFieldDef => refIdFieldDef ??= FieldDef.ModelDef.GetFieldDefinition(RefId)
                                                              ?? throw new ArgumentException($"Could not find '{RefId}' in '{RefModel.Name}'");
    
    /// <summary>
    /// Specify Field to reference (if different from property name)
    /// </summary>
    public string RefField { get; set; }

    private FieldDefinition refFieldDef;
    public FieldDefinition RefFieldDef => refFieldDef ??= RefModelDef.GetFieldDefinition(RefField)
                                                          ?? throw new ArgumentException($"Could not find '{RefField}' in '{RefModelDef.Name}'");
}