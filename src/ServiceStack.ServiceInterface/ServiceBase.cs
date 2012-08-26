using System;
using System.Diagnostics;
using ServiceStack.CacheAccess;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
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
    public abstract class ServiceBase<TRequest>
        : IService<TRequest>, IRequiresRequestContext, IServiceBase, IAsyncService<TRequest>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceBase<>));

        /// <summary>
        /// Stopwatch used to record the duration of each request
        /// </summary>
        private Stopwatch _requestDurationStopwatch;

        /// <summary>
        /// Service error logs are kept in 'urn:ServiceErrors:{ServiceName}'
        /// </summary>
        public const string UrnServiceErrorType = "ServiceErrors";

        /// <summary>
        /// Combined service error logs are maintained in 'urn:ServiceErrors:All'
        /// </summary>
        public const string CombinedServiceLogId = "All";
        
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
        private IAppHost appHost; //not property to stop alt IOC's creating new instances of AppHost
        
        public IAppHost GetAppHost()
        {
            return appHost ?? EndpointHost.AppHost;
        }

        public ServiceBase<TRequest> SetAppHost(IAppHost appHost) //Allow chaining
        {
            this.appHost = appHost;
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
                _requestDurationStopwatch = Stopwatch.StartNew();
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
                    RequestLogger.Log(this.RequestContext, requestDto, response, _requestDurationStopwatch.Elapsed);
                }
                catch (Exception ex)
                {
                    Log.Error("Error while logging request: " + requestDto.Dump(), ex);
                }
            }

            return response.IsErrorResponse() ? response : OnAfterExecute(response); //only call OnAfterExecute if no exception occured
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

                return session ?? (session =
                    SessionFactory.GetOrCreateSession(
                        RequestContext.Get<IHttpRequest>(),
                        RequestContext.Get<IHttpResponse>()
                    ));
            }
        }

        /// <summary>
        /// Typed UserSession
        /// </summary>
        private object userSession;
        protected TUserSession SessionAs<TUserSession>()
        {
            if (userSession != null) return (TUserSession) userSession;
            if (SessionKey != null)
                userSession = this.GetCacheClient().Get<TUserSession>(SessionKey);
            else
                SessionFeature.CreateSessionIds();
            var unAuthorizedSession = typeof(TUserSession).CreateInstance();
            return (TUserSession) (userSession ?? (userSession = unAuthorizedSession));
        }

        /// <summary>
        /// The UserAgent's SessionKey
        /// </summary>
        protected string SessionKey
        {
            get
            {
                var sessionId = SessionFeature.GetSessionId();
                return sessionId == null ? null : SessionFeature.GetSessionKey(sessionId);
            }
        }

        /// <summary>
        /// Resolve an alternate Web Service from ServiceStack's IOC container.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ResolveService<T>()
        {
            var service = this.GetAppHost().TryResolve<T>();
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
        /// Override to provide additional/less context about the Service Exception. 
        /// By default the request is serialized and appended to the ResponseStatus StackTrace.
        /// </summary>
        public virtual string GetRequestErrorBody()
        {
            var requestString = "";
            try
            {
                requestString = TypeSerializer.SerializeToString(CurrentRequestDto);
            }
            catch /*(Exception ignoreSerializationException)*/
            {
                //Serializing request successfully is not critical and only provides added error info
            }

            return string.Format("[{0}: {1}]:\n[REQUEST: {2}]", GetType().Name, DateTime.UtcNow, requestString);
        }

        /// <summary>
        /// Resolve a dependency from the AppHost's IOC
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T TryResolve<T>()
        {
            return this.GetAppHost() == null
                ? default(T)
                : this.GetAppHost().TryResolve<T>();
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
        /// This method is only called if no exception occured while executing the service.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        protected virtual object OnAfterExecute(object response)
        {
            return response;
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
            if (ex.InnerException != null && !(ex is IHttpError))
                ex = ex.InnerException;

            var responseStatus = ResponseStatusTranslator.Instance.Parse(ex);

            if (EndpointHost.UserConfig.DebugMode)
            {
                // View stack trace in tests and on the client
                responseStatus.StackTrace = GetRequestErrorBody() + ex;
            }

            Log.Error("ServiceBase<TRequest>::Service Exception", ex);

            //If Redis is configured, maintain rolling service error logs in Redis (an in-memory datastore)
            var redisManager = TryResolve<IRedisClientsManager>();
            if (redisManager != null)
            {
                try
                {
                    //Get a thread-safe redis client from the client manager pool
                    using (var client = redisManager.GetClient())
                    {
                        //Get a client with a native interface for storing 'ResponseStatus' objects
                        var redis = client.GetTypedClient<ResponseStatus>();

                        //Store the errors in predictable Redis-named lists i.e. 
                        //'urn:ServiceErrors:{ServiceName}' and 'urn:ServiceErrors:All' 
                        var redisSeriviceErrorList = redis.Lists[UrnId.Create(UrnServiceErrorType, ServiceName)];
                        var redisCombinedErrorList = redis.Lists[UrnId.Create(UrnServiceErrorType, CombinedServiceLogId)];

                        //Append the error at the start of the service-specific and combined error logs.
                        redisSeriviceErrorList.Prepend(responseStatus);
                        redisCombinedErrorList.Prepend(responseStatus);

                        //Clip old error logs from the managed logs
                        const int rollingErrorCount = 1000;
                        redisSeriviceErrorList.Trim(0, rollingErrorCount);
                        redisCombinedErrorList.Trim(0, rollingErrorCount);
                    }
                }
                catch (Exception suppressRedisException)
                {
                    Log.Error("Could not append exception to redis service error logs", suppressRedisException);
                }
            }

            var errorResponse = ServiceUtils.CreateErrorResponse(request, ex, responseStatus);
            
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

            BeforeEachRequest(request);

            //Capture and persist this async request on this Services 'In Queue' 
            //for execution after this request has been completed
            using (var producer = MessageFactory.CreateMessageProducer())
            {
                producer.Publish(request);
            }

            return ServiceUtils.CreateResponseDto(request);
        }

    }
}
