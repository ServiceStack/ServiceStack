#if !NETCORE
using System.Web;
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// A custom placeholder SharpPagesFeature plugin optimized to support for SPA and SSG features 
/// </summary>
public class SpaFeature : SharpPagesFeature 
{
}

public class SharpPagesFeature : ScriptContext, IPlugin, IViewEngine, Model.IHasStringId
{
    public string Id { get; set; } = ServiceStack.Plugins.SharpPages;
    public bool? EnableHotReload { get; set; }
        
    public bool? EnableSpaFallback { get; set; }

    public bool DisablePageBasedRouting { get; set; }

    public string DebugDefaultTemplate { get; set; }
        
    public string ApiPath { get; set; }
    public string ApiDefaultContentType { get; set; } = MimeTypes.Json;

    /// <summary>
    /// Role Required to call Templates Admin Service (/script/admin), Default is Admin.
    /// If null Templates Admin Service will not be registered.
    /// </summary>
    public string ScriptAdminRole { get; set; } = RoleNames.Admin;

    /// <summary>
    /// Role Required to call Metadata Debug Service (/metadata/debug).
    /// If null Metadata Debug Service will only be registered in DebugMode.
    /// </summary>
    public string MetadataDebugAdminRole { get; set; }

    public List<string> IgnorePaths { get; set; } = new() {
        "/Views/",
        "/swagger-ui" // Swagger's handler needs to process index.html 
    };

    public ServiceStackScripts ServiceStackScripts => ScriptMethods.FirstOrDefault(x => x is ServiceStackScripts) as ServiceStackScripts;

    public string HtmlExtension
    {
        get => PageFormats.First(x => x is HtmlPageFormat).Extension;
        set => PageFormats.First(x => x is HtmlPageFormat).Extension = value;
    }

    public bool ExcludeProtectedFilters
    {
        set
        {
            if (value) ScriptMethods.RemoveAll(x => x is ProtectedScripts);
        }
    }
        
    /// <summary>
    /// Whether to auto populate scoped args with Request.FormData[] and Request.QueryString[] params (default false).
    /// Recommendation is to instead use `form.name` or `query.name` (or `qs.name`) to explicitly reference user input values.
    /// </summary>
    public bool ImportRequestParams { get; set; }

    public SharpPagesFeature()
    {
        var appHost = HostContext.AssertAppHost();
        ScanAssemblies.AddRange(appHost.ServiceAssemblies);
        ScanAssemblies = ScanAssemblies.Distinct().ToList();

        this.InitForSharpPages(appHost);
        SkipExecutingFiltersIfError = true;
    }

    public virtual void Register(IAppHost appHost)
    {
        this.UseAppHost(appHost);

        appHost.Register(Pages);
        appHost.Register(this);
        appHost.CatchAllHandlers.Add(RequestHandler);
        appHost.ViewEngines.Add(this);

        if (!DisablePageBasedRouting)
        {
            appHost.FallbackHandlers.Add(PageBasedRoutingHandler);
        }
            
        InitViewPages(appHost);

        if (EnableHotReload.GetValueOrDefault(DebugMode))
        {
            appHost.RegisterService(typeof(HotReloadPageService));

            // Also enable hot-fileloader.js for hot reloading when static files changed in /wwwroot
            if (!appHost.Plugins.Any(x => x is HotReloadFeature)) 
            {
                appHost.RegisterService(typeof(HotReloadFilesService));
            }
        }

        if (EnableSpaFallback.GetValueOrDefault())
        {
            appHost.RegisterService(typeof(SpaFallbackService));
        }
            
        if (!string.IsNullOrEmpty(ApiPath))
            appHost.RegisterService(typeof(SharpApiService), 
                (ApiPath[0] == '/' ? ApiPath : '/' + ApiPath).CombineWith("/{PageName}/{PathInfo*}"));

        var enableMetadataDebug = DebugMode || MetadataDebugAdminRole != null;
        if (enableMetadataDebug)
        {
            appHost.RegisterService(typeof(MetadataDebugService), MetadataDebugService.Route);
            appHost.ConfigurePlugin<MetadataFeature>(
                feature => feature.AddDebugLink(MetadataDebugService.Route, "Debug Inspector"));
        }

        if (!string.IsNullOrEmpty(ScriptAdminRole))
        {
            appHost.RegisterService(typeof(ScriptAdminService), ScriptAdminService.Routes);
        }
            
        appHost.AddToAppMetadata(meta => {
            meta.Plugins.SharpPages = new SharpPagesInfo {
                ApiPath = ApiPath,
                SpaFallback = EnableSpaFallback,
                ScriptAdminRole = ScriptAdminRole,
                MetadataDebugAdminRole = MetadataDebugAdminRole,
                MetadataDebug = enableMetadataDebug,
            };
        });

        Init();
            
        InitPage = Pages.GetPage("_init");
        if (InitPage == null)
        {
            var initScript = appHost.VirtualFileSources.GetFile("_init.ss");
            if (initScript != null)
                InitPage = this.SharpScriptPage(initScript.ReadAllText());
        }
        if (InitPage != null)
        {
            appHost.AfterInitCallbacks.Add(host => RunInitPage());
        }
    }

