using System;
using System.Collections.Generic;
using System.IO;
using MarkdownSharp;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Formats;

namespace ServiceStack.WebHost.EndPoints.Formats
{
	public class MarkdownFormat
	{
		public static string TemplateName = "default.htm";
		public static string TemplatePlaceHolder = "<!--Response-->";

		public static MarkdownFormat Instance = new MarkdownFormat();

		public Dictionary<string, MarkdownPage> Pages = new Dictionary<string, MarkdownPage>(
			StringComparer.CurrentCultureIgnoreCase);

		public Dictionary<string, MarkdownTemplate> PageTemplates = new Dictionary<string, MarkdownTemplate>(
			StringComparer.CurrentCultureIgnoreCase);

		private Markdown markdown;

		public MarkdownFormat()
		{
			markdown = new Markdown();
		}

		public class MarkdownPage
		{
			public MarkdownPage() { }

			public MarkdownPage(string fullPath, string name, string contents)
			{
				Path = fullPath;
				Name = name;
				Contents = contents;
			}

			public string Path { get; set; }
			public string Name { get; set; }
			public string Contents { get; set; }

			public string GetTemplatePath()
			{
				var tplName = System.IO.Path.Combine(
					System.IO.Path.GetDirectoryName(this.Path),
					TemplateName);

				return tplName;
			}
		}

		public class MarkdownTemplate
		{
			public MarkdownTemplate() { }

			public MarkdownTemplate(string fullPath, string name, string contents)
			{
				Path = fullPath;
				Name = name;
				Contents = contents;
			}

			public string Path { get; set; }
			public string Name { get; set; }
			public string Contents { get; set; }
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

		private void AddPage(MarkdownPage page)
		{
			Pages.Add(page.Name, page);

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

				PageTemplates.Add(template.Path, template);
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
			
			MarkdownTemplate markdownTemplate;
			PageTemplates.TryGetValue(templatePath, out markdownTemplate);
			if (markdownTemplate == null) return pageHtml;
			
			var htmlPage = markdownTemplate.Contents.ReplaceFirst(
				TemplatePlaceHolder, pageHtml);
			
			return htmlPage;
		}
	}
}