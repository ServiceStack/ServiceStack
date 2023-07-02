#nullable enable
#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ServiceStack.Host;
using ServiceStack.IO;

namespace ServiceStack.Mvc;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class RenderStaticAttribute : Attribute
{
    public string? Path { get; }
    public RenderStaticAttribute(){}
    public RenderStaticAttribute(string path)
    {
        Path = path;
    }
}

public class RenderContext
{
    public IServiceProvider Services { get; }
    public IVirtualFile RazorFile { get; }
    public RenderContext(IServiceProvider services, IVirtualFile razorFile)
    {
        Services = services;
        RazorFile = razorFile;
    }

    public T Resolve<T>() => Services.Resolve<T>();
    public T? TryResolve<T>() => Services.TryResolve<T>();
}
public interface IRenderStatic {}
public interface IRenderStatic<T> : IRenderStatic where T : PageModel
{
    List<T> GetStaticProps(RenderContext ctx);
}

public interface IRenderStaticWithPath<T> : IRenderStatic<T> where T : PageModel
{
    string? GetStaticPath(T model);
}


public class RazorSsg
{
    public static string? GetBaseHref()
    {
        var args = Environment.GetCommandLineArgs()
            .Select(arg => arg.TrimPrefixes("/", "--")).ToList();
        var argPos = args.IndexOf("BaseHref");
        var baseHref = (argPos >= 0 && argPos + 1 < args.Count
            ? args[argPos + 1]
            : null) ?? Environment.GetEnvironmentVariable("BASE_HREF");
        return !string.IsNullOrEmpty(baseHref) 
            ? baseHref 
            : null;
    }
    
    public static string? GetBaseUrl()
    {
        var args = Environment.GetCommandLineArgs()
            .Select(arg => arg.TrimPrefixes("/", "--")).ToList();
        var argPos = args.IndexOf("BaseUrl");
        var baseUrl = (argPos >= 0 && argPos + 1 < args.Count
            ? args[argPos + 1]
            : null) ?? Environment.GetEnvironmentVariable("BASE_URL");
        return !string.IsNullOrEmpty(baseUrl) 
            ? baseUrl 
            : RazorSsg.GetBaseHref();
    }
    
    public static async Task<string?> GetPageRouteAsync(IVirtualFile razorFile)
    {
        using var readFs = razorFile.OpenText();
        var firstLine = await readFs.ReadLineAsync();
        if (firstLine?.StartsWith("@page") != true) return null;
        var pageRoute = firstLine["@page".Length..].Trim().StripQuotes();
        if (!string.IsNullOrEmpty(pageRoute))
            return pageRoute;
        return null;
    }

    public static HttpContext CreateHttpContext(ServiceStackHost appHost, string pathInfo)
    {
        var url = "https://localhost:5001".CombineWith(pathInfo);
        var ctx = new DefaultHttpContext
        {
            RequestServices = appHost.Container,
            Items = {
                [Keywords.IRequest] = new BasicHttpRequest(null,
                    RequestAttributes.LocalSubnet | RequestAttributes.Http | RequestAttributes.InProcess)
                {
                    PathInfo = pathInfo,
                    AbsoluteUri = url,
                    RawUrl = url,
                }
            }
        };
        return ctx;
    }
    
    public static async Task PrerenderAsync(ServiceStackHost appHost, IEnumerable<IVirtualFile> razorFiles, string distDir)
    {
        var log = appHost.Resolve<ILogger<RazorSsg>>();
        
        var razorPages = appHost.Resolve<RazorPagesEngine>();
        foreach (var razorFile in razorFiles)
        {
            var isMainPage = razorFile.VirtualPath.EndsWith("Layout.cshtml");
            var viewResult = razorPages.GetView(razorFile.VirtualPath, isMainPage: isMainPage);
            if (!viewResult.Success) continue;
            
            var razorPage = (viewResult.View as RazorView)?.RazorPage;
            if (razorPage == null) continue;
            
            var razorPageType = razorPage.GetType();
            var attrs = razorPageType.AllAttributes<RenderStaticAttribute>();
            foreach (var attr in attrs)
            {
                var pageRoute = await GetPageRouteAsync(razorFile);
                var staticPath = attr.Path ?? pageRoute;

                if (string.IsNullOrEmpty(staticPath))
                    throw new Exception($"Razor Page {razorFile.VirtualPath} contains an empty [RenderStatic] in @page with no route");

                if (staticPath.EndsWith("/"))
                    staticPath += "index.html";
                else if (staticPath.IndexOf('.') == -1)
                    staticPath += ".html";
                
                var toPath = distDir.CombineWith(staticPath);
                FileSystemVirtualFiles.AssertDirectory(Path.GetDirectoryName(toPath));
                
                log.LogInformation("Rendering {0} to {1}", razorFile.VirtualPath, staticPath);
                await using var fs = File.OpenWrite(toPath);
                var ctx = CreateHttpContext(appHost, pathInfo: pageRoute ?? staticPath.LastLeftPart('.'));
                await razorPages.WriteHtmlAsync(fs, viewResult.View, model:null, ctx:ctx);
            }

            if (razorPage is not IRenderStatic) 
                continue;

            var renderStaticDef = razorPageType.GetTypeWithGenericTypeDefinitionOf(typeof(IRenderStatic<>));
            if (renderStaticDef == null) continue;

            var modelType = renderStaticDef.GetGenericArguments()[0];
            var method = typeof(RazorSsg).GetMethod(nameof(RenderStaticRazorPageAsync));
            var genericMi = method.MakeGenericMethod(modelType);
            var task = (Task) genericMi.Invoke(null, new object[] { appHost, razorFile, distDir })!;
            await task;
        }
    }

