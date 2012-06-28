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
		private T userSession;
		protected T UserSession
		{
			get
			{
				if (userSession != null) return userSession;
				if (SessionKey != null)
					userSession = this.Cache.Get<T>(SessionKey);
				else
					SessionFeature.CreateSessionIds();

				var unAuthorizedSession = new T();
				return userSession ?? (userSession = unAuthorizedSession);
			}
		}

		public override IAuthSession AuthSession
		{
			get { return UserSession; }
		}

		public override void ClearSession()
		{
			userSession = null;
			this.Cache.Remove(SessionKey);
		}
	}


	[ExecuteServiceStackFilters]
	public abstract class ServiceStackController : Controller
	{
		public static string DefaultAction = "Index";
		public static Func<RequestContext, ServiceStackController> CatchAllController;

		public virtual string LoginRedirectUrl
		{
			get { return "/login?redirect={0}"; }
		}

		public virtual ActionResult AuthorizationErrorResult
		{
			get
			{
				return new RedirectToRouteResult(new RouteValueDictionary(new {
					controller = "Error",
					action = "Unauthorized"
				}));
			}
		}

		public ICacheClient Cache { get; set; }
		public ISessionFactory SessionFactory { get; set; }

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
			get { return null; }
		}

		protected string SessionKey
		{
			get
			{
				var sessionId = SessionFeature.GetSessionId();
				return sessionId == null ? null : SessionFeature.GetSessionKey(sessionId);
			}
		}

		protected override JsonResult Json(object data, string contentType, Encoding contentEncoding, JsonRequestBehavior behavior)
		{
			return new ServiceStackJsonResult {
				Data = data,
				ContentType = contentType,
				ContentEncoding = contentEncoding
			};
		}

		public virtual void ClearSession()
		{
			this.Cache.Remove(SessionKey);
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