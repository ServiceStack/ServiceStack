using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.MiniProfiler;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack
{
    public partial class AutoQueryFeature
    {
    }

    public abstract partial class AutoQueryServiceBase
    {
        private async Task<object> ExecAndReturnResponseAsync<Table>(object dto, IDbConnection db, Func<ExecContext,Task<object>> fn)
        {
            var responseType = HostContext.Metadata.GetOperation(dto.GetType())?.ResponseType;
            var responseProps = responseType == null ? null : TypeProperties.Get(responseType);
            var idProp = responseProps?.GetAccessor(Keywords.Id);
            var countProp = responseProps?.GetAccessor(Keywords.Count);
            var resultProp = responseProps?.GetAccessor(Keywords.Result);
            var rowVersionProp = responseProps?.GetAccessor(Keywords.RowVersion);

            var retValue = await fn(new ExecContext(idProp, resultProp, countProp, rowVersionProp));
            if (responseType == null)
                return null;

            object idValue = null;
                
            var response = responseType.CreateInstance();
            if (idProp != null)
            {
                idValue = retValue.ConvertTo(idProp.PropertyInfo.PropertyType);
                idProp.PublicSetter(response, idValue);
            }
            else
            {
                countProp?.PublicSetter(response, retValue.ConvertTo(countProp.PropertyInfo.PropertyType));
            }

            if (resultProp != null)
            {
                var result = await db.SingleByIdAsync<Table>(retValue);
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

        /// <summary>
        /// Inserts new entry into Table
        /// </summary>
        public virtual async Task<object> Create<Table>(ICreateDb<Table> dto)
        {
            //TODO: Allow Create to use Default Values
            using var db = AutoQuery.GetDb<Table>(Request);
            using (Profiler.Current.Step("AutoQuery.Create"))
            {
                var response = await ExecAndReturnResponseAsync<Table>(dto, db,async ctx => {
                    var dtoValues = ResolveDtoValues(dto);
                    var pkFieldDef = typeof(Table).GetModelMetadata()?.PrimaryKey;
                    var isAutoId = pkFieldDef?.AutoId == true;
                        var autoIntId = await db.InsertAsync<Table>(dtoValues, selectIdentity: ctx.IdProp != null || ctx.ResultProp != null);
                        // [AutoId] Guid's populate the PK Property
                        if (isAutoId)
                            return pkFieldDef.GetValue(dtoValues);
                        return autoIntId;
                });
                
                return response;
            }
        }

        /// <summary>
        /// Updates entry into Table
        /// </summary>
        public virtual Task<object> Update<Table>(IUpdateDb<Table> dto)
        {
            var partialDefault = dto.GetType().FirstAttribute<AutoUpdateAttribute>()?.Style == AutoUpdateStyle.NonDefaults;
            return UpdateInternalAsync<Table>(dto, partialDefault);
        }

        /// <summary>
        /// Partially Updates entry into Table (Uses OrmLite UpdateNonDefaults behavior)
        /// </summary>
        public virtual Task<object> Patch<Table>(IPatchDb<Table> dto)
        {
            return UpdateInternalAsync<Table>(dto, skipDefaults:true);
        }

        private async Task<object> UpdateInternalAsync<Table>(object dto, bool skipDefaults)
        {
            using var db = AutoQuery.GetDb<Table>(Request);
            using (Profiler.Current.Step("AutoQuery.Update"))
            {
                var response = ExecAndReturnResponseAsync<Table>(dto, db,
                    async ctx => {
                        var dtoValues = ResolveDtoValues(dto, skipDefaults);
                        var pkFieldDef = typeof(Table).GetModelMetadata()?.PrimaryKey;
                        if (pkFieldDef == null)
                            throw new NotSupportedException($"Table '{typeof(Table).Name}' does not have a primary key");
                        if (!dtoValues.TryGetValue(pkFieldDef.Name, out var idValue) || AutoMappingUtils.IsDefaultValue(idValue))
                            throw new ArgumentNullException(pkFieldDef.Name);
                        
                        // Should only update a Single Row
                        var rowsUpdated = await db.UpdateOnlyAsync<Table>(dtoValues);
                        if (rowsUpdated != 1)
                            throw new OptimisticConcurrencyException($"{rowsUpdated} rows were updated by '{dto.GetType().Name}'");

                        return idValue;
                    }); //TODO: UpdateOnly

                return response;
            }
        }
        
        internal class AutoCrudMetadata
        {
            internal Type DtoType;
            internal TypeProperties DtoProps;
            internal List<AutoPopulateAttribute> PopulateAttrs;
            internal Dictionary<string, AutoUpdateAttribute> UpdateAttrs;
            internal Dictionary<string, AutoDefaultAttribute> DefaultAttrs;
            internal Dictionary<string, AutoMapAttribute> MapAttrs;
            internal HashSet<string> NullableProps;
            internal GetMemberDelegate RowVersionGetter;
            
            static readonly ConcurrentDictionary<Type, AutoCrudMetadata> cache = 
                new ConcurrentDictionary<Type, AutoCrudMetadata>();

            internal static AutoCrudMetadata Create(Type dtoType)
            {
                if (cache.TryGetValue(dtoType, out var to))
                    return to;
                
                to = new AutoCrudMetadata {
                    DtoType = dtoType,
                    DtoProps = TypeProperties.Get(dtoType),
                };
                to.RowVersionGetter = to.DtoProps.GetPublicGetter(Keywords.RowVersion);
                
                var dtoAttrs = dtoType.AllAttributes();
                foreach (var dtoAttr in dtoAttrs)
                {
                    if (dtoAttr is AutoPopulateAttribute populateAttr)
                    {
                        to.PopulateAttrs ??= new List<AutoPopulateAttribute>();
                        to.PopulateAttrs.Add(populateAttr);
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

        private Dictionary<string, object> ResolveDtoValues(object dto, bool skipDefaults=false)
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
                            replaceValues[entry.Key] = appHost.EvalScriptValue(defaultAttr, Request);
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
                    dtoValues[populateAttr.Name] = appHost.EvalScriptValue(populateAttr, Request);
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

        /// <summary>
        /// Deletes entry into Table
        /// </summary>
        public virtual async Task<object> Delete<Table>(IDeleteDb<Table> dto)
        {
            using var db = AutoQuery.GetDb<Table>(Request);
            using (Profiler.Current.Step("AutoQuery.Update"))
            {
                var response = await ExecAndReturnResponseAsync<Table>(dto, db,
                    async ctx => {
                        var dtoValues = ResolveDtoValues(dto, skipDefaults:true);
                        
                        //Should have at least 1 non-default filter
                        if (dtoValues.Count == 0)
                            throw new NotSupportedException($"'{dto.GetType().Name}' did not contain any filters");

                        return await db.DeleteAsync<Table>(dtoValues);
                    });
                
                return response;
            }
        }

        /// <summary>
        /// Inserts or Updates entry into Table
        /// </summary>
        public virtual async Task<object> Save<Table>(ISaveDb<Table> dto)
        {
            using var db = AutoQuery.GetDb<Table>(Request);
            using (Profiler.Current.Step("AutoQuery.Update"))
            {
                var row = dto.ConvertTo<Table>();
                var response = await ExecAndReturnResponseAsync<Table>(dto, db,
                    async ctx => await db.SaveAsync(row)); //TODO: Use Upsert when available
                
                return response;
            }
        }
        
    }
}