    internal SharpPage InitPage { get; set; }

    public string RunInitPage()
    {
        if (InitPage == null)
            return "_init.html not found";
            
        try
        {
            var pageResult = new PageResult(InitPage);
            var execInit = pageResult.RenderToStringAsync().GetAwaiter().GetResult();
            Args["initout"] = execInit;
            if (pageResult.LastFilterError != null)
            {
                Args["initError"] = pageResult.LastFilterError.ToString();
            }
            return execInit;
        }
        catch (Exception ex)
        {
            Args["initError"] = Args["initout"] = ex.ToString();
            return ex.ToString();
        }
    }

    private readonly ConcurrentDictionary<string, byte> catchAllPathsNotFound = new();

    protected virtual IHttpHandler RequestHandler(string httpMethod, string pathInfo, string filePath)
    {
        if (!DebugMode && catchAllPathsNotFound.ContainsKey(pathInfo))
            return null;
        if (!VirtualPathUtils.IsValidFilePath(pathInfo))
            return null;

        foreach (var ignorePath in IgnorePaths)
        {
            if (pathInfo.StartsWithIgnoreCase(ignorePath))
            {
                catchAllPathsNotFound[pathInfo] = 1;
                return null;
            }
        }

        var codePage = Pages.GetCodePage(pathInfo);
        if (codePage != null)
            return new SharpCodePageHandler(codePage);

        var page = Pages.GetPage(pathInfo);
        if (page != null)
        {
            if (page.File.Name.StartsWith("_"))
                return HostContext.AppHost.CustomErrorHttpHandlers
                    .GetValueOrDefault(HttpStatusCode.Forbidden) as IHttpHandler ?? new ForbiddenHttpHandler(); 

            //If it's a dir index page and doesn't have a trailing '/' let it pass through to RedirectDirectoriesToTrailingSlashes
            if (pathInfo[pathInfo.Length - 1] != '/' && pathInfo.Substring(1) == page.File.Directory?.VirtualPath)
                return null;

            return new SharpPageHandler(page);
        }

        if (!DebugMode)
        {
            if (catchAllPathsNotFound.Count > 10000) //prevent DOS
                catchAllPathsNotFound.Clear();
            catchAllPathsNotFound[pathInfo] = 1;
        }
            
        return null;
    }

    protected virtual IHttpHandler PageBasedRoutingHandler(string httpMethod, string pathInfo, string requestFilePath)
    {
        var page = GetRoutingPage(pathInfo, out var args);
        return page != null
            ? new SharpPageHandler(page) {Args = args}
            : null;
    }

    /// <summary>
    /// Resolve Page-based Routing page from /path/info
    /// </summary>
    public SharpPage GetRoutingPage(string pathInfo, out Dictionary<string,object> routingArgs)
    {
        // Sync with ServiceStack.Mvc RazorFormat GetRoutingPage()

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

        SharpPage GetPageFromPath(IVirtualFile file, string[] pathParts, out Dictionary<string,object> args)
        {
            var filePath = file.VirtualPath.WithoutExtension();
            var page = Pages.TryGetPage(filePath) ?? 
                       Pages.AddPage(filePath, file);

            args = new Dictionary<string, object>();
            var fileParts = filePath.Split('/');

            for (var i = 0; i < pathParts.Length; i++)
            {
                if (i >= fileParts.Length)
                    break;

                var part = fileParts[i];
                if (part[0] == '_')
                    args[part.Substring(1)] = pathParts[i];
            }

            return page;
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
                                file.Name.StartsWith("_init"))                                
                                continue;
                        }

                        var fileNameWithoutExt = file.Name.WithoutExtension();
                        if (fileNameWithoutExt == "index")
                            continue;
                                
