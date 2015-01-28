using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public delegate object ServiceExecFn(IRequest requestContext, object request);
    public delegate object InstanceExecFn(IRequest requestContext, object intance, object request);
    public delegate object ActionInvokerFn(object intance, object request);
    public delegate void VoidActionInvokerFn(object intance, object request);

    public class ServiceController : IServiceController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceController));
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

        readonly Dictionary<Type, ServiceExecFn> requestExecMap
            = new Dictionary<Type, ServiceExecFn>();

        readonly Dictionary<Type, RestrictAttribute> requestServiceAttrs
            = new Dictionary<Type, RestrictAttribute>();

        public Dictionary<Type, Func<IRequest, object>> RequestTypeFactoryMap { get; set; }

        public string DefaultOperationsNamespace { get; set; }

        private IResolver resolver;
        public IResolver Resolver
        {
            get { return resolver ?? Service.GlobalResolver; }
            set { resolver = value; }
        }

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
                var msg = string.Format("Failed loading types, last assembly '{0}', type: '{1}'", assemblyName, typeName);
                Log.Error(msg, ex);
                throw new Exception(msg, ex);
            }
        }

        public void RegisterService(Type serviceType)
        {
            try
            {
                var isNService = typeof(IService).IsAssignableFrom(serviceType);
                if (!isNService)
                    throw new ArgumentException("Type {0} is not a Web Service that inherits IService".Fmt(serviceType.FullName));
                
                RegisterService(typeFactory, serviceType);
                appHost.Container.RegisterAutoWiredType(serviceType);
            }
            catch (Exception ex)
            {
                appHost.NotifyStartupException(ex);
                Log.Error(ex.Message, ex);
            }
        }

        public void Register(ITypeFactory serviceFactoryFn)
        {
            foreach (var serviceType in ResolveServicesFn())
            {
                RegisterService(serviceFactoryFn, serviceType);
            }
        }

        public void RegisterService(ITypeFactory serviceFactoryFn, Type serviceType)
        {
            var processedReqs = new HashSet<Type>();

            if (typeof(IService).IsAssignableFrom(serviceType)
                && !serviceType.IsAbstract && !serviceType.IsGenericTypeDefinition && !serviceType.ContainsGenericParameters)
            {
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
                        : AssemblyUtils.FindType(requestType.FullName + ResponseDtoSuffix);

                    RegisterRestPaths(requestType);

                    appHost.Metadata.Add(serviceType, requestType, responseType);

                    if (typeof(IRequiresRequestStream).IsAssignableFrom(requestType))
                    {
                        this.RequestTypeFactoryMap[requestType] = req =>
                        {
                            var restPath = req.GetRoute();
                            var request = RestHandler.CreateRequest(req, restPath, req.GetRequestParams(), requestType.CreateInstance());

                            var rawReq = (IRequiresRequestStream)request;
                            rawReq.RequestStream = req.InputStream;
                            return rawReq;
                        };
                    }

                    Log.DebugFormat("Registering {0} service '{1}' with request '{2}'",
                        (responseType != null ? "Reply" : "OneWay"), serviceType.GetOperationName(), requestType.GetOperationName());
                }
            }
        }

        public readonly Dictionary<string, List<RestPath>> RestPathMap = new Dictionary<string, List<RestPath>>();

        public void RegisterRestPaths(Type requestType)
        {
            var attrs = appHost.GetRouteAttributes(requestType);
            foreach (RouteAttribute attr in attrs)
            {
                var restPath = new RestPath(requestType, attr.Path, attr.Verbs, attr.Summary, attr.Notes);

                var defaultAttr = attr as FallbackRouteAttribute;
                if (defaultAttr != null)
                {
                    if (appHost.Config.FallbackRestPath != null)
                        throw new NotSupportedException(string.Format(
                            "Config.FallbackRestPath is already defined. Only 1 [FallbackRoute] is allowed."));

                    appHost.Config.FallbackRestPath = (httpMethod, pathInfo, filePath) =>
                    {
                        var pathInfoParts = RestPath.GetPathPartsForMatching(pathInfo);
                        return restPath.IsMatch(httpMethod, pathInfoParts) ? restPath : null;
                    };

                    continue;
                }

                if (!restPath.IsValid)
                    throw new NotSupportedException(string.Format(
                        "RestPath '{0}' on Type '{1}' is not Valid", attr.Path, requestType.GetOperationName()));

                RegisterRestPath(restPath);
            }
        }

        private static readonly char[] InvalidRouteChars = new[] { '?', '&' };

        public void RegisterRestPath(RestPath restPath)
        {
            if (!restPath.Path.StartsWith("/"))
                throw new ArgumentException("Route '{0}' on '{1}' must start with a '/'".Fmt(restPath.Path, restPath.RequestType.GetOperationName()));
            if (restPath.Path.IndexOfAny(InvalidRouteChars) != -1)
                throw new ArgumentException(("Route '{0}' on '{1}' contains invalid chars. " +
                                            "See https://github.com/ServiceStack/ServiceStack/wiki/Routing for info on valid routes.").Fmt(restPath.Path, restPath.RequestType.GetOperationName()));

            List<RestPath> pathsAtFirstMatch;
            if (!RestPathMap.TryGetValue(restPath.FirstMatchHashKey, out pathsAtFirstMatch))
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

            appHost.Metadata.AfterInit();
        }

        public IRestPath GetRestPathForRequest(string httpMethod, string pathInfo)
        {
            var matchUsingPathParts = RestPath.GetPathPartsForMatching(pathInfo);

            List<RestPath> firstMatches;

            var yieldedHashMatches = RestPath.GetFirstMatchHashKeys(matchUsingPathParts);
            foreach (var potentialHashMatch in yieldedHashMatches)
            {
                if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

                var bestScore = -1;
                foreach (var restPath in firstMatches)
                {
                    var score = restPath.MatchScore(httpMethod, matchUsingPathParts);
                    if (score > bestScore) bestScore = score;
                }
                if (bestScore > 0)
                {
                    foreach (var restPath in firstMatches)
                    {
                        if (bestScore == restPath.MatchScore(httpMethod, matchUsingPathParts))
                            return restPath;
                    }
                }
            }

            var yieldedWildcardMatches = RestPath.GetFirstMatchWildCardHashKeys(matchUsingPathParts);
            foreach (var potentialHashMatch in yieldedWildcardMatches)
            {
                if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

                var bestScore = -1;
                foreach (var restPath in firstMatches)
                {
                    var score = restPath.MatchScore(httpMethod, matchUsingPathParts);
                    if (score > bestScore) bestScore = score;
                }
                if (bestScore > 0)
                {
                    foreach (var restPath in firstMatches)
                    {
                        if (bestScore == restPath.MatchScore(httpMethod, matchUsingPathParts))
                            return restPath;
                    }
                }
            }

            return null;
        }

        internal class TypeFactoryWrapper : ITypeFactory
        {
            private readonly Func<Type, object> typeCreator;

            public TypeFactoryWrapper(Func<Type, object> typeCreator)
            {
                this.typeCreator = typeCreator;
            }

            public object CreateInstance(Type type)
            {
                return typeCreator(type);
            }
        }

        private readonly Dictionary<Type, List<Type>> serviceExecCache = new Dictionary<Type, List<Type>>();
        public void ResetServiceExecCachesIfNeeded(Type serviceType, Type requestType)
        {
            List<Type> requestTypes;
            if (!serviceExecCache.TryGetValue(serviceType, out requestTypes))
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
                    .MakeGenericMethod(requestType);

                mi.Invoke(null, new object[] { });

                requestTypes.Add(requestType);
            }
        }

        public void RegisterServiceExecutor(Type requestType, Type serviceType, ITypeFactory serviceFactoryFn)
        {
            ResetServiceExecCachesIfNeeded(serviceType, requestType);

            var serviceExecDef = typeof(ServiceRequestExec<,>).MakeGenericType(serviceType, requestType);
            var iserviceExec = (IServiceExec)serviceExecDef.CreateInstance();

            ServiceExecFn handlerFn = (requestContext, dto) =>
            {
                var service = serviceFactoryFn.CreateInstance(serviceType);

                ServiceExecFn serviceExec = (reqCtx, req) =>
                    iserviceExec.Execute(reqCtx, service, req);

                return ManagedServiceExec(serviceExec, (IService)service, requestContext, dto);
            };

            AddToRequestExecMap(requestType, serviceType, handlerFn);
        }

        private void AddToRequestExecMap(Type requestType, Type serviceType, ServiceExecFn handlerFn)
        {
            if (requestExecMap.ContainsKey(requestType))
            {
                throw new AmbiguousMatchException(
                    string.Format(
                    "Could not register Request '{0}' with service '{1}' as it has already been assigned to another service.\n"
                    + "Each Request DTO can only be handled by 1 service.",
                    requestType.FullName, serviceType.FullName));
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

        private object ManagedServiceExec(ServiceExecFn serviceExec, IService service, IRequest request, object requestDto)
        {
            try
            {
                InjectRequestContext(service, request);

                object response = null;
                try
                {
                    requestDto = request.Dto = appHost.OnPreExecuteServiceFilter(service, requestDto, request, request.Response);

                    //Executes the service and returns the result
                    response = serviceExec(request, requestDto);

                    response = appHost.OnPostExecuteServiceFilter(service, response, request, request.Response);

                    return response;
                }
                finally
                {
                    //Gets disposed by AppHost or ContainerAdapter if set
                    var taskResponse = response as Task;
                    if (taskResponse != null)
                    {
                        taskResponse.ContinueWith(task => appHost.Release(service));
                    }
                    else
                    {
                        appHost.Release(service);
                    }
                }
            }
            catch (TargetInvocationException tex)
            {
                //Mono invokes using reflection
                throw tex.InnerException ?? tex;
            }
        }

        internal static void InjectRequestContext(object service, IRequest requestContext)
        {
            if (requestContext == null) return;

            var serviceRequiresContext = service as IRequiresRequest;
            if (serviceRequiresContext != null)
            {
                serviceRequiresContext.Request = requestContext;
            }
        }

        /// <summary>
        /// Execute MQ
        /// </summary>
        public object ExecuteMessage(IMessage mqMessage)
        {
            return ExecuteMessage(mqMessage, new BasicRequest(mqMessage));
        }

        /// <summary>
        /// Execute MQ with requestContext
        /// </summary>
        public object ExecuteMessage(IMessage dto, IRequest req)
        {
            req.Dto = dto.Body;
            if (HostContext.ApplyMessageRequestFilters(req, req.Response, dto.Body))
                return req.Response.Dto;

            var response = Execute(dto.Body, req);

            var taskResponse = response as Task;
            if (taskResponse != null)
            {
                //Ensure messages are executed synchronously
                taskResponse.Wait();
                response = taskResponse.GetResult();
            }

            if (HostContext.ApplyMessageResponseFilters(req, req.Response, response))
                return req.Response.Dto;

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
        /// Execute HTTP
        /// </summary>
        public object Execute(object requestDto, IRequest req)
        {
            req.Dto = requestDto;
            var requestType = requestDto.GetType();
            req.OperationName = requestType.Name;

            if (appHost.Config.EnableAccessRestrictions)
                AssertServiceRestrictions(requestType, req.RequestAttributes);

            var handlerFn = GetService(requestType);
            return handlerFn(req, requestDto);
        }

        public object Execute(IRequest req)
        {
            string contentType;
            var restPath = RestHandler.FindMatchingRestPath(req.Verb, req.PathInfo, out contentType);
            req.SetRoute(restPath as RestPath);
            req.OperationName = restPath.RequestType.GetOperationName();
            var request = RestHandler.CreateRequest(req, restPath);
            req.Dto = request;

            if (appHost.ApplyRequestFilters(req, req.Response, request))
                return null;

            var response = Execute(request, req);

            if (appHost.ApplyResponseFilters(req, req.Response, response))
                return null;

            return response;
        }

        public Task<object> ExecuteAsync(object requestDto, IRequest req)
        {
            req.Dto = requestDto;
            var requestType = requestDto.GetType();

            if (appHost.Config.EnableAccessRestrictions)
            {
                AssertServiceRestrictions(requestType,
                    req != null ? req.RequestAttributes : RequestAttributes.None);
            }

            var handlerFn = GetService(requestType);
            var response = handlerFn(req, requestDto);

            var taskResponse = response as Task;
            if (taskResponse != null)
            {
                return taskResponse.ContinueWith(x => x.GetResult());
            }

            return response.AsTaskResult();
        }

        public ServiceExecFn GetService(Type requestType)
        {
            ServiceExecFn handlerFn;
            if (!requestExecMap.TryGetValue(requestType, out handlerFn))
            {
                if (requestType.IsArray)
                {
                    var elType = requestType.GetElementType();
                    if (requestExecMap.TryGetValue(elType, out handlerFn))
                    {
                        return (req, dtos) => 
                            from object dto in (IEnumerable)dtos 
                            select handlerFn(req, dto);
                    }
                }

                throw new NotImplementedException(string.Format("Unable to resolve service '{0}'", requestType.GetOperationName()));
            }

            return handlerFn;
        }

        public void AssertServiceRestrictions(Type requestType, RequestAttributes actualAttributes)
        {
            if (!appHost.Config.EnableAccessRestrictions) return;

            RestrictAttribute restrictAttr;
            var hasNoAccessRestrictions = !requestServiceAttrs.TryGetValue(requestType, out restrictAttr)
                || restrictAttr.HasNoAccessRestrictions;

            if (hasNoAccessRestrictions)
            {
                return;
            }

            var failedScenarios = new StringBuilder();
            foreach (var requiredScenario in restrictAttr.AccessibleToAny)
            {
                var allServiceRestrictionsMet = (requiredScenario & actualAttributes) == actualAttributes;
                if (allServiceRestrictionsMet)
                {
                    return;
                }

                var passed = requiredScenario & actualAttributes;
                var failed = requiredScenario & ~(passed);

                failedScenarios.AppendFormat("\n -[{0}]", failed);
            }

            var internalDebugMsg = (RequestAttributes.InternalNetworkAccess & actualAttributes) != 0
                ? "\n Unauthorized call was made from: " + actualAttributes
                : "";

            throw new UnauthorizedAccessException(
                string.Format("Could not execute service '{0}', The following restrictions were not met: '{1}'" + internalDebugMsg,
                    requestType.GetOperationName(), failedScenarios));
        }
    }

}