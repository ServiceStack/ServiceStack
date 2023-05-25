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
using ServiceStack.DataAnnotations;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace ServiceStack.OrmLite;

public class ModelDefinition
{
    public ModelDefinition()
    {
        this.FieldDefinitions = new List<FieldDefinition>();
        this.IgnoredFieldDefinitions = new List<FieldDefinition>();
        this.CompositeIndexes = new List<CompositeIndexAttribute>();
        this.UniqueConstraints = new List<UniqueConstraintAttribute>();
    }

    public const string RowVersionName = "RowVersion";

    public string Name { get; set; }

    public string Alias { get; set; }

    public string Schema { get; set; }

    public string PreCreateTableSql { get; set; }

    public string PostCreateTableSql { get; set; }

    public string PreDropTableSql { get; set; }

    public string PostDropTableSql { get; set; }

    public bool IsInSchema => this.Schema != null;

    public bool HasAutoIncrementId => PrimaryKey is { AutoIncrement: true };

    public bool HasSequenceAttribute => this.FieldDefinitions.Any(x => !x.Sequence.IsNullOrEmpty());

    public FieldDefinition RowVersion { get; set; }

    public string ModelName => this.Alias ?? this.Name;

    public Type ModelType { get; set; }

    public FieldDefinition PrimaryKey
    {
        get { return this.FieldDefinitions.First(x => x.IsPrimaryKey); }
    }

    public object GetPrimaryKey(object instance)
    {
        var pk = PrimaryKey;
        return pk != null
            ? pk.GetValue(instance)
            : instance.GetId();
    }

    public List<FieldDefinition> FieldDefinitions { get; set; }

    public FieldDefinition[] FieldDefinitionsArray { get; private set; }

    public FieldDefinition[] FieldDefinitionsWithAliases { get; private set; }

    public List<FieldDefinition> IgnoredFieldDefinitions { get; set; }

    public FieldDefinition[] IgnoredFieldDefinitionsArray { get; private set; }

    public FieldDefinition[] AllFieldDefinitionsArray { get; private set; }
    public FieldDefinition[] ReferenceFieldDefinitionsArray { get; private set; }
    public HashSet<string> ReferenceFieldNames { get; private set; }

    private readonly object fieldDefLock = new();
    private Dictionary<string, FieldDefinition> fieldDefinitionMap;
    private Func<string, string> fieldNameSanitizer;
        
    public FieldDefinition[] AutoIdFields { get; private set; }

    public List<FieldDefinition> GetAutoIdFieldDefinitions()
    {
        var to = new List<FieldDefinition>();
        foreach (var fieldDef in FieldDefinitionsArray)
        {
            if (fieldDef.AutoId)
            {
                to.Add(fieldDef);
            }
        }
        return to;
    }

    public FieldDefinition[] GetOrderedFieldDefinitions(ICollection<string> fieldNames, Func<string, string> sanitizeFieldName=null)
    {
        if (fieldNames == null)
            throw new ArgumentNullException(nameof(fieldNames));
            
        var fieldDefs = new FieldDefinition[fieldNames.Count];

        var i = 0;
        foreach (var fieldName in fieldNames)
        {                 
            var fieldDef = sanitizeFieldName != null 
                ? AssertFieldDefinition(fieldName, sanitizeFieldName)
                : AssertFieldDefinition(fieldName);
            fieldDefs[i++] = fieldDef;
        }

        return fieldDefs;
    }

    public Dictionary<string, FieldDefinition> GetFieldDefinitionMap(Func<string, string> sanitizeFieldName)
    {
        lock (fieldDefLock)
        {
            if (fieldDefinitionMap != null && fieldNameSanitizer == sanitizeFieldName) 
                return fieldDefinitionMap;
                
            fieldDefinitionMap = new Dictionary<string, FieldDefinition>(StringComparer.OrdinalIgnoreCase);
            fieldNameSanitizer = sanitizeFieldName;
            foreach (var fieldDef in FieldDefinitionsArray)
            {
                fieldDefinitionMap[sanitizeFieldName(fieldDef.FieldName)] = fieldDef;
            }
            return fieldDefinitionMap;
        }
    }

    public List<CompositeIndexAttribute> CompositeIndexes { get; set; }

    public List<UniqueConstraintAttribute> UniqueConstraints { get; set; }

    public FieldDefinition GetFieldDefinition<T>(Expression<Func<T, object>> field)
    {
        return GetFieldDefinition(ExpressionUtils.GetMemberName(field));
    }
        
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowNoFieldException(string fieldName) =>
        throw new NotSupportedException($"'{fieldName}' is not a property of '{Name}'");

