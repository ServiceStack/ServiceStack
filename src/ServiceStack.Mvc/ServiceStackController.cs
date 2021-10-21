﻿using System;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Web;
using System.Web;

#if !NETSTANDARD
    using ServiceStack.Host.AspNet;
    using System.Web.Mvc;
    using System.Web.Routing;
#else
    using ServiceStack.Host;
    using ServiceStack.Host.NetCore;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
#endif

namespace ServiceStack.Mvc
{
    public abstract class ServiceStackController<T> : ServiceStackController
        where T : IAuthSession
    {
        protected T UserSession => SessionAs<T>();

        public IAuthSession AuthSession => UserSession;
    }

    [ExecuteServiceStackFilters]
    public abstract class ServiceStackController : Controller, IHasServiceStackProvider
    {
        public static string DefaultAction = "Index";

        /// <summary>
        /// Default redirect URL if [Authenticate] attribute doesn't permit access.
        /// </summary>
        public virtual string UnauthorizedRedirectUrl => 
            HostContext.GetPlugin<AuthFeature>().GetHtmlRedirect();

        /// <summary>
        /// To change the error result when authentication (<see cref="AuthenticateAttribute"/>) fails.
        /// Override this property and return the appropriate result.
        /// </summary>
        public virtual ActionResult AuthenticationErrorResult
        {
            get
            {
                var returnUrl = HttpContext.Request.GetPathAndQuery();
                var unauthorizedUrl = UnauthorizedRedirectUrl;
                if (unauthorizedUrl.IsNullOrEmpty() )
                    throw new HttpException(401, "Unauthorized");

                return new RedirectResult(unauthorizedUrl + "?redirect={0}#f=Unauthorized".Fmt(returnUrl.UrlEncode()));
            }
        }

        /// <summary>
        /// Default redirect URL if Required Role or Permission attributes doesn't permit access.
        /// </summary>
        public virtual string ForbiddenRedirectUrl => 
            HostContext.GetPlugin<AuthFeature>().GetHtmlRedirect();

        /// <summary>
        /// To change the error result when user doesn't have required role or permissions (<see cref="RequiredRoleAttribute"/>).
        /// Override this property and return the appropriate result.
        /// </summary>
        public virtual ActionResult ForbiddenErrorResult
        {
            get
            {
                var returnUrl = HttpContext.Request.GetPathAndQuery();
                var forbiddenUrl = ForbiddenRedirectUrl;
                if (forbiddenUrl.IsNullOrEmpty())
                    throw new HttpException(403, "Forbidden");

                return new RedirectResult(forbiddenUrl + "?redirect={0}#f=Forbidden".Fmt(returnUrl.UrlEncode()));
            }
        }

        /// <summary>
        /// To change the error result when authorization fails
        /// to something else, override this property and return the appropriate result.
        /// </summary>
        public virtual ActionResult AuthorizationErrorResult => 
            new RedirectToRouteResult(new RouteValueDictionary(new
            {
                controller = "Error",
                action = "Unauthorized"
            }));

#if !NETSTANDARD
        public static Func<System.Web.Routing.RequestContext, ServiceStackController> CatchAllController;

        protected virtual ActionResult InvokeDefaultAction(HttpContextBase httpContext)
        {
            try
            {
                this.View(DefaultAction).ExecuteResult(this.ControllerContext);
            }
            catch
            {
                // We failed to execute our own default action, so we'll fall back to
                // the CatchAllController, if one is specified.

                if (CatchAllController != null)
                {
                    var catchAllController = CatchAllController(this.Request.RequestContext);
                    InvokeControllerDefaultAction(catchAllController, httpContext);
                }
            }

            return new EmptyResult();
        }

        protected override void HandleUnknownAction(string actionName)
        {
            if (CatchAllController == null)
            {
                base.HandleUnknownAction(actionName); // delegate to default MVC behaviour, which will throw 404.
            }
            else
            {
                var catchAllController = CatchAllController(this.Request.RequestContext);
                InvokeControllerDefaultAction(catchAllController, HttpContext);
            }
        }

        private void InvokeControllerDefaultAction(ServiceStackController controller, HttpContextBase httpContext)
        {
            var routeData = new RouteData();
            var controllerName = controller.GetType().Name.Replace("Controller", "");
            routeData.Values.Add("controller", controllerName);
            routeData.Values.Add("action", DefaultAction);
            routeData.Values.Add("url", httpContext.Request.Url.OriginalString);
            controller.Execute(new System.Web.Routing.RequestContext(httpContext, routeData));
        }

        protected override JsonResult Json(object data, string contentType, Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new ServiceStackJsonResult
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding
            };
        }
#else
        public override JsonResult Json(object data)
        {
            return new ServiceStackJsonResult(data);
        }
#endif

        private IServiceStackProvider serviceStackProvider;
        public virtual IServiceStackProvider ServiceStackProvider => 
            serviceStackProvider ??=  
#if !NETSTANDARD
            new ServiceStackProvider(new AspNetRequest(base.HttpContext, GetType().Name));
#else
            new ServiceStackProvider(new NetCoreRequest(base.HttpContext, GetType().Name));
#endif
        public virtual IAppSettings AppSettings => ServiceStackProvider.AppSettings;

