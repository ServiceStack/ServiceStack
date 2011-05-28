using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MarkdownSharp;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Logging;
using ServiceStack.Markdown;
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

			HtmlFormat.ContentResolvers.Add((requestContext, dto, stream) => {
				var pageName = dto.GetType().Name;

				MarkdownPage markdownPage;
				if (!Pages.TryGetValue(pageName, out markdownPage))
					return false;

				var html = RenderStaticPage(markdownPage);
				var htmlBytes = html.ToUtf8Bytes();
				stream.Write(htmlBytes, 0, htmlBytes.Length);
				return true;
			});
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

				AddPage(new MarkdownPage(markDownFile, pageName, pageContents));
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

			if (!PageTemplates.ContainsKey(templatePath))
			{
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
		}

		public string RenderStaticPage(string pageName)
		{
			MarkdownPage markdownPage;
			if (!Pages.TryGetValue(pageName, out markdownPage))
				throw new KeyNotFoundException(pageName);

			return RenderStaticPage(markdownPage);
		}

		private string RenderStaticPage(MarkdownPage markdownPage)
		{
			var pageHtml = markdown.Transform(markdownPage.Contents);
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

		public string Transform(string markdownText)
		{
			var pageHtml = markdown.Transform(markdownText);
			return pageHtml;
		}

		public string RenderDynamicPage(string pageName, object model)
		{
			MarkdownPage markdownPage;
			if (!Pages.TryGetValue(pageName, out markdownPage))
				throw new KeyNotFoundException(pageName);

			var scopeArgs = new Dictionary<string, object> { { MarkdownPage.ModelName, model } };

			var htmlPage = markdownPage.RenderToString(scopeArgs);

			var html = RenderInTemplateIfAny(markdownPage.GetTemplatePath(), htmlPage);

			return html;
		}
	}
}