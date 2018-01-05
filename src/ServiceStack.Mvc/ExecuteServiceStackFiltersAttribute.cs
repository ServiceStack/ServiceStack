using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
#if !NETSTANDARD2_0
    using System.Web.Mvc;
#else
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.AspNetCore.Mvc.Controllers;
#endif

namespace ServiceStack.Mvc
{
    public class ExecuteServiceStackFiltersAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!(filterContext.Controller is ServiceStackController ssController)) return;

            ssController.ViewData[Keywords.IRequest] = ssController.ServiceStackRequest;

            var authAttr = GetActionAndControllerAttributes<AuthenticateAttribute>(filterContext)
                .FirstOrDefault();

            if (!ssController.IsAuthorized(authAttr))
            {
                var authError = authAttr?.HtmlRedirect != null
                    ? new RedirectResult(authAttr.HtmlRedirect.AddQueryParam("redirect", ssController.Request.GetPathAndQuery()))
                    : ssController.AuthenticationErrorResult;

                filterContext.Result = authError;
                return;
            }

            var roleAttrs = GetActionAndControllerAttributes<RequiredRoleAttribute>(filterContext);
            var anyRoleAttrs = GetActionAndControllerAttributes<RequiresAnyRoleAttribute>(filterContext);
            var permAttrs = GetActionAndControllerAttributes<RequiredPermissionAttribute>(filterContext);
            var anyPermAttrs = GetActionAndControllerAttributes<RequiresAnyPermissionAttribute>(filterContext);

            if (!ssController.HasAccess(roleAttrs, anyRoleAttrs, permAttrs, anyPermAttrs))
            {
                var authError = authAttr?.HtmlRedirect != null
                    ? new RedirectResult(authAttr.HtmlRedirect.AddQueryParam("redirect", ssController.Request.GetPathAndQuery()))
                    : ssController.ForbiddenErrorResult;

                filterContext.Result = authError;
            }
        }

        private static List<T> GetActionAndControllerAttributes<T>(ActionExecutingContext filterContext)
            where T : Attribute
        {
            var attrs = new List<T>();

#if !NETSTANDARD2_0
            var attr = filterContext.ActionDescriptor
                .GetCustomAttributes(typeof(T), true)
                .FirstOrDefault() as T;
#else
            var controllerActionDescriptor = filterContext.ActionDescriptor as ControllerActionDescriptor;
            var attr = controllerActionDescriptor?.MethodInfo.FirstAttribute<T>();
#endif


            if (attr != null)
                attrs.Add(attr);

            attr = filterContext.Controller.GetType().FirstAttribute<T>();

            if (attr != null)
                attrs.Add(attr);

            return attrs;
        }

    }
}