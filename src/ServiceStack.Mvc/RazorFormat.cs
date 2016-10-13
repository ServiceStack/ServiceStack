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
            Html.HtmlHelper htmlHelper = null) => null;

        public bool ProcessRequest(IRequest req, IResponse res, object dto)
        {
            if (req.Dto == null || dto == null)
                return false;

            var httpResult = dto as IHttpResult;
            if (httpResult != null)
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

            RenderView(req, CreateViewData(dto), viewEngineResult.View, req.GetTemplate());

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

        internal void RenderView(IRequest req, ViewDataDictionary viewData, IView view, string layout=null)
        {
            var razorView = view as RazorView;
            try
            {
                var actionContext = new ActionContext(
                    ((HttpRequest) req.OriginalRequest).HttpContext,
                    new RouteData(),
                    new ActionDescriptor());

                using (var sw = new StreamWriter(req.Response.OutputStream))
                {
                    if (viewData == null)
                        viewData = CreateViewData((object)null);

                    viewData["Layout"] = layout;

                    viewData[Keywords.IRequest] = req;
                    var viewContext = new ViewContext(
                        actionContext,
                        view,
                        viewData,
                        new TempDataDictionary(actionContext.HttpContext, tempDataProvider),
                        sw,
                        new HtmlHelperOptions());

                    view.RenderAsync(viewContext).GetAwaiter().GetResult();

                    try
                    {
                        using (razorView?.RazorPage as IDisposable) { }
                    }
                    catch (Exception ex)
                    {
                        //Throws "Cannot access a disposed object." on `MemoryPoolViewBufferScope` in Linux
                        log.Warn("Error trying to dispose Razor View: " + ex.Message, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                //Can't set HTTP Headers which are already written at this point
                req.Response.WriteErrorBody(ex);
            }
        }
    }

    public class RazorHandler : ServiceStackHandlerBase
    {
        private ViewEngineResult viewEngineResult;
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

        public override void ProcessRequest(IRequest req, IResponse res, string operationName)
        {
            var format = HostContext.GetPlugin<RazorFormat>();
            try
            {
                if (viewEngineResult == null)
                {
                    if (PathInfo == null)
                        throw new ArgumentNullException(nameof(PathInfo));

                    viewEngineResult = format.GetPageFromPathInfo(PathInfo);

                    if (viewEngineResult == null)
                        throw new ArgumentException("Could not find Razor Page at " + PathInfo);
                }

                res.ContentType = MimeTypes.Html;
                var model = Model;
                if (model == null)
                    req.Items.TryGetValue("Model", out model);

                ViewDataDictionary viewData = null;
                if (model == null)
                {
                    var razorView = viewEngineResult.View as RazorView;
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
                    foreach (string header in req.Headers)
                    {
                        viewData[header] = req.Headers[header];
                    }
                    foreach (string key in req.QueryString)
                    {
                        viewData[key] = req.QueryString[key];
                    }
                    foreach (string key in req.FormData)
                    {
                        viewData[key] = req.QueryString[key];
                    }
                    foreach (var entry in req.Items)
                    {
                        viewData[entry.Key] = entry.Value;
                    }
                }

                format.RenderView(req, viewData, viewEngineResult.View);
            }
            catch (Exception ex)
            {
                //Can't set HTTP Headers which are already written at this point
                req.Response.WriteErrorBody(ex);
            }
        }

        public override object CreateRequest(IRequest request, string operationName)
        {
            return null;
        }

        public override object GetResponse(IRequest httpReq, object request)
        {
            return null;
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
            var feature = HostContext.GetPlugin<Formats.MarkdownFormat>();
            return feature != null
                ? new HtmlString(feature.Transform(markdown))
                : new HtmlString(new MarkdownSharp.Markdown().Transform(markdown));
        }

        public static string ResolveLayout(this IHtmlHelper htmlHelper, string defaultLayout)
        {
            var layout = htmlHelper.ViewData["Layout"] as string;
            if (layout != null)
                return layout;

            var template = htmlHelper.GetRequest()?.GetTemplate();
            return template ?? defaultLayout;
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
                object oRequest;
                if (base.ViewContext.ViewData.TryGetValue(Keywords.IRequest, out oRequest))
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

            var status = response as ResponseStatus;
            if (status != null)
                return status;

            var hasResponseStatus = response as IHasResponseStatus;
            if (hasResponseStatus != null)
                return hasResponseStatus.ResponseStatus;

            var propertyInfo = response.GetType().GetPropertyInfo("ResponseStatus");
            return propertyInfo?.GetProperty(response) as ResponseStatus;
        }

        public HtmlString GetErrorHtml()
        {
            return new HtmlString(RazorViewExtensions.GetErrorHtml(GetErrorStatus()) ?? "");
        }

        public virtual T GetPlugin<T>() where T : class, IPlugin => HostContext.AppHost.GetPlugin<T>();

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

        protected virtual T TryResolve<T>() => ServiceStackProvider.TryResolve<T>();

        protected virtual T ResolveService<T>() => ServiceStackProvider.ResolveService<T>();

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