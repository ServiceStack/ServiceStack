using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Common.Web;
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

    public class IServiceExec<TService, TRequest> : ICanServiceExec
    {
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
                if (args[0].ParameterType != typeof(TRequest)) continue;
                if (!HttpHeaders.AllVerbs.Contains(mi.Name.ToUpper()))
                    continue;

                var actionCtx = new ActionContext {
                    //TODO make faster
                    ServiceAction = (service, request) =>
                        mi.Invoke(service, new[] { request }),
                };

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

                var serviceRunner = EndpointHost.AppHost.CreateServiceRunner<TRequest>(actionCtx);

                var key = mi.Name.ToLower();
                if (execMap.ContainsKey(key))
                {
                    throw new AmbiguousMatchException(
                        string.Format(
                        "Could not register the service '{0}' as another action with '{1}({2})' already exists.",
                        typeof(TService).FullName, mi.Name, typeof(TRequest).Name));
                }

                execMap[key] = serviceRunner.Process;
            }
        }

        public object Execute(IRequestContext requestContext, object instance, object request)
        {
            var key = requestContext.Get<IHttpRequest>().HttpMethod.ToLower();

            InstanceExecFn action;
            if (execMap.TryGetValue(key, out action)
                || execMap.TryGetValue("any", out action))
            {
                return action(requestContext, instance, request);
            }

            var expectedMethodName = key.Substring(0, 1).ToUpper() + key.Substring(1);
            throw new MissingMethodException(
                "Could not find method named {1}({0}) or Any({0}) on Service {2}"
                .Fmt(expectedMethodName, typeof(TRequest).Name, typeof(TService).Name));
        }
    }
}