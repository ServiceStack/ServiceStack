#if !NETSTANDARD1_6

using System.Web;
using ServiceStack.MiniProfiler.Helpers;
//using System.Web.Routing;

namespace ServiceStack.MiniProfiler
{
    /// <summary>
    /// HttpContext based profiler provider.  This is the default provider to use in a web context.
    /// The current profiler is associated with a HttpContext.Current ensuring that profilers are 
    /// specific to a individual HttpRequest.
    /// </summary>
    public partial class WebRequestProfilerProvider : BaseProfilerProvider
    {
        /// <summary>
        /// Public constructor.  This also registers any UI routes needed to display results
        /// </summary>
        public WebRequestProfilerProvider()
        {
			//UI.MiniProfilerHandler.RegisterRoutes();
        }

        /// <summary>
        /// Starts a new MiniProfiler and associates it with the current <see cref="HttpContext.Current"/>.
        /// </summary>
        public override Profiler Start(ProfileLevel level)
        {
            var context = HttpContext.Current;
            if (context == null) return null;

            var url = context.Request.Url;
            var path = context.Request.AppRelativeCurrentExecutionFilePath.Substring(1);

            // don't profile /content or /scripts, either - happens in web.dev
            foreach (var ignored in Profiler.Settings.IgnoredPaths.Safe())
            {
                if (path.ToUpperInvariant().Contains((ignored ?? "").ToUpperInvariant()))
                    return null;
            }

            var result = new Profiler(url.OriginalString, level);
            Current = result;

            SetProfilerActive(result);

            // don't really want to pass in the context to MiniProfler's constructor or access it statically in there, either
            result.User = (Settings.UserProvider ?? new IpAddressIdentity()).GetUser(context.Request);

            return result;
        }


        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="Profiler.Current"/> for this HttpContext, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public override void Stop(bool discardResults)
        {
            var context = HttpContext.Current;
            if (context == null)
                return;

            var current = Current;
            if (current == null)
                return;

            // stop our timings - when this is false, we've already called .Stop before on this session
            if (!StopProfiler(current))
                return;

            if (discardResults)
            {
                Current = null;
                return;
            }

            var request = context.Request;
            var response = context.Response;

            // set the profiler name to Controller/Action or /url
            EnsureName(current, request);

            // save the profiler
            SaveProfiler(current);

            try
            {
                var arrayOfIds = Profiler.Settings.Storage.GetUnviewedIds(current.User).ToJson();
                // allow profiling of ajax requests
                response.AppendHeader("X-MiniProfiler-Ids", arrayOfIds);
            }
            catch { } // headers blew up
        }


        /// <summary>
        /// Makes sure 'profiler' has a Name, pulling it from route data or url.
        /// </summary>
        private static void EnsureName(Profiler profiler, HttpRequest request)
        {
            // also set the profiler name to Controller/Action or /url
            if (profiler.Name.IsNullOrWhiteSpace())
            {
                //var rc = request.RequestContext;
                //RouteValueDictionary values;

                //if (rc != null && rc.RouteData != null && (values = rc.RouteData.Values).Count > 0)
                //{
                //    var controller = values["Controller"];
                //    var action = values["Action"];

                //    if (controller != null && action != null)
                //        profiler.Name = controller.ToString() + "/" + action.ToString();
                //}

                if (profiler.Name.IsNullOrWhiteSpace())
                {
                    profiler.Name = request.Path ?? "";
                    if (profiler.Name.Length > 70)
                        profiler.Name = profiler.Name.Remove(70);
                }
            }
        }

        /// <summary>
        /// Returns the current profiler
        /// </summary>
        /// <returns></returns>
        public override Profiler GetCurrentProfiler()
        {
            return Current;
        }


        private const string CacheKey = ":mini-profiler:";

        /// <summary>
        /// Gets the currently running MiniProfiler for the current HttpContext; null if no MiniProfiler was <see cref="Start"/>ed.
        /// </summary>
        private Profiler Current
        {
            get
            {
                var context = HttpContext.Current;
                return context?.Items[CacheKey] as Profiler;
            }
            set
            {
                var context = HttpContext.Current;
                if (context == null) return;

                context.Items[CacheKey] = value;
            }
        }
    }
}

#endif