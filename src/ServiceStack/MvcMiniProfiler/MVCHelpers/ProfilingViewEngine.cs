#if ASP_NET_MVC3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web;

namespace MvcMiniProfiler.MVCHelpers
{
    /// <summary>
    /// You can wrap your view engines with this view to enable profiling on views and partial
    /// </summary>
    public class ProfilingViewEngine : IViewEngine
    {
        class WrappedView : IView
        {
            IView wrapped;
            string name;
            bool isPartial;

            public WrappedView(IView wrapped, string name, bool isPartial)
            {
                this.wrapped = wrapped;
                this.name = name;
                this.isPartial = isPartial;
            }

            public void Render(ViewContext viewContext, System.IO.TextWriter writer)
            {
                using (MiniProfiler.Current.Step("Render " + (isPartial ? "partial" : "") + ": " + name))
                {
                    wrapped.Render(viewContext, writer);
                }
            }
        }

        IViewEngine wrapped;

        /// <summary>
        /// Wrap your view engines with this to allow profiling
        /// </summary>
        /// <param name="wrapped"></param>
        public ProfilingViewEngine(IViewEngine wrapped)
        {
            this.wrapped = wrapped;
        }


        private ViewEngineResult Find(ControllerContext controllerContext, string name, Func<ViewEngineResult> finder, bool isPartial)
        {
            var profiler = MiniProfiler.Current;
            IDisposable block = null;
            var key = "find-view-or-partial";

            if (profiler != null)
            {
                block = HttpContext.Current.Items[key] as IDisposable;
                if (block == null)
                {
                    HttpContext.Current.Items[key] = block = profiler.Step("Find: " + name);
                }
            }

            var found = finder();
            if (found != null && found.View != null)
            {
                found = new ViewEngineResult(new WrappedView(found.View, name, isPartial: isPartial), this);

                if (found != null && block != null)
                {
                    block.Dispose();
                    HttpContext.Current.Items[key] = null;
                }
            }

            if (found == null && block != null && this == ViewEngines.Engines.Last())
            {
                block.Dispose();
                HttpContext.Current.Items[key] = null;
            }

            return found;
        }


        /// <summary>
        /// Find a partial
        /// </summary>
        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            return Find(controllerContext, partialViewName, () => wrapped.FindPartialView(controllerContext, partialViewName, useCache), isPartial: true);
        }

        /// <summary>
        /// Find a view
        /// </summary>
        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            return Find(controllerContext, viewName, () => wrapped.FindView(controllerContext, viewName, masterName, useCache), isPartial: false);
        }

        /// <summary>
        /// Find a partial
        /// </summary>
        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            wrapped.ReleaseView(controllerContext, view);
        }
    }
}

#endif