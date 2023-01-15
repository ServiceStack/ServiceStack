#nullable enable
#if NET6_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Mvc;

public class RazorPagesEngine
{
    private readonly ILogger<RazorPagesEngine> log;
    private IRazorViewEngine ViewEngine { get; }
    readonly ITempDataProvider tempDataProvider;
    public RazorPagesEngine(ILogger<RazorPagesEngine> log, IRazorViewEngine viewEngine, ITempDataProvider tempDataProvider)
    {
        this.log = log;
        this.ViewEngine = viewEngine;
        this.tempDataProvider = tempDataProvider;
    }

    public ViewEngineResult GetView(string path, bool isMainPage = false)
    {
        var result = ViewEngine.GetView("", path, isMainPage: isMainPage);
        return result;
    }
    
    public async Task WriteHtmlAsync(Stream stream, IView? view, object? model = null, string? layout = null, HttpContext? ctx = null, IRequest? req = null)
    {
        if (view == null)
            throw new ArgumentNullException(nameof(view));

        var razorView = (RazorView)view;
        var razorPage = (Page)razorView.RazorPage;
        if (model == null)
        {
            var viewDataProp = razorPage.GetType().GetProperty("ViewData") 
                ?? throw new NotSupportedException($"Could not resolve ViewData from {razorPage.GetType().Name}");
            var modelType = viewDataProp.PropertyType.FirstGenericArg();
            model = modelType.CreateInstance();
        }
        
        try
        {
            ctx ??= new DefaultHttpContext {
                RequestServices = HostContext.Container
            };
            
            var actionContext = new ActionContext(
                ctx,
                new RouteData(),
                new ActionDescriptor());

            var sw = new StreamWriter(stream);
            var viewData = CreateViewData(model);

            // Use "_Layout" if unspecified
            razorView.RazorPage.Layout = layout ??  "_Layout";

            if (layout != null)
                viewData["Layout"] = layout;

            viewData[Keywords.IRequest] = req ?? new ServiceStack.Host.BasicRequest { PathInfo = view.Path };
            
            razorPage.PageContext = new PageContext(actionContext)
            {
                ViewData = viewData,
                RouteData = ctx.GetRouteData(),
                HttpContext = ctx,
            };

            var viewContext = new ViewContext(
                actionContext,
                view,
                viewData,
                new TempDataDictionary(actionContext.HttpContext, tempDataProvider),
                sw,
                new HtmlHelperOptions());

            await view.RenderAsync(viewContext).ConfigAwait();

            await sw.FlushAsync().ConfigAwait();

            try
            {
                using (razorView.RazorPage as IDisposable) { }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Error trying to dispose Razor View: {0}", ex.Message);
            }
        }
        catch (StopExecutionException) { }
        catch (Exception origEx)
        {
            var ex = origEx.UnwrapIfSingleException();
            if (ex is StopExecutionException)
                return;
            if (ex == origEx)
                throw;
            throw ex;
        }
    }

    static ConcurrentDictionary<Type, StaticMethodInvoker> createViewDataCache = new();
    public static ViewDataDictionary CreateViewData(object model)
    {
        var invoker = createViewDataCache.GetOrAdd(model.GetType(), type => {
            var mi = typeof(RazorPagesEngine).GetStaticMethod(nameof(CreateViewDataGeneric)).MakeGenericMethod(type);
            var invoker = mi.GetStaticInvoker();
            return invoker;
        });
        var pageViewData = invoker(model) as ViewDataDictionary;
        return pageViewData;
    }
    
    public static ViewDataDictionary CreateViewDataGeneric<T>(T model)
    {
        if (model is ViewDataDictionary viewData)
            return viewData;
        
        if (model != null && model.GetType().IsAnonymousType())
        {
            return new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: new ModelStateDictionary()) {
                Model = new DictionaryDynamicObject(model.ToObjectDictionary())
            };
        }

        return new ViewDataDictionary<T>(
            metadataProvider: new EmptyModelMetadataProvider(),
            modelState: new ModelStateDictionary()) {
            Model = model
        };
    }

    public static void PopulateRazorPageContext(HttpContext httpCtx, RazorPage razorPage, ViewDataDictionary viewData, ActionContext actionContext = null)
    {
        // var urlHelperFactory = httpCtx.RequestServices.GetRequiredService<IUrlHelperFactory>();
        // var urlHelper = urlHelperFactory.GetUrlHelper(actionContext);
        var viewDataProp = razorPage.GetType().GetProperty("ViewData");
        if (viewDataProp == null)
            return;
        var modelType = viewData.Model?.GetType() ?? viewDataProp.PropertyType.FirstGenericArg();

        //Razor Pages needs to have a typed ViewDataDictionary
        var viewModelType = viewData.GetType();
        var viewDataModelType = viewModelType.IsGenericType ? viewModelType.FirstGenericArg() : typeof(object);
        if (modelType != viewDataModelType)
        {
            var pagModel = viewData.Model ?? modelType.CreateInstance();
            var pageViewData = CreateViewData(pagModel);
            foreach (var entry in viewData)
            {
                pageViewData[entry.Key] = entry.Value;
            }
            viewData = pageViewData;
        }
        
        razorPage.PageContext = new PageContext(actionContext)
        {
            ViewData = viewData,
            RouteData = httpCtx.GetRouteData(),
            HttpContext = httpCtx,
        };
    }
}

#endif