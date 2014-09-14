using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ServiceStack.Auth;

namespace ServiceStack.Mvc
{
	public class ExecuteServiceStackFiltersAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			var ssController = filterContext.Controller as ServiceStackController;
			if (ssController == null) return;

		    var authSession = ssController.GetSession();

			var authAttrs = GetActionAndControllerAttributes<AuthenticateAttribute>(filterContext);
            if (authAttrs.Count > 0 && !(authSession == null || !authSession.IsAuthenticated))
			{
				filterContext.Result = ssController.AuthenticationErrorResult;
				return;
			}

			var roleAttrs = GetActionAndControllerAttributes<RequiredRoleAttribute>(filterContext);
			var anyRoleAttrs = GetActionAndControllerAttributes<RequiresAnyRoleAttribute>(filterContext);
			var permAttrs = GetActionAndControllerAttributes<RequiredPermissionAttribute>(filterContext);
			var anyPermAttrs = GetActionAndControllerAttributes<RequiresAnyPermissionAttribute>(filterContext);

			if (roleAttrs.Count + anyRoleAttrs.Count + permAttrs.Count + anyPermAttrs.Count == 0) return;

			var httpReq = HttpContext.Current.ToRequest();
			var userAuthRepo = httpReq.TryResolve<IAuthRepository>();

            var hasRoles = roleAttrs.All(x => x.HasAllRoles(httpReq, authSession, userAuthRepo));
			if (!hasRoles)
			{
				filterContext.Result = ssController.AuthorizationErrorResult;
				return;
			}

            var hasAnyRole = anyRoleAttrs.All(x => x.HasAnyRoles(httpReq, authSession, userAuthRepo));
			if (!hasAnyRole)
			{
				filterContext.Result = ssController.AuthorizationErrorResult;
				return;
			}

            var hasPermssions = permAttrs.All(x => x.HasAllPermissions(httpReq, authSession, userAuthRepo));
			if (!hasPermssions)
			{
				filterContext.Result = ssController.AuthorizationErrorResult;
				return;
			}

            var hasAnyPermission = anyPermAttrs.All(x => x.HasAnyPermissions(httpReq, authSession, userAuthRepo));
			if (!hasAnyPermission)
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