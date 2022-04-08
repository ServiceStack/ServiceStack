using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Support
{
    internal abstract class LoadList<Into, From>
    {
        protected IDbCommand dbCmd;
        protected SqlExpression<From> q;

        protected IOrmLiteDialectProvider dialectProvider;
        protected List<Into> parentResults;
        protected ModelDefinition modelDef;
        protected FieldDefinition[] fieldDefs;
        protected string subSql;

        public FieldDefinition[] FieldDefs => fieldDefs;

        public List<Into> ParentResults => parentResults;

        protected LoadList(IDbCommand dbCmd, SqlExpression<From> q)
        {
            dialectProvider = dbCmd.GetDialectProvider();

            if (q == null)
                q = dialectProvider.SqlExpression<From>();

            this.dbCmd = dbCmd;
            this.q = q;

            //Use .Clone() to prevent SqlExpressionSelectFilter from adding params to original query
            var parentQ = q.Clone();
            var sql = parentQ.SelectInto<Into>(QueryType.Select);
            parentResults = dbCmd.ExprConvertToList<Into>(sql, parentQ.Params, onlyFields:q.OnlyFields);

            modelDef = ModelDefinition<Into>.Definition;
            fieldDefs = modelDef.ReferenceFieldDefinitionsArray;

            var subQ = q.Clone();
            var subQSql = dialectProvider.GetLoadChildrenSubSelect(subQ);
            subSql = dialectProvider.MergeParamsIntoSql(subQSql, subQ.Params);
        }

        protected string GetRefListSql(ModelDefinition refModelDef, FieldDefinition refField)
        {
            var sqlRef = $"SELECT {dialectProvider.GetColumnNames(refModelDef)} " +
                         $"FROM {dialectProvider.GetQuotedTableName(refModelDef)} " +
                         $"WHERE {dialectProvider.GetQuotedColumnName(refField)} " +
                         $"IN ({subSql})";

            if (OrmLiteConfig.LoadReferenceSelectFilter != null)
                sqlRef = OrmLiteConfig.LoadReferenceSelectFilter(refModelDef.ModelType, sqlRef);

            return sqlRef;
        }

        protected void SetListChildResults(FieldDefinition fieldDef, Type refType, IList childResults, FieldDefinition refField)
        {
            var map = new Dictionary<object, List<object>>();
            List<object> refValues;

            foreach (var result in childResults)
            {
                var refValue = refField.GetValue(result);
                if (!map.TryGetValue(refValue, out refValues))
                {
                    map[refValue] = refValues = new List<object>();
                }
                refValues.Add(result);
            }

            var untypedApi = dbCmd.CreateTypedApi(refType);
            foreach (var result in parentResults)
            {
                var pkValue = modelDef.PrimaryKey.GetValue(result);
                if (map.TryGetValue(pkValue, out refValues))
                {
                    var castResults = untypedApi.Cast(refValues);
                    fieldDef.SetValue(result, castResults);
                }
            }
        }

        protected string GetRefSelfSql(ModelDefinition modelDef, FieldDefinition refSelf, ModelDefinition refModelDef)
        {
            //Load Self Table.RefTableId PK
            var refQ = q.Clone();
            refQ.Select(dialectProvider.GetQuotedColumnName(modelDef, refSelf));
            refQ.OrderBy().ClearLimits(); //clear any ORDER BY or LIMIT's in Sub Select's

            var subSqlRef = refQ.ToMergedParamsSelectStatement();

            var sqlRef = $"SELECT {dialectProvider.GetColumnNames(refModelDef)} " +
                         $"FROM {dialectProvider.GetQuotedTableName(refModelDef)} " +
                         $"WHERE {dialectProvider.GetQuotedColumnName(refModelDef.PrimaryKey)} " +
                         $"IN ({subSqlRef})";

            if (OrmLiteConfig.LoadReferenceSelectFilter != null)
                sqlRef = OrmLiteConfig.LoadReferenceSelectFilter(refModelDef.ModelType, sqlRef);

            return sqlRef;
        }

        protected string GetRefFieldSql(ModelDefinition refModelDef, FieldDefinition refField)
        {
            var sqlRef = $"SELECT {dialectProvider.GetColumnNames(refModelDef)} " +
                         $"FROM {dialectProvider.GetQuotedTableName(refModelDef)} " +
                         $"WHERE {dialectProvider.GetQuotedColumnName(refField)} " +
                         $"IN ({subSql})";

            if (OrmLiteConfig.LoadReferenceSelectFilter != null)
                sqlRef = OrmLiteConfig.LoadReferenceSelectFilter(refModelDef.ModelType, sqlRef);

            return sqlRef;
        }

        protected string GetFieldReferenceSql(FieldDefinition fieldDef, FieldReference fieldRef)
        {
            var refModelDef = fieldRef.RefModelDef;
            
            var useSubSql = $"SELECT {dialectProvider.GetQuotedColumnName(fieldRef.RefIdFieldDef)} FROM "
                + subSql.RightPart("FROM");

            var pk = dialectProvider.GetQuotedColumnName(refModelDef.PrimaryKey);
            var sqlRef = $"SELECT {pk}, {dialectProvider.GetQuotedColumnName(fieldRef.RefFieldDef)} " +
                         $"FROM {dialectProvider.GetQuotedTableName(refModelDef)} " +
                         $"WHERE {pk} " +
                         $"IN ({useSubSql})";

            if (OrmLiteConfig.LoadReferenceSelectFilter != null)
                sqlRef = OrmLiteConfig.LoadReferenceSelectFilter(refModelDef.ModelType, sqlRef);

            return sqlRef;
        }

        protected Dictionary<object, object> CreateRefMap()
        {
            return OrmLiteConfig.IsCaseInsensitive
                ? new Dictionary<object, object>(CaseInsensitiveObjectComparer.Instance)
                : new Dictionary<object, object>(); 
        }

        public class CaseInsensitiveObjectComparer : IEqualityComparer<object>
        {
            public static CaseInsensitiveObjectComparer Instance = new CaseInsensitiveObjectComparer();

            public new bool Equals(object x, object y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;

                var xStr = x as string;
                var yStr = y as string;

                return xStr != null && yStr != null
                    ? xStr.Equals(yStr, StringComparison.OrdinalIgnoreCase)
                    : x.Equals(y);
            }

            public int GetHashCode(object obj)
            {
                var str = obj as string;
                return str?.ToUpper().GetHashCode() ?? obj.GetHashCode();
            }
        }

        protected void SetRefSelfChildResults(FieldDefinition fieldDef, ModelDefinition refModelDef, FieldDefinition refSelf, IList childResults)
        {
            var map = CreateRefMap();

            foreach (var result in childResults)
            {
                var pkValue = refModelDef.PrimaryKey.GetValue(result);
                map[pkValue] = result;
            }

            foreach (var result in parentResults)
            {
                var fkValue = refSelf.GetValue(result);
                if (fkValue != null && map.TryGetValue(fkValue, out var childResult))
                {
                    fieldDef.SetValue(result, childResult);
                }
            }
        }

        protected void SetRefFieldChildResults(FieldDefinition fieldDef, FieldDefinition refField, IList childResults)
        {
            var map = CreateRefMap();

            foreach (var result in childResults)
            {
                var refValue = refField.GetValue(result);
                map[refValue] = result;
            }

            foreach (var result in parentResults)
            {
                var pkValue = modelDef.PrimaryKey.GetValue(result);
                if (map.TryGetValue(pkValue, out var childResult))
                {
                    fieldDef.SetValue(result, childResult);
                }
            }
        }

        protected void SetFieldReferenceChildResults(FieldDefinition fieldDef, FieldReference fieldRef, IList childResults)
        {
            var map = CreateRefMap();

            var refField = fieldRef.RefModelDef.PrimaryKey;
            foreach (var result in childResults)
            {
                var refValue = refField.GetValue(result);
                var refFieldValue = fieldRef.RefFieldDef.GetValue(result);
                map[refValue] = refFieldValue;
            }

            foreach (var result in parentResults)
            {
                var fkValue = fieldRef.RefIdFieldDef.GetValue(result);
                if (map.TryGetValue(fkValue, out var childResult))
                {
                    fieldDef.SetValue(result, childResult);
                }
            }
        }
    }

    internal class LoadListSync<Into, From> : LoadList<Into, From>
    {
        public LoadListSync(IDbCommand dbCmd, SqlExpression<From> q) : base(dbCmd, q) {}

        public void SetRefFieldList(FieldDefinition fieldDef, Type refType)
        {
            var refModelDef = refType.GetModelDefinition();
            var refField = modelDef.GetRefFieldDef(refModelDef, refType);

            var sqlRef = GetRefListSql(refModelDef, refField);

            var childResults = dbCmd.ConvertToList(refType, sqlRef);

            SetListChildResults(fieldDef, refType, childResults, refField);
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
                var sqlRef = GetRefSelfSql(modelDef, refSelf, refModelDef);
                var childResults = dbCmd.ConvertToList(refType, sqlRef);
                SetRefSelfChildResults(fieldDef, refModelDef, refSelf, childResults);
            }
            else if (refField != null)
            {
                var sqlRef = GetRefFieldSql(refModelDef, refField);
                var childResults = dbCmd.ConvertToList(refType, sqlRef);
                SetRefFieldChildResults(fieldDef, refField, childResults);
            }
        }

        public void SetFieldReference(FieldDefinition fieldDef, FieldReference fieldRef)
        {
            var sqlRef = GetFieldReferenceSql(fieldDef, fieldRef);
            var childResults = dbCmd.ConvertToList(fieldRef.RefModel, sqlRef);
            SetFieldReferenceChildResults(fieldDef, fieldRef, childResults);
        }
    }

