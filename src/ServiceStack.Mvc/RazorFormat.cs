﻿#if NETSTANDARD

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host.Handlers;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Platforms;
using ServiceStack.Redis;
using ServiceStack.Script;
using ServiceStack.VirtualPath;
using ServiceStack.Web;
using ServiceStack.Text;

namespace ServiceStack.Mvc
{
    public class RazorFormat : IPlugin, Html.IViewEngine
    {
        public static ILog log = LogManager.GetLogger(typeof(RazorFormat));

        public static string DefaultLayout { get; set; } = "_Layout";

        public List<string> ViewLocations { get; set; }

        public string PagesPath { get; set; } = "~/Views/Pages";

        private const string ErrorMvcNotInit = "MVC Services have not been configured, Please add `services.AddMvc()` to StartUp.ConfigureServices()";

        public IRazorViewEngine ViewEngine => viewEngine ?? throw new Exception(ErrorMvcNotInit);

        public bool DisablePageBasedRouting { get; set; }

        IRazorViewEngine viewEngine;
        ITempDataProvider tempDataProvider;

        public static List<string> GetDefaultViewLocations(IVirtualFiles virtualFiles)
        {
            var views = virtualFiles.GetDirectory("Views");
            if (views == null)
                return new List<string> { "~/Views" };

            var files = views.GetAllMatchingFiles("*.cshtml");
            var folders = files.Map(x => x.VirtualPath.LastLeftPart("/"));
            var locations = folders.Distinct().Map(x => "~/" + x);
            return locations;
        }

        public void Register(IAppHost appHost)
        {
            if (ViewLocations == null)
                ViewLocations = GetDefaultViewLocations(appHost.VirtualFiles);

            appHost.CatchAllHandlers.Add(CatchAllHandler);
            appHost.ViewEngines.Add(this);

            viewEngine = appHost.TryResolve<IRazorViewEngine>();
            tempDataProvider = appHost.TryResolve<ITempDataProvider>();

            if (!DisablePageBasedRouting)
            {
                appHost.FallbackHandlers.Add(PageBasedRoutingHandler);
            }
            
            if (viewEngine == null || tempDataProvider == null)
                throw new Exception(ErrorMvcNotInit);
        }

        public System.Web.IHttpHandler CatchAllHandler(string httpMethod, string pathInfo, string filepath)
        {
            var viewEngineResult = GetPageFromPathInfo(pathInfo);

            return viewEngineResult != null
                ? new RazorHandler(viewEngineResult)
                : null;
        }

        public ViewEngineResult GetPageFromPathInfo(string pathInfo)
        {
            if (pathInfo.EndsWith("/"))
                pathInfo += "default.cshtml";

            var viewPath = "~/wwwroot".CombineWith(pathInfo);
            if (!viewPath.EndsWith(".cshtml"))
                viewPath += ".cshtml";

            var viewEngineResult = ViewEngine.GetView("", viewPath, 
                isMainPage: viewPath == "~/wwwroot/default.cshtml");

            if (!viewEngineResult.Success)
            {
                viewPath = PagesPath.CombineWith(pathInfo);
                if (!viewPath.EndsWith(".cshtml"))
                    viewPath += ".cshtml";

                viewEngineResult = ViewEngine.GetView("", viewPath,
                    isMainPage: viewPath == $"{PagesPath}/default.cshtml");
            }

            return viewEngineResult.Success 
                ? viewEngineResult 
                : null;
        }

        public bool HasView(string viewName, IRequest httpReq = null) => false;

        public string RenderPartial(string pageName, object model, bool renderHtml, StreamWriter writer = null,
            Html.IHtmlContext htmlHelper = null) => null;


        public string IndexPage { get; set; } = "default";

        protected virtual System.Web.IHttpHandler PageBasedRoutingHandler(string httpMethod, string pathInfo, string requestFilePath)
        {
            var extPos = pathInfo.LastIndexOf('.');
            if (extPos >= 0 && pathInfo.Substring(extPos) != ".cshtml")
                return null;
            
            var viewEngineResult = GetRoutingPage(pathInfo, out var args);
            return viewEngineResult != null
                ? new RazorHandler(viewEngineResult) { Args = args }
                : null;
        }

