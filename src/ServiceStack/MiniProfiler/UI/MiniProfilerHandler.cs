using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using ServiceStack.Text;
using ServiceStack.MiniProfiler.Helpers;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Support;
using HttpRequestWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpRequestWrapper;
using HttpResponseWrapper = ServiceStack.WebHost.Endpoints.Extensions.HttpResponseWrapper;

namespace ServiceStack.MiniProfiler.UI
{
	/// <summary>
	/// Understands how to route and respond to MiniProfiler UI urls.
	/// </summary>
	public class MiniProfilerHandler : /*IRouteHandler, */ IHttpHandler, IServiceStackHttpHandler
	{
		public static IHttpHandler MatchesRequest(IHttpRequest request)
		{
			var file = GetFileNameWithoutExtension(request.PathInfo);
			return file != null && file.StartsWith("ssr-")
				? new MiniProfilerHandler()
				: null;
		}

		public static HtmlString RenderIncludes(Profiler profiler, RenderPosition? position = null, bool? showTrivial = null, bool? showTimeWithChildren = null, int? maxTracesToShow = null, bool xhtml = false, bool? showControls = null, string path = null)
		{
			const string format =
@"<link rel=""stylesheet"" type=""text/css"" href=""{path}ssr-includes.css?v={version}""{closeXHTML}>
<script type=""text/javascript"">
    if (!window.jquip) document.write(unescape(""%3Cscript src='{path}ssr-jquip.all.js?v={version}' type='text/javascript'%3E%3C/script%3E""));
</script>
<script type=""text/javascript"" src=""{path}ssr-includes.js?v={version}""></script>
<script type=""text/javascript"">
    jQuery(function() {{
        MiniProfiler.init({{
            ids: {ids},
            path: '{path}',
            version: '{version}',
            renderPosition: '{position}',
            showTrivial: {showTrivial},
            showChildrenTime: {showChildren},
            maxTracesToShow: {maxTracesToShow},
            showControls: {showControls}
        }});
    }});
</script>";

			var result = "";

			if (profiler != null)
			{
				// HACK: unviewed ids are added to this list during Storage.Save, but we know we haven't see the current one yet,
				// so go ahead and add it to the end - it's usually the only id, but if there was a redirect somewhere, it'll be there, too
				Profiler.Settings.EnsureStorageStrategy();
				var ids = Profiler.Settings.Storage.GetUnviewedIds(profiler.User);
				ids.Add(profiler.Id);

                path = (path ?? VirtualPathUtility.ToAbsolute(Profiler.Settings.RouteBasePath).EnsureTrailingSlash()) + EndpointHost.Config.ServiceStackHandlerFactoryPath;

				result = format.Format(new {
					//path = VirtualPathUtility.ToAbsolute(MiniProfiler.Settings.RouteBasePath).EnsureTrailingSlash(),
                    path = !string.IsNullOrEmpty(path) ? path.EnsureTrailingSlash() : "",
					version = Profiler.Settings.Version,
					ids = ids.ToJson(),
					position = (position ?? Profiler.Settings.PopupRenderPosition).ToString().ToLower(),
					showTrivial = showTrivial ?? Profiler.Settings.PopupShowTrivial ? "true" : "false",
					showChildren = showTimeWithChildren ?? Profiler.Settings.PopupShowTimeWithChildren ? "true" : "false",
					maxTracesToShow = maxTracesToShow ?? Profiler.Settings.PopupMaxTracesToShow,
					closeXHTML = xhtml ? "/" : "",
					showControls = showControls ?? Profiler.Settings.ShowControls ? "true" : "false"
				});
			}

			return new HtmlString(result);
		}

        public static string GetFileNameWithoutExtension(string pathInfo)
        {
            //Path.GetFileNameWithoutExtension() throws exception with illegal chars
            return pathInfo.SplitOnLast('.')[0].SplitOnLast('/').Last();
        }

		//internal static void RegisterRoutes()
		//{
		//    var urls = new[] 
		//    { 
		//        "mini-profiler-jquery.1.6.2.js",
		//        "mini-profiler-jquery.tmpl.beta1.js",
		//        "mini-profiler-includes.js", 
		//        "mini-profiler-includes.css", 
		//        "mini-profiler-includes.tmpl", 
		//        "mini-profiler-results"
		//    };
		//    var routes = RouteTable.Routes;
		//    var handler = new MiniProfilerHandler();
		//    var prefix = (MiniProfiler.Settings.RouteBasePath ?? "").Replace("~/", "").EnsureTrailingSlash();

		//    using (routes.GetWriteLock())
		//    {
		//        foreach (var url in urls)
		//        {
		//            var route = new Route(prefix + url, handler)
		//            {
		//                // we have to specify these, so no MVC route helpers will match, e.g. @Html.ActionLink("Home", "Index", "Home")
		//                Defaults = new RouteValueDictionary(new { controller = "MiniProfilerHandler", action = "ProcessRequest" })
		//            };

		//            // put our routes at the beginning, like a boss
		//            routes.Insert(0, route);
		//        }
		//    }
		//}

		/// <summary>
		/// Returns this <see cref="MiniProfilerHandler"/> to handle <paramref name="requestContext"/>.
		/// </summary>
		//public IHttpHandler GetHttpHandler(RequestContext requestContext)
		//{
		//    return this; // elegant? I THINK SO.
		//}

