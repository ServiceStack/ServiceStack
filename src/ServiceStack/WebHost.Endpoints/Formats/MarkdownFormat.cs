using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Html;
using ServiceStack.Logging;
using ServiceStack.Markdown;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support.Markdown;

namespace ServiceStack.WebHost.Endpoints.Formats
{
    public enum MarkdownPageType
    {
        ContentPage = 1,
        ViewPage = 2,
        SharedViewPage = 3,
    }

    public class MarkdownFormat : IViewEngine, IPlugin
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MarkdownFormat));

        private const string ErrorPageNotFound = "Could not find Markdown page '{0}'";

        public static string DefaultTemplateName = "default.shtml";
        public static string DefaultTemplate = "/Views/Shared/default.shtml";
        public static string TemplatePlaceHolder = "<!--@Body-->";
        public static string WebHostUrlPlaceHolder = "~/";
        public static string MarkdownExt = "md";
        public static string TemplateExt = "shtml";
        public static string SharedDir = "/Views/Shared";
        public static string[] PageExts = new[] { MarkdownExt, TemplateExt };

        public static MarkdownFormat Instance = new MarkdownFormat();

        // ~/View - Dynamic Pages
        public Dictionary<string, MarkdownPage> ViewPages = new Dictionary<string, MarkdownPage>(
            StringComparer.CurrentCultureIgnoreCase);

        // ~/View/Shared - Dynamic Shared Pages
        public Dictionary<string, MarkdownPage> ViewSharedPages = new Dictionary<string, MarkdownPage>(
            StringComparer.CurrentCultureIgnoreCase);

        //Content Pages outside of ~/View
        public Dictionary<string, MarkdownPage> ContentPages = new Dictionary<string, MarkdownPage>(
            StringComparer.CurrentCultureIgnoreCase);

        public Dictionary<string, MarkdownTemplate> MasterPageTemplates = new Dictionary<string, MarkdownTemplate>(
            StringComparer.CurrentCultureIgnoreCase);

        public Type MarkdownBaseType { get; set; }
        public Dictionary<string, Type> MarkdownGlobalHelpers { get; set; }

        public Func<string, IEnumerable<MarkdownPage>> FindMarkdownPagesFn { get; set; }

        private readonly MarkdownSharp.Markdown markdown;

        public IAppHost AppHost { get; set; }

        public Dictionary<string, string> MarkdownReplaceTokens { get; set; }

        public IVirtualPathProvider VirtualPathProvider { get; set; }

        public bool WatchForModifiedPages { get; set; }

        public MarkdownFormat()
        {
            markdown = new MarkdownSharp.Markdown();

            this.WatchForModifiedPages = true;
            this.MarkdownBaseType = typeof(MarkdownViewBase);
            this.MarkdownGlobalHelpers = new Dictionary<string, Type>();
            this.FindMarkdownPagesFn = FindMarkdownPages;
            this.MarkdownReplaceTokens = new Dictionary<string, string>();
        }

        static HashSet<string> catchAllPathsNotFound = new HashSet<string>();

        public void Register(IAppHost appHost)
        {
            this.AppHost = appHost;
            appHost.ViewEngines.Add(this);

            foreach (var ns in EndpointHostConfig.RazorNamespaces)
                Evaluator.AddAssembly(ns);

            this.MarkdownBaseType = appHost.Config.MarkdownBaseType ?? this.MarkdownBaseType;
            this.MarkdownGlobalHelpers = appHost.Config.MarkdownGlobalHelpers ?? this.MarkdownGlobalHelpers;

            this.MarkdownReplaceTokens = appHost.Config.MarkdownReplaceTokens ?? new Dictionary<string, string>();
            if (!appHost.Config.WebHostUrl.IsNullOrEmpty() && this.MarkdownReplaceTokens.ContainsKey("~/"))
                this.MarkdownReplaceTokens["~/"] = appHost.Config.WebHostUrl.WithTrailingSlash();

            if (VirtualPathProvider == null)
                VirtualPathProvider = AppHost.VirtualPathProvider;

            RegisterMarkdownPages(appHost.Config.MarkdownSearchPath);

            appHost.CatchAllHandlers.Add((httpMethod, pathInfo, filePath) => {
                MarkdownPage markdownPage = null;

                if (catchAllPathsNotFound.Contains(pathInfo))
                    return null;

                if (filePath != null)
                    markdownPage = GetContentPage(filePath.WithoutExtension());

                if (filePath != null)
                    markdownPage = GetContentResourcePage(pathInfo);

                if (WatchForModifiedPages)
                    ReloadModifiedPageAndTemplates(markdownPage);

                if (markdownPage == null)
                {
                    if (catchAllPathsNotFound.Count > 1000) //prevent DDOS
                        catchAllPathsNotFound = new HashSet<string>();

                    catchAllPathsNotFound.Add(pathInfo);
                    return null;
                }
                
                return new MarkdownHandler {
                    MarkdownFormat = this,
                    MarkdownPage = markdownPage,
                    RequestName = "MarkdownPage",
                    PathInfo = pathInfo,
                    FilePath = filePath
                };
            });

            appHost.ContentTypeFilters.Register(ContentType.MarkdownText, SerializeToStream, null);
            appHost.ContentTypeFilters.Register(ContentType.PlainText, SerializeToStream, null);
            appHost.Config.IgnoreFormatsInMetadata.Add(ContentType.MarkdownText.ToContentFormat());
            appHost.Config.IgnoreFormatsInMetadata.Add(ContentType.PlainText.ToContentFormat());
        }

        public bool ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, object dto)
        {
            MarkdownPage markdownPage;
            if ((markdownPage = GetViewPageByResponse(dto, httpReq)) == null)
                return false;

            if (WatchForModifiedPages)
                ReloadModifiedPageAndTemplates(markdownPage);

            return ProcessMarkdownPage(httpReq, markdownPage, dto, httpRes);
        }

        public bool HasView(string viewName)
        {
            return GetViewPage(viewName) != null;
        }

        public string RenderPartial(string pageName, object model, bool renderHtml)
        {
            return RenderDynamicPage(GetViewPage(pageName), pageName, model, renderHtml, false);
        }

        public bool ProcessMarkdownPage(IHttpRequest httpReq, MarkdownPage markdownPage, object dto, IHttpResponse httpRes)
        {
            httpRes.AddHeaderLastModified(markdownPage.GetLastModified());

            var renderInTemplate = true;
            var renderHtml = true;
            string format;
            if (httpReq != null && (format = httpReq.QueryString["format"]) != null)
            {
                renderHtml = !(format.StartsWithIgnoreCase("markdown")
                    || format.StartsWithIgnoreCase("text")
                    || format.StartsWithIgnoreCase("plain"));
                renderInTemplate = !httpReq.GetFormatModifier().StartsWithIgnoreCase("bare");
            }

            if (!renderHtml)
            {
                httpRes.ContentType = ContentType.PlainText;
            }

            var template = httpReq.GetTemplate();
            var markup = RenderDynamicPage(markdownPage, markdownPage.Name, dto, renderHtml, renderInTemplate, template);
            var markupBytes = markup.ToUtf8Bytes();
            httpRes.OutputStream.Write(markupBytes, 0, markupBytes.Length);

            return true;
        }

        public void ReloadModifiedPageAndTemplates(MarkdownPage markdownPage)
        {
            var lastWriteTime = File.GetLastWriteTime(markdownPage.FilePath);
            if (lastWriteTime > markdownPage.LastModified)
            {
                markdownPage.Reload();
            }

            MarkdownTemplate template;
            if (markdownPage.DirectiveTemplate != null
                && this.MasterPageTemplates.TryGetValue(markdownPage.DirectiveTemplate, out template))
            {
                lastWriteTime = File.GetLastWriteTime(markdownPage.DirectiveTemplate);
                if (lastWriteTime > template.LastModified)
                    ReloadTemplate(template);
            }
            if (markdownPage.Template != null
                && this.MasterPageTemplates.TryGetValue(markdownPage.Template, out template))
            {
                lastWriteTime = File.GetLastWriteTime(markdownPage.Template);
                if (lastWriteTime > template.LastModified)
                    ReloadTemplate(template);
            }
        }

        private void ReloadTemplate(MarkdownTemplate template)
        {
            var contents = File.ReadAllText(template.FilePath);
            foreach (var markdownReplaceToken in MarkdownReplaceTokens)
            {
                contents = contents.Replace(markdownReplaceToken.Key, markdownReplaceToken.Value);
            }
            template.Reload(contents);
        }

        /// <summary>
        /// Render Markdown for text/markdown and text/plain ContentTypes
        /// </summary>
        public void SerializeToStream(IRequestContext requestContext, object dto, Stream stream)
        {
            MarkdownPage markdownPage;
            if ((markdownPage = GetViewPageByResponse(dto, requestContext.Get<IHttpRequest>())) == null)
                throw new InvalidDataException(ErrorPageNotFound.FormatWith(GetPageName(dto, requestContext)));

            ReloadModifiedPageAndTemplates(markdownPage);

            const bool renderHtml = false; //i.e. render Markdown
            var markup = RenderStaticPage(markdownPage, renderHtml);
            var markupBytes = markup.ToUtf8Bytes();
            stream.Write(markupBytes, 0, markupBytes.Length);
        }

        public string GetPageName(object dto, IRequestContext requestContext)
        {
            var httpRequest = requestContext != null ? requestContext.Get<IHttpRequest>() : null;
            var httpResult = dto as IHttpResult;
            if (httpResult != null)
            {
                dto = httpResult.Response;
            }
            if (dto != null) return dto.GetType().Name;
            return httpRequest != null ? httpRequest.OperationName : null;
        }

        public MarkdownPage GetViewPageByResponse(object dto, IHttpRequest httpReq)
        {
            var httpResult = dto as IHttpResult;
            if (httpResult != null)
            {
                dto = httpResult.Response;
            }

            //If View was specified don't look for anything else.
            var viewName = httpReq.GetView();
            if (viewName != null)
                return GetViewPage(viewName);

            if (dto != null)
            {
                var responseTypeName = dto.GetType().Name;
                var markdownPage = GetViewPage(responseTypeName);
                if (markdownPage != null) return markdownPage;
            }

            return httpReq != null ? GetViewPage(httpReq.OperationName) : null;
        }

        public MarkdownPage GetViewPage(string pageName)
        {
            MarkdownPage markdownPage;

            ViewPages.TryGetValue(pageName, out markdownPage);
            if (markdownPage != null) return markdownPage;

            ViewSharedPages.TryGetValue(pageName, out markdownPage);
            return markdownPage;
        }

        public MarkdownPage GetContentPage(string pageFilePath)
        {
            MarkdownPage markdownPage;
            ContentPages.TryGetValue(pageFilePath, out markdownPage);

            return markdownPage;
        }

        static readonly char[] DirSeps = new[] { '\\', '/' };
        public MarkdownPage GetContentResourcePage(string pathInfo)
        {
            MarkdownPage markdownPage;
            ContentPages.TryGetValue(pathInfo.TrimStart(DirSeps), out markdownPage);
            return markdownPage;
        }

        public void RegisterMarkdownPages(string dirPath)
        {
            foreach (var page in FindMarkdownPagesFn(dirPath))
            {
                AddPage(page);
            }

            var templateFiles = VirtualPathProvider.GetAllMatchingFiles("*." + TemplateExt);
            foreach (var templateFile in templateFiles)
            {
                try
                {
                    var templateContents = templateFile.OpenText().ReadToEnd();
                    AddTemplate(templateFile.VirtualPath, templateContents);
                }
                catch (Exception ex)
                {
                    Log.Error("AddTemplate(): " + ex.Message, ex);
                }
            }
        }

        public IEnumerable<MarkdownPage> FindMarkdownPages(string dirPath)
        {
            var hasReloadableWebPages = false;

            var markDownFiles = VirtualPathProvider.GetAllMatchingFiles("*." + MarkdownExt);
            foreach (var markDownFile in markDownFiles)
            {
                if (markDownFile.GetType().Name != "ResourceVirtualFile")
                    hasReloadableWebPages = true;

                var pageName = markDownFile.Name.WithoutExtension();
                var pageContents = markDownFile.ReadAllText();

                var pageType = MarkdownPageType.ContentPage;
                if (VirtualPathProvider.IsSharedFile(markDownFile))
                    pageType = MarkdownPageType.SharedViewPage;
                else if (VirtualPathProvider.IsViewFile(markDownFile))
                    pageType = MarkdownPageType.ViewPage;

                var templatePath = pageType == MarkdownPageType.ContentPage
                    ? GetTemplatePath(markDownFile.Directory)
                    : null;

                yield return new MarkdownPage(this, markDownFile.VirtualPath,
                    pageName, pageContents, pageType) {
                        Template = templatePath,
                        LastModified = markDownFile.LastModified,
                    };
            }                

            if (!hasReloadableWebPages)
                WatchForModifiedPages = false;
        }

        readonly Dictionary<string, IVirtualFile> templatePathsFound = new Dictionary<string, IVirtualFile>(StringComparer.InvariantCultureIgnoreCase);
        readonly HashSet<string> templatePathsNotFound = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        private string GetTemplatePath(IVirtualDirectory fileDir)
        {
            try
            {
                if (templatePathsNotFound.Contains(fileDir.VirtualPath)) return null;

                var templateDir = fileDir;
                IVirtualFile templateFile;
                while (templateDir != null && templateDir.GetFile(DefaultTemplateName) == null)
                {
                    if (templatePathsFound.TryGetValue(templateDir.VirtualPath, out templateFile))
                        return templateFile.RealPath;

                    templateDir = templateDir.ParentDirectory;
                }

                if (templateDir != null)
                {
                    templateFile = templateDir.GetFile(DefaultTemplateName);
                    templatePathsFound[templateDir.VirtualPath] = templateFile;
                    return templateFile.VirtualPath;
                }

                templatePathsNotFound.Add(fileDir.VirtualPath);
                return null;

            }
            catch (Exception ex)
            {
                ex.Message.Print();
                throw;
            }
        }

        public void RegisterMarkdownPage(MarkdownPage markdownPage)
        {
            AddPage(markdownPage);
        }

        public void AddPage(MarkdownPage page)
        {
            try
            {
                page.Prepare();
                AddViewPage(page);
            }
            catch (Exception ex)
            {
                Log.Error("AddViewPage() page.Prepare(): " + ex.Message, ex);
            }

            try
            {
                var templatePath = page.Template;
                if (page.Template == null) return;

                if (MasterPageTemplates.ContainsKey(templatePath)) return;

                //AddTemplate(templatePath, File.ReadAllText(templatePath));

                var templateFile = VirtualPathProvider.GetFile(templatePath);
                var templateContents = templateFile.OpenText().ReadToEnd();
                AddTemplate(templatePath, templateContents);
            }
            catch (Exception ex)
            {
                Log.Error("Error compiling template " + page.Template + ": " + ex.Message, ex);
            }
        }

        private void AddViewPage(MarkdownPage page)
        {
            switch (page.PageType)
            {
                case MarkdownPageType.ViewPage:
                    ViewPages.Add(page.Name, page);
                    break;
                case MarkdownPageType.SharedViewPage:
                    ViewSharedPages.Add(page.Name, page);
                    break;
                case MarkdownPageType.ContentPage:
                    ContentPages.Add(page.FilePath.WithoutExtension().TrimStart(DirSeps), page);
                    break;
            }
        }

        public MarkdownTemplate AddTemplate(string templatePath, string templateContents)
        {
            MarkdownTemplate template;
            if (MasterPageTemplates.TryGetValue(templatePath, out template))             
                return template;

            var templateFile = VirtualPathProvider.GetFile(templatePath);
            var templateName = templateFile.Name.WithoutExtension();

            foreach (var markdownReplaceToken in MarkdownReplaceTokens)
            {
                templateContents = templateContents.Replace(markdownReplaceToken.Key, markdownReplaceToken.Value);
            }

            template = new MarkdownTemplate(templatePath, templateName, templateContents) {
                LastModified = templateFile.LastModified,
            };

            MasterPageTemplates.Add(templatePath, template);

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

        public string Transform(string template)
        {
            return markdown.Transform(template);
        }

        public string Transform(string template, bool renderHtml)
        {
            return renderHtml ? markdown.Transform(template) : template;
        }

        public string RenderStaticPageHtml(string filePath)
        {
            return RenderStaticPage(filePath, true);
        }

        public string RenderStaticPage(string filePath, bool renderHtml)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            filePath = filePath.WithoutExtension();

            MarkdownPage markdownPage;
            if (!ContentPages.TryGetValue(filePath, out markdownPage))
                throw new InvalidDataException(ErrorPageNotFound.FormatWith(filePath));

            return RenderStaticPage(markdownPage, renderHtml);
        }

        private string RenderStaticPage(MarkdownPage markdownPage, bool renderHtml)
        {
            //TODO: Optimize if contains no dynamic elements
            return RenderDynamicPage(markdownPage, new Dictionary<string, object>(), renderHtml, true);
        }

        private string RenderInTemplateIfAny(MarkdownPage markdownPage, Dictionary<string, object> scopeArgs, string pageHtml, string templatePath = null)
        {
            MarkdownTemplate markdownTemplate = null;

            if (templatePath != null)
                MasterPageTemplates.TryGetValue(templatePath, out markdownTemplate);

            var directiveTemplate = markdownPage.DirectiveTemplate;
            if (markdownTemplate == null && directiveTemplate != null)
            {
                if (!MasterPageTemplates.TryGetValue(directiveTemplate, out markdownTemplate))
                {
                    var templateInSharedPath = "{0}/{1}.shtml".Fmt(SharedDir, directiveTemplate);
                    if (!MasterPageTemplates.TryGetValue(templateInSharedPath, out markdownTemplate))
                    {
                        var virtualFile = VirtualPathProvider.GetFile(directiveTemplate);
                        if (virtualFile == null)
                            throw new FileNotFoundException("Could not find template: " + directiveTemplate);

                        var templateContents = virtualFile.ReadAllText();
                        markdownTemplate = AddTemplate(directiveTemplate, templateContents);
                    }
                }
            }

            if (markdownTemplate == null)
            {
                if (markdownPage.Template != null)
                    MasterPageTemplates.TryGetValue(markdownPage.Template, out markdownTemplate);

                if (markdownTemplate == null && templatePath == null)
                    MasterPageTemplates.TryGetValue(DefaultTemplate, out markdownTemplate);

                if (markdownTemplate == null)
                {
                    throw new Exception("No template found for page: " + markdownPage.FilePath);
                }
            }

            if (scopeArgs != null)
                scopeArgs[MarkdownTemplate.BodyPlaceHolder] = pageHtml;

            var htmlPage = markdownTemplate.RenderToString(scopeArgs);

            return htmlPage;
        }

        public string RenderDynamicPageHtml(string pageName, object model)
        {
            return RenderDynamicPage(pageName, model, true);
        }

        public string RenderDynamicPageHtml(string pageName)
        {
            return RenderDynamicPage(GetViewPage(pageName), new Dictionary<string, object>(), true, true);
        }

        public string RenderDynamicPageHtml(string pageName, Dictionary<string, object> scopeArgs)
        {
            return RenderDynamicPage(GetViewPage(pageName), scopeArgs, true, true);
        }

        public string RenderDynamicPage(string pageName, object model, bool renderHtml)
        {
            return RenderDynamicPage(GetViewPage(pageName), pageName, model, renderHtml, true);
        }

        private string RenderDynamicPage(MarkdownPage markdownPage, string pageName, object model, bool renderHtml, bool renderTemplate, string templatePath = null)
        {
            if (markdownPage == null)
                throw new InvalidDataException(ErrorPageNotFound.FormatWith(pageName));

            var scopeArgs = new Dictionary<string, object> { { MarkdownPage.ModelName, model } };

            return RenderDynamicPage(markdownPage, scopeArgs, renderHtml, renderTemplate, templatePath);
        }

        public string RenderDynamicPage(MarkdownPage markdownPage, Dictionary<string, object> scopeArgs,
            bool renderHtml, bool renderTemplate, string templatePath = null)
        {
            scopeArgs = scopeArgs ?? new Dictionary<string, object>();
            var htmlPage = markdownPage.RenderToString(scopeArgs, renderHtml);
            if (!renderTemplate) return htmlPage;

            var html = RenderInTemplateIfAny(
                markdownPage, scopeArgs, htmlPage, templatePath);

            return html;
        }
    }
}
