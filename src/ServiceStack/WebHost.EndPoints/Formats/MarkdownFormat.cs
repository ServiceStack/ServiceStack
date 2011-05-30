using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
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
	public class MarkdownFormat
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(MarkdownFormat));

		public static string TemplateName = "default.htm";
		public static string TemplatePlaceHolder = "<!--@Content-->";

		public static MarkdownFormat Instance = new MarkdownFormat();

		public Dictionary<string, MarkdownPage> Pages = new Dictionary<string, MarkdownPage>(
			StringComparer.CurrentCultureIgnoreCase);

		public Dictionary<string, MarkdownTemplate> PageTemplates = new Dictionary<string, MarkdownTemplate>(
			StringComparer.CurrentCultureIgnoreCase);

		public Type MarkdownBaseType { get; set; }
		public Dictionary<string, Type> MarkdownGlobalHelpers { get; set; }

		private readonly MarkdownSharp.Markdown markdown;

		public MarkdownFormat()
		{
			markdown = new MarkdownSharp.Markdown();

			this.MarkdownBaseType = typeof(MarkdownViewBase);
			this.MarkdownGlobalHelpers = new Dictionary<string, Type>();
		}

		public void Register(IAppHost appHost)
		{
			RegisterMarkdownPages("~".MapHostAbsolutePath());

			//Render HTML
			HtmlFormat.ContentResolvers.Add((requestContext, dto, stream) => {
				var pageName = dto.GetType().Name;

				MarkdownPage markdownPage;
				if (!Pages.TryGetValue(pageName, out markdownPage))
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
			var pageName = dto.GetType().Name;

			MarkdownPage markdownPage;
			if (!Pages.TryGetValue(pageName, out markdownPage))
				throw new InvalidDataException("Could not find markdown page");

			const bool renderHtml = false; //i.e. render Markdown
			var markup = RenderStaticPage(markdownPage, renderHtml);
			var markupBytes = markup.ToUtf8Bytes();
			stream.Write(markupBytes, 0, markupBytes.Length);
		}

		public void RegisterMarkdownPages(string dirPath)
		{
			var di = new DirectoryInfo(dirPath);
			var markDownFiles = di.GetMatchingFiles("*.md");

			foreach (var markDownFile in markDownFiles)
			{
				var fileInfo = new FileInfo(markDownFile);
				var pageName = fileInfo.Name.SplitOnFirst('.')[0];
				var pageContents = File.ReadAllText(markDownFile);

				AddPage(new MarkdownPage(this, markDownFile, pageName, pageContents));
			}
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
				Pages.Add(page.Name, page);
			}
			catch (Exception ex)
			{
				Log.Error("AddPage() page.Prepare(): " + ex.Message, ex);
			}

			var templatePath = page.GetTemplatePath();

			if (PageTemplates.ContainsKey(templatePath)) return;

			var templateFile = new FileInfo(templatePath);

			if (!templateFile.Exists)
			{
				PageTemplates.Add(templateFile.FullName, null);
				return;
			}

			var pageContents = File.ReadAllText(templatePath);
			var template = new MarkdownTemplate(
			templatePath, templateFile.Name, pageContents);

			try
			{
				template.Prepare();
				PageTemplates.Add(template.FilePath, template);
			}
			catch (Exception ex)
			{
				Log.Error("AddPage() template.Prepare(): " + ex.Message, ex);
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

		public string RenderStaticPageHtml(string pageName)
		{
			return RenderStaticPage(pageName, true);
		}

		public string RenderStaticPage(string pageName, bool renderHtml)
		{
			MarkdownPage markdownPage;
			if (!Pages.TryGetValue(pageName, out markdownPage))
				throw new KeyNotFoundException(pageName);

			return RenderStaticPage(markdownPage, renderHtml);
		}

		private string RenderStaticPage(MarkdownPage markdownPage, bool renderHtml)
		{
			var pageHtml = Transform(markdownPage.Contents, renderHtml);
			var templatePath = markdownPage.GetTemplatePath();

			return RenderInTemplateIfAny(templatePath, pageHtml);
		}

		private string RenderInTemplateIfAny(string templatePath, string pageHtml)
		{
			MarkdownTemplate markdownTemplate;
			PageTemplates.TryGetValue(templatePath, out markdownTemplate);
			if (markdownTemplate == null) return pageHtml;
			var htmlPage = markdownTemplate.Contents.ReplaceFirst(
			TemplatePlaceHolder, pageHtml); 
			return htmlPage;
		}

		public string RenderDynamicPageHtml(string pageName, object model)
		{
			return RenderDynamicPage(pageName, model, true);
		}

		public string RenderDynamicPage(string pageName, object model, bool renderHtml)
		{
			MarkdownPage markdownPage;
			if (!Pages.TryGetValue(pageName, out markdownPage))
				throw new KeyNotFoundException(pageName);

			var scopeArgs = new Dictionary<string, object> { { MarkdownPage.ModelName, model } };

			var htmlPage = markdownPage.RenderToString(scopeArgs, renderHtml);

			var html = RenderInTemplateIfAny(markdownPage.GetTemplatePath(), htmlPage);

			return html;
		}
	}
}