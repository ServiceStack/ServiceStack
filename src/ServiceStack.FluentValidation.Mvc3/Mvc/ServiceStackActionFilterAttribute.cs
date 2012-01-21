using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Mvc
{
	public class ExecuteServiceStackFiltersAttribute : ActionFilterAttribute
	{
		static readonly ActionResult AuthorizationErrorResult = new RedirectToRouteResult(new RouteValueDictionary(new {
			controller = "Error",
			action = "Unauthorized"
		}));

		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			var ssController = filterContext.Controller as ControllerBase;
			if (ssController == null) return;

			var authAttr = filterContext.Controller.GetType()
				.GetCustomAttributes(typeof(AuthenticateAttribute), true)
				.FirstOrDefault() as AuthenticateAttribute;

			if (authAttr != null && !ssController.AuthSession.IsAuthenticated)
			{
				var returnUrl = filterContext.HttpContext.Request.Url.AbsolutePath;
				filterContext.Result = new RedirectResult("/login?return=" + returnUrl);
				return;
			}

			var roleAttr = filterContext.Controller.GetType()
				.GetCustomAttributes(typeof(RequiredRoleAttribute), true)
				.FirstOrDefault() as RequiredRoleAttribute;

			if (roleAttr != null && !roleAttr.RequiredRoles.All(ssController.AuthSession.HasRole))
			{
				filterContext.Result = AuthorizationErrorResult;
				return;
			}

			var permAttr = filterContext.Controller.GetType()
				.GetCustomAttributes(typeof(RequiredPermissionAttribute), true)
				.FirstOrDefault() as RequiredPermissionAttribute;

			if (permAttr != null && !permAttr.RequiredPermissions.All(ssController.AuthSession.HasPermission))
			{
				filterContext.Result = AuthorizationErrorResult;
				return;
			}
		}
	}
}