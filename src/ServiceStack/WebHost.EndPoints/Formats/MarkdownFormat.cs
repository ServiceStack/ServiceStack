using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Markdown;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Formats;
using ServiceStack.WebHost.EndPoints.Support.Markdown;

namespace ServiceStack.WebHost.EndPoints.Formats
{
	public enum MarkdownPageType
	{
		ContentPage = 1,
		ViewPage = 2,
		SharedViewPage = 3,
	}

	public class MarkdownFormat
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(MarkdownFormat));

		private const string ErrorPageNotFound = "Could not find Markdown page '{0}'";

		public static string TemplateName = "default.htm";
		public static string TemplatePlaceHolder = "<!--@Content-->";

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

		public Dictionary<string, MarkdownTemplate> PageTemplates = new Dictionary<string, MarkdownTemplate>(
			StringComparer.CurrentCultureIgnoreCase);

		public Type MarkdownBaseType { get; set; }
		public Dictionary<string, Type> MarkdownGlobalHelpers { get; set; }

		public Func<string, IEnumerable<MarkdownPage>> FindMarkdownPagesFn { get; set; }

		private readonly MarkdownSharp.Markdown markdown;

		public MarkdownFormat()
		{
			markdown = new MarkdownSharp.Markdown();

			this.MarkdownBaseType = typeof(MarkdownViewBase);
			this.MarkdownGlobalHelpers = new Dictionary<string, Type>();
			this.FindMarkdownPagesFn = FindMarkdownPages;
		}

		public void Register(IAppHost appHost)
		{
			RegisterMarkdownPages(appHost.Config.WebHostPhysicalPath);

			//Render HTML
			HtmlFormat.ContentResolvers.Add((requestContext, dto, stream) => {

				MarkdownPage markdownPage;
				if ((markdownPage = GetViewPageByResponse(dto, requestContext.Get<IHttpRequest>())) == null)
					return false;

				var markup = RenderStaticPage(markdownPage, true);
				var markupBytes = markup.ToUtf8Bytes();
				stream.Write(markupBytes, 0, markupBytes.Length);
				return true;
			});

			appHost.ContentTypeFilters.Register(ContentType.MarkdownText, SerializeToStream, null);
			appHost.ContentTypeFilters.Register(ContentType.PlainText, SerializeToStream, null);
		}

		/// <summary>
		/// Render Markdown for text/markdown and text/plain ContentTypes
		/// </summary>
		public void SerializeToStream(IRequestContext requestContext, object dto, Stream stream)
		{
			MarkdownPage markdownPage;
			if ((markdownPage = GetViewPageByResponse(dto, requestContext.Get<IHttpRequest>())) == null)
				throw new InvalidDataException(ErrorPageNotFound.FormatWith(GetPageName(dto, requestContext)));

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
				if (httpResult.TemplateName != null) return httpResult.TemplateName;
				dto = httpResult.Response;
			}
			if (dto != null) return dto.GetType().Name;
			return httpRequest != null ? httpRequest.OperationName : null;
		}

		public MarkdownPage GetViewPageByResponse(object dto, IHttpRequest httpRequest)
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

		public MarkdownPage GetViewPage(string pageName)
		{
			MarkdownPage markdownPage;

			ViewPages.TryGetValue(pageName, out markdownPage);
			if (markdownPage != null) return markdownPage;

			ViewSharedPages.TryGetValue(pageName, out markdownPage);
			return markdownPage;
		}

		public MarkdownPage GetContentPage(string pageName)
		{
			MarkdownPage markdownPage;
			ContentPages.TryGetValue(pageName, out markdownPage);

			return markdownPage;
		}

		readonly Dictionary<string,string> templatePathsFound = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		readonly HashSet<string> templatePathsNotFound = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		public void RegisterMarkdownPages(string dirPath)
		{
			foreach (var page in FindMarkdownPagesFn(dirPath))
			{
				AddPage(page);
			}
		}

		public IEnumerable<MarkdownPage> FindMarkdownPages(string dirPath)
		{
			var di = new DirectoryInfo(dirPath);
			var markDownFiles = di.GetMatchingFiles("*.md")
				.Concat(di.GetMatchingFiles("*.markdown"));

			var viewPath = Path.Combine(di.FullName, "Views");
			var viewSharedPath = Path.Combine(viewPath, "Shared");

			foreach (var markDownFile in markDownFiles)
			{
				var fileInfo = new FileInfo(markDownFile);
				var pageName = fileInfo.Name.WithoutExtension();
				var pageContents = File.ReadAllText(markDownFile);

				var pageType = MarkdownPageType.ContentPage;
				if (fileInfo.FullName.StartsWithIgnoreCase(viewSharedPath))
					pageType = MarkdownPageType.SharedViewPage;
				else if (fileInfo.FullName.StartsWithIgnoreCase(viewPath))
					pageType = MarkdownPageType.ViewPage;

				var templatePath = GetTemplatePath(fileInfo.DirectoryName);

				yield return new MarkdownPage(this, markDownFile, pageName, pageContents, pageType) {
					TemplatePath = templatePath,
				};
			}
		}

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

		public void RegisterMarkdownPage(MarkdownPage markdownPage)
		{
			AddPage(markdownPage);
		}

		private void AddPage(MarkdownPage page)
		{
			try
			{
				page.Prepare();
				switch (page.PageType)
				{
					case MarkdownPageType.ViewPage:
						ViewPages.Add(page.Name, page);
						break;
					case MarkdownPageType.SharedViewPage:
						ViewSharedPages.Add(page.Name, page);
						break;
					case MarkdownPageType.ContentPage:
						ContentPages.Add(page.FilePath.WithoutExtension(), page);
						break;
				}
			}
			catch (Exception ex)
			{
				Log.Error("AddViewPage() page.Prepare(): " + ex.Message, ex);
			}

			var templatePath = page.TemplatePath;
			if (page.TemplatePath == null) return;
			
			if (PageTemplates.ContainsKey(templatePath)) return;

			var templateName = Path.GetFileName(templatePath).WithoutExtension();
			var pageContents = File.ReadAllText(templatePath);
			var template = new MarkdownTemplate(templatePath, templateName, pageContents);
			
			PageTemplates.Add(templatePath, template);

			try
			{
				template.Prepare();
				PageTemplates.Add(template.FilePath, template);
			}
			catch (Exception ex)
			{
				Log.Error("AddViewPage() template.Prepare(): " + ex.Message, ex);
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
			var pageHtml = Transform(markdownPage.Contents, renderHtml);
			var templatePath = markdownPage.TemplatePath;

			return RenderInTemplateIfAny(templatePath, pageHtml);
		}

		private string RenderInTemplateIfAny(string templatePath, string pageHtml)
		{
			if (templatePath == null) return pageHtml;

			var markdownTemplate = PageTemplates[templatePath];

			var htmlPage = markdownTemplate.Contents.ReplaceFirst(TemplatePlaceHolder, pageHtml);

			return htmlPage;
		}

		public string RenderDynamicPageHtml(string pageName, object model)
		{
			return RenderDynamicPage(pageName, model, true);
		}

		public string RenderDynamicPage(string pageName, object model, bool renderHtml)
		{
			var markdownPage = GetViewPage(pageName);
			if (markdownPage == null)
				throw new InvalidDataException(ErrorPageNotFound.FormatWith(pageName));

			var scopeArgs = new Dictionary<string, object> { { MarkdownPage.ModelName, model } };

			var htmlPage = markdownPage.RenderToString(scopeArgs, renderHtml);

			var html = RenderInTemplateIfAny(markdownPage.TemplatePath, htmlPage);

			return html;
		}
	}
}