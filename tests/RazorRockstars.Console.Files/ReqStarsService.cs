using System;
using System.Data;
using System.Diagnostics;
using Alternate.ExpressLike.Controller.Proposal;
using ServiceStack.CacheAccess;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace RazorRockstars.Console.Files
{
    //Proposal 2: Keeping ServiceStack's message-based semantics
    //Inspired by Ivan's proposal: http://korneliuk.blogspot.com/2012/08/servicestack-reusing-dtos.html

    [Route("/", "GET")]
    [Route("/aged/{Age}")]
    public class SearchReqstars
    {
        public int Id { get; set; }
        public int? Age { get; set; }
    }

    [Route("/reqstars/reset")]
    public class ResetReqstar : IReturnVoid {}

    [Route("/reqstars/{Id}", "GET")]
    public class GetReqstar : IReturn<Reqstar>
    {
        public int Id { get; set; }
    }

    [Route("/reqstars/{Id}/delete")]
    public class DeleteReqstar : IReturnVoid
    {
        public int Id { get; set; }
    }

    [Authenticate]
    public class ReqStarsService : Service
    {
        public object Get(SearchReqstars request)
        {
            return new ReqstarsResponse //matches ReqstarsResponse.cshtml razor view
            {
                Aged = request.Age,
                Total = Db.GetScalar<int>("select count(*) from Reqstar"),
                Results = request.Age.HasValue ?
                    Db.Select<Reqstar>(q => q.Age == request.Age.Value)
                      : Db.Select<Reqstar>()
            };
        }

        [ClientCanSwapTemplates] //aka action-level filters
        public object Get(GetReqstar request)
        {
            return Db.Id<Reqstar>(request.Id);
        }

        public object Post(Reqstar request)
        {
            Db.Insert(request.TranslateTo<Reqstar>());
            return Get(new SearchReqstars());
        }

        public void Any(DeleteReqstar request)
        {
            Db.DeleteById<Reqstar>(request.Id);
        }

        public void Any(ResetReqstar request)
        {
            Db.DeleteAll<Reqstar>();
            Db.Insert(Reqstar.SeedData);
        }
    }

    public class AppHostDummy : BasicAppHost
    {
        public virtual IServiceRunner<TRequest> GetServiceRunner<TRequest>(
            ActionContext<TRequest> actionContext)
        {
            return new ServiceRunner<TRequest>(this, actionContext); //cached per service action
        }
    }

    public class ActionContext<TRequest>
    {
        public Func<TRequest, object> ServiceAction { get; set; }
        public IHasRequestFilter[] RequestFilters { get; set; }
        public IHasResponseFilter[] ResponseFilters { get; set; }
    }

    public class ServiceRunner<TRequest> : IServiceRunner<TRequest>
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(ServiceRunner<>));

        protected readonly IAppHost appHost;
        protected readonly Func<TRequest, object> serviceAction;
        public IHasRequestFilter[] requestFilters;
        public IHasResponseFilter[] responseFilters;

        public ServiceRunner() { }

        public ServiceRunner(IAppHost appHost, ActionContext<TRequest> actionContext)
        {
            this.appHost = appHost;
            this.serviceAction = actionContext.ServiceAction;
            this.requestFilters = actionContext.RequestFilters;
            this.responseFilters = actionContext.ResponseFilters;
        }

        public IAppHost GetAppHost()
        {
            return appHost ?? EndpointHost.AppHost;
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

        public virtual object Execute(IRequestContext requestContext, TRequest request)
        {
            try
            {
                BeforeEachRequest(requestContext, request);

                var httpReq = requestContext.Get<IHttpRequest>();
                var httpRes = requestContext.Get<IHttpResponse>();

                if (requestFilters != null)
                {
                    foreach (var requestFilter in requestFilters)
                    {
                        requestFilter.RequestFilter(httpReq, httpRes, request);
                        if (httpRes.IsClosed) return null;
                    }
                }

                var response = AfterEachRequest(requestContext, request, serviceAction(request));

                if (responseFilters != null)
                {
                    foreach (var responseFilter in responseFilters)
                    {
                        responseFilter.ResponseFilter(httpReq, httpRes, response);
                        if (httpRes.IsClosed) return null;
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

        public virtual object Execute(IRequestContext requestContext, IMessage<TRequest> request)
        {
            return Execute(requestContext, request.GetBody());
        }

        public virtual object HandleException(IRequestContext requestContext, TRequest request, Exception ex)
        {
            if (ex.InnerException != null && !(ex is IHttpError))
                ex = ex.InnerException;

            var responseStatus = ResponseStatusTranslator.Instance.Parse(ex);

            if (EndpointHost.UserConfig.DebugMode)
            {
                // View stack trace in tests and on the client
                responseStatus.StackTrace = GetRequestErrorBody(request) + ex;
            }

            Log.Error("ServiceBase<TRequest>::Service Exception", ex);

            var errorResponse = ServiceUtils.CreateErrorResponse(request, ex, responseStatus);

            AfterEachRequest(requestContext, request, errorResponse ?? ex);

            return errorResponse;
        }

        public virtual string GetRequestErrorBody(TRequest request)
        {
            var requestString = "";
            try
            {
                requestString = TypeSerializer.SerializeToString(request);
            }
            catch /*(Exception ignoreSerializationException)*/
            {
                //Serializing request successfully is not critical and only provides added error info
            }

            return string.Format("[{0}: {1}]:\n[REQUEST: {2}]", GetType().Name, DateTime.UtcNow, requestString);
        }


        public object ExecuteAsync(IRequestContext requestContext, TRequest request)
        {
            var msgFactory = TryResolve<IMessageFactory>();
            if (msgFactory == null)
            {
                return Execute(requestContext, request);
            }

            //Capture and persist this async request on this Services 'In Queue' 
            //for execution after this request has been completed
            using (var producer = msgFactory.CreateMessageProducer())
            {
                producer.Publish(request);
            }

            return ServiceUtils.CreateResponseDto(request);
        }

        public object Process(IRequestContext requestContext, object request)
        {
            return Execute(requestContext, (TRequest)request);
        }

        public object Process(IRequestContext requestContext, IMessage message)
        {
            return Execute(requestContext, (IMessage<TRequest>)message);
        }

        public object ProcessAsync(IRequestContext requestContext, object request)
        {
            return ExecuteAsync(requestContext, (TRequest)request);
        }
    }

    public interface IServiceRunner
    {
        object Process(IRequestContext requestContext, object request);
        object Process(IRequestContext requestContext, IMessage message);
        object ProcessAsync(IRequestContext requestContext, object request);
    }

    public interface IServiceRunner<TRequest> : IServiceRunner
    {
        void OnBeforeExecute(IRequestContext requestContext, TRequest request);
        object OnAfterExecute(IRequestContext requestContext, object response);
        object HandleException(IRequestContext requestContext, TRequest request, Exception ex);

        object Execute(IRequestContext requestContext, TRequest request);
        object Execute(IRequestContext requestContext, IMessage<TRequest> request);
        object ExecuteAsync(IRequestContext requestContext, TRequest request);
    }


    /// <summary>
    /// Generic + Useful base class
    /// </summary>
    public class Service : IService, IRequiresRequestContext, IDisposable
    {
        public IRequestContext RequestContext { get; set; }

        private IAppHost appHost;
        public virtual IAppHost GetAppHost()
        {
            return appHost ?? EndpointHost.AppHost;
        }

        public virtual void SetAppHost(IAppHost appHost) //Allow chaining
        {
            this.appHost = appHost;
        }

        public virtual T TryResolve<T>()
        {
            return this.GetAppHost() == null
                ? default(T)
                : this.GetAppHost().TryResolve<T>();
        }

        public virtual T ResolveService<T>()
        {
            var service = TryResolve<T>();
            var requiresContext = service as IRequiresRequestContext;
            if (requiresContext != null)
            {
                requiresContext.RequestContext = this.RequestContext;
            }
            return service;
        }

        private IHttpRequest request;
        protected virtual IHttpRequest Request
        {
            get { return request ?? (request = RequestContext.Get<IHttpRequest>()); }
        }

        private IHttpResponse response;
        protected virtual IHttpResponse Response
        {
            get { return response ?? (response = RequestContext.Get<IHttpResponse>()); }
        }

        private ICacheClient cache;
        public virtual ICacheClient Cache
        {
            get { return cache ?? (cache = TryResolve<ICacheClient>()); }
        }

        private IDbConnection db;
        public virtual IDbConnection Db
        {
            get { return db ?? (db = TryResolve<IDbConnectionFactory>().Open()); }
        }
        
        public ISessionFactory sessionFactory;
        public virtual ISessionFactory SessionFactory
        {
            get { return sessionFactory ?? (sessionFactory = TryResolve<ISessionFactory>()) ?? new SessionFactory(Cache); }
        }

        /// <summary>
        /// Dynamic Session Bag
        /// </summary>
        private ISession session;
        public virtual ISession Session
        {
            get
            {
                return session ?? (session = SessionFactory.GetOrCreateSession(Request, Response));
            }
        }

        /// <summary>
        /// Typed UserSession
        /// </summary>
        private object userSession;
        protected virtual TUserSession SessionAs<TUserSession>()
        {
            if (userSession != null) return (TUserSession)userSession;
            if (SessionKey != null)
                userSession = Cache.Get<TUserSession>(SessionKey);
            else
                SessionFeature.CreateSessionIds();
            var unAuthorizedSession = typeof(TUserSession).CreateInstance();
            return (TUserSession)(userSession ?? (userSession = unAuthorizedSession));
        }

        /// <summary>
        /// The UserAgent's SessionKey
        /// </summary>
        protected virtual string SessionKey
        {
            get
            {
                var sessionId = SessionFeature.GetSessionId();
                return sessionId == null ? null : SessionFeature.GetSessionKey(sessionId);
            }
        }

        public virtual void Dispose()
        {
            if (cache != null)
                cache.Dispose();
            if (db != null)
                db.Dispose();
        }
    }

}