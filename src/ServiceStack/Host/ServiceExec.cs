//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public interface IServiceExec
    {
        object Execute(IRequest requestContext, object instance, object request);
    }

    public class ServiceRequestExec<TService, TRequest> : IServiceExec
    {
        public object Execute(IRequest requestContext, object instance, object request)
        {
            return ServiceExec<TService>.Execute(requestContext, instance, request,
                typeof(TRequest).GetOperationName());
        }
    }

    public class ActionMethod
    {
        public const string Async = nameof(Async);
        public MethodInfo MethodInfo { get; }
        public bool IsAsync { get; }
        public string Name { get; }
        public ActionMethod(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo;
            IsAsync = methodInfo.Name.EndsWith(Async);
            Name = IsAsync
                ? methodInfo.Name.Substring(0, methodInfo.Name.Length - Async.Length)
                : methodInfo.Name;
        }
        
        public ParameterInfo[] GetParameters() => MethodInfo.GetParameters();
        public bool IsGenericMethod => MethodInfo.IsGenericMethod;
        public Type ReturnType => MethodInfo.ReturnType;
        public object[] GetCustomAttributes(bool inherit) => MethodInfo.GetCustomAttributes(inherit);

        public object[] AllAttributes() => MethodInfo.AllAttributes();
        public T[] AllAttributes<T>() => MethodInfo.AllAttributes<T>();
    }

    public static class ServiceExecExtensions
    {
        public static List<ActionMethod> GetActions(this Type serviceType)
        {
            var to = new List<ActionMethod>();
            
            foreach (var mi in serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var actionMethod = new ActionMethod(mi);
                if (!ServiceController.IsServiceAction(actionMethod)) 
                    continue;

                to.Add(actionMethod);
            }

            // Remove all sync methods where async equivalents exist & have async methods masquerades as sync methods for cheaper runtime invokation  
            var asyncActions = new HashSet<string>(to.Where(x => x.IsAsync).Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
            if (asyncActions.Count > 0)
                to.RemoveAll(x => asyncActions.Contains(x.Name) && !x.IsAsync);

            return to;
        }
    }

    internal class ServiceExec<TService>
    {
        private static Dictionary<Type, List<ActionContext>> actionMap;

        private static Dictionary<string, InstanceExecFn> execMap;

        public static void Reset()
        {
            actionMap = new Dictionary<Type, List<ActionContext>>();
            execMap = new Dictionary<string, InstanceExecFn>();

            foreach (var mi in typeof(TService).GetActions())
            {
                var actionName = mi.Name.ToUpper();
                var args = mi.GetParameters();

                var requestType = args[0].ParameterType;
                var actionCtx = new ActionContext {
                    Id = ActionContext.Key(actionName, requestType.GetOperationName()),
                    ServiceType = typeof(TService),
                    RequestType = requestType,
                    ServiceAction = CreateExecFn(requestType, mi.MethodInfo),
                };


                var reqFilters = new List<IRequestFilterBase>();
                var resFilters = new List<IResponseFilterBase>();

                foreach (var attr in mi.MethodInfo.GetCustomAttributes(true))
                {
                    var hasReqFilter = attr as IRequestFilterBase;
                    var hasResFilter = attr as IResponseFilterBase;

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

        private static ActionInvokerFn CreateExecFn(Type requestType, MethodInfo mi)
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
                if (mi.ReturnType.IsValueType)
                    callExecute = Expression.Convert(callExecute, typeof(object));
                    
                var executeFunc = Expression.Lambda<ActionInvokerFn>
                    (callExecute, serviceParam, requestDtoParam).Compile();

                return executeFunc;
            }
            else
            {
                var executeFunc = Expression.Lambda<VoidActionInvokerFn>
                (callExecute, serviceParam, requestDtoParam).Compile();

                return (service, request) =>
                {
                    executeFunc(service, request);
                    return null;
                };
            }
        }

        private static IEnumerable<ActionContext> GetActionsFor<TRequest>()
        {
            return actionMap.TryGetValue(typeof(TRequest), out List<ActionContext> requestActions)
                ? requestActions
                : new List<ActionContext>();
        }

        public static void CreateServiceRunnersFor<TRequest>() //used in ServiceController
        {
            foreach (var actionCtx in GetActionsFor<TRequest>())
            {
                if (execMap.ContainsKey(actionCtx.Id)) 
                    continue;

                var serviceRunner = HostContext.CreateServiceRunner<TRequest>(actionCtx);
                execMap[actionCtx.Id] = serviceRunner.Process;
            }
        }

        public static object Execute(IRequest request, object instance, object requestDto, string requestName)
        {
            var actionName = request.Verb 
                ?? HttpMethods.Post; //MQ Services

            if (request.GetItem(Keywords.InvokeVerb) is string overrideVerb)
                actionName = overrideVerb;

            var format = request.ResponseContentType.ToContentFormat()?.ToUpper();

            if (execMap.TryGetValue(ActionContext.Key(actionName + format, requestName), out var action) ||
                execMap.TryGetValue(ActionContext.AnyFormatKey(format, requestName), out action) ||
                execMap.TryGetValue(ActionContext.Key(actionName, requestName), out action) ||
                execMap.TryGetValue(ActionContext.AnyKey(requestName), out action))
            {
                return action(request, instance, requestDto);
            }

            var expectedMethodName = actionName.Substring(0, 1) + actionName.Substring(1).ToLowerInvariant();
            throw new NotImplementedException(
                $"Could not find method named {expectedMethodName}({requestDto.GetType().GetOperationName()}) " +
                $"or Any({requestDto.GetType().GetOperationName()}) on Service {typeof(TService).GetOperationName()}");
        }
    }
}