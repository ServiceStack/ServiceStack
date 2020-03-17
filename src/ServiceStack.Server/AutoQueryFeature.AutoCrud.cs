using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.MiniProfiler;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public partial class AutoQueryFeature
    {
        
    }

    public partial class AutoQuery : IAutoCrudDb
    {
        public async Task<object> Create<Table>(ICreateDb<Table> dto, IRequest req)
        {
            //TODO: Allow Create to use Default Values
            using var db = GetDb<Table>(req);
            using (Profiler.Current.Step("AutoQuery.Create"))
            {
                var response = await ExecAndReturnResponseAsync<Table>(dto, db,async ctx => {
                    var dtoValues = ResolveDtoValues(req, dto);
                    var pkFieldDef = typeof(Table).GetModelMetadata()?.PrimaryKey;
                    var isAutoId = pkFieldDef?.AutoId == true;
                    var selectIdentity = ctx.IdProp != null || ctx.ResultProp != null;
                    var autoIntId = await db.InsertAsync<Table>(dtoValues, selectIdentity: selectIdentity);
                    // [AutoId] Guid's populate the PK Property
                    if (isAutoId)
                        return new ExecValue(pkFieldDef.GetValue(dtoValues), selectIdentity ? 1 : autoIntId);

                    return selectIdentity
                        ? new ExecValue(autoIntId, 1)
                        : pkFieldDef != null && dtoValues.TryGetValue(pkFieldDef.Name, out var idValue)
                            ? new ExecValue(idValue, autoIntId)
                            : new ExecValue(null, autoIntId);
                });
                
                return response;
            }
        }

        public Task<object> Update<Table>(IUpdateDb<Table> dto, IRequest req)
        {
            var skipDefaults = dto.GetType().FirstAttribute<AutoUpdateAttribute>()?.Style == AutoUpdateStyle.NonDefaults;
            return UpdateInternalAsync<Table>(req, dto, skipDefaults);
        }

        public Task<object> Patch<Table>(IPatchDb<Table> dto, IRequest req)
        {
            return UpdateInternalAsync<Table>(req, dto, skipDefaults:true);
        }

        public async Task<object> Delete<Table>(IDeleteDb<Table> dto, IRequest req)
        {
            using var db = GetDb<Table>(req);
            using (Profiler.Current.Step("AutoQuery.Delete"))
            {
                var response = await ExecAndReturnResponseAsync<Table>(dto, db,
                    async ctx => {
                        var dtoValues = ResolveDtoValues(req, dto, skipDefaults:true);
                        
                        //Should have at least 1 non-default filter
                        if (dtoValues.Count == 0)
                            throw new NotSupportedException($"'{dto.GetType().Name}' did not contain any filters");
                        
                        var meta = AutoCrudMetadata.Create(dto.GetType());
                        var pkFieldDef = meta.ModelDef.PrimaryKey;
                        var idValue = pkFieldDef != null && dtoValues.TryGetValue(pkFieldDef.Name, out var oId)
                            ? oId
                            : null;

                        // Should only update a Single Row
                        if (GetAutoFilterExpressions(db, dto, dtoValues, req, out var expr, out var exprParams))
                        {
                            //If there were Auto Filters, construct filter expression manually by adding any remaining DTO values
                            foreach (var entry in dtoValues)
                            {
                                var fieldDef = meta.ModelDef.GetFieldDefinition(entry.Key);
                                if (fieldDef == null)
                                    throw new NotSupportedException($"Unknown '{entry.Key}' Field in '{dto.GetType().Name}' IDeleteDb<{typeof(Table).Name}> Request");
                                
                                if (expr.Length > 0)
                                    expr += " AND ";

                                var quotedColumn = db.GetDialectProvider().GetQuotedColumnName(meta.ModelDef, fieldDef);

                                expr += quotedColumn + " = {" + exprParams.Count + "}";
                                exprParams.Add(entry.Value);
                            }

                            var q = db.From<Table>();
                            q.Where(expr, exprParams.ToArray());
                            return new ExecValue(idValue, await db.DeleteAsync(q));
                        }
                        else
                        {
                            return new ExecValue(idValue, await db.DeleteAsync<Table>(dtoValues));
                        }
                    });
                
                return response;
            }
        }

        public async Task<object> Save<Table>(ISaveDb<Table> dto, IRequest req)
        {
            using var db = GetDb<Table>(req);
            using (Profiler.Current.Step("AutoQuery.Save"))
            {
                var row = dto.ConvertTo<Table>();
                var response = await ExecAndReturnResponseAsync<Table>(dto, db,
                    //TODO: Use Upsert when available
                    fn: async ctx => {
                        await db.SaveAsync(row);
                        object idValue = null;
                        var pkField = typeof(Table).GetModelMetadata().PrimaryKey;
                        if (pkField != null)
                        {
                            var propGetter = TypeProperties.Get(dto.GetType()).GetPublicGetter(pkField.Name);
                            if (propGetter != null)
                                idValue = propGetter(dto);
                        }
                        return new ExecValue(idValue, 1);
                    }); 
                
                return response;
            }
        }
        
        internal struct ExecValue
        {
            internal object Id;
            internal long? RowsUpdated;
            public ExecValue(object id, long? rowsUpdated)
            {
                Id = id;
                RowsUpdated = rowsUpdated;
            }
        }
        
        private async Task<object> ExecAndReturnResponseAsync<Table>(object dto, IDbConnection db, Func<ExecContext,Task<ExecValue>> fn)
        {
            var responseType = HostContext.Metadata.GetOperation(dto.GetType())?.ResponseType;
            var responseProps = responseType == null ? null : TypeProperties.Get(responseType);
            var idProp = responseProps?.GetAccessor(Keywords.Id);
            var countProp = responseProps?.GetAccessor(Keywords.Count);
            var resultProp = responseProps?.GetAccessor(Keywords.Result);
            var rowVersionProp = responseProps?.GetAccessor(Keywords.RowVersion);

            var execValue = await fn(new ExecContext(idProp, resultProp, countProp, rowVersionProp));
            if (responseType == null)
                return null;

            object idValue = null;
                
            var response = responseType.CreateInstance();
            if (idProp != null && execValue.Id != null)
            {
                idValue = execValue.Id.ConvertTo(idProp.PropertyInfo.PropertyType);
                idProp.PublicSetter(response, idValue);
            }
            if (countProp != null && execValue.RowsUpdated != null)
            {
                countProp.PublicSetter(response, execValue.RowsUpdated.ConvertTo(countProp.PropertyInfo.PropertyType));
            }

            if (resultProp != null && execValue.Id != null)
            {
                var result = await db.SingleByIdAsync<Table>(execValue.Id);
                resultProp.PublicSetter(response, result.ConvertTo(resultProp.PropertyInfo.PropertyType));
            }

            if (rowVersionProp != null)
            {
                if (AutoMappingUtils.IsDefaultValue(idValue))
                {
                    var modelDef = typeof(Table).GetModelMetadata();
                    var dtoIdGetter = TypeProperties.Get(dto.GetType()).GetPublicGetter(modelDef.PrimaryKey.Name);
                    if (dtoIdGetter != null)
                        idValue = dtoIdGetter(dto);
                }
                if (AutoMappingUtils.IsDefaultValue(idValue))
                    throw new NotSupportedException($"Could not resolve Primary Key from '{dto.GetType().Name}' to be able to resolve RowVersion");
                
                var rowVersion = await db.GetRowVersionAsync<Table>(idValue);
                rowVersionProp.PublicSetter(response, rowVersion.ConvertTo(rowVersionProp.PropertyInfo.PropertyType));
            }
            
            return response;
        }

        internal struct ExecContext
        {
            internal PropertyAccessor IdProp;
            internal PropertyAccessor ResultProp;
            internal PropertyAccessor CountProp;
            internal PropertyAccessor RowVersionProp;
            public ExecContext(PropertyAccessor idProp, PropertyAccessor resultProp, PropertyAccessor countProp, PropertyAccessor rowVersionProp)
            {
                IdProp = idProp;
                ResultProp = resultProp;
                CountProp = countProp;
                RowVersionProp = rowVersionProp;
            }
        }

        internal bool GetAutoFilterExpressions(IDbConnection db, object dto, Dictionary<string, object> dtoValues, IRequest req, out string expr, out List<object> exprParams)
        {
            var meta = AutoCrudMetadata.Create(dto.GetType());
            if (meta.AutoFilters != null)
            {
                var dialectProvider = db.GetDialectProvider();
                var sb = StringBuilderCache.Allocate();
                var exprParamsList = new List<object>();

                //Update's require PK's, Delete's don't need to
                if (dtoValues.TryRemove(meta.ModelDef.PrimaryKey.Name, out var idValue))
                {
                    var idColumn = dialectProvider.GetQuotedColumnName(meta.ModelDef, meta.ModelDef.PrimaryKey);
                    sb.Append(idColumn + " = {0}");
                    exprParamsList.Add(idValue);
                }
                
                var appHost = HostContext.AppHost;
                for (var i = 0; i < meta.AutoFilters.Count; i++)
                {
                    var filter = meta.AutoFilters[i];
                    var dbAttr = meta.AutoFiltersDbFields[i];
                    
                    var fieldDef = meta.ModelDef.GetFieldDefinition(filter.Field);
                    if (fieldDef == null)
                        throw new NotSupportedException($"{dto.GetType().Name} '{filter.Field}' AutoFilter was not found on '{meta.ModelType.Name}'");

                    var quotedColumn = dialectProvider.GetQuotedColumnName(meta.ModelDef, fieldDef);

                    var value = appHost.EvalScriptValue(filter, req);
                    
                    var ret = ExprResult.CreateExpression("AND", quotedColumn, value, dbAttr);


                    if (ret != null)
                    {
                        if (sb.Length > 0)
                            sb.Append(" AND ");

                        var exprResult = ret.Value;
                        if (exprResult.Format.IndexOf("{1}", StringComparison.Ordinal) >= 0)
                            throw new NotSupportedException($"SQL Template '{exprResult.Format}' with multiple arguments is not supported");

                        if (exprResult.Values != null)
                        {
                            for (var index = 0; index < exprResult.Values.Length; index++)
                            {
                                sb.Append(exprResult.Format.Replace("{" + index + "}", "{" + exprParamsList.Count + "}"));
                                exprParamsList.Add(exprResult.Values[index]);
                            }
                        }
                    }

                    expr = StringBuilderCache.ReturnAndFree(sb);
                    exprParams = exprParamsList;
                    return true;
                }
            }

            expr = null;
            exprParams = null;
            return false;
        }

        private async Task<object> UpdateInternalAsync<Table>(IRequest req, object dto, bool skipDefaults)
        {
            using var db = GetDb<Table>(req);
            using (Profiler.Current.Step("AutoQuery.Update"))
            {
                var response = await ExecAndReturnResponseAsync<Table>(dto, db,
                    async ctx => {
                        var dtoValues = ResolveDtoValues(req, dto, skipDefaults);
                        var pkFieldDef = typeof(Table).GetModelMetadata()?.PrimaryKey;
                        if (pkFieldDef == null)
                            throw new NotSupportedException($"Table '{typeof(Table).Name}' does not have a primary key");
                        if (!dtoValues.TryGetValue(pkFieldDef.Name, out var idValue) || AutoMappingUtils.IsDefaultValue(idValue))
                            throw new ArgumentNullException(pkFieldDef.Name);
                        
                        // Should only update a Single Row
                        var rowsUpdated = GetAutoFilterExpressions(db, dto, dtoValues, req, out var expr, out var exprParams) 
                            ? await db.UpdateOnlyAsync<Table>(dtoValues, expr, exprParams.ToArray())
                            : await db.UpdateOnlyAsync<Table>(dtoValues);

                        if (rowsUpdated != 1)
                            throw new OptimisticConcurrencyException($"{rowsUpdated} rows were updated by '{dto.GetType().Name}'");

                        return new ExecValue(idValue, rowsUpdated);
                    }); //TODO: UpdateOnly

                return response;
            }
        }
        
        internal class AutoCrudMetadata
        {
            internal Type DtoType;
            internal Type ModelType;
            internal ModelDefinition ModelDef;
            internal TypeProperties DtoProps;
            internal List<AutoPopulateAttribute> PopulateAttrs;
            internal List<AutoFilterAttribute> AutoFilters;
            internal List<QueryDbFieldAttribute> AutoFiltersDbFields;
            internal Dictionary<string, AutoUpdateAttribute> UpdateAttrs;
            internal Dictionary<string, AutoDefaultAttribute> DefaultAttrs;
            internal Dictionary<string, AutoMapAttribute> MapAttrs;
            internal HashSet<string> NullableProps;
            internal GetMemberDelegate RowVersionGetter;
            
            static readonly ConcurrentDictionary<Type, AutoCrudMetadata> cache = 
                new ConcurrentDictionary<Type, AutoCrudMetadata>();

            internal static Type GetModelType(Type requestType)
            {
                var intoTypeDef = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryDb<,>));
                if (intoTypeDef != null)
                {
                    var args = intoTypeDef.GetGenericArguments();
                    return args[1];
                }
            
                var typeDef = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryDb<>));
                if (typeDef != null)
                {
                    var args = typeDef.GetGenericArguments();
                    return args[0];
                }
                
                var crudTypes = AutoQueryFeature.GetCrudGenericDefTypes(requestType, typeof(ICreateDb<>))
                    ?? AutoQueryFeature.GetCrudGenericDefTypes(requestType, typeof(IUpdateDb<>))
                    ?? AutoQueryFeature.GetCrudGenericDefTypes(requestType, typeof(IDeleteDb<>))
                    ?? AutoQueryFeature.GetCrudGenericDefTypes(requestType, typeof(IPatchDb<>))
                    ?? AutoQueryFeature.GetCrudGenericDefTypes(requestType, typeof(ISaveDb<>));

                if (crudTypes != null)
                {
                    var genericDef = crudTypes.Item1;
                    return genericDef.GenericTypeArguments[0];
                }

                return null;
            }

            internal static AutoCrudMetadata Create(Type dtoType)
            {
                if (cache.TryGetValue(dtoType, out var to))
                    return to;
                
                to = new AutoCrudMetadata {
                    DtoType = dtoType,
                    ModelType = GetModelType(dtoType),
                    DtoProps = TypeProperties.Get(dtoType),
                };
                if (to.ModelType != null)
                    to.ModelDef = to.ModelType.GetModelMetadata();
                
                to.RowVersionGetter = to.DtoProps.GetPublicGetter(Keywords.RowVersion);
                
                var dtoAttrs = dtoType.AllAttributes();
                foreach (var dtoAttr in dtoAttrs)
                {
                    if (dtoAttr is AutoPopulateAttribute populateAttr)
                    {
                        to.PopulateAttrs ??= new List<AutoPopulateAttribute>();
                        to.PopulateAttrs.Add(populateAttr);
                    }
                    else if (dtoAttr is AutoFilterAttribute filterAttr)
                    {
                        to.AutoFilters ??= new List<AutoFilterAttribute>();
                        to.AutoFiltersDbFields ??= new List<QueryDbFieldAttribute>();

                        to.AutoFilters.Add(filterAttr);
                        to.AutoFiltersDbFields.Add(ExprResult.ToDbFieldAttribute(filterAttr));
                    }
                }

                foreach (var pi in to.DtoProps.PublicPropertyInfos)
                {
                    var allAttrs = pi.AllAttributes();
                    var propName = pi.Name;
                
                    if (allAttrs.FirstOrDefault(x => x is AutoMapAttribute) is AutoMapAttribute mapAttr)
                    {
                        to.MapAttrs ??= new Dictionary<string, AutoMapAttribute>();
                        to.MapAttrs[propName] = mapAttr;
                        propName = mapAttr.To;
                    }

                    if (allAttrs.FirstOrDefault(x => x is AutoUpdateAttribute) is AutoUpdateAttribute updateAttr)
                    {
                        to.UpdateAttrs ??= new Dictionary<string, AutoUpdateAttribute>();
                        to.UpdateAttrs[propName] = updateAttr;
                    }

                    if (allAttrs.FirstOrDefault(x => x is AutoDefaultAttribute) is AutoDefaultAttribute defaultAttr)
                    {
                        to.DefaultAttrs ??= new Dictionary<string, AutoDefaultAttribute>();
                        to.DefaultAttrs[propName] = defaultAttr;
                    }

                    if (pi.PropertyType.IsNullableType())
                    {
                        to.NullableProps ??= new HashSet<string>();
                        to.NullableProps.Add(propName);
                    }
                }

                return cache[dtoType] = to;
            }
        }

        private Dictionary<string, object> ResolveDtoValues(IRequest req, object dto, bool skipDefaults=false)
        {
            var dtoValues = dto.ToObjectDictionary();

            var meta = AutoCrudMetadata.Create(dto.GetType());

            if (meta.MapAttrs != null)
            {
                foreach (var entry in meta.MapAttrs)
                {
                    if (dtoValues.TryRemove(entry.Key, out var value))
                    {
                        dtoValues[entry.Value.To] = value;
                    }
                }
            }

            var appHost = HostContext.AppHost;
            if (skipDefaults || meta.UpdateAttrs != null || meta.DefaultAttrs != null)
            {
                List<string> removeKeys = null;
                Dictionary<string, object> replaceValues = null;

                foreach (var entry in dtoValues)
                {
                    var isNullable = meta.NullableProps?.Contains(entry.Key) == true;
                    var isDefaultValue = entry.Value == null || (!isNullable && AutoMappingUtils.IsDefaultValue(entry.Value));
                    if (isDefaultValue)
                    {
                        var handled = false;
                        if (meta.DefaultAttrs != null && meta.DefaultAttrs.TryGetValue(entry.Key, out var defaultAttr))
                        {
                            handled = true;
                            replaceValues ??= new Dictionary<string, object>();
                            replaceValues[entry.Key] = appHost.EvalScriptValue(defaultAttr, req);
                        }
                        if (!handled)
                        {
                            if (skipDefaults ||
                                (meta.UpdateAttrs != null && meta.UpdateAttrs.TryGetValue(entry.Key, out var attr) &&
                                 attr.Style == AutoUpdateStyle.NonDefaults))
                            {
                                removeKeys ??= new List<string>();
                                removeKeys.Add(entry.Key);
                            }
                        }
                    }
                }
                
                if (removeKeys != null)
                {
                    foreach (var key in removeKeys)
                    {
                        dtoValues.RemoveKey(key);
                    }
                }

                if (replaceValues != null)
                {
                    foreach (var entry in replaceValues)
                    {
                        dtoValues[entry.Key] = entry.Value;
                    }
                }
            }

            if (meta.PopulateAttrs != null)
            {
                foreach (var populateAttr in meta.PopulateAttrs)
                {
                    dtoValues[populateAttr.Field] = appHost.EvalScriptValue(populateAttr, req);
                }
            }

            var populatorFn = AutoMappingUtils.GetPopulator(
                typeof(Dictionary<string, object>), meta.DtoType);
            populatorFn?.Invoke(dtoValues, dto);

            // Ensure RowVersion is always populated if defined on Request DTO
            if (meta.RowVersionGetter != null && !dtoValues.ContainsKey(Keywords.RowVersion))
                dtoValues[Keywords.RowVersion] = default(uint);

            return dtoValues;
        }
        
    }

    public abstract partial class AutoQueryServiceBase
    {
        /// <summary>
        /// Inserts new entry into Table
        /// </summary>
        public virtual async Task<object> Create<Table>(ICreateDb<Table> dto) => AutoQuery.Create(dto, Request);

        /// <summary>
        /// Updates entry into Table
        /// </summary>
        public virtual Task<object> Update<Table>(IUpdateDb<Table> dto) => AutoQuery.Update(dto, Request);

        /// <summary>
        /// Partially Updates entry into Table (Uses OrmLite UpdateNonDefaults behavior)
        /// </summary>
        public virtual Task<object> Patch<Table>(IPatchDb<Table> dto) => AutoQuery.Patch(dto, Request);

        /// <summary>
        /// Deletes entry from Table
        /// </summary>
        public virtual async Task<object> Delete<Table>(IDeleteDb<Table> dto) => AutoQuery.Delete(dto, Request);

        /// <summary>
        /// Inserts or Updates entry into Table
        /// </summary>
        public virtual async Task<object> Save<Table>(ISaveDb<Table> dto) => AutoQuery.Save(dto, Request);
    }
}