using System.Collections.Generic;
using ServiceStack.Razor;

namespace ServiceStack.ServiceHost.Tests.Formats_Razor
{
	public class RazorTestBase
	{
		public const string TemplateName = "Template";
		protected const string PageName = "Page";

		public RazorFormat Create(string websiteTemplate, string pageTemplate)
		{
			var razorFormat = new RazorFormat();

			razorFormat.AddTemplate("/path/to/websitetpl", websiteTemplate);
			razorFormat.AddPage(
				new ViewPage(razorFormat, "/path/to/tpl", PageName, pageTemplate) {
					TemplatePath = "/path/to/websitetpl",
				});

			return razorFormat;
		}

		public RazorFormat Create(string pageTemplate)
		{
			var razorFormat = new RazorFormat();
			razorFormat.AddPage(
				new ViewPage(razorFormat, "/path/to/tpl", PageName, pageTemplate));

			return razorFormat;
		}

		public string RenderToHtml(string pageTemplate, Dictionary<string, object> scopeArgs)
		{
			var razorFormat = Create(pageTemplate);
			var template = razorFormat.ExecuteTemplate(scopeArgs, PageName, null);
			return template.Result;
		}

		public string RenderToHtml(string pageTemplate, Dictionary<string, object> scopeArgs, string websiteTemplate)
		{
			var razorFormat = Create(pageTemplate);
			var template = razorFormat.ExecuteTemplate(scopeArgs, PageName, websiteTemplate);
			return template.Result;
		}

		public string RenderToHtml<T>(string pageTemplate, T model)
		{
			var razorFormat = Create(pageTemplate);
			var template = razorFormat.ExecuteTemplate(model, PageName, null);
			return template.Result;
		}
	}

}