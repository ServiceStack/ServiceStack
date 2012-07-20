using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Html;
using ServiceStack.Logging;
using ServiceStack.Razor.Templating;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor
{
	public enum RazorPageType
	{
		ContentPage = 1,
		ViewPage = 2,
		SharedViewPage = 3,
		Template = 4,
	}

	public class RazorFormat : ITemplateResolver, IActivator, IViewEngine, IPlugin, IRazorPlugin
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(RazorFormat));

		public static RazorFormat Instance = new RazorFormat();

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

		public RazorFormat()
		{
			this.FindRazorPagesFn = FindRazorPages;
			this.ReplaceTokens = new Dictionary<string, string>();
		}

		public void Register(IAppHost appHost)
		{
			Instance.Configure(appHost);
		}

		public void Configure(IAppHost appHost)
		{
			this.AppHost = appHost;
			this.ReplaceTokens = new Dictionary<string, string>(appHost.Config.MarkdownReplaceTokens);
			if (!appHost.Config.WebHostUrl.IsNullOrEmpty())
				this.ReplaceTokens["~/"] = appHost.Config.WebHostUrl.WithTrailingSlash();

			var razorBaseType = appHost.Config.RazorBaseType;
			Init(razorBaseType);

			RegisterRazorPages(appHost.Config.MarkdownSearchPath);

			//Render HTML
			appHost.HtmlProviders.Add((requestContext, dto, httpRes) => {

				var httpReq = requestContext.Get<IHttpRequest>();
				ViewPage razorPage;
				if ((razorPage = GetViewPageByResponse(dto, httpReq)) == null)
					return false;

				ReloadModifiedPageAndTemplates(razorPage);

				return ProcessRazorPage(httpReq, razorPage, dto, httpRes);
			});

			appHost.CatchAllHandlers.Add((httpMethod, pathInfo, filePath) => {
				ViewPage razorPage;
				if (filePath == null || (razorPage = GetContentPage(filePath.WithoutExtension())) == null) 
                    return null;

				return new RazorHandler {
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
			//Force Binder to load
			var loaded = typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly != null;
			if (!loaded)
				throw new ConfigurationErrorsException("Microsoft.CSharp not properly loaded");

			if (razorBaseType != null)
			{
				if (!razorBaseType.HasInterface(typeof(ITemplatePage)))
					throw new ConfigurationErrorsException(razorBaseType.FullName + " must inherit from RazorBasePage");

				RazorHost.SetTemplateBase(razorBaseType);
			}
			else
			{
				RazorHost.SetTemplateBase(typeof(ViewPage<>));
			}

			RazorHost.DefaultTemplateService.RazorFormat = this;
			RazorHost.AddResolver(this);
			RazorHost.SetActivator(this);
		}

		public IEnumerable<ViewPage> FindRazorPages(string dirPath)
		{
			var di = new DirectoryInfo(dirPath);
			var razorFiles = di.GetMatchingFiles("*.cshtml");

			var viewPath = Path.Combine(di.FullName, "Views");
			var viewSharedPath = Path.Combine(viewPath, "Shared");

			foreach (var razorFile in razorFiles)
			{
				var fileInfo = new FileInfo(razorFile);
				var pageName = fileInfo.Name.WithoutExtension();
				var pageContents = File.ReadAllText(razorFile);

				var pageType = RazorPageType.ContentPage;
				if (fileInfo.FullName.StartsWithIgnoreCase(viewSharedPath))
					pageType = RazorPageType.SharedViewPage;
				else if (fileInfo.FullName.StartsWithIgnoreCase(viewPath))
					pageType = RazorPageType.ViewPage;

				var templatePath = GetTemplatePath(fileInfo.DirectoryName);

				yield return new ViewPage(this, razorFile, pageName, pageContents, pageType) {
					TemplatePath = templatePath,
					LastModified = fileInfo.LastWriteTime,
				};
			}
		}

		readonly Dictionary<string,string> templatePathsFound = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		readonly HashSet<string> templatePathsNotFound = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		private string GetTemplatePath(string fileDirPath)
		{
			if (templatePathsNotFound.Contains(fileDirPath)) return null;

			var templateDirPath = fileDirPath;
			string templatePath;
			while (templateDirPath != null && !File.Exists(Path.Combine(templateDirPath, TemplateName)))
			{
				if (templatePathsFound.TryGetValue(templateDirPath, out templatePath))
					return templatePath;

				templateDirPath = templateDirPath.ParentDirectory();
			}

			if (templateDirPath != null)
			{
				templatePath = Path.Combine(templateDirPath, TemplateName);
				templatePathsFound[templateDirPath] = templatePath;
				return templatePath;
			}

			templatePathsNotFound.Add(fileDirPath);
			return null;
		}

		public bool ProcessRazorPage(IHttpRequest httpReq, ViewPage razorPage, object dto, IHttpResponse httpRes)
		{
			httpRes.AddHeaderLastModified(razorPage.GetLastModified());

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
			var instance = ReflectionUtils.CreateInstance(type);

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
            return RazorHost.DefaultTemplateService.ExecuteTemplate(model, name, templatePath, httpReq, httpRes);
		}

		public string RenderPartial(string pageName, object model, bool renderHtml)
		{
			//Razor writes partial to static StringBuilder so don't return or it will write x2
			var template = RazorHost.DefaultTemplateService.RenderPartial(model, pageName);
			//return template.Result;
			return null;
		}
	}
}