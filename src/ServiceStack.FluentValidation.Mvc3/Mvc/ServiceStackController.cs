using System;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ServiceStack.CacheAccess;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;

namespace ServiceStack.Mvc
{
    [Obsolete("To avoid name conflicts with MVC's ControllerBase this has been renamed to ServiceStackController")]
    public abstract class ControllerBase<T> : ServiceStackController<T> where T : class, IAuthSession, new() { }
    [Obsolete("To avoid name conflicts with MVC's ControllerBase this has been renamed to ServiceStackController")]
    public abstract class ControllerBase : ServiceStackController { }

    public abstract class ServiceStackController<T> : ServiceStackController
        where T : class, IAuthSession, new()
    {
        protected T UserSession
        {
            get { return SessionAs<T>(); }
        }

        public override IAuthSession AuthSession
        {
            get { return UserSession; }
        }
    }


    [ExecuteServiceStackFilters]
    public abstract class ServiceStackController : Controller
    {
        public static string DefaultAction = "Index";
        public static Func<RequestContext, ServiceStackController> CatchAllController;

        /// <summary>
        /// Default redirct URL if [Authenticate] attribute doesn't permit access.
        /// </summary>
        public virtual string LoginRedirectUrl
        {
            get { return "/login?redirect={0}"; }
        }

        /// <summary>
        /// To change the error result when authentication (<see cref="AuthenticateAttribute"/>) 
        /// fails from redirection to something else, 
        /// override this property and return the appropriate result.
        /// </summary>
        public virtual ActionResult AuthenticationErrorResult
        {
            get
            {
                var returnUrl = HttpContext.Request.Url.PathAndQuery;
                return new RedirectResult(LoginRedirectUrl.Fmt(HttpUtility.UrlEncode(returnUrl)));
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

        public ICacheClient Cache { get; set; }

        private ISessionFactory sessionFactory;
        public ISessionFactory SessionFactory
        {
            get { return sessionFactory ?? new SessionFactory(Cache); }
            set { sessionFactory = value; }
        }

        /// <summary>
        /// Typed UserSession
        /// </summary>
        private object userSession;
        protected TUserSession SessionAs<TUserSession>()
        {
            return (TUserSession)(userSession ?? (userSession = Cache.SessionAs<TUserSession>()));
        }

        public virtual void ClearSession()
        {
            userSession = null;
            Cache.ClearSession();
        }

        /// <summary>
        /// Dynamic Session Bag
        /// </summary>
        private ISession session;
        public new ISession Session
        {
            get
            {
                return session ?? (session = SessionFactory.GetOrCreateSession());
            }
        }

        public virtual IAuthSession AuthSession
        {
            get { return (IAuthSession)userSession; }
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

        public virtual ActionResult InvokeDefaultAction(HttpContextBase httpContext)
        {
            try
            {
                this.View(DefaultAction).ExecuteResult(this.ControllerContext);
            }
            catch (Exception ex)
            {
                var catchAllController = CatchAllController != null
                    ? CatchAllController(this.Request.RequestContext)
                    : null;

                if (catchAllController != null)
                {
                    var routeData = new RouteData();
                    var controllerName = catchAllController.GetType().Name.Replace("Controller", "");
                    routeData.Values.Add("controller", controllerName);
                    routeData.Values.Add("action", DefaultAction);
                    routeData.Values.Add("url", httpContext.Request.Url.OriginalString);
                    catchAllController.Execute(new RequestContext(httpContext, routeData));
                }
            }

            return new EmptyResult();
        }

        protected override void HandleUnknownAction(string actionName)
        {
            this.InvokeDefaultAction(HttpContext);
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