                        foreach (var format in PageFormats)
                        {
                            if (file.Extension == format.Extension)
                            {
                                if (fileNameWithoutExt == segment || isWildPath)
                                    return GetPageFromPath(file, pathSegments, out routingArgs);
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
                    foreach (var format in PageFormats)
                    {
                        var file = dir.GetFile(IndexPage + "." + format.Extension);
                        if (file != null)
                            return GetPageFromPath(file, pathSegments, out routingArgs);
                    }
                }
            }
        }

        routingArgs = null;
        return null;
    }

    private readonly ConcurrentDictionary<string, SharpPage> viewPagesMap = new();

    private void InitViewPages(IAppHost appHost)
    {
        var viewsDir = VirtualFiles.GetDirectory("Views");
        if (viewsDir == null)
            return;

        var htmlFormat = PageFormats.First(x => x is HtmlPageFormat);

        var files = viewsDir.GetAllMatchingFiles("*." + htmlFormat.Extension);
        foreach (var file in files)
        {
            if (file.Name.StartsWith("_")) // _layout.html or _partial.html which can have duplicate names
                continue;
                
            var viewName = file.Name.WithoutExtension();
            if (viewPagesMap.TryGetValue(viewName, out var existingFile))
                throw new NotSupportedException($"Multiple views found named '{file.Name}' in '{file.VirtualPath}' and '{existingFile.VirtualPath}'");

            viewPagesMap[viewName] = new SharpPage(this, file, htmlFormat);
        }
    }

    public SharpPage GetViewPage(string viewName)
    {
        return viewPagesMap.TryGetValue(viewName, out var view) ? view : null;
    }

    public bool HasView(string viewName, IRequest httpReq = null)
    {
        return GetCodePage("Views/" + viewName) != null || GetViewPage(viewName) != null;
    }

    public string RenderPartial(string pageName, object model, bool renderHtml, StreamWriter writer = null, IHtmlContext htmlHelper = null)
    {
        var codePage = htmlHelper?.HttpRequest != null 
            ? htmlHelper.HttpRequest.GetCodePage("Views/" + pageName) 
            : GetCodePage("Views/" + pageName);

        var viewPage = codePage == null ? GetViewPage(pageName) : null;

        if (codePage == null && viewPage == null)
            return null;
            
        var output = codePage != null 
            ? new PageResult(codePage) { Model = model }.Result
            : new PageResult(viewPage) { Model = model }.Result;

        if (writer != null)
        {
            writer.Write(output);
            writer.Flush();
            return null;
        }

        return output;
    }

    public async Task<bool> ProcessRequestAsync(IRequest req, object dto, Stream outputStream)
    {
        var explicitView = req.GetView();
            
        if (dto is IHttpResult httpResult)
        {
            dto = httpResult.Response;
            if (httpResult is HttpResult viewResult && viewResult.View != null)
                explicitView = viewResult.View;
        }

        var errorStatus = dto.GetResponseStatus()
                          ?? (dto is Exception ex
                              ? ex.ToResponseStatus()
                              : null);
        if (errorStatus?.ErrorCode != null)
            req.Items[Keywords.ErrorStatus] = errorStatus;

        var viewNames = new List<string>();
        if (explicitView != null)
            viewNames.Add(explicitView);
            
        viewNames.Add(req.OperationName);
        viewNames.Add(dto.GetType().Name);

        SharpCodePage codePage = null;
        SharpPage viewPage = null;

        foreach (var name in viewNames)
        {
            codePage = req.GetCodePage(name.FirstCharEquals('/') ? name : "Views/" + name);
            if (codePage != null)
                break;
        }

        if (codePage == null)
        {
            foreach (var name in viewNames)
            {
                viewPage = name.FirstCharEquals('/') 
                    ? Pages.GetPage(name) //content page
                    : GetViewPage(name);
                if (viewPage != null)
                    break;
            }
        }

        Dictionary<string, object> args = null;
        if (codePage == null && viewPage == null && explicitView != null)
            viewPage = GetRoutingPage(explicitView, out args);

        if (codePage == null && viewPage == null)
            return false;

        if (codePage != null)
            codePage.Init();
        else
            await viewPage.Init();

        var layoutName = req.GetItem(Keywords.Template) as string;
        var layoutPage = codePage != null 
            ? Pages.ResolveLayoutPage(codePage, layoutName)
            : Pages.ResolveLayoutPage(viewPage, layoutName);

        var handler = codePage != null
            ? (HttpAsyncTaskHandler)new SharpCodePageHandler(codePage, layoutPage) { OutputStream = outputStream, Model = dto }
            : new SharpPageHandler(viewPage, layoutPage) { OutputStream = outputStream, Model = dto, Args = args };

        await handler.ProcessRequestAsync(req, req.Response, req.OperationName);

        return true;
    }
}

[FallbackRoute("/{PathInfo*}", Matches="AcceptsHtml"), ExcludeMetadata]
public class SpaFallback : IReturn<string>
{
    public string PathInfo { get; set; }
}

