using System;
using System.Collections.Generic;

namespace ServiceStack.OrmLite;

public interface INamingStrategy
{
    Dictionary<string, string> SchemaAliases { get; }
    Dictionary<string, string> TableAliases { get; }
    Dictionary<string, string> ColumnAliases { get; }
    string GetAlias(string name);
    string GetSchemaName(string name);
    string GetSchemaName(ModelDefinition modelDef);
    string GetTableName(string name);
    string GetTableName(ModelDefinition modelDef);
    string GetColumnName(string name);
    string GetSequenceName(string modelName, string fieldName);
    string ApplyNameRestrictions(string name);
}