#if ASYNC
    internal class LoadListAsync<Into, From> : LoadList<Into, From>
    {
        public LoadListAsync(IDbCommand dbCmd, SqlExpression<From> expr) : base(dbCmd, expr) { }

        public async Task SetRefFieldListAsync(FieldDefinition fieldDef, Type refType, CancellationToken token)
        {
            var refModelDef = refType.GetModelDefinition();
            var refField = modelDef.GetRefFieldDef(refModelDef, refType);

            var sqlRef = GetRefListSql(refModelDef, refField);

            var childResults = await dbCmd.ConvertToListAsync(refType, sqlRef, token).ConfigAwait();

            SetListChildResults(fieldDef, refType, childResults, refField);
        }

        public async Task SetRefFieldAsync(FieldDefinition fieldDef, Type refType, CancellationToken token)
        {
            var refModelDef = refType.GetModelDefinition();
            var refSelf = modelDef.GetSelfRefFieldDefIfExists(refModelDef, fieldDef);
            var refField = refSelf == null
                ? modelDef.GetRefFieldDef(refModelDef, refType)
                : modelDef.GetRefFieldDefIfExists(refModelDef);

            if (refSelf != null)
            {
                var sqlRef = GetRefSelfSql(modelDef, refSelf, refModelDef);
                var childResults = await dbCmd.ConvertToListAsync(refType, sqlRef, token).ConfigAwait();
                SetRefSelfChildResults(fieldDef, refModelDef, refSelf, childResults);
            }
            else if (refField != null)
            {
                var sqlRef = GetRefFieldSql(refModelDef, refField);
                var childResults = await dbCmd.ConvertToListAsync(refType, sqlRef, token).ConfigAwait();
                SetRefFieldChildResults(fieldDef, refField, childResults);
            }
        }

        public async Task SetFieldReferenceAsync(FieldDefinition fieldDef, FieldReference fieldRef, CancellationToken token)
        {
            var sqlRef = GetFieldReferenceSql(fieldDef, fieldRef);
            var childResults = await dbCmd.ConvertToListAsync(fieldRef.RefModel, sqlRef, token).ConfigAwait();
            SetFieldReferenceChildResults(fieldDef, fieldRef, childResults);
        }
    }
#endif
}