        public ViewEngineResult GetRoutingPage(string pathInfo, out Dictionary<string, object> routingArgs)
        {
            // Sync with SharpPagesFeature GetRoutingPage()

            var path = pathInfo.Trim('/');

            var vfs = HostContext.VirtualFileSources;

            int CompareByWeightedName(IVirtualNode a, IVirtualNode b)
            {
                var aIsWildPath = a.Name[0] == '_';
                var bIsWildPath = b.Name[0] == '_';

                if (aIsWildPath && !bIsWildPath)
                    return 1;
                if (bIsWildPath && !aIsWildPath)
                    return -1;

                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            }
            
            ViewEngineResult GetPageFromPath(IVirtualFile file, string[] pathParts, out Dictionary<string,object> args)
            {
                var viewEngineResult = GetPageFromPathInfo(file.VirtualPath);

                args = null;
                if (!viewEngineResult.Success)
                    return null;

                args = new Dictionary<string, object>();
                var filePath = file.VirtualPath.WithoutExtension();
                var fileParts = filePath.Split('/');

                for (var i = 0; i < pathParts.Length; i++)
                {
                    if (i >= fileParts.Length)
                        break;

                    var part = fileParts[i];
                    if (part[0] == '_')
                        args[part.Substring(1)] = pathParts[i];
                }

                return viewEngineResult;
            }

            List<IVirtualDirectory> GetCandidateDirs(IVirtualDirectory[] argDirs, string segment)
            {
                var exactDirMatches = new List<IVirtualDirectory>();
                var candidateDirs = new List<IVirtualDirectory>();
                foreach (var parentDir in argDirs)
                {
                    var parentDirs = parentDir.GetDirectories().ToArray();
                    foreach (var dir in parentDirs)
                    {
                        if (segment.EqualsIgnoreCase(dir.Name))
                            exactDirMatches.Add(dir);
                        else if (dir.Name[0] == '_')
                            candidateDirs.Add(dir);
                    }
                }
                return exactDirMatches.Count > 0 ? exactDirMatches : candidateDirs;
            }

            var dirs = vfs.GetAllRootDirectories();
            
            var segCounts = path.CountOccurrencesOf('/');

            var index = 0;
            var pathSegments = path.Split('/');

            foreach (var segment in pathSegments)
            {
                var isLast = index++ == segCounts;
                if (isLast)
                {
                    foreach (var dir in dirs)
                    {
                        foreach (var file in dir.GetFiles())
                        {
                            var isWildPath = file.Name[0] == '_';
                            if (isWildPath)
                            {
                                if (file.Name.IndexOf("layout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    file.Name.IndexOf("partial", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    file.Name.IndexOf("viewimports", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    file.Name.IndexOf("viewstart", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    file.Name.StartsWith("_init"))                                
                                    continue;
                            }

                            var fileNameWithoutExt = file.Name.WithoutExtension();
                            if (fileNameWithoutExt == "index")
                                continue;
                                
                            if (file.Extension == "cshtml")
                            {
                                if (fileNameWithoutExt == segment || isWildPath)
                                {
                                    var result = GetPageFromPath(file, pathSegments, out routingArgs);
                                    if (result != null)
                                        return result;
                                }
                            }
                        }
                    }
                }

                var candidateDirs = GetCandidateDirs(dirs, segment);
                if (candidateDirs.Count == 0)
                    break;
                
                dirs = candidateDirs.ToArray();
                Array.Sort(dirs, CompareByWeightedName);

                if (isLast)
                {
                    foreach (var dir in dirs)
                    {
                        var file = dir.GetFile(IndexPage + ".cshtml");
                        if (file != null)
                        {
                            var result = GetPageFromPath(file, pathSegments, out routingArgs);
                            if (result != null)
                                return result;
                        }
                    }
                }
            }

            routingArgs = null;
            return null;
        }

        private const string RenderException = "RazorFormat.Exception";

        public async Task<bool> ProcessRequestAsync(IRequest req, object dto, Stream outputStream)
        {
            var explicitView = req.GetView();
            
            if (dto is IHttpResult httpResult)
            {
                dto = httpResult.Response;
                if (httpResult is HttpResult viewResult && viewResult.View != null)
                    explicitView = viewResult.View;
            }

            if (explicitView == null && (req.Dto == null || dto == null))
                return false;

            if (req.Items.ContainsKey(RenderException))
                return false;

            var errorStatus = dto.GetResponseStatus() ?? 
                (dto is Exception ex ? ex.ToResponseStatus() : null);
            if (errorStatus?.ErrorCode != null)
                req.Items[Keywords.ErrorStatus] = errorStatus;

            var viewNames = new List<string>();
            if (explicitView != null)
                viewNames.Add(explicitView);

            if (req.Dto != null)
                viewNames.Add(req.Dto.GetType().Name);
            if (dto != null)
                viewNames.Add(dto.GetType().Name);

            var viewEngineResult = FindView(viewNames, out var routingArgs);
            if (viewEngineResult == null)
                return false;

            ViewDataDictionary viewData = null;

            if (errorStatus?.ErrorCode != null)
            {
                var razorView = viewEngineResult.View as RazorView;
                var genericDef = razorView.RazorPage.GetType().FirstGenericType();
                var modelType = genericDef?.GetGenericArguments()[0];
                if (modelType != null && modelType != typeof(object))
                {
                    var model = modelType.CreateInstance();
                    viewData = CreateViewData(model);
                }
            }

            if (viewData == null)
                viewData = CreateViewData(dto);

            if (routingArgs != null)
            {
                foreach (var entry in routingArgs)
                {
                    viewData[entry.Key] = entry.Value;
                }
            }

            await RenderView(req, outputStream, viewData, viewEngineResult.View, req.GetTemplate());

            return true;
        }

        private ViewEngineResult FindView(IEnumerable<string> viewNames, out Dictionary<string, object> routingArgs)
        {
            routingArgs = null;
            const string execPath = "";
            foreach (var viewName in viewNames)
            {
                if (viewName.StartsWith("/"))
                {
                    var viewEngineResult = GetPageFromPathInfo(viewName);                
                    if (viewEngineResult?.Success == true)
                        return viewEngineResult;
            
                    viewEngineResult = GetRoutingPage(viewName, out routingArgs);                
                    if (viewEngineResult?.Success == true)
                        return viewEngineResult;
                }
                else
                {
                    foreach (var location in ViewLocations)
                    {
                        var viewPath = location.CombineWith(viewName) + ".cshtml";
                        var viewEngineResult = ViewEngine.GetView(execPath, viewPath, isMainPage: false);
                        if (viewEngineResult?.Success == true)
                            return viewEngineResult;
                    }
                }
            }

            return null;
        }

        internal static ViewDataDictionary CreateViewData<T>(T model)
        {
            return new ViewDataDictionary<T>(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: new ModelStateDictionary())
            {
                Model = model
            };
        }

        internal async Task RenderView(IRequest req, Stream stream, ViewDataDictionary viewData, IView view, string layout=null)
        {
            var razorView = view as RazorView;
            try
            {
                var actionContext = new ActionContext(
                    ((HttpRequest) req.OriginalRequest).HttpContext,
                    new RouteData(),
                    new ActionDescriptor());

                var sw = new StreamWriter(stream);
                {
                    if (viewData == null)
                        viewData = CreateViewData((object)null);

                    // Use "_Layout" if unspecified
                    if (razorView != null)
                        razorView.RazorPage.Layout = layout ?? DefaultLayout;

                    // Allows Layout from being overridden in page with: Layout = Html.ResolveLayout("LayoutUnlessOverridden")
                    if (layout != null)
                        viewData["Layout"] = layout;

                    viewData[Keywords.IRequest] = req;

                    var viewContext = new ViewContext(
                        actionContext,
                        view,
                        viewData,
                        new TempDataDictionary(actionContext.HttpContext, tempDataProvider),
                        sw,
                        new HtmlHelperOptions());

                    await view.RenderAsync(viewContext);

                    await sw.FlushAsync();

                    try
                    {
                        using (razorView?.RazorPage as IDisposable) { }
                    }
                    catch (Exception ex)
                    {
                        log.Warn("Error trying to dispose Razor View: " + ex.Message, ex);
                    }
                }
            }
            catch (StopExecutionException) { }
            catch (Exception ex)
            {
                ex = ex.UnwrapIfSingleException();
                if (ex is StopExecutionException)
                    return;
                
                req.Items[RenderException] = ex;
                //Can't set HTTP Headers which are already written at this point
                await req.Response.WriteErrorBody(ex);
            }
        }
    }

    public class RazorHandler : ServiceStackHandlerBase
    {
        private readonly ViewEngineResult viewEngineResult;
        protected object Model { get; set; }
        protected string PathInfo { get; set; }
        
        public Dictionary<string, object> Args { get; set; }

        public RazorHandler(string pathInfo, object model = null)
        {
            this.PathInfo = pathInfo;
            this.Model = model;
        }

        public RazorHandler(ViewEngineResult viewEngineResult, object model = null)
        {
            this.viewEngineResult = viewEngineResult;
            this.Model = model;
        }

        public override async Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(req, res))
                return;

            var format = HostContext.GetPlugin<RazorFormat>();
            try
            {
                var view = viewEngineResult?.View;
                if (view == null)
                {
                    if (PathInfo == null)
                        throw new ArgumentNullException(nameof(PathInfo));

                    //If resolving from PathInfo, same RazorPage is used so must fetch new instance each time
                    var viewResult = format.GetPageFromPathInfo(PathInfo);
                    view = viewResult?.View ?? throw new ArgumentException("Could not find Razor Page at " + PathInfo);
                }

                await RenderView(format, req, res, view, Args);
            }
            catch (Exception ex)
            {
                //Can't set HTTP Headers which are already written at this point
                await req.Response.WriteErrorBody(ex);
            }
        }