    private static readonly Regex RouteConstraintsRegex = new(":[^}]+", RegexOptions.Multiline);

    public static string ResolvePageRoute(string pageRoute, object pageModel)
    {
        var to = pageRoute;
        if (pageRoute.IndexOf('{') >= 0)
        {
            var jsExpr = pageRoute.Replace("*", "").Replace("?", "");
            if (jsExpr.IndexOf(':') >= 0)
            {
                jsExpr = RouteConstraintsRegex.Replace(jsExpr,"");
            }
            jsExpr = '`' + jsExpr.Replace("{", "${") + '`';
            var scope = JS.CreateScope(new Dictionary<string, object>(pageModel.ToObjectDictionary(), StringComparer.OrdinalIgnoreCase));
            var jsResult = JS.eval(jsExpr, scope);
            to = jsResult.ToString();
        }
        return to.EndsWith('/')
            ? to + "index.html"
            : to + ".html";
    }

    public static Func<object, Task>? ResolveOnGetAsync(Type modelType)
    {
        var methods = modelType.GetMethods();
        var miAsync = methods.FirstOrDefault(x => x.Name == "OnGetAsync" && x.GetParameters().Length == 0);
        if (miAsync != null)
        {
            var invoker = miAsync.GetInvoker();
            return async model =>
            {
                var ret = invoker(model, Array.Empty<object>());
                if (ret is Task task)
                    await task;
            };
        }

        var mi = methods.FirstOrDefault(x => x.Name == "OnGet" && x.GetParameters().Length == 0);
        if (mi != null)
        {
            var actionInvoker = mi.GetActionInvoker();
            return model => {
                actionInvoker(model);
                return Task.CompletedTask;
            };
        }

        return null;
    }

    public static async Task RenderStaticRazorPageAsync<T>(ServiceStackHost appHost, IVirtualFile razorFile, string destDir) where T : PageModel
    {
        var log = appHost.Resolve<ILogger<RazorSsg>>();
        var razorPages = appHost.Resolve<RazorPagesEngine>();
        var viewResult = razorPages.GetView(razorFile.VirtualPath);
        if (!viewResult.Success)
            throw new Exception($"Could not resolve Razor Page at: {razorFile.VirtualPath}");
            
        var razorPage = (viewResult.View as RazorView)?.RazorPage;
        if (razorPage == null)
            throw new Exception($"Razor Page is not a RazorView: {razorFile.VirtualPath}");
        
        var pageRoute = await GetPageRouteAsync(razorFile); 
        
        var renderStatic = (IRenderStatic<T>)razorPage;
        var pageModels = renderStatic.GetStaticProps(new RenderContext(appHost.Container, razorFile));

        if (pageModels.Count > 0)
        {
            log.LogInformation("Rendering {0} {1}'s in {2}...", pageModels.Count, typeof(T).Name, razorFile.VirtualPath);            
        }

        var onGetAsyncInvoker = ResolveOnGetAsync(typeof(T));
        
        for (var i = 0; i < pageModels.Count; i++)
        {
            var pageModel = pageModels[i];
            string? staticPath = null;
            if (razorPage is IRenderStaticWithPath<T> renderStaticWithPath)
            {
                staticPath = renderStaticWithPath.GetStaticPath(pageModel);
            }
            else if (pageRoute != null)
            {
                staticPath = ResolvePageRoute(pageRoute, pageModel);
            }

            if (staticPath == null)
            {
                log.LogWarning("Could not resolve static path for {0}, ignoring...",
                    pageRoute ?? razorFile.VirtualPath);
                return;
            }

            viewResult = razorPages.GetView(razorFile.VirtualPath);
            if (!viewResult.Success)
                return;

            var toPath = destDir.CombineWith(staticPath);
            FileSystemVirtualFiles.AssertDirectory(Path.GetDirectoryName(toPath));

            log.LogInformation("Rendering {0}/{1} to {2}", i+1, pageModels.Count, staticPath);
            if (onGetAsyncInvoker != null)
                await onGetAsyncInvoker(pageModel);
            
            var pathInfo = pageRoute == null || pageRoute.Contains('{')
                ? staticPath.LastLeftPart('.')
                : pageRoute;

            await using var fs = File.OpenWrite(toPath);
            var ctx = CreateHttpContext(appHost, pathInfo:pathInfo);
            await razorPages.WriteHtmlAsync(fs, viewResult.View, model: pageModel, ctx: ctx);
        }
    }
}

#endif