using System;
using System.Linq.Expressions;
using ServiceStack.WebHost.EndPoints.Formats;

namespace ServiceStack.Markdown
{
	public class HtmlHelper
	{
		//some mockable love...
		private MarkdownFormat markdown;
		public MarkdownFormat Markdown
		{
			get { return markdown ?? MarkdownFormat.Instance; }
			set { markdown = value; }
		}

		public static HtmlHelper Instance = new HtmlHelper();

		public string Partial(string viewName, object model)
		{
			var result = Markdown.RenderDynamicPage(viewName, model);
			return result;
		}

		public string Raw(string content)
		{
			return content;
		}
	}
}