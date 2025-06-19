//
// ServiceStack.OrmLite: Light-weight POCO ORM for .NET and Mono
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//   Tomasz Kubacki (tomasz.kubacki@gmail.com)
//
// Copyright (c) ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System.Collections.Generic;

namespace ServiceStack.OrmLite;

public class OrmLiteNamingStrategyBase : INamingStrategy
{
    public Dictionary<string, string> SchemaAliases = new();
    public Dictionary<string, string> TableAliases = new();
    public Dictionary<string, string> ColumnAliases = new();
    public virtual string GetAlias(string name) => name;
    public virtual string GetSchemaName(string name) => name == null ? null 
        : SchemaAliases.TryGetValue(name, out var alias) 
            ? alias 
            : name;
    public virtual string GetSchemaName(ModelDefinition modelDef) => GetSchemaName(modelDef.Schema);

    public virtual string GetTableName(string name) => TableAliases.TryGetValue(name, out var alias) 
        ? alias 
        : name;

    public virtual string GetTableName(ModelDefinition modelDef) => modelDef.Alias != null
        ? GetAlias(modelDef.Alias)
        : GetTableName(modelDef.Name);

    public virtual string GetColumnName(string name) => ColumnAliases.TryGetValue(name, out var alias) 
        ? alias 
        : name;

    public virtual string GetSequenceName(string modelName, string fieldName) => "SEQ_" + modelName + "_" + fieldName;

    public virtual string ApplyNameRestrictions(string name) => name;
}