using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Razor2.Templating;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.Razor2
{
    public enum RazorPageType
    {
        ContentPage = 1,
        ViewPage = 2,
        SharedViewPage = 3,
        Template = 4,
    }

    public class RazorFormat : IRazorViewEngine, IPlugin, IRazor2Plugin
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RazorFormat));


        private static RazorFormat instance;
        public static RazorFormat Instance
        {
            get { return instance ?? (instance = new RazorFormat()); }
        }

        public static bool IsRegistered
        {
            get { return instance != null; }
        }

        private const string ErrorPageNotFound = "Could not find Razor page '{0}'";

        public static string DefaultTemplateName = "_Layout.cshtml";
        public static string DefaultTemplate = "_Layout";
        public static string DefaultPage = "default";
        public static string TemplatePlaceHolder = "@RenderBody()";

        // ~/View - Dynamic Pages
        public Dictionary<string, ViewPageRef> ViewPages;

        // ~/View/Shared - Dynamic Shared Pages
        public Dictionary<string, ViewPageRef> ViewSharedPages;

        //Content Pages outside of ~/View
        public Dictionary<string, ViewPageRef> ContentPages;

        public Dictionary<string, ViewPageRef> MasterPageTemplates;

        public IAppHost AppHost { get; set; }

        public Dictionary<string, string> ReplaceTokens { get; set; }

        public Func<string, IEnumerable<ViewPageRef>> FindRazorPagesFn { get; set; }

        public Func<object, IHttpRequest, ViewPageRef> ResolveViewFn { get; set; }

        public IVirtualPathProvider VirtualPathProvider { get; set; }

        public HashSet<string> TemplateNamespaces { get; set; }

        public bool WatchForModifiedPages { get; set; }

        public Dictionary<string, Type> RazorExtensionBaseTypes { get; set; }

        public TemplateProvider TemplateProvider { get; set; }

        public List<string> SkipPaths { get; set; }

        public Type DefaultBaseType
        {
            get
            {
                Type baseType;
                return RazorExtensionBaseTypes.TryGetValue("cshtml", out baseType) ? baseType : null;
            }
            set
            {
                RazorExtensionBaseTypes["cshtml"] = value;
            }
        }

        public TemplateService TemplateService
        {
            get
            {
                TemplateService templateService;
                return templateServices.TryGetValue("cshtml", out templateService) ? templateService : null;
            }
        }

        public RazorFormat()
        {
            this.FindRazorPagesFn = FindRazorPages;
            this.ResolveViewFn = GetViewPageByResponse;
            this.ReplaceTokens = new Dictionary<string, string>();
            this.TemplateNamespaces = new HashSet<string> {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "ServiceStack.Html",
                "ServiceStack.Razor2",
            };
            this.RazorExtensionBaseTypes = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase) {
				{"cshtml", typeof(ViewPage<>) },
				{"rzr", typeof(ViewPage<>) },
			};
            this.TemplateProvider = new TemplateProvider(DefaultTemplateName) {
                CompileInParallelWithNoOfThreads = null,
            };
            //Skip scanning common VS.NET extensions
            this.SkipPaths = new List<string> {
                "/obj/", 
                "/bin/",
            };
        }

        public void Register(IAppHost appHost)
        {
            if (instance == null) instance = this;
            Configure(appHost);
        }

        static readonly char[] DirSeps = new[] { '\\', '/' };
        static HashSet<string> catchAllPathsNotFound = new HashSet<string>();

        public void Configure(IAppHost appHost)
        {
            this.AppHost = appHost;
            appHost.ViewEngines.Add(this);

            //Default to watching modfied pages in DebugMode
            if (!WatchForModifiedPages)
                WatchForModifiedPages = appHost.Config.DebugMode;

            //Default to parallel execution in DebugMode
            if (!this.TemplateProvider.CompileInParallelWithNoOfThreads.HasValue)
                this.TemplateProvider.CompileInParallelWithNoOfThreads = appHost.Config.DebugMode
                    ? Environment.ProcessorCount * 2
                    : 0;

            foreach (var ns in EndpointHostConfig.RazorNamespaces)
                TemplateNamespaces.Add(ns);

            this.ReplaceTokens = appHost.Config.HtmlReplaceTokens ?? new Dictionary<string, string>();
            var webHostUrl = appHost.Config.WebHostUrl;

            if (VirtualPathProvider == null)
                VirtualPathProvider = AppHost.VirtualPathProvider;

            Init();

            RegisterRazorPages(appHost.Config.WebHostPhysicalPath);

            appHost.CatchAllHandlers.Add((httpMethod, pathInfo, filePath) => {
                ViewPageRef razorPage = null;

                if (catchAllPathsNotFound.Contains(pathInfo))
                    return null;

                try
                {
                    razorPage = FindByPathInfo( pathInfo );
                }
                catch(TemplateCompilationException tcex)
                {
                    if ( appHost.Config.DebugMode && tcex.HttpCompileException != null )
                    {
                        throw tcex.HttpCompileException;
                    }
                    throw;
                }

                if (WatchForModifiedPages)
                    ReloadIfNeeeded(razorPage);

                if (razorPage == null)
                {
                    foreach(var entry in RazorExtensionBaseTypes)
                    {
                        if (pathInfo.EndsWith("." + entry.Key))
                        {
                            pathInfo = pathInfo.EndsWith(DefaultPage + "." + entry.Key, StringComparison.InvariantCultureIgnoreCase)
                                ? pathInfo.Substring(0, pathInfo.Length - (DefaultPage + "." + entry.Key).Length)
                                : pathInfo.WithoutExtension();

                            return new RedirectHttpHandler {
                                AbsoluteUrl = webHostUrl.IsNullOrEmpty() 
                                    ? null
                                    : webHostUrl.CombineWith(pathInfo),
                                RelativeUrl = webHostUrl.IsNullOrEmpty()
                                    ? pathInfo
                                    : null
                            };
                        }
                    }

                    if (catchAllPathsNotFound.Count > 1000) //prevent DDOS
                        catchAllPathsNotFound = new HashSet<string>();

					var tmp = new HashSet<string>(catchAllPathsNotFound) { pathInfo };
                    catchAllPathsNotFound = tmp;
                    return null;
                }

                return new RazorHandler(pathInfo) {
                    RazorFormat = this,
                    RazorPage = razorPage,
                    RequestName = "RazorPage",
                };
            });
        }

        public ViewPageRef FindByPathInfo(string pathInfo)
        {
            var normalizedPathInfo = pathInfo.IsNullOrEmpty() ? DefaultPage : pathInfo.TrimStart(DirSeps);
            var razorPage = GetContentPage(
                normalizedPathInfo,
                normalizedPathInfo.CombineWith(DefaultPage));
            return razorPage;
        }

        public bool ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, object dto)
        {
            ViewPageRef razorPage;
            if ((razorPage = ResolveViewFn(dto, httpReq)) == null)
                return false;

            if (WatchForModifiedPages)
                ReloadIfNeeeded(razorPage);

            return ProcessRazorPage(httpReq, razorPage, dto, httpRes);
        }

        public bool HasView(string viewName, IHttpRequest httpReq=null)
        {
            return GetTemplateService(viewName, httpReq) != null;
        }

        public string RenderPartial(string pageName, object model, bool renderHtml, HtmlHelper htmlHelper = null)
        {
            var httpReq = htmlHelper.GetHttpRequest();
            var template = GetTemplateService(pageName, httpReq);
            if (template == null)
            {
                string result = null;
                foreach (var viewEngine in AppHost.ViewEngines)
                {
                    if (viewEngine == this || !viewEngine.HasView(pageName, httpReq)) continue;
                    result = viewEngine.RenderPartial(pageName, model, renderHtml, htmlHelper);
                    if (result != null) break;
                }
                return result ?? "<!--{0} not found-->".Fmt(pageName);
            }

            //Razor writes partial to static StringBuilder so don't return or it will write zx2
            template.RenderPartial(model, pageName, htmlHelper);

            //return template.Result;
            return null;
        }
        
        private Dictionary<string, TemplateService> templateServices;
        private TemplateService[] templateServicesArray;

        public void Init()
        {
            ViewPages = new Dictionary<string, ViewPageRef>(StringComparer.CurrentCultureIgnoreCase);
            // ~/View/Shared - Dynamic Shared Pages
            ViewSharedPages = new Dictionary<string, ViewPageRef>(StringComparer.CurrentCultureIgnoreCase);
            //Content Pages outside of ~/View
            ContentPages = new Dictionary<string, ViewPageRef>(StringComparer.CurrentCultureIgnoreCase);
            MasterPageTemplates = new Dictionary<string, ViewPageRef>(StringComparer.CurrentCultureIgnoreCase);

            //Force Binder to load
            var loaded = typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly != null;
            if (!loaded)
                throw new ConfigurationErrorsException("Microsoft.CSharp not properly loaded");

            templateServices = new Dictionary<string, TemplateService>(StringComparer.CurrentCultureIgnoreCase);

            foreach (var entry in RazorExtensionBaseTypes)
            {
                var razorBaseType = entry.Value;
                if (razorBaseType != null && !razorBaseType.HasInterface(typeof(ITemplatePage)))
                    throw new ConfigurationErrorsException(razorBaseType.FullName + " must inherit from RazorBasePage");

                var ext = entry.Key[0] == '.' ? entry.Key.Substring(1) : entry.Key;
                templateServices[ext] = new TemplateService(this, razorBaseType) {
                    Namespaces = TemplateNamespaces
                };
            }

            templateServicesArray = templateServices.Values.ToArray();
        }

        public bool ProcessRazorPage(IHttpRequest httpReq, ViewPageRef razorPage, object dto, IHttpResponse httpRes)
        {
            //Add extensible way to control caching
            //httpRes.AddHeaderLastModified(razorPage.GetLastModified());

            var razorTemplate = ExecuteTemplate(dto, razorPage.PageName, razorPage.Template, httpReq, httpRes);
            var html = razorTemplate.Result;

            var htmlBytes = html.ToUtf8Bytes();

            //var hasBom = html.Contains((char)65279);
            //TODO: Replace sad hack replacing the BOM with whitespace (ASP.NET+Mono):
            for (var i=0; i < htmlBytes.Length - 3; i++)
            {
                if (htmlBytes[i] == 0xEF && htmlBytes[i + 1] == 0xBB && htmlBytes[i + 2] == 0xBF)
                {
                    htmlBytes[i] = (byte)' ';
                    htmlBytes[i + 1] = (byte)' ';
                    htmlBytes[i + 2] = (byte)' ';
                }
            }

            httpRes.OutputStream.Write(htmlBytes, 0, htmlBytes.Length);

            var disposable = razorTemplate as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
            httpRes.EndServiceStackRequest(skipHeaders: true);

            return true;
        }

        public void ReloadIfNeeeded(ViewPageRef razorPage)
        {
            if (razorPage == null || razorPage.FilePath == null || !WatchForModifiedPages) return;
            
            var latestPage = GetLatestPage(razorPage);
            if (latestPage.LastModified > razorPage.LastModified)
                razorPage.Reload(GetPageContents(latestPage), latestPage.LastModified);
            
            ViewPageRef template;
            if (razorPage.Template != null
                && this.MasterPageTemplates.TryGetValue(razorPage.Template, out template))
            {
                latestPage = GetLatestPage(template);
                if (latestPage.LastModified > template.LastModified)
                    template.Reload(GetPageContents(latestPage), latestPage.LastModified);
            }
        }

        private IVirtualFile GetLatestPage(ViewPageRef razorPage)
        {
            var file = VirtualPathProvider.GetFile(razorPage.FilePath);
            return file;
        }

        private ViewPageRef GetViewPageByResponse(object dto, IHttpRequest httpReq)
        {
            var httpResult = dto as IHttpResult;
            if (httpResult != null)
            {
                dto = httpResult.Response;
            }

            //If View was specified don't look for anything else.
            var viewName = httpReq.GetView();
            if (!string.IsNullOrEmpty(viewName))
                return GetViewPage(viewName);

            //Since the Request DTO Name should be unique, look for a viewName with that first
            if (httpReq != null && httpReq.OperationName != null)
            {
                var pageRef = GetViewPage(httpReq.OperationName);
                if (pageRef != null) 
                    return pageRef;
            }

            if (dto != null)
            {
                var responseTypeName = dto.GetType().Name;
                var pageRef = GetViewPage(responseTypeName);
                if (pageRef != null) return pageRef;
            }

            return null;
        }

        public IViewPage GetView(string name)
        {
            return GetViewPage(name);
        }

        public void EnsureAllCompiled()
        {
            TemplateProvider.EnsureAllCompiled();
        }

        public ViewPageRef GetViewPage(string pageName)
        {
            if (pageName == null) return null;

            ViewPageRef razorPage;

            ViewPages.TryGetValue(pageName, out razorPage);
            if (razorPage != null)
            {
                if (WatchForModifiedPages)
                    ReloadIfNeeeded(razorPage);

                razorPage.EnsureCompiled();
                return razorPage;
            }

            ViewSharedPages.TryGetValue(pageName, out razorPage);
            if (razorPage != null)
            {
                if (WatchForModifiedPages)
                    ReloadIfNeeeded(razorPage);

                razorPage.EnsureCompiled();
            }
            return razorPage;
        }
        
        private void RegisterRazorPages(string razorSearchPath)
        {
            foreach (var page in FindRazorPagesFn(razorSearchPath))
            {
                AddPage(page);
            }

            try
            {
                TemplateProvider.CompileQueuedPages();
            }
            catch (Exception ex)
            {
                HandleCompilationException(null, ex);
            }
        }

        public IEnumerable<ViewPageRef> FindRazorPages(string dirPath)
        {
            var hasReloadableWebPages = false;
            foreach (var entry in templateServices)
            {
                var ext = entry.Key;
                var csHtmlFiles = VirtualPathProvider.GetAllMatchingFiles("*." + ext).ToList();
                foreach (var csHtmlFile in csHtmlFiles)
                {
                    if (ShouldSkipPath(csHtmlFile)) continue;

					if (csHtmlFile.GetType().Name != "ResourceVirtualFile")
                        hasReloadableWebPages = true;

                    var pageName = csHtmlFile.Name.WithoutExtension();
                    var pageContents = GetPageContents(csHtmlFile);

                    var pageType = RazorPageType.ContentPage;
                    if (VirtualPathProvider.IsSharedFile(csHtmlFile))
                        pageType = RazorPageType.SharedViewPage;
                    else if (VirtualPathProvider.IsViewFile(csHtmlFile))
                        pageType = RazorPageType.ViewPage;

                    var templateService = entry.Value;
                    templateService.RegisterPage(csHtmlFile.VirtualPath, pageName);

                    var templatePath = pageType == RazorPageType.ContentPage
						? TemplateProvider.GetTemplatePath(csHtmlFile.Directory)
                        : null;

                    yield return new ViewPageRef(this, csHtmlFile.VirtualPath, pageName, pageContents, pageType) {
                        Template = templatePath,
                        LastModified = csHtmlFile.LastModified,
                        Service = templateService
                    };
                }
            }

            if (!hasReloadableWebPages)
                WatchForModifiedPages = false;
        }

        private bool ShouldSkipPath(IVirtualFile csHtmlFile)
        {
            foreach (var skipPath in SkipPaths)
            {
                if (csHtmlFile.VirtualPath.StartsWith(skipPath, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        public void AddPage(ViewPageRef page)
        {
            try
            {
                TemplateProvider.QueuePageToCompile(page);
                AddViewPage(page);
            }
            catch (Exception ex)
            {
                HandleCompilationException(page, ex);
            }

            try
            {
                var templatePath = page.Template;
                if (page.Template == null) return;

                if (MasterPageTemplates.ContainsKey(templatePath)) return;

                var templateFile = VirtualPathProvider.GetFile(templatePath);
                var templateContents = GetPageContents(templateFile);
                AddTemplate(templatePath, templateContents);
            }
            catch (Exception ex)
            {
                Log.Error("Error compiling template " + page.Template + ": " + ex.Message, ex);
            }
        }

        private void HandleCompilationException(ViewPageRef page, Exception ex)
        {
            var tcex = ex as TemplateCompilationException;
            if (page == null)
            {
                Log.Error("Error compiling Razor page", ex);
                if (tcex != null)
                {
                    Log.Error(tcex.Errors.Dump());
                }
                return;
            }

            if (tcex != null)
            {
                Log.Error("Error compiling page {0}".Fmt(page.Name));
                Log.Error(tcex.Errors.Dump());
            }

            var errorViewPage = new ErrorViewPage(this, ex) {
                Name = page.Name,
                PageType = page.PageType,
                FilePath = page.FilePath,
            };
            errorViewPage.Compile();
            AddViewPage(errorViewPage);
            Log.Error("Razor AddViewPage() page.Prepare(): " + ex.Message, ex);
        }

        private void AddViewPage(ViewPageRef page)
        {
            switch (page.PageType)
            {
                case RazorPageType.ViewPage:
                    ViewPages.Add(page.Name, page);
                    break;
                case RazorPageType.SharedViewPage:
                    ViewSharedPages.Add(page.Name, page);
                    break;
                case RazorPageType.ContentPage:
                    ContentPages.Add(page.FilePath.WithoutExtension().TrimStart(DirSeps), page);
                    break;
            }
        }

        public ViewPageRef AddTemplate(string templatePath, string templateContents)
        {
            var templateFile = VirtualPathProvider.GetFile(templatePath);
            var templateName = templateFile.Name.WithoutExtension();

            TemplateService templateService;
            if (!templateServices.TryGetValue(templateFile.Extension, out templateService))
                throw new ConfigurationErrorsException(
                    "No BaseType registered with extension " + templateFile.Extension + " for template " + templateFile.Name);

            var template = new ViewPageRef(this, templatePath, templateName, templateContents, RazorPageType.Template) {
                LastModified = templateFile.LastModified,
                Service = templateService,
            };

            MasterPageTemplates.Add(templatePath, template);
            
            try
            {
                //template.Compile();
                TemplateProvider.QueuePageToCompile(template);
                return template;
            }
            catch (Exception ex)
            {
                Log.Error("AddViewPage() template.Prepare(): " + ex.Message, ex);
                return null;
            }
        }

        private string GetPageContents(IVirtualFile page)
        {
            return ReplaceContentWithRewriteTokens(page.ReadAllText());
        }

        private string ReplaceContentWithRewriteTokens(string contents)
        {
            foreach (var replaceToken in ReplaceTokens)
            {
                contents = contents.Replace(replaceToken.Key, replaceToken.Value);
            }
            return contents;
        }

        public ViewPageRef GetContentPage(string pageFilePath)
        {
            ViewPageRef razorPage;
            if (ContentPages.TryGetValue(pageFilePath, out razorPage))
            {
                razorPage.EnsureCompiled();
            }
            return razorPage;
        }

        public ViewPageRef GetContentPage(params string[] pageFilePaths)
        {
            foreach (var pageFilePath in pageFilePaths)
            {
                var razorPage = GetContentPage(pageFilePath);
                if (razorPage != null)
                    return razorPage;
            }
            return null;
        }

        public string GetTemplate(string name)
        {
            ViewPageRef template;
            MasterPageTemplates.TryGetValue(name, out template); //e.g. /NoModelNoController.cshtml
            if (template != null)
            {
                ReloadIfNeeeded(template);
                template.EnsureCompiled();
                return template.Contents;
            }
            return null;
        }
        
        public ITemplate CreateInstance(Type type)
        {
            var instance = type.CreateInstance();

            var templatePage = instance as ITemplatePage;
            if (templatePage != null)
            {
                templatePage.AppHost = AppHost;
                templatePage.ViewEngine = this;
            }

            var template = (ITemplate)instance;
            return template;
        }

        public string RenderStaticPage(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            filePath = filePath.WithoutExtension();

            ViewPageRef razorPage;
            if (!ContentPages.TryGetValue(filePath, out razorPage))
                throw new InvalidDataException(ErrorPageNotFound.FormatWith(filePath));

            var template = ExecuteTemplate((object)null, razorPage.PageName, razorPage.Template);

            return template.Result;
        }

        public IRazorTemplate ExecuteTemplate<T>(T model, string name, string templatePath)
        {
            return ExecuteTemplate(model, name, templatePath, null, null);
        }

        public bool HasTemplate(string pagePathOrName)
        {
            return GetTemplateService(pagePathOrName) != null;
        }

        public TemplateService GetTemplateService(string pagePathOrName)
        {
            foreach (var templateService in templateServicesArray)
            {
                if (TemplateService.ContainsPagePath(pagePathOrName))
                    return templateService;
            }

            foreach (var templateService in templateServicesArray)
            {
                if (TemplateService.ContainsPageName(pagePathOrName))
                    return templateService;
            }

            return null;
        }

        public TemplateService GetTemplateService(string pagePathOrName, IHttpRequest httpReq)
        {
            var serviceWithView = GetTemplateService(pagePathOrName);
            if (serviceWithView != null) return serviceWithView;
            if (httpReq == null || httpReq.PathInfo == null) return null;

            var normalizedPathInfo = httpReq.PathInfo;
            if (!httpReq.RawUrl.EndsWith("/"))
                normalizedPathInfo = normalizedPathInfo.ParentDirectory();

            normalizedPathInfo = normalizedPathInfo.CombineWith(pagePathOrName).TrimStart(DirSeps);

            var razorPage = GetContentPage(
                normalizedPathInfo,
                normalizedPathInfo.CombineWith(DefaultPage));

            if (razorPage != null && razorPage.FilePath != null)
                return GetTemplateService(razorPage.FilePath);
            
            return null;
        }

        public IRazorTemplate ExecuteTemplate<T>(T model, string name, string templatePath, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            return GetTemplateService(name).ExecuteTemplate(model, name, templatePath, httpReq, httpRes);
        }
    }
}