        public virtual IHttpRequest ServiceStackRequest => ServiceStackProvider.Request;

        public virtual IHttpResponse ServiceStackResponse => ServiceStackProvider.Response;

        public virtual ICacheClient Cache => ServiceStackProvider.Cache;

        public virtual ICacheClientAsync CacheAsync => ServiceStackProvider.CacheAsync;

        public virtual IDbConnection Db => ServiceStackProvider.Db;

        public virtual IRedisClient Redis => ServiceStackProvider.Redis;

        public virtual IMessageProducer MessageProducer => ServiceStackProvider.MessageProducer;

        public virtual IAuthRepository AuthRepository => ServiceStackProvider.AuthRepository;
        
        public virtual IAuthRepositoryAsync AuthRepositoryAsync => ServiceStackProvider.AuthRepositoryAsync;

        public virtual ISessionFactory SessionFactory => ServiceStackProvider.SessionFactory;

        public virtual Caching.ISession SessionBag => ServiceStackProvider.SessionBag;
        
        public virtual Caching.ISessionAsync SessionBagAsync => ServiceStackProvider.SessionBagAsync;

        public virtual bool IsAuthenticated => ServiceStackProvider.IsAuthenticated;

        protected virtual IAuthSession GetSession(bool reload = true) => ServiceStackProvider.GetSession(reload);

        protected virtual Task<IAuthSession> GetSessionAsync(bool reload = false, CancellationToken token=default) => 
            ServiceStackProvider.GetSessionAsync(reload, token);

        protected virtual Task<TUserSession> SessionAsAsync<TUserSession>(CancellationToken token=default) => 
            ServiceStackProvider.SessionAsAsync<TUserSession>(token);

        //don't expose public generic methods in MVC Controllers
        protected virtual TUserSession SessionAs<TUserSession>() => ServiceStackProvider.SessionAs<TUserSession>();

        [Obsolete("Use SaveSessionAsync")]
        protected virtual void SaveSession(IAuthSession session, TimeSpan? expiresIn = null) => ServiceStackProvider.Request.SaveSession(session, expiresIn);

        protected virtual Task SaveSessionAsync(IAuthSession session, TimeSpan? expiresIn = null, CancellationToken token=default) => 
            ServiceStackProvider.Request.SaveSessionAsync(session, expiresIn, token);

        protected virtual void ClearSession() => ServiceStackProvider.ClearSession();
        
        protected virtual Task ClearSessionAsync(CancellationToken token=default) => ServiceStackProvider.ClearSessionAsync(token);

        public virtual IServiceGateway Gateway => ServiceStackProvider.Gateway;
        public virtual RpcGateway RpcGateway => ServiceStackProvider.RpcGateway;

        private bool hasDisposed = false;
        protected override void Dispose(bool disposing)
        {
            if (hasDisposed)
                return;

            hasDisposed = true;
            base.Dispose(disposing);

            if (serviceStackProvider != null)
            {
                serviceStackProvider.Dispose();
                serviceStackProvider = null;
            }

            EndServiceStackRequest();
        }

        protected virtual void EndServiceStackRequest() => 
            HostContext.AppHost.OnEndRequest(ServiceStackRequest);
    }

    // Move non session public convenience methods out of ServiceStackController so it doesn't interfere with routes.MapMvcAttributeRoutes()
    public static class ServiceStackControllerExt
    {
        public static object ForwardRequestToServiceStack(this ServiceStackController controller, IRequest request = null) 
            => controller.ServiceStackProvider.Execute(request ?? controller.ServiceStackProvider.Request);
        
#if NET472 || NETSTANDARD
        public static ValueTask<IRedisClientAsync> GetRedisAsync(this ServiceStackController controller) => 
            controller.ServiceStackProvider.GetRedisAsync();
#endif
        public static T TryResolve<T>(this ServiceStackController controller) => controller.ServiceStackProvider.TryResolve<T>();
        public static T ResolveService<T>(this ServiceStackController controller) => controller.ServiceStackProvider.ResolveService<T>();
    }
    
#if !NETSTANDARD
    public class ServiceStackJsonResult : JsonResult
    {
        public override void ExecuteResult(ControllerContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = !string.IsNullOrEmpty(ContentType) ? ContentType : "application/json";

            if (ContentEncoding != null)
            {
                response.ContentEncoding = ContentEncoding;
            }

            if (Data != null)
            {
                response.Write(JsonSerializer.SerializeToString(Data));
            }
        }
    }
#else
    public class ServiceStackJsonResult : JsonResult
    {
        public ServiceStackJsonResult(object value) : base(value) {}

        public override Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = !string.IsNullOrEmpty(ContentType) ? ContentType : "application/json";

            if (Value != null)
                return response.WriteAsync(JsonSerializer.SerializeToString(Value));

            return TypeConstants.EmptyTask;
        }
    }
#endif
}
