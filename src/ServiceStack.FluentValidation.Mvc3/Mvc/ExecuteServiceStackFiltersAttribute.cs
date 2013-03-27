using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.Mvc
{
	public class ExecuteServiceStackFiltersAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			var ssController = filterContext.Controller as ServiceStackController;
			if (ssController == null) return;

			var authAttrs = GetActionAndControllerAttributes<AuthenticateAttribute>(filterContext);
			if (authAttrs.Count > 0 && ( ssController.AuthSession==null || !ssController.AuthSession.IsAuthenticated))
			{
                filterContext.Result = ssController.AuthenticationErrorResult;
                return;
			}

			var roleAttrs = GetActionAndControllerAttributes<RequiredRoleAttribute>(filterContext);
			var permAttrs = GetActionAndControllerAttributes<RequiredPermissionAttribute>(filterContext);

			if (roleAttrs.Count == 0 && permAttrs.Count == 0) return;

			var httpReq = HttpContext.Current.Request.ToRequest();
			var userAuthRepo = httpReq.TryResolve<IUserAuthRepository>();

			var hasRoles = roleAttrs.All(x => x.HasAllRoles(httpReq, ssController.AuthSession, userAuthRepo));
			if (!hasRoles)
			{
				filterContext.Result = ssController.AuthorizationErrorResult;
				return;
			}

			var hasPermssions = permAttrs.All(x => x.HasAllPermissions(httpReq, ssController.AuthSession, userAuthRepo));
			if (!hasPermssions)
			{
				filterContext.Result = ssController.AuthorizationErrorResult;
				return;
			}
		}

		private static List<T> GetActionAndControllerAttributes<T>(ActionExecutingContext filterContext)
			where T : Attribute
		{
			var attrs = new List<T>();

			var attr = filterContext.ActionDescriptor
				.GetCustomAttributes(typeof(T), true)
				.FirstOrDefault() as T;

			if (attr != null)
				attrs.Add(attr);

			attr = filterContext.Controller.GetType()
				.GetCustomAttributes(typeof(T), true)
				.FirstOrDefault() as T;

			if (attr != null)
				attrs.Add(attr);

			return attrs;
		}
	}
}