﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class ServiceRunner<TRequest> : IServiceRunner<TRequest>
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(ServiceRunner<>));

        protected readonly IAppHost AppHost;
        protected readonly ActionContext ActionContext;
        protected readonly ActionInvokerFn ServiceAction;
        protected readonly IRequestFilterBase[] RequestFilters;
        protected readonly IResponseFilterBase[] ResponseFilters;

        public ServiceRunner(IAppHost appHost, ActionContext actionContext)
        {
            this.AppHost = appHost;
            this.ActionContext = actionContext;
            this.ServiceAction = actionContext.ServiceAction;

            if (actionContext.RequestFilters != null)
                this.RequestFilters = actionContext.RequestFilters.OrderBy(x => x.Priority).ToArray();

            if (actionContext.ResponseFilters != null)
                this.ResponseFilters = actionContext.ResponseFilters.OrderBy(x => x.Priority).ToArray();
        }

        public T ResolveService<T>(IRequest requestContext)
        {
            var service = AppHost.TryResolve<T>();
            if (service is IRequiresRequest requiresContext)
            {
                requiresContext.Request = requestContext;
            }
            return service;
        }

        public virtual void BeforeEachRequest(IRequest req, TRequest request, object service)
        {
            var requestLogger = AppHost.TryResolve<IRequestLogger>();
            if (requestLogger != null)
            {
                req.SetItem(Keywords.RequestDuration, Stopwatch.StartNew());
            }
            
            OnBeforeExecute(req, request, service);
        }

        public virtual object AfterEachRequest(IRequest req, TRequest request, object response, object service)
        {
            if (response.IsErrorResponse())
            {
                var autoBatchIndex = req.GetItem(Keywords.AutoBatchIndex)?.ToString();
                if (autoBatchIndex != null)
                {
                    var responseStatus = response.GetResponseStatus();
                    if (responseStatus != null)
                    {
                        if (responseStatus.Meta == null)
                            responseStatus.Meta = new Dictionary<string, string>();

                        responseStatus.Meta[Keywords.AutoBatchIndex] = autoBatchIndex;
                    }
                }
            }

            //only call OnAfterExecute if no exception occured
            return response.IsErrorResponse() ? response : OnAfterExecute(req, response, service);
        }

        [Obsolete("Use OnBeforeExecute(req, requestDto, service)")]
        public virtual void OnBeforeExecute(IRequest req, TRequest request) { }
        public void OnBeforeExecute(IRequest req, TRequest request, object service)
        {
            OnBeforeExecute(req, request);
            if (service is IServiceBeforeFilter filter)
                filter.OnBeforeExecute(request);
        }

        [Obsolete("Use OnAfterExecute(req, requestDto, service)")]
        public virtual object OnAfterExecute(IRequest req, object response) => response;

        public object OnAfterExecute(IRequest req, object response, object service)
        {
            response = OnAfterExecute(req, response);
            if (service is IServiceAfterFilter filter)
                return filter.OnAfterExecute(response);
            return response;
        }

        [Obsolete("Override ExecuteAsync instead")]
        public virtual object Execute(IRequest req, object instance, TRequest requestDto)
        {
            return ExecuteAsync(req, instance, requestDto);
        }

        public virtual async Task<object> ExecuteAsync(IRequest req, object instance, TRequest requestDto)
        {
            try
            {
                BeforeEachRequest(req, requestDto, instance);

                var res = req.Response;
                var container = HostContext.Container;

                if (RequestFilters != null)
                {
                    foreach (var requestFilter in RequestFilters)
                    {
                        var attrInstance = requestFilter.Copy();
                        container.AutoWire(attrInstance);

                        if (attrInstance is IHasRequestFilter filterSync)
                            filterSync.RequestFilter(req, res, requestDto);
                        else if (attrInstance is IHasRequestFilterAsync filterAsync)
                            await filterAsync.RequestFilterAsync(req, res, requestDto);

                        AppHost.Release(attrInstance);
                        if (res.IsClosed) 
                            return null;
                    }
                }

                var response = AfterEachRequest(req, requestDto, ServiceAction(instance, requestDto), instance);

                if (HostContext.StrictMode)
                {
                    if (response != null && response.GetType().IsValueType)
                        throw new StrictModeException(
                            $"'{requestDto.GetType().Name}' Service cannot return Value Types for its Service Responses. " +
                            $"You can embed its '{response.GetType().Name}' return value in a Response DTO or return as raw data in a string or byte[]",
                            StrictModeCodes.ReturnsValueType);
                }

                if (response is Task taskResponse)
                {
                    if (taskResponse.Status == TaskStatus.Created)
                        taskResponse.Start();

                    await taskResponse;
                    response = taskResponse.GetResult();
                }
                LogRequest(req, requestDto, response);

                if (response is IHttpError error)
                {
                    var ex = (Exception) error;
                    var result = await HandleExceptionAsync(req, requestDto, ex, instance);

                    if (result == null)
                        throw ex;

                    return result;
                }

                if (ResponseFilters != null)
                {
                    foreach (var responseFilter in ResponseFilters)
                    {
                        var attrInstance = responseFilter.Copy();
                        container.AutoWire(attrInstance);

                        if (attrInstance is IHasResponseFilter filter)
                            filter.ResponseFilter(req, res, response);
                        else if (attrInstance is IHasResponseFilterAsync filterAsync)
                            await filterAsync.ResponseFilterAsync(req, res, response);

                        AppHost.Release(attrInstance);

                        if (res.IsClosed)
                            return null;
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                //Sync Exception Handling
                var result = await HandleExceptionAsync(req, requestDto, ex, instance);

                if (result == null)
                    throw;

                return result;
            }
        }

        private void LogRequest(IRequest req, object requestDto, object response)
        {
            var requestLogger = AppHost.TryResolve<IRequestLogger>();
            if (requestLogger != null 
                && !req.IsInProcessRequest()
                && !req.Items.ContainsKey(Keywords.HasLogged))
            {
                try
                {
                    req.Items[Keywords.HasLogged] = true;
                    var stopWatch = req.GetItem(Keywords.RequestDuration) as Stopwatch;
                    requestLogger.Log(req, requestDto, response, stopWatch?.Elapsed ?? TimeSpan.Zero);
                }
                catch (Exception ex)
                {
                    Log.ErrorStrict("Error while logging req: " + req.Dump(), ex);
                }
            }
        }

        public virtual object Execute(IRequest req, object instance, IMessage<TRequest> request)
        {
            var task = ExecuteAsync(req, instance, request.GetBody());
            return task.Result;
        }

        [Obsolete("Use HandleExceptionAsync(req, requestDto, ex, service)")]
        public virtual Task<object> HandleExceptionAsync(IRequest request, TRequest requestDto, Exception ex) => 
            TypeConstants.EmptyTask;

        public virtual async Task<object> HandleExceptionAsync(IRequest request, TRequest requestDto, Exception ex, object service)
        {
            var errorResponse = (service is IServiceErrorFilter filter ? await filter.OnExceptionAsync(requestDto, ex) : null)
                ?? await HandleExceptionAsync(request, requestDto, ex)
                ?? await HostContext.RaiseServiceException(request, requestDto, ex)
                ?? DtoUtils.CreateErrorResponse(requestDto, ex);

            AfterEachRequest(request, requestDto, errorResponse ?? ex, service);
            
            return errorResponse;
        }

        public object ExecuteOneWay(IRequest requestContext, object instance, TRequest request)
        {
            var msgFactory = AppHost.TryResolve<IMessageFactory>();
            if (msgFactory == null)
            {
                var task = ExecuteAsync(requestContext, instance, request);
                return task.Result;
            }

            //Capture and persist this async req on this Services 'In Queue' 
            //for execution after this req has been completed
            using (var producer = msgFactory.CreateMessageProducer())
            {
                AppHost.PublishMessage(producer, request);
            }

            return WebRequestUtils.GetErrorResponseDtoType(request).CreateInstance();
        }

        //signature matches ServiceExecFn
        public object Process(IRequest requestContext, object instance, object request)
        {
            return requestContext != null && requestContext.RequestAttributes.Has(RequestAttributes.OneWay) 
                ? ExecuteOneWay(requestContext, instance, (TRequest)request) 
                : Execute(requestContext, instance, (TRequest)request);
        }
    }

}
