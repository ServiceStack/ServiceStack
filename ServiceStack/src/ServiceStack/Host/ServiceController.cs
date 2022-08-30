using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public delegate Task<object> ServiceExecFn(IRequest requestContext, object request);
    public delegate object InstanceExecFn(IRequest requestContext, object instance, object request);
    public delegate object ActionInvokerFn(object instance, object request);
    public delegate void VoidActionInvokerFn(object instance, object request);

    public class ServiceController : IServiceController
    {
        private const string ResponseDtoSuffix = "Response";
        private readonly ServiceStackHost appHost;

        public ServiceController(ServiceStackHost appHost)
        {
            this.appHost = appHost;
            appHost.Container.DefaultOwner = Owner.External;
            this.RequestTypeFactoryMap = new Dictionary<Type, Func<IRequest, object>>();
        }

        public ServiceController(ServiceStackHost appHost, Func<IEnumerable<Type>> resolveServicesFn)
            : this(appHost)
        {
            this.ResolveServicesFn = resolveServicesFn;
        }

        public ServiceController(ServiceStackHost appHost, params Assembly[] assembliesWithServices)
            : this(appHost)
        {
            if (assembliesWithServices == null || assembliesWithServices.Length == 0)
                throw new ArgumentException(
                    "No Assemblies provided in your AppHost's base constructor.\n"
                    + "To register your services, please provide the assemblies where your web services are defined.");

            this.ResolveServicesFn = () => GetAssemblyTypes(assembliesWithServices);
        }

        private readonly HashSet<Type> registeredServices = new();
        readonly Dictionary<Type, ServiceExecFn> requestExecMap = new();
        readonly Dictionary<Type, RestrictAttribute> requestServiceAttrs = new();

        public Dictionary<Type, Func<IRequest, object>> RequestTypeFactoryMap { get; set; }

        public string DefaultOperationsNamespace { get; set; }

        public Func<IEnumerable<Type>> ResolveServicesFn { get; set; }

        private ContainerResolveCache typeFactory;

        public ServiceController Init()
        {
            typeFactory = new ContainerResolveCache(appHost.Container);

            this.Register(typeFactory);

            appHost.Container.RegisterAutoWiredTypes(appHost.Metadata.ServiceTypes);

            return this;
        }

        private List<Type> GetAssemblyTypes(Assembly[] assembliesWithServices)
        {
            var results = new List<Type>();
            string assemblyName = null;
            string typeName = null;

            try
            {
                foreach (var assembly in assembliesWithServices)
                {
                    assemblyName = assembly.FullName;
                    foreach (var type in assembly.GetTypes())
                    {
                        if (appHost.ExcludeAutoRegisteringServiceTypes.Contains(type))
                            continue;

                        typeName = type.GetOperationName();
                        results.Add(type);
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                var msg = $"Failed loading types, last assembly '{assemblyName}', type: '{typeName}'";
                LogManager.GetLogger(GetType()).Error(msg, ex);
                throw new Exception(msg, ex);
            }
        }
        public void RegisterServicesInAssembly(Assembly assembly)
        {
            foreach (var serviceType in assembly.GetTypes().Where(IsServiceType))
            {
                RegisterService(serviceType);
            }
        }

        public void RegisterService(Type serviceType)
        {
            try
            {
                if (!IsServiceType(serviceType))
                    throw new ArgumentException($"Type {serviceType.FullName} is not a Web Service that implements IService");
                
                RegisterService(typeFactory, serviceType);
                appHost.Container.RegisterAutoWiredType(serviceType);
            }
            catch (Exception ex)
            {
                appHost.NotifyStartupException(ex, serviceType.Name, nameof(RegisterService));
                LogManager.GetLogger(GetType()).Error(ex.Message, ex);
            }
        }

        // Called from ServiceController.Init() in AppHost.Init()
        internal void Register(ITypeFactory serviceFactoryFn)
        {
            foreach (var serviceType in ResolveServicesFn())
            {
                RegisterService(serviceFactoryFn, serviceType);
            }
        }

        public void RegisterService(ITypeFactory serviceFactoryFn, Type serviceType)
        {
            var processedReqs = new HashSet<Type>();

            if (IsServiceType(serviceType))
            {
                if (registeredServices.Contains(serviceType))
                    return;
                registeredServices.Add(serviceType);

                var log = LogManager.GetLogger(GetType());
                
                foreach (var mi in serviceType.GetActions())
                {
                    var requestType = mi.GetParameters()[0].ParameterType;
                    if (processedReqs.Contains(requestType)) continue;
                    processedReqs.Add(requestType);

                    RegisterServiceExecutor(requestType, serviceType, serviceFactoryFn);

                    var returnMarker = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
                    var responseType = returnMarker != null ?
                          returnMarker.GetGenericArguments()[0]
                        : mi.ReturnType != typeof(object) && mi.ReturnType != typeof(void) ?
                          mi.ReturnType
#if NETCORE
                        : Type.GetType(requestType.FullName + ResponseDtoSuffix + "," + requestType.Assembly.GetName().Name);
#else                                                  
                        : AssemblyUtils.FindType(requestType.FullName + ResponseDtoSuffix);
#endif
                    if (responseType == typeof(Task))
                        responseType = null;
                    else if (responseType?.Name == "Task`1" && responseType.GetGenericArguments()[0] != typeof(object))
                        responseType = responseType.GetGenericArguments()[0];
                    else if (responseType?.Name == "ValueTask`1" && responseType.GetGenericArguments()[0] != typeof(object))
                        responseType = responseType.GetGenericArguments()[0];
                    
                    RegisterRestPaths(requestType);

                    appHost.Metadata.Add(serviceType, requestType, responseType);

                    if (typeof(IRequiresRequestStream).IsAssignableFrom(requestType))
                    {
                        this.RequestTypeFactoryMap[requestType] = req =>
                        {
                            var restPath = req.GetRoute();
                            var request = restPath != null 
                                ? RestHandler.CreateRequest(req, restPath, req.GetRequestParams(exclude:null), requestType.CreateInstance())
                                : KeyValueDataContractDeserializer.Instance.Parse(req.QueryString, requestType);

                            var rawReq = (IRequiresRequestStream)request;
                            rawReq.RequestStream = req.InputStream;
                            return rawReq;
                        };
                    }

                    if (log.IsDebugEnabled)
                        log.DebugFormat("Registering {0} service '{1}' with request '{2}'",
                            responseType != null ? "Reply" : "OneWay", serviceType.GetOperationName(), requestType.GetOperationName());
                }
            }
        }

        public static bool IsServiceType(Type serviceType)
        {
            return typeof(IService).IsAssignableFrom(serviceType)
                && !serviceType.IsAbstract 
                && !serviceType.IsGenericTypeDefinition 
                && !serviceType.ContainsGenericParameters;
        }

        public static bool IsServiceAction(ActionMethod mi)
        {
            if (mi.IsGenericMethod || mi.GetParameters().Length != 1)
                return false;

            return IsServiceAction(mi.Name, mi.GetParameters()[0].ParameterType);
        }

        public static bool IsServiceAction(string actionName, Type requestType)
        {
            if (requestType.IsValueType || requestType == typeof(string))
                return false;

            actionName = actionName.ToUpper();
            if (actionName.EndsWith(ActionMethod.AsyncUpper))
                actionName = actionName.Substring(0, actionName.Length - ActionMethod.AsyncUpper.Length);

            var ret = HttpMethods.AllVerbs.Contains(actionName) || actionName == ActionContext.AnyAction 
                || HttpMethods.AllVerbs.Any(verb => 
                    ContentTypes.KnownFormats.Any(format => actionName.EqualsIgnoreCase(verb + format))) 
                || ContentTypes.KnownFormats.Any(format => actionName.EqualsIgnoreCase(ActionContext.AnyAction + format));
            return ret;
        }

        public readonly Dictionary<string, List<RestPath>> RestPathMap = new Dictionary<string, List<RestPath>>();

        private static RestPath fallbackRestPath = null;

        public void RegisterRestPaths(Type requestType)
        {
            var attrs = appHost.GetRouteAttributes(requestType);
            foreach (RouteAttribute attr in attrs)
            {
                var restPath = new RestPath(requestType, attr.Path, attr.Verbs, attr.Summary, attr.Notes, attr.Matches)
                {
                    Priority = attr.Priority
                };

                if (attr is FallbackRouteAttribute defaultAttr)
                {
                    if (appHost.Config.FallbackRestPath != null)
                        throw new NotSupportedException(
                            "Config.FallbackRestPath is already defined. Only 1 [FallbackRoute] is allowed.");

                    appHost.Config.FallbackRestPath = httpReq => restPath.IsMatch(httpReq) ? restPath : null;
                    fallbackRestPath = restPath;
                    
                    continue;
                }

                if (!restPath.IsValid)
                    throw new NotSupportedException(
                        $"RestPath '{attr.Path}' on Type '{requestType.GetOperationName()}' is not Valid");

                RegisterRestPath(restPath);
            }
        }

        private static readonly char[] InvalidRouteChars = { '?', '&' };

        public void RegisterRestPath(RestPath restPath)
        {
            if (!restPath.Path.StartsWith("/"))
                throw new ArgumentException($"Route '{restPath.Path}' on '{restPath.RequestType.GetOperationName()}' must start with a '/'");
            if (restPath.Path.IndexOfAny(InvalidRouteChars) != -1)
                throw new ArgumentException($"Route '{restPath.Path}' on '{restPath.RequestType.GetOperationName()}' contains invalid chars. " +
                                            "See https://docs.servicestack.net/routing for info on valid routes.");

            if (!RestPathMap.TryGetValue(restPath.FirstMatchHashKey, out var pathsAtFirstMatch))
            {
                pathsAtFirstMatch = new List<RestPath>();
                RestPathMap[restPath.FirstMatchHashKey] = pathsAtFirstMatch;
            }
            pathsAtFirstMatch.Add(restPath);
        }

        public void AfterInit()
        {
            //Register any routes configured on Metadata.Routes
            foreach (var restPath in appHost.RestPaths)
            {
                RegisterRestPath(restPath);

                //Auto add Route Attributes so they're available in T.ToUrl() extension methods
                restPath.RequestType
                    .AddAttributes(new RouteAttribute(restPath.Path, restPath.AllowedVerbs)
                    {
                        Priority = restPath.Priority,
                        Summary = restPath.Summary,
                        Notes = restPath.Notes,
                    });
            }

            //Sync the RestPaths collections
            appHost.RestPaths.Clear();
            appHost.RestPaths.AddRange(RestPathMap.Values.SelectMany(x => x));
            appHost.RestPaths.ForEach(x => x.AfterInit());

            fallbackRestPath?.AfterInit();

            appHost.Metadata.AfterInit();
        }

        [Obsolete("Use GetRestPathForRequest(httpMethod, pathInfo, httpReq)")]
        public IRestPath GetRestPathForRequest(string httpMethod, string pathInfo) => GetRestPathForRequest(httpMethod, pathInfo, null);
        
        /// <summary>
        /// Get Best Matching Route. 
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="pathInfo"></param>
        /// <param name="httpReq">If not null, ensures any Route matches any [Route(Matches)]</param>
        /// <returns></returns>
        public RestPath GetRestPathForRequest(string httpMethod, string pathInfo, IHttpRequest httpReq)
        {
            var matchUsingPathParts = RestPath.GetPathPartsForMatching(pathInfo);

            List<RestPath> firstMatches;
            RestPath bestMatch = null;

            var yieldedHashMatches = RestPath.GetFirstMatchHashKeys(matchUsingPathParts);
            foreach (var potentialHashMatch in yieldedHashMatches)
            {
                if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

                var bestScore = -1;
                foreach (var restPath in firstMatches)
                {
                    var matchScore = 0;
                    //Handle [Route(Matches)]
                    if (httpReq != null)
                    {
                        var matchFn = restPath.GetRequestRule();
                        if (matchFn != null)
                        {
                            var validRoute = matchFn(httpReq);
                            if (!validRoute)
                                continue;

                            matchScore = 1;
                        }
                    }
                    
                    var score = restPath.MatchScore(httpMethod, matchUsingPathParts) + matchScore;
                    if (score > bestScore) 
                    {
                        bestScore = score;
                        bestMatch = restPath;
                    }
                }
                if (bestScore > 0)
                {
                    return bestMatch;
                }
            }

            var yieldedWildcardMatches = RestPath.GetFirstMatchWildCardHashKeys(matchUsingPathParts);
            foreach (var potentialHashMatch in yieldedWildcardMatches)
            {
                if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

                var bestScore = -1;
                foreach (var restPath in firstMatches)
                {
                    //Handle [Route(Matches)]
                    if (httpReq != null)
                    {
                        var matchFn = restPath.GetRequestRule();
                        if (matchFn != null)
                        {
                            var validRoute = matchFn(httpReq);
                            if (!validRoute)
                                continue;
                        }
                    }

                    var score = restPath.MatchScore(httpMethod, matchUsingPathParts);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatch = restPath;
                    }
                }
                if (bestScore > 0)
                {
                    return bestMatch;
                }
            }

            return null;
        }

        private readonly Dictionary<Type, List<Type>> serviceExecCache = new();
        public void ResetServiceExecCachesIfNeeded(Type serviceType, Type requestType)
        {
            if (!serviceExecCache.TryGetValue(serviceType, out var requestTypes))
            {
                var mi = typeof(ServiceExec<>)
                    .MakeGenericType(serviceType)
                    .GetMethod("Reset", BindingFlags.Public | BindingFlags.Static);

                mi.Invoke(null, new object[] { });

                serviceExecCache[serviceType] = requestTypes = new List<Type>();
            }

            if (!requestTypes.Contains(requestType))
            {
                var mi = typeof(ServiceExec<>)
                    .MakeGenericType(serviceType)
                    .GetMethod("CreateServiceRunnersFor", BindingFlags.Public | BindingFlags.Static)
                    ?.MakeGenericMethod(requestType) ?? throw new Exception("ServiceExec.CreateServiceRunnersFor does not exist");

                mi.Invoke(null, new object[] { });

                requestTypes.Add(requestType);
            }
        }

        public void RegisterServiceExecutor(Type requestType, Type serviceType, ITypeFactory serviceFactoryFn)
        {
            ResetServiceExecCachesIfNeeded(serviceType, requestType);

            var serviceExecDef = typeof(ServiceRequestExec<,>).MakeGenericType(serviceType, requestType);
            var iServiceExec = (IServiceExec)serviceExecDef.CreateInstance();

            Task<object> HandlerFn(IRequest req, object dto)
            {
                var service = serviceFactoryFn.CreateInstance(req, serviceType);

                Task<object> ServiceExec(IRequest reqCtx, object requestDto) => 
                    iServiceExec.Execute(reqCtx, service, requestDto).InTask();

                return ManagedServiceExec(ServiceExec, (IService) service, req, dto);
            }

            AddToRequestExecMap(requestType, serviceType, HandlerFn);
        }

        private void AddToRequestExecMap(Type requestType, Type serviceType, ServiceExecFn handlerFn)
        {
            if (requestExecMap.ContainsKey(requestType))
            {
                throw new AmbiguousMatchException(
                    $"Could not register Request '{requestType.FullName}' with service '{serviceType.FullName}' as it has already been assigned to another service.\n" +
                    "Each Request DTO can only be handled by 1 service.");
            }

            requestExecMap.Add(requestType, handlerFn);

            var requestAttrs = requestType.AllAttributes<RestrictAttribute>();
            if (requestAttrs.Length > 0)
            {
                requestServiceAttrs[requestType] = requestAttrs[0];
            }
            else
            {
                var serviceAttrs = serviceType.AllAttributes<RestrictAttribute>();
                if (serviceAttrs.Length > 0)
                {
                    requestServiceAttrs[requestType] = serviceAttrs[0];
                }
            }
        }

        private async Task<object> ManagedServiceExec(ServiceExecFn serviceExec, IService service, IRequest request, object requestDto)
        {
            try
            {
                InjectRequestContext(service, request);

                object response = null;

                async Task<object> ReleaseAsync(object result)
                {
                    //Gets disposed by AppHost or ContainerAdapter if set
                    if (result is Task taskResponse)
                    {
                        await taskResponse.ConfigAwait();
                        appHost.Release(service);
                        return taskResponse.GetResult();
                    }
                    await using (service as IAsyncDisposable) {}
                    appHost.Release(service);
                    return result;
                }
                
                try
                {
                    requestDto = appHost.OnPreExecuteServiceFilter(service, requestDto, request, request.Response);

                    if (request.Dto == null) // Don't override existing batched DTO[]
                        request.Dto = requestDto;

                    //Executes the service and returns the result
                    response = await serviceExec(request, requestDto).ConfigAwait();

                    response = appHost.OnPostExecuteServiceFilter(service, response, request, request.Response);

                    return await ReleaseAsync(response).ConfigAwait();
                }
                catch (Exception)
                {
                    await ReleaseAsync(response).ConfigAwait();
                    throw;
                }
            }
            catch (TargetInvocationException tex)
            {
                //Mono invokes using reflection
                throw tex.InnerException ?? tex;
            }
        }

        internal static void InjectRequestContext(object service, IRequest req)
        {
            if (req == null) return;

            if (service is IRequiresRequest serviceRequiresContext)
            {
                serviceRequiresContext.Request = req;
            }
        }

        [Obsolete("Use ApplyResponseFiltersAsync")]
        public object ApplyResponseFilters(object response, IRequest req)
        {
            return ApplyResponseFiltersAsync(response, req).Result;
        }

        private async Task<object> ApplyResponseFiltersAsync(object response, IRequest req)
        {
            if (response is Task taskResponse)
            {
                await taskResponse.ConfigAwait();
                response = taskResponse.GetResult();
            }

            response = await appHost.ApplyResponseConvertersAsync(req, response).ConfigAwait();

            await appHost.ApplyResponseFiltersAsync(req, req.Response, response).ConfigAwait();
            if (req.Response.IsClosed)
                return req.Response.Dto;

            return response;
        }

        /// <summary>
        /// Execute MQ
        /// </summary>
        public object ExecuteMessage(IMessage mqMessage)
        {
            return ExecuteMessage(mqMessage, new BasicRequest(mqMessage));
        }

        /// <summary>
        /// Execute MQ
        /// </summary>
        public Task<object> ExecuteMessageAsync(IMessage mqMessage, CancellationToken token=default)
        {
            return ExecuteMessageAsync(mqMessage, new BasicRequest(mqMessage), token);
        }

        /// <summary>
        /// Execute MQ with requestContext
        /// </summary>
        public object ExecuteMessage(IMessage dto, IRequest req)
        {
            RequestContext.Instance.StartRequestContext();
#if NETCORE
            using var scope = req.StartScope();
#endif
            
            req.PopulateFromRequestIfHasSessionId(dto.Body);

            req.Dto = appHost.ApplyRequestConvertersAsync(req, dto.Body).GetAwaiter().GetResult();
            if (appHost.ApplyMessageRequestFilters(req, req.Response, dto.Body))
                return req.Response.Dto;

            var response = Execute(dto.Body, req);

            if (response is Task taskResponse)
                response = taskResponse.GetResult();

            response = appHost.ApplyResponseConvertersAsync(req, response).GetAwaiter().GetResult();

            if (appHost.ApplyMessageResponseFilters(req, req.Response, response))
                response = req.Response.Dto;

            req.Response.EndMqRequest();

            return response;
        }

        /// <summary>
        /// Execute MQ with requestContext
        /// </summary>
        public async Task<object> ExecuteMessageAsync(IMessage dto, IRequest req, CancellationToken token=default)
        {
            RequestContext.Instance.StartRequestContext();
#if NETCORE
            using var scope = req.StartScope();
#endif
            
            req.PopulateFromRequestIfHasSessionId(dto.Body);

            req.Dto = await appHost.ApplyRequestConvertersAsync(req, dto.Body).ConfigAwait();
            if (appHost.ApplyMessageRequestFilters(req, req.Response, dto.Body))
                return req.Response.Dto;

            var response = await ExecuteAsync(dto.Body, req);

            response = await appHost.ApplyResponseConvertersAsync(req, response).ConfigAwait();

            if (appHost.ApplyMessageResponseFilters(req, req.Response, response))
                response = req.Response.Dto;

            req.Response.EndMqRequest();

            return response;
        }

        /// <summary>
        /// Execute using empty RequestContext
        /// </summary>
        public object Execute(object requestDto)
        {
            return Execute(requestDto, new BasicRequest());
        }

        /// <summary>
        /// Execute a Service with a Request DTO. See ExecuteAsync for a non-blocking alternative.
        /// </summary>
        public virtual object Execute(object requestDto, IRequest req)
        {
            req.Dto = requestDto;
            var requestType = requestDto.GetType();
            req.OperationName = requestType.Name;

            if (appHost.Config.EnableAccessRestrictions)
                AssertServiceRestrictions(requestType, req.RequestAttributes);

            var handlerFn = GetService(requestType);
            var responseTask = handlerFn(req, requestDto);
            var response = responseTask.GetResult();

            response = appHost.OnAfterExecute(req, requestDto, response);

            return response;
        }

        /// <summary>
        /// Execute a Service with a Request DTO.
        /// </summary>
        public virtual async Task<object> ExecuteAsync(object requestDto, IRequest req) //Used by HTTP handlers to Execute Services
        {
            req.Dto = requestDto;
            var requestType = requestDto.GetType();
            req.OperationName = requestType.Name;

            if (appHost.Config.EnableAccessRestrictions)
                AssertServiceRestrictions(requestType, req.RequestAttributes);

            var handlerFn = GetService(requestType);
            var responseTask = await handlerFn(req, requestDto).ConfigAwaitNetCore();
            var response = responseTask;

            response = appHost.OnAfterExecute(req, requestDto, response);

            return response;
        }

        // Only Used internally by TypedFilterTests 
        public object Execute(object requestDto, IRequest req, bool applyFilters)
        {
            try
            {
                req.SetInProcessRequest();
                
                if (applyFilters)
                {
                    requestDto = appHost.ApplyRequestConvertersAsync(req, requestDto).Result;

                    appHost.ApplyRequestFiltersAsync(req, req.Response, requestDto).Wait();
                    if (req.Response.IsClosed)
                        return null;
                }

                var response = Execute(requestDto, req);

                return applyFilters
                    ? ApplyResponseFiltersAsync(response, req).Result
                    : response;
            }
            finally 
            {
                req.ReleaseIfInProcessRequest();
            }
        }

        public object Execute(IRequest req, bool applyFilters)
        {
            try
            {
                req.SetInProcessRequest();
                
                var restPath = req is IHttpRequest httpReq
                    ? RestHandler.FindMatchingRestPath(httpReq, out _)
                    : RestHandler.FindMatchingRestPath(req.Verb, req.PathInfo, out _);

                req.SetRoute(restPath as RestPath);
                req.OperationName = restPath.RequestType.GetOperationName();
                var requestDto = RestHandler.CreateRequestAsync(req, restPath).Result;
                req.Dto = requestDto;

                if (applyFilters)
                {
                    requestDto = appHost.ApplyRequestConvertersAsync(req, requestDto).Result;

                    appHost.ApplyRequestFiltersAsync(req, req.Response, requestDto).Wait();
                    if (req.Response.IsClosed)
                        return null;
                }

                var response = Execute(requestDto, req);

                return applyFilters 
                    ? ApplyResponseFiltersAsync(response, req).Result
                    : response;
            }
            finally 
            {
                req.ReleaseIfInProcessRequest();
            }
        }

        public async Task<object> GatewayExecuteAsync(object requestDto, IRequest req, bool applyFilters)
        {
            req.Dto = requestDto;
            var requestType = requestDto.GetType();

            if (appHost.Config.EnableAccessRestrictions)
            {
                AssertServiceRestrictions(requestType, req.RequestAttributes);
            }

            if (applyFilters)
            {
                requestDto = await appHost.ApplyRequestConvertersAsync(req, requestDto).ConfigAwait();
                await appHost.ApplyRequestFiltersAsync(req, req.Response, requestDto).ConfigAwait();
                if (req.Response.IsClosed)
                    return null;
            }

            var handlerFn = GetService(requestType);
            var taskObj = handlerFn(req, requestDto);

            var response = await taskObj.ConfigAwait();

            if (response is Task[] tasks)
            {
                await Task.WhenAll(tasks).ConfigAwait();

                object[] ret = null;
                for (int i = 0; i < tasks.Length; i++)
                {
                    var tResult = tasks[i];
                    if (ret == null)
                        ret = (object[])Array.CreateInstance(tResult.GetType(), tasks.Length);

                    ret[i] = applyFilters ? await ApplyResponseFiltersAsync(tResult, req).ConfigAwait() : tResult;
                }
                return ret;
            }

            if (applyFilters)
                return await ApplyResponseFiltersAsync(response, req).ConfigAwait(); 
            return response;
        }

        public bool HasService(Type requestType) => requestExecMap.ContainsKey(requestType);

        public virtual ServiceExecFn GetService(Type requestType)
        {
            if (!requestExecMap.TryGetValue(requestType, out var handlerFn))
            {
                if (requestType.IsArray)
                {
                    var elType = requestType.GetElementType();
                    if (requestExecMap.TryGetValue(elType, out handlerFn))
                    {
                        return CreateAutoBatchServiceExec(handlerFn);
                    }
                }

                throw new NotImplementedException($"Unable to resolve service '{requestType.GetOperationName()}'");
            }

            return handlerFn;
        }

        private static ServiceExecFn CreateAutoBatchServiceExec(ServiceExecFn handlerFnAsync)
        {
            return async (req, dtos) => 
            {
                var dtosList = ((IEnumerable) dtos).Map(x => x);
                if (dtosList.Count == 0)
                    return TypeConstants.EmptyObjectArray;

                var firstDto = dtosList[0];

                req.Items[Keywords.AutoBatchIndex] = 0;

                var firstResponse = await handlerFnAsync(req, firstDto).ConfigAwait();
                if (firstResponse is Exception)
                {
                    req.SetAutoBatchCompletedHeader(0);
                    return firstResponse;
                }

                //sync
                if (!(firstResponse is Task asyncResponse)) 
                {
                    var ret = firstResponse != null
                        ? (object[])Array.CreateInstance(firstResponse.GetType(), dtosList.Count)
                        : new object[dtosList.Count];

                    ret[0] = firstResponse; //don't re-execute first request
                    for (var i = 1; i < dtosList.Count; i++)
                    {
                        var dto = dtosList[i];
                        req.Items[Keywords.AutoBatchIndex] = i;
                        var response = await handlerFnAsync(req, dto).ConfigAwait();
                        //short-circuit on first error
                        if (response is Exception)
                        {
                            req.SetAutoBatchCompletedHeader(i);
                            return response;
                        }

                        ret[i] = response;
                    }
                    req.Items.Remove(Keywords.AutoBatchIndex);
                    req.SetAutoBatchCompletedHeader(dtosList.Count);
                    return ret;
                }

                //async
                var asyncResponses = new Task[dtosList.Count];
                asyncResponses[0] = asyncResponse; //don't re-execute first request

                for (var i = 1; i < dtosList.Count; i++)
                {
                    try
                    {
                        req.Items[Keywords.AutoBatchIndex] = i;
                        var dto = dtosList[i];

                        var task = handlerFnAsync(req, dto);
                        await task.ConfigAwait();
                        asyncResponses[i] = task;
                    }
                    catch (Exception e)
                    {
                        req.SetAutoBatchCompletedHeader(i);
                        return e;
                    }
                }
                req.Items.Remove(Keywords.AutoBatchIndex);
                req.SetAutoBatchCompletedHeader(dtosList.Count);
                return asyncResponses;
            };
        }

        public void AssertServiceRestrictions(Type requestType, RequestAttributes actualAttributes)
        {
            if (!appHost.Config.EnableAccessRestrictions) 
                return;

            var hasNoAccessRestrictions = !requestServiceAttrs.TryGetValue(requestType, out var restrictAttr)
                || restrictAttr.HasNoAccessRestrictions;
            if (hasNoAccessRestrictions)
                return;

            if (restrictAttr.AccessTo != RequestAttributes.None && (RequestAttributes.InProcess & actualAttributes) == RequestAttributes.InProcess) 
                return;

            var failedScenarios = StringBuilderCache.Allocate();
            foreach (var requiredScenario in restrictAttr.AccessibleToAny)
            {
                var allServiceRestrictionsMet = (requiredScenario & actualAttributes) == actualAttributes;
                if (allServiceRestrictionsMet)
                {
                    return;
                }

                var passed = requiredScenario & actualAttributes;
                var failed = requiredScenario & ~(passed);

                failedScenarios.Append($"\n -[{failed}]");
            }

            var internalDebugMsg = (RequestAttributes.InternalNetworkAccess & actualAttributes) != 0
                ? "\n Unauthorized call was made from: " + actualAttributes
                : "";

            throw new UnauthorizedAccessException(
                $"Could not execute service '{requestType.GetOperationName()}', The following restrictions were not met: " +
                $"'{StringBuilderCache.ReturnAndFree(failedScenarios)}'{internalDebugMsg}");
        }
    }

}