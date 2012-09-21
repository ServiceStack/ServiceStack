using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Common.Web;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceHost
{
    public class ServiceExec
    {
        public const string Execute = "Execute";
        public const string ExecuteAsync = "ExecuteAsync";

        public const string RestGet = "Get";
        public const string RestPost = "Post";
        public const string RestPut = "Put";
        public const string RestDelete = "Delete";
        public const string RestPatch = "Patch";


        private static readonly Dictionary<Type, MethodInfo> ServiceExecCache = new Dictionary<Type, MethodInfo>();

        public static MethodInfo GetExecMethodInfo(Type serviceType, Type requestType)
        {
            MethodInfo mi;
            lock (ServiceExecCache)
            {
                if (!ServiceExecCache.TryGetValue(requestType /*serviceType */, out mi))
                {
                    var genericType = typeof(ServiceExec<>).MakeGenericType(requestType);

                    mi = genericType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);

                    ServiceExecCache.Add(requestType /* serviceType */, mi);
                }
            }

            return mi;
        }

        public static MethodInfo GetRunTimeExecMethod(Type serviceType, Type requestType, EndpointAttributes attrs)
        {
            if ((attrs & EndpointAttributes.AsyncOneWay) == EndpointAttributes.AsyncOneWay)
            {
                var mi = serviceType.GetMethod(ExecuteAsync, new[] { requestType });
                if (mi != null) return mi;
            }
            else if ((attrs & EndpointAttributes.HttpGet) == EndpointAttributes.HttpGet)
            {
                var mi = serviceType.GetMethod(RestGet, new[] { requestType });
                if (mi != null) return mi;
            }
            else if ((attrs & EndpointAttributes.HttpPost) == EndpointAttributes.HttpPost)
            {
                var mi = serviceType.GetMethod(RestPost, new[] { requestType });
                if (mi != null) return mi;
            }
            else if ((attrs & EndpointAttributes.HttpPut) == EndpointAttributes.HttpPut)
            {
                var mi = serviceType.GetMethod(RestPut, new[] { requestType });
                if (mi != null) return mi;
            }
            else if ((attrs & EndpointAttributes.HttpDelete) == EndpointAttributes.HttpDelete)
            {
                var mi = serviceType.GetMethod(RestDelete, new[] { requestType });
                if (mi != null) return mi;
            }
            else if ((attrs & EndpointAttributes.HttpPatch) == EndpointAttributes.HttpPatch)
            {
                var mi = serviceType.GetMethod(RestPatch, new[] { requestType });
                if (mi != null) return mi;
            }
            return serviceType.GetMethod(Execute, new[] { requestType });
        }
    }

    public class ServiceExec<TReq>
    {
        public const string ExecuteMethodName = "Execute";

        public static object Execute(IService<TReq> service, TReq request, EndpointAttributes attrs)
        {
            if ((attrs & EndpointAttributes.AsyncOneWay) == EndpointAttributes.AsyncOneWay)
            {
                var asyncService = service as IAsyncService<TReq>;
                if (asyncService != null) return asyncService.ExecuteAsync(request);
            }
            else if ((attrs & EndpointAttributes.HttpGet) == EndpointAttributes.HttpGet)
            {
                var restService = service as IRestGetService<TReq>;
                if (restService != null) return restService.Get(request);
            }
            else if ((attrs & EndpointAttributes.HttpPost) == EndpointAttributes.HttpPost)
            {
                var restService = service as IRestPostService<TReq>;
                if (restService != null) return restService.Post(request);
            }
            else if ((attrs & EndpointAttributes.HttpPut) == EndpointAttributes.HttpPut)
            {
                var restService = service as IRestPutService<TReq>;
                if (restService != null) return restService.Put(request);
            }
            else if ((attrs & EndpointAttributes.HttpDelete) == EndpointAttributes.HttpDelete)
            {
                var restService = service as IRestDeleteService<TReq>;
                if (restService != null) return restService.Delete(request);
            }
            else if ((attrs & EndpointAttributes.HttpPatch) == EndpointAttributes.HttpPatch)
            {
                var restService = service as IRestPatchService<TReq>;
                if (restService != null) return restService.Patch(request);
            }
            return service.Execute(request);
        }
    }

    public interface ICanServiceExec
    {
        object Execute(IRequestContext requestContext, object instance, object request);
    }

    public class IServiceExec<TService>
    {
        private static Dictionary<Type, List<ActionContext>> actionMap
            = new Dictionary<Type, List<ActionContext>>();

        private static Dictionary<string, InstanceExecFn> execMap 
            = new Dictionary<string, InstanceExecFn>();
        
        static IServiceExec()
        {
            var mis = typeof(TService).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var methodInfo in mis)
            {
                var mi = methodInfo;
                if (mi.ReturnType != typeof(object) && mi.ReturnType != typeof(void)) continue;
                var args = mi.GetParameters();
                if (args.Length != 1) continue;
                var actionName = mi.Name.ToUpper();
                if (!HttpMethod.AllVerbs.Contains(actionName) && actionName != ActionContext.AnyAction)
                    continue;

                var requestType = args[0].ParameterType;
                var actionCtx = new ActionContext {
                    Id = ActionContext.Key(actionName, requestType.Name),
                    RequestType = requestType,
                };
                
                try
                {
                    actionCtx.ServiceAction = CreateExecFn(requestType, mi);
                }
                catch
                {
                    //Potential problems with MONO, using reflection for fallback
                    actionCtx.ServiceAction = (service, request) =>
                        mi.Invoke(service, new[] { request });
                }
                
                var reqFilters = new List<IHasRequestFilter>();
                var resFilters = new List<IHasResponseFilter>();

                foreach (var attr in mi.GetCustomAttributes(false))
                {
                    var hasReqFilter = attr as IHasRequestFilter;
                    var hasResFilter = attr as IHasResponseFilter;

                    if (hasReqFilter != null)
                        reqFilters.Add(hasReqFilter);

                    if (hasResFilter != null)
                        resFilters.Add(hasResFilter);
                }

                if (reqFilters.Count > 0)
                    actionCtx.RequestFilters = reqFilters.ToArray();

                if (resFilters.Count > 0)
                    actionCtx.ResponseFilters = resFilters.ToArray();

                if (!actionMap.ContainsKey(requestType))
                    actionMap[requestType] = new List<ActionContext>();

                actionMap[requestType].Add(actionCtx);
            }
        }

        public static ActionInvokerFn CreateExecFn(Type requestType, MethodInfo mi)
        {
            var serviceType = typeof(TService);

            var serviceParam = Expression.Parameter(typeof(object), "serviceObj");
            var serviceStrong = Expression.Convert(serviceParam, serviceType);

            var requestDtoParam = Expression.Parameter(typeof(object), "requestDto");
            var requestDtoStrong = Expression.Convert(requestDtoParam, requestType);

            Expression callExecute = Expression.Call(
                serviceStrong, mi, requestDtoStrong);

            if (mi.ReturnType != typeof(void))
            {
                var executeFunc = Expression.Lambda<ActionInvokerFn>
                    (callExecute, serviceParam, requestDtoParam).Compile();

                return executeFunc;
            }
            else
            {
                var executeFunc = Expression.Lambda<VoidActionInvokerFn>
                    (callExecute, serviceParam, requestDtoParam).Compile();

                return (service, request) => {
                    executeFunc(service, request);
                    return null;
                };
            }
        }

        public static List<ActionContext> GetActionsFor<TRequest>()
        {
            List<ActionContext> requestActions;
            return actionMap.TryGetValue(typeof(TRequest), out requestActions)
                ? requestActions
                : new List<ActionContext>();
        }

        public static void CreateServiceRunnersFor<TRequest>()
        {
            foreach (var actionCtx in GetActionsFor<TRequest>())
            {
                if (execMap.ContainsKey(actionCtx.Id)) continue;

                var serviceRunner = EndpointHost.AppHost.CreateServiceRunner<TRequest>(actionCtx);
                execMap[actionCtx.Id] = serviceRunner.Process;
            }
        }

        public static object Execute(IRequestContext requestContext, 
            object instance, object request, string requestName)
        {
            var actionName = requestContext.Get<IHttpRequest>().HttpMethod;

            InstanceExecFn action;
            if (execMap.TryGetValue(ActionContext.Key(actionName, requestName), out action)
                || execMap.TryGetValue(ActionContext.AnyKey(requestName), out action))
            {
                return action(requestContext, instance, request);
            }

            var expectedMethodName = actionName.Substring(0, 1) + actionName.Substring(1).ToLower();
            throw new NotImplementedException(
                "Could not find method named {1}({0}) or Any({0}) on Service {2}"
                .Fmt(request.GetType().Name, expectedMethodName, typeof(TService).Name));
        }
    }

    public class IServiceRequestExec<TService, TRequest> : ICanServiceExec
    {
        static IServiceRequestExec()
        {
            IServiceExec<TService>.CreateServiceRunnersFor<TRequest>();
        }

        public object Execute(IRequestContext requestContext, object instance, object request)
        {
            return IServiceExec<TService>.Execute(requestContext, instance, request, 
                typeof(TRequest).Name);
        }
    }
}