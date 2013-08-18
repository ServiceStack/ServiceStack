using System;
using System.Diagnostics;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceHost
{
    public class ServiceRunner<TRequest> : IServiceRunner<TRequest>
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(ServiceRunner<>));

        protected readonly IAppHost AppHost;
        protected readonly ActionInvokerFn ServiceAction;
        protected readonly IHasRequestFilter[] RequestFilters;
        protected readonly IHasResponseFilter[] ResponseFilters;

        public ServiceRunner() { }

        public ServiceRunner(IAppHost appHost, ActionContext actionContext)
        {
            this.AppHost = appHost;
            this.ServiceAction = actionContext.ServiceAction;
            this.RequestFilters = actionContext.RequestFilters;
            this.ResponseFilters = actionContext.ResponseFilters;
        }

        public IAppHost GetAppHost()
        {
            return AppHost ?? EndpointHost.AppHost;
        }

        public T TryResolve<T>()
        {
            return this.GetAppHost() == null
                ? default(T)
                : this.GetAppHost().TryResolve<T>();
        }

        public T ResolveService<T>(IRequestContext requestContext)
        {
            var service = this.GetAppHost().TryResolve<T>();
            var requiresContext = service as IRequiresRequestContext;
            if (requiresContext != null)
            {
                requiresContext.RequestContext = requestContext;
            }
            return service;
        }

        public virtual void BeforeEachRequest(IRequestContext requestContext, TRequest request)
        {
            OnBeforeExecute(requestContext, request);

            var requestLogger = TryResolve<IRequestLogger>();
            if (requestLogger != null)
            {
                requestContext.SetItem("_requestDurationStopwatch", Stopwatch.StartNew());
            }
        }

        public virtual object AfterEachRequest(IRequestContext requestContext, TRequest request, object response)
        {
            var requestLogger = TryResolve<IRequestLogger>();
            if (requestLogger != null)
            {
                try
                {
                    var stopWatch = (Stopwatch)requestContext.GetItem("_requestDurationStopwatch");
                    requestLogger.Log(requestContext, request, response, stopWatch.Elapsed);
                }
                catch (Exception ex)
                {
                    Log.Error("Error while logging request: " + request.Dump(), ex);
                }
            }

            //only call OnAfterExecute if no exception occured
            return response.IsErrorResponse() ? response : OnAfterExecute(requestContext, response);
        }

        public virtual void OnBeforeExecute(IRequestContext requestContext, TRequest request) { }

        public virtual object OnAfterExecute(IRequestContext requestContext, object response)
        {
            return response;
        }

        public virtual object Execute(IRequestContext requestContext, object instance, TRequest request)
        {
            try
            {
                BeforeEachRequest(requestContext, request);

                var appHost = GetAppHost();
                var container = appHost != null ? appHost.Config.ServiceManager.Container : null;
                var httpReq = requestContext != null ? requestContext.Get<IHttpRequest>() : null;
                var httpRes = requestContext != null ? requestContext.Get<IHttpResponse>() : null;

                if (RequestFilters != null)
                {
                    foreach (var requestFilter in RequestFilters)
                    {
                        var attrInstance = requestFilter.Copy();
                        if (container != null)
                            container.AutoWire(attrInstance);
                        attrInstance.RequestFilter(httpReq, httpRes, request);
                        if (appHost != null)
                            appHost.Release(attrInstance);
                        if (httpRes != null && httpRes.IsClosed) return null;
                    }
                }

                var response = AfterEachRequest(requestContext, request, ServiceAction(instance, request));

                if (ResponseFilters != null)
                {
                    foreach (var responseFilter in ResponseFilters)
                    {
                        var attrInstance = responseFilter.Copy();
                        if (container != null)
                            container.AutoWire(attrInstance);
                        attrInstance.ResponseFilter(httpReq, httpRes, response);
                        if (appHost != null)
                            appHost.Release(attrInstance);
                        if (httpRes != null && httpRes.IsClosed) return null;
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                var result = HandleException(requestContext, request, ex);

                if (result == null) throw;

                return result;
            }
        }

        public virtual object Execute(IRequestContext requestContext, object instance, IMessage<TRequest> request)
        {
            return Execute(requestContext, instance, request.GetBody());
        }

        public virtual object HandleException(IRequestContext requestContext, TRequest request, Exception ex)
        {
            var useAppHost = GetAppHost();

            object errorResponse = null;

            if (useAppHost != null && useAppHost.ServiceExceptionHandler != null)
                errorResponse = useAppHost.ServiceExceptionHandler(request, ex);

            if (errorResponse == null)
                errorResponse = DtoUtils.HandleException(useAppHost, request, ex);

            AfterEachRequest(requestContext, request, errorResponse ?? ex);
            
            return errorResponse;
        }

        public object ExecuteOneWay(IRequestContext requestContext, object instance, TRequest request)
        {
            var msgFactory = TryResolve<IMessageFactory>();
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
        public object Process(IRequestContext requestContext, object instance, object request)
        {
            return requestContext != null && requestContext.EndpointAttributes.Has(EndpointAttributes.OneWay) 
                ? ExecuteOneWay(requestContext, instance, (TRequest)request) 
                : Execute(requestContext, instance, (TRequest)request);
        }
    }

}