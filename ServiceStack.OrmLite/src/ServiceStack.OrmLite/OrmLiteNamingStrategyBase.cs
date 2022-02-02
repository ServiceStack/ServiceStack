//
// ServiceStack.OrmLite: Light-weight POCO ORM for .NET and Mono
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//   Tomasz Kubacki (tomasz.kubacki@gmail.com)
//
// Copyright 2012 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack.
//

namespace ServiceStack.OrmLite
{
    public class OrmLiteNamingStrategyBase : INamingStrategy
    {
        public virtual string GetSchemaName(string name) => name;

        public virtual string GetSchemaName(ModelDefinition modelDef) => GetSchemaName(modelDef.Schema);

        public virtual string GetTableName(string name) => name;

        public virtual string GetTableName(ModelDefinition modelDef) => GetTableName(modelDef.ModelName);

        public virtual string GetColumnName(string name) => name;

        public virtual string GetSequenceName(string modelName, string fieldName) => "SEQ_" + modelName + "_" + fieldName;

        public virtual string ApplyNameRestrictions(string name) => name;
    }
}
