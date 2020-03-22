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

    public class CrudContext
    {
        public IRequest Request { get; private set; }
        public IDbConnection Db { get; private set; }
        public ICrudEvents Events { get; private set; }
        public string Operation { get; set; }
        public object Dto { get; private set; }
        public Type ModelType { get; private set; }
        public Type RequestType { get; private set; }
        public Type ResponseType { get; private set; }
        public ModelDefinition ModelDef { get; private set; }
        public PropertyAccessor IdProp { get; private set; }
        public PropertyAccessor ResultProp { get; private set; }
        public PropertyAccessor CountProp { get; private set; }
        public PropertyAccessor RowVersionProp { get; private set; }
        
        public object Id { get; set; }
        
        public long? RowsUpdated { get; set; }

        internal void SetResult(AutoQuery.ExecValue result)
        {
            Id = result.Id;
            RowsUpdated = result.RowsUpdated;
        }
        
        internal GetMemberDelegate RequestIdGetter() => 
            TypeProperties.Get(RequestType).GetPublicGetter(ModelDef.PrimaryKey.Name);
        
        internal void ThrowPrimaryKeyRequiredForRowVersion() =>
            throw new NotSupportedException($"Could not resolve Primary Key from '{RequestType.Name}' to be able to resolve RowVersion");

        internal static CrudContext Create<Table>(IRequest request, IDbConnection db, object dto, string operation)
        {
            var appHost = HostContext.AppHost;
            var requestType = dto?.GetType() ?? throw new ArgumentNullException(nameof(dto));
            var responseType = appHost.Metadata.GetOperation(requestType)?.ResponseType;
            var responseProps = responseType == null ? null : TypeProperties.Get(responseType);
            return new CrudContext {
                Operation = operation,
                Request = request ?? throw new ArgumentNullException(nameof(request)),
                Db = db ?? throw new ArgumentNullException(nameof(db)),
                Events = appHost.TryResolve<ICrudEvents>(),
                Dto = dto,
                ModelType = typeof(Table),
                RequestType = requestType,
                ModelDef = typeof(Table).GetModelMetadata(),
                ResponseType = responseType,
                IdProp = responseProps?.GetAccessor(Keywords.Id),
                CountProp = responseProps?.GetAccessor(Keywords.Count),
                ResultProp = responseProps?.GetAccessor(Keywords.Result),
                RowVersionProp = responseProps?.GetAccessor(Keywords.RowVersion),
            };
        }
    }

    public static class AutoCrudOperation
    {
        public const string Create = nameof(Create);
        public const string Update = nameof(Update);
        public const string Patch = nameof(Patch);
        public const string Delete = nameof(Delete);
        public const string Save = nameof(Save);

        public static string ToHttpMethod(string operation) => operation switch {
            Create => HttpMethods.Post,
            Update => HttpMethods.Put,
            Patch => HttpMethods.Patch,
            Delete => HttpMethods.Delete,
            Save => HttpMethods.Post,
        };

        public static AutoCrudDtoType? GetCrudGenericDefTypes(Type requestType, Type crudType)
        {
            var genericDef = requestType.GetTypeWithGenericTypeDefinitionOf(crudType);
            if (genericDef != null)
                return new AutoCrudDtoType(genericDef, crudType);
            return null;
        }

        public static AutoCrudDtoType? GetAutoCrudDtoType(Type requestType)
        {
            var crudTypes = GetCrudGenericDefTypes(requestType, typeof(ICreateDb<>))
                ?? GetCrudGenericDefTypes(requestType, typeof(IUpdateDb<>))
                ?? GetCrudGenericDefTypes(requestType, typeof(IDeleteDb<>))
                ?? GetCrudGenericDefTypes(requestType, typeof(IPatchDb<>))
                ?? GetCrudGenericDefTypes(requestType, typeof(ISaveDb<>));
            return crudTypes;
        }
        
        public static AutoCrudDtoType AssertAutoCrudDtoType(Type requestType) =>
            GetAutoCrudDtoType(requestType) ?? throw new NotSupportedException($"{requestType.Name} is not an ICrud Type");
    }

    public struct AutoCrudDtoType
    {
        public Type GenericDef { get; }
        public Type ModelType { get; }
        public AutoCrudDtoType(Type genericDef, Type modelType)
        {
            GenericDef = genericDef;
            ModelType = modelType;
        }
    }
    

    public partial class AutoQuery : IAutoCrudDb
    {
        public object Create<Table>(ICreateDb<Table> dto, IRequest req)
        {
            //TODO: Allow Create to use Default Values
            using var db = GetDb<Table>(req);
            using var profiler = Profiler.Current.Step("AutoQuery.Create");

            var response = ExecAndReturnResponse<Table>(CrudContext.Create<Table>(req,db,dto,AutoCrudOperation.Create),
                ctx => {
                    var dtoValues = ResolveDtoValues(req, dto);
                    var pkField = ctx.ModelDef.PrimaryKey;
                    var selectIdentity = ctx.IdProp != null || ctx.ResultProp != null || ctx.Events != null;

                    //Use same Id if being executed from id
                    if (req.Items.TryGetValue(Keywords.EventModelId, out var eventId) && eventId != null 
                        && !dtoValues.ContainsKey(pkField.Name))
                    {
                        dtoValues[pkField.Name] = eventId.ConvertTo(pkField.PropertyInfo.PropertyType);
                        selectIdentity = false;
                    }

                    var autoIntId = db.Insert<Table>(dtoValues, selectIdentity: selectIdentity);
                    return CreateInternal(dtoValues, pkField, selectIdentity, autoIntId);
                });
                
            return response;
        }

        public async Task<object> CreateAsync<Table>(ICreateDb<Table> dto, IRequest req)
        {
            //TODO: Allow Create to use Default Values
            using var db = GetDb<Table>(req);
            using var profiler = Profiler.Current.Step("AutoQuery.Create");

            var response = await ExecAndReturnResponseAsync<Table>(CrudContext.Create<Table>(req,db,dto,AutoCrudOperation.Create),
                async ctx => {
                    var dtoValues = ResolveDtoValues(ctx.Request, ctx.Dto);
                    var pkField = ctx.ModelDef.PrimaryKey;
                    var selectIdentity = ctx.IdProp != null || ctx.ResultProp != null || ctx.Events != null;

                    //Use same Id if being executed from id
                    if (req.Items.TryGetValue(Keywords.EventModelId, out var eventId) && eventId != null 
                        && !dtoValues.ContainsKey(pkField.Name))
                    {
                        dtoValues[pkField.Name] = eventId.ConvertTo(pkField.PropertyInfo.PropertyType);
                        selectIdentity = false;
                    }
                    
                    var autoIntId = await db.InsertAsync<Table>(dtoValues, selectIdentity: selectIdentity);
                    return CreateInternal(dtoValues, pkField, selectIdentity, autoIntId);
                });
                
            return response;
        }

        private static ExecValue CreateInternal(Dictionary<string, object> dtoValues,
            FieldDefinition pkField, bool selectIdentity, long autoIntId)
        {
            // [AutoId] Guid's populate the PK Property or return Id if provided
            var isAutoId = pkField?.AutoId == true;
            var providedId = pkField != null && dtoValues.ContainsKey(pkField.Name);
            if (isAutoId || providedId)
                return new ExecValue(pkField.GetValue(dtoValues), selectIdentity ? 1 : autoIntId);

            return selectIdentity
                ? new ExecValue(autoIntId, 1)
                : pkField != null && dtoValues.TryGetValue(pkField.Name, out var idValue)
                    ? new ExecValue(idValue, autoIntId)
                    : new ExecValue(null, autoIntId);
        }

        public object Update<Table>(IUpdateDb<Table> dto, IRequest req)
        {
            return UpdateInternal<Table>(req, dto,AutoCrudOperation.Update);
        }

        public Task<object> UpdateAsync<Table>(IUpdateDb<Table> dto, IRequest req)
        {
            return UpdateInternalAsync<Table>(req, dto, AutoCrudOperation.Update);
        }

        public object Patch<Table>(IPatchDb<Table> dto, IRequest req)
        {
            return UpdateInternal<Table>(req, dto, AutoCrudOperation.Patch);
        }

        public Task<object> PatchAsync<Table>(IPatchDb<Table> dto, IRequest req)
        {
            return UpdateInternalAsync<Table>(req, dto, AutoCrudOperation.Patch);
        }

        public object Delete<Table>(IDeleteDb<Table> dto, IRequest req)
        {
            using var db = GetDb<Table>(req);
            using var profiler = Profiler.Current.Step("AutoQuery.Delete");
            
            var response = ExecAndReturnResponse<Table>(CrudContext.Create<Table>(req,db,dto,AutoCrudOperation.Delete),
                ctx => {
                    var dtoValues = ResolveDtoValues(ctx.Request, ctx.Dto, skipDefaults:true);
                    var idValue = ctx.ModelDef.PrimaryKey != null && dtoValues.TryGetValue(ctx.ModelDef.PrimaryKey.Name, out var oId)
                        ? oId
                        : null;
                    var q = DeleteInternal<Table>(ctx, dtoValues);
                    if (q != null)
                        return new ExecValue(idValue, ctx.Db.Delete(q));
                    return new ExecValue(idValue, ctx.Db.Delete<Table>(dtoValues));
                });
            
            return response;
        }

        public async Task<object> DeleteAsync<Table>(IDeleteDb<Table> dto, IRequest req)
        {
            using var db = GetDb<Table>(req);
            using var profiler = Profiler.Current.Step("AutoQuery.Delete");
            
            var response = await ExecAndReturnResponseAsync<Table>(CrudContext.Create<Table>(req,db,dto,AutoCrudOperation.Delete),
                async ctx => {
                    var dtoValues = ResolveDtoValues(req, dto, skipDefaults:true);
                    var idValue = ctx.ModelDef.PrimaryKey != null && dtoValues.TryGetValue(ctx.ModelDef.PrimaryKey.Name, out var oId)
                        ? oId
                        : null;
                    var q = DeleteInternal<Table>(ctx, dtoValues);
                    if (q != null)
                        return new ExecValue(idValue, await ctx.Db.DeleteAsync(q));
                    return new ExecValue(idValue, await ctx.Db.DeleteAsync<Table>(dtoValues));
                });
            
            return response;
        }

        internal SqlExpression<Table> DeleteInternal<Table>(CrudContext ctx, Dictionary<string, object> dtoValues)
        {
            //Should have at least 1 non-default filter
            if (dtoValues.Count == 0)
                throw new NotSupportedException($"'{ctx.RequestType.Name}' did not contain any filters");
                    
            // Should only update a Single Row
            if (GetAutoFilterExpressions(ctx, dtoValues, out var expr, out var exprParams))
            {
                //If there were Auto Filters, construct filter expression manually by adding any remaining DTO values
                foreach (var entry in dtoValues)
                {
                    var fieldDef = ctx.ModelDef.GetFieldDefinition(entry.Key);
                    if (fieldDef == null)
                        throw new NotSupportedException($"Unknown '{entry.Key}' Field in '{ctx.RequestType.Name}' IDeleteDb<{typeof(Table).Name}> Request");
                            
                    if (expr.Length > 0)
                        expr += " AND ";

                    var quotedColumn = ctx.Db.GetDialectProvider().GetQuotedColumnName(ctx.ModelDef, fieldDef);

                    expr += quotedColumn + " = {" + exprParams.Count + "}";
                    exprParams.Add(entry.Value);
                }

                var q = ctx.Db.From<Table>();
                q.Where(expr, exprParams.ToArray());
                return q;
            }
            return null;
        }

        public object Save<Table>(ISaveDb<Table> dto, IRequest req)
        {
            using var db = GetDb<Table>(req);
            using var profiler = Profiler.Current.Step("AutoQuery.Save");

            var row = dto.ConvertTo<Table>();
            var response = ExecAndReturnResponse<Table>(CrudContext.Create<Table>(req,db,dto,AutoCrudOperation.Save),
                ctx => {
                    ctx.Db.Save(row);
                    return SaveInternal(dto, ctx);
                }); 
                
            return response;
        }

        public async Task<object> SaveAsync<Table>(ISaveDb<Table> dto, IRequest req)
        {
            using var db = GetDb<Table>(req);
            using var profiler = Profiler.Current.Step("AutoQuery.Save");

            var row = dto.ConvertTo<Table>();
            var response = await ExecAndReturnResponseAsync<Table>(CrudContext.Create<Table>(req,db,dto,AutoCrudOperation.Save),
                async ctx => {
                    await ctx.Db.SaveAsync(row);
                    return SaveInternal(dto, ctx);
                }); 
                
            return response;
        }

        private static ExecValue SaveInternal<Table>(ISaveDb<Table> dto, CrudContext ctx)
        {
            //TODO: Use Upsert when available
            object idValue = null;
            var pkField = ctx.ModelDef.PrimaryKey;
            if (pkField != null)
            {
                var propGetter = TypeProperties.Get(dto.GetType()).GetPublicGetter(pkField.Name);
                if (propGetter != null)
                    idValue = propGetter(dto);
            }

            return new ExecValue(idValue, 1);
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
        
        private object ExecAndReturnResponse<Table>(CrudContext context, Func<CrudContext,ExecValue> fn)
        {
            var ignoreEvent = context.Request.Items.ContainsKey(Keywords.IgnoreEvent);
            var trans = context.Events != null && !ignoreEvent
                ? context.Db.OpenTransaction()
                : null;

            using (trans)
            {
                context.SetResult(fn(context));
                if (context.Events != null && !ignoreEvent)
                    context.Events?.Record(context);
                
                trans?.Commit();
            }
            
            if (context.ResponseType == null)
                return null;

            object idValue = null;
                
            var response = context.ResponseType.CreateInstance();
            if (context.IdProp != null && context.Id != null)
            {
                idValue = context.Id.ConvertTo(context.IdProp.PropertyInfo.PropertyType);
                context.IdProp.PublicSetter(response, idValue);
            }
            if (context.CountProp != null && context.RowsUpdated != null)
            {
                context.CountProp.PublicSetter(response, context.RowsUpdated.ConvertTo(context.CountProp.PropertyInfo.PropertyType));
            }

            if (context.ResultProp != null && context.Id != null)
            {
                var result = context.Db.SingleById<Table>(context.Id);
                context.ResultProp.PublicSetter(response, result.ConvertTo(context.ResultProp.PropertyInfo.PropertyType));
            }

            if (context.RowVersionProp != null)
            {
                if (AutoMappingUtils.IsDefaultValue(idValue))
                {
                    var dtoIdGetter = context.RequestIdGetter();
                    if (dtoIdGetter != null)
                        idValue = dtoIdGetter(context.Dto);
                }
                if (AutoMappingUtils.IsDefaultValue(idValue))
                    context.ThrowPrimaryKeyRequiredForRowVersion();
                
                var rowVersion = context.Db.GetRowVersion<Table>(idValue);
                context.RowVersionProp.PublicSetter(response, rowVersion.ConvertTo(context.RowVersionProp.PropertyInfo.PropertyType));
            }
            
            return response;
        }

        private async Task<object> ExecAndReturnResponseAsync<Table>(CrudContext context, Func<CrudContext,Task<ExecValue>> fn)
        {
            var ignoreEvent = context.Request.Items.ContainsKey(Keywords.IgnoreEvent);
            var trans = context.Events != null && !ignoreEvent
                ? context.Db.OpenTransaction()
                : null;

            using (trans)
            {
                context.SetResult(await fn(context));
                if (context.Events != null && !ignoreEvent)
                    context.Events?.Record(context);
                
                trans?.Commit();
            }
            
            if (context.ResponseType == null)
                return null;

            object idValue = null;
                
            var response = context.ResponseType.CreateInstance();
            if (context.IdProp != null && context.Id != null)
            {
                idValue = context.Id.ConvertTo(context.IdProp.PropertyInfo.PropertyType);
                context.IdProp.PublicSetter(response, idValue);
            }
            if (context.CountProp != null && context.RowsUpdated != null)
            {
                context.CountProp.PublicSetter(response, context.RowsUpdated.ConvertTo(context.CountProp.PropertyInfo.PropertyType));
            }

            if (context.ResultProp != null && context.Id != null)
            {
                var result = await context.Db.SingleByIdAsync<Table>(context.Id);
                context.ResultProp.PublicSetter(response, result.ConvertTo(context.ResultProp.PropertyInfo.PropertyType));
            }

            if (context.RowVersionProp != null)
            {
                if (AutoMappingUtils.IsDefaultValue(idValue))
                {
                    var dtoIdGetter = context.RequestIdGetter();
                    if (dtoIdGetter != null)
                        idValue = dtoIdGetter(context.Dto);
                }

                if (AutoMappingUtils.IsDefaultValue(idValue))
                    context.ThrowPrimaryKeyRequiredForRowVersion();
                
                var rowVersion = await context.Db.GetRowVersionAsync<Table>(idValue);
                context.RowVersionProp.PublicSetter(response, rowVersion.ConvertTo(context.RowVersionProp.PropertyInfo.PropertyType));
            }
            
            return response;
        }

        internal bool GetAutoFilterExpressions(CrudContext ctx, Dictionary<string, object> dtoValues, out string expr, out List<object> exprParams)
        {
            var meta = AutoCrudMetadata.Create(ctx.RequestType);
            if (meta.AutoFilters != null)
            {
                var dialectProvider = ctx.Db.GetDialectProvider();
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
                        throw new NotSupportedException($"{ctx.RequestType.Name} '{filter.Field}' AutoFilter was not found on '{ctx.ModelType.Name}'");

                    var quotedColumn = dialectProvider.GetQuotedColumnName(meta.ModelDef, fieldDef);

                    var value = appHost.EvalScriptValue(filter, ctx.Request);
                    
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

        private object UpdateInternal<Table>(IRequest req, object dto, string operation)
        {
            var skipDefaults = operation == AutoCrudOperation.Patch;
            using var db = GetDb<Table>(req);
            using (Profiler.Current.Step("AutoQuery.Update"))
            {
                var response = ExecAndReturnResponse<Table>(CrudContext.Create<Table>(req,db,dto,operation),
                    ctx => {
                        var dtoValues = ResolveDtoValues(req, dto, skipDefaults);
                        var pkField = ctx.ModelDef?.PrimaryKey;
                        if (pkField == null)
                            throw new NotSupportedException($"Table '{typeof(Table).Name}' does not have a primary key");
                        if (!dtoValues.TryGetValue(pkField.Name, out var idValue) || AutoMappingUtils.IsDefaultValue(idValue))
                            throw new ArgumentNullException(pkField.Name);
                        
                        // Should only update a Single Row
                        var rowsUpdated = GetAutoFilterExpressions(ctx, dtoValues, out var expr, out var exprParams) 
                            ? ctx.Db.UpdateOnly<Table>(dtoValues, expr, exprParams.ToArray())
                            : ctx.Db.UpdateOnly<Table>(dtoValues);

                        if (rowsUpdated != 1)
                            throw new OptimisticConcurrencyException($"{rowsUpdated} rows were updated by '{dto.GetType().Name}'");

                        return new ExecValue(idValue, rowsUpdated);
                    }); //TODO: UpdateOnly

                return response;
            }
        }

        private async Task<object> UpdateInternalAsync<Table>(IRequest req, object dto, string operation)
        {
            var skipDefaults = operation == AutoCrudOperation.Patch;
            using var db = GetDb<Table>(req);
            using (Profiler.Current.Step("AutoQuery.Update"))
            {
                var response = await ExecAndReturnResponseAsync<Table>(CrudContext.Create<Table>(req,db,dto,operation), 
                    async ctx => {
                        var dtoValues = ResolveDtoValues(req, dto, skipDefaults);
                        var pkField = ctx.ModelDef?.PrimaryKey;
                        if (pkField == null)
                            throw new NotSupportedException($"Table '{typeof(Table).Name}' does not have a primary key");
                        if (!dtoValues.TryGetValue(pkField.Name, out var idValue) || AutoMappingUtils.IsDefaultValue(idValue))
                            throw new ArgumentNullException(pkField.Name);
                        
                        // Should only update a Single Row
                        var rowsUpdated = GetAutoFilterExpressions(ctx, dtoValues, out var expr, out var exprParams) 
                            ? await ctx.Db.UpdateOnlyAsync<Table>(dtoValues, expr, exprParams.ToArray())
                            : await ctx.Db.UpdateOnlyAsync<Table>(dtoValues);

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
            internal List<string> RemoveDtoProps;
            
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
                
                var crudTypes = AutoCrudOperation.GetAutoCrudDtoType(requestType);
                return crudTypes?.GenericDef.GenericTypeArguments[0];
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

                    if (IgnoreCrudProperties.Contains(pi.Name) && to.ModelDef.GetFieldDefinition(propName) == null)
                    {
                        to.RemoveDtoProps ??= new List<string>();
                        to.RemoveDtoProps.Add(pi.Name);
                    }
                }

                return cache[dtoType] = to;
            }
        }
        
        public static HashSet<string> IgnoreCrudProperties { get; } = new HashSet<string> {
            nameof(IHasSessionId.SessionId),
            nameof(IHasBearerToken.BearerToken),
            nameof(IHasVersion.Version),
        };

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
            List<string> removeKeys = null;
            if (meta.RemoveDtoProps != null)
            {
                foreach (var removeDtoProp in meta.RemoveDtoProps)
                {
                    removeKeys ??= new List<string>();
                    removeKeys.Add(removeDtoProp);
                }
            }

            var appHost = HostContext.AppHost;
            if (skipDefaults || meta.UpdateAttrs != null || meta.DefaultAttrs != null)
            {
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
                
                if (replaceValues != null)
                {
                    foreach (var entry in replaceValues)
                    {
                        dtoValues[entry.Key] = entry.Value;
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
        public virtual object Create<Table>(ICreateDb<Table> dto) => AutoQuery.Create(dto, Request);

        public virtual Task<object> CreateAsync<Table>(ICreateDb<Table> dto) => AutoQuery.CreateAsync(dto, Request);

        public virtual object Update<Table>(IUpdateDb<Table> dto) => AutoQuery.Update(dto, Request);

        public virtual Task<object> UpdateAsync<Table>(IUpdateDb<Table> dto) => AutoQuery.UpdateAsync(dto, Request);

        public virtual object Patch<Table>(IPatchDb<Table> dto) => AutoQuery.Patch(dto, Request);

        public virtual Task<object> PatchAsync<Table>(IPatchDb<Table> dto) => AutoQuery.PatchAsync(dto, Request);

        public virtual object Delete<Table>(IDeleteDb<Table> dto) => AutoQuery.Delete(dto, Request);

        public virtual Task<object> DeleteAsync<Table>(IDeleteDb<Table> dto) => AutoQuery.DeleteAsync(dto, Request);

        public virtual object Save<Table>(ISaveDb<Table> dto) => AutoQuery.Save(dto, Request);

        public virtual Task<object> SaveAsync<Table>(ISaveDb<Table> dto) => AutoQuery.SaveAsync(dto, Request);
    }
}