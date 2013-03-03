using System;
using System.Diagnostics;
using ServiceStack.CacheAccess;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// A Useful ServiceBase for all services with support for automatically serializing
    /// Exceptions into a common ResponseError DTO so errors can be handled generically by clients. 
    /// 
    /// If an 'IRedisClientsManager' is configured in your AppHost, service errors will
    /// also be maintained into a service specific and combined rolling error log.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    [Obsolete("Use the New API (ServiceStack.ServiceInterface.Service) for future services. See: https://github.com/ServiceStack/ServiceStack/wiki/New-Api")]
    public abstract class ServiceBase<TRequest>
        : IService<TRequest>, IRequiresRequestContext, IServiceBase, IAsyncService<TRequest>, IRestOptionsService<TRequest>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceBase<>));

        /// <summary>
        /// Stopwatch used to record the duration of each request
        /// </summary>
        private Stopwatch requestDurationStopwatch;
        
        /// <summary>
        /// Can be overriden to supply Custom 'ServiceName' error logs
        /// </summary>
        public virtual string ServiceName
        {
            get { return typeof(TRequest).Name; }
        }

        /// <summary>
        /// Access to the Applications ServiceStack AppHost Instance
        /// </summary>
        /// 
        private IResolver resolver; //not property to stop alt IOC's creating new instances of AppHost
        public IResolver GetResolver()
        {
            return resolver ?? EndpointHost.AppHost;
        }

        private HandleServiceExceptionDelegate serviceExceptionHandler;
        public HandleServiceExceptionDelegate ServiceExceptionHandler
        {
            get { return serviceExceptionHandler ?? (GetResolver() is IAppHost ? ((IAppHost)GetResolver()).ServiceExceptionHandler : null); }
            set { serviceExceptionHandler = value; }
        }

        [Obsolete("Use SetResolver")]
        public ServiceBase<TRequest> SetAppHost(IAppHost appHost)
        {
            return SetResolver(appHost);
        }

        public ServiceBase<TRequest> SetResolver(IResolver appHost) //Allow chaining
        {
            this.resolver = appHost;
            return this;
        }

        /// <summary>
        /// Endpoint-agnostic Request Context
        /// </summary>
        public IRequestContext RequestContext { get; set; }

        /// <summary>
        /// The Http Request (for HTTP/S only)
        /// </summary>
        public IHttpRequest Request
        {
            get { return RequestContext.Get<IHttpRequest>(); }
        }

        /// <summary>
        /// The HTTP Response (for HTTP/S only)
        /// </summary>
        public IHttpResponse Response
        {
            get { return RequestContext.Get<IHttpResponse>(); }
        }

        public ISessionFactory SessionFactory { get; set; }

        /// <summary>
        /// Logs each request
        /// </summary>
        public IRequestLogger RequestLogger { get; set; }

        /// <summary>
        /// Easy way to log all requests
        /// </summary>
        /// <param name="requestDto"></param>
        protected void BeforeEachRequest(TRequest requestDto)
        {
            this.CurrentRequestDto = requestDto;
            OnBeforeExecute(requestDto);

            if (this.RequestLogger != null)
            {
                requestDurationStopwatch = Stopwatch.StartNew();
            }
        }

        /// <summary>
        /// Filter called after each request. Lets you change the response type
        /// </summary>
        /// <param name="requestDto"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        protected object AfterEachRequest(TRequest requestDto, object response)
        {
            if (this.RequestLogger != null)
            {
                try
                {
                    RequestLogger.Log(this.RequestContext, requestDto, response, requestDurationStopwatch.Elapsed);
                }
                catch (Exception ex)
                {
                    Log.Error("Error while logging request: " + requestDto.Dump(), ex);
                }
            }

            return response.IsErrorResponse() ? response : OnAfterExecute(response); //only call OnAfterExecute if no exception occurred
        }

        /// <summary>
        /// Dynamic Session Bag
        /// </summary>
        private ISession session;
        public ISession Session
        {
            get
            {
                if (SessionFactory == null)
                    SessionFactory = new SessionFactory(this.GetCacheClient());

                return session ?? (session = SessionFactory.GetOrCreateSession(Request, Response));
            }
        }

        /// <summary>
        /// Typed UserSession
        /// </summary>
        private object userSession;
        protected TUserSession SessionAs<TUserSession>()
        {
            return (TUserSession)(userSession ?? (userSession = this.GetCacheClient().SessionAs<TUserSession>(Request, Response)));
        }

        /// <summary>
        /// Resolve an alternate Web Service from ServiceStack's IOC container.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ResolveService<T>()
        {
            var service = this.GetResolver().TryResolve<T>();
            var requiresContext = service as IRequiresRequestContext;
            if (requiresContext != null)
            {
                requiresContext.RequestContext = this.RequestContext;
            }
            return service;
        }

        /// <summary>
        /// Maintains the current request DTO in this property
        /// </summary>
        protected TRequest CurrentRequestDto;

        /// <summary>
        /// Resolve a dependency from the AppHost's IOC
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T TryResolve<T>()
        {
            return this.GetResolver() == null
                ? default(T)
                : this.GetResolver().TryResolve<T>();
        }

        /// <summary>
        /// Single method sub classes should implement to execute the request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract object Run(TRequest request);

        /// <summary>
        /// Called before the request is Executed. Override to enforce generic validation logic.
        /// </summary>
        /// <param name="request"></param>
        protected virtual void OnBeforeExecute(TRequest request) { }
        
        /// <summary>
        /// Called after the request is Executed. Override to decorate the response dto.
        /// This method is only called if no exception occurred while executing the service.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        protected virtual object OnAfterExecute(object response)
        {
            return response;
        }

        public virtual object Options(TRequest request)
        {
            return null; //NoOp to let OPTIONS requests through
        }
        
        /// <summary>
        /// Execute the request with the protected abstract Run() method in a managed scope by
        /// provide default handling of Service Exceptions by serializing exceptions in the response
        /// DTO and maintaining all service errors in a managed service-specific and combined rolling error logs
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public object Execute(TRequest request)
        {
            try
            {
                BeforeEachRequest(request);
                return AfterEachRequest(request, Run(request));
            }
            catch (Exception ex)
            {
                var result = HandleException(request, ex);

                if (result == null) throw;

                return result;
            }
        }

        /// <summary>
        /// Override the built-in Exception handling
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual object HandleException(TRequest request, Exception ex)
        {
            var errorResponse = ServiceExceptionHandler != null
                ? ServiceExceptionHandler(request, ex)
                : DtoUtils.HandleException(GetResolver(), request, ex);

            AfterEachRequest(request, errorResponse ?? ex);
            
            return errorResponse;
        }
        
        /// <summary>
        /// Return a custom view with this response.
        /// Not required for views named after the Response or Request DTO name - which are automatically resolved.
        /// </summary>
        protected HttpResult View(object response, string viewName, string templateName=null)
        {
            return new HttpResult(response)
            {
                View = viewName,
                Template = templateName,
            };
        }

        /// <summary>
        /// The Deferred execution of ExecuteAsync(request)'s. 
        /// This request is typically invoked from a messaging queue service host.
        /// </summary>
        /// <param name="request"></param>
        public virtual object Execute(IMessage<TRequest> request)
        {
            return Execute(request.GetBody());
        }

        /// <summary>
        /// Injected by the ServiceStack IOC with the registered dependency in the Funq IOC container.
        /// </summary>
        public IMessageFactory MessageFactory { get; set; }

        /// <summary>
        /// Persists the request into the registered message queue if configured, 
        /// otherwise calls Execute() to handle the request immediately.
        /// 
        /// IAsyncService.ExecuteAsync() will be used instead of IService.Execute() for 
        /// EndpointAttributes.AsyncOneWay requests
        /// </summary>
        /// <param name="request"></param>
        public virtual object ExecuteAsync(TRequest request)
        {
            if (MessageFactory == null)
            {
                return Execute(request);
            }

            //Capture and persist this async request on this Services 'In Queue' 
            //for execution after this request has been completed
            using (var producer = MessageFactory.CreateMessageProducer())
            {
                producer.Publish(request);
            }

            return WebRequestUtils.GetErrorResponseDtoType(request).CreateInstance();
        }
    }
}