		/// <summary>
		/// Try to keep everything static so we can easily be reused.
		/// </summary>
		public bool IsReusable
		{
			get { return true; }
		}

		/// <summary>
		/// Returns either includes' css/javascript or results' html.
		/// </summary>
		public void ProcessRequest(HttpContext context)
		{
			string path = context.Request.AppRelativeCurrentExecutionFilePath;
			ProcessRequest(
				new HttpRequestWrapper(null, context.Request),
				new HttpResponseWrapper(context.Response),
				null);
		}

		public void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			var path = httpReq.PathInfo;

			string output;
			switch (Path.GetFileNameWithoutExtension(path))
			{
				//case "mini-profiler-jquery.1.6.2":
				//case "mini-profiler-jquery.tmpl.beta1":
				case "ssr-jquip.all":
				case "ssr-includes":
					output = Includes(httpReq, httpRes, path);
					break;

				case "ssr-results":
					output = Results(httpReq, httpRes);
					break;

				default:
					output = NotFound(httpRes);
					break;
			}

			httpRes.Write(output);
		}

		/// <summary>
		/// Handles rendering static content files.
		/// </summary>
		private static string Includes(IHttpRequest httpReq, IHttpResponse httpRes, string path)
		{
			var response = httpRes;

			switch (Path.GetExtension(path))
			{
				case ".js":
					response.ContentType = "application/javascript";
					break;
				case ".css":
					response.ContentType = "text/css";
					break;
				case ".tmpl":
					response.ContentType = "text/x-jquery-tmpl";
					break;
				default:
					return NotFound(httpRes);
			}

			//var cache = response.Cache;
			//cache.SetCacheability(System.Web.HttpCacheability.Public);
			//cache.SetExpires(DateTime.Now.AddDays(7));
			//cache.SetValidUntilExpires(true);

			var embeddedFile = Path.GetFileName(path).Replace("ssr-", "");
			return GetResource(embeddedFile);
		}

		/// <summary>
		/// Handles rendering a previous MiniProfiler session, identified by its "?id=GUID" on the query.
		/// </summary>
		private static string Results(IHttpRequest httpReq, IHttpResponse httpRes)
		{
			// when we're rendering as a button/popup in the corner, we'll pass ?popup=1
			// if it's absent, we're rendering results as a full page for sharing
			var isPopup = httpReq.QueryString["popup"].IsNullOrWhiteSpace();

			// this guid is the MiniProfiler.Id property
			var id = new Guid();

			var validGuid = false;
			try
			{
				id = new Guid(httpReq.QueryString["id"]);
				validGuid = true;
			}
			catch { }

			if (!validGuid)
				return isPopup ? NotFound(httpRes) : NotFound(httpRes, "text/plain", "No Guid id specified on the query string");

			Profiler.Settings.EnsureStorageStrategy();
			var profiler = Profiler.Settings.Storage.Load(id);

			if (profiler == null)
				return isPopup ? NotFound(httpRes) : NotFound(httpRes, "text/plain", "No MiniProfiler results found with Id=" + id.ToString());

			// ensure that callers have access to these results
			var authorize = Profiler.Settings.Results_Authorize;
			if (authorize != null && !authorize(httpReq, profiler))
			{
				httpRes.StatusCode = 401;
				httpRes.ContentType = "text/plain";
				return "Unauthorized";
			}

			return isPopup ? ResultsJson(httpRes, profiler) : ResultsFullPage(httpRes, profiler);
		}

		private static string ResultsJson(IHttpResponse httpRes, Profiler profiler)
		{
			httpRes.ContentType = "application/json";
			return Profiler.ToJson(profiler);
		}

		private static string ResultsFullPage(IHttpResponse httpRes, Profiler profiler)
		{
			httpRes.ContentType = "text/html";
			return new StringBuilder()
				.AppendLine("<html><head>")
				.AppendFormat("<title>{0} ({1} ms) - MvcMiniProfiler Results</title>", profiler.Name, profiler.DurationMilliseconds)
				.AppendLine()
				.AppendLine("<script type='text/javascript' src='https://ajax.googleapis.com/ajax/libs/jquery/1.6.2/jquery.min.js'></script>")
				.Append("<script type='text/javascript'> var profiler = ")
				.Append(Profiler.ToJson(profiler))
				.AppendLine(";</script>")
				.Append(RenderIncludes(profiler)) // figure out how to better pass display options
				.AppendLine("</head><body><div class='profiler-result-full'></div></body></html>")
				.ToString();
		}

		private static string GetResource(string filename)
		{
			filename = filename.ToLower();
			string result;

			if (!_ResourceCache.TryGetValue(filename, out result))
			{
				using (var stream = typeof(MiniProfilerHandler).Assembly.GetManifestResourceStream("ServiceStack.MiniProfiler.UI." + filename))
				using (var reader = new StreamReader(stream))
				{
					result = reader.ReadToEnd();
				}

				_ResourceCache[filename] = result;
			}

			return result;
		}

		/// <summary>
		/// Embedded resource contents keyed by filename.
		/// </summary>
		private static readonly Dictionary<string, string> _ResourceCache = new Dictionary<string, string>();

		/// <summary>
		/// Helper method that sets a proper 404 response code.
		/// </summary>
		private static string NotFound(IHttpResponse httpRes, string contentType = "text/plain", string message = null)
		{
			httpRes.StatusCode = 404;
			httpRes.ContentType = contentType;

			return message;
		}
	}
}