[DefaultRequest(typeof(SpaFallback))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class SpaFallbackService : Service
{
    //Return index.html for unmatched requests so routing is handled on client
    public object Any(SpaFallback request) => Request.GetPageResult("/");
}


[ExcludeMetadata]
[Route("/hotreload/page")]
public class HotReloadPage : IReturn<HotReloadPageResponse>
{
    public string Path { get; set; }
    public string ETag { get; set; }
}

public class HotReloadPageResponse
{
    public string ETag { get; set; }
    public string LastUpdatedPath { get; set; }
    public bool Reload { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[DefaultRequest(typeof(HotReloadPage))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class HotReloadPageService : Service
{
    public static TimeSpan LongPollDuration = TimeSpan.FromSeconds(60);
    public static TimeSpan CheckDelay = TimeSpan.FromMilliseconds(50);
    public static TimeSpan ModifiedDelay = TimeSpan.FromMilliseconds(50);

    public ISharpPages Pages { get; set; }

    public async Task<HotReloadPageResponse> Any(HotReloadPage request)
    {
        var pathInfo = request.Path ?? "/";
        var page = Pages.GetPage(pathInfo);
        if (page == null)
        {
            var matchingRoute = RestHandler.FindMatchingRestPath(
                HttpMethods.Get, pathInfo, out var contentType);

            var feature = HostContext.AppHost.AssertPlugin<SharpPagesFeature>();

            if (matchingRoute != null)
            {
                page = feature.GetViewPage(matchingRoute.RequestType.Name);

                if (page == null)
                {
                    var responseType = HostContext.AppHost.Metadata.GetResponseTypeByRequest(matchingRoute.RequestType);
                    page = feature.GetViewPage(responseType.Name);
                }
            }

            if (page == null)
                page = feature.GetRoutingPage(pathInfo, out var args);
        }

        if (page == null)
            throw HttpError.NotFound("Page not found: " + request.Path);

        if (!page.HasInit)
            await page.Init();

        var startedAt = DateTime.UtcNow;
        var eTagTicks = string.IsNullOrEmpty(request.ETag) ? (long?) null : long.Parse(request.ETag);
        var maxLastModified = DateTime.MinValue;
        var shouldReload = false;

        while (DateTime.UtcNow - startedAt < LongPollDuration)
        {
            maxLastModified = Pages.GetLastModified(page);

            if (eTagTicks == null)
                return new HotReloadPageResponse { ETag = maxLastModified.Ticks.ToString() };

            shouldReload = maxLastModified.Ticks > eTagTicks;
            if (shouldReload)
            {
                await Task.Delay(ModifiedDelay);
                break;
            }

            await Task.Delay(CheckDelay);
        }

        return new HotReloadPageResponse { Reload = shouldReload, ETag = maxLastModified.Ticks.ToString() };
    }
}

[ExcludeMetadata]
public class ApiPages
{
    public string PageName { get; set; }
    public string PathInfo { get; set; }
}

[DefaultRequest(typeof(ApiPages))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class SharpApiService : Service
{
    public async Task<object> Any(ApiPages request) 
    {
        if (string.IsNullOrEmpty(request.PageName))
            throw new ArgumentNullException(nameof(request.PageName));

        var parts = string.IsNullOrEmpty(request.PathInfo)  
            ? TypeConstants.EmptyStringArray
            : request.PathInfo.SplitOnLast('.');

        var hasPathContentType = parts.Length > 1 && ContentTypes.KnownFormats.Contains(parts[1]);
        var pathInfo = hasPathContentType
            ? parts[0]
            : request.PathInfo;
            
        var pathArgs = string.IsNullOrEmpty(pathInfo)
            ? TypeConstants.EmptyStringArray
            : pathInfo.Split('/');
            
        parts = request.PageName.SplitOnLast('.');
        var hasPageContentType = pathArgs.Length == 0 && parts.Length > 1 && ContentTypes.KnownFormats.Contains(parts[1]);
        var pageName = hasPageContentType
            ? parts[0]
            : request.PageName;

        // Change .csv download file name
        base.Request.OperationName = pageName + (pathArgs.Length > 0 ? "_" + string.Join("_", pathArgs) : "");
            
        var feature = HostContext.GetPlugin<SharpPagesFeature>();

        if (feature.ApiDefaultContentType != null &&
            !hasPathContentType &&
            !hasPageContentType &&
            base.Request.QueryString[ScriptConstants.Format] == null && base.Request.ResponseContentType == MimeTypes.Html)
        {
            base.Request.ResponseContentType = feature.ApiDefaultContentType;
        }

        var pagePath = feature.ApiPath.CombineWith(pageName).TrimStart('/');
        var page = base.Request.GetPage(pagePath);
        if (page == null)
            throw HttpError.NotFound($"No Sharp API was found at '{pagePath}'");
            
        var requestArgs = base.Request.GetScriptRequestParams(importRequestParams:feature.ImportRequestParams);
        requestArgs[ScriptConstants.PathInfo] = request.PathInfo;
        requestArgs[ScriptConstants.PathArgs] = pathArgs; 

        var pageResult = new PageResult(page) {
            NoLayout = true,
            RethrowExceptions = true,
            Args = requestArgs
        };

        var discardedOutput = await pageResult.RenderToStringAsync();

        if (pageResult.ReturnValue == null)
            throw HttpError.NotFound($"The Sharp API did not specify a response. Use the 'return' filter to set a return value for the page.");

        var response = pageResult.ReturnValue.Result;
        if (response is Task<object> responseTask)
            response = await responseTask;
        if (response is IRawString raw)
            response = raw.ToRawString();

        var httpResult = ToHttpResult(pageResult, response);
        return httpResult;
    }

    internal static IHttpResult ToHttpResult(PageResult pageResult, object response)
    {
        if (!(response is IHttpResult httpResult))
        {
            if (pageResult.ReturnValue?.Args != null)
            {
                httpResult = ServiceStackScripts.ToHttpResult(pageResult.ReturnValue.Args);
                httpResult.Response = response;
            }
            else
            {
                httpResult = new HttpResult(response);
            }
        }
            
        return httpResult;
    }
}

[ExcludeMetadata]
public class MetadataDebug : IReturn<string>
{
    public string Script { get; set; }
    public string AuthSecret { get; set; }
}

[ReturnExceptionsInJson]
[DefaultRequest(typeof(MetadataDebug))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class MetadataDebugService : Service
{
    public static string Route = "/metadata/debug"; 
        
    public static string DefaultTemplate = @"<table><tr><td style='width:50%'><pre>
Service Name              {{ appHost.ServiceName }}
Handler Path              {{ appConfig.HandlerFactoryPath }}
VirtualFiles Path         {{ appVirtualFilesPath }}
VirtualFileSources Path   {{ appVirtualFileSourcesPath }}
OS Environment Variable   {{ 'OS' |> envVariable }}
ServiceStack Version      {{ envServiceStackVersion }}

Request: 
  - RemoteIp              {{ request.RemoteIp }}
  - UserHostAddress       {{ request.UserHostAddress }}
  - PathInfo              {{ request.PathInfo }}
  - UserAgent             {{ request.UserAgent }}

Session:
  - ss-id                 {{ userSessionId }}
  - IsAuthenticated       {{ userSession.IsAuthenticated }}
  - UserAuthId            {{ userAuthId }}
  - Username              {{ userAuthName }}
  - LastName              {{ userSession.LastName }}
  - Is Admin              {{ 'Admin'         |> userHasRole }}
  - Has Permission        {{ 'ThePermission' |> userHasPermission }}

Plugins:
{{ plugins |> map => `  - ${it.typeName()}` |> joinln }}
</pre></td><td style='width:50%'> 
{{ meta.Operations |> take(10) |> map => {Request:it.Name,Response:it.ResponseType.Name??'', Service:it.ServiceType.Name} |> htmlDump({ caption:'First 10 Services'}) }}
<table><caption>Network Information</caption>
<tr><th>    IPv4 Addresses                            </th><th>              IPv6 Addresses                            </th></tr>
<td>{{#each ip in networkIpv4Addresses}}<div>{{ip}}</div>{{/each}}</td><td>{{#each ip in networkIpv6Addresses}}<div>{{ip}}</div>{{/each}}<td></tr></pre></td>
</tr></table>";
        
    public async Task<object>  Any(MetadataDebug request)
    {
        if (string.IsNullOrEmpty(request.Script))
            return null;

        var feature = HostContext.AssertPlugin<SharpPagesFeature>();
        await RequestUtils.AssertAccessRoleOrDebugModeAsync(Request, accessRole: feature.MetadataDebugAdminRole, authSecret: request.AuthSecret);

        var appHost = HostContext.AppHost;
        var context = new ScriptContext
        {
            ScriptMethods = { new InfoScripts() },
            Args =
            {
                {ScriptConstants.Request, base.Request},
                {"request", base.Request},
                {"appHost", appHost},
                {"appConfig", appHost.Config},
                {"appVirtualFilesPath", appHost.ContentRootDirectory.RealPath},
                {"appVirtualFileSourcesPath", appHost.RootDirectory.RealPath},
                {"meta", HostContext.Metadata},
            }
        }.Init();

        feature.Args.Each(x => context.Args[x.Key] = x.Value);

        var result = await context.EvaluateScriptAsync(request.Script);
        return new HttpResult(result) { ContentType = MimeTypes.PlainText }; 
    }

    public async Task<object> GetHtml(MetadataDebug request)
    {
        var feature = HostContext.GetPlugin<SharpPagesFeature>();
        if (!HostContext.DebugMode)
        {
            await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.MetadataDebugAdminRole);
        }
            
        if (request.Script != null)
            return Any(request);

        var defaultTemplate = feature.DebugDefaultTemplate ?? DefaultTemplate;

        var html = Templates.HtmlTemplates.GetMetadataDebugTemplate();
        html = html.Replace("{0}", defaultTemplate);

        var authSecret = Request.GetAuthSecret();
        if (HostContext.Config.AdminAuthSecret != null &&
            HostContext.Config.AdminAuthSecret == authSecret)
        {
            html = html.Replace("{ template: template }", 
                "{ template: template, authsecret:" + feature.DefaultMethods.jsQuotedString(authSecret).ToRawString() + " }");
        }
 
        return html;
    }
}

[ExcludeMetadata]
public class ScriptAdmin : IReturn<ScriptAdminResponse>
{
    public string Actions { get; set; }
}

public class ScriptAdminResponse
{
    public string[] Results { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[DefaultRequest(typeof(ScriptAdmin))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class ScriptAdminService : Service
{
    public static string[] Routes { get; set; } = { "/script/admin", "/script/admin/{Actions}" }; 
        
    public static string[] Actions = {
        nameof(ProtectedScripts.invalidateAllCaches),
        nameof(SharpPagesFeature.RunInitPage),
    };
        
    public async Task<object> Any(ScriptAdmin request)
    {
        var feature = HostContext.AssertPlugin<SharpPagesFeature>();
            
        await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.ScriptAdminRole);
            
        if (string.IsNullOrEmpty(request.Actions))
            return new ScriptAdminResponse { Results = new[]{ "Available actions: " + string.Join(",", Actions) } };

        var actions = request.Actions.Split(',');

        var results = new List<string>();
        using var ms = MemoryStreamFactory.GetStream();
        var scope = new ScriptScopeContext(new PageResult(feature.EmptyPage), ms, new Dictionary<string, object>());
            
        if (actions.Any(x => x.EqualsIgnoreCase(nameof(ProtectedScripts.invalidateAllCaches))))
            results.Add(nameof(ProtectedScripts.invalidateAllCaches) + ": " + feature.ProtectedMethods.invalidateAllCaches(scope).ToJsv());
                
        if (actions.Any(x => x.EqualsIgnoreCase(nameof(SharpPagesFeature.RunInitPage))))
            results.Add(nameof(SharpPagesFeature.RunInitPage) + ": " + feature.RunInitPage());
                
        if (results.Count > 0)
            return new ScriptAdminResponse { Results = results.ToArray() };

        throw new NotSupportedException("Unknown Action. Available actions: " + string.Join(",", Actions));
    }
}

public class SharpPageHandler : HttpAsyncTaskHandler
{
    public Action<IRequest> Filter { get; set; }
    public Func<IRequest,bool> ValidateFn { get; set; }
        
    public SharpPage Page { get; private set; }
    public SharpPage LayoutPage { get; private set; }
    public Dictionary<string, object> Args { get; set; }
    public object Model { get; set; }
    public Stream OutputStream { get; set; }
    private readonly string pagePath; 
    private readonly string layoutPath;
        
    public ScriptContext Context { get; set; }

    public static ScriptContext NewContext(IAppHost appHost) =>
        new ScriptContext().InitForSharpPages(appHost).UseAppHost(appHost).Init();

    public SharpPageHandler(string pagePath, string layoutPath=null)
    {
        this.RequestName = this.pagePath = pagePath ?? throw new ArgumentNullException(nameof(pagePath));
        this.layoutPath = layoutPath;
    }

    public SharpPageHandler(SharpPage page, SharpPage layoutPage = null)
    {
        this.RequestName = !string.IsNullOrEmpty(page.VirtualPath) ? page.VirtualPath : nameof(SharpPageHandler);
        this.Page = page;
        this.LayoutPage = layoutPage;
    }

    public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
    {
        httpReq.UseBufferedStream = true;

        if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
            return;

        Filter?.Invoke(httpReq);

        if (ValidateFn != null && !ValidateFn(httpReq))
        {
            httpRes.StatusCode = (int) HttpStatusCode.Forbidden;
            httpRes.StatusDescription = "Request Validation Failed";
            await httpRes.EndRequestAsync();
            return;
        }
            
        if (Page == null && pagePath != null)
        {
            var pages = Context != null
                ? Context.Pages 
                : httpReq.TryResolve<ISharpPages>();
                
            Page = pages.GetPage(pagePath)
                   ?? throw new FileNotFoundException($"Sharp Page not found '{pagePath}'");

            if (!string.IsNullOrEmpty(layoutPath))
            {
                LayoutPage = pages.GetPage(layoutPath) 
                             ?? throw new FileNotFoundException($"Sharp Page not found '{layoutPath}'");
            }
        }

        if (Page == null)
            throw new NotSupportedException($"SharpPageHandler needs to be initialized with a Page or a PagePath");
            
        var context = Context ?? HostContext.GetPlugin<SharpPagesFeature>();
            
        var args = httpReq.GetScriptRequestParams(importRequestParams:(context as SharpPagesFeature)?.ImportRequestParams ?? false);
        if (Args != null)
        {
            foreach (var entry in Args)
            {
                args[entry.Key] = entry.Value;
            }
        }
            
        var pageResult = new PageResult(Page)
        {
            Args = args,
            LayoutPage = LayoutPage,
            Model = Model,
        };

        try
        {
            httpRes.ContentType = Page.Format.ContentType;
            if (OutputStream != null)
            {
                await pageResult.WriteToAsync(OutputStream);
            }
            else
            {
                // Buffering improves perf when running behind a reverse proxy (recommended for .NET Core) 
                using var ms = MemoryStreamFactory.GetStream();
                await pageResult.WriteToAsync(ms);

                if (pageResult.ReturnValue != null)
                {
                    var response = pageResult.ReturnValue?.Result; 
                    if (response is Task<object> responseTask)
                        response = await responseTask;
                    if (response is IRawString raw)
                        response = raw.ToRawString();

                    if (response != null)
                    {
                        var httpResult = SharpApiService.ToHttpResult(pageResult, response);
                        if (httpReq.ResponseContentType == MimeTypes.Csv)
                        {
                            var fileName = httpReq.OperationName + ".csv";
                            httpRes.AddHeader(HttpHeaders.ContentDisposition, $"attachment;{HttpExt.GetDispositionFileName(fileName)}");
                        }
                        await httpRes.WriteToResponse(httpReq, httpResult);
                    }
                    return;
                }

                ms.Position = 0;
                await ms.WriteToAsync(httpRes.OutputStream);
            }
        }
        catch (Exception ex)
        {
            await Page.Format.OnViewException(pageResult, httpReq, ex);
        }
    }
}

public class SharpCodePageHandler : HttpAsyncTaskHandler
{
    private readonly SharpCodePage page;
    private readonly SharpPage layoutPage;
    public object Model { get; set; }
    public Stream OutputStream { get; set; }
        
    public Dictionary<string, object> Args { get; set; }

    public SharpCodePageHandler(SharpCodePage page, SharpPage layoutPage = null)
    {
        this.page = page;
        this.layoutPage = layoutPage;
    }

    public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
    {
        httpReq.UseBufferedStream = true;

        if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
            return;

        if (page is IRequiresRequest requiresRequest)
            requiresRequest.Request = httpReq;
            
        var feature = HostContext.GetPlugin<SharpPagesFeature>();

        var result = new PageResult(page)
        {
            //import request params for code pages so they can be accessed from render parameters
            Args = httpReq.GetScriptRequestParams(feature.ImportRequestParams), 
            LayoutPage = layoutPage,
            Model = Model,
        };

        if (Args != null)
        {
            foreach (var entry in Args)
            {
                result.Args[entry.Key] = entry.Value;
            }
        }

        try
        {
            httpRes.ContentType = page.Format.ContentType;
            await result.WriteToAsync(OutputStream ?? httpRes.OutputStream);
        }
        catch (Exception ex)
        {
            await page.Format.OnViewException(result, httpReq, ex);
        }
    }
}

public abstract class ServiceStackCodePage : SharpCodePage, IRequiresRequest
{
    public IRequest Request { get; set; }

    public virtual IResolver GetResolver() => Service.GlobalResolver;

    public virtual T TryResolve<T>()
    {
        return this.GetResolver() == null
            ? default(T)
            : this.GetResolver().TryResolve<T>();
    }

    public virtual T ResolveService<T>()
    {
        var service = TryResolve<T>();
        return HostContext.ResolveService(this.Request, service);
    }

    protected virtual IResponse Response => Request?.Response;

    private ICacheClient cache;
    public virtual ICacheClient Cache => cache ??= HostContext.AppHost.GetCacheClient(Request);

    private MemoryCacheClient localCache;

    public virtual MemoryCacheClient LocalCache => localCache ??= HostContext.AppHost.GetMemoryCacheClient(Request);

    private IDbConnection db;
    public virtual IDbConnection Db => db ??= HostContext.AppHost.GetDbConnection(Request);

    private IRedisClient redis;
    public virtual IRedisClient Redis => redis ??= HostContext.AppHost.GetRedisClient(Request);

    private IMessageProducer messageProducer;

    public virtual IMessageProducer MessageProducer => messageProducer ??= HostContext.AppHost.GetMessageProducer(Request);

    private ISessionFactory sessionFactory;

    public virtual ISessionFactory SessionFactory =>
        sessionFactory ?? (sessionFactory = TryResolve<ISessionFactory>()) ?? new SessionFactory(Cache);

    private IAuthRepository authRepository;

    public virtual IAuthRepository AuthRepository => authRepository ??= HostContext.AppHost.GetAuthRepository(Request);

    private IServiceGateway gateway;
    public virtual IServiceGateway Gateway => gateway ??= HostContext.AppHost.GetServiceGateway(Request);

    public IVirtualPathProvider VirtualFileSources => HostContext.VirtualFileSources;

    public IVirtualFiles VirtualFiles => HostContext.VirtualFiles;

    private ISession session;

    public virtual ISession SessionBag => session ??= TryResolve<ISession>() //Easier to mock
                                                      ?? SessionFactory.GetOrCreateSession(Request, Response);

    public virtual IAuthSession GetSession(bool reload = false)
    {
        var req = this.Request;
        if (req.GetSessionId() == null)
            req.Response.CreateSessionIds(req);
        return req.GetSession(reload);
    }

    protected virtual TUserSession SessionAs<TUserSession>()
    {
        if (HostContext.TestMode)
        {
            var mockSession = TryResolve<TUserSession>();
            if (Equals(mockSession, default(TUserSession)))
                mockSession = TryResolve<IAuthSession>() is TUserSession
                    ? (TUserSession) TryResolve<IAuthSession>()
                    : default(TUserSession);

            if (!Equals(mockSession, default(TUserSession)))
                return mockSession;
        }

        return SessionFeature.GetOrCreateSession<TUserSession>(Cache, Request, Response);
    }

    public virtual bool IsAuthenticated => this.GetSession().IsAuthenticated;

    public virtual void PublishMessage<T>(T message) => HostContext.AppHost.PublishMessage(MessageProducer, message);

    public override void Dispose()
    {
        db?.Dispose();
        redis?.Dispose();
        messageProducer?.Dispose();
        using (authRepository as IDisposable) {}
    }
}

public static class SharpPagesFeatureExtensions
{
    public static ScriptContext InitForSharpPages(this ScriptContext context, IAppHost appHost)
    {
        context.Container = appHost.GetContainer();
        context.AllowScriptingOfAllTypes = true;
        context.ScriptNamespaces.AddRange(new [] {
            "System",
            "System.Collections",
            "System.Collections.Generic",
            "ServiceStack",
        });
        context.ScriptMethods.Add(new ProtectedScripts());
        context.ScriptMethods.Add(new InfoScripts());
        context.ScriptMethods.Add(new ServiceStackScripts());
        context.ScriptMethods.Add(new ValidateScripts());
        context.ScriptMethods.Add(new BootstrapScripts());
        context.Plugins.Add(new ServiceStackScriptBlocks());
        context.Plugins.Add(new MarkdownScriptPlugin { RegisterPageFormat = false });
        context.SkipExecutingFiltersIfError = true;
        return context;
    }

    public static ScriptContext UseAppHost(this ScriptContext context, IAppHost appHost)
    {
        context.DebugMode = appHost.Config.DebugMode;
        context.VirtualFiles = appHost.VirtualFileSources;
        context.AppSettings = appHost.AppSettings;
        return context;
    }

    public static Dictionary<string, object> GetScriptRequestParams(this IRequest request, bool importRequestParams=false)
    {
        var to = importRequestParams
            ? request.GetRequestParams(exclude:null).ToObjectDictionary()
            : new Dictionary<string, object>();
            
        to[nameof(IRequest.RawUrl)] = request.RawUrl;
        to[ScriptConstants.PathBase] = HostContext.Config.PathBase;
        to[ScriptConstants.PathInfo] = request.OriginalPathInfo;
        to[nameof(IRequest.AbsoluteUri)] = request.AbsoluteUri;
        to[nameof(IRequest.Verb)] = to["Method"] = request.Verb;

        foreach (var item in request.Items)
        {
            to[item.Key.Trim('_')] = item.Value; //Remove __ prefixes for internal Keywords
        }

        to[ScriptConstants.Request] = request;

        if (request.GetItem(Keywords.DbInfo) is ConnectionInfo connInfo)
            to[Keywords.DbInfo] = connInfo;
            
        return to;
    }
        
    public static SharpCodePage GetCodePage(this IRequest request, string virtualPath)
    {
        return HostContext.AssertPlugin<SharpPagesFeature>().GetCodePage(virtualPath).With(request);
    }
        
    public static SharpPage GetPage(this IRequest request, string virtualPath)
    {
        return HostContext.AssertPlugin<SharpPagesFeature>().GetPage(virtualPath);
    }

    private static Uri createUri(string url)
    {
        try
        {
            return new Uri(url);
        }
        catch {}
        return null;
    }

    public static Dictionary<string, object> CreateRequestArgs(Dictionary<string, object> args)
    {
        string getValue(string name) => args != null && args.TryGetValue(name, out var oValue)
            ? oValue?.ToString()
            : null;
            
        var baseUrl = getValue(ScriptConstants.BaseUrl) ?? "http://localhost/";
        var method = getValue(nameof(IRequest.Verb)) ?? HttpMethods.Get;
        return SetRequestArgs(args, new BasicHttpRequest {
            RawUrl = baseUrl,
            AbsoluteUri = getValue(nameof(IRequest.AbsoluteUri)) ?? baseUrl,
            PathInfo = getValue(nameof(IRequest.PathInfo)) ?? "/", 
            Verb = method,
            HttpMethod = method,
            UrlReferrer = createUri(getValue(nameof(IRequest.UrlReferrer))),
        });
    }
        
    public static PageResult BindRequest(this PageResult result, IRequest request)
    {
        SetRequestArgs(result.Args, request);
        return result;
    }

    public static Dictionary<string, object> SetRequestArgs(Dictionary<string, object> args, IRequest request)
    {
        args["Request"] = request;
        args[nameof(request.RawUrl)] = request.RawUrl;
        args[ScriptConstants.PathBase] = HostContext.Config.PathBase;
        args[ScriptConstants.PathInfo] = request.OriginalPathInfo;
        args[nameof(request.AbsoluteUri)] = request.AbsoluteUri;
        args[nameof(request.Verb)] = args["Method"] = request.Verb;
        args[ScriptConstants.BaseUrl] = request.GetBaseUrl();
        return args;
    }

    public static PageResult GetPageResult(this IRequest request, string virtualPath, Dictionary<string,object> args=null)
    {
        var page = HostContext.AssertPlugin<SharpPagesFeature>().GetPage(virtualPath);
        var pageResult = new PageResult(page).BindRequest(request);
        if (args != null)
        {
            foreach (var entry in args)
            {
                pageResult.Args[entry.Key] = entry.Value;
            }
        }
        return pageResult;
    }
        
    public static SharpPage OneTimePage(this IRequest request, string contents, string ext=null)
    {
        return HostContext.AssertPlugin<SharpPagesFeature>().OneTimePage(contents, ext);
    }
        
    public static SharpCodePage With(this SharpCodePage page, IRequest request)
    {
        if (page is IRequiresRequest requiresRequest)
            requiresRequest.Request = request;
        return page;
    }
        
    public static ServiceStackScripts GetServiceStackFilters(this ScriptContext context) =>
        context.ScriptMethods.FirstOrDefault(x => x is ServiceStackScripts) as ServiceStackScripts;
}

public interface IAutoQueryDbFilters
{
    object sendToAutoQuery(ScriptScopeContext scope, object dto, string requestName, object options);
}