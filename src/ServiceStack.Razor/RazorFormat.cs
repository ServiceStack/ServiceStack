using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ServiceStack.Common;
using ServiceStack.Configuration;
using ServiceStack.Html;
using ServiceStack.Logging;
using ServiceStack.Razor.Compilation.CSharp;
using ServiceStack.Razor.Templating;
using ServiceStack.Razor.VirtualPath;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.Razor
{
    public enum RazorPageType
    {
        ContentPage = 1,
        ViewPage = 2,
        SharedViewPage = 3,
        Template = 4,
    }

    public class RazorFormat : IRazorViewEngine, IPlugin, IRazorPlugin
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RazorFormat));

        public const string NamespacesAppSettingsKey = "servicestack.razor.namespaces";

        private static RazorFormat instance;
        public static RazorFormat Instance
        {
            get { return instance ?? (instance = new RazorFormat()); }
        }

        private const string ErrorPageNotFound = "Could not find Razor page '{0}'";

        public static string TemplateName = "_Layout.cshtml";
        public static string TemplatePlaceHolder = "@RenderBody()";

        // ~/View - Dynamic Pages
        public Dictionary<string, ViewPage> ViewPages = new Dictionary<string, ViewPage>(
            StringComparer.CurrentCultureIgnoreCase);

        // ~/View/Shared - Dynamic Shared Pages
        public Dictionary<string, ViewPage> ViewSharedPages = new Dictionary<string, ViewPage>(
            StringComparer.CurrentCultureIgnoreCase);

        //Content Pages outside of ~/View
        public Dictionary<string, ViewPage> ContentPages = new Dictionary<string, ViewPage>(
            StringComparer.CurrentCultureIgnoreCase);

        public Dictionary<string, ViewPage> PageTemplates = new Dictionary<string, ViewPage>(
            StringComparer.CurrentCultureIgnoreCase);

        public IAppHost AppHost { get; set; }

        public Dictionary<string, string> ReplaceTokens { get; set; }

        public Func<string, IEnumerable<ViewPage>> FindRazorPagesFn { get; set; }

        public IVirtualPathProvider VirtualPathProvider { get; set; }

        public HashSet<string> TemplateNamespaces { get; set; }

        public bool WatchForModifiedPages { get; set; }

        public RazorFormat()
        {
            this.WatchForModifiedPages = true;
            this.FindRazorPagesFn = FindRazorPages;
            this.ReplaceTokens = new Dictionary<string, string>();
            this.TemplateNamespaces = new HashSet<string> {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "ServiceStack.Html",
                "ServiceStack.Razor",
            };
            RegisterNamespacesInConfig();
        }

        private void RegisterNamespacesInConfig()
        {
            //Infer from <system.web.webPages.razor> - what VS.NET's intell-sense uses
            var configPath = EndpointHostConfig.GetAppConfigPath();
            if (configPath != null)
            {
                var xml = configPath.ReadAllText();
                var doc = XElement.Parse(xml);
                doc.AnyElement("system.web.webPages.razor")
                    .AnyElement("pages")
                        .AnyElement("namespaces")
                            .AllElements("add").ToList()
                                .ForEach(x => TemplateNamespaces.Add(x.AnyAttribute("namespace").Value));
			}

            //E.g. <add key="servicestack.razor.namespaces" value="System,ServiceStack.Text" />
            if (ConfigUtils.GetNullableAppSetting(NamespacesAppSettingsKey) != null)
            {
                ConfigUtils.GetListFromAppSetting(NamespacesAppSettingsKey)
                    .ForEach(x => TemplateNamespaces.Add(x));
            }
        }

        public void Register(IAppHost appHost)
        {
            if (instance == null) instance = this;
            Configure(appHost);
        }

        public void Configure(IAppHost appHost)
        {
            this.AppHost = appHost;
            this.ReplaceTokens = new Dictionary<string, string>(appHost.Config.MarkdownReplaceTokens);
            if (!appHost.Config.WebHostUrl.IsNullOrEmpty())
                this.ReplaceTokens["~/"] = appHost.Config.WebHostUrl.WithTrailingSlash();

            if (VirtualPathProvider == null)
                VirtualPathProvider = new FileSystemVirtualPathProvider(AppHost);

            var razorBaseType = appHost.Config.RazorBaseType;
            Init(razorBaseType);

            RegisterRazorPages(appHost.Config.MarkdownSearchPath);

            //Render HTML
            appHost.HtmlProviders.Add((requestContext, dto, httpRes) => {

                var httpReq = requestContext.Get<IHttpRequest>();
                ViewPage razorPage;
                if ((razorPage = GetViewPageByResponse(dto, httpReq)) == null)
                    return false;

                if (WatchForModifiedPages)
                    ReloadModifiedPageAndTemplates(razorPage);

                return ProcessRazorPage(httpReq, razorPage, dto, httpRes);
            });

            appHost.CatchAllHandlers.Add((httpMethod, pathInfo, filePath) => {
                ViewPage razorPage = null;

                if (filePath != null)
                    razorPage = GetContentPage(filePath.WithoutExtension());

                if (razorPage == null)
                    razorPage = GetContentResourcePage(pathInfo);

                if (razorPage == null)
                    razorPage = GetContentPage(pathInfo);
                    
                return razorPage == null 
                    ? null
                    : new RazorHandler 
                {
                    RazorFormat = this,
                    RazorPage = razorPage,
                    RequestName = "RazorPage",
                    PathInfo = pathInfo,
                    FilePath = filePath
                };
            });
        }

        public void Init(Type razorBaseType = null)
        {
            if (razorBaseType != null && !razorBaseType.HasInterface(typeof(ITemplatePage)))
                throw new ConfigurationErrorsException(razorBaseType.FullName + " must inherit from RazorBasePage");

            //Force Binder to load
            var loaded = typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly != null;
            if (!loaded)
                throw new ConfigurationErrorsException("Microsoft.CSharp not properly loaded");

            var service = new TemplateService(this, new CSharpDirectCompilerService(), razorBaseType) {
                Namespaces = TemplateNamespaces
            };

            RazorHost.Configure(service);
        }

        public IEnumerable<ViewPage> FindRazorPages(string dirPath)
        {
            var csHtmlFiles = VirtualPathProvider.GetAllMatchingFiles("*.cshtml");

            var hasWebPages = false;
            foreach (var csHtmlFile in csHtmlFiles)
            {
                hasWebPages = true;

                var pageName = csHtmlFile.Name.WithoutExtension();
                var pageContents = csHtmlFile.ReadAllText();

                var pageType = RazorPageType.ContentPage;
                if (VirtualPathProvider.IsSharedFile(csHtmlFile))
                    pageType = RazorPageType.SharedViewPage;
                else if (VirtualPathProvider.IsViewFile(csHtmlFile))
                    pageType = RazorPageType.ViewPage;

                yield return new ViewPage(this, csHtmlFile.VirtualPath, pageName, pageContents, pageType)
                {
                    LastModified = csHtmlFile.LastModified
                };
            }

            if (!hasWebPages) 
                WatchForModifiedPages = false;
        }

        public bool ProcessRazorPage(IHttpRequest httpReq, ViewPage razorPage, object dto, IHttpResponse httpRes)
        {
            //Add extensible way to control caching
            //httpRes.AddHeaderLastModified(razorPage.GetLastModified());

            var templatePath = razorPage.TemplatePath;
            if (httpReq != null && httpReq.QueryString["format"] != null)
            {
                if (!httpReq.GetFormatModifier().StartsWithIgnoreCase("bare"))
                    templatePath = null;
            }

            var template = ExecuteTemplate(dto, razorPage.PageName, templatePath, httpReq, httpRes);
            var html = template.Result;
            var htmlBytes = html.ToUtf8Bytes();
            httpRes.OutputStream.Write(htmlBytes, 0, htmlBytes.Length);

            var disposable = template as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
            httpRes.EndServiceStackRequest(skipHeaders:true);

            return true;
        }

        public void ReloadModifiedPageAndTemplates(ViewPage razorPage)
        {
            if (razorPage.FilePath == null) return;

            var lastWriteTime = File.GetLastWriteTime(razorPage.FilePath);
            if (lastWriteTime > razorPage.LastModified)
            {
                razorPage.Reload();
            }

            ViewPage template;
            if (razorPage.DirectiveTemplatePath != null
                && this.PageTemplates.TryGetValue(razorPage.DirectiveTemplatePath, out template))
            {
                lastWriteTime = File.GetLastWriteTime(razorPage.DirectiveTemplatePath);
                if (lastWriteTime > template.LastModified)
                    ReloadTemplate(template);
            }
            if (razorPage.TemplatePath != null
                && this.PageTemplates.TryGetValue(razorPage.TemplatePath, out template))
            {
                lastWriteTime = File.GetLastWriteTime(razorPage.TemplatePath);
                if (lastWriteTime > template.LastModified)
                    ReloadTemplate(template);
            }
        }

        private void ReloadTemplate(ViewPage template)
        {
            var contents = File.ReadAllText(template.FilePath);
            foreach (var markdownReplaceToken in ReplaceTokens)
            {
                contents = contents.Replace(markdownReplaceToken.Key, markdownReplaceToken.Value);
            }
            template.Reload(contents);
        }

        private ViewPage GetViewPageByResponse(object dto, IHttpRequest httpRequest)
        {
            var httpResult = dto as IHttpResult;
            if (httpResult != null)
            {
                //If TemplateName was specified don't look for anything else.
                if (httpResult.TemplateName != null)
                    return GetViewPage(httpResult.TemplateName);

                dto = httpResult.Response;
            }
            if (dto != null)
            {
                var responseTypeName = dto.GetType().Name;
                var markdownPage = GetViewPage(responseTypeName);
                if (markdownPage != null) return markdownPage;
            }

            return httpRequest != null ? GetViewPage(httpRequest.OperationName) : null;
        }

        public ViewPage GetViewPage(string pageName)
        {
            ViewPage razorPage;

            ViewPages.TryGetValue(pageName, out razorPage);
            if (razorPage != null) return razorPage;

            ViewSharedPages.TryGetValue(pageName, out razorPage);
            return razorPage;
        }

        private void RegisterRazorPages(string razorSearchPath)
        {
            foreach (var page in FindRazorPagesFn(razorSearchPath))
            {
                AddPage(page);
            }
        }

        public void AddPage(ViewPage page)
        {
            try
            {
                page.Prepare();
                AddViewPage(page);
            }
            catch (Exception ex)
            {
                var errorViewPage = new ErrorViewPage(this, ex) {
                    Name = page.Name,
                    PageType = page.PageType,
                    FilePath = page.FilePath,
                };
                errorViewPage.Prepare();
                AddViewPage(errorViewPage);
                Log.Error("Razor AddViewPage() page.Prepare(): " + ex.Message, ex);
            }

            var templatePath = page.TemplatePath;
            if (page.TemplatePath == null) return;

            if (PageTemplates.ContainsKey(templatePath)) return;

            AddTemplate(templatePath, File.ReadAllText(templatePath));
        }

        private void AddViewPage(ViewPage page)
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
                    ContentPages.Add(page.FilePath.WithoutExtension(), page);
                    break;
            }
        }

        public ViewPage AddTemplate(string templatePath, string templateContents)
        {
            var templateFile = new FileInfo(templatePath);
            var templateName = templateFile.FullName.WithoutExtension();

            foreach (var markdownReplaceToken in ReplaceTokens)
            {
                templateContents = templateContents.Replace(markdownReplaceToken.Key, markdownReplaceToken.Value);
            }

            var template = new ViewPage(this, templatePath, templateName, templateContents, RazorPageType.Template) {
                LastModified = templateFile.LastWriteTime,
            };
            PageTemplates.Add(templatePath, template);
            try
            {
                template.Prepare();
                return template;
            }
            catch (Exception ex)
            {
                Log.Error("AddViewPage() template.Prepare(): " + ex.Message, ex);
                return null;
            }
        }

        public ViewPage GetContentPage(string pageFilePath)
        {
            ViewPage razorPage;
            ContentPages.TryGetValue(pageFilePath, out razorPage);
            return razorPage;
        }

        static readonly char[] DirSeps = new[] { '\\', '/' };
        public ViewPage GetContentResourcePage(string pathInfo)
        {
            ViewPage razorPage;
            ContentPages.TryGetValue(pathInfo.TrimStart(DirSeps), out razorPage);
            return razorPage;
        }

        public string GetTemplate(string name)
        {
            Console.WriteLine("GetTemplate(): " + name);
            ViewPage template;
            PageTemplates.TryGetValue(name, out template);
            return template != null ? template.Contents : null;
        }

        public ITemplate CreateInstance(Type type)
        {
            //Console.WriteLine("CreateInstance(): " + type.Name);
            var instance = type.CreateInstance();

            var templatePage = instance as ITemplatePage;
            if (templatePage != null)
            {
                templatePage.AppHost = AppHost;
            }

            var template = (ITemplate)instance;
            return template;
        }

        public string RenderStaticPage(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            filePath = filePath.WithoutExtension();

            ViewPage razorPage;
            if (!ContentPages.TryGetValue(filePath, out razorPage))
                throw new InvalidDataException(ErrorPageNotFound.FormatWith(filePath));

            return RenderStaticPage(razorPage);
        }

        private string RenderStaticPage(ViewPage markdownPage)
        {
            var template = ExecuteTemplate((object)null,
                markdownPage.PageName, markdownPage.TemplatePath);

            return template.Result;
        }

        public IRazorTemplate ExecuteTemplate<T>(T model, string name, string templatePath)
        {
            return ExecuteTemplate(model, name, templatePath, null, null);
        }

        public IRazorTemplate ExecuteTemplate<T>(T model, string name, string templatePath, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            return RazorHost.TemplateService.ExecuteTemplate(model, name, templatePath, httpReq, httpRes);
        }

        public string RenderPartial(string pageName, object model, bool renderHtml)
        {
            //Razor writes partial to static StringBuilder so don't return or it will write x2
            var template = RazorHost.TemplateService.RenderPartial(model, pageName);
            //return template.Result;
            return null;
        }
    }
}