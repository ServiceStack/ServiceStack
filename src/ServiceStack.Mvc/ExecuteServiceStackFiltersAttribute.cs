using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ServiceStack.Mvc
{
    public class ExecuteServiceStackFiltersAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var ssController = filterContext.Controller as ServiceStackController;
            if (ssController == null) return;

            var authAttr = GetActionAndControllerAttributes<AuthenticateAttribute>(filterContext)
                .FirstOrDefault();

            if (!ssController.IsAuthorized(authAttr))
            {
                var authError = authAttr != null && authAttr.HtmlRedirect != null
                    ? new RedirectResult(authAttr.HtmlRedirect.AddQueryParam("redirect", ssController.Request.GetPathAndQuery()))
                    : ssController.AuthenticationErrorResult;

                filterContext.Result = authError;
            }

            var roleAttrs = GetActionAndControllerAttributes<RequiredRoleAttribute>(filterContext);
            var anyRoleAttrs = GetActionAndControllerAttributes<RequiresAnyRoleAttribute>(filterContext);
            var permAttrs = GetActionAndControllerAttributes<RequiredPermissionAttribute>(filterContext);
            var anyPermAttrs = GetActionAndControllerAttributes<RequiresAnyPermissionAttribute>(filterContext);

            if (!ssController.HasAccess(roleAttrs, anyRoleAttrs, permAttrs, anyPermAttrs))
            {
                var authError = authAttr != null && authAttr.HtmlRedirect != null
                    ? new RedirectResult(authAttr.HtmlRedirect.AddQueryParam("redirect", ssController.Request.GetPathAndQuery()))
                    : ssController.ForbiddenErrorResult;

                filterContext.Result = authError;
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