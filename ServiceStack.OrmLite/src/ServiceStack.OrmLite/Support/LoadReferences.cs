using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Support
{
    internal abstract class LoadReferences<T>
    {
        protected IDbCommand dbCmd;
        protected T instance;
        protected ModelDefinition modelDef;
        protected FieldDefinition[] fieldDefs;
        protected object pkValue;
        protected IOrmLiteDialectProvider dialectProvider;

        protected LoadReferences(IDbCommand dbCmd, T instance)
        {
            this.dbCmd = dbCmd;
            this.instance = instance;
            
            modelDef = ModelDefinition<T>.Definition;
            fieldDefs = modelDef.ReferenceFieldDefinitionsArray;
            pkValue = modelDef.PrimaryKey.GetValue(instance);
            dialectProvider = dbCmd.GetDialectProvider();
        }

        public FieldDefinition[] FieldDefs => fieldDefs;

        protected string GetRefListSql(Type refType)
        {
            var refModelDef = refType.GetModelDefinition();

            var refField = modelDef.GetRefFieldDef(refModelDef, refType);

            var sqlFilter = dialectProvider.GetQuotedColumnName(refField.FieldName) + "={0}";
            var sql = dialectProvider.ToSelectStatement(refType, sqlFilter, pkValue);

            if (OrmLiteConfig.LoadReferenceSelectFilter != null)
                sql = OrmLiteConfig.LoadReferenceSelectFilter(refType, sql);

            return sql;
        }

        protected string GetRefFieldSql(Type refType, FieldDefinition refField)
        {
            var sqlFilter = dialectProvider.GetQuotedColumnName(refField.FieldName) + "={0}";
            var sql = dialectProvider.ToSelectStatement(refType, sqlFilter, pkValue);

            if (OrmLiteConfig.LoadReferenceSelectFilter != null)
                sql = OrmLiteConfig.LoadReferenceSelectFilter(refType, sql);

            return sql;
        }

        protected string GetRefSelfSql(Type refType, FieldDefinition refSelf, ModelDefinition refModelDef)
        {
            //Load Self Table.RefTableId PK
            var refPkValue = refSelf.GetValue(instance);
            if (refPkValue == null)
                return null;

            var sqlFilter = dialectProvider.GetQuotedColumnName(refModelDef.PrimaryKey.FieldName) + "={0}";
            var sql = dialectProvider.ToSelectStatement(refType, sqlFilter, refPkValue);

            if (OrmLiteConfig.LoadReferenceSelectFilter != null)
                sql = OrmLiteConfig.LoadReferenceSelectFilter(refType, sql);

            return sql;
        }
    }

    internal class LoadReferencesSync<T> : LoadReferences<T>
    {
        public LoadReferencesSync(IDbCommand dbCmd, T instance) 
            : base(dbCmd, instance) {}

        public void SetRefFieldList(FieldDefinition fieldDef, Type refType)
        {
            var sql = GetRefListSql(refType);

            var results = dbCmd.ConvertToList(refType, sql);
            fieldDef.SetValue(instance, results);
        }

        public void SetRefField(FieldDefinition fieldDef, Type refType)
        {
            var refModelDef = refType.GetModelDefinition();

            var refSelf = modelDef.GetSelfRefFieldDefIfExists(refModelDef, fieldDef);
            var refField = refSelf == null
                ? modelDef.GetRefFieldDef(refModelDef, refType)
                : modelDef.GetRefFieldDefIfExists(refModelDef);

            if (refSelf != null)
            {
                var sql = GetRefSelfSql(refType, refSelf, refModelDef);
                if (sql == null)
                    return;

                var result = dbCmd.ConvertTo(refType, sql);
                fieldDef.SetValue(instance, result);
            }
            else if (refField != null)
            {
                var sql = GetRefFieldSql(refType, refField);
                var result = dbCmd.ConvertTo(refType, sql);
                fieldDef.SetValue(instance, result);
            }
        }
    }

#if ASYNC
    internal class LoadReferencesAsync<T> : LoadReferences<T>
    {
        public LoadReferencesAsync(IDbCommand dbCmd, T instance)
            : base(dbCmd, instance) { }

        public async Task SetRefFieldList(FieldDefinition fieldDef, Type refType, CancellationToken token)
        {
            var sql = GetRefListSql(refType);

            var results = await dbCmd.ConvertToListAsync(refType, sql, token).ConfigAwait();
            fieldDef.SetValue(instance, results);
        }

        public async Task SetRefField(FieldDefinition fieldDef, Type refType, CancellationToken token)
        {
            var refModelDef = refType.GetModelDefinition();

            var refSelf = modelDef.GetSelfRefFieldDefIfExists(refModelDef, fieldDef);
            var refField = refSelf == null
                ? modelDef.GetRefFieldDef(refModelDef, refType)
                : modelDef.GetRefFieldDefIfExists(refModelDef);

            if (refField != null)
            {
                var sql = GetRefFieldSql(refType, refField);
                var result = await dbCmd.ConvertToAsync(refType, sql, token).ConfigAwait();
                fieldDef.SetValue(instance, result);
            }
            else if (refSelf != null)
            {
                var sql = GetRefSelfSql(refType, refSelf, refModelDef);
                if (sql == null)
                    return;

                var result = await dbCmd.ConvertToAsync(refType, sql, token).ConfigAwait();
                fieldDef.SetValue(instance, result);
            }
        }
    }
#endif
}