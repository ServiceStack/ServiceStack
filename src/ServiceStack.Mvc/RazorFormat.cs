#if NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Platforms;
using ServiceStack.Redis;
using ServiceStack.VirtualPath;
using ServiceStack.Web;
using ServiceStack.Text;

namespace ServiceStack.Mvc
{
    public class RazorFormat : IPlugin, Html.IViewEngine
    {
        public static ILog log = LogManager.GetLogger(typeof(RazorFormat));

        public static string DefaultLayout { get; set; } = "_Layout";

        public IVirtualPathProvider VirtualFileSources { get; set; }

        public List<string> ViewLocations { get; set; }

        public string PagesPath { get; set; } = "~/Views/Pages";

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

            if (viewEngine == null || tempDataProvider == null)
                throw new Exception("MVC Services have not been configured, Please add `services.AddMvc()` to StartUp.ConfigureServices()");
        }

        public System.Web.IHttpHandler CatchAllHandler(string httpmethod, string pathInfo, string filepath)
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

            var viewEngineResult = viewEngine.GetView("", viewPath, 
                isMainPage: viewPath == "~/wwwroot/default.cshtml");

            if (!viewEngineResult.Success)
            {
                viewPath = PagesPath.CombineWith(pathInfo);
                if (!viewPath.EndsWith(".cshtml"))
                    viewPath += ".cshtml";

                viewEngineResult = viewEngine.GetView("", viewPath,
                    isMainPage: viewPath == $"{PagesPath}/default.cshtml");
            }

            return viewEngineResult.Success 
                ? viewEngineResult 
                : null;
        }

        public bool HasView(string viewName, IRequest httpReq = null) => false;

        public string RenderPartial(string pageName, object model, bool renderHtml, StreamWriter writer = null,
            Html.IHtmlContext htmlHelper = null) => null;

        private const string RenderException = "RazorFormat.Exception";

        public async Task<bool> ProcessRequestAsync(IRequest req, object dto, Stream outputStream)
        {
            if (req.Dto == null || dto == null)
                return false;

            if (req.Items.ContainsKey(RenderException))
                return false;

            if (dto is IHttpResult httpResult)
                dto = httpResult.Response;

            var viewNames = new List<string> { req.Dto.GetType().Name, dto.GetType().Name };

            var useView = req.GetView();
            if (useView != null)
            {
                viewNames.Insert(0, req.GetView());
            }

            var viewEngineResult = FindView(viewNames);
            if (viewEngineResult == null)
                return false;

            ViewDataDictionary viewData = null;

            if (dto is ErrorResponse errorDto)
            {
                var razorView = viewEngineResult.View as RazorView;
                var genericDef = razorView.RazorPage.GetType().FirstGenericType();
                var modelType = genericDef?.GetGenericArguments()[0];
                if (modelType != null && modelType != typeof(object))
                {
                    var model = modelType.CreateInstance();
                    viewData = CreateViewData(model);
                    req.Items[Formats.HtmlFormat.ErrorStatusKey] = errorDto;
                }
            }

            if (viewData == null)
                viewData = CreateViewData(dto);

            await RenderView(req, outputStream, viewData, viewEngineResult.View, req.GetTemplate());

            return true;
        }

        private ViewEngineResult FindView(IEnumerable<string> viewNames)
        {
            const string execPath = "";
            foreach (var viewName in viewNames)
            {
                foreach (var location in ViewLocations)
                {
                    var viewPath = location.CombineWith(viewName) + ".cshtml";
                    var viewEngineResult = viewEngine.GetView(execPath, viewPath, isMainPage: false);
                    if (viewEngineResult.Success)
                        return viewEngineResult;
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

                    sw.Flush();

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
            catch (Exception ex)
            {
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

                await RenderView(format, req, res, view);
            }
            catch (Exception ex)
            {
                //Can't set HTTP Headers which are already written at this point
                await req.Response.WriteErrorBody(ex);
            }
        }

        private async Task RenderView(RazorFormat format, IRequest req, IResponse res, IView view)
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
                    model = DeserializeHttpRequest(modelType, req, req.ContentType);
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

        public static IHtmlContent PartialMarkdown(this IHtmlHelper htmlHelper, string partial)
        {
            var req = htmlHelper.GetRequest();
            var pathInfo = req.PathInfo;
            if (pathInfo != null)
            {
                var dir = pathInfo.LastLeftPart("/");
                var partialPath = dir.CombineWith(partial) + ".md";
                partialPath = partialPath.TrimPrefixes("/");

                var markdownPaths = new[]
                {
                    partialPath,
                    $"wwwroot/{partialPath}",
                    $"Views/Shared/{partial}.md",
                    $"Views/{partial}.md",
                };

                foreach (var path in markdownPaths)
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
            return htmlHelper.GetRequest().QueryString[paramName];
        }

        public static HtmlString IncludeFile(this IHtmlHelper htmlHelper, string virtualPath)
        {
            var file = HostContext.VirtualFileSources.GetFile(virtualPath);
            return file != null
                ? new HtmlString(file.ReadAllText())
                : HtmlString.Empty;
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

        public string GetLayout(string defaultLayout) => ViewData["Layout"] as string ?? defaultLayout;

        public bool IsError => ModelError != null || GetErrorStatus() != null;

        public object ModelError { get; set; }

        public bool IsPostBack => this.Request.Verb == HttpMethods.Post;

        public ResponseStatus GetErrorStatus()
        {
            var errorStatus = this.Request.GetItem(Formats.HtmlFormat.ErrorStatusKey);
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

        protected virtual IAuthSession GetSession(bool reload = true) => ServiceStackProvider.GetSession(reload);

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
    }
}

#endif
