#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web;
using ServiceStack.MiniProfiler;

namespace ServiceStack.Mvc.MiniProfiler
{
    /// <summary>
    /// This filter can be applied globally to hook up automatic action profiling
    /// </summary>
    public class ProfilingActionFilter : ActionFilterAttribute
    {
        const string stackKey = "ProfilingActionFilterStack";

        /// <summary>
        /// Happens before the action starts running
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var mp = Profiler.Current;
            if (mp != null)
            {
                var stack = HttpContext.Current.Items[stackKey] as Stack<IDisposable>;
                if (stack == null)
                {
                    stack = new Stack<IDisposable>();
                    HttpContext.Current.Items[stackKey] = stack;
                }

				var profiler = Profiler.Current;
                if (profiler != null)
                {
                    var tokens = filterContext.RouteData.DataTokens;
                    string area = tokens.ContainsKey("area") && !string.IsNullOrEmpty(tokens["area"].ToString()) ?
                        tokens["area"] + "." :
                        "";
                    string controller = filterContext.Controller.ToString().Split('.').Last() + ".";
                    string action = filterContext.ActionDescriptor.ActionName;

                    stack.Push(profiler.Step("Controller: " + area + controller + action));
                }

            
            }
            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// Happens after the action executes
        /// </summary>
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
            if (HttpContext.Current.Items[stackKey] is Stack<IDisposable> stack && stack.Count > 0)
            {
                stack.Pop()?.Dispose();
            }
        }
    }
}
#endif