    public FieldDefinition AssertFieldDefinition(string fieldName)
    {
        var fieldDef = GetFieldDefinition(fieldName);
        if (fieldDef == null)
            ThrowNoFieldException(fieldName);
            
        return fieldDef;
    }

    public FieldDefinition GetFieldDefinition(string fieldName)
    {
        if (fieldName != null)
        {
            foreach (var f in FieldDefinitionsWithAliases)
            {
                if (f.Alias == fieldName)
                    return f;
            }
            foreach (var f in FieldDefinitionsArray)
            {
                if (f.Name == fieldName)
                    return f;
            }
            foreach (var f in FieldDefinitionsWithAliases)
            {
                if (string.Equals(f.Alias, fieldName, StringComparison.OrdinalIgnoreCase))
                    return f;
            }
            foreach (var f in FieldDefinitionsArray)
            {
                if (string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase))
                    return f;
            }
        }
        return null;
    }

    public FieldDefinition AssertFieldDefinition(string fieldName, Func<string, string> sanitizeFieldName)
    {
        var fieldDef = GetFieldDefinition(fieldName, sanitizeFieldName);
        if (fieldDef == null)
            ThrowNoFieldException(fieldName);

        return fieldDef;
    }
        
    public FieldDefinition GetFieldDefinition(string fieldName, Func<string, string> sanitizeFieldName)
    {
        if (fieldName != null)
        {
            foreach (var f in FieldDefinitionsWithAliases)
            {
                if (f.Alias == fieldName || sanitizeFieldName(f.Alias) == fieldName)
                    return f;
            }
            foreach (var f in FieldDefinitionsArray)
            {
                if (f.Name == fieldName || sanitizeFieldName(f.Name) == fieldName)
                    return f;
            }
            foreach (var f in FieldDefinitionsWithAliases)
            {
                if (string.Equals(f.Alias, fieldName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(sanitizeFieldName(f.Alias), fieldName, StringComparison.OrdinalIgnoreCase))
                    return f;
            }
            foreach (var f in FieldDefinitionsArray)
            {
                if (string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(sanitizeFieldName(f.Name), fieldName, StringComparison.OrdinalIgnoreCase))
                    return f;
            }
        }
        return null;
    }

    public string GetQuotedName(string fieldName, IOrmLiteDialectProvider dialectProvider) => 
        GetFieldDefinition(fieldName)?.GetQuotedName(dialectProvider);

    public FieldDefinition GetFieldDefinition(Func<string, bool> predicate)
    {
        foreach (var f in FieldDefinitionsWithAliases)
        {
            if (predicate(f.Alias))
                return f;
        }
        foreach (var f in FieldDefinitionsArray)
        {
            if (predicate(f.Name))
                return f;
        }

        return null;
    }

    public void AfterInit()
    {
        FieldDefinitionsArray = FieldDefinitions.ToArray();
        FieldDefinitionsWithAliases = FieldDefinitions.Where(x => x.Alias != null).ToArray();

        IgnoredFieldDefinitionsArray = IgnoredFieldDefinitions.ToArray();

        var allItems = new List<FieldDefinition>(FieldDefinitions);
        allItems.AddRange(IgnoredFieldDefinitions);
        AllFieldDefinitionsArray = allItems.ToArray();
        ReferenceFieldDefinitionsArray = allItems.Where(x => x.IsReference).ToArray();
        ReferenceFieldNames = new HashSet<string>(ReferenceFieldDefinitionsArray.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);

        AutoIdFields = GetAutoIdFieldDefinitions().ToArray();

        OrmLiteConfig.OnModelDefinitionInit?.Invoke(this);
    }

    public bool IsRefField(FieldDefinition fieldDef)
    {
        return (fieldDef.Alias != null && IsRefField(fieldDef.Alias))
               || IsRefField(fieldDef.Name);
    }

    private bool IsRefField(string name)
    {
        return (Alias != null && Alias + "Id" == name)
               || Name + "Id" == name;
    }

    public override string ToString() => Name;

    public bool IsReference(string fieldName) => ReferenceFieldNames.Contains(fieldName);

    public bool HasAnyReferences(IEnumerable<string> fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            if (ReferenceFieldNames.Contains(fieldName))
                return true;
        }
        return false;
    }

    public static ModelDefinition For(Type modelType) => modelType.GetModelDefinition();
}


public static class ModelDefinition<T>
{
    private static ModelDefinition definition;
    public static ModelDefinition Definition => definition ??= typeof(T).GetModelDefinition();

    private static string primaryKeyName;
    public static string PrimaryKeyName => primaryKeyName ??= Definition.PrimaryKey.FieldName;
}