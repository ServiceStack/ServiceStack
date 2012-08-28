using System.Collections.Generic;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.VirtualPath;
using ServiceStack.WebHost.Endpoints.Formats;
using ServiceStack.WebHost.Endpoints.Support.Markdown;

namespace ServiceStack.ServiceHost.Tests.Formats
{
	public class MarkdownTestBase
	{
		public const string TemplateName = "Template";
		protected const string PageName = "Page";

		public MarkdownFormat Create(string websiteTemplate, string pageTemplate)
		{
			var markdownFormat = new MarkdownFormat {
			    VirtualPathProvider = new InMemoryVirtualPathProvider(new BasicAppHost())
            };

            markdownFormat.AddFileAndTemplate("websiteTemplate", websiteTemplate);
			markdownFormat.AddPage(
				new MarkdownPage(markdownFormat, "/path/to/tpl", PageName, pageTemplate) {
                    Template = "websiteTemplate",
				});

			return markdownFormat;
		}

		public MarkdownFormat Create(string pageTemplate)
		{
			var markdownFormat = new MarkdownFormat();
			markdownFormat.AddPage(
				new MarkdownPage(markdownFormat, "/path/to/tpl", PageName, pageTemplate));

			return markdownFormat;
		}

		public string RenderToHtml(string pageTemplate, Dictionary<string, object> scopeArgs)
		{
			var markdown = Create(pageTemplate);
			var html = markdown.RenderDynamicPageHtml(PageName, scopeArgs);
			return html;
		}

		public string RenderToHtml(string pageTemplate, Dictionary<string, object> scopeArgs, string websiteTemplate)
		{
			var markdown = Create(pageTemplate);
			var html = markdown.RenderDynamicPageHtml(PageName, scopeArgs);
			return html;
		}

		public string RenderToHtml(string pageTemplate, object model)
		{
			var markdown = Create(pageTemplate);
			var html = markdown.RenderDynamicPageHtml(PageName, model);
			return html;
		}
	}

	public static class MarkdownTestExtensions
	{
		public static string NormalizeNewLines(this string text)
		{
			return text.Replace("\r\n", "\n");
		}
	}
}