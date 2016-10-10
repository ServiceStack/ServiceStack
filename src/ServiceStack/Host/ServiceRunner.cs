using System;
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
        protected readonly IHasRequestFilter[] RequestFilters;
        protected readonly IHasResponseFilter[] ResponseFilters;

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
            var requiresContext = service as IRequiresRequest;
            if (requiresContext != null)
            {
                requiresContext.Request = requestContext;
            }
            return service;
        }

        public virtual void BeforeEachRequest(IRequest requestContext, TRequest request)
        {
            var requestLogger = AppHost.TryResolve<IRequestLogger>();
            if (requestLogger != null)
            {
                requestContext.SetItem("_requestDurationStopwatch", Stopwatch.StartNew());
            }
            
            OnBeforeExecute(requestContext, request);
        }

        public virtual object AfterEachRequest(IRequest requestContext, TRequest request, object response)
        {
            var requestLogger = AppHost.TryResolve<IRequestLogger>();
            if (requestLogger != null)
            {
                try
                {
                    var stopWatch = requestContext.GetItem("_requestDurationStopwatch") as Stopwatch;
                    if (stopWatch != null)
                    {
                        requestLogger.Log(requestContext, request, response, stopWatch.Elapsed);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error while logging request: " + request.Dump(), ex);
                }
            }

            //only call OnAfterExecute if no exception occured
            return response.IsErrorResponse() ? response : OnAfterExecute(requestContext, response);
        }

        public virtual void OnBeforeExecute(IRequest requestContext, TRequest request) { }

        public virtual object OnAfterExecute(IRequest requestContext, object response)
        {
            return response;
        }

        public virtual object Execute(IRequest request, object instance, TRequest requestDto)
        {
            try
            {
                BeforeEachRequest(request, requestDto);

                var container = HostContext.Container;

                if (RequestFilters != null)
                {
                    foreach (var requestFilter in RequestFilters)
                    {
                        var attrInstance = requestFilter.Copy();
                        container.AutoWire(attrInstance);
                        attrInstance.RequestFilter(request, request.Response, requestDto);
                        AppHost.Release(attrInstance);
                        if (request.Response.IsClosed) return null;
                    }
                }

                var response = AfterEachRequest(request, requestDto, ServiceAction(instance, requestDto));
                var error = response as IHttpError;
                if (error != null)
                {
                    var ex = (Exception) error;
                    var result = HandleException(request, requestDto, ex);

                    if (result == null)
                        throw ex;

                    return result;
                }

                var taskResponse = response as Task;
                if (taskResponse != null)
                {
                    if (taskResponse.Status == TaskStatus.Created)
                    {
                        taskResponse.Start();
                    }
                    return taskResponse.ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            var ex = task.Exception.UnwrapIfSingleException();

                            //Async Exception Handling
                            var result = HandleException(request, requestDto, ex);

                            if (result == null)
                                return ex;

                            return result;
                        }

                        response = task.GetResult();
                        if (ResponseFilters != null)
                        {
                            //Async Exec ResponseFilters
                            foreach (var responseFilter in ResponseFilters)
                            {
                                var attrInstance = responseFilter.Copy();
                                container.AutoWire(attrInstance);

                                attrInstance.ResponseFilter(request, request.Response, response);
                                AppHost.Release(attrInstance);

                                if (request.Response.IsClosed)
                                    return null;
                            }
                        }

                        return response;
                    });
                }

                //Sync Exec ResponseFilters
                if (ResponseFilters != null)
                {
                    foreach (var responseFilter in ResponseFilters)
                    {
                        var attrInstance = responseFilter.Copy();
                        container.AutoWire(attrInstance);

                        attrInstance.ResponseFilter(request, request.Response, response);
                        AppHost.Release(attrInstance);

                        if (request.Response.IsClosed) return null;
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                //Sync Exception Handling
                var result = HandleException(request, requestDto, ex);

                if (result == null) throw;

                return result;
            }
        }

        public virtual object Execute(IRequest requestContext, object instance, IMessage<TRequest> request)
        {
            return Execute(requestContext, instance, request.GetBody());
        }

        public virtual object HandleException(IRequest request, TRequest requestDto, Exception ex)
        {
            var errorResponse = HostContext.RaiseServiceException(request, requestDto, ex)
                ?? DtoUtils.CreateErrorResponse(requestDto, ex);

            AfterEachRequest(request, requestDto, errorResponse ?? ex);
            
            return errorResponse;
        }

        public object ExecuteOneWay(IRequest requestContext, object instance, TRequest request)
        {
            var msgFactory = AppHost.TryResolve<IMessageFactory>();
            if (msgFactory == null)
            {
                return Execute(requestContext, instance, request);
            }

            //Capture and persist this async request on this Services 'In Queue' 
            //for execution after this request has been completed
            using (var producer = msgFactory.CreateMessageProducer())
            {
                producer.Publish(request);
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
