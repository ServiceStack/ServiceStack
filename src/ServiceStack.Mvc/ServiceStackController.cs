using System;
using System.Data;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host.AspNet;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Mvc
{
    public abstract class ServiceStackController<T> : ServiceStackController
        where T : IAuthSession
    {
        protected T UserSession
        {
            get { return SessionAs<T>(); }
        }

        public IAuthSession AuthSession
        {
            get { return UserSession; }
        }
    }


    [ExecuteServiceStackFilters]
    public abstract class ServiceStackController : Controller, IHasServiceStackProvider
    {
        public static string DefaultAction = "Index";
        public static Func<System.Web.Routing.RequestContext, ServiceStackController> CatchAllController;

        /// <summary>
        /// Default redirct URL if [Authenticate] attribute doesn't permit access.
        /// </summary>
        public virtual string UnauthorizedRedirectUrl
        {
            get { return HostContext.GetPlugin<AuthFeature>().GetHtmlRedirect() + "?redirect={0}#f=Unauthorized"; }
        }

        /// <summary>
        /// To change the error result when authentication (<see cref="AuthenticateAttribute"/>) fails.
        /// Override this property and return the appropriate result.
        /// </summary>
        public virtual ActionResult AuthenticationErrorResult
        {
            get
            {
                var returnUrl = HttpContext.Request.GetPathAndQuery();
                return new RedirectResult(UnauthorizedRedirectUrl.Fmt(returnUrl.UrlEncode()));
            }
        }

        /// <summary>
        /// Default redirct URL if Required Role or Permission attributes doesn't permit access.
        /// </summary>
        public virtual string ForbiddenRedirectUrl
        {
            get { return HostContext.GetPlugin<AuthFeature>().GetHtmlRedirect() + "?redirect={0}#f=Forbidden"; }
        }

        /// <summary>
        /// To change the error result when user doesn't have required role or permissions (<see cref="RequiredRoleAttribute"/>).
        /// Override this property and return the appropriate result.
        /// </summary>
        public virtual ActionResult ForbiddenErrorResult
        {
            get
            {
                var returnUrl = HttpContext.Request.GetPathAndQuery();
                return new RedirectResult(ForbiddenRedirectUrl.Fmt(returnUrl.UrlEncode()));
            }
        }

        /// <summary>
        /// To change the error result when authorization fails
        /// to something else, override this property and return the appropriate result.
        /// </summary>
        public virtual ActionResult AuthorizationErrorResult
        {
            get
            {
                return new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    controller = "Error",
                    action = "Unauthorized"
                }));
            }
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

        private IServiceStackProvider serviceStackProvider;
        public virtual IServiceStackProvider ServiceStackProvider
        {
            get
            {
                return serviceStackProvider ?? (serviceStackProvider = 
                    new ServiceStackProvider(new AspNetRequest(base.HttpContext, GetType().Name)));
            }
        }
        public virtual IAppSettings AppSettings
        {
            get { return ServiceStackProvider.AppSettings; }
        }
        public virtual IHttpRequest ServiceStackRequest
        {
            get { return ServiceStackProvider.Request; }
        }
        public virtual IHttpResponse ServiceStackResponse
        {
            get { return ServiceStackProvider.Response; }
        }
        public virtual ICacheClient Cache
        {
            get { return ServiceStackProvider.Cache; }
        }
        public virtual IDbConnection Db
        {
            get { return ServiceStackProvider.Db; }
        }
        public virtual IRedisClient Redis
        {
            get { return ServiceStackProvider.Redis; }
        }
        public virtual IMessageFactory MessageFactory
        {
            get { return ServiceStackProvider.MessageFactory; }
        }
        public virtual IMessageProducer MessageProducer
        {
            get { return ServiceStackProvider.MessageProducer; }
        }
        public virtual ISessionFactory SessionFactory
        {
            get { return ServiceStackProvider.SessionFactory; }
        }
        public virtual ISession SessionBag
        {
            get { return ServiceStackProvider.SessionBag; }
        }
        public virtual bool IsAuthenticated
        {
            get { return ServiceStackProvider.IsAuthenticated; }
        }
        protected virtual T TryResolve<T>()
        {
            return ServiceStackProvider.TryResolve<T>();
        }
        protected virtual T ResolveService<T>()
        {
            return ServiceStackProvider.ResolveService<T>();
        }
        protected virtual object Execute(object requestDto)
        {
            return ServiceStackProvider.Execute(requestDto);
        }
        protected virtual object ForwardRequestToServiceStack(IRequest request = null)
        {
            return ServiceStackProvider.Execute(request ?? ServiceStackProvider.Request);
        }
        protected virtual IAuthSession GetSession(bool reload = true)
        {
            return ServiceStackProvider.GetSession(reload);
        }
        protected virtual TUserSession SessionAs<TUserSession>()
        {
            return ServiceStackProvider.SessionAs<TUserSession>();
        }
        protected virtual void ClearSession()
        {
            ServiceStackProvider.ClearSession();
        }
        protected virtual void PublishMessage<T>(T message)
        {
            ServiceStackProvider.PublishMessage(message);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (serviceStackProvider != null)
            {
                serviceStackProvider.Dispose();
                serviceStackProvider = null;
            }
        }    
    }

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
}