        private async Task RenderView(RazorFormat format, IRequest req, IResponse res, IView view, Dictionary<string, object> args=null)
        {
            res.ContentType = MimeTypes.Html;
            var model = Model;
            if (model == null)
                req.Items.TryGetValue("Model", out model);

            ViewDataDictionary viewData = null;
            if (model == null)
            {
                var razorView = view as RazorView;
                var genericDef = razorView.RazorPage.GetType().FirstGenericType();
                var modelType = genericDef?.GetGenericArguments()[0];
                if (modelType != null && modelType != typeof(object))
                {
                    model = await DeserializeHttpRequestAsync(modelType, req, req.ContentType);
                    viewData = RazorFormat.CreateViewData(model);
                }
            }

            if (viewData == null)
            {
                viewData = new ViewDataDictionary<object>(
                    metadataProvider: new EmptyModelMetadataProvider(),
                    modelState: new ModelStateDictionary());

                foreach (var cookie in req.Cookies)
                {
                    viewData[cookie.Key] = cookie.Value.Value;
                }
                foreach (var header in req.Headers.AllKeys)
                {
                    viewData[header] = req.Headers[header];
                }
                foreach (var key in req.QueryString.AllKeys)
                {
                    viewData[key] = req.QueryString[key];
                }
                foreach (var key in req.FormData.AllKeys)
                {
                    viewData[key] = req.FormData[key];
                }
                foreach (var entry in req.Items)
                {
                    viewData[entry.Key] = entry.Value;
                }

                if (args != null)
                {
                    foreach (var entry in args)
                    {
                        viewData[entry.Key] = entry.Value;
                    }
                }
            }

            await format.RenderView(req, res.OutputStream, viewData, view);
        }
    }

    public static class RazorViewExtensions
    {
        public static HtmlString AsRawJson<T>(this T model)
        {
            var json = !Equals(model, default(T)) ? model.ToJson() : "null";
            return new HtmlString(json);
        }

        public static HtmlString AsRaw<T>(this T model)
        {
            return new HtmlString(
                (model != null ? model : default(T))?.ToString());
        }

        public static string GetErrorHtml(ResponseStatus responseStatus)
        {
            if (responseStatus == null) return null;

            var stackTrace = responseStatus.StackTrace != null
                ? "<pre>" + responseStatus.StackTrace + "</pre>"
                : "";

            var html = @"
                <div id=""error-response"" class=""alert alert-danger"">
                    <h4>" +
                        responseStatus.ErrorCode + ": " +
                        responseStatus.Message + @"
                    </h4>" +
                    stackTrace +
                "</div>";

            return html;
        }

        public static IRequest GetRequest(this IHtmlHelper htmlHelper)
        {
            return htmlHelper.ViewContext.ViewData[Keywords.IRequest] as IRequest
                ?? HostContext.AppHost.TryGetCurrentRequest();
        }

        public static IResponse GetResponse(this IHtmlHelper htmlHelper) => 
            htmlHelper.GetRequest().Response;

        public static HttpRequest GetHttpRequest(this IHtmlHelper htmlHelper) =>
            htmlHelper.ViewContext.HttpContext.Request;

        public static HttpResponse GetHttpResponse(this IHtmlHelper htmlHelper) =>
            htmlHelper.ViewContext.HttpContext.Response;

        public static IHtmlContent PartialMarkdown(this IHtmlHelper htmlHelper, string partial)
        {
            var req = htmlHelper.GetRequest();
            var pathInfo = req.PathInfo;
            if (pathInfo != null)
            {
                var dir = pathInfo.LastLeftPart("/");
                var partialPath = dir.CombineWith(partial) + ".md";
                partialPath = partialPath.TrimPrefixes("/");

                var viewPaths = new[]
                {
                    partialPath,
                    $"wwwroot/{partialPath}",
                    $"Views/Shared/{partial}.md",
                    $"Views/{partial}.md",
                };

                foreach (var path in viewPaths)
                {
                    var file = HostContext.AppHost.VirtualFiles.GetFile(path);
                    if (file != null)
                        return htmlHelper.RenderMarkdown(file.ReadAllText());
                }
            }

            return new HtmlString($"{partial} not found");
        }

        public static IHtmlContent RenderMarkdown(this IHtmlHelper htmlHelper, string markdown)
        {
            return new HtmlString(MarkdownConfig.Transform(markdown));
        }

        public static string ResolveLayout(this IHtmlHelper htmlHelper, string defaultLayout)
        {
            if (htmlHelper.ViewData["Layout"] is string layout)
                return layout;

            var template = htmlHelper.GetRequest()?.GetTemplate();
            return template ?? defaultLayout;
        }

        public static string GetQueryString(this IHtmlHelper htmlHelper, string paramName)
        {
            return htmlHelper.GetHttpRequest().Query[paramName];
        }

        public static string GetFormData(this IHtmlHelper htmlHelper, string paramName)
        {
            var req = htmlHelper.GetHttpRequest();
            return req.HasFormContentType 
                ? req.Form[paramName].FirstOrDefault() 
                : null;
        }

        public static HtmlString IncludeFile(this IHtmlHelper htmlHelper, string virtualPath)
        {
            var file = HostContext.VirtualFileSources.GetFile(virtualPath);
            return file != null
                ? new HtmlString(file.ReadAllText())
                : HtmlString.Empty;
        }

        public static HtmlString ToHtmlString(this string str) => str == null ? HtmlString.Empty : new HtmlString(str);

        public static object GetItem(this IHtmlHelper html, string key) =>
            html.GetRequest().GetItem(key);

        public static ResponseStatus GetErrorStatus(this IHtmlHelper html) =>
            ViewUtils.GetErrorStatus(html.GetRequest());

        public static bool HasErrorStatus(this IHtmlHelper html) =>
            ViewUtils.HasErrorStatus(html.GetRequest());
        
        public static string Form(this IHtmlHelper html, string name) => html.GetRequest().FormData[name];
        public static string Query(this IHtmlHelper html, string name) => html.GetRequest().QueryString[name];
        
        public static string FormQuery(this IHtmlHelper html, string name)
        {
            var req = html.GetRequest();
            return req.FormData[name] ?? req.QueryString[name];
        }

        public static string[] FormQueryValues(this IHtmlHelper html, string name) =>
            ViewUtils.FormQueryValues(html.GetRequest(), name);

        public static string FormValue(this IHtmlHelper html, string name) => 
            ViewUtils.FormValue(html.GetRequest(), name, null);

        public static string FormValue(this IHtmlHelper html, string name, string defaultValue) =>
            ViewUtils.FormValue(html.GetRequest(), name, defaultValue);

        public static string[] FormValues(this IHtmlHelper html, string name) =>
            ViewUtils.FormValues(html.GetRequest(), name);

        public static bool FormCheckValue(this IHtmlHelper html, string name) =>
            ViewUtils.FormCheckValue(html.GetRequest(), name);

        public static string GetParam(this IHtmlHelper html, string name) =>
            ViewUtils.GetParam(html.GetRequest(), name);

        public static string ErrorResponseExcept(this IHtmlHelper html, string fieldNames) =>
            ViewUtils.ErrorResponseExcept(html.GetErrorStatus(), fieldNames);

        public static string ErrorResponseExcept(this IHtmlHelper html, ICollection<string> fieldNames) =>
            ViewUtils.ErrorResponseExcept(html.GetErrorStatus(), fieldNames);

        public static string ErrorResponseSummary(this IHtmlHelper html) =>
            ViewUtils.ErrorResponseSummary(html.GetErrorStatus());
        public static string ErrorResponseSummary(this IHtmlHelper html, string exceptFor) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), exceptFor);

        public static string ErrorResponse(this IHtmlHelper html, string fieldName) =>
            ViewUtils.ErrorResponse(html.GetErrorStatus(), fieldName);

        public static bool IsDebug(this IHtmlHelper html) => HostContext.DebugMode;

        public static IAuthSession GetSession(this IHtmlHelper html) => html.GetRequest().GetSession();

        public static bool IsAuthenticated(this IHtmlHelper html) =>
            html.GetSession().IsAuthenticated;
        public static string UserProfileUrl(this IHtmlHelper html) =>
            html.GetSession().GetProfileUrl();


        /// <summary>
        /// Alias for ServiceStack Html.ValidationSummary() with comma-delimited field names 
        /// </summary>
        public static HtmlString ErrorSummary(this IHtmlHelper html, string exceptFor) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), exceptFor).ToHtmlString();
        public static HtmlString ErrorSummary(this IHtmlHelper html) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), null).ToHtmlString();
        public static HtmlString ErrorSummary(this IHtmlHelper html, ICollection<string> exceptFields) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), exceptFields, null).ToHtmlString();
        public static HtmlString ErrorSummary(this IHtmlHelper html, ICollection<string> exceptFields, Dictionary<string, object> divAttrs) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), exceptFields, divAttrs).ToHtmlString();
        public static HtmlString ErrorSummary(this IHtmlHelper html, ICollection<string> exceptFields, object divAttrs) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), exceptFields, divAttrs.ToObjectDictionary()).ToHtmlString();

        public static HtmlString ValidationSummary(this IHtmlHelper html, ICollection<string> exceptFields) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), exceptFields, null).ToHtmlString();
        public static HtmlString ValidationSummary(this IHtmlHelper html, ICollection<string> exceptFields, Dictionary<string, object> divAttrs) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), exceptFields, divAttrs).ToHtmlString();
        public static HtmlString ValidationSummary(this IHtmlHelper html, ICollection<string> exceptFields, object divAttrs) =>
            ViewUtils.ValidationSummary(html.GetErrorStatus(), exceptFields, divAttrs.ToObjectDictionary()).ToHtmlString();

        public static HtmlString ValidationSuccess(this IHtmlHelper html, string message) => html.ValidationSuccess(message, null);
        public static HtmlString ValidationSuccess(this IHtmlHelper html, string message, Dictionary<string,object> divAttrs)
        {
            var errorStatus = html.GetErrorStatus();
            if (message == null 
                || errorStatus != null
                || html.GetRequest().Verb == HttpMethods.Get)
                return null; 

            return ViewUtils.ValidationSuccess(message, divAttrs).ToHtmlString();
        }

        public static HtmlString HiddenInputs(this IHtmlHelper html, IEnumerable<KeyValuePair<string, string>> kvps) =>
            ViewUtils.HtmlHiddenInputs(kvps.ToObjectDictionary()).ToHtmlString();
        public static HtmlString HiddenInputs(this IHtmlHelper html, IEnumerable<KeyValuePair<string, object>> kvps) =>
            ViewUtils.HtmlHiddenInputs(kvps).ToHtmlString();
        public static HtmlString HiddenInputs(this IHtmlHelper html, object kvps) =>
            ViewUtils.HtmlHiddenInputs(kvps.ToObjectDictionary()).ToHtmlString();

        public static HtmlString FormTextarea(this IHtmlHelper html, object inputAttrs) =>
            FormControl(html, inputAttrs.ToObjectDictionary(), "textarea", null);
        public static HtmlString FormTextarea(this IHtmlHelper html, Dictionary<string, object> inputAttrs) =>
            FormControl(html, inputAttrs, "textarea", null);
        public static HtmlString FormTextarea(this IHtmlHelper html, object inputAttrs, InputOptions inputOptions) =>
            FormControl(html, inputAttrs.ToObjectDictionary(), "textarea", inputOptions);
        public static HtmlString FormTextarea(this IHtmlHelper html, Dictionary<string, object> inputAttrs, InputOptions inputOptions) =>
            FormControl(html, inputAttrs, "textarea", inputOptions);

        public static HtmlString FormSelect(this IHtmlHelper html, object inputAttrs) =>
            FormControl(html, inputAttrs.ToObjectDictionary(), "select", null);
        public static HtmlString FormSelect(this IHtmlHelper html, Dictionary<string, object> inputAttrs) =>
            FormControl(html, inputAttrs, "select", null);
        public static HtmlString FormSelect(this IHtmlHelper html, object inputAttrs, InputOptions inputOptions) =>
            FormControl(html, inputAttrs.ToObjectDictionary(), "select", inputOptions);
        public static HtmlString FormSelect(this IHtmlHelper html, Dictionary<string, object> inputAttrs, InputOptions inputOptions) =>
            FormControl(html, inputAttrs, "select", inputOptions);

        public static HtmlString FormInput(this IHtmlHelper html, object inputAttrs) =>
            FormControl(html, inputAttrs.ToObjectDictionary(), "input", null);
        public static HtmlString FormInput(this IHtmlHelper html, Dictionary<string, object> inputAttrs) =>
            FormControl(html, inputAttrs, "input", null);
        public static HtmlString FormInput(this IHtmlHelper html, object inputAttrs, InputOptions inputOptions) =>
            FormControl(html, inputAttrs.ToObjectDictionary(), "input", inputOptions);
        public static HtmlString FormInput(this IHtmlHelper html, Dictionary<string, object> inputAttrs, InputOptions inputOptions) =>
            FormControl(html, inputAttrs, "input", inputOptions);

        public static HtmlString FormControl(this IHtmlHelper html, object inputAttrs, string tagName, InputOptions inputOptions) =>
            ViewUtils.FormControl(html.GetRequest(), inputAttrs.ToObjectDictionary(), tagName, inputOptions).ToHtmlString();
        public static HtmlString FormControl(this IHtmlHelper html, Dictionary<string, object> inputAttrs, string tagName, InputOptions inputOptions) =>
            ViewUtils.FormControl(html.GetRequest(), inputAttrs, tagName, inputOptions).ToHtmlString();

        public static HtmlString BundleJs(this IHtmlHelper html, BundleOptions options) => ViewUtils.BundleJs(
            nameof(BundleJs), HostContext.VirtualFileSources, HostContext.VirtualFiles, Minifiers.JavaScript, options).ToHtmlString();

        public static HtmlString BundleCss(this IHtmlHelper html, BundleOptions options) => ViewUtils.BundleCss(
            nameof(BundleCss), HostContext.VirtualFileSources, HostContext.VirtualFiles, Minifiers.Css, options).ToHtmlString();

        public static HtmlString BundleHtml(this IHtmlHelper html, BundleOptions options) => ViewUtils.BundleHtml(
            nameof(BundleHtml), HostContext.VirtualFileSources, HostContext.VirtualFiles, Minifiers.Html, options).ToHtmlString();

        public static T Exec<T>(this IHtmlHelper html, Func<T> fn, out Exception ex)
        {
            try
            {
                ex = null;
                return fn();
            }
            catch (Exception e)
            {
                ex = e;
                return default(T);
            }
        }

        public static string TextDump(this IHtmlHelper html, object target) => target.TextDump();
        public static string TextDump(this IHtmlHelper html, object target, TextDumpOptions options) => target.TextDump(options);

        public static HtmlString HtmlDump(this IHtmlHelper html, object target) => ViewUtils.HtmlDump(target).ToHtmlString();
        public static HtmlString HtmlDump(this IHtmlHelper html, object target, HtmlDumpOptions options) => 
            ViewUtils.HtmlDump(target,options).ToHtmlString();

        public static List<NavItem> GetNavItems(this IHtmlHelper html) => ViewUtils.NavItems;
        public static List<NavItem> GetNavItems(this IHtmlHelper html, string key) => ViewUtils.GetNavItems(key);

        public static HtmlString Nav(this IHtmlHelper html) => html.Nav(ViewUtils.NavItems, null);
        public static HtmlString Nav(this IHtmlHelper html, NavOptions options) => html.Nav(ViewUtils.NavItems, options);
        public static HtmlString Nav(this IHtmlHelper html, List<NavItem> navItems) => html.Nav(navItems, null);
        public static HtmlString Nav(this IHtmlHelper html, List<NavItem> navItems, NavOptions options) =>
            ViewUtils.Nav(navItems, options.ForNav().WithDefaults(html.GetRequest())).ToHtmlString();

        public static HtmlString Navbar(this IHtmlHelper html) => html.Navbar(ViewUtils.NavItems, null);
        public static HtmlString Navbar(this IHtmlHelper html, NavOptions options) => html.Navbar(ViewUtils.NavItems, options);
        public static HtmlString Navbar(this IHtmlHelper html, List<NavItem> navItems) => html.Navbar(navItems, null);
        public static HtmlString Navbar(this IHtmlHelper html, List<NavItem> navItems, NavOptions options) =>
            ViewUtils.Nav(navItems, options.ForNavbar().WithDefaults(html.GetRequest())).ToHtmlString();

        public static HtmlString NavLink(this IHtmlHelper html, NavItem navItem) => html.NavLink(navItem, null);
        public static HtmlString NavLink(this IHtmlHelper html, NavItem navItem, NavOptions options) =>
            ViewUtils.NavLink(navItem, options.ForNavLink().WithDefaults(html.GetRequest())).ToHtmlString();

        public static HtmlString NavButtonGroup(this IHtmlHelper html) => html.NavButtonGroup(ViewUtils.NavItems, null);
        public static HtmlString NavButtonGroup(this IHtmlHelper html, NavOptions options) => html.NavButtonGroup(ViewUtils.NavItems, options);
        public static HtmlString NavButtonGroup(this IHtmlHelper html, List<NavItem> navItems) => html.NavButtonGroup(navItems, null);
        public static HtmlString NavButtonGroup(this IHtmlHelper html, List<NavItem> navItems, NavOptions options) =>
            ViewUtils.NavButtonGroup(navItems, options.ForNavButtonGroup().WithDefaults(html.GetRequest())).ToHtmlString();

        public static HtmlString CssIncludes(this IHtmlHelper html, params string[] cssFiles) =>
            ViewUtils.CssIncludes(HostContext.VirtualFileSources, cssFiles.ToList()).ToHtmlString();
        public static HtmlString JsIncludes(this IHtmlHelper html, params string[] jsFiles) =>
            ViewUtils.CssIncludes(HostContext.VirtualFileSources, jsFiles.ToList()).ToHtmlString();

        public static HtmlString SvgImage(this IHtmlHelper html, string name) => Svg.GetImage(name).ToHtmlString();
        public static HtmlString SvgImage(this IHtmlHelper html, string name, string fillColor) => Svg.GetImage(name, fillColor).ToHtmlString();

        public static HtmlString SvgDataUri(this IHtmlHelper html, string name) => Svg.GetDataUri(name).ToHtmlString();
        public static HtmlString SvgDataUri(this IHtmlHelper html, string name, string fillColor) => Svg.GetDataUri(name, fillColor).ToHtmlString();

        public static HtmlString SvgBackgroundImageCss(this IHtmlHelper html, string name) => Svg.GetBackgroundImageCss(name).ToHtmlString();
        public static HtmlString SvgBackgroundImageCss(this IHtmlHelper html, string name, string fillColor) => Svg.GetBackgroundImageCss(name, fillColor).ToHtmlString();
        public static HtmlString SvgInBackgroundImageCss(this IHtmlHelper html, string svg) => Svg.InBackgroundImageCss(svg).ToHtmlString();
        
        public static HtmlString SvgFill(this IHtmlHelper html, string svg, string color) => Svg.Fill(svg, color).ToHtmlString();

        public static string SvgBaseUrl(this IHtmlHelper html) => html.GetRequest().ResolveAbsoluteUrl(HostContext.AssertPlugin<SvgFeature>().RoutePath);
        

        /* HTML Helpers for MVC Pages which don't inherit ServiceStack.Razor View Pages */
        
        public static HtmlString GetAbsoluteUrl(this IHtmlHelper html, string virtualPath) => 
            new HtmlString(HostContext.AppHost.ResolveAbsoluteUrl(virtualPath, html.GetRequest()));
        public static IServiceGateway Gateway(this IHtmlHelper html) =>
            HostContext.AppHost.GetServiceGateway(html.GetRequest());

        public static void RedirectIfNotAuthenticated(this IHtmlHelper html, string redirect = null) =>
            html.GetRequest().RedirectIfNotAuthenticated(redirect);

        internal static void RedirectIfNotAuthenticated(this IRequest req, string redirect = null)
        {
            if (req.GetSession().IsAuthenticated)
                return;

            redirect = redirect
               ?? AuthenticateService.HtmlRedirect
               ?? HostContext.Config.DefaultRedirectPath
               ?? HostContext.Config.WebHostUrl
               ?? "/";
            AuthenticateAttribute.DoHtmlRedirect(redirect, req, req.Response, includeRedirectParam: true);
            throw new StopExecutionException();
        }
        
        private static HtmlString WaitStopAsync(Func<Task> fn)
        {
            fn().Wait();
            return HtmlString.Empty;
        }

        public static Task RedirectToAsync(this IHtmlHelper html, string path) => html.GetRequest().RedirectToAsync(path);
        
        internal static async Task RedirectToAsync(this IRequest req, string path)
        {
            var result = new HttpResult(null, null, HttpStatusCode.Redirect) {
                Headers = {
                    [HttpHeaders.Location] = path.FirstCharEquals('~')
                        ? req.ResolveAbsoluteUrl(path)
                        : path
                }
            };
            await req.Response.WriteToResponse(req, result);
            throw new StopExecutionException();
        }

        public static HtmlString RedirectTo(this IHtmlHelper html, string path) => WaitStopAsync(() => 
            html.GetRequest().RedirectToAsync(path));

        internal static HtmlString RedirectTo(this IRequest req, string path) => WaitStopAsync(() => 
            req.RedirectToAsync(path));

        public static bool HasRole(this IHtmlHelper html, string role) => 
            html.GetRequest().GetSession().HasRole(role, html.GetRequest().TryResolve<IAuthRepository>());
        public static bool HasPermission(this IHtmlHelper html, string permission) => 
            html.GetRequest().GetSession().HasPermission(permission, html.GetRequest().TryResolve<IAuthRepository>());

        public static HtmlString AssertRole(this IHtmlHelper html, string role, string message = null, string redirect = null) =>
            WaitStopAsync(() => html.GetRequest().AssertRoleAsync(role:role, message:message, redirect:redirect));
        internal static HtmlString AssertRole(this IRequest req, string role, string message = null, string redirect = null) =>
            WaitStopAsync(() => req.AssertRoleAsync(role:role, message:message, redirect:redirect));

        public static Task AssertRoleAsync(this IHtmlHelper html, string role, string message = null, string redirect = null) =>
            html.GetRequest().AssertRoleAsync(role:role, message:message, redirect:redirect);
        internal static async Task AssertRoleAsync(this IRequest req, string role, string message = null, string redirect = null)
        {
            var session = req.GetSession();
            if (!session.IsAuthenticated)
            {
                req.RedirectIfNotAuthenticated(redirect);
                return;
            }

            if (!session.HasRole(role, req.TryResolve<IAuthRepository>()))
            {
                if (redirect != null)
                    await req.RedirectToAsync(redirect);
                        
                var error = new HttpError(HttpStatusCode.Forbidden, message ?? ErrorMessages.InvalidRole.Localize(req));
                await req.Response.WriteToResponse(req, error);
                throw new StopExecutionException();
            }
        }

        public static HtmlString AssertPermission(this IHtmlHelper html, string permission, string message = null, string redirect = null) =>
            WaitStopAsync(() => html.GetRequest().AssertPermissionAsync(permission:permission, message:message, redirect:redirect));
        internal static HtmlString AssertPermission(this IRequest req, string permission, string message = null, string redirect = null) =>
            WaitStopAsync(() => req.AssertPermissionAsync(permission:permission, message:message, redirect:redirect));

        public static Task AssertPermissionAsync(this IHtmlHelper html, string permission, string message = null, string redirect = null) =>
            html.GetRequest().AssertPermissionAsync(permission:permission, message:message, redirect:redirect);
        
        internal static async Task AssertPermissionAsync(this IRequest req, string permission, string message = null, string redirect = null)
        {
            var session = req.GetSession();
            if (!session.IsAuthenticated)
            {
                req.RedirectIfNotAuthenticated(redirect);
                return;
            }

            if (!session.HasPermission(permission, req.TryResolve<IAuthRepository>()))
            {
                if (redirect != null)
                    await req.RedirectToAsync(redirect);
                        
                var error = new HttpError(HttpStatusCode.Forbidden, message ?? ErrorMessages.InvalidPermission.Localize(req));
                await req.Response.WriteToResponse(req, error);
                throw new StopExecutionException();
            }
        }
    }

    public abstract class ViewPage : ViewPage<object>
    {
    }

    //Workaround base-class to fix R# intelli-sense issue
    public abstract class ResharperViewPage<T> : ViewPage<object>
    {
        public T Dto => (T) Model;
    }

    //Razor Pages still only work when base class is RazorPage<object>
    public abstract class ViewPage<T> : RazorPage<T>, IDisposable
    {
        public IHttpRequest Request
        {
            get
            {
                if (base.ViewContext.ViewData.TryGetValue(Keywords.IRequest, out var oRequest))
                    return (IHttpRequest)oRequest;

                return AppHostBase.GetOrCreateRequest(Context) as IHttpRequest;
            }
        }

        public IHttpResponse Response => (IHttpResponse)Request.Response;

        public string GetLayout(string defaultLayout) => ViewData["Layout"] as string ?? defaultLayout;

        public bool IsError => ModelError != null || GetErrorStatus() != null;

        public object ModelError { get; set; }

        public bool IsPostBack => this.Request.Verb == HttpMethods.Post;

        public ResponseStatus GetErrorStatus()
        {
            var errorStatus = this.Request.GetItem(Keywords.ErrorStatus);
            return errorStatus as ResponseStatus
                ?? GetResponseStatus(ModelError);
        }

        private static ResponseStatus GetResponseStatus(object response)
        {
            if (response == null)
                return null;

            if (response is ResponseStatus status)
                return status;

            if (response is IHasResponseStatus hasResponseStatus)
                return hasResponseStatus.ResponseStatus;

            var propertyInfo = response.GetType().GetProperty("ResponseStatus");
            return propertyInfo?.GetProperty(response) as ResponseStatus;
        }

        public HtmlString GetErrorMessage()
        {
            var errorStatus = GetErrorStatus();
            return errorStatus == null ? null : new HtmlString(errorStatus.Message);
        }

        public HtmlString GetAbsoluteUrl(string virtualPath)
        {
            return new HtmlString(AppHost.ResolveAbsoluteUrl(virtualPath, Request));
        }

        public void ApplyRequestFilters(object requestDto)
        {
            HostContext.ApplyRequestFiltersAsync(Request, Response, requestDto).Wait();
            if (Response.IsClosed)
                throw new StopExecutionException();
        }

        public HtmlString GetErrorHtml()
        {
            return new HtmlString(RazorViewExtensions.GetErrorHtml(GetErrorStatus()) ?? "");
        }

        public IVirtualFiles VirtualFiles => HostContext.VirtualFiles;
        public IVirtualPathProvider VirtualFileSources => HostContext.VirtualFileSources;

        public IAppHost AppHost => ServiceStackHost.Instance;

        public bool DebugMode => HostContext.DebugMode;

        public virtual TPlugin GetPlugin<TPlugin>() where TPlugin : class, IPlugin => HostContext.AppHost.GetPlugin<TPlugin>();

        private IServiceStackProvider provider;
        public virtual IServiceStackProvider ServiceStackProvider => provider ?? (provider = new ServiceStackProvider(Request));

        public virtual IAppSettings AppSettings => ServiceStackProvider.AppSettings;

        public virtual IHttpRequest ServiceStackRequest => ServiceStackProvider.Request;

        public virtual IHttpResponse ServiceStackResponse => ServiceStackProvider.Response;

        public virtual ICacheClient Cache => ServiceStackProvider.Cache;

        public virtual IDbConnection Db => ServiceStackProvider.Db;

        public virtual IRedisClient Redis => ServiceStackProvider.Redis;

        public virtual IMessageProducer MessageProducer => ServiceStackProvider.MessageProducer;

        public virtual IAuthRepository AuthRepository => ServiceStackProvider.AuthRepository;

        public virtual ISessionFactory SessionFactory => ServiceStackProvider.SessionFactory;

        public virtual Caching.ISession SessionBag => ServiceStackProvider.SessionBag;

        public virtual bool IsAuthenticated => ServiceStackProvider.IsAuthenticated;

        protected virtual IAuthSession GetSession(bool reload = false) => ServiceStackProvider.GetSession(reload);

        protected virtual IAuthSession UserSession => GetSession();

        protected virtual TUserSession SessionAs<TUserSession>() => ServiceStackProvider.SessionAs<TUserSession>();

        protected virtual void SaveSession(IAuthSession session, TimeSpan? expiresIn = null) => ServiceStackProvider.Request.SaveSession(session, expiresIn);

        protected virtual void ClearSession() => ServiceStackProvider.ClearSession();

        protected virtual TDependency TryResolve<TDependency>() => ServiceStackProvider.TryResolve<TDependency>();

        protected virtual TService ResolveService<TService>() => ServiceStackProvider.ResolveService<TService>();

        protected virtual object ForwardRequestToServiceStack(IRequest request = null) => ServiceStackProvider.Execute(request ?? ServiceStackProvider.Request);

        public virtual IServiceGateway Gateway => ServiceStackProvider.Gateway;

        public void Dispose()
        {
            if (provider == null)
                return;

            provider?.Dispose();
            provider = null;
            EndServiceStackRequest();
        }

        public virtual void EndServiceStackRequest() => HostContext.AppHost.OnEndRequest(Request);

        public bool RenderErrorIfAny()
        {
            var html = GetErrorHtml(GetErrorStatus());
            if (html == null)
                return false;

            WriteLiteral(html);

            return true;
        }

        private string GetErrorHtml(ResponseStatus responseStatus)
        {
            if (responseStatus == null) return null;

            var stackTrace = responseStatus.StackTrace != null
                ? "<pre>" + responseStatus.StackTrace + "</pre>"
                : "";

            var html = @"
                <div id=""error-response"" class=""alert alert-danger"">
                    <h4>" +
                       responseStatus.ErrorCode + ": " +
                       responseStatus.Message + @"
                    </h4>" +
                       stackTrace +
                       "</div>";
            return html;
        }


        public void RedirectIfNotAuthenticated(string redirect = null) => Request.RedirectIfNotAuthenticated(redirect);

        public Task RedirectToAsync(string path) => Request.RedirectToAsync(path);

        public HtmlString RedirectTo(string path) => Request.RedirectTo(path);

        public HtmlString AssertRole(string role, string message = null, string redirect = null) =>
            Request.AssertRole(role: role, message: message, redirect: redirect);

        public Task AssertRoleAsync(string role, string message = null, string redirect = null) =>
            Request.AssertRoleAsync(role: role, message: message, redirect: redirect);

        public HtmlString AssertPermission(string permission, string message = null, string redirect = null) =>
            Request.AssertPermission(permission: permission, message: message, redirect: redirect);

        public Task AssertPermissionAsync(string permission, string message = null, string redirect = null) =>
            Request.AssertPermissionAsync(permission: permission, message: message, redirect: redirect);
    }
}

#endif
