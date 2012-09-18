using System;
using System.Diagnostics;
using ServiceStack.CacheAccess;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
    public abstract class Express : IExpress, IRequiresRequestContext
    {
        private readonly string baseUri;

        protected Express() {}

        protected Express(string baseUri)
        {
            this.baseUri = baseUri;
            if (this.baseUri != null)
            {
                if (!this.baseUri.StartsWith("/"))
                    this.baseUri = "/" + this.baseUri;

                this.baseUri = this.baseUri.TrimEnd('/');
            }
        }
        
        private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceBase<>));

        /// <summary>
        /// Stopwatch used to record the duration of each request
        /// </summary>
        private Stopwatch requestDurationStopwatch;

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
            get { return CurrentRequestDto != null ? CurrentRequestDto.GetType().Name : null; }
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

        public Express SetAppHost(IAppHost appHost) //Allow chaining
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
        protected void BeforeEachRequest<TRequest>(TRequest requestDto)
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
        protected object AfterEachRequest<TRequest>(TRequest requestDto, object response)
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
                    SessionFactory = new SessionFactory(this.GetAppHost().GetCacheClient());

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
            if (userSession != null) return (TUserSession)userSession;
            if (SessionKey != null)
                userSession = this.GetAppHost().GetCacheClient().Get<TUserSession>(SessionKey);
            else
                SessionFeature.CreateSessionIds();
            var unAuthorizedSession = typeof(TUserSession).CreateInstance();
            return (TUserSession)(userSession ?? (userSession = unAuthorizedSession));
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
        protected object CurrentRequestDto;

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
        protected object Run<TRequest>(RouteAttribute route, TRequest request)
        {
            return null;
        }

        /// <summary>
        /// Called before the request is Executed. Override to enforce generic validation logic.
        /// </summary>
        /// <param name="request"></param>
        protected virtual void OnBeforeExecute<TRequest>(TRequest request) { }

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
        public object Execute<TRequest>(TRequest request)
        {
            try
            {
                BeforeEachRequest(request);
                return AfterEachRequest(request, Run(null, request));
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
        protected virtual object HandleException<TRequest>(TRequest request, Exception ex)
        {
            GetAppHost().ExceptionHandler(Request, Response, request.Dump(), ex);
            return ex;
        }

        /// <summary>
        /// Return a custom view with this response.
        /// Not required for views named after the Response or Request DTO name - which are automatically resolved.
        /// </summary>
        protected HttpResult View(object response, string viewName, string templateName = null)
        {
            return new HttpResult(response) {
                View = viewName,
                Template = templateName,
            };
        }
        
    }

}