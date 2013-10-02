using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Common.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceHost
{
    public interface INServiceExec
    {
        object Execute(IRequestContext requestContext, object instance, object request);
    }

    public class NServiceRequestExec<TService, TRequest> : INServiceExec
    {
        static NServiceRequestExec()
        {
            try
            {
                NServiceExec<TService>.CreateServiceRunnersFor<TRequest>();
            }
            catch (Exception ex)
            {
                ex.Message.Print();
                throw;
            }
        }

        public object Execute(IRequestContext requestContext, object instance, object request)
        {
            return NServiceExec<TService>.Execute(requestContext, instance, request,
                typeof(TRequest).Name);
        }
    }

    public static class NServiceExecExtensions
    {
        public static IEnumerable<MethodInfo> GetActions(this Type serviceType)
        {
            foreach (var mi in serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (mi.GetParameters().Length != 1)
                    continue;

                var actionName = mi.Name.ToUpper();
                if (!HttpMethods.AllVerbs.Contains(actionName) && actionName != ActionContext.AnyAction)
                    continue;

                yield return mi;
            }
        }
    }

    public class NServiceExec<TService>
    {
        private static Dictionary<Type, List<ActionContext>> actionMap
            = new Dictionary<Type, List<ActionContext>>();

        private static Dictionary<string, InstanceExecFn> execMap 
            = new Dictionary<string, InstanceExecFn>();

        static NServiceExec()
        {
            foreach (var mi in typeof(TService).GetActions())
            {
                var actionName = mi.Name.ToUpper();
                var args = mi.GetParameters();

                var requestType = args[0].ParameterType;
                var actionCtx = new ActionContext {
                    Id = ActionContext.Key(actionName, requestType.Name),
                    ServiceType = typeof(TService),
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

                var serviceRunner = EndpointHost.CreateServiceRunner<TRequest>(actionCtx);
                execMap[actionCtx.Id] = serviceRunner.Process;
            }
        }

        public static object Execute(IRequestContext requestContext,
                                     object instance, object request, string requestName)
        {
            var actionName = requestContext != null
                ? requestContext.Get<IHttpRequest>().HttpMethod
                : HttpMethods.Post; //MQ